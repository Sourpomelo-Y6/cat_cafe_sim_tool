using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneAdoptionSmoke
    {
        private static int checks;
        private static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("Adoption: "+message);}
        private static CafeResponse Command(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1,string eventId=null)
        {
            CafeResponse result=null;var cmd=new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,save_slot=slot,save_id=slot.ToString(),event_id=eventId,working_cats=new string[0]};
            var it=b.Request("/commands",JsonUtility.ToJson(cmd),r=>result=r);while(it.MoveNext()){}return result;
        }
        private static void Good(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1,string eventId=null){var r=Command(b,kind,choice,cat,slot,eventId);Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static CafeSnapshot Clone(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();CafeDoublePrecision.Restore(s,CafeDoublePrecision.Capture(b.Simulation.State));return s;}
        private static string Capture(LocalCafeBackend b){var s=Clone(b);s.revision=0;s.instance_id="";return JsonUtility.ToJson(s);}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null,string cat=null,string eventId=null){string before=Capture(b);Check(Command(b,kind,choice,cat,eventId:eventId).result==UnityWebRequest.Result.ProtocolError,"Invalid command accepted: "+kind);Check(before==Capture(b),"Rejected command changed state");}
        private static void Save(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Save/resume changed state");}
        private static void Events(LocalCafeBackend b)
        {
            foreach(var e in b.Simulation.State.dispatches.Where(e=>e.status=="waiting").ToArray())Good(b,"receive_general",e.id,e.cat_id);
            while(b.View().growth_choices.Length>0){var r=b.View().growth_choices[0];Good(b,r.kind,r.choices[0].choice,r.cat_id);}
            if(b.Simulation.IntakeWaiting)Good(b,"resolve_intake","decline");if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction","decline");if(b.Simulation.ReservationWaiting)Good(b,"resolve_reservation","decline");
            foreach(var e in b.Simulation.State.trust_events.Where(e=>e.status=="waiting").ToArray())throw new Exception("Unexpected trust event in isolated fixture");
        }
        public static void Verify(){string root=Environment.GetEnvironmentVariable("CATCAFE_ADOPTION_SMOKE")??"adoption-smoke";Directory.CreateDirectory(root);int total=Run(root);File.WriteAllText(Path.Combine(root,"result.json"),"{\"passed\":true,\"checks\":"+total+"}");}
        public static int Run(string root)
        {
            checks=0;
            foreach(string mode in new[]{"free","popularity","bond","patron"})foreach(string answer in new[]{"accept","decline"})
            {
                string path=Path.Combine(root,mode+"-"+answer);var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.starting_funds=20000;rules.store_events.probability=0;rules.rules_id+="-adoption";rules.growth.threshold=1000;rules.exchange.ticks=1;rules.exchange.affinity_enthusiastic=rules.exchange.affinity_favorable=80;rules.opening_ticks=2;rules.arrival_ticks=new[]{0,0};rules.patterns=Enumerable.Range(0,7).Select(i=>new WeekPattern {days=Enumerable.Range(0,7).ToArray()}).ToArray();rules.max_probability=rules.stress_per_service_tick=rules.fatigue_per_service_tick=0;
                var b=new LocalCafeBackend(path,rules);
                if(File.Exists(Path.Combine(path,"slot-8.json")))foreach(int slot in Enumerable.Range(2,7))
                {
                    Good(b,"load_game",slot:slot);Check(b.Simulation.State.objective==mode,"Restart changed mode");Check(slot<4?b.View().adoption.events.Length==0:b.View().adoption.events.Length==2,"Restart lost offers");
                    if(slot==4)Check(b.Simulation.AdoptionWaiting,"Restart lost pending offers");
                    if(slot==5)Check(b.View().adoption.events.Count(e=>e.status=="waiting")==1,"Restart lost independent pending answer");
                    if(slot>=6)Check(!b.Simulation.AdoptionWaiting && b.Simulation.State.cats.Count(c=>c.activity=="adopted")== (answer=="accept"?1:0),"Restart lost adopted state");
                }
                b.Simulation.State.seed=42;Good(b,"new_game",mode);Check(!b.View().adoption.enabled,"Adoption should default OFF");Save(b,2);Reject(b,"configure_adoption","invalid");Reject(b,"resolve_adoption","accept","cat-mugi",eventId:"unknown");
                Good(b,"configure_adoption","on");Save(b,3);Good(b,"start_business");Good(b,"advance_business");Check(b.Simulation.State.phase=="closed" && b.View().adoption.events.Length==2,"Simultaneous closing offers did not occur");Save(b,4);
                Reject(b,"configure_adoption","off");Reject(b,"next_day");Reject(b,"day_off");Reject(b,"player_begin",cat:"cat-mugi");
                var offer=b.View().adoption.events[0];double funds=b.Simulation.State.funds,stamina=b.Simulation.State.cats.Single(c=>c.id==offer.cat_id).stamina;string record=string.Join("|",b.Simulation.State.relationships.Select(r=>r.cat_id+"/"+r.customer_id+"/"+r.affinity.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
                Reject(b,"resolve_adoption","accept","wrong",eventId:offer.event_id);Reject(b,"resolve_adoption","invalid",offer.cat_id,eventId:offer.event_id);Good(b,"resolve_adoption",answer,offer.cat_id,eventId:offer.event_id);Check(b.Simulation.State.funds==funds && string.Join("|",b.Simulation.State.relationships.Select(r=>r.cat_id+"/"+r.customer_id+"/"+r.affinity.ToString("R",System.Globalization.CultureInfo.InvariantCulture)))==record,"Adoption changed money or relations");Save(b,5);Reject(b,"next_day");Reject(b,"resolve_adoption",answer,offer.cat_id,eventId:offer.event_id);
                var other=b.View().adoption.events[1];Good(b,"resolve_adoption","decline",other.cat_id,eventId:other.event_id);Check(!b.Simulation.AdoptionWaiting,"Second offer was not independent");Save(b,6);
                for(int variant=0;variant<8;variant++)
                {
                    string before=Capture(b);var bad=Clone(b);var a=bad.adoption;var e=a.events[0];if(variant==0)e.threshold++;else if(variant==1)e.source="invalid";else if(variant==2)e.tick++;else if(variant==3)e.guest_affinity--;else if(variant==4)e.player_affinity++;else if(variant==5)a.events.Add(e);else if(variant==6)e.resolved_day++;else a.events=null;
                    bool rejected=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected && before==Capture(b),"Corrupt adoption save accepted or changed state");
                }
                if(b.Simulation.GoalWaiting)Good(b,"advance_goal");Good(b,"next_day");Events(b);Good(b,"configure_adoption","off");Check(b.View().adoption.events.Length==2,"OFF erased history");Save(b,7);
                if(answer=="accept")
                {
                    var cat=b.Simulation.State.cats.Single(c=>c.id==offer.cat_id);Check(cat.activity=="adopted" && !cat.working && cat.stamina==stamina && !b.View().cats.Single(c=>c.cat_id==cat.id).can_player_start,"Adopted cat changed or became available");
                    Reject(b,"player_begin",cat:cat.id);Reject(b,"dispatch_general","neighborhood_visit",cat.id);Check(b.View().housing.count==4 && b.View().housing.free==2,"Adoption did not release housing");
                }
                Good(b,"purchase_cat",cat:"shop-1");if(answer=="accept")Good(b,"purchase_cat",cat:"shop-2");Check(b.Simulation.ActiveCatCount(b.Simulation.State)==6 && b.Simulation.State.cats.Count==(answer=="accept"?7:6),"Physical records and current housing differ");Save(b,8);
            }
            return checks;
        }
    }
}
