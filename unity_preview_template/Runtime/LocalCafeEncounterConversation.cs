using System.Collections.Generic;
using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        List<CafeEncounterConversation> encounterConversations=new List<CafeEncounterConversation>();
        string encounterSelected;
        public bool EncounterConversationOpen {get;set;}
        public CafeEncounterConversation[] EncounterConversations {get{SyncConversations();return encounterConversations.ToArray();}}
        public CafeEncounterConversation EncounterConversation {get{SyncConversations();return encounterConversations.FirstOrDefault(r=>r.event_id==encounterSelected);}}
        public string EncounterConversationSelected=>EncounterConversation?.event_id;
        public bool EncounterResultPending=>EncounterConversations.Any(CafeEncounterDialogue.ResultPending);
        public string EncounterConversationReason=>EncounterResultPending?"派遣中の選択は回答済みですが、結果会話が未完了です。イベント一覧の「派遣中の選択イベント」から再開してください。":EncounterConversationOpen?"派遣中の選択イベントの会話中です。営業・自動進行は停止しています。":"";
        void SyncEncounterConversations(){foreach(var row in encounterConversations)CafeEncounterDialogue.Sync(row,Simulation.State.dispatches.FirstOrDefault(e=>e.id==row.event_id));}
        public bool BeginEncounterConversation(string id)
        {
            SyncConversations();var ev=Simulation.State.dispatches.FirstOrDefault(e=>e.id==id);
            if(!CafeEncounterDialogue.Supports(ev) || ev.encounter.status=="scheduled")return false;
            var row=encounterConversations.FirstOrDefault(r=>r.event_id==id);
            if(row==null){if(ev.encounter.status!="waiting")return false;row=new CafeEncounterConversation{event_id=id,progress=CafeEncounterDialogue.Start()};encounterConversations.Add(row);}
            if(row.progress.completed)return false;encounterSelected=id;return true;
        }
        public void PrepareEncounterConversation()
        {
            var pending=EncounterConversations.FirstOrDefault(CafeEncounterDialogue.ResultPending);
            if(pending!=null){encounterSelected=pending.event_id;return;}
            if(EncounterConversation?.progress.completed==false)return;
            var waiting=Simulation.State.dispatches.FirstOrDefault(e=>CafeEncounterDialogue.Supports(e) && e.encounter.status=="waiting");if(waiting!=null)BeginEncounterConversation(waiting.id);
        }
        void RestoreEncounterConversations(CafeEncounterConversation[] rows,string selected){encounterConversations=(rows??new CafeEncounterConversation[0]).ToList();encounterSelected=selected;EncounterConversationOpen=false;}
    }
}
