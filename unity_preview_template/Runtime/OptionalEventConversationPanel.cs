using System;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;

namespace CatCafePreview
{
    public sealed class OptionalEventConversationPanel : AcquisitionConversationPanel
    {
        GameObject host;
        CafeOptionalEvent selected;
        bool requested;
        CafeOptionalEvent Row=>Backend.OptionalEvent;
        public void Apply(PythonCafeState value)
        {
            if(!ReferenceEquals(selected,Row))requested=false;
            if(host==null)
            {
                var owner=GetComponent<PythonCatDetailsPanel>();
                host=new GameObject("Optional Cat Event",typeof(RectTransform),typeof(Image));
                host.transform.SetParent(owner.panel.transform.parent,false);host.SetActive(false);
            }
            Bind(value,host,GetComponent<PythonCatDetailsPanel>().title.font);
        }
        public bool OpenEvent()
        {
            if(!Client.CanManageSaves || Client.AutoPlayExecuting || GetComponents<AcquisitionConversationPanel>().AnyShowing(this))return false;
            var value=Client.CurrentState;
            if(!Backend.BeginOptionalEvent(value.instance_id,value.revision,NormalImage()))return false;
            selected=Row;requested=true;Apply(value);Open();return Showing;
        }
        string NormalImage()
        {
            try {
            var catalog=JsonUtility.FromJson<ContentCatalog>(Resources.Load<TextAsset>("CatCafeContent/catalog").text);
            var bundle=JsonUtility.FromJson<ContentBundle>(Resources.Load<TextAsset>(catalog.bundleResource).text);
            var cat=Array.Find(bundle.cats,c=>c.cat_id==Backend.Details.ContentId("cat-mike"));
            var image=cat==null?null:Array.Find(cat.scenes??new SceneContent[0],s=>s.key=="normal")?.image;
            if(string.IsNullOrEmpty(image) || !image.EndsWith(".png",StringComparison.OrdinalIgnoreCase))return "";
            return catalog.bundleResource.Substring(0,catalog.bundleResource.Length-"bundle".Length)+image.Substring(0,image.Length-4);
            } catch(Exception) {return "";}
        }
        protected override CafeConversationProgress ResolveProgress()=>Row?.progress;
        protected override bool Available=>requested && base.Available;
        protected override bool AutoRunning=>false;
        protected override bool ThreeLinePages=>true;
        protected override void SetOpen(bool open){Backend.OptionalEventOpen=open;}
        protected override CafeConversationLine[] Lines(string branch)=>CafeOptionalEvents.Lines(Row,branch);
        protected override PythonIntakeState Request=>new PythonIntakeState{cat_id=Row.content_cat_id,name=Row.cat_name,personality="開始時の記録",can_decline=true};
        protected override Texture2D PortraitTexture(string id)=>string.IsNullOrEmpty(Row.image_resource)?null:Resources.Load<Texture2D>(Row.image_resource);
        protected override string Title=>Row.title;
        protected override string SceneLabel=>Row.started_day+"日目 · 開始時の記録";
        protected override string ChoicePrompt=>"ミケと、どのように過ごしますか。";
        protected override string AcceptCaption=>Row.accept_caption;
        protected override string DeclineCaption=>Row.decline_caption;
        protected override string AcceptEffect=>"数値の変化はありません。";
        protected override string DeclineEffect=>"数値の変化はありません。";
        protected override string Notice=>"閉じても会話の位置を保持します。通常操作・おまかせを続けられます。";
        protected override string EndText=>"ミケと過ごしたひとときを記録しました。";
        protected override string HistoryPrefix=>progress.branch_id=="introduction"?"":string.Join("\n\n",Array.ConvertAll(Row.introduction,l=>l.text))+"\n\n【"+(progress.branch_id=="accepted"?Row.accept_caption:Row.decline_caption)+"】\n\n";
        protected override bool CanAnswer(bool yes)=>Ready && progress.branch_id=="introduction";
        protected override void SendAnswer(bool yes){CafeOptionalEvents.Choose(Row,yes);}
        protected override void Finish()=>CafeOptionalEvents.Finish(Row);
        protected override bool ReadSkippable(int node)=>true;
        public override void Close(){base.Close();if(!Showing)requested=false;}
    }

    static class OptionalEventPanelExtensions
    {
        public static bool AnyShowing(this AcquisitionConversationPanel[] panels,AcquisitionConversationPanel except)
        {
            foreach(var panel in panels)if(panel!=except && panel.Showing)return true;return false;
        }
    }
}
