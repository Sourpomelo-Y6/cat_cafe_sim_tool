using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneDispatchDestinationsSmoke
    {
        private static int checks;
        private static void Check(bool value,string message){checks++;if(!value)throw new Exception("Destinations: "+message);}
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
            if(b.Simulation.IntakeWaiting)Good(b,"resolve_intake","decline");
            if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction","decline",b.Simulation.Rules.regular_introduction.cat.id);
            if(b.Simulation.GoalWaiting)Good(b,"continue_goal");
        }
        public static int Run(string path,string destination,bool second=false)
        {
            checks=0;var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
            rules.starting_funds=20000;rules.store_events.probability=0;rules.max_probability=0;
            foreach(var u in rules.dispatch_unlocks)if(u.stage==0)u.popularity=100;
            string cat=destination=="out_of_town_visit"?"cat-tama":destination=="quiet_reading_salon"?"cat-kohaku":"cat-mugi";
            var r=rules.dispatch_destinations.Single(r=>r.id==destination);var definition=rules.cats.Single(c=>c.id==cat);definition.trait_id=r.required_trait;
            if(destination=="cat_exercise_class"){definition.service_fatigue=.75;definition.rest_fatigue=.75;}
            if(destination=="cat_product_trial"){definition.service_stress=1.25;definition.service_fatigue=.75;}
            if(destination=="small_art_museum"){definition.service_stress=.75;definition.service_fatigue=.75;rules.goal_stages[0].target=101;rules.goal_stages[1].target=102;}
            rules.rules_id+="-destinations-"+destination+"-"+second;
            var b=new LocalCafeBackend(path,rules);
            if(File.Exists(Path.Combine(path,"slot-6.json")))
            {
                foreach(int slot in new[]{2,3,4,5,6})
                {Good(b,"load_game",slot:slot);var e=b.Simulation.State.dispatches.Last();Check(e.destination==destination && (slot==2?e.encounter.status=="scheduled":slot==3?e.encounter.status=="waiting":slot==4?e.encounter.status=="resolved" && e.status=="travelling":slot==5?e.status=="waiting":e.status=="resolved"),"Cross-process checkpoint differs");}
            }
            Good(b,"new_game",destination=="small_art_museum"?"popularity":"free");Reject(b,"dispatch_general",destination,cat);
            foreach(string unsupported in new[]{"mountain_lodge_visit"})Reject(b,"dispatch_general",unsupported,cat);
            for(int day=0;day<3;day++){string id="dispatch-"+b.Simulation.State.day+"-cat-sora";Good(b,"dispatch_general","neighborhood_visit","cat-sora");Good(b,"day_off");Good(b,"receive_general",id,"cat-sora");Events(b);}
            if(destination=="small_art_museum")for(int stage=0;stage<2;stage++){Good(b,"set_shifts");Good(b,"start_business");while(b.Simulation.State.phase!="closed"){Events(b);Good(b,"advance_business");}Events(b);Good(b,"advance_goal");Good(b,"next_day");Events(b);}
            Reject(b,"dispatch_general",destination,"cat-sora");double max=r.max_fatigue;var catState=b.Simulation.State.cats.Single(c=>c.id==cat);double beforeFatigue=catState.fatigue;catState.fatigue=max;Check(b.Simulation.DispatchReason(cat,destination)=="","Fatigue equality rejected");catState.fatigue=max+.001;Check(b.Simulation.DispatchReason(cat,destination)!="","Excess fatigue allowed");catState.fatigue=beforeFatigue;
            Good(b,"dispatch_general",destination,cat);string eventId="dispatch-"+b.Simulation.State.day+"-"+cat;Resume(b,2);
            Reject(b,"resolve_dispatch_choice","invalid",source:eventId);Good(b,"day_off");Check(b.View().required_action=="dispatch_choice" && b.View().dispatch_choices.Any(e=>e.event_id==eventId && e.choices.All(c=>c.can_select)),"Encounter not projected");Resume(b,3);
            Reject(b,"day_off");Reject(b,"next_day");Reject(b,"receive_general",eventId,cat);
            var eventRule=b.Simulation.DispatchEncounterDefinition(destination,b.Simulation.State.dispatches.Last().started_day);var option=eventRule.choices[second?1:0];double funds=b.Simulation.State.funds;
            string payload=Command(b,"resolve_dispatch_choice",option.id,source:eventId);var reply=Request(b,payload);Check(reply.result==UnityWebRequest.Result.Success,"Encounter response failed");string captured=Capture(b);Check(Request(b,payload).text==reply.text && captured==Capture(b),"Duplicate encounter applied twice");
            Check(b.Simulation.State.funds==funds,"Encounter paid before return");Reject(b,"resolve_dispatch_choice",eventRule.choices[second?0:1].id,source:eventId);Resume(b,4);
            for(int field=0;field<5;field++)
            {
                var bad=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));bad.NormalizeJsonStrings();var e=bad.dispatches.Last();
                if(field==0)e.encounter.choice="invalid";else if(field==1)e.encounter.fatigue_after++;else if(field==2)e.encounter.occurred_day++;else if(field==3)e.destination="unknown";else bad.dispatch_unlocks.RemoveAll(u=>u.id==destination);
                bool rejected=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected && captured==Capture(b),"Forged encounter/unlock accepted");
            }
            while(!b.Simulation.DispatchWaiting){Good(b,"day_off");Events(b);}Resume(b,5);var trip=b.Simulation.State.dispatches.Last();double reward=b.Simulation.DispatchReceivedReward(trip);funds=b.Simulation.State.funds;Good(b,"receive_general",eventId,cat);Check(b.Simulation.State.funds==funds+reward,"Wrong return reward");Reject(b,"receive_general",eventId,cat);Events(b);Resume(b,6);return checks;
        }
    }
}
