using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneDispatchIntroductionSmoke
    {
        private static int checks;private static bool acceptIntake;
        private static void Check(bool value,string message){checks++;if(!value)throw new Exception("Dispatch introduction: "+message);}
        private static string Command(LocalCafeBackend b,string kind,string choice=null,string cat=null,string source=null,int slot=1)=>JsonUtility.ToJson(new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,event_id=source,working_cats=b.Simulation.State.cats.Where(c=>c.activity=="cafe" && c.health=="healthy").Select(c=>c.id).ToArray(),save_slot=slot,save_id=slot.ToString()});
        private static CafeResponse Request(LocalCafeBackend b,string payload){CafeResponse r=null;var it=b.Request("/commands",payload,x=>r=x);while(it.MoveNext()){}return r;}
        private static void Good(LocalCafeBackend b,string kind,string choice=null,string cat=null,string source=null,int slot=1){var r=Request(b,Command(b,kind,choice,cat,source,slot));Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static string Capture(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();s.instance_id="";s.revision=0;return JsonUtility.ToJson(s);}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null,string cat=null,string source=null){string before=Capture(b);Check(Request(b,Command(b,kind,choice,cat,source)).result==UnityWebRequest.Result.ProtocolError,"Invalid "+kind+" accepted");Check(before==Capture(b),"Rejected operation mutated state");}
        private static void Resume(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Resume changed destination/encounter");}
        private static void Events(LocalCafeBackend b)
        {
            while(b.View().growth_choices.Length>0){var r=b.View().growth_choices[0];Good(b,r.kind,r.choices[0].choice,r.cat_id);}
            if(b.Simulation.ReservationWaiting)Good(b,"resolve_reservation","decline");
            if(b.Simulation.IntakeWaiting)Good(b,"resolve_intake",acceptIntake?"accept":"decline");
            if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction","decline",b.Simulation.Rules.regular_introduction.cat.id);
            if(b.Simulation.GoalWaiting)Good(b,"continue_goal");
        }
        public static int Run(string path,string destination,bool accept,bool closed)
        {
            checks=0;acceptIntake=closed;var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.starting_funds=20000;rules.store_events.probability=0;rules.max_probability=0;
            foreach(var u in rules.dispatch_unlocks)if(u.stage==0)u.popularity=100;
            rules.rules_id+="-introduction-"+destination+"-"+accept+"-"+closed;var b=new LocalCafeBackend(path,rules);
            if(File.Exists(Path.Combine(path,"slot-6.json")))foreach(int slot in new[]{2,3,4,5,6})
            {Good(b,"load_game",slot:slot);var e=b.Simulation.State.dispatches.Last();Check(e.destination==destination && (slot==2?e.introduction.status=="scheduled" && e.status=="travelling":slot==3?e.status=="waiting":slot==4?e.status=="resolved" && e.introduction.status==(closed?"scheduled":"waiting"):slot==5?e.introduction.status=="waiting":e.introduction.status==(accept?"accepted":"declined")),"Cross-process introduction checkpoint differs");}
            Good(b,"new_game","free");Good(b,"dispatch_general","neighborhood_visit","cat-sora");Good(b,"day_off");Good(b,"receive_general","dispatch-1-cat-sora","cat-sora");Events(b);
            Good(b,"dispatch_general",destination,"cat-mike");string id="dispatch-2-cat-mike";Resume(b,2);Reject(b,"resolve_dispatch_introduction","accept","dispatch-rescue-1",id);
            Good(b,"day_off");Good(b,"resolve_dispatch_choice",closed?"decline":"accept",source:id);Events(b);
            if(closed){Good(b,"set_shifts");Good(b,"start_business");while(b.Simulation.State.phase!="closed"){Events(b);Good(b,"advance_business");}}else Good(b,"day_off");
            Resume(b,3);Good(b,"receive_general",id,"cat-mike");Events(b);Resume(b,4);
            if(closed){Reject(b,"resolve_dispatch_introduction","decline","dispatch-rescue-1",id);Good(b,"next_day");Events(b);}
            Check(b.View().dispatch_introductions.Single().status=="waiting","Introduction not projected");Resume(b,5);
            Reject(b,"day_off");Reject(b,"set_shifts");Reject(b,"resolve_dispatch_introduction","invalid","dispatch-rescue-1",id);Reject(b,"resolve_dispatch_introduction","accept","wrong-cat",id);
            // Funds queries are restored before any save; finance validation uses the real state.
            double funds=b.Simulation.State.funds,cost=b.Simulation.State.dispatches.Last().introduction.cost;
            foreach(double value in new[]{cost-1,cost,cost+.001}){b.Simulation.State.funds=value;Check((b.Simulation.DispatchIntroductionAcceptReason(id)=="")== (value>cost && b.Simulation.State.cats.Count<rules.housing.initial_capacity),"Strict funds boundary differs");}b.Simulation.State.funds=funds;
            if(accept && b.Simulation.State.cats.Count==rules.housing.initial_capacity){Reject(b,"resolve_dispatch_introduction","accept","dispatch-rescue-1",id);Good(b,"purchase_housing");}
            string payload=Command(b,"resolve_dispatch_introduction",accept?"accept":"decline","dispatch-rescue-1",id);int count=b.Simulation.State.cats.Count;funds=b.Simulation.State.funds;
            var reply=Request(b,payload);Check(reply.result==UnityWebRequest.Result.Success,"Introduction response failed");string captured=Capture(b);Check(Request(b,payload).text==reply.text && captured==Capture(b),"Duplicate introduction applied twice");
            Check(b.Simulation.State.cats.Count==count+(accept?1:0) && b.Simulation.State.funds==funds-(accept?cost:0),"Wrong admission or payment");Reject(b,"resolve_dispatch_introduction",accept?"decline":"accept","dispatch-rescue-1",id);Resume(b,6);
            for(int field=0;field<7;field++)
            {
                var bad=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));bad.NormalizeJsonStrings();var v=bad.dispatches.Last().introduction;
                if(field==0)v.cost++;else if(field==1)v.cat_id="cat-mike";else if(field==2)v.name+="changed";else if(field==3)v.presented_day++;else if(field==4)v.resolved_day++;else if(field==5)v.status="scheduled";else bad.dispatches.Last().introduction=null;
                bool rejected=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected && captured==Capture(b),"Forged introduction accepted");
            }
            Good(b,"day_off");Events(b);Good(b,"dispatch_general",destination,"cat-mike");var next=b.Simulation.State.dispatches.Last();Check(destination=="cat_photo_studio"?next.introduction==null:next.introduction.cat_id=="dispatch-rescue-2" && next.introduction.name=="セナ（派遣紹介2）","Repeat introduction rule differs");return checks;
        }
    }
}
