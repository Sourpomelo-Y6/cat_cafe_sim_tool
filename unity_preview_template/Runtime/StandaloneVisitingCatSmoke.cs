using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneVisitingCatSmoke
    {
        private static int checks;
        private const string Cat="visiting-cat-1";
        private static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("Visiting cat: "+message);}
        private static CafeResponse Command(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1)
        {
            CafeResponse result=null;var cmd=new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,save_slot=slot,save_id=slot.ToString(),working_cats=new string[0]};
            var it=b.Request("/commands",JsonUtility.ToJson(cmd),r=>result=r);while(it.MoveNext()){}return result;
        }
        private static void Good(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1){var r=Command(b,kind,choice,cat,slot);Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static CafeSnapshot Clone(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();CafeDoublePrecision.Restore(s,CafeDoublePrecision.Capture(b.Simulation.State));return s;}
        private static string Capture(LocalCafeBackend b){var s=Clone(b);s.revision=0;s.instance_id="";return JsonUtility.ToJson(s);}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null,string cat=null){string before=Capture(b);Check(Command(b,kind,choice,cat).result==UnityWebRequest.Result.ProtocolError,"Invalid command accepted: "+kind);Check(before==Capture(b),"Rejected command changed state");}
        private static void Visit(LocalCafeBackend b,string choice){Good(b,"resolve_visiting_cat",choice,Cat);}
        private static void Save(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Save/resume changed state");}
        private static void Events(LocalCafeBackend b)
        {
            foreach(var e in b.Simulation.State.dispatches.Where(e=>e.status=="waiting").ToArray())Good(b,"receive_general",e.id,e.cat_id);
            while(b.View().growth_choices.Length>0){var r=b.View().growth_choices[0];Good(b,r.kind,r.choices[0].choice,r.cat_id);}
            if(b.Simulation.IntakeWaiting)Good(b,"resolve_intake","decline");if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction","decline");if(b.Simulation.ReservationWaiting)Good(b,"resolve_reservation","decline");
            foreach(var e in b.Simulation.State.trust_events.Where(e=>e.status=="waiting").ToArray())throw new Exception("Unexpected trust event in isolated fixture");
        }
        public static void Verify(){string root=Environment.GetEnvironmentVariable("CATCAFE_VISITING_CAT_SMOKE")??"visiting-cat-smoke";Directory.CreateDirectory(root);int total=Run(root);File.WriteAllText(Path.Combine(root,"result.json"),"{\"passed\":true,\"checks\":"+total+"}");}
        public static int Run(string root)
        {
            checks=0;
            foreach(string mode in new[]{"free","popularity","bond","patron"})
            {
                string path=Path.Combine(root,mode);var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
                rules.starting_funds=20000;rules.store_events.probability=0;rules.rules_id+="-visiting-cat";rules.goal_stages[0].target=101;rules.goal_stages[0].days=30;
                rules.growth.threshold=1;rules.max_probability=rules.stress_per_service_tick=rules.fatigue_per_service_tick=0;
                var b=new LocalCafeBackend(path,rules);
                if(File.Exists(Path.Combine(path,"slot-8.json")))foreach(int slot in Enumerable.Range(2,7))
                {
                    Good(b,"load_game",slot:slot);var v=b.View().visiting_cat;Check(b.Simulation.State.objective==mode,"Restart changed original mode");
                    Check(slot==2?v.status=="untriggered":slot==3?v.status=="visiting" && v.progress==0:slot==4?v.progress==1:slot==5?v.status=="ready" && v.progress==3:v.status=="accepted","Restart lost visiting progress");
                    if(slot>=6)Check(b.Simulation.State.cats.Any(c=>c.id==Cat),"Restart lost accepted cat");
                }
                Good(b,"new_game",mode);Check(b.View().visiting_cat.name=="ココ" && b.View().visiting_cat.required==3 && b.View().visiting_cat.cost==200,"Source configuration differs");
                Reject(b,"resolve_visiting_cat","interact",Cat);Save(b,2);
                if(mode!="popularity")Good(b,"start_objective","popularity");
                Good(b,"start_business");while(b.Simulation.State.phase!="closed"){Events(b);Good(b,"advance_business");}Events(b);
                Check(b.Simulation.State.goal[0].status=="cleared","First popularity stage did not clear");Reject(b,"resolve_visiting_cat","interact",Cat);Good(b,"advance_goal");Good(b,"next_day");Events(b);
                Check(b.View().visiting_cat.presented_day==2 && b.View().visiting_cat.can_interact,"First clear did not unlock next preparation");Save(b,3);
                Reject(b,"resolve_visiting_cat","interact","cat-mugi");Reject(b,"resolve_visiting_cat","invalid",Cat);Visit(b,"skip");Reject(b,"resolve_visiting_cat","interact",Cat);
                Good(b,"day_off");Events(b);Good(b,"player_begin",cat:"cat-mugi");Reject(b,"resolve_visiting_cat","interact",Cat);Good(b,"player_finish");
                Visit(b,"interact");Check(b.View().visiting_cat.progress==1 && !b.View().visiting_cat.can_interact,"Daily action failed");Save(b,4);Reject(b,"resolve_visiting_cat","skip",Cat);Reject(b,"resolve_visiting_cat","accept",Cat);
                Good(b,"day_off");if(b.Simulation.IntakeWaiting)Reject(b,"resolve_visiting_cat","interact",Cat);Events(b);Visit(b,"interact");Good(b,"day_off");Events(b);Visit(b,"interact");
                Check(b.View().visiting_cat.status=="ready" && b.View().visiting_cat.can_accept,"Three interactions did not unlock admission");Save(b,5);Reject(b,"resolve_visiting_cat","interact",Cat);
                // Pure admission reasons probe exact source boundaries without saving forged finances.
                double funds=b.Simulation.State.funds;foreach(double amount in new[]{199.0,200.0,201.0}){b.Simulation.State.funds=amount;Check((b.Simulation.VisitingAdmissionReason()=="")==(amount>200),"Admission funds boundary differs");}b.Simulation.State.funds=funds;
                int oldCapacity=rules.housing.initial_capacity;rules.housing.initial_capacity=b.Simulation.State.cats.Count;Check(!b.View().visiting_cat.can_accept && b.View().visiting_cat.accept_reason.Contains("満員"),"Full housing did not block admission");rules.housing.initial_capacity=oldCapacity;
                Visit(b,"accept");var cat=b.Simulation.State.cats.Single(c=>c.id==Cat);Check(b.Simulation.State.funds==funds-200 && b.Simulation.State.recruitment_expenses==200,"Admission charged incorrect expense");
                Check(!cat.working && cat.health=="healthy" && cat.stamina==rules.max_stamina && cat.fatigue==0 && cat.stress==0 && b.Simulation.PlayerCat(Cat).affinity==0,"Joined cat initialization differs");Save(b,6);Reject(b,"resolve_visiting_cat","accept",Cat);Reject(b,"resolve_visiting_cat","skip",Cat);
                for(int variant=0;variant<9;variant++)
                {
                    string before=Capture(b);var bad=Clone(b);var v=bad.visiting_cat;
                    if(variant==0)v.presented_day++;else if(variant==1)v.status="visiting";else if(variant==2)v.actions.Add(new CafeVisitingAction {day=bad.day,choice="interact"});else if(variant==3)v.accepted_day=1;else if(variant==4)v.actions[0].choice="accept";else if(variant==5)bad.cats.RemoveAll(c=>c.id==Cat);else if(variant==6)bad.recruitment_expenses=0;else if(variant==7)bad.visiting_cat=null;else v.actions=null;
                    bool rejected=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected && before==Capture(b),"Corrupt save accepted or changed state");
                }
                Good(b,"player_begin",cat:Cat);Good(b,"player_step","direct");Save(b,7);Good(b,"player_finish");Check(b.Simulation.PlayerCat(Cat).total==1,"Joined cat relationship did not finish");
                b.Simulation.Execute("set_shifts",working:new[]{Cat});Good(b,"start_business");while(b.Simulation.State.phase!="closed"){Events(b);Good(b,"advance_business");}Events(b);Good(b,"next_day");Events(b);
                Check(b.Simulation.State.growth.Single(g=>g.cat_id==Cat).specialization!=null,"Joined cat did not grow through service");
                Good(b,"dispatch_general","neighborhood_visit",Cat);Good(b,"day_off");Events(b);Check(b.Simulation.State.dispatches.Any(e=>e.cat_id==Cat && e.status=="resolved"),"Joined cat dispatch failed");Save(b,8);
            }
            return checks;
        }
    }
}
