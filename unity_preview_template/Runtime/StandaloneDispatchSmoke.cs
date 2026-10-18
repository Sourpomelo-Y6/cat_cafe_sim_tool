using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneDispatchSmoke
    {
        private static int checks;
        private const string Cat="cat-tama",Destination="neighborhood_visit";
        private static void Check(bool value,string message){checks++;if(!value)throw new Exception("Dispatch: "+message);}
        private static string Command(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1)=>JsonUtility.ToJson(new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,working_cats=b.Simulation.State.cats.Where(c=>c.activity=="cafe" && c.health=="healthy").Select(c=>c.id).ToArray(),save_slot=slot,save_id=slot.ToString()});
        private static CafeResponse Request(LocalCafeBackend b,string json){CafeResponse r=null;var it=b.Request("/commands",json,x=>r=x);while(it.MoveNext()){}return r;}
        private static void Good(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1){var r=Request(b,Command(b,kind,choice,cat,slot));Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static string Capture(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();s.instance_id="";s.revision=0;return JsonUtility.ToJson(s);}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null,string cat=null){string before=Capture(b);Check(Request(b,Command(b,kind,choice,cat)).result==UnityWebRequest.Result.ProtocolError,"Invalid command accepted: "+kind);Check(before==Capture(b),"Rejection changed state");}
        private static void Resume(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Save changed dispatch");}
        public static int Run(string path)
        {
            checks=0;var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.store_events.probability=0; // Isolated fixture directory; keep the presentation catalog ID compatible.
            var b=new LocalCafeBackend(path,rules);
            if(File.Exists(Path.Combine(path,"slot-6.json")))
            {
                foreach(int slot in new[]{2,3,4,5,6})
                {
                    Good(b,"load_game",slot:slot);var s=b.Simulation.State;
                    Check(slot==2?s.dispatches[0].status=="travelling":slot==3?s.dispatches[0].status=="waiting":slot==4?s.dispatches[0].status=="resolved":slot==5?s.dispatches[1].status=="waiting":s.dispatches.All(e=>e.status=="resolved"),"Restart lost return phase");
                }
            }
            Good(b,"new_game",choice:"free");Reject(b,"dispatch_general","unknown",Cat);Reject(b,"dispatch_general",Destination,"unknown");
            var sim=b.Simulation;var cat=sim.State.cats.Single(c=>c.id==Cat);
            foreach(string field in new[]{"fatigue","health","stamina","cannot_continue"})
            {
                double fatigue=cat.fatigue,stamina=cat.stamina;string health=cat.health;bool cannot=cat.cannot_continue;
                if(field=="fatigue"){cat.fatigue=60;Check(sim.DispatchReason(Cat,Destination)=="","Fatigue 60 rejected");cat.fatigue=60.0001;}
                if(field=="health")cat.health="sick";if(field=="stamina")cat.stamina=0;if(field=="cannot_continue")cat.cannot_continue=true;
                Check(sim.DispatchReason(Cat,Destination)!="","Unavailable cat admitted");cat.fatigue=fatigue;cat.stamina=stamina;cat.health=health;cat.cannot_continue=cannot;
            }
            Check(sim.DispatchReward("cat-kohaku")==120,"Longhair welcome differs");
            double funds=sim.State.funds,staminaBefore=cat.stamina;string payload=Command(b,"dispatch_general",Destination,Cat);var reply=Request(b,payload);Check(reply.result==UnityWebRequest.Result.Success,"Departure failed");
            string after=Capture(b);Check(Request(b,payload).text==reply.text && after==Capture(b),"Duplicate departure changed state");
            Check(cat.activity=="dispatched" && !cat.working && sim.State.funds==funds && sim.State.dispatches[0].reward==125,"Departure terms changed: activity="+cat.activity+", working="+cat.working+", funds="+sim.State.funds+", expected="+funds+", reward="+sim.State.dispatches[0].reward);
            Reject(b,"dispatch_general",Destination,Cat);Reject(b,"receive_general","dispatch-1-"+Cat,Cat);Resume(b,2);
            Good(b,"set_shifts");Good(b,"start_business");while(sim.State.phase!="closed")Good(b,"advance_business");
            Check(sim.DispatchWaiting && b.View().required_action=="dispatch_return","Return not projected");cat=sim.State.cats.Single(c=>c.id==Cat);Check(cat.stamina==staminaBefore && cat.fatigue==0 && sim.State.growth.Single(g=>g.cat_id==Cat).rest==0,"Dispatch received cafe settlement");
            Resume(b,3);Reject(b,"next_day");Reject(b,"receive_general","dispatch-1-"+Cat,"cat-mike");
            funds=sim.State.funds;payload=Command(b,"receive_general","dispatch-1-"+Cat,Cat);reply=Request(b,payload);Check(reply.result==UnityWebRequest.Result.Success,"Return failed");after=Capture(b);Check(Request(b,payload).text==reply.text && after==Capture(b),"Duplicate return paid twice");
            Check(sim.State.funds==funds+125 && sim.State.dispatch_income==125 && sim.ItemInventory().Any(i=>i.source=="dispatch-1-"+Cat) && sim.State.growth.Single(g=>g.cat_id==Cat).dispatch==1,"Return rewards differ");
            Reject(b,"receive_general","dispatch-1-"+Cat,Cat);Resume(b,4);
            for(int field=0;field<11;field++)
            {
                var bad=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(sim.State));bad.NormalizeJsonStrings();var e=bad.dispatches[0];
                if(field==0)e.id="forged";else if(field==1)e.reward++;else if(field==2)e.remaining++;else if(field==3)e.started_day++;else if(field==4)e.resolved_day++;else if(field==5)e.growth_multiplier++;else if(field==6)e.feature_matched=!e.feature_matched;else if(field==7)e.status="waiting";else if(field==8)bad.cats.Single(c=>c.id==Cat).activity="dispatched";else if(field==9)bad.dispatch_income++;else bad.growth_operations.RemoveAll(o=>o.kind=="dispatch");
                string before=Capture(b);bool rejected=false;try{sim.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected && before==Capture(b),"Forged dispatch accepted");
            }
            Good(b,"next_day");Good(b,"dispatch_general",Destination,Cat);Good(b,"day_off");Check(sim.State.day==3 && sim.State.phase=="preparation" && sim.DispatchWaiting,"Holiday return day differs");Resume(b,5);Reject(b,"day_off");Good(b,"receive_general","dispatch-2-"+Cat,Cat);
            Check(sim.State.dispatch_income==125 && sim.State.history[0].dispatch_income==125 && sim.State.history[1].dispatch_income==0,"Confirmation day income differs");
            Good(b,"use_item","dispatch-1-"+Cat,Cat);Reject(b,"sell_item","dispatch-1-"+Cat);Good(b,"sell_item","dispatch-2-"+Cat);Reject(b,"sell_item","dispatch-2-"+Cat);Resume(b,6);
            Check(sim.ItemInventory().Length==0,"Consumed return item remains");return checks;
        }
    }
}
