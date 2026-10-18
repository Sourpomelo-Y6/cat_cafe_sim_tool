using System;
using System.Collections.Generic;
using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        List<CafeDispatchConversation> dispatchConversations=new List<CafeDispatchConversation>();
        string dispatchSelected;
        public bool DispatchConversationOpen {get;set;}
        public CafeDispatchConversation[] DispatchConversations {get{SyncConversations();return dispatchConversations.ToArray();}}
        public CafeDispatchConversation DispatchConversation {get{SyncConversations();return dispatchConversations.FirstOrDefault(r=>r.event_id==dispatchSelected);}}
        public string DispatchConversationSelected=>DispatchConversation?.event_id;
        public bool DispatchResultPending=>DispatchConversations.Any(CafeDispatchDialogue.ResultPending);
        public string DispatchConversationReason=>DispatchResultPending?"通常派遣の手続きは確定済みですが、結果会話が未完了です。イベント一覧の「一般派遣・帰還」から再開し、「会話を終える」まで進めてください。":DispatchConversationOpen?"通常派遣の会話中です。閉じるか、結果会話を最後まで進めてください。":"";
        void SyncDispatchConversations(){foreach(var row in dispatchConversations)CafeDispatchDialogue.Sync(row,Simulation.State.dispatches.FirstOrDefault(e=>e.id==row.event_id));}
        public void BeginDispatchDeparture(string cat)
        {
            SyncConversations();if(!Simulation.State.cats.Any(c=>c.id==cat))throw new ArgumentException("猫を選んでください。");
            string id="dispatch-"+Simulation.State.day+"-"+cat;
            var ev=Simulation.State.dispatches.FirstOrDefault(e=>e.id==id);if(ev!=null){BeginDispatchEvent(id);return;}
            var row=dispatchConversations.FirstOrDefault(r=>r.event_id==id);
            if(row==null){row=new CafeDispatchConversation{event_id=id,cat_id=cat,day=Simulation.State.day,departure=CafeDispatchDialogue.Start("departure")};dispatchConversations.Add(row);}
            if(row.departure.completed){row.departure.completed=false;row.departure.branch_id="introduction";row.departure.node=0;}
            row.stage="departure";dispatchSelected=id;
        }
        public bool BeginDispatchEvent(string id)
        {
            SyncConversations();var ev=Simulation.State.dispatches.FirstOrDefault(e=>e.id==id && e.destination==CafeDispatchDialogue.Destination);if(ev==null)return false;
            var row=dispatchConversations.FirstOrDefault(r=>r.event_id==id);
            if(row==null){if(ev.status=="resolved")return false;row=new CafeDispatchConversation{event_id=id,cat_id=ev.cat_id,day=ev.started_day};dispatchConversations.Add(row);}
            if(row.departure!=null && !row.departure.completed && row.departure.branch_id=="departed")row.stage="departure";
            else if(ev.status=="travelling"){row.stage="travelling";row.travelling=row.travelling??CafeDispatchDialogue.Start("travelling");if(row.travelling.completed)return false;}
            else if(ev.status=="waiting"){row.stage="return";row.returning=row.returning??CafeDispatchDialogue.Start("return");}
            else if(row.returning?.completed==false)row.stage="return";
            else return false;
            dispatchSelected=id;return true;
        }
        public void PrepareDispatchConversation()
        {
            var rows=DispatchConversations;var pending=rows.FirstOrDefault(CafeDispatchDialogue.ResultPending);
            if(pending!=null){dispatchSelected=pending.event_id;pending.stage=pending.departure!=null && !pending.departure.completed && pending.departure.branch_id!="introduction"?"departure":"return";return;}
            var current=DispatchConversation;if(current?.Current?.completed==false && (Simulation.State.dispatches.Any(e=>e.id==current.event_id) || current.day==Simulation.State.day))return;
            var waiting=Simulation.State.dispatches.FirstOrDefault(e=>e.destination==CafeDispatchDialogue.Destination && e.status=="waiting");if(waiting!=null)BeginDispatchEvent(waiting.id);
        }
        void RestoreDispatchConversations(CafeDispatchConversation[] rows,string selected){dispatchConversations=(rows??new CafeDispatchConversation[0]).ToList();dispatchSelected=selected;DispatchConversationOpen=false;}
    }
}
