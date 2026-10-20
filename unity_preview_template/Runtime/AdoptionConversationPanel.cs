using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class AdoptionConversationPanel : AcquisitionConversationPanel
    {
        GameObject host;PythonAdoptionPanel owner;
        CafeAdoptionConversation Row=>Backend.AdoptionConversation;
        CafeAdoptionEvent Event=>Backend.Simulation.State.adoption?.events.FirstOrDefault(e=>e.id==Row?.event_id);
        public GameObject Panel=>host;
        protected override bool Ready=>base.Ready && !owner.Confirming;
        public void Apply(PythonCafeState value,PythonAdoptionPanel original)
        {
            owner=original;if(host==null){host=new GameObject("Adoption Conversation",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));host.transform.SetParent(owner.panel.transform.parent,false);host.SetActive(false);}
            Bind(value,host,owner.settings.font);
            if(Showing && !owner.Confirming)owner.HideForConversation();
            if(owner.Confirming)owner.panel.transform.SetAsLastSibling();
        }
        public bool OpenEvent(string id){if(!Backend.BeginAdoptionConversation(id))return false;Apply(Client.CurrentState,GetComponent<PythonAdoptionPanel>());Open();return true;}
        protected override CafeConversationProgress ResolveProgress(){Backend.PrepareAdoptionConversation();return Row?.progress;}
        protected override bool ThreeLinePages=>true;
        protected override void SetOpen(bool open){Backend.AdoptionConversationOpen=open;}
        protected override CafeConversationLine[] Lines(string branch)=>CafeAdoptionDialogue.Lines(branch,Backend.Simulation.Definition(Event.cat_id).name,Backend.Details.CustomerName(Event.customer_id),Event);
        protected override PythonIntakeState Request=>new PythonIntakeState{cat_id=Backend.Details.ContentId(Event.cat_id),name=Backend.Simulation.Definition(Event.cat_id).name,personality=Backend.Details.CustomerName(Event.customer_id)+"からの申し出",can_decline=true};
        protected override bool CanAnswer(bool yes)=>Backend.Simulation.AdoptionResponseReason(Event.id)=="";
        protected override string Title=>"猫の譲渡申し出";
        protected override string SceneLabel=>Event.day+"日目の申し出 · "+Backend.Details.CustomerName(Event.customer_id)+" / "+(Event.status=="waiting"?"回答待ち":Event.choice=="accept"?"譲渡済み":"見送り済み");
        protected override string ChoicePrompt=>"送り出すことも、店で一緒に過ごすことも尊重します。\n譲渡する／見送るを選んでください。";
        protected override string AcceptCaption=>"譲渡する（最終確認へ）";
        protected override string DeclineCaption=>"今回は見送る";
        protected override string AcceptEffect=>(CanAnswer(true)?"選択可能":"選べない理由："+Backend.Simulation.AdoptionResponseReason(Event.id))+"\n"+CafeAdoptionDialogue.Consequences;
        protected override string DeclineEffect=>"譲渡せず、店での活動を続けます。\n費用・報酬なし。翌日以降は既存の発生条件で再び申し出が届くことがあります。";
        protected override string HistoryPrefix=>progress.branch_id=="introduction"?"":string.Join("\n\n",Lines("introduction").Select(l=>(string.IsNullOrEmpty(l.speaker)?"地の文":l.speaker)+"\n"+l.text))+"\n\n";
        protected override string Notice=>Backend.AdoptionConversationReason;
        protected override string EndText=>"回答は記録済みです。会話を終えて、残るイベントや営業の予定を確認しましょう。";
        protected override void SendAnswer(bool yes){if(yes)owner.BeginConversationAccept(Event.id);else Client.ResolveAdoption(Event.id,"decline");}
        protected override void Finish(){CafeAdoptionDialogue.Finish(Row);}
        public override void Close(){if(owner.Confirming)return;base.Close();}
        public override void Open(){base.Open();if(Showing)owner.HideForConversation();}
    }
}
