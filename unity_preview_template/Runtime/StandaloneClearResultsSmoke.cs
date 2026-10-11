using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneClearResultsSmoke
    {
        private static int checks;
        private static void Check(bool value,string message){checks++;if(!value)throw new Exception("Clear result: "+message);}
        private static string Command(LocalCafeBackend b,string kind,string choice=null,string seat=null,string cat=null,int slot=1) => JsonUtility.ToJson(new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,seat_id=seat,cat_id=cat,working_cats=b.Simulation.State.cats.Where(c=>c.health=="healthy" && c.activity=="cafe").Select(c=>c.id).ToArray(),save_slot=slot,save_id=slot.ToString()});
        private static CafeResponse Request(LocalCafeBackend b,string json){CafeResponse r=null;var request=b.Request("/commands",json,x=>r=x);while(request.MoveNext()){}return r;}
        private static void Good(LocalCafeBackend b,string kind,string choice=null,string seat=null,string cat=null,int slot=1){var r=Request(b,Command(b,kind,choice,seat,cat,slot));Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static string Capture(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();s.instance_id="";s.revision=0;return JsonUtility.ToJson(s);}
        private static void Resume(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Resume changed service/equipment");}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null,string seat=null){string before=Capture(b);Check(Request(b,Command(b,kind,choice,seat)).result==UnityWebRequest.Result.ProtocolError,"Invalid command accepted");Check(before==Capture(b),"Rejection changed state");}
        private static void Events(LocalCafeBackend b)
        {
            while(b.View().growth_choices.Length>0){var row=b.View().growth_choices[0];Good(b,row.kind,choice:row.kind=="resolve_growth"?"service":row.choices[0].choice,cat:row.cat_id);}
            if(b.Simulation.ReservationWaiting)Good(b,"resolve_reservation",choice:"decline");
            if(b.Simulation.GoalWaiting)Good(b,"continue_goal");
            if(b.Simulation.IntakeWaiting)Good(b,"resolve_intake",choice:"decline");
            if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction",choice:"decline",cat:b.Simulation.Rules.regular_introduction.cat.id);
            if(b.Simulation.GoalResponseReason("advance_goal")=="")Good(b,"advance_goal");
        }
        public static int Run(string path,string variant)
        {
            checks=0;var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
            rules.starting_funds=20000;rules.store_events.probability=0;rules.stress_per_service_tick=0;rules.fatigue_per_service_tick=0;rules.max_probability=0;
            rules.goal_stages[0].target=110;rules.goal_stages[1].target=120;rules.goal_stages[2].target=130;
            if(variant=="expired"){rules.goal_stages[0].days=1;rules.goal_stages[0].target=600;rules.goal_stages[1].target=625;rules.goal_stages[2].target=650;}
            if(variant=="funds")rules.starting_funds=1;
            if(variant=="popularity"){rules.starting_popularity=1;rules.runaway_threshold=1;rules.return_stress=0;rules.popularity_loss=100;rules.stress_per_service_tick=3;}
            rules.rules_id+="-clear-fixture-"+variant;
            var b=new LocalCafeBackend(path,rules);
            if(File.Exists(Path.Combine(path,"slot-4.json")))
            {
                Good(b,"load_game",slot:4);Check(variant=="cleared"?b.View().clear_results.rows.Length==1:b.View().clear_results.rows.Length==0,"Restart lost outcome");
                if(variant=="cleared"){Good(b,"load_game",slot:2);Check(b.View().ending?.status=="cleared" && b.Simulation.GoalWaiting,"Restart lost unconfirmed ending");Good(b,"load_game",slot:3);Check(b.View().ending==null && b.Simulation.State.goal.Last().continued,"Restart lost continuation");}
                else Check(variant=="expired"?b.Simulation.State.goal[0].continued:b.View().ending?.status=="game_over","Restart lost terminal result");
            }
            Good(b,"new_game",choice:"popularity");Check(b.View().clear_results.rows.Length==0 && b.View().ending==null,"New game retained ending");
            string frozen=null;bool seen=false;
            for(int day=1;day<=(variant=="cleared"?12:2);day++)
            {
                Events(b);
                if(variant=="funds")Good(b,"day_off");
                else{Good(b,"set_shifts");Good(b,"start_business");while(b.Simulation.State.phase!="closed"){Events(b);Good(b,"advance_business");}}
                var s=b.Simulation.State;
                if(s.game_over!=null)
                {
                    Check(s.game_over==variant && b.View().ending.status=="game_over" && !b.View().ending.can_continue,"Wrong terminal reason");Check(s.clear_results.Count==0,"Failure created a clear record");Resume(b,4);
                    Reject(b,"next_day");Reject(b,"continue_goal");Reject(b,"start_business");Reject(b,"advance_goal");
                    var bad=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(s));bad.NormalizeJsonStrings();bad.game_over=variant=="funds"?"popularity":"funds";bool rejected=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected,"Forged terminal reason accepted");seen=true;break;
                }
                if(variant=="expired" && s.goal.Last().status=="expired")
                {
                    Check(b.View().ending?.status=="expired" && s.clear_results.Count==0,"Deadline created a clear record");Reject(b,"next_day");Resume(b,2);Good(b,"continue_goal");Check(b.View().ending==null,"Expired ending did not close");seen=true;
                }
                if(s.clear_results.Count>0)
                {
                    var r=s.clear_results.Single();string record=JsonUtility.ToJson(r);
                    if(frozen==null)
                    {
                        Check(b.View().ending?.status=="cleared" && b.View().clear_results.rows.Length==1,"Final ending not projected");
                        Check(r.funds==s.funds && r.popularity==s.popularity && r.cats==s.cats.Count && r.revenue==s.history.Sum(h=>h.revenue)+s.revenue,"Capture timing differs");
                        Reject(b,"next_day");Resume(b,2);string command=Command(b,"continue_goal");var reply=Request(b,command);Check(reply.result==UnityWebRequest.Result.Success,"Continue failed");string after=Capture(b);Check(Request(b,command).text==reply.text && after==Capture(b),"Duplicate continuation mutated state");Reject(b,"continue_goal");Resume(b,3);frozen=record;seen=true;
                    }
                    Check(frozen==record,"Continued service changed fixed result");
                    string before=Capture(b);b.View();b.View();Check(before==Capture(b),"Viewing clear record mutated state");
                }
                Resume(b,1);Events(b);Good(b,"next_day");
                if(variant=="expired"){Resume(b,4);break;}
            }
            Check(seen,"Outcome fixture not reached");if(variant=="cleared")Resume(b,4);
            if(variant=="cleared")
            {
                string before=Capture(b);
                for(int i=0;i<8;i++)
                {
                    var bad=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));bad.NormalizeJsonStrings();var r=bad.clear_results[0];
                    if(i==0)bad.clear_results.Clear();else if(i==1)bad.clear_results.Add(r);else if(i==2)r.day++;else if(i==3)r.revenue++;else if(i==4)r.cats++;else if(i==5)r.funds=0;else if(i==6)r.target++;else r.mode="bond";
                    bool rejected=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected && before==Capture(b),"Forged clear result accepted");
                }
            }
            Good(b,"new_game",choice:"free");Check(b.View().clear_results.rows.Length==0 && b.View().ending==null,"Free game retained previous result");
            return checks;
        }
    }
}
