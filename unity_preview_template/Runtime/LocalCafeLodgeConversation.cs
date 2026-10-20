using System.Collections.Generic;
using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        List<CafeLodgeConversation> lodgeConversations=new List<CafeLodgeConversation>();string lodgeSelected;
        public bool LodgeConversationOpen {get;set;}
        public CafeLodgeConversation[] LodgeConversations {get{SyncConversations();return lodgeConversations.ToArray();}}
        public CafeLodgeConversation LodgeConversation {get{SyncConversations();return lodgeConversations.FirstOrDefault(r=>r.event_id==lodgeSelected);}}
        public string LodgeConversationSelected=>LodgeConversation?.event_id;
        public bool LodgeResultPending=>LodgeConversations.Any(CafeLodgeDialogue.ResultPending);
        public string LodgeConversationReason=>LodgeResultPending?"宿トラブルの手続きは確定済みですが、結果会話が未完了です。イベント一覧の「山あいの宿・トラブル対応」から再開し、会話を終えてください。":LodgeConversationOpen?"宿トラブルの会話中です。閉じた会話はイベント一覧から再開できます。":"";
        void SyncLodgeConversations(){foreach(var r in lodgeConversations)CafeLodgeDialogue.Sync(r,Simulation.State.dispatches.FirstOrDefault(e=>e.id==r.event_id));}
        public bool BeginLodgeConversation(string id)
        {
            SyncConversations();var e=Simulation.State.dispatches.FirstOrDefault(x=>x.id==id);if(!CafeLodgeDialogue.Supports(e))return false;
            var r=lodgeConversations.FirstOrDefault(x=>x.event_id==id);
            if(r==null){if(e.trouble.status!="waiting" && e.status!="waiting")return false;r=new CafeLodgeConversation{event_id=id};lodgeConversations.Add(r);}
            if(r.response==null && e.trouble.status=="waiting")r.response=CafeLodgeDialogue.Start(false);
            if(r.response?.completed!=false && r.returning==null && e.status=="waiting")r.returning=CafeLodgeDialogue.Start(true);
            if(r.Current==null || r.Current.completed)return false;lodgeSelected=id;return true;
        }
        public void PrepareLodgeConversation()
        {
            var rows=LodgeConversations;if(LodgeConversation?.Current?.completed==false)return;
            var pending=rows.FirstOrDefault(CafeLodgeDialogue.ResultPending);if(pending!=null){lodgeSelected=pending.event_id;return;}
            foreach(var e in Simulation.State.dispatches.Where(CafeLodgeDialogue.Supports))if(BeginLodgeConversation(e.id))return;
        }
        void RestoreLodgeConversations(CafeLodgeConversation[] rows,string selected){lodgeConversations=(rows??new CafeLodgeConversation[0]).ToList();lodgeSelected=selected;LodgeConversationOpen=false;}
    }
}
