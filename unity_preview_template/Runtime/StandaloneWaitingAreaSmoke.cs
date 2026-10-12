using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneWaitingAreaSmoke
    {
        private static int checks;
        private static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("Waiting area: "+message);}
        private static CafeResponse Command(LocalCafeBackend b,string kind,string choice=null,int slot=1,string[] working=null,string cat=null,string eventId=null,string target=null)
        {
            CafeResponse result=null;var cmd=new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,event_id=eventId,target_type=target,save_slot=slot,save_id=slot.ToString(),working_cats=working??new string[0]};
            var it=b.Request("/commands",JsonUtility.ToJson(cmd),r=>result=r);while(it.MoveNext()){}return result;
        }
        private static void Good(LocalCafeBackend b,string kind,string choice=null,int slot=1,string[] working=null,string cat=null,string eventId=null){var r=Command(b,kind,choice,slot,working,cat,eventId);Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static CafeSnapshot Clone(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();CafeDoublePrecision.Restore(s,CafeDoublePrecision.Capture(b.Simulation.State));return s;}
        private static string Capture(LocalCafeBackend b){var s=Clone(b);s.instance_id="";s.revision=0;return JsonUtility.ToJson(s);}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null,string[] working=null,string cat=null,string eventId=null,string target=null){string before=Capture(b);Check(Command(b,kind,choice,working:working,cat:cat,eventId:eventId,target:target).result==UnityWebRequest.Result.ProtocolError,"Invalid command accepted: "+kind);Check(before==Capture(b),"Rejected command changed state");}
        private static void Save(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Save/resume changed limits, queue or finance");}
        private static void Events(LocalCafeBackend b)
        {
            StandaloneGrowthSmoke.ResolvePending(b);
            foreach(var e in b.Simulation.State.trust_events.Where(e=>e.status=="waiting").ToArray())Good(b,"resolve_customer_trust","recover",eventId:e.id);
            if(b.Simulation.IntakeWaiting)Good(b,"resolve_intake","decline");if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction","decline",cat:b.Simulation.Rules.regular_introduction.cat.id);if(b.Simulation.ReservationWaiting)Good(b,"resolve_reservation","decline");
            if(b.Simulation.GoalWaiting){Reject(b,"purchase_waiting_area");Reject(b,"upgrade_waiting_area");Good(b,b.Simulation.State.goal.Count<3?"advance_goal":"continue_goal");}
        }
        public static void Verify(){string root=Environment.GetEnvironmentVariable("CATCAFE_WAITING_AREA_SMOKE")??"waiting-area-smoke";Directory.CreateDirectory(root);int total=Run(root);File.WriteAllText(Path.Combine(root,"result.json"),"{\"passed\":true,\"checks\":"+total+"}");}
        public static int Run(string root)
        {
            checks=0;
            foreach(string mode in new[]{"free","popularity","bond","patron"})
            {
                string path=Path.Combine(root,mode);var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.starting_funds=20000;rules.store_events.probability=0;rules.rules_id+="-waiting-area";rules.growth.threshold=1000;rules.goal_stages=new[]{101d,102d,103d}.Select(v=>new GoalStageRule {days=10,target=v}).ToArray();rules.max_probability=rules.stress_per_service_tick=0;rules.exchange.ticks=10;rules.arrival_ticks=new int[8];rules.patterns=Enumerable.Range(0,7).Select(i=>new WeekPattern {days=Enumerable.Range(0,7).ToArray()}).ToArray();
                var b=new LocalCafeBackend(path,rules);
                if(File.Exists(Path.Combine(path,"slot-9.json")))foreach(int slot in Enumerable.Range(2,8))
                {
                    Good(b,"load_game",slot:slot);int stage=slot==2?0:slot<6?1:2;
                    Check(b.Simulation.State.objective==mode,"Restart changed original mode");Check(b.View().waiting_area.capacity==rules.queue_capacity+(stage==0?0:stage==1?1:2) && b.View().waiting_area.max_wait_ticks==rules.max_wait_ticks+(stage==0?0:stage==1?2:4),"Restart lost limits");
                    Check(b.View().finance.waiting_area_expenses==(float)b.Simulation.State.waiting_area_expenses && b.View().finance.facility_cost==(float)b.Simulation.State.waiting_area_cost,"Restart lost facility finance");
                    if(slot==5 || slot==7)Check(b.Simulation.State.phase=="open" && b.Simulation.State.tick==b.Simulation.MaxWaitTicks-1,"Restart lost deadline boundary");
                    if(slot==9)Check(b.Simulation.State.history.Last().waiting_area_cost==20,"Restart lost day-off upkeep");
                }
                Good(b,"new_game",mode);Save(b,2);Reject(b,"purchase_waiting_area");Reject(b,"upgrade_waiting_area");
                if(mode!="popularity")Good(b,"start_objective","popularity");
                for(int day=1;day<=3;day++)
                {
                    Events(b);
                    if(day>1)
                    {
                        string kind=day==2?"purchase_waiting_area":"upgrade_waiting_area";double funds=b.Simulation.State.funds,cost=b.Simulation.WaitingAreaRule(kind).cost;
                        foreach(double money in new[]{cost-1,cost,cost+1}){b.Simulation.State.funds=money;Check((b.Simulation.WaitingAreaReason(kind)=="")==(money>cost),"Funds boundary differs");}b.Simulation.State.funds=funds;
                        Reject(b,kind,choice:"invalid");Reject(b,kind,working:new[]{"cat-mugi"});Reject(b,kind,cat:"cat-mugi");Reject(b,kind,eventId:"invalid");Reject(b,kind,target:"invalid");
                        int cats=b.Simulation.ActiveCatCount(b.Simulation.State),seats=b.Simulation.State.seat_count;
                        Good(b,kind);Check(b.Simulation.State.funds==funds-cost && b.View().finance.waiting_area_expenses==cost,"Purchase cost differs");Check(cats==b.Simulation.ActiveCatCount(b.Simulation.State) && seats==b.Simulation.State.seat_count,"Waiting purchase changed cats or seats");Reject(b,kind);Save(b,day==2?3:6);
                    }
                    Good(b,"set_shifts",working:rules.cats.Select(c=>c.id).ToArray());Good(b,"start_business");
                    Check(b.Simulation.State.queue.Count==b.Simulation.QueueCapacity,"Crowded arrival capacity differs");Check(b.Simulation.State.visits.Any(v=>v.departure=="queue_full"),"Missing full-queue departures");Reject(b,"purchase_waiting_area");Reject(b,"upgrade_waiting_area");
                    if(day==2)Save(b,4);
                    bool saved=false;while(b.Simulation.State.phase!="closed")
                    {
                        if(day>1 && b.Simulation.State.tick==b.Simulation.MaxWaitTicks-1){Save(b,day==2?5:7);saved=true;}
                        Good(b,"advance_business");Events(b);
                    }
                    Check(day==1 || saved,"Deadline save was not reached");Check(b.Simulation.State.waiting_area_cost==(day==1?0:day==2?10:20),"Daily upkeep differs");
                    Check(b.View().housing.operating_cost==b.View().waiting_area.operating_cost && b.View().expansion.operating_cost==b.View().waiting_area.operating_cost,"Facility estimates disagree");
                    if(day==2)Check(b.Simulation.State.visits.Any(v=>v.departure=="wait_timeout"),"Missing purchased waiting deadline");
                    if(day==3){Check(b.Simulation.State.visits.Count(v=>v.seated_at>=0)>=4,"Upgraded patience did not permit another service");Save(b,8);}
                    Events(b);Good(b,"next_day");Events(b);
                }
                Good(b,"purchase_housing");Good(b,"upgrade_housing");Good(b,"purchase_rest_space");Good(b,"upgrade_rest_space");Good(b,"soundproof_rest_space");Good(b,"expand_seats");
                Check(b.View().waiting_area.operating_cost==rules.base_cost+3*rules.per_seat_cost+rules.housing_upgrade.daily_cost+20,"Combined facility estimate differs");
                double before=b.Simulation.State.funds;int previous=b.Simulation.State.day;Good(b,"day_off");Events(b);
                Check(b.Simulation.State.day==previous+1 && b.Simulation.State.history.Last().waiting_area_cost==20 && b.Simulation.State.funds==before-(rules.base_cost+3*rules.per_seat_cost+rules.housing_upgrade.daily_cost+20),"Day-off maintenance differs");Check(b.Simulation.State.waiting_area_cost==0 && b.Simulation.State.waiting_area_expenses==0,"Next day did not reset expenses");Save(b,9);
                string final=Capture(b);
                for(int mutation=0;mutation<9;mutation++)
                {
                    var bad=Clone(b);var r=bad.waiting_area;if(mutation==0)bad.waiting_area=null;else if(mutation==1)r.purchased_day=-1;else if(mutation==2)r.purchased_day=bad.day+1;else if(mutation==3)r.purchased_day=1;else if(mutation==4)r.upgraded_day=1;else if(mutation==5)bad.waiting_area_expenses++;else if(mutation==6)bad.waiting_area_cost++;else if(mutation==7)bad.history[0].waiting_area_cost++;else bad.history[0].waiting_area_expenses++;
                    bool rejected=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected && final==Capture(b),"Forged waiting area save accepted");
                }
            }
            return checks;
        }
    }
}
