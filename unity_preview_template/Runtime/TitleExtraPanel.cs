using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class TitleExtraPanel : MonoBehaviour
    {
        PythonTitleScreen title;GameObject surface,replaySurface;Font font;Text notice;ScrollRect list;
        public Button Entry {get;private set;}
        public GameObject Panel=>surface;
        public ReplayConversationPanel Replay {get;private set;}
        LocalCafeBackend Backend=>CafeBackends.Local as LocalCafeBackend;
        public void Initialize(PythonTitleScreen owner)
        {
            if(Entry!=null)return;title=owner;font=owner.newGame.GetComponentInChildren<Text>().font;
            Entry=UnityEngine.Object.Instantiate(owner.newGame,owner.home.transform);Entry.name="Extra";Entry.GetComponentInChildren<Text>().text="Extra · 会話の回想";Entry.GetComponent<RectTransform>().anchoredPosition=new Vector2(260,-215);Entry.onClick=new Button.ButtonClickedEvent();Entry.onClick.AddListener(Open);
            surface=Box(transform,"Extra Conversations");Stretch(surface.GetComponent<RectTransform>());Label(surface.transform,"Heading",48,32,1000,44,28).text="Extra · 会話の回想";
            notice=Label(surface.transform,"Notice",48,86,1184,68,17);
            var go=Box(surface.transform,"List");var rect=go.GetComponent<RectTransform>();rect.anchorMin=new Vector2(0,0);rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(48,104);rect.offsetMax=new Vector2(-48,-160);list=go.AddComponent<ScrollRect>();list.horizontal=false;list.scrollSensitivity=36;
            var viewport=Box(go.transform,"Viewport");Stretch(viewport.GetComponent<RectTransform>());viewport.AddComponent<RectMask2D>();list.viewport=viewport.GetComponent<RectTransform>();
            var scrollbar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());scrollbar.name="Scrollbar";scrollbar.transform.SetParent(go.transform,false);var sr=scrollbar.GetComponent<RectTransform>();sr.anchorMin=new Vector2(1,0);sr.anchorMax=Vector2.one;sr.offsetMin=new Vector2(-18,0);sr.offsetMax=Vector2.zero;var bar=scrollbar.GetComponent<Scrollbar>();scrollbar.GetComponent<Image>().color=new Color(.88f,.88f,.82f);bar.handleRect.GetComponent<Image>().color=new Color(.40f,.48f,.32f);bar.direction=Scrollbar.Direction.BottomToTop;list.verticalScrollbar=bar;list.viewport.offsetMax=new Vector2(-26,0);
            var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(viewport.transform,false);list.content=content.GetComponent<RectTransform>();list.content.anchorMin=new Vector2(0,1);list.content.anchorMax=Vector2.one;list.content.pivot=new Vector2(.5f,1);list.content.sizeDelta=new Vector2(0,CafeReplayCatalog.All.Length*76);
            for(int i=0;i<CafeReplayCatalog.All.Length;i++){var d=CafeReplayCatalog.All[i];var button=MakeButton(content.transform,d.Key.Replace('/','_'),d.title,0,i*76,1184,66);var r=button.GetComponent<RectTransform>();r.anchorMax=Vector2.one;r.anchorMin=new Vector2(0,1);r.offsetMin=new Vector2(0,-i*76-66);r.offsetMax=new Vector2(0,-i*76);button.onClick.AddListener(()=>OpenReplay(d.Key));}
            MakeButton(surface.transform,"タイトルへ戻る","タイトルへ戻る",48,0,240,48).GetComponent<RectTransform>().SetBottom(48,28,240,48);
            surface.transform.Find("タイトルへ戻る").GetComponent<Button>().onClick.AddListener(Hide);
            replaySurface=Box(transform,"Extra Replay");Stretch(replaySurface.GetComponent<RectTransform>());replaySurface.SetActive(false);Replay=gameObject.AddComponent<ReplayConversationPanel>();surface.SetActive(false);
        }
        void Update(){if(Entry!=null)Entry.interactable=CafeBackends.Standalone && title.CanManageSaves;}
        public void Open()
        {
            if(!CafeBackends.Standalone || !title.CanManageSaves)return;Backend.ImportViewedSaves();RefreshList();surface.SetActive(true);surface.transform.SetAsLastSibling();list.verticalNormalizedPosition=1;
        }
        public void RefreshList()
        {
            var profile=Backend.Replays.Load();foreach(var d in CafeReplayCatalog.All){var b=list.content.Find(d.Key.Replace('/','_')).GetComponent<Button>();bool unlocked=profile.entries.Any(e=>e.Key==d.Key);b.interactable=unlocked;b.GetComponentInChildren<Text>().text=d.title+"  ·  "+(unlocked?"閲覧済み":"未解放");}
            notice.text="最後まで読んだ分岐を読み返せます（"+profile.entries.Length+" / "+CafeReplayCatalog.All.Length+"）。回想は営業に影響しません。\n"+(Backend.Replays.Error??(Backend.Replays.Recovered?"バックアップの回想記録を使用しています。":Backend.ReplayImportNotice));
        }
        public void OpenReplay(string key)
        {
            var profile=Backend.Replays.Load();var entry=profile.entries.FirstOrDefault(e=>e.Key==key);if(entry==null || !surface.activeSelf)return;
            surface.SetActive(false);Replay.Play(entry,replaySurface,font,()=>{RefreshList();surface.SetActive(true);surface.transform.SetAsLastSibling();});
        }
        public void Hide(){if(surface!=null)surface.SetActive(false);if(replaySurface!=null)replaySurface.SetActive(false);}
        GameObject Box(Transform parent,string name){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=new Color(.99f,.97f,.92f);return go;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        Text Label(Transform p,string name,float x,float y,float w,float h,int size){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(p,false);var t=go.GetComponent<Text>();t.font=font;t.fontSize=size;t.color=new Color(.22f,.26f,.20f);t.supportRichText=false;t.raycastTarget=false;Place(t.rectTransform,x,y,w,h);return t;}
        Button MakeButton(Transform p,string name,string caption,float x,float y,float w,float h){var go=Box(p,name);go.GetComponent<Image>().color=new Color(.87f,.91f,.82f);Place(go.GetComponent<RectTransform>(),x,y,w,h);var b=go.AddComponent<Button>();b.targetGraphic=go.GetComponent<Image>();var text=Label(go.transform,"Caption",12,0,w-24,h,20);var tr=text.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(12,0);tr.offsetMax=new Vector2(-12,0);text.text=caption;text.alignment=TextAnchor.MiddleLeft;return b;}
    }
    static class ReplayRectExtensions
    {public static void SetBottom(this RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}}
}
