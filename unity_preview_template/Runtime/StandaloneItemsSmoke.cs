using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneItemsSmoke
    {
        private static int checks;
        private static void Check(bool v,string m){checks++;if(!v)throw new Exception("Items: "+m);}
        private static CafeResponse Request(LocalCafeBackend b,string json){CafeResponse result=null;var request=b.Request("/commands",json,r=>result=r);while(request.MoveNext()){}return result;}
        private static string Command(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1)=>JsonUtility.ToJson(new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,working_cats=new string[0],save_id=slot.ToString(),save_slot=slot});
        private static void Good(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1){var r=Request(b,Command(b,kind,choice,cat,slot));Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static string Capture(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();s.instance_id="";s.revision=0;return JsonUtility.ToJson(s);}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null,string cat=null){string before=Capture(b);Check(Request(b,Command(b,kind,choice,cat)).result==UnityWebRequest.Result.ProtocolError,"Accepted invalid "+kind);Check(before==Capture(b),"Rejected item changed state");}
        private static void Resume(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Save changed items or finance");}
        public static int Run(string path)
        {
            checks=0;var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.starting_funds=2000;rules.rules_id+="-items-fixture";
            var b=new LocalCafeBackend(Path.Combine(path,"normal"),rules);
            if(File.Exists(Path.Combine(path,"normal/slot-4.json"))){Good(b,"load_game",slot:4);Check(b.Simulation.State.item_purchases.Count==3 && b.Simulation.State.item_sales.Count>=1 && b.Simulation.State.item_uses.Count>=2,"Restart lost item lifecycle");}
            int seed=Enumerable.Range(0,1000).First(i=>new CafeSimulation(rules,i).CurrentStoreEvent.type=="supplies");b.Simulation.Restore(new CafeSimulation(rules,seed).State);Good(b,"new_game",choice:"free");
            Reject(b,"purchase_item","unknown");string first=b.Simulation.State.cats[0].id;Reject(b,"use_item","store-event-1",first);
            foreach(var offer in rules.item_shop)Good(b,"purchase_item",offer.item.id);Resume(b,2);
            string duplicate=Command(b,"sell_item","store-event-1");Check(Request(b,duplicate).result==UnityWebRequest.Result.Success,"Gift sale failed");string sold=Capture(b);Check(Request(b,duplicate).result==UnityWebRequest.Result.Success && sold==Capture(b),"Retry sold gift twice");Reject(b,"sell_item","store-event-1");Reject(b,"use_item","store-event-1",first);
            Good(b,"start_business");Reject(b,"purchase_item","care_supplies");Reject(b,"sell_item","item-purchase-1");while(b.Simulation.State.phase!="closed")Good(b,"advance_business");Good(b,"next_day");
            if(b.Simulation.StoreEventWaiting)Good(b,"resolve_store_event",b.Simulation.CurrentStoreEvent.type=="trouble"?"patch":"decline");
            var cat=b.Simulation.State.cats.First(c=>c.activity=="cafe" && c.fatigue>0 && c.stress>0);double fatigue=cat.fatigue;Good(b,"use_item","item-purchase-2",cat.id);Check(cat.fatigue==Math.Max(0,fatigue-15),"Snack effect differs");Resume(b,3);
            cat=b.Simulation.State.cats.First(c=>c.id==cat.id);Good(b,"use_item","item-purchase-3",cat.id);Resume(b,4);Reject(b,"sell_item","item-purchase-2");Reject(b,"use_item","item-purchase-3",cat.id);
            Good(b,"sell_item","item-purchase-1");Resume(b,4);string final=Capture(b);var fresh=new LocalCafeBackend(Path.Combine(path,"normal"),rules);Good(fresh,"load_game",slot:4);Check(final==Capture(fresh),"Fresh backend lost items");
            foreach(int kind in new[]{0,1,2,3}){var bad=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));bad.NormalizeJsonStrings();if(kind==0)bad.item_purchases[0].cost++;else if(kind==1)bad.item_sales[0].price++;else if(kind==2)bad.item_uses[0].after_fatigue++;else bad.item_sales[0].source=bad.item_uses[0].source;bool failed=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){failed=true;}Check(failed && final==Capture(b),"Forged lifecycle accepted");}
            rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.starting_funds=100;rules.rules_id+="-exact-item-fixture";rules.store_events.probability=0;
            var exact=new LocalCafeBackend(Path.Combine(path,"exact"),rules);Good(exact,"new_game",choice:"free");Good(exact,"purchase_item","care_supplies");Check(exact.Simulation.State.funds==0 && exact.Simulation.State.game_over=="funds","Exact price must end game");Resume(exact,2);Reject(exact,"sell_item","item-purchase-1");return checks;
        }
    }
}
