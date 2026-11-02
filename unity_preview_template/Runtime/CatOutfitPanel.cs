using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public sealed class CatOutfitPanel : MonoBehaviour
    {
        public Button entry,normal,ribbon,apply,cancel;
        public GameObject host;
        public RawImage candidate;
        public Text notice;
        PythonCatDetailsPanel owner;PythonCatState cat;string choice,session;
        PythonStateClient Client=>GetComponent<PythonStateClient>();
        bool CanEdit=>Client.CanChangeCatOutfit && owner.panel.activeSelf && cat?.cat_id=="cat-mike" && !GetComponents<AcquisitionConversationPanel>().AnyShowing(null) && !GameOptionsPanel.IsOpen;
        static RectTransform Place(GameObject go,Transform parent,float x,float y,float w,float h)
        {go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        Text Label(Transform parent,string name,float x,float y,float w,float h,int size)
        {var t=new GameObject(name,typeof(RectTransform),typeof(Text)).GetComponent<Text>();Place(t.gameObject,parent,x,y,w,h);t.font=owner.title.font;t.fontSize=size;t.color=new Color(.16f,.21f,.18f);t.supportRichText=false;t.raycastTarget=false;return t;}
        Button Button(Transform parent,string name,string caption,float x,float y,float w,Action action)
        {var b=Instantiate(owner.interact,parent);b.name=name;Place(b.gameObject,parent,x,y,w,40);b.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();b.onClick.AddListener(()=>action());var t=b.GetComponentInChildren<Text>();t.text=caption;t.fontSize=18;t.supportRichText=false;return b;}
        public void Refresh(PythonCatDetailsPanel details,PythonCatState value)
        {
            owner=details;if(cat?.cat_id!=value.cat_id || session!=Client.CurrentState?.instance_id)Close();cat=value;session=Client.CurrentState?.instance_id;
            if(entry==null)entry=Button(owner.panel.transform.Find("Details Card"),"Cat Outfit Entry","見た目…",184,20,112,Open);
            owner.title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,CafeBackends.Standalone && value.cat_id=="cat-mike"?144:310);
            entry.GetComponentInChildren<Text>().fontSize=15;entry.gameObject.SetActive(CafeBackends.Standalone && value.cat_id=="cat-mike");entry.interactable=CanEdit;
            if(host!=null && host.activeSelf){if((CafeBackends.Local as LocalCafeBackend)?.CanChangeCatOutfit!=true){Close();return;}host.transform.SetAsLastSibling();UpdatePreview();}
        }
        public void Open()
        {
            if(!CanEdit)return;
            if(host==null)
            {
                host=new GameObject("Cat Outfit Selection",typeof(RectTransform),typeof(Image));host.transform.SetParent(owner.panel.transform.parent,false);var overlay=host.GetComponent<RectTransform>();overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;host.GetComponent<Image>().color=new Color(0,0,0,.65f);
                var card=new GameObject("Card",typeof(RectTransform),typeof(Image));card.transform.SetParent(host.transform,false);var rect=card.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(640,480);card.GetComponent<Image>().color=new Color(.96f,.96f,.92f);
                Label(card.transform,"Title",24,20,592,40,24).text="ミケの見た目";
                normal=Button(card.transform,"Normal","通常",24,78,180,()=>Choose(""));ribbon=Button(card.transform,"Ribbon",CatOutfits.Label,218,78,180,()=>Choose("ribbon"));
                candidate=new GameObject("Candidate",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();Place(candidate.gameObject,card.transform,24,138,192,192);candidate.raycastTarget=false;
                notice=Label(card.transform,"Notice",240,140,376,220,18);
                apply=Button(card.transform,"Apply","適用",24,404,180,Apply);cancel=Button(card.transform,"Cancel","キャンセル",218,404,180,Close);
                Button(card.transform,"Clear","通常に戻す",412,404,204,()=>{Choose("");Apply();});
            }
            choice=cat.outfit_id=="ribbon"?"ribbon":"";host.SetActive(true);host.transform.SetAsLastSibling();UpdatePreview();
        }
        public void Choose(string value){choice=value;UpdatePreview();}
        void UpdatePreview()
        {
            if(host==null || !host.activeSelf)return;
            var image=choice=="ribbon"?CatOutfits.ImageFor(cat.content_cat_id):GetComponent<ContentPreview>().PythonCatTexture(cat);candidate.texture=image;candidate.enabled=image!=null;
            if(image!=null){float ratio=(float)image.width/image.height;candidate.rectTransform.sizeDelta=ratio>=1?new Vector2(192,192/ratio):new Vector2(192*ratio,192);}
            ribbon.GetComponentInChildren<Text>().text=CatOutfits.Label;ribbon.interactable=CanEdit && CatOutfits.ImageFor(cat.content_cat_id)!=null;
            notice.text=CatOutfits.Reason(cat)+"\n\n候補："+(choice=="ribbon"?CatOutfits.Label:"通常")+"\n状態表示を優先します。\n適用後、次のゲーム保存に含まれます。";
            if(CatOutfits.ImageFor(cat.content_cat_id)==null)notice.text+="\nリボン姿の画像が未登録です。";
            apply.interactable=CanEdit && (choice=="" || ribbon.interactable);
        }
        public void Apply(){if(!CanEdit || choice=="ribbon" && CatOutfits.ImageFor(cat.content_cat_id)==null)return;Client.SetCatOutfit(choice);Close();}
        public void RefreshControls()
        {
            if(entry!=null)entry.interactable=CanEdit;
            if(host!=null && host.activeSelf){normal.interactable=CanEdit;cancel.interactable=CanEdit;UpdatePreview();}
        }
        public void Close(){if(host!=null)host.SetActive(false);choice=null;}
    }
}
