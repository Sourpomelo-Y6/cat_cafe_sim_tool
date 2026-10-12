using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneRecruitmentSmoke
    {
        private static int checks;
        private static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("Recruitment: "+message);}
        private static CafeResponse Command(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1)
        {
            CafeResponse result=null;var cmd=new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,save_slot=slot,save_id=slot.ToString(),working_cats=new string[0]};
            var it=b.Request("/commands",JsonUtility.ToJson(cmd),r=>result=r);while(it.MoveNext()){}return result;
        }
        private static void Good(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1){var r=Command(b,kind,choice,cat,slot);Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static CafeSnapshot Clone(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();CafeDoublePrecision.Restore(s,CafeDoublePrecision.Capture(b.Simulation.State));return s;}
        private static string Capture(LocalCafeBackend b){var s=Clone(b);s.revision=0;s.instance_id="";return JsonUtility.ToJson(s);}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null,string cat=null){string before=Capture(b);Check(Command(b,kind,choice,cat).result==UnityWebRequest.Result.ProtocolError,"Invalid command accepted: "+kind);Check(before==Capture(b),"Rejected command changed state");}
        private static void Save(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Save/resume changed state");}
        private static void Events(LocalCafeBackend b)
        {
            foreach(var e in b.Simulation.State.dispatches.Where(e=>e.status=="waiting").ToArray())Good(b,"receive_general",e.id,e.cat_id);
            while(b.View().growth_choices.Length>0){var r=b.View().growth_choices[0];Good(b,r.kind,r.choices[0].choice,r.cat_id);}
            if(b.Simulation.IntakeWaiting)Good(b,"resolve_intake","decline");if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction","decline");if(b.Simulation.ReservationWaiting)Good(b,"resolve_reservation","decline");
            foreach(var e in b.Simulation.State.trust_events.Where(e=>e.status=="waiting").ToArray())throw new Exception("Unexpected trust event in isolated fixture");
        }
        public static void Verify(){string root=Environment.GetEnvironmentVariable("CATCAFE_RECRUITMENT_SMOKE")??"recruitment-smoke";Directory.CreateDirectory(root);int total=Run(root);File.WriteAllText(Path.Combine(root,"result.json"),"{\"passed\":true,\"checks\":"+total+"}");}
        private static void NoChange(LocalCafeBackend b){string before=Capture(b);Good(b,"open_recruitment");Check(before==Capture(b),"Early/pending view changed candidates");}
        private static void Holiday(LocalCafeBackend b){Good(b,"day_off");Events(b);}
        public static int Run(string root)
        {
            checks=0;
            foreach(string mode in new[]{"free","popularity","bond","patron"})
            {
                string path=Path.Combine(root,mode);var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.starting_funds=20000;rules.store_events.probability=0;rules.rules_id+="-recruitment";rules.growth.threshold=1;rules.max_probability=rules.stress_per_service_tick=rules.fatigue_per_service_tick=0;foreach(var goal in rules.goal_stages)goal.days=100;
                var b=new LocalCafeBackend(path,rules);
                if(File.Exists(Path.Combine(path,"slot-8.json")))foreach(int slot in Enumerable.Range(2,7))
                {
                    Good(b,"load_game",slot:slot);Check(b.Simulation.State.objective==mode,"Restart changed mode");Check(b.View().recruitment.rows.Length==(slot==2?0:slot<5?3:slot<7?6:21),"Restart lost candidate batches");
                    if(slot==6)Check(b.Simulation.PlayerActive && b.Simulation.ActivePlayer.cat_id=="rescue-1","Restart lost recruited cat exchange");
                    if(slot==8)Check(b.Simulation.State.dispatches.Any(e=>e.cat_id=="rescue-1" && e.status=="resolved"),"Restart lost recruited cat dispatch");
                }
                Good(b,"new_game",mode);Reject(b,"recruit_cat",cat:"rescue-1");Good(b,"player_begin",cat:"cat-mugi");Reject(b,"open_recruitment");Good(b,"player_finish");Save(b,2);
                Good(b,"open_recruitment");Check(b.View().recruitment.rows.Select(r=>r.name).SequenceEqual(new[]{"ハル","リン","ユキ"}) && b.Simulation.NextRecruitmentDay==4,"Initial source candidates differ");Save(b,3);NoChange(b);
                double funds=b.Simulation.State.funds;foreach(double amount in new[]{199.0,200.0,201.0}){b.Simulation.State.funds=amount;Check((b.Simulation.RecruitReason("rescue-1")=="")==(amount>200),"Admission funds boundary differs");}b.Simulation.State.funds=funds;
                Good(b,"player_begin",cat:"cat-mugi");NoChange(b);Reject(b,"recruit_cat",cat:"rescue-1");Good(b,"player_finish");Good(b,"recruit_cat",cat:"rescue-1");Save(b,4);Reject(b,"recruit_cat",cat:"rescue-1");Reject(b,"recruit_cat",cat:"rescue-2");Good(b,"purchase_housing");Good(b,"recruit_cat",cat:"rescue-2");Good(b,"purchase_cat",cat:"shop-1");Check(b.Simulation.State.recruitment_expenses==400 && b.Simulation.State.pet_shop_expenses==400,"Mixed acquisition accounting differs");
                while(b.Simulation.State.day<4){NoChange(b);Holiday(b);}Good(b,"open_recruitment");Check(b.View().recruitment.rows.Length==6 && b.Simulation.NextRecruitmentDay==7,"Refresh date or retained candidates differ");Save(b,5);
                Good(b,"recruit_cat",cat:"rescue-3");var cat=b.Simulation.State.cats.Single(c=>c.id=="rescue-3");Check(!cat.working && cat.health=="healthy" && cat.stamina==rules.max_stamina && cat.fatigue==0 && cat.stress==0 && b.Simulation.PlayerCat(cat.id).affinity==0,"Old candidate initialization differs");
                Good(b,"player_begin",cat:"rescue-1");Good(b,"player_step","direct");Save(b,6);Good(b,"player_finish");b.Simulation.Execute("set_shifts",working:new[]{"rescue-1"});Good(b,"start_business");NoChange(b);Reject(b,"recruit_cat",cat:"rescue-4");while(b.Simulation.State.phase!="closed"){Events(b);Good(b,"advance_business");}Events(b);if(b.Simulation.GoalWaiting)Good(b,"advance_goal");NoChange(b);Good(b,"next_day");Events(b);Check(!string.IsNullOrEmpty(b.Simulation.State.growth.Single(g=>g.cat_id=="rescue-1").specialization),"Recruited cat did not grow");
                for(int batch=2;batch<=6;batch++){while(b.Simulation.State.day<b.Simulation.NextRecruitmentDay)Holiday(b);if(batch==2)for(int delay=0;delay<4;delay++)Holiday(b);Good(b,"open_recruitment");NoChange(b);}
                Check(b.View().recruitment.rows.Length==21 && b.View().recruitment.rows.Single(r=>r.cat_id=="rescue-19").name=="ハル（紹介7）","Cyclic catalog identity differs");Reject(b,"recruit_cat",cat:"rescue-19");Good(b,"upgrade_housing");Good(b,"recruit_cat",cat:"rescue-19");Save(b,7);
                for(int variant=0;variant<8;variant++)
                {
                    string before=Capture(b);var bad=Clone(b);var r=bad.recruitment;if(variant==0)r.days[0]=0;else if(variant==1)r.days[r.days.Count-1]=bad.day+1;else if(variant==2)r.days.Add(r.days.Last()+1);else if(variant==3)r.accepted[0].day=0;else if(variant==4)r.accepted.Add(r.accepted[0]);else if(variant==5)bad.recruitment=null;else if(variant==6)r.accepted=null;else r.days=null;
                    bool rejected=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected && before==Capture(b),"Corrupt recruitment save accepted or changed state");
                }
                Good(b,"dispatch_general","neighborhood_visit","rescue-1");Holiday(b);Check(b.Simulation.State.dispatches.Any(e=>e.cat_id=="rescue-1" && e.status=="resolved"),"Recruited cat dispatch failed");Save(b,8);
            }
            return checks;
        }
    }
}
