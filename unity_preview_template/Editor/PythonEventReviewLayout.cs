using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonEventReviewLayout
    {
        static void Place(Component c,float x,float y,float w,float h){var rect=c.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);}
        static GameObject Box(Transform parent,string name,float x,float y,float w,float h,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);Place(go.transform,x,y,w,h);go.GetComponent<Image>().color=color;return go;}
        static Text Text(Transform parent,string name,float x,float y,float w,float h,int size){var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Place(go.transform,x,y,w,h);var t=go.GetComponent<Text>();t.font=ContentPreviewLayout.LoadFont();t.fontSize=size;t.color=new Color(.16f,.23f,.19f);t.supportRichText=false;t.raycastTarget=false;return t;}
        static Button Button(Transform parent,string name,string label,float x,float y,float w,UnityEngine.Events.UnityAction action){var go=DefaultControls.CreateButton(new DefaultControls.Resources());go.name=name;go.transform.SetParent(parent,false);Place(go.transform,x,y,w,40);var b=go.GetComponent<Button>();var t=b.GetComponentInChildren<Text>();t.font=ContentPreviewLayout.LoadFont();t.fontSize=17;t.text=label;UnityEventTools.AddPersistentListener(b.onClick,action);b.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);return b;}
        static Text Scroll(Transform parent,string name,float x,float y,float w,float h,int size){var area=Box(parent,name,x,y,w,h,new Color(.93f,.94f,.89f));var s=area.AddComponent<ScrollRect>();s.horizontal=false;s.movementType=ScrollRect.MovementType.Clamped;var viewport=Box(area.transform,"Viewport",8,6,w-32,h-12,Color.white);viewport.AddComponent<Mask>().showMaskGraphic=false;var t=Text(viewport.transform,"Text",0,0,w-32,h-12,size);t.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;s.content=t.rectTransform;s.viewport=viewport.GetComponent<RectTransform>();var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.transform.SetParent(area.transform,false);Place(bar.transform,w-18,6,12,h-12);s.verticalScrollbar=bar.GetComponent<Scrollbar>();s.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;return t;}
        static void Choices(Button[] buttons,Text[] texts)
        {
            if(texts==null)return;bool grid=buttons.Length==4;int pitch=grid ? 200 : 400/buttons.Length;
            for(int i=0;i<buttons.Length && i<texts.Length;i++){if(texts[i]==null)continue;float x=348+(grid?i%2*290:0),y=86+(grid?i/2:i)*pitch,w=grid?278:568;Place(buttons[i],x,y+pitch-36,w,30);buttons[i].GetComponentInChildren<Text>().fontSize=16;var s=texts[i].GetComponentInParent<ScrollRect>(true);Place(s,x,y,w,pitch-42);Place(s.viewport,8,6,w-32,pitch-54);Place(s.verticalScrollbar,w-18,6,12,pitch-54);s.content.sizeDelta=new Vector2(w-32,s.content.sizeDelta.y);texts[i].fontSize=14;}
        }
        public static void RefreshIfReady(){if(Object.FindObjectOfType<PythonEventListPanel>()?.panel!=null)Build();}
        [MenuItem("Cat Cafe/開発用/UI再生成/イベントの回答と記録",false,48)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=Object.FindObjectOfType<ContentPreview>();var root=preview.transform.Find("Content Canvas/Background");var list=preview.GetComponent<PythonEventListPanel>();list.close.GetComponentInChildren<Text>().text="営業画面へ戻る";list.close.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(list.close.onClick,list.ReturnToBusiness);list.close.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            for(int i=0;i<14;i++)Place(list.entries[i],4,i*72,868,64);
            var store=preview.GetComponent<PythonStoreEventPanel>();Choices(store.choices,store.choiceDetails);var growth=preview.GetComponent<PythonGrowthPanel>();Choices(growth.choices,growth.choiceDetails);
            var old=root.Find("Event Record");if(old!=null)Object.DestroyImmediate(old.gameObject);var review=preview.GetComponent<PythonEventRecordPanel>() ?? preview.gameObject.AddComponent<PythonEventRecordPanel>();review.panel=Box(root,"Event Record",0,174,1280,626,new Color(0,0,0,.55f));var card=Box(review.panel.transform,"Record Card",170,8,940,600,new Color(.98f,.97f,.93f));review.title=Text(card.transform,"Title",24,18,892,48,25);review.details=Scroll(card.transform,"Details",24,86,892,396,18);review.notice=Scroll(card.transform,"Notice",24,490,892,54,14);Button(card.transform,"Back","イベント一覧へ戻る",24,550,284,review.Back);review.cat=Button(card.transform,"Cat","迎えた猫の詳細",320,550,284,review.Cat);Button(card.transform,"Business","営業画面へ戻る",616,550,300,review.Business);review.panel.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_EVENT_REVIEW_LAYOUT_OK");
        }
    }
}
