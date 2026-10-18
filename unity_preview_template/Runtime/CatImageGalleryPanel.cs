using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public sealed class CatImageGalleryPanel : MonoBehaviour
    {
        TitleExtraPanel owner;Font font;GameObject panel,viewer;Text notice,caption;RawImage enlarged;ScrollRect scroll;GridLayoutGroup grid;CatGalleryImage[] items;float width;
        public GameObject Panel=>panel;
        public GameObject Viewer=>viewer;
        public ScrollRect Scroll=>scroll;
        public RawImage Enlarged=>enlarged;
        public CatGalleryImage[] Items=>items;
        LocalCafeBackend Backend=>CafeBackends.Local as LocalCafeBackend;
        public void Initialize(TitleExtraPanel parent,Font textFont)
        {
            owner=parent;font=textFont;items=CatImageGalleryCatalog.Load();panel=Box(transform,"Extra CG");Stretch(panel.GetComponent<RectTransform>());
            Label(panel.transform,"CG Heading","Extra · CG一覧",48,32,700,44,28);notice=Label(panel.transform,"CG Notice","",48,88,1184,52,17);
            Button(panel.transform,"会話の回想",1000,32,232,44,owner.Open);
            var list=Box(panel.transform,"CG List");var lr=list.GetComponent<RectTransform>();Stretch(lr);lr.offsetMin=new Vector2(48,104);lr.offsetMax=new Vector2(-48,-160);scroll=list.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.scrollSensitivity=36;
            var viewport=Box(list.transform,"Viewport");Stretch(viewport.GetComponent<RectTransform>());viewport.GetComponent<Image>().color=Color.clear;viewport.AddComponent<RectMask2D>();scroll.viewport=viewport.GetComponent<RectTransform>();scroll.viewport.offsetMax=new Vector2(-26,0);
            var content=new GameObject("Content",typeof(RectTransform),typeof(GridLayoutGroup),typeof(ContentSizeFitter));content.transform.SetParent(viewport.transform,false);scroll.content=content.GetComponent<RectTransform>();scroll.content.anchorMin=new Vector2(0,1);scroll.content.anchorMax=Vector2.one;scroll.content.pivot=new Vector2(.5f,1);scroll.content.sizeDelta=Vector2.zero;grid=content.GetComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=3;grid.spacing=new Vector2(18,18);content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            foreach(var item in items)
            {
                var card=Box(content.transform,item.key.Replace('/','_'));card.GetComponent<Image>().color=new Color(.88f,.91f,.83f);var button=card.AddComponent<Button>();button.targetGraphic=card.GetComponent<Image>();button.onClick.AddListener(()=>OpenImage(item.key));
                var frame=new GameObject("Thumbnail Frame",typeof(RectTransform));frame.transform.SetParent(card.transform,false);var r=frame.GetComponent<RectTransform>();r.anchorMin=new Vector2(0,.25f);r.anchorMax=Vector2.one;r.offsetMin=new Vector2(12,12);r.offsetMax=new Vector2(-12,-12);var image=new GameObject("Thumbnail",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));image.transform.SetParent(frame.transform,false);var raw=image.GetComponent<RawImage>();raw.raycastTarget=false;image.AddComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;
                var label=Label(card.transform,"Caption","未解放",12,178,300,48,18);var tr=label.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=new Vector2(1,.25f);tr.offsetMin=new Vector2(12,4);tr.offsetMax=new Vector2(-12,-4);label.alignment=TextAnchor.MiddleCenter;
            }
            var scrollbar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());scrollbar.transform.SetParent(list.transform,false);scrollbar.name="Scrollbar";var sr=scrollbar.GetComponent<RectTransform>();sr.anchorMin=new Vector2(1,0);sr.anchorMax=Vector2.one;sr.offsetMin=new Vector2(-18,0);sr.offsetMax=Vector2.zero;var bar=scrollbar.GetComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;scrollbar.GetComponent<Image>().color=new Color(.88f,.88f,.82f);bar.handleRect.GetComponent<Image>().color=new Color(.40f,.48f,.32f);scroll.verticalScrollbar=bar;
            BottomButton(panel.transform,"タイトルへ戻る",()=>{Hide();owner.Hide();});
            viewer=Box(transform,"CG Viewer");Stretch(viewer.GetComponent<RectTransform>());caption=Label(viewer.transform,"Image Caption","",48,32,1100,44,26);
            var area=Box(viewer.transform,"Image Area");var a=area.GetComponent<RectTransform>();Stretch(a);a.offsetMin=new Vector2(48,104);a.offsetMax=new Vector2(-48,-104);area.GetComponent<Image>().color=new Color(.91f,.90f,.84f);
            var full=new GameObject("CG Image",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage),typeof(AspectRatioFitter));full.transform.SetParent(area.transform,false);enlarged=full.GetComponent<RawImage>();enlarged.raycastTarget=false;full.GetComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            BottomButton(viewer.transform,"CG一覧へ戻る",()=>{viewer.SetActive(false);panel.SetActive(true);panel.transform.SetAsLastSibling();});Hide();
        }
        void Update(){if(panel!=null && panel.activeSelf)Layout();}
        void Layout(){float w=scroll.viewport.rect.width;if(w<=0 || Mathf.Abs(w-width)<.1f)return;width=w;grid.cellSize=new Vector2((w-36)/3,230);}
        public void Open()
        {
            if(!owner.CanOpen)return;owner.Hide();Refresh();panel.SetActive(true);panel.transform.SetAsLastSibling();Canvas.ForceUpdateCanvases();Layout();scroll.verticalNormalizedPosition=1;
        }
        public void Refresh()
        {
            var profile=Backend.Gallery.Load();int count=0;
            foreach(var item in items)
            {
                var card=scroll.content.Find(item.key.Replace('/','_'));bool seen=profile.viewed.Contains(item.key);if(seen)count++;var texture=seen?item.Texture():null;card.GetComponent<Button>().interactable=seen && texture!=null;
                var picture=card.GetComponentInChildren<RawImage>(true);picture.texture=texture;picture.enabled=texture!=null;if(texture!=null)picture.GetComponent<AspectRatioFitter>().aspectRatio=(float)texture.width/texture.height;
                card.Find("Caption").GetComponent<Text>().text=!seen?"未解放":item.name+" · 通常"+(texture==null?"（画像を読み込めません）":"");
            }
            notice.text=(items.Length==0?"登録済みの通常画像はありません。":"プレイ画面で表示した通常画像だけを鑑賞できます（"+count+" / "+items.Length+"）。")+"\n"+(Backend.Gallery.Error??(Backend.Gallery.Recovered?"バックアップのCG閲覧記録を使用しています。":"未解放画像は表示しません。鑑賞で営業状態は変わりません。"));
        }
        public void OpenImage(string key)
        {
            if(!panel.activeSelf || !owner.CanOpen)return;var item=items.FirstOrDefault(i=>i.key==key);if(item==null || !Backend.Gallery.Load().viewed.Contains(key))return;var texture=item.Texture();if(texture==null){Refresh();return;}
            enlarged.texture=texture;enlarged.GetComponent<AspectRatioFitter>().aspectRatio=(float)texture.width/texture.height;caption.text=item.name+" · 通常";panel.SetActive(false);viewer.SetActive(true);viewer.transform.SetAsLastSibling();
        }
        public void Hide(){if(panel!=null)panel.SetActive(false);if(viewer!=null)viewer.SetActive(false);}
        GameObject Box(Transform p,string n){var go=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(p,false);go.GetComponent<Image>().color=new Color(.99f,.97f,.92f);return go;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        Text Label(Transform p,string n,string text,float x,float y,float w,float h,int size){var go=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(p,false);var t=go.GetComponent<Text>();t.font=font;t.fontSize=size;t.text=text;t.supportRichText=false;t.color=new Color(.22f,.26f,.20f);t.raycastTarget=false;Place(t.rectTransform,x,y,w,h);return t;}
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        void Button(Transform p,string text,float x,float y,float w,float h,UnityEngine.Events.UnityAction action){var go=Box(p,text);Place(go.GetComponent<RectTransform>(),x,y,w,h);go.GetComponent<Image>().color=new Color(.87f,.91f,.82f);go.AddComponent<Button>().onClick.AddListener(action);var label=Label(go.transform,"Caption",text,0,0,w,h,18);label.alignment=TextAnchor.MiddleCenter;}
        void BottomButton(Transform p,string text,UnityEngine.Events.UnityAction action){Button(p,text,48,0,240,48,action);p.Find(text).GetComponent<RectTransform>().SetBottom(48,28,240,48);}
    }
}
