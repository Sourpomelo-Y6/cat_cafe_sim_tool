using System.Linq;
using CatCafePreview.Standalone;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public sealed class SatoDispatchConversationPanel : AcquisitionConversationPanel
    {
        private GameObject host;
        private string catId, instance;
        private bool request;
        private CafeSimulation Simulation=>Backend.Simulation;
        private string CatName=>Simulation.Definition(catId).name;
        private string AnswerReason(bool accept)=>Simulation.SatoDispatchResponseReason(accept);

        public void OpenEvent(string id,bool isRequest)
        {
            catId=id;
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
                (Simulation.State.sato_dispatch?.accepted_day>0 || Simulation.State.sato_dispatch?.declined_day>0)))
            {
                Close();
                return;
            }
            base.Update();
        }

        protected override CafeConversationProgress ResolveProgress()=>request
            ? Simulation.State.sato_dispatch?.conversation
            : Simulation.State.sato_affinity.FirstOrDefault(e=>e.cat_id==catId)?.conversation;
        protected override bool ThreeLinePages=>true;
        protected override void SetOpen(bool open){if(Backend!=null)Backend.SatoConversationOpen=open;}
        protected override bool CanAnswer(bool answer)=>Ready && request && AnswerReason(answer)=="";
        protected override void SendAnswer(bool answer){if(CanAnswer(answer))Client.RespondSatoDispatch(answer);}
        protected override void Finish()=>CafeMissingDialogue.Finish(progress);
        protected override void RecordCompletion(){}
        protected override Texture2D PortraitTexture(string id)=>HumanPortraitResources.Resolve(CafeSimulation.Sato);
        protected override PythonIntakeState Request=>new PythonIntakeState{cat_id=catId,name=CatName,can_decline=true};
        protected override string Title=>request?"佐藤さんからの指名依頼":"佐藤さんといつもの席";
        protected override string ChoicePrompt=>"回答期限は "+(Simulation.State.sato_dispatch.requested_day+2)+"日目の営業準備までです。";
        protected override string AcceptCaption=>"派遣する";
        protected override string DeclineCaption=>"辞退する";
        protected override string AcceptEffect=>AnswerReason(true)!=""?AnswerReason(true):
            Simulation.DiscontentScore(CafeSimulation.Sato,Simulation.State.day)>=Simulation.Rules.discontent.threshold
                ?"文面には、予定どおり迎えに行っても返してもらえない懸念があります。":"1日の派遣。帰還時に佐藤さんの不満が下がります。";
        protected override string DeclineEffect=>AnswerReason(false)!=""?AnswerReason(false):"依頼を終了します。";
        protected override string HistoryPrefix=>"";
        protected override string Notice=>request?"閉じても営業できます。期限を過ぎると回答できません。":"イベント一覧で確認できる猫とお客さんの交流です。";

        protected override CafeConversationLine[] Lines(string branch)
        {
            if(!request)return new[]{
                new CafeConversationLine("", "佐藤さんの声が聞こえると、"+CatName+"は自分から近くへ歩いていった。"),
                new CafeConversationLine("佐藤さん", "今日も隣に来てくれたのね。ここ、空けておいたよ。")
            };
            bool reminder=Simulation.State.day>Simulation.State.sato_dispatch.requested_day;
            bool severe=Simulation.DiscontentScore(CafeSimulation.Sato,Simulation.State.day)>=Simulation.Rules.discontent.threshold;
            return new[]{
                new CafeConversationLine("佐藤さん",reminder?CatName+"のこと、昨日のお返事をいただけますか。":"お店で待っても会えない日が続きました。"+CatName+"を、うちに一日だけ呼べませんか。"),
                new CafeConversationLine("佐藤さん",severe?"一日で迎えに来られても、あの子が落ち着いていたら、そのままうちにいてほしいんです。":"あの子が落ち着けるように、窓辺に場所を空けておきます。")
            };
        }
    }
}
