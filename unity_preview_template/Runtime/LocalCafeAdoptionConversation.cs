using System.Collections.Generic;
using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        List<CafeAdoptionConversation> adoptionConversations=new List<CafeAdoptionConversation>();string adoptionSelected;
        public bool AdoptionConversationOpen {get;set;}
        public CafeAdoptionConversation[] AdoptionConversations {get{SyncConversations();return adoptionConversations.ToArray();}}
        public CafeAdoptionConversation AdoptionConversation {get{SyncConversations();return adoptionConversations.FirstOrDefault(r=>r.event_id==adoptionSelected);}}
        public string AdoptionConversationSelected=>AdoptionConversation?.event_id;
        public bool AdoptionResultPending=>AdoptionConversations.Any(CafeAdoptionDialogue.ResultPending);
        public string AdoptionConversationReason=>AdoptionResultPending?"譲渡申し出の回答は確定済みですが、結果会話が未完了です。イベント一覧の「譲渡の申し出」から再開してください。":Simulation.AdoptionWaiting?"譲渡申し出が未回答です。イベント一覧の「譲渡の申し出」から再開し、譲渡する／見送るを選んでください。":AdoptionConversationOpen?"譲渡の会話中です。イベント一覧から再開できます。":"";
        void SyncAdoptionConversations(){foreach(var r in adoptionConversations)CafeAdoptionDialogue.Sync(r,Simulation.State.adoption?.events.FirstOrDefault(e=>e.id==r.event_id));}
        public bool BeginAdoptionConversation(string id)
        {
            SyncConversations();var e=Simulation.State.adoption?.events.FirstOrDefault(x=>x.id==id);if(e==null)return false;
            var r=adoptionConversations.FirstOrDefault(x=>x.event_id==id);
            if(r==null){if(e.status!="waiting")return false;r=new CafeAdoptionConversation{event_id=id,progress=CafeAdoptionDialogue.Start()};adoptionConversations.Add(r);}
            if(r.progress.completed)return false;adoptionSelected=id;return true;
        }
        public void PrepareAdoptionConversation()
        {
            var rows=AdoptionConversations;if(AdoptionConversation?.progress.completed==false)return;
            var pending=rows.FirstOrDefault(CafeAdoptionDialogue.ResultPending);if(pending!=null){adoptionSelected=pending.event_id;return;}
            foreach(var e in Simulation.State.adoption?.events??new List<CafeAdoptionEvent>())if(BeginAdoptionConversation(e.id))return;
        }
        void RestoreAdoptionConversations(CafeAdoptionConversation[] rows,string selected){adoptionConversations=(rows??new CafeAdoptionConversation[0]).ToList();adoptionSelected=selected;AdoptionConversationOpen=false;}
    }
}
