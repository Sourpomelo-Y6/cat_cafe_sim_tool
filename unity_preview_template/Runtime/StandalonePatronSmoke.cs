using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandalonePatronSmoke
    {
        private static int checks;
        private static void Check(bool value,string message){checks++;if(!value)throw new Exception("Patron: "+message);}
        private static string Command(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1,string source=null)=>JsonUtility.ToJson(new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,event_id=source,save_slot=slot,save_id=slot.ToString(),working_cats=b.Simulation.State.cats.Where(c=>c.activity=="cafe").Select(c=>c.id).ToArray()});
        private static CafeResponse Request(LocalCafeBackend b,string command){CafeResponse response=null;var request=b.Request("/commands",command,r=>response=r);while(request.MoveNext()){}return response;}
        private static void Good(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1,string source=null){var r=Request(b,Command(b,kind,choice,cat,slot,source));Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static string Capture(LocalCafeBackend b){var state=Clone(b);state.instance_id="";state.revision=0;return JsonUtility.ToJson(state);}
        private static CafeSnapshot Clone(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();CafeDoublePrecision.Restore(s,CafeDoublePrecision.Capture(b.Simulation.State));return s;}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null,string cat=null){string before=Capture(b);Check(Request(b,Command(b,kind,choice,cat)).result==UnityWebRequest.Result.ProtocolError,"Accepted invalid "+kind);Check(before==Capture(b),"Rejected operation changed state");}
        private static void Resume(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Save/load changed progress");}
        private static void Events(LocalCafeBackend b,string specialization)
        {
            foreach(var c in b.Simulation.State.cats.Where(c=>c.activity=="missing" && c.missing_remaining==0 && !b.Simulation.DispatchMissing(b.Simulation.State,c.id)).ToArray())Good(b,"resolve_missing",cat:c.id);
            foreach(var e in b.View().customer_trust.events.Where(e=>e.status=="waiting"))Good(b,"resolve_customer_trust","recover",source:e.event_id);
            while(b.View().growth_choices.Length>0){var r=b.View().growth_choices[0];Good(b,r.kind,r.kind=="resolve_growth"?specialization:r.choices[0].choice,r.cat_id);}
            if(b.Simulation.IntakeWaiting)Good(b,"resolve_intake","decline");
            if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction","decline",b.Simulation.Rules.regular_introduction.cat.id);
        }
        public static int Run(string path,bool closed=false,string specialization="service")
        {
            checks=0;var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.starting_funds=20000;rules.store_events.probability=0;rules.growth.threshold=1;rules.max_probability=0;rules.rules_id+="-patron-"+closed+specialization;
            if(closed){rules.fatigue_per_service_tick=0;rules.stress_per_service_tick=0;}
            var b=new LocalCafeBackend(path,rules);
            if(File.Exists(Path.Combine(path,"slot-6.json")))foreach(int slot in new[]{2,3,4,5,6}){Good(b,"load_game",slot:slot);Check(b.Simulation.State.objective=="patron" && b.View().patron_goal!=null,"Cross-process patron state lost");Check(slot<5 || b.Simulation.State.patron.status=="cleared","Cross-process clear lost");Check(slot!=6 || b.Simulation.State.patron.continued,"Cross-process continued result lost");}
            if(specialization=="dispatch" && File.Exists(Path.Combine(path,"slot-5.json")))foreach(int slot in new[]{2,3,4,5}){Good(b,"load_game",slot:slot);Check(b.Simulation.State.patron.status=="active","Specialist restart changed progress");if(slot==5)Check(b.Simulation.State.dispatches.Last().patron_match.gain==0,"Specialist departure terms lost");}
            Good(b,"new_game","free");Reject(b,"dispatch_patron","patron_visit","cat-sora");Good(b,"new_game","patron");Check(b.View().patron_dispatch.options.Length==15 && b.View().general_dispatch.options.Length==45,"Dispatch catalogs overlap");Reject(b,"continue_patron");
            var cat=b.Simulation.State.cats.Single(c=>c.id=="cat-sora");cat.fatigue=40;Check(b.Simulation.DispatchReason(cat.id,"patron_visit")=="","Fatigue equality rejected");cat.fatigue=40.001;Check(b.Simulation.DispatchReason(cat.id,"patron_visit")!="","Fatigue limit missing");cat.fatigue=0;
            Action close=()=>{Events(b,specialization);if(b.Simulation.State.phase=="closed")Good(b,"next_day");Events(b,specialization);if(!closed){Good(b,"day_off");return;}Good(b,"set_shifts");Good(b,"start_business");while(b.Simulation.State.phase!="closed"){Events(b,specialization);Good(b,"advance_business");}};
            Good(b,"dispatch_patron","patron_visit","cat-sora");Check(b.View().patron_dispatch.events.Single().reward==220 && b.View().patron_dispatch.options.Single(o=>o.cat_id=="cat-sora" && o.choice=="patron_visit").satisfaction==30,"White feature terms changed");Resume(b,2);Reject(b,"receive_patron","dispatch-1-cat-sora","cat-sora");close();Resume(b,3);close();Resume(b,4);
            string first="dispatch-1-cat-sora";string payload=Command(b,"receive_patron",first,"cat-sora");var reply=Request(b,payload);Check(reply.result==UnityWebRequest.Result.Success,"Return failed");string before=Capture(b);Check(Request(b,payload).text==reply.text && before==Capture(b),"Duplicate return counted twice");Reject(b,"receive_patron",first,"cat-sora");Check(b.Simulation.State.patron.satisfaction==30,"Wrong initial satisfaction");Events(b,specialization);
            if(specialization=="dispatch")
            {
                if(b.Simulation.State.phase=="closed")Good(b,"next_day");Events(b,specialization);Good(b,"dispatch_patron","patron_strict_visit","cat-sora");Check(b.Simulation.State.dispatches.Last().patron_match.gain==0 && b.Simulation.State.dispatches.Last().reward==220.00000000000003,"Dispatch expertise or strict AND changed");Resume(b,5);return checks;
            }
            int guard=0;
            while(b.Simulation.State.patron.status=="active")
            {
                Check(guard++<30,"Patron progression did not complete");if(b.Simulation.State.phase=="closed")Good(b,"next_day");Events(b,specialization);var p=b.Simulation.State.patron;
                string destination=p.satisfaction<100?"patron_visit":p.members.Single(m=>m.id=="patron_strict_visit").value<100?"patron_strict_visit":"patron_moody_visit";int day=b.Simulation.State.day;
                Good(b,"dispatch_patron",destination,"cat-sora");if(destination!="patron_visit")Reject(b,"dispatch_patron",destination,"cat-mugi");close();close();Good(b,"receive_patron","dispatch-"+day+"-cat-sora","cat-sora");Events(b,specialization);
            }
            Check(b.View().required_action=="patron_result" && b.View().ending.can_continue && b.Simulation.State.clear_results.Single().value==300,"Clear projection missing");Resume(b,5);Reject(b,"day_off");Reject(b,"set_shifts");string record=JsonUtility.ToJson(b.Simulation.State.clear_results.Single());
            for(int field=0;field<8;field++)
            {
                var bad=Clone(b);if(field==0)bad.patron.satisfaction--;else if(field==1)bad.patron.members[0].value--;else if(field==2)bad.patron.resolved_day++;else if(field==3)bad.patron.status="active";else if(field==4)bad.dispatches.First(e=>e.patron_match!=null).patron_match.gain++;else if(field==5)bad.dispatches.First(e=>e.patron_match!=null).patron_match.condition_index++;else if(field==6)bad.clear_results[0].value--;else bad.dispatches.First(e=>e.patron_match!=null).patron_match=null;
                bool rejected=false;string captured=Capture(b);try{b.Simulation.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected && captured==Capture(b),"Invalid saved progress accepted: "+field);
            }
            Good(b,"continue_patron");Reject(b,"continue_patron");close();Events(b,specialization);Check(record==JsonUtility.ToJson(b.Simulation.State.clear_results.Single()),"Clear record changed after continuing");Resume(b,6);return checks;
        }
    }
}
