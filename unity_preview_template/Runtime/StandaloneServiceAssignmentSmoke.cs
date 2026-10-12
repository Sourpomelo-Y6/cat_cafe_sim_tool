using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneServiceAssignmentSmoke
    {
        private static int checks;
        private static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("Service assignment: "+message);}
        private static CafeResponse Raw(LocalCafeBackend b,PythonShiftCommand cmd){CafeResponse reply=null;var it=b.Request("/commands",JsonUtility.ToJson(cmd),r=>reply=r);while(it.MoveNext()){}return reply;}
        private static PythonShiftCommand Make(LocalCafeBackend b,string kind,string choice=null,string cat=null,string customer=null,string seat=null,int slot=1,string[] working=null) => new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,customer_id=customer,seat_id=seat,working_cats=working??new string[0],save_slot=slot,save_id=slot.ToString()};
        private static void Good(LocalCafeBackend b,string kind,string choice=null,string cat=null,string customer=null,string seat=null,int slot=1){var r=Raw(b,Make(b,kind,choice,cat,customer,seat,slot));Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static string Capture(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();CafeDoublePrecision.Restore(s,CafeDoublePrecision.Capture(b.Simulation.State));s.instance_id="";s.revision=0;return JsonUtility.ToJson(s);}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null,string cat=null,string customer=null,string seat=null,string[] working=null){string before=Capture(b);Check(Raw(b,Make(b,kind,choice,cat,customer,seat,working:working)).result==UnityWebRequest.Result.ProtocolError,"Invalid command accepted: "+kind);Check(before==Capture(b),"Rejected command changed state");}
        private static void Save(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Save/resume changed mode, queue or services");}
        public static void Verify(){string root=Environment.GetEnvironmentVariable("CATCAFE_ASSIGNMENT_SMOKE")??"assignment-smoke";Directory.CreateDirectory(root);int total=Run(root);File.WriteAllText(Path.Combine(root,"result.json"),"{\"passed\":true,\"checks\":"+total+"}");}
        public static int Run(string root)
        {
            checks=0;
            foreach(string mode in new[]{"free","popularity","bond","patron"})
            {
                string path=Path.Combine(root,mode);var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.rules_id+="-assignment";rules.starting_funds=20000;rules.store_events.probability=0;rules.growth.threshold=1000;rules.max_probability=rules.stress_per_service_tick=0;rules.arrival_ticks=new int[3];rules.patterns=Enumerable.Range(0,7).Select(i=>new WeekPattern {days=Enumerable.Range(0,7).ToArray()}).ToArray();rules.opening_ticks=6;rules.exchange.ticks=2;
                var b=new LocalCafeBackend(path,rules);
                if(File.Exists(Path.Combine(path,"slot-10.json")))foreach(int slot in Enumerable.Range(2,9))
                {
                    Good(b,"load_game",slot:slot);Check(b.Simulation.State.objective==mode,"Restart changed mode");Check(b.Simulation.State.auto_assignment==(slot<3 || slot>=7),"Restart lost assignment setting");
                    if(slot==4 || slot==5)Check(b.View().service_assignment.waiting,"Restart lost manual waiting");
                    if(slot==5)Check(b.Simulation.State.exchanges.Count==1 && b.Simulation.State.tick==1,"Restart advanced the initial manual service");
                    if(slot==6 || slot==7 || slot==8)Check(b.Simulation.State.exchanges.Count==2,"Restart lost parallel services");
                    if(slot==10)Check(b.Simulation.State.day==2 && b.Simulation.State.phase=="preparation","Restart lost next day");
                }
                Good(b,"new_game",mode);Check(b.View().service_assignment.auto_assign,"New game must default automatic");Save(b,2);Reject(b,"assign_service",cat:"cat-mugi",customer:"guest-1",seat:"seat-1");Reject(b,"set_auto_assignment","invalid");Reject(b,"set_auto_assignment","manual",working:new[]{"cat-mugi"});
                var stale=Make(b,"set_auto_assignment","automatic");Good(b,"set_auto_assignment","manual");string before=Capture(b);Check(Raw(b,stale).result==UnityWebRequest.Result.ProtocolError && before==Capture(b),"Stale revision was accepted");Save(b,3);
                Good(b,"start_business");Check(b.View().service_assignment.waiting && b.View().required_action=="service_assignment","Manual mode did not pause for assignment");Save(b,4);Reject(b,"advance_business");Reject(b,"assign_service",cat:"unknown",customer:"guest-1",seat:"seat-1");Reject(b,"assign_service",cat:"cat-mugi",customer:"unknown",seat:"seat-1");Reject(b,"assign_service",cat:"cat-mugi",customer:"guest-1",seat:"seat-99");Reject(b,"assign_service",cat:"cat-mugi",customer:"guest-1",seat:"seat-1",working:new[]{"cat-mugi"});
                double funds=b.Simulation.State.funds,stamina=b.Simulation.State.cats.Single(c=>c.id=="cat-mugi").stamina;
                var request=Make(b,"assign_service",cat:"cat-mugi",customer:"guest-3",seat:"seat-2");Check(Raw(b,request).result==UnityWebRequest.Result.Success,"Manual non-front assignment failed");before=Capture(b);Check(Raw(b,request).result==UnityWebRequest.Result.Success && before==Capture(b),"Duplicate request changed services");
                Check(b.Simulation.State.tick==1 && b.Simulation.State.funds==funds && b.Simulation.State.cats.Single(c=>c.id=="cat-mugi").stamina==stamina,"Manual assignment advanced time, money or stamina");Check(b.Simulation.State.queue.SequenceEqual(new[]{"guest-1","guest-2"}),"Manual selection changed FIFO remainder");Save(b,5);Reject(b,"advance_business");Reject(b,"assign_service",cat:"cat-mugi",customer:"guest-1",seat:"seat-1");Reject(b,"assign_service",cat:"cat-sora",customer:"guest-1",seat:"seat-2");
                Good(b,"assign_service",cat:"cat-sora",customer:"guest-1",seat:"seat-1");Check(!b.View().service_assignment.waiting,"Occupied seats should permit time to advance");Save(b,6);
                Good(b,"set_auto_assignment","automatic");Check(b.Simulation.State.tick==1 && b.Simulation.State.exchanges.Single(e=>e.seat==1).cat_id=="cat-mugi","Automatic setting replaced an active assignment");Save(b,7);Reject(b,"assign_service",cat:"cat-kohaku",customer:"guest-2",seat:"seat-1");Good(b,"advance_business");Save(b,8);
                while(b.Simulation.State.phase!="closed")Good(b,"advance_business");Save(b,9);Good(b,"set_auto_assignment","manual");Reject(b,"assign_service",cat:"cat-mugi",customer:"guest-2",seat:"seat-1");Good(b,"set_auto_assignment","automatic");Good(b,"next_day");Save(b,10);
                // Read-only eligibility probes supplement real Python health/dispatch/adoption traces.
                var probe=new CafeSimulation(rules);probe.Execute("set_auto_assignment",choice:"manual");probe.Execute("start_business");var c=probe.State.cats[0];
                foreach(string activity in new[]{"dispatched","missing","adopted"}){c.activity=activity;Check(probe.AssignmentCatReason(c.id)!="","Unavailable activity accepted");}c.activity="cafe";c.health="sick";Check(probe.AssignmentCatReason(c.id)!="","Sick cat accepted");c.health="healthy";c.working=false;Check(probe.AssignmentCatReason(c.id)!="","Resting cat accepted");c.working=true;c.stamina=0;Check(probe.AssignmentCatReason(c.id)!="","Exhausted cat accepted");
                foreach(var cat in probe.State.cats)cat.working=false;Check(!probe.AssignmentWaiting,"No available cats should not pause");int tick=probe.State.tick;probe.Execute("advance_business");Check(probe.State.tick==tick+1,"No available cats prevented clock advancement");
            }
            return checks;
        }
    }
}
