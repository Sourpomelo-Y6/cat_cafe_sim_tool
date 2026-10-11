using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneObjectiveStartSmoke
    {
        private static int checks;
        private static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("Objective start: "+message);}
        private static CafeResponse Command(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1)
        {
            CafeResponse result=null;var command=new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,save_slot=slot,save_id=slot.ToString()};var it=b.Request("/commands",JsonUtility.ToJson(command),r=>result=r);while(it.MoveNext()){}return result;
        }
        private static void Good(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1){var r=Command(b,kind,choice,cat,slot);Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static string Capture(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();CafeDoublePrecision.Restore(s,CafeDoublePrecision.Capture(b.Simulation.State));s.revision=0;s.instance_id="";return JsonUtility.ToJson(s);}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null){string before=Capture(b);Check(Command(b,kind,choice).result==UnityWebRequest.Result.ProtocolError,"Invalid command accepted");Check(before==Capture(b),"Rejected command changed state");}
        private static void Save(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Save/resume changed state");}
        private static void Events(LocalCafeBackend b)
        {
            foreach(var e in b.Simulation.State.dispatches.Where(e=>e.status=="waiting").ToArray())Good(b,b.Simulation.IsPatronDestination(e.destination)?"receive_patron":"receive_general",e.id,e.cat_id);
            while(b.View().growth_choices.Length>0){var r=b.View().growth_choices[0];Good(b,r.kind,r.choices[0].choice,r.cat_id);}
            if(b.Simulation.IntakeWaiting)Good(b,"resolve_intake","decline");if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction","decline");if(b.Simulation.ReservationWaiting)Good(b,"resolve_reservation","decline");
            foreach(var e in b.Simulation.State.trust_events.Where(e=>e.status=="waiting").ToArray())throw new Exception("Unexpected trust event in isolated fixture");
        }
        public static void Verify(){string root=Environment.GetEnvironmentVariable("CATCAFE_OBJECTIVE_START_SMOKE")??"objective-start-smoke";Directory.CreateDirectory(root);int total=Run(root);File.WriteAllText(Path.Combine(root,"result.json"),"{\"passed\":true,\"checks\":"+total+"}");}
        public static int Run(string root)
        {
            checks=0;
            foreach(string mode in new[]{"free","popularity","bond","patron"})
            {
                string path=Path.Combine(root,mode);var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.starting_funds=20000;rules.store_events.probability=0;rules.rules_id+="-objective-start";
                rules.bond.target=1;rules.bond.affinity=.5;rules.patron.target=1;foreach(var member in rules.patron.members)member.target=1;rules.growth.threshold=1;
                for(int i=0;i<3;i++){rules.goal_stages[i].target=101+i;rules.goal_stages[i].days=10;}rules.max_probability=rules.stress_per_service_tick=rules.fatigue_per_service_tick=0;
                var b=new LocalCafeBackend(path,rules);
                if(File.Exists(Path.Combine(path,"slot-7.json")))foreach(int slot in Enumerable.Range(2,6)){Good(b,"load_game",slot:slot);Check(b.Simulation.State.objective==mode,"Restart changed starting mode");Check(slot<3 || b.Simulation.State.bond_goal!=null,"Restart lost introduced bond goal");Check(slot<4 || b.Simulation.State.patron!=null,"Restart lost introduced patron goal");Check(slot!=7 || b.Simulation.State.clear_results.Count==3 && b.Simulation.State.patron.continued,"Restart lost independent clear records");}
                Good(b,"new_game",mode);Good(b,"player_begin",cat:"cat-mugi");Reject(b,"start_objective","bond");Reject(b,"start_objective","patron");Good(b,"player_finish");
                Good(b,"dispatch_general","neighborhood_visit","cat-sora");Good(b,"day_off");Events(b);Good(b,"day_off");Events(b);Save(b,2);
                Good(b,"player_begin",cat:"cat-mugi");Good(b,"player_step","direct");Good(b,"player_finish");if(b.Simulation.BondWaiting)Good(b,"continue_bond_goal");
                double funds=b.Simulation.State.funds,affinity=b.Simulation.PlayerCat("cat-mugi").affinity;
                if(b.Simulation.State.bond_goal==null){Good(b,"start_objective","bond");Check(b.Simulation.BondWaiting && b.Simulation.State.bond_goal.started_day==3,"Existing affinity did not clear on introduction");Reject(b,"start_objective","patron");Save(b,3);Good(b,"continue_bond_goal");}else Save(b,3);
                Check(b.Simulation.PlayerCat("cat-mugi").affinity==affinity && b.Simulation.State.funds==funds,"Introduction reset affinity or charged funds");
                if(b.Simulation.State.patron==null)Good(b,"start_objective","patron");Check(b.Simulation.State.patron.satisfaction==0 && b.Simulation.State.patron.members.All(row=>row.value==0),"Normal dispatch retroactively added satisfaction");Save(b,4);Reject(b,"start_objective","bond");Reject(b,"start_objective","patron");
                if(b.Simulation.State.goal.Count==0)Good(b,"start_objective","popularity");Check(b.Simulation.State.objective==mode,"Introduction changed original mode");
                for(int stage=0;stage<3;stage++)
                {
                    Events(b);Good(b,"start_business");if(stage==0){Good(b,"advance_business");Save(b,5);Reject(b,"start_objective","bond");Reject(b,"start_objective","patron");}
                    while(b.Simulation.State.phase!="closed"){Events(b);Good(b,"advance_business");}Events(b);Check(b.Simulation.State.goal.Last().status=="cleared","Popularity stage did not clear");Good(b,stage<2?"advance_goal":"continue_goal");Good(b,"next_day");Events(b);
                }
                foreach(string destination in new[]{"patron_visit","patron_moody_visit","patron_strict_visit"}){Good(b,"dispatch_patron",destination,"cat-sora");Good(b,"day_off");Good(b,"day_off");Events(b);}
                Check(b.Simulation.State.clear_results.Count==3 && b.View().clear_results.rows.Length==3 && b.Simulation.PatronWaiting,"Three independent records not captured");Save(b,6);Reject(b,"day_off");Good(b,"continue_patron");Save(b,7);
                foreach(int mutation in Enumerable.Range(0,4))
                {
                    string before=Capture(b);var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();CafeDoublePrecision.Restore(s,CafeDoublePrecision.Capture(b.Simulation.State));if(mutation==0)s.bond_goal.started_day=0;else if(mutation==1)s.patron.started_day=s.day+1;else if(mutation==2)s.patron.satisfaction++;else s.clear_results.Add(s.clear_results[0]);bool rejected=false;try{b.Simulation.Restore(s);}catch(ArgumentException){rejected=true;}Check(rejected && before==Capture(b),"Corrupt objective save accepted");
                }
            }
            return checks;
        }
    }
}
