using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneRestSpaceSmoke
    {
        private static int checks;
        private static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("Rest space: "+message);}
        private static CafeResponse Command(LocalCafeBackend b,string kind,string choice=null,int slot=1,string[] working=null,string cat=null)
        {
            CafeResponse result=null;var cmd=new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,save_slot=slot,save_id=slot.ToString(),working_cats=working??new string[0]};
            var it=b.Request("/commands",JsonUtility.ToJson(cmd),r=>result=r);while(it.MoveNext()){}return result;
        }
        private static void Good(LocalCafeBackend b,string kind,string choice=null,int slot=1,string[] working=null){var r=Command(b,kind,choice,slot,working);Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static CafeSnapshot Clone(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();CafeDoublePrecision.Restore(s,CafeDoublePrecision.Capture(b.Simulation.State));return s;}
        private static string Capture(LocalCafeBackend b){var s=Clone(b);s.instance_id="";s.revision=0;return JsonUtility.ToJson(s);}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null,string[] working=null,string cat=null){string before=Capture(b);Check(Command(b,kind,choice,working:working,cat:cat).result==UnityWebRequest.Result.ProtocolError,"Invalid command accepted: "+kind);Check(before==Capture(b),"Rejected command changed state");}
        private static void Save(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Save/resume changed equipment or recovery");}
        private static void Events(LocalCafeBackend b)
        {
            StandaloneGrowthSmoke.ResolvePending(b);
            if(b.Simulation.IntakeWaiting)Good(b,"resolve_intake","decline");if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction","decline");if(b.Simulation.ReservationWaiting)Good(b,"resolve_reservation","decline");
            if(b.Simulation.GoalWaiting)Good(b,b.Simulation.State.goal.Count<3?"advance_goal":"continue_goal");
        }
        public static void Verify(){string root=Environment.GetEnvironmentVariable("CATCAFE_REST_SPACE_SMOKE")??"rest-space-smoke";Directory.CreateDirectory(root);int total=Run(root);File.WriteAllText(Path.Combine(root,"result.json"),"{\"passed\":true,\"checks\":"+total+"}");}
        public static int Run(string root)
        {
            checks=0;
            foreach(string mode in new[]{"free","popularity","bond","patron"})
            {
                string path=Path.Combine(root,mode);var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.starting_funds=20000;rules.store_events.probability=0;rules.rules_id+="-rest-space";rules.growth.threshold=1000;rules.goal_stages=new[]{101d,102d,103d}.Select(v=>new GoalStageRule {days=10,target=v}).ToArray();rules.max_probability=0;rules.runaway_threshold=100;rules.stress_per_service_tick=0.2;
                var b=new LocalCafeBackend(path,rules);
                if(File.Exists(Path.Combine(path,"slot-8.json")))foreach(int slot in Enumerable.Range(2,7))
                {
                    Good(b,"load_game",slot:slot);Check(b.Simulation.State.objective==mode,"Restart changed mode");Check(b.Simulation.State.rest_space.purchased_day==(slot==2?0:1),"Restart lost purchase");
                    if(mode=="popularity" && slot>=7)Check(b.Simulation.State.rest_space.upgraded_day>0 && b.Simulation.State.rest_space.soundproofed_day>0,"Restart lost upgrade or soundproofing");
                    if(mode=="popularity" && slot==6)Check(b.Simulation.State.rest_space.upgraded_day>0 && b.Simulation.State.rest_space.soundproofed_day==0,"Restart lost pre-soundproof stage");
                    Check(b.View().finance.equipment_expenses==(float)b.Simulation.State.rest_space_expenses,"Restart lost finance projection");
                }
                Good(b,"new_game",mode);Save(b,2);Reject(b,"upgrade_rest_space");Reject(b,"soundproof_rest_space");Reject(b,"purchase_rest_space",working:new[]{"cat-mugi"});Reject(b,"purchase_rest_space",choice:"invalid");Reject(b,"purchase_rest_space",cat:"cat-mugi");
                double originalFunds=b.Simulation.State.funds;
                foreach(double funds in new[]{399d,400d,401d}){b.Simulation.State.funds=funds;Check(b.View().rest_space.actions[0].can_execute==(funds>400),"Purchase funding boundary differs");}b.Simulation.State.funds=originalFunds;
                Good(b,"purchase_rest_space");Check(b.Simulation.State.funds==19600 && b.View().finance.equipment_expenses==400 && b.View().finance.seat_equipment_expenses==0,"Purchase finance differs");Save(b,3);Reject(b,"purchase_rest_space");Reject(b,"upgrade_rest_space");Reject(b,"soundproof_rest_space");
                for(int day=1;day<=6;day++)
                {
                    Events(b);
                    foreach(string kind in new[]{"upgrade_rest_space","soundproof_rest_space"})if(b.Simulation.RestSpaceReason(kind)=="")
                    {
                        double money=b.Simulation.State.funds,cost=b.Simulation.RestSpaceRule(kind).cost;
                        b.Simulation.State.funds=cost;Check(b.Simulation.RestSpaceReason(kind)!="","Exact cost accepted");b.Simulation.State.funds=cost+1;Check(b.Simulation.RestSpaceReason(kind)=="","Positive remainder rejected");b.Simulation.State.funds=money;
                        Good(b,kind);Reject(b,kind);Save(b,kind=="upgrade_rest_space"?6:7);
                    }
                    Good(b,"set_shifts",working:day%2==0?new[]{"cat-kohaku"}:new[]{"cat-mugi","cat-sora","cat-tama"});
                    var before=b.Simulation.State.cats.ToDictionary(c=>c.id,c=>new[]{c.fatigue,c.stress});double fatigueBonus=b.Simulation.RestSpaceRecovery,stressBonus=b.Simulation.RestSpaceStressRecovery;
                    Good(b,"start_business");Reject(b,"purchase_rest_space");Reject(b,"upgrade_rest_space");Reject(b,"soundproof_rest_space");Save(b,4);
                    while(b.Simulation.State.phase!="closed"){Good(b,"advance_business");Events(b);}
                    foreach(var cat in b.Simulation.State.cats.Where(c=>!c.working && c.activity=="cafe"))
                    {
                        var definition=rules.cats.Single(c=>c.id==cat.id);Check(Math.Abs(cat.fatigue-Math.Max(0,before[cat.id][0]-rules.rest_day_recovery*definition.rest_fatigue-fatigueBonus))<1e-8,"Rest fatigue recovery differs");Check(Math.Abs(cat.stress-Math.Max(0,before[cat.id][1]-rules.rest_recovery-stressBonus))<1e-8,"Rest stress recovery differs");
                    }
                    Save(b,5);Events(b);Good(b,"next_day");Events(b);
                }
                if(mode=="popularity")Check(b.Simulation.State.rest_space.upgraded_day>0 && b.Simulation.State.rest_space.soundproofed_day>0,"Fixture missed advanced facilities");
                if(mode!="popularity"){Save(b,6);Save(b,7);}Save(b,8);
                string final=Capture(b);
                for(int mutation=0;mutation<8;mutation++)
                {
                    var bad=Clone(b);var r=bad.rest_space;if(mutation==0)bad.rest_space=null;else if(mutation==1)r.purchased_day=-1;else if(mutation==2)r.purchased_day=bad.day+1;else if(mutation==3)r.upgraded_day=1;else if(mutation==4)r.soundproofed_day=1;else if(mutation==5)bad.rest_space_expenses++;else if(mutation==6)bad.history[0].rest_space_expenses++;else r.purchased_day=0;
                    bool rejected=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected && final==Capture(b),"Forged equipment save accepted");
                }
                // Isolated day-end probes cover non-cafe cats and day-off recovery without forging saves.
                var probe=new CafeSimulation(rules);probe.State.rest_space=new CafeRestSpace {purchased_day=1,soundproofed_day=1};
                foreach(var cat in probe.State.cats){cat.fatigue=90;cat.stress=80;cat.working=false;}
                probe.State.cats[0].activity="dispatched";probe.State.cats[1].activity="adopted";probe.Execute("day_off");
                Check(probe.State.cats[0].fatigue==90 && probe.State.cats[0].stress==80 && probe.State.cats[1].fatigue==90 && probe.State.cats[1].stress==80,"Non-cafe cats recovered");
                Check(probe.State.cats[2].fatigue<90 && probe.State.cats[2].stress==50,"Day-off bonuses missing");
            }
            return checks;
        }
    }
}
