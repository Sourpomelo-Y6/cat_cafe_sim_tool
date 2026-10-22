using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class DispatchIntroductionConversationPanel : AcquisitionConversationPanel
    {
        GameObject host;PythonDispatchIntroductionPanel owner;Button housing;
        CafeDispatchIntroductionConversation Row=>Backend.DispatchIntroductionConversation;
        PythonDispatchIntroduction Candidate=>state?.dispatch_introductions?.FirstOrDefault(e=>e.event_id==Row?.event_id);
        public GameObject Panel=>host;
        public Button HousingButton=>housing;
        public void Apply(PythonCafeState value,PythonDispatchIntroductionPanel original)
        {
            owner=original;if(host==null){host=new GameObject("Shopping Street Cat Introduction",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));host.transform.SetParent(owner.panel.transform.parent,false);host.SetActive(false);}
            Bind(value,host,owner.title.font);
            if(housing==null && Row!=null)
            {
                var go=new GameObject("飼育枠を確認",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));go.transform.SetParent(host.transform,false);housing=go.GetComponent<Button>();go.GetComponent<Image>().color=new Color(.88f,.91f,.82f);
                var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=Vector2.one;rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-48,-42);rect.sizeDelta=new Vector2(220,42);
                var text=new GameObject("Label",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));text.transform.SetParent(go.transform,false);var t=text.GetComponent<Text>();t.text="飼育枠を確認";t.font=owner.title.font;t.fontSize=18;t.color=new Color(.22f,.26f,.20f);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;var r=t.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
                housing.onClick.AddListener(()=>{if(Showing && Client.CanManageSaves && progress.branch_id=="introduction")GetComponent<PythonCatAcquisitionNavigation>().OpenHousing(host);});
            }
            UpdateHousing();if(Showing)owner.HideForConversation();
        }
        void UpdateHousing(){if(housing!=null){housing.gameObject.SetActive(Showing && progress.branch_id=="introduction");housing.interactable=Client.CanManageSaves;}}
        protected override void Update(){base.Update();UpdateHousing();}
        protected override bool Available=>base.Available && !Backend.DispatchResultPending && !Backend.Simulation.DispatchWaiting;
        protected override bool CanRaise=>GetComponent<PythonCatAcquisitionNavigation>()?.HousingFrom(host)!=true;
        public bool OpenEvent(string id){if(!Backend.BeginDispatchIntroductionConversation(id))return false;Apply(Client.CurrentState,GetComponent<PythonDispatchIntroductionPanel>());Open();return true;}
        protected override CafeConversationProgress ResolveProgress(){Backend.PrepareDispatchIntroductionConversation();return Row?.progress;}
        protected override void SetOpen(bool open){Backend.DispatchIntroductionConversationOpen=open;}
        protected override CafeConversationLine[] Lines(string branch)=>CafeDispatchIntroductionDialogue.Lines(branch,Candidate.name,Candidate.personality,Candidate.features,Candidate.trait,Destination);
        protected override PythonIntakeState Request=>new PythonIntakeState{cat_id=Candidate.content_cat_id,name=Candidate.name,personality=Candidate.personality+" · "+Candidate.features,cost=Candidate.cost,housing=Candidate.housing,can_accept=Candidate.can_accept,can_decline=Candidate.can_decline,response_reason=Candidate.response_reason,accept_reason=Candidate.accept_reason};
        protected override bool CanAnswer(bool yes)=>Client.CanRespondDispatchIntroduction(Row.event_id,yes?"accept":"decline");
        string Destination=>Backend.Simulation.State.dispatches.First(e=>e.id==Row.event_id).destination;
        protected override bool ThreeLinePages=>Destination==CafeDispatchIntroductionDialogue.StudioDestination;
        protected override string AcceptEffect=>(Candidate.can_accept?"選択可能":"選べない理由："+(string.IsNullOrEmpty(Candidate.response_reason)?Candidate.accept_reason:Candidate.response_reason))+"\n初期費用："+Candidate.cost+" / 資金："+state.funds+" → "+Candidate.funds_after+"\n飼育枠："+Candidate.housing+"\n健康・体力全回復、休養予定で加入。";
        protected override string Title=>CafeDispatchIntroductionDialogue.Title(Destination);
        protected override string SceneLabel=>state.day+"日目 · "+(progress.branch_id=="introduction"?"帰還後の相談":progress.branch_id=="accepted"?"受け入れ済み":"見送り済み");
        protected override string DeclineEffect=>(Candidate.can_decline?"選択可能":"選べない理由："+Candidate.response_reason)+"\n費用・人気・派遣報酬の変化なし。\nこの候補への回答は変更できません。";
        protected override void SendAnswer(bool yes){if(yes)GetComponent<PythonCatAcquisitionNavigation>()?.Watch(Candidate.cat_id);Client.RespondDispatchIntroduction(Row.event_id,yes?"accept":"decline");}
        protected override void Finish(){CafeDispatchIntroductionDialogue.Finish(progress);}
        public override void Open(){base.Open();UpdateHousing();if(Showing)owner.HideForConversation();}
    }
}
