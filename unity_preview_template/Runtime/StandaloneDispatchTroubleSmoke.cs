using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneDispatchTroubleSmoke
    {
        private static int checks;
        private static void Check(bool value,string message){checks++;if(!value)throw new Exception("Lodging trouble: "+message);}
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
        public static void Verify()
        {
            string root=Environment.GetEnvironmentVariable("CATCAFE_DISPATCH_TROUBLE_SMOKE")??"dispatch-trouble-smoke";Directory.CreateDirectory(root);int total=0;
            foreach(bool trigger in new[]{false,true})foreach(string choice in new[]{"search","wait"})foreach(bool closed in new[]{false,true})total+=Run(Path.Combine(root,trigger+"-"+choice+"-"+closed),trigger,choice,closed);
            File.WriteAllText(Path.Combine(root,"result.json"),"{\"passed\":true,\"checks\":"+total+"}");
        }
        public static int Run(string path,bool trigger,string choice,bool closed)
        {
            checks=0;var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.starting_funds=20000;rules.store_events.probability=0;rules.max_probability=0;rules.dispatch_trouble.base_probability=trigger?1:0;rules.dispatch_trouble.stress_probability=0;
            rules.rules_id+="-lodging-"+trigger+"-"+choice+"-"+closed;var b=new LocalCafeBackend(path,rules);
            if(File.Exists(Path.Combine(path,"slot-6.json")))foreach(int slot in new[]{2,3,4,5,6})
            {Good(b,"load_game",slot:slot);var e=b.Simulation.State.dispatches.First();Check(e.destination=="mountain_lodge_visit" && (slot==2?e.trouble.status=="scheduled":slot==3?e.trouble.status==(trigger?"waiting":"not_triggered"):slot==4?e.trouble.status==(trigger?"resolved":"not_triggered"):slot==5?e.status=="waiting":e.status=="resolved"),"Cross-process lodging checkpoint differs");}
            Good(b,"new_game","free");Reject(b,"dispatch_general","mountain_lodge_visit","cat-mike");
            var cat=b.Simulation.State.cats.Single(c=>c.id=="cat-tama");cat.fatigue=20;Check(b.Simulation.DispatchReason(cat.id,"mountain_lodge_visit")=="","Fatigue equality rejected");cat.fatigue=20.001;Check(b.Simulation.DispatchReason(cat.id,"mountain_lodge_visit")!="","Excess fatigue allowed");cat.fatigue=0;
            Good(b,"dispatch_general","mountain_lodge_visit","cat-tama");string id="dispatch-1-cat-tama";Good(b,"dispatch_general","neighborhood_visit","cat-sora");Resume(b,2);Reject(b,"resolve_dispatch_trouble",choice,"cat-tama",id);
            Action close=()=>{if(!closed){Good(b,"day_off");return;}Good(b,"set_shifts");Good(b,"start_business");while(b.Simulation.State.phase!="closed"){Events(b);Good(b,"advance_business");}};
            close();Resume(b,3);Good(b,"receive_general","dispatch-1-cat-sora","cat-sora");
            if(trigger)
            {
                Check(b.View().required_action=="dispatch_trouble" && b.View().dispatch_troubles.First().choices.All(o=>o.can_select),"Trouble choices not projected");Reject(b,"day_off");Reject(b,"set_shifts");Reject(b,"resolve_missing",cat:"cat-tama");Reject(b,"receive_general",id,"cat-tama");Reject(b,"resolve_dispatch_trouble","invalid","cat-tama",id);
                double funds=b.Simulation.State.funds,cost=rules.dispatch_trouble.search_cost;
                foreach(double value in new[]{cost-1,cost,cost+.001}){b.Simulation.State.funds=value;Check((b.Simulation.DispatchTroubleReason(id,"search")=="")== (value>cost),"Strict search funds boundary differs");if(value<=cost)Reject(b,"resolve_dispatch_trouble","search","cat-tama",id);}b.Simulation.State.funds=funds;
                string payload=Command(b,"resolve_dispatch_trouble",choice,"cat-tama",id);var reply=Request(b,payload);Check(reply.result==UnityWebRequest.Result.Success,"Trouble response failed");string before=Capture(b);Check(Request(b,payload).text==reply.text && before==Capture(b),"Duplicate search payment applied twice");Check(b.Simulation.State.funds==funds-(choice=="search"?cost:0),"Wrong search payment");Reject(b,"resolve_dispatch_trouble",choice=="search"?"wait":"search","cat-tama",id);
            }
            else Reject(b,"resolve_dispatch_trouble",choice,"cat-tama",id);
            Resume(b,4);
            while(!b.Simulation.DispatchWaiting){Events(b);if(b.Simulation.State.phase=="closed")Good(b,"next_day");Events(b);close();}
            Resume(b,5);double money=b.Simulation.State.funds;double experience=b.Simulation.State.growth.Single(g=>g.cat_id=="cat-tama").dispatch;Good(b,"receive_general",id,"cat-tama");cat=b.Simulation.State.cats.Single(c=>c.id=="cat-tama");
            Check(b.Simulation.State.funds==money+(trigger?0:812.5) && b.Simulation.State.growth.Single(g=>g.cat_id==cat.id).dispatch==experience+(trigger?0:1),"Interrupted reward/experience differs");if(trigger)Check(cat.stamina==rules.max_stamina && cat.stress==rules.dispatch_trouble.return_stress && !cat.cannot_continue && cat.activity=="cafe","Wrong rescued cat state");Reject(b,"receive_general",id,"cat-tama");Events(b);Resume(b,6);
            string captured=Capture(b);
            for(int field=0;field<8;field++)
            {
                var bad=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));bad.NormalizeJsonStrings();CafeDoublePrecision.Restore(bad,CafeDoublePrecision.Capture(b.Simulation.State));var e=bad.dispatches.First();var v=e.trouble;
                if(field==0)v.draw+=.01;else if(field==1)v.probability+=.01;else if(field==2)v.cost++;else if(field==3)v.missing_day++;else if(field==4)v.choice_day+=3;else if(field==5)e.remaining++;else if(field==6)e.trouble=null;else bad.dispatch_trouble_expenses++;
                bool rejected=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected && captured==Capture(b),"Forged lodging save accepted: "+field+" / "+trigger+" / "+choice+" / "+closed);
            }
            return checks;
        }
    }
}
