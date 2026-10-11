using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
namespace CatCafePreview
{
    public static class StandaloneHousingSmoke
    {
        private static int checks;
        private static void Check(bool v,string m){checks++;if(!v)throw new Exception("Housing: "+m);}
        private static CafeResponse Send(LocalCafeBackend b,string kind,string choice=null,int slot=1,string[] working=null,string cat=null,string eventId=null)
        {
            var cmd=new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,
                cat_id=cat,event_id=eventId,save_id=slot.ToString(),save_slot=slot,working_cats=working??new string[0]};
            CafeResponse reply=null;var request=b.Request("/commands",JsonUtility.ToJson(cmd),r=>reply=r);while(request.MoveNext()){}return reply;
        }
        private static void Good(LocalCafeBackend b,string kind,string choice=null,int slot=1,string[] working=null,string cat=null,string eventId=null)
        {var r=Send(b,kind,choice,slot,working,cat,eventId);Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static string Capture(LocalCafeBackend b)
        {var s=JsonUtility.FromJson<Standalone.CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();s.instance_id="";s.revision=0;return JsonUtility.ToJson(s);}
        private static void Resume(LocalCafeBackend b,int slot)
        {string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(Capture(b)==before,"Save changed housing or finance");}
        private static void Reject(LocalCafeBackend b,string kind)
        {string before=Capture(b);Check(Send(b,kind).result==UnityWebRequest.Result.ProtocolError,"Accepted "+kind);Check(Capture(b)==before,"Rejected command changed state");}
        public static int Run(string path)
        {
            checks=0;var b=new LocalCafeBackend(path);
            if(File.Exists(Path.Combine(path,"slot-6.json")))
            {Good(b,"load_game",slot:2);Check(b.Simulation.IntakeWaiting && b.View().housing.stage==0,"Restart lost pending admission");Good(b,"load_game",slot:6);Check(b.View().housing.stage==2 && b.Simulation.State.cats.Count==7,"Restart lost expansion or cats");}
            Good(b,"new_game",choice:"free");Reject(b,"upgrade_housing");bool upgraded=false;
            for(int day=1;day<=14;day++)
            {
                foreach(var c in b.Simulation.State.cats.Where(c=>c.activity=="missing" && c.missing_remaining==0).ToArray())Good(b,"resolve_missing",cat:c.id);
                foreach(var e in b.Simulation.State.trust_events.Where(e=>e.status=="waiting").ToArray())Good(b,"resolve_customer_trust",choice:"recover",eventId:e.id);
                StandaloneGrowthSmoke.ResolvePending(b);
                if(b.Simulation.StoreEventWaiting)Good(b,"resolve_store_event",choice:b.Simulation.CurrentStoreEvent.type=="trouble"?"patch":"decline");
                if(b.Simulation.IntakeWaiting)
                {Resume(b,2);Check(b.View().housing.can_expand,"Pending intake blocked housing");Good(b,"purchase_housing");Resume(b,3);Reject(b,"purchase_housing");Good(b,"resolve_intake",choice:"accept");}
                if(b.Simulation.RegularIntroductionWaiting)
                {Check(b.Simulation.State.cats.Count==6 && b.View().housing.capacity==9,"Admission capacity mismatch");Good(b,"resolve_regular_introduction",choice:"accept",cat:b.Simulation.Rules.regular_introduction.cat.id);Check(b.Simulation.State.cats.Count==7,"Expanded housing failed seventh cat");}
                if(day>=10 && b.View().housing.can_expand){Good(b,"upgrade_housing");upgraded=true;Resume(b,4);Reject(b,"upgrade_housing");}
                Good(b,"set_shifts",working:b.Simulation.State.cats.Where((c,i)=>(i+day)%3!=0 && c.health=="healthy" && c.activity=="cafe").Select(c=>c.id).ToArray());
                Good(b,"start_business");
                while(b.Simulation.State.phase!="closed")
                {StandaloneGrowthSmoke.ResolvePending(b);if(b.Simulation.State.tick==11){Reject(b,"purchase_housing");Resume(b,5);}Good(b,"advance_business");}
                Check(b.Simulation.State.housing_cost==b.View().housing.daily_cost,"Daily housing settlement mismatch");
                StandaloneGrowthSmoke.ResolvePending(b);
                foreach(var c in b.Simulation.State.cats.Where(c=>c.activity=="missing" && c.missing_remaining==0).ToArray())Good(b,"resolve_missing",cat:c.id);
                Good(b,"next_day");
            }
            Check(upgraded && b.View().housing.capacity==12 && b.Simulation.State.cats.Count==7,"Fixture missed two stages");Resume(b,6);
            string final=Capture(b);var fresh=new LocalCafeBackend(path);Good(fresh,"load_game",slot:6);Check(Capture(fresh)==final,"Fresh backend lost housing");
            var bad=JsonUtility.FromJson<Standalone.CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));bad.NormalizeJsonStrings();bad.housing.purchased_day=0;
            bool failed=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){failed=true;}Check(failed && Capture(b)==final,"Forged housing accepted");return checks;
        }
    }
}
