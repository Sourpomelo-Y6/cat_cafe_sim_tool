using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public sealed class GameOptionsPanel : MonoBehaviour
    {
        public static bool IsOpen=>active!=null && active.Panel!=null && active.Panel.activeInHierarchy;
        static GameOptionsPanel active;
        public GameObject Panel {get;private set;}
        public Button Entry {get;private set;}public Slider Speed {get;private set;}public Toggle Instant {get;private set;}public Toggle Fullscreen {get;private set;}
        public Text Preview {get;private set;}public Text Status {get;private set;}
        PythonStateClient client;PythonTitleScreen title;Font font;Text speedLabel;CafeOptionsData draft;float previewCount;bool refreshing;
        const string Sample="窓辺にやわらかな光が差し込みます。\n猫はゆっくりと目を細め、あなたのほうを見ました。";
        public static GameOptionsPanel Attach(PythonTitleScreen owner)
        {
            var ui=owner.GetComponent<GameOptionsPanel>()??owner.gameObject.AddComponent<GameOptionsPanel>();if(ui.Panel!=null)return ui;ui.title=owner;ui.font=owner.newGame.GetComponentInChildren<Text>().font;ui.Build(owner.transform);
            ui.Entry=UnityEngine.Object.Instantiate(owner.newGame,owner.home.transform);ui.Entry.name="Options";ui.Entry.GetComponentInChildren<Text>().text="オプション";ui.Entry.GetComponent<RectTransform>().anchoredPosition=new Vector2(260,-280);ui.Entry.onClick=new Button.ButtonClickedEvent();ui.Entry.onClick.AddListener(ui.Open);return ui;
        }
        public static GameOptionsPanel Attach(ContentPreview owner)
        {
            var ui=owner.GetComponent<GameOptionsPanel>()??owner.gameObject.AddComponent<GameOptionsPanel>();if(ui.Panel!=null)return ui;ui.client=owner.GetComponent<PythonStateClient>();var menu=owner.GetComponent<PythonMenuPanel>();ui.font=menu.title.font;ui.Build(owner.GetComponentInChildren<Canvas>(true).transform);
            ui.Entry=ui.Button(menu.title.transform.parent,"Options","オプション",0,24,180,40,ui.Open);var r=ui.Entry.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(1,1);r.anchoredPosition=new Vector2(-24,-24);return ui;
        }
        public bool CanOpen=>!IsOpen && (title!=null?title.CanManageSaves:client!=null && client.CanSendCommands && !client.AutoPlayRunning && !client.BatchAutomatic && !client.BatchStopping);
        public void Open()
        {
            if(!CanOpen)return;client?.PauseAutomaticForSave();draft=CafeOptions.Current.Copy();active=this;Panel.SetActive(true);Panel.transform.SetAsLastSibling();Refresh();Status.text=CafeOptions.Store?.Notice??"設定は全セーブ共通です。保存すると反映します。";
        }
        public void Cancel(){if(!Panel.activeSelf)return;Panel.SetActive(false);active=null;client?.GetComponent<ContentPreview>().RefreshPythonControls();}
        public void Save(){if(!Panel.activeSelf)return;if(CafeOptions.Save(draft))Cancel();else Status.text=CafeOptions.Store.Notice;}
        public void Defaults(){draft=new CafeOptionsData();Refresh();Status.text="初期設定を選びました。保存すると反映します。";}
        void Refresh(){refreshing=true;Speed.SetValueWithoutNotify(draft.charactersPerSecond);Instant.SetIsOnWithoutNotify(draft.instant);Fullscreen.SetIsOnWithoutNotify(draft.fullscreen);refreshing=false;Changed();}
        void Changed(){if(refreshing || draft==null)return;draft.charactersPerSecond=(int)Speed.value;draft.instant=Instant.isOn;draft.fullscreen=Fullscreen.isOn;Speed.interactable=!draft.instant;speedLabel.text="文字表示速度　"+(draft.instant?"全文を即時表示":draft.charactersPerSecond+" 文字／秒");previewCount=0;Preview.text="";}
        void Update()
        {
            if(Entry!=null)Entry.interactable=CanOpen;
            if(!Panel.activeSelf)return;
            previewCount=draft.instant?Sample.Length:Math.Min(Sample.Length,previewCount+Time.unscaledDeltaTime*draft.charactersPerSecond);Preview.text=Sample.Substring(0,(int)previewCount);
            if(Input.GetKeyDown(KeyCode.Escape))Cancel();
        }
        void LateUpdate(){if(Panel!=null && Panel.activeSelf)Panel.transform.SetAsLastSibling();}
        void OnDestroy(){if(active==this)active=null;}
        void Build(Transform parent)
        {
            Panel=Box(parent,"Game Options");var r=Panel.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var card=Box(Panel.transform,"Card");r=card.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(960,720);r.anchoredPosition=Vector2.zero;
            Label(card.transform,"Heading","オプション",32,20,880,48,28);
            Label(card.transform,"Guide","会話の読みやすさと画面表示を調整できます。",32,72,880,32,18);
            speedLabel=Label(card.transform,"Speed Label","",32,124,880,32,20);
            var slider=DefaultControls.CreateSlider(new DefaultControls.Resources());slider.name="Text Speed";slider.transform.SetParent(card.transform,false);Place(slider.GetComponent<RectTransform>(),40,174,860,32);Speed=slider.GetComponent<Slider>();Speed.minValue=10;Speed.maxValue=80;Speed.wholeNumbers=true;slider.transform.Find("Background").GetComponent<Image>().color=new Color(.82f,.85f,.77f);Speed.fillRect.GetComponent<Image>().color=new Color(.43f,.54f,.34f);Speed.handleRect.GetComponent<Image>().color=new Color(.28f,.38f,.22f);Speed.onValueChanged.AddListener(_=>Changed());
            Instant=Toggle(card.transform,"Instant","会話を全文即時表示する",32,226);Fullscreen=Toggle(card.transform,"Fullscreen","全画面で表示する（OFF：ウィンドウ）",32,284);
            Label(card.transform,"Audio","BGM・効果音：現在の配布版は音声未対応です。",32,350,880,32,18);
            Label(card.transform,"Sample Label","文章プレビュー",32,404,880,32,18);Preview=Label(card.transform,"Sample","",32,448,880,88,22);Preview.lineSpacing=1.2f;
            Button(card.transform,"Preview Again","もう一度読む",32,550,200,40,()=>{previewCount=0;});
            Status=Label(card.transform,"Status","",32,602,880,40,16);
            Button(card.transform,"Defaults","初期設定",32,660,180,42,Defaults);Button(card.transform,"Cancel","変更を取り消す",236,660,250,42,Cancel);Button(card.transform,"Save","保存して戻る",640,660,280,42,Save);Panel.SetActive(false);
        }
        GameObject Box(Transform p,string n){var g=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));g.transform.SetParent(p,false);g.GetComponent<Image>().color=new Color(.99f,.97f,.92f);return g;}
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        Text Label(Transform p,string n,string caption,float x,float y,float w,float h,int size){var g=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));g.transform.SetParent(p,false);var t=g.GetComponent<Text>();t.font=font;t.fontSize=size;t.color=new Color(.22f,.26f,.20f);t.supportRichText=false;t.raycastTarget=false;t.text=caption;Place(t.rectTransform,x,y,w,h);return t;}
        Button Button(Transform p,string n,string caption,float x,float y,float w,float h,UnityEngine.Events.UnityAction action){var g=Box(p,n);g.GetComponent<Image>().color=new Color(.87f,.91f,.82f);Place(g.GetComponent<RectTransform>(),x,y,w,h);var b=g.AddComponent<Button>();b.targetGraphic=g.GetComponent<Image>();var t=Label(g.transform,"Caption",caption,0,0,w,h,19);t.alignment=TextAnchor.MiddleCenter;b.onClick.AddListener(action);return b;}
        Toggle Toggle(Transform p,string n,string caption,float x,float y){var g=DefaultControls.CreateToggle(new DefaultControls.Resources());g.name=n;g.transform.SetParent(p,false);Place(g.GetComponent<RectTransform>(),x,y,880,44);var text=g.GetComponentInChildren<Text>();text.font=font;text.fontSize=20;text.text=caption;text.color=new Color(.22f,.26f,.20f);var t=g.GetComponent<Toggle>();t.targetGraphic.color=new Color(.76f,.81f,.69f);t.graphic.color=new Color(.25f,.38f,.18f);t.onValueChanged.AddListener(_=>Changed());return t;}
    }
}
