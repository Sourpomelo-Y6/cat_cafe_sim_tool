using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class PopularityEndingPanel : AcquisitionConversationPanel
    {
        GameObject host,outro;PythonGoalPanel owner;bool offered;Button continueButton,titleButton,saveButton;
        public GameObject Panel=>host;public GameObject Outro=>outro;
        bool bond;
        CafeEndingProgress Row=>bond?Backend.BondEnding:Backend.PopularityEnding;
        CafeClearRecord Record=>bond?CafeBondEnding.Record(Backend.Simulation.State):CafePopularityEnding.Record(Backend.Simulation.State);
        bool Waiting=>bond?CafeBondEnding.Waiting(Backend.Simulation.State):CafePopularityEnding.Waiting(Backend.Simulation.State);
        public void Apply(PythonCafeState value,PythonGoalPanel original)
        {
            owner=original;bool nextBond=value.goal_result?.kind=="continue_bond_goal";if(nextBond!=bond){offered=false;if(host!=null)host.SetActive(false);if(outro!=null)outro.SetActive(false);}bond=nextBond;
            if(host==null){host=new GameObject("Popularity Ending",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));host.transform.SetParent(owner.panel.transform.parent,false);host.SetActive(false);}
            if(Row==null || !Waiting){host.SetActive(false);if(outro!=null)outro.SetActive(false);offered=false;return;}
            if(value.growth_choices?.Length>0 || value.required_action!=(bond?"bond_result":"goal_result") || GetComponents<AcquisitionConversationPanel>().Any(p=>p!=this && p.Showing)){host.SetActive(false);if(outro!=null)outro.SetActive(false);return;}
            bool first=!offered;offered=true;Bind(value,host,owner.title.font);if(first && !Row.progress.completed)Open();
            if(Row.progress.completed)ShowOutro();else if(Showing)owner.panel.SetActive(false);
        }
        protected override CafeConversationProgress ResolveProgress()=>Row?.progress;
        protected override bool Choosing=>false;
        protected override bool ThreeLinePages=>true;
        protected override void SetOpen(bool open){}
        protected override string Title=>bond?"好感度目標エンディング · 猫たちとのつながり":"人気目標エンディング · 猫カフェのこれから";
        protected override string SceneLabel=>"達成 "+Record.day+(bond?"日目 · 好感度目標達成":"日目 · 最終段階");
        protected override CafeConversationLine[] Lines(string branch){string name=string.IsNullOrEmpty(Row.cat_id)?"":Backend.Simulation.Definition(Row.cat_id).name;return bond?CafeBondEnding.Lines(Record,name):CafePopularityEnding.Lines(Record,name);}
        protected override PythonIntakeState Request=>new PythonIntakeState{cat_id=string.IsNullOrEmpty(Row.cat_id)?"":Backend.Details.ContentId(Row.cat_id),name=string.IsNullOrEmpty(Row.cat_id)?"猫たちのカフェ":Backend.Simulation.Definition(Row.cat_id).name,personality="達成時のカフェ",can_decline=true};
        protected override bool CanAnswer(bool answer)=>false;
        protected override void SendAnswer(bool answer){}
        protected override string AcceptEffect=>"";protected override string DeclineEffect=>"";protected override string HistoryPrefix=>"";
        protected override string Notice=>"達成結果は記録済みです。閉じた場合は結果画面から再開できます。営業は手動で再開してください。";
        protected override string EndText=>"目標達成、おめでとうございます。\n会話を終えると、継続営業かタイトルへ戻る操作を選べます。";
        protected override void Finish(){CafePopularityEnding.Finish(progress);}
        protected override void AfterClose(){if(Row?.progress.completed==true)ShowOutro();else owner.Apply(Client.CurrentState);GetComponent<ContentPreview>().RefreshPythonControls();}
        public override void Open(){base.Open();if(Showing)owner.panel.SetActive(false);}
        void ShowOutro()
        {
            if(outro==null)
            {
                outro=new GameObject("Ending Complete",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));outro.transform.SetParent(host.transform.parent,false);outro.GetComponent<Image>().color=new Color(.99f,.97f,.92f);var r=outro.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
                var heading=Label("Ending Heading","猫カフェのこれから",60,60,1100,60,32);Label("Thanks","人気目標の最終段階を達成しました。\n猫や資金を引き継いで、これからの営業を続けられます。",60,145,1100,100,23);
                Label("Record",string.Join("\n",Lines("").Where(l=>l.speaker=="営業の記録").Select(l=>l.text)),60,280,1100,220,22);
                Label("Save Hint","進捗を残す場合は、タイトルへ戻る前に保存してください。\n営業・自動進行は手動で再開します。",60,540,1100,70,18);
                saveButton=Button("保存",60,660,220,()=>GetComponent<PythonSaveBrowser>().OpenSave());continueButton=Button("継続営業",320,660,280,()=>Client.ContinueGoal());titleButton=Button("タイトルへ",640,660,280,()=>GetComponent<PythonReturnToTitle>().Return());
            }
            outro.transform.Find("Ending Heading").GetComponent<Text>().text=bond?"猫たちとのつながり":"猫カフェのこれから";outro.transform.Find("Thanks").GetComponent<Text>().text=(bond?"好感度目標を達成しました。":"人気目標の最終段階を達成しました。")+"\n猫や資金を引き継いで、これからの営業を続けられます。";outro.transform.Find("Record").GetComponent<Text>().text=string.Join("\n",Lines("").Where(l=>l.speaker=="営業の記録").Select(l=>l.text));owner.panel.SetActive(false);outro.SetActive(true);Client.PauseAutomaticForSave();
        }
        protected override void Update(){base.Update();if(outro!=null && outro.activeSelf){continueButton.interactable=Client.CanContinueGoal;saveButton.interactable=Client.CanSaveNow;titleButton.interactable=Client.CanLeaveResult;}}
        void LateUpdate(){if(GameOptionsPanel.IsOpen || GetComponent<PythonSavePanel>()?.panel.activeSelf==true)return;if(outro!=null && outro.activeSelf)outro.transform.SetAsLastSibling();else if(Showing)host.transform.SetAsLastSibling();}
        Text Label(string name,string caption,float x,float y,float w,float h,int size){var g=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));g.transform.SetParent(outro.transform,false);var t=g.GetComponent<Text>();t.font=owner.title.font;t.text=caption;t.fontSize=size;t.color=new Color(.22f,.26f,.20f);t.raycastTarget=false;var r=t.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return t;}
        Button Button(string caption,float x,float y,float w,UnityEngine.Events.UnityAction action){var t=Label(caption,caption,x,y,w,50,21);t.alignment=TextAnchor.MiddleCenter;var g=new GameObject(caption+" Button",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));g.transform.SetParent(outro.transform,false);var r=g.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,50);g.GetComponent<Image>().color=new Color(.87f,.91f,.82f);t.transform.SetParent(g.transform,false);t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=t.rectTransform.offsetMax=Vector2.zero;var b=g.GetComponent<Button>();b.onClick.AddListener(action);return b;}
    }
}
