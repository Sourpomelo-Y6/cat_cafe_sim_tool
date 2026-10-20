using System.Collections.Generic;
using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        List<CafeDispatchIntroductionConversation> dispatchIntroductionConversations=new List<CafeDispatchIntroductionConversation>();
        string dispatchIntroductionSelected;
        public bool DispatchIntroductionConversationOpen {get;set;}
        public CafeDispatchIntroductionConversation[] DispatchIntroductionConversations {get{SyncConversations();return dispatchIntroductionConversations.ToArray();}}
        public CafeDispatchIntroductionConversation DispatchIntroductionConversation {get{SyncConversations();return dispatchIntroductionConversations.FirstOrDefault(r=>r.event_id==dispatchIntroductionSelected);}}
        public string DispatchIntroductionConversationSelected=>DispatchIntroductionConversation?.event_id;
        public bool DispatchIntroductionResultPending=>DispatchIntroductionConversations.Any(CafeDispatchIntroductionDialogue.ResultPending);
        public string DispatchIntroductionConversationReason=>DispatchIntroductionResultPending?"派遣先からの猫紹介は回答済みですが、結果会話が未完了です。イベント一覧の「派遣先からの猫紹介」から再開してください。":DispatchIntroductionConversationOpen?"派遣先からの猫紹介の会話中です。営業・自動進行は停止しています。":"";
        void SyncDispatchIntroductionConversations(){foreach(var row in dispatchIntroductionConversations)CafeDispatchIntroductionDialogue.Sync(row,Simulation.State.dispatches.FirstOrDefault(e=>e.id==row.event_id));}
        public bool BeginDispatchIntroductionConversation(string id)
        {
            SyncConversations();var ev=Simulation.State.dispatches.FirstOrDefault(e=>e.id==id);
            if(!CafeDispatchIntroductionDialogue.Supports(ev) || ev.introduction.status=="scheduled")return false;
            var row=dispatchIntroductionConversations.FirstOrDefault(r=>r.event_id==id);
            if(row==null){if(ev.introduction.status!="waiting")return false;row=new CafeDispatchIntroductionConversation{event_id=id,progress=CafeDispatchIntroductionDialogue.Start(ev.destination)};dispatchIntroductionConversations.Add(row);}
            if(row.progress.completed)return false;dispatchIntroductionSelected=id;return true;
        }
        public void PrepareDispatchIntroductionConversation()
        {
            var pending=DispatchIntroductionConversations.FirstOrDefault(CafeDispatchIntroductionDialogue.ResultPending);
            if(pending!=null){dispatchIntroductionSelected=pending.event_id;return;}
            if(DispatchIntroductionConversation?.progress.completed==false)return;
            var waiting=Simulation.State.dispatches.FirstOrDefault(e=>CafeDispatchIntroductionDialogue.Supports(e) && e.introduction.status=="waiting");if(waiting!=null)BeginDispatchIntroductionConversation(waiting.id);
        }
        void RestoreDispatchIntroductionConversations(CafeDispatchIntroductionConversation[] rows,string selected){dispatchIntroductionConversations=(rows??new CafeDispatchIntroductionConversation[0]).ToList();dispatchIntroductionSelected=selected;DispatchIntroductionConversationOpen=false;}
    }
}
