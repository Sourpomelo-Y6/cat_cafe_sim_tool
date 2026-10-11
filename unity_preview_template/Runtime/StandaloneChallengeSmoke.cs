using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneChallengeSmoke
    {
        private static int checks;
        private static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("Popularity challenge: "+message);}
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
            while(b.View().growth_choices.Length>0){var r=b.View().growth_choices[0];Good(b,r.kind,r.choices[0].choice,r.cat_id);}
            if(b.Simulation.IntakeWaiting)Good(b,"resolve_intake","decline");if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction","decline");if(b.Simulation.ReservationWaiting)Good(b,"resolve_reservation","decline");
            foreach(var e in b.Simulation.State.trust_events.Where(e=>e.status=="waiting").ToArray())throw new Exception("Unexpected trust event in isolated fixture");
        }
        public static void Verify(){string root=Environment.GetEnvironmentVariable("CATCAFE_CHALLENGE_SMOKE")??"challenge-smoke";Directory.CreateDirectory(root);int total=Run(root);File.WriteAllText(Path.Combine(root,"result.json"),"{\"passed\":true,\"checks\":"+total+"}");}
        public static int Run(string root)
        {
            checks=0;
            foreach(string mode in new[]{"free","bond","patron"})foreach(bool clear in new[]{false,true})
            {
                string path=Path.Combine(root,mode+"-"+clear);var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.starting_funds=20000;rules.store_events.probability=0;rules.rules_id+="-challenge-"+clear;
                if(clear){for(int i=0;i<3;i++){rules.goal_stages[i].target=101+i;rules.goal_stages[i].days=10;}rules.max_probability=rules.stress_per_service_tick=rules.fatigue_per_service_tick=0;if(mode=="bond"){rules.bond.target=1;rules.bond.affinity=.5;}}
                var b=new LocalCafeBackend(path,rules);int last=clear?6:5;
                if(File.Exists(Path.Combine(path,"slot-"+last+".json")))foreach(int slot in Enumerable.Range(2,last-1)){Good(b,"load_game",slot:slot);Check(b.Simulation.State.objective==mode,"Restart changed original mode");Check(slot==2 || b.Simulation.State.challenge_started_day==3,"Restart lost challenge marker");}
                Good(b,"new_game",mode);Good(b,"player_begin",cat:"cat-mugi");Reject(b,"start_objective","popularity");Good(b,"player_finish");Good(b,"day_off");Good(b,"day_off");Save(b,2);
                double funds=b.Simulation.State.funds;Good(b,"start_objective","popularity");Check(b.Simulation.State.objective==mode && b.Simulation.State.funds==funds && b.Simulation.State.goal.Single().started_day==3,"Start changed funds, mode or clock");Save(b,3);Reject(b,"start_objective","popularity");
                if(!clear)
                {
                    for(int day=0;day<rules.goal_stages[0].days;day++){Events(b);Good(b,"day_off");}
                    Check(b.Simulation.State.goal[0].status=="expired" && b.Simulation.State.goal[0].resolved_day==22,"Deadline did not start on challenge day");Save(b,4);Reject(b,"day_off");Good(b,"continue_goal");Save(b,5);Reject(b,"start_objective","popularity");
                }
                else
                {
                    if(mode=="bond"){Good(b,"player_begin",cat:"cat-mugi");Good(b,"player_step","direct");Good(b,"player_finish");Reject(b,"start_objective","popularity");Good(b,"continue_bond_goal");}
                    for(int stage=0;stage<3;stage++)
                    {
                        Events(b);Good(b,"start_business");if(stage==0){Good(b,"advance_business");Save(b,4);Reject(b,"start_objective","popularity");}
                        while(b.Simulation.State.phase!="closed"){Events(b);Good(b,"advance_business");}
                        Check(b.Simulation.State.goal.Last().status=="cleared","Stage did not clear");Good(b,stage<2?"advance_goal":"continue_goal");Good(b,"next_day");Events(b);
                    }
                    Check(!Enumerable.Range(b.Simulation.State.day,14).Any(day=>b.Simulation.CustomerSchedule(day).Any(row=>row.customer_id=="advanced-longhair")),"Mid-game challenge introduced longhair specialist");Save(b,5);if(mode=="bond"){Good(b,"player_begin",cat:"cat-mugi");Good(b,"player_step","direct");Good(b,"player_finish");Check(b.Simulation.State.clear_results.Count==2,"Independent records lost");Reject(b,"advance_goal");Check(b.View().goal_result.details.Contains(b.View().clear_results.rows.Single(row=>row.mode=="popularity").details),"Goal result displayed another goal record");}Save(b,6);
                }
                foreach(int corruption in Enumerable.Range(0,3))
                {
                    string before=Capture(b);var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();CafeDoublePrecision.Restore(s,CafeDoublePrecision.Capture(b.Simulation.State));if(corruption==0)s.challenge_started_day=0;else if(corruption==1)s.goal[0].started_day++;else s.clear_results.Add(new CafeClearRecord());bool rejected=false;try{b.Simulation.Restore(s);}catch(ArgumentException){rejected=true;}Check(rejected && before==Capture(b),"Corrupt challenge save accepted");
                }
            }
            return checks;
        }
    }
}
