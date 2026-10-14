using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonSeatNarrativePanel : MonoBehaviour
    {
        public GameObject panel;public Text title,body;public ScrollRect scroll;
        private string catId;
        private GameObject Box(Transform parent,string name,Vector2 min,Vector2 max,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;go.GetComponent<Image>().color=color;return go;}
        private Text Label(Transform parent,string name,Vector2 min,Vector2 max){var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var t=go.GetComponent<Text>();t.font=GetComponent<PythonPlayerPanel>().history.font;t.fontSize=22;t.lineSpacing=1.25f;t.color=new Color(.12f,.17f,.14f);t.supportRichText=false;return t;}
        public void Open(PythonSeatState seat,int index,PythonCatState cat)
        {
            var client=GetComponent<PythonStateClient>();if(!client.CanManageSaves)return;catId=cat?.cat_id;client.PauseAutomaticForSave();
            if(panel==null){var canvas=GetComponent<PythonPlayerPanel>().panel.transform.parent;panel=Box(canvas,"Seat Narrative",Vector2.zero,Vector2.one,new Color(0,0,0,.7f));var card=Box(panel.transform,"Card",new Vector2(.12f,.06f),new Vector2(.88f,.94f),new Color(.98f,.97f,.92f));title=Label(card.transform,"Title",new Vector2(.04f,.82f),new Vector2(.96f,.97f));title.fontSize=24;
                var area=Box(card.transform,"History",new Vector2(.04f,.13f),new Vector2(.96f,.80f),Color.white);scroll=area.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;var viewport=Box(area.transform,"Viewport",new Vector2(.025f,.02f),new Vector2(.975f,.98f),Color.white);viewport.AddComponent<RectMask2D>();body=Label(viewport.transform,"Text",new Vector2(0,1),Vector2.one);body.rectTransform.pivot=new Vector2(.5f,1);body.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=body.rectTransform;
                var button=Box(card.transform,"Close",new Vector2(.52f,.025f),new Vector2(.96f,.10f),new Color(.8f,.88f,.82f)).AddComponent<Button>();var caption=Label(button.transform,"Caption",Vector2.zero,Vector2.one);caption.text="閉じる（進行は停止のまま）";caption.fontSize=18;caption.alignment=TextAnchor.MiddleCenter;button.onClick.AddListener(()=>panel.SetActive(false));
                var details=Box(card.transform,"Cat Details",new Vector2(.04f,.025f),new Vector2(.48f,.10f),new Color(.8f,.88f,.82f)).AddComponent<Button>();var detailCaption=Label(details.transform,"Caption",Vector2.zero,Vector2.one);detailCaption.text="担当猫の詳細";detailCaption.fontSize=18;detailCaption.alignment=TextAnchor.MiddleCenter;details.onClick.AddListener(()=>{if(catId!=null){panel.SetActive(false);GetComponent<PythonCatDetailsPanel>().OpenFromList(catId,true);}});
            }
            panel.transform.Find("Card/Cat Details").GetComponent<Button>().interactable=catId!=null;
            title.text="席 "+(index+1)+" の詳細　（履歴はスクロール）\n"+(cat==null?"空席":seat.customer_name+" / 担当："+cat.name+" / 体力 "+cat.stamina.ToString("0.#")+" / "+cat.max_stamina.ToString("0.#"));
            body.text=seat.narrative??"この接客には文章の記録がありません。";panel.SetActive(true);panel.transform.SetAsLastSibling();Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=1;
        }
    }
}
