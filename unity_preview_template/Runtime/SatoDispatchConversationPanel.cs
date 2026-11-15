using System.Linq;
using CatCafePreview.Standalone;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public sealed class SatoDispatchConversationPanel : AcquisitionConversationPanel
    {
        private GameObject host;
        private string catId, instance, customer;
        private string CustomerName=>Backend.Details.CustomerName(customer);
        private bool request;
        private CafeSimulation Simulation=>Backend.Simulation;
        private string CatName=>Simulation.Definition(catId).name;
        private string AnswerReason(bool accept)=>Simulation.SatoDispatchResponseReason(accept,customer);

        public void OpenEvent(string id,bool isRequest,string customerId)
        {
            catId=id;
            customer=customerId;
            request=isRequest;
            instance=Client.CurrentState.instance_id;
            if(host==null)
            {
                var owner=GetComponent<PythonCatDetailsPanel>();
                host=new GameObject("Sato Dispatch Event",typeof(RectTransform),typeof(Image));
                host.transform.SetParent(owner.panel.transform.parent,false);
                host.SetActive(false);
            }
            Bind(Client.CurrentState,host,GetComponent<PythonCatDetailsPanel>().title.font);
            Open();
        }

        protected override void Update()
        {
            if(Showing && (Client.CurrentState?.instance_id!=instance || Backend==null || request &&
                (Simulation.NamedRequest(customer)?.accepted_day>0 || Simulation.NamedRequest(customer)?.declined_day>0)))
            {
                Close();
                return;
            }
            base.Update();
        }

        protected override CafeConversationProgress ResolveProgress()=>request
            ? Simulation.NamedRequest(customer)?.conversation
            : Simulation.NamedAffinity(customer).FirstOrDefault(e=>e.cat_id==catId)?.conversation;
        protected override bool ThreeLinePages=>true;
        protected override void SetOpen(bool open){if(Backend!=null)Backend.SatoConversationOpen=open;}
        protected override bool CanAnswer(bool answer)=>Ready && request && AnswerReason(answer)=="";
        protected override void SendAnswer(bool answer){if(CanAnswer(answer))Client.RespondSatoDispatch(answer,customer);}
        protected override void Finish()=>CafeMissingDialogue.Finish(progress);
        protected override void RecordCompletion(){}
        protected override Texture2D PortraitTexture(string id)=>HumanPortraitResources.Resolve(customer);
        protected override PythonIntakeState Request=>new PythonIntakeState{cat_id=catId,name=CatName,can_decline=true};
        private bool Reminder=>request && Simulation.State.day>Simulation.NamedRequest(customer).requested_day;
        protected override string SceneLabel=>Simulation.State.day+"日目 · "+(Simulation.State.phase=="closed"?"閉店後":Simulation.State.phase=="open"?"営業中":"営業準備");
        protected override string Title=>CustomerName+(request?(Reminder?"からの催促":"からの指名依頼"):customer==CafeSimulation.Suzuki?"とねこじゃらし":customer==CafeSimulation.Sato?"といつもの席":"との交流");
        protected override string ChoicePrompt=>(Simulation.NamedRequest(customer).requested_day+1)+"日目の営業準備が回答最終日です。";
        protected override string AcceptCaption=>"派遣する";
        protected override string DeclineCaption=>"辞退する";
        protected override string AcceptEffect=>AnswerReason(true)!=""?AnswerReason(true):
            Simulation.DiscontentScore(customer,Simulation.State.day)>=Simulation.Rules.discontent.threshold
                ?"文面には、予定どおり迎えに行っても返してもらえない懸念があります。":"1日の派遣。帰還時にお客さんの不満が下がります。";
        protected override string DeclineEffect=>AnswerReason(false)!=""?AnswerReason(false):"依頼を終了します。";
        protected override string HistoryPrefix=>"";
        protected override string Notice=>request?"閉じても営業できます。期限を過ぎると回答できません。":"イベント一覧で確認できる猫とお客さんの交流です。";

        protected override CafeConversationLine[] Lines(string branch)
            => CafeNamedDispatchDialogue.Request(Simulation.Rules.named_dispatch,customer,CatName,CustomerName,request,Reminder,
                request && Simulation.DiscontentScore(customer,Simulation.State.day)>=Simulation.Rules.discontent.threshold);
    }
}
