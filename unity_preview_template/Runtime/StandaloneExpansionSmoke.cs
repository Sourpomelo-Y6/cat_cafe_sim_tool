using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneExpansionSmoke
    {
        private static int checks;
        private static void Check(bool v,string m){checks++;if(!v)throw new Exception("Expansion: "+m);}
        private static CafeResponse Request(LocalCafeBackend b,string json){CafeResponse result=null;var request=b.Request("/commands",json,r=>result=r);while(request.MoveNext()){}return result;}
        private static string Command(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1)=>JsonUtility.ToJson(new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,working_cats=b.Simulation.State.cats.Where((c,i)=>c.health=="healthy" && c.activity=="cafe" && (i+b.Simulation.State.day)%3!=0).Select(c=>c.id).ToArray(),save_id=slot.ToString(),save_slot=slot});
        private static void Good(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1){var r=Request(b,Command(b,kind,choice,cat,slot));Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static string Capture(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();s.instance_id="";s.revision=0;return JsonUtility.ToJson(s);}
        private static void Reject(LocalCafeBackend b,string kind){string before=Capture(b);Check(Request(b,Command(b,kind)).result==UnityWebRequest.Result.ProtocolError,"Accepted invalid "+kind);Check(before==Capture(b),"Rejected operation changed state");}
        private static void Resume(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Save changed seats or goals");}
        private static PythonCafeState View(LocalCafeBackend b){CafeResponse r=null;var q=b.Request("/state",null,x=>r=x);while(q.MoveNext()){}return JsonUtility.FromJson<PythonCafeState>(r.text);}
        private static void Events(LocalCafeBackend b)
        {
            foreach(var c in b.Simulation.State.cats.Where(c=>c.activity=="missing" && c.missing_remaining==0).ToArray())Good(b,"resolve_missing",cat:c.id);
            foreach(var e in b.Simulation.State.trust_events.Where(e=>e.status=="waiting").ToArray())
            {
                var command=JsonUtility.FromJson<PythonShiftCommand>(Command(b,"resolve_customer_trust",choice:"recover"));command.event_id=e.id;
                Check(Request(b,JsonUtility.ToJson(command)).result==UnityWebRequest.Result.Success,"Trust recovery failed");
            }
            while(View(b).growth_choices.Length>0){var row=View(b).growth_choices[0];Good(b,row.kind,choice:row.kind=="resolve_growth"?"service":row.choices[0].choice,cat:row.cat_id);}
            if(b.Simulation.GoalWaiting){Check(View(b).goal_result.pending,"Pending result missing");Resume(b,2);Reject(b,"expand_seats");Reject(b,"next_day");Good(b,"continue_goal");}
            if(b.Simulation.IntakeWaiting)Good(b,"resolve_intake",choice:"decline");
            if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction",choice:"decline",cat:b.Simulation.Rules.regular_introduction.cat.id);
            if(b.Simulation.GoalResponseReason("advance_goal")=="")Good(b,"advance_goal");
        }
        public static int Run(string path)
        {
            checks=0;var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.starting_funds=20000;rules.store_events.probability=0;rules.goal_stages[0].target=110;rules.goal_stages[1].target=120;rules.goal_stages[2].target=130;rules.rules_id+="-expansion-fixture";
            var b=new LocalCafeBackend(path,rules);
            if(File.Exists(Path.Combine(path,"slot-4.json"))){Good(b,"load_game",slot:4);Check(b.Simulation.State.seat_count==8 && b.Simulation.State.goal.Count==3,"Cross-process restart lost expansion");}
            Good(b,"new_game",choice:"free");Good(b,"expand_seats");Reject(b,"expand_seats");Resume(b,3);
            Good(b,"new_game",choice:"popularity");Reject(b,"advance_goal");Good(b,"expand_seats");Reject(b,"expand_seats");
            bool[] arrivals=new bool[5];
            for(int day=1;day<=12;day++)
            {
                Events(b);while(b.Simulation.ExpansionReason()=="")Good(b,"expand_seats");Resume(b,3);
                Check(View(b).seats.Length==b.Simulation.State.seat_count,"Seat projection is fixed");Good(b,"set_shifts");
                if(!b.Simulation.State.cats.Any(c=>c.working)){Good(b,"day_off");if(b.Simulation.State.phase=="closed"){Events(b);Good(b,"next_day");}continue;}
                Good(b,"start_business");Reject(b,"expand_seats");Resume(b,1);
                while(b.Simulation.State.phase!="closed"){Events(b);Good(b,"advance_business");}
                for(int i=0;i<5;i++)arrivals[i]|=b.Simulation.State.visits.Any(v=>v.id==(i==0?"expanded-guest":"expanded-guest-"+(i+1)));
                Check(View(b).expansion.operating_cost==rules.base_cost+rules.per_seat_cost*b.Simulation.State.seat_count,"Seat operating cost differs");Resume(b,1);Events(b);Good(b,"next_day");
            }
            Check(b.Simulation.State.seat_count==8 && arrivals.All(v=>v),"Eight seats or additional arrivals missing");Reject(b,"expand_seats");Resume(b,4);
            string final=Capture(b);var fresh=new LocalCafeBackend(path,rules);Good(fresh,"load_game",slot:4);Check(final==Capture(fresh),"Fresh backend lost seat history");
            for(int kind=0;kind<5;kind++){var bad=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));bad.NormalizeJsonStrings();if(kind==0)bad.seat_count--;else if(kind==1)bad.expansion[0].cost++;else if(kind==2)bad.expansion[1].day=1;else if(kind==3)bad.goal[0].resolved_day++;else bad.history[0].seat_count++;bool failed=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){failed=true;}Check(failed && final==Capture(b),"Forged expansion accepted");}
            return checks;
        }
    }
}
