using System;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class ReplayConversationPanel : AcquisitionConversationPanel
    {
        Texture2D cachedPortrait;bool portraitLoaded;
        CafeReplayEntry entry;CafeConversationProgress position;Action closed;GameObject host;
        public GameObject Panel=>host;
        public void Play(CafeReplayEntry value,GameObject surface,Font font,Action onClose)
        {
            portraitLoaded=false;cachedPortrait=null;entry=value;host=surface;closed=onClose;position=new CafeConversationProgress{conversation_id=value.conversation_id,branch_id="replay",viewed_branches=new string[0]};
            Bind(new PythonCafeState{instance_id=Guid.NewGuid().ToString("N")},surface,font);Open();
        }
        protected override bool Ready=>true;
        protected override bool AutoRunning=>false;
        protected override bool SaveControls=>false;
        protected override string EndText=>"回想を終えて、Extraの一覧へ戻りましょう。";
        protected override string BusyNotice=>Notice;
        protected override string Notice=>"回想中です。資金・加入・日付・営業セーブは変わりません。";
        protected override string Title=>entry.title;
        protected override string SceneLabel=>"Extra · 閲覧済みの会話";
        protected override bool Choosing=>false;
        protected override bool ThreeLinePages=>CafeReplayCatalog.IsEnding(entry?.conversation_id) || entry?.conversation_id==CafeOpening.Id;
        protected override CafeConversationProgress ResolveProgress()=>position;
        protected override CafeConversationLine[] Lines(string branch)=>entry.lines;
        protected override PythonIntakeState Request=>new PythonIntakeState{cat_id=entry.cat_id,name=entry.cat_name,personality=entry.personality,can_decline=true};
        protected override bool CanAnswer(bool yes)=>false;
        protected override string AcceptEffect=>"";
        protected override string DeclineEffect=>"";
        protected override string HistoryPrefix=>"";
        protected override void SendAnswer(bool yes){}
        protected override void SetOpen(bool open){}
        protected override void PauseProgress(){}
        protected override void RecordCompletion(){}
        protected override void Finish(){position.completed=true;}
        protected override void AfterClose(){closed?.Invoke();}
        protected override Texture2D PortraitTexture(string id)
        {
            if(!portraitLoaded){portraitLoaded=true;cachedPortrait=LoadPortrait(id);}
            return cachedPortrait;
        }
        Texture2D LoadPortrait(string id)
        {
            try
            {
                var catalog=JsonUtility.FromJson<ContentCatalog>(Resources.Load<TextAsset>("CatCafeContent/catalog").text);
                var bundle=JsonUtility.FromJson<ContentBundle>(Resources.Load<TextAsset>(catalog.bundleResource).text);
                var cat=Array.Find(bundle.cats,c=>c.cat_id==id);var scene=cat==null?null:Array.Find(cat.scenes,s=>s.key=="normal");
                if(scene==null || string.IsNullOrEmpty(scene.image))return ContentPreview.CatPlaceholderTexture();
                return Resources.Load<Texture2D>(catalog.bundleResource.Substring(0,catalog.bundleResource.Length-"bundle".Length)+scene.image.Substring(0,scene.image.Length-4));
            }
            catch(Exception){return ContentPreview.CatPlaceholderTexture();}
        }
    }
}
