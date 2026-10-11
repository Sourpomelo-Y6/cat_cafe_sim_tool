using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneServiceSmoke
    {
        private static int checks;
        private static void Check(bool value,string message){checks++;if(!value)throw new Exception("Service: "+message);}
        private static string Command(LocalCafeBackend b,string kind,string choice=null,string seat=null,string cat=null,int slot=1) => JsonUtility.ToJson(new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,seat_id=seat,cat_id=cat,working_cats=b.Simulation.State.cats.Where(c=>c.health=="healthy" && c.activity=="cafe").Select(c=>c.id).ToArray(),save_slot=slot,save_id=slot.ToString()});
        private static CafeResponse Request(LocalCafeBackend b,string json){CafeResponse r=null;var request=b.Request("/commands",json,x=>r=x);while(request.MoveNext()){}return r;}
        private static void Good(LocalCafeBackend b,string kind,string choice=null,string seat=null,string cat=null,int slot=1){var r=Request(b,Command(b,kind,choice,seat,cat,slot));Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static string Capture(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();s.instance_id="";s.revision=0;return JsonUtility.ToJson(s);}
        private static void Resume(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Resume changed service/equipment");}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null,string seat=null){string before=Capture(b);Check(Request(b,Command(b,kind,choice,seat)).result==UnityWebRequest.Result.ProtocolError,"Invalid command accepted");Check(before==Capture(b),"Rejection changed state");}
        private static void Events(LocalCafeBackend b)
        {
            while(b.View().growth_choices.Length>0){var row=b.View().growth_choices[0];Good(b,row.kind,choice:row.kind=="resolve_growth"?"service":row.choices[0].choice,cat:row.cat_id);}
            if(b.Simulation.ReservationWaiting)Good(b,"resolve_reservation",choice:"accept");
            if(b.Simulation.GoalWaiting)Good(b,"continue_goal");
            if(b.Simulation.IntakeWaiting)Good(b,"resolve_intake",choice:"decline");
            if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction",choice:"decline",cat:b.Simulation.Rules.regular_introduction.cat.id);
            if(b.Simulation.GoalResponseReason("advance_goal")=="")Good(b,"advance_goal");
        }
        public static int Run(string path,string kind,bool insufficient=false)
        {
            checks=0;var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
            rules.starting_funds=20000;rules.store_events.probability=0;rules.stress_per_service_tick=0;rules.fatigue_per_service_tick=0;rules.max_probability=0;
            rules.goal_stages[0].target=110;rules.goal_stages[1].target=120;rules.goal_stages[2].target=130;
            if(insufficient)rules.play_customer.play_count=999;
            rules.rules_id+="-service-fixture-"+kind+"-"+insufficient;
            var b=new LocalCafeBackend(path,rules);string customer=kind=="quiet_space"?"advanced-quiet":kind=="cat_tower"?"advanced-play":"advanced-contact";
            if(File.Exists(Path.Combine(path,"slot-4.json")))
            {
                Good(b,"load_game",slot:4);Check(b.Simulation.State.equipment_purchases.Count==8,"Restart lost equipment");
                Check(b.Simulation.State.history.SelectMany(h=>h.customer_records).Any(v=>v.customer_id==customer && v.specialist_result==(insufficient?"failure":"success")),"Restart lost result");
                Good(b,"load_game",slot:2);Check(b.Simulation.State.exchanges.Any(e=>e.service_group!=""),"Restart lost service in progress");
            }
            Good(b,"new_game",choice:"popularity");Good(b,"expand_seats");bool checkpoint=false,seen=false;
            for(int day=1;day<=12;day++)
            {
                Events(b);while(b.Simulation.ExpansionReason()=="")Good(b,"expand_seats");
                if(b.Simulation.State.seat_count==8 && b.Simulation.State.equipment_purchases.Count==0)
                {
                    Reject(b,"purchase_seat_equipment",kind,"seat-99");Reject(b,"equip_seat","equipment-99","seat-1");
                    for(int i=1;i<=8;i++)Good(b,"purchase_seat_equipment",kind,"seat-"+i);
                    Good(b,"equip_seat","equipment-1","seat-2");Check(b.View().seat_equipment.owned[0].seat_id=="seat-2","Move did not project");
                    Good(b,"equip_seat","equipment-1","seat-1");Good(b,"equip_seat","equipment-2","seat-2");Good(b,"equip_seat","","seat-8");Good(b,"equip_seat","equipment-8","seat-8");Resume(b,3);
                }
                Good(b,"set_shifts");Good(b,"start_business");Reject(b,"equip_seat","","seat-1");
                while(b.Simulation.State.phase!="closed")
                {
                    Events(b);Good(b,"advance_business");
                    if(!checkpoint && b.Simulation.State.exchanges.Any(e=>e.customer_id==customer)){Resume(b,2);checkpoint=true;}
                }
                foreach(var v in b.Simulation.State.visits.Where(v=>v.id==customer && v.satisfaction!=null && b.Simulation.State.equipment_purchases.Count==8))
                {
                    Check(v.service_equipment,"Preferred equipped seat was not selected");Check(v.specialist_result==(insufficient?"failure":"success"),"Result differs");
                    Check(v.specialist_bonus==(insufficient?0:kind=="quiet_space"?rules.quiet_customer.bonus:kind=="cat_tower"?rules.play_customer.bonus:rules.contact_customer.bonus),"Bonus differs");seen=true;
                }
                Resume(b,1);Events(b);Good(b,"next_day");
            }
            Check(seen && checkpoint,"Target customer was not served");Resume(b,4);
            string final=Capture(b);
            for(int i=0;i<5;i++)
            {
                var bad=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));bad.NormalizeJsonStrings();
                var record=bad.history.SelectMany(h=>h.customer_records).First(v=>v.customer_id==customer && v.satisfaction!=null && v.service_equipment);
                if(i==0)bad.equipment_placements[0].item="equipment-99";else if(i==1)bad.equipment_moves.Clear();else if(i==2)record.service_equipment=false;else if(i==3)record.service_count++;else record.specialist_bonus++;
                bool rejected=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected && final==Capture(b),"Forged equipment/result accepted");
            }
            return checks;
        }
    }
}
