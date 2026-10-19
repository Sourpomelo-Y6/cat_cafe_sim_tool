using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class OpeningPanel : AcquisitionConversationPanel
    {
        GameObject host;PythonPreparationPanel owner;
        public GameObject Panel=>host;
        CafeOpeningProgress Row=>Backend.Opening;
        public void Apply(PythonCafeState value,PythonPreparationPanel preparation)
        {
            owner=preparation;if(owner==null)return;
            if(!Backend.OpeningPending){if(host!=null)host.SetActive(false);return;}
            if(host==null){host=new GameObject("Opening",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));host.transform.SetParent(owner.panel.transform.parent,false);host.SetActive(false);}
            Bind(value,host,owner.title.font);owner.panel.SetActive(false);
        }
        protected override CafeConversationProgress ResolveProgress()=>Row?.progress;
        protected override bool Choosing=>false;
        protected override bool ThreeLinePages=>true;
        protected override bool AutoRunning=>false;
        protected override string CloseCaption=>"スキップ";
        protected override string Title=>"オープニング · 猫カフェのはじまり";
        protected override string SceneLabel=>"1日目 · 営業前 ／ 初回からスキップできます";
        protected override CafeConversationLine[] Lines(string branch)=>CafeOpening.Lines(Row);
        int CatIndex=>Row.cat_ids.Length==0?0:Mathf.Clamp((Row.progress.node-2)/2,0,Row.cat_ids.Length-1);
        string CatId=>Row.cat_ids.Length==0?"":Row.cat_ids[CatIndex];
        protected override PythonIntakeState Request=>new PythonIntakeState{cat_id=CatId==""?"":Backend.Details.ContentId(CatId),name=Row.cat_names.Length==0?"猫たちのカフェ":Row.cat_names[CatIndex],personality="1日目の営業前",can_decline=true};
        protected override void SetOpen(bool open){}
        protected override bool CanAnswer(bool answer)=>false;
        protected override void SendAnswer(bool answer){}
        protected override string AcceptEffect=>"";
        protected override string DeclineEffect=>"";
        protected override string HistoryPrefix=>"";
        protected override string EndText=>"営業準備へ進みましょう。\n出勤と休養を確認して、営業開始または休業を選べます。";
        protected override void Finish()=>Backend.FinishOpening(false);
        public override void Close(){if(!Ready)return;if(Backend.OpeningPending)Backend.FinishOpening(true);base.Close();}
        protected override void AfterClose(){owner.Apply(Client.CurrentState);owner.panel.SetActive(true);GetComponent<ContentPreview>().RefreshPythonControls();}
        void LateUpdate(){if(Showing && !GameOptionsPanel.IsOpen && GetComponent<PythonSavePanel>()?.panel.activeSelf!=true)host.transform.SetAsLastSibling();}
    }
}
