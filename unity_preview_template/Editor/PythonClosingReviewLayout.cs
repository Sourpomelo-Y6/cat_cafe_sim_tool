using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonClosingReviewLayout
    {
        static void Place(Component c,float x,float y,float w,float h){var r=c.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static Button Button(Transform parent,string name,string caption,float x,float y,float w){var go=DefaultControls.CreateButton(new DefaultControls.Resources());go.name=name;go.transform.SetParent(parent,false);Place(go.transform,x,y,w,32);var b=go.GetComponent<Button>();var t=b.GetComponentInChildren<Text>();t.font=ContentPreviewLayout.LoadFont();t.fontSize=16;t.text=caption;return b;}
        static void Bind(Button b,UnityEngine.Events.UnityAction action){b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,action);b.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);}
        static void Scroll(ScrollRect s,float x,float y,float w,float h){Place(s,x,y,w,h);Place(s.viewport,8,6,w-32,h-12);Place(s.verticalScrollbar,w-18,6,12,h-12);s.content.sizeDelta=new Vector2(w-32,s.content.sizeDelta.y);}
        public static void RefreshIfReady(){if(Object.FindObjectOfType<PythonClosingPanel>()?.panel!=null)Build();}
        [MenuItem("Cat Cafe/開発用/UI再生成/閉店の振り返り整理",false,47)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var p=Object.FindObjectOfType<PythonClosingPanel>();var review=p.GetComponent<PythonClosingReview>() ?? p.gameObject.AddComponent<PythonClosingReview>();var root=p.title.transform.parent;
            foreach(var name in new[]{"Closing Tabs","Closing Cats","Closing Cat Details"}){var old=root.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);}
            root.Find("Goal Heading").gameObject.SetActive(false);var heading=root.Find("Cats Heading").GetComponent<Text>();Place(heading,24,208,448,28);heading.text="猫の状態・翌日の準備";
            Scroll(p.catsScroll,24,284,448,198);Scroll(p.goalScroll,496,244,420,238);p.details.fontSize=p.goalDetails.fontSize=16;
            var drop=DefaultControls.CreateDropdown(new DefaultControls.Resources());drop.name="Closing Cats";drop.transform.SetParent(root,false);Place(drop.transform,24,240,290,32);review.cats=drop.GetComponent<Dropdown>();foreach(var t in drop.GetComponentsInChildren<Text>(true)){t.font=ContentPreviewLayout.LoadFont();t.fontSize=16;t.color=new Color(.16f,.23f,.19f);}
            Place(review.cats.captionText,10,2,244,28);review.cats.captionText.alignment=TextAnchor.MiddleLeft;
            review.cats.itemText.rectTransform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,32);review.cats.template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,180);review.cats.itemText.verticalOverflow=VerticalWrapMode.Overflow;drop.transform.Find("Arrow").GetComponent<Image>().color=Color.clear;var arrow=new GameObject("Arrow Label",typeof(RectTransform),typeof(Text));arrow.transform.SetParent(drop.transform,false);Place(arrow.transform,260,3,24,26);var arrowText=arrow.GetComponent<Text>();arrowText.font=ContentPreviewLayout.LoadFont();arrowText.fontSize=16;arrowText.text="▼";arrowText.color=new Color(.16f,.23f,.19f);arrowText.raycastTarget=false;UnityEventTools.AddPersistentListener(review.cats.onValueChanged,review.SelectCat);review.cats.onValueChanged.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            review.catDetails=Button(root,"Closing Cat Details","選択した猫の詳細",326,240,146);review.catDetails.GetComponentInChildren<Text>().fontSize=14;Bind(review.catDetails,review.OpenCat);
            var tabs=new GameObject("Closing Tabs",typeof(RectTransform));tabs.transform.SetParent(root,false);Place(tabs.transform,496,208,420,32);review.tabs=new[]{Button(tabs.transform,"Goal","目標",0,0,128),Button(tabs.transform,"Business","今日の出来事",140,0,128),Button(tabs.transform,"Finance","収支・前日比較",280,0,140)};Bind(review.tabs[0],review.Goal);Bind(review.tabs[1],review.Business);Bind(review.tabs[2],review.Finance);
            p.save.GetComponentInChildren<Text>().text="セーブ画面を開く";p.close.GetComponentInChildren<Text>().text="営業画面へ戻る";Bind(p.save,p.Save);Bind(p.next,p.Next);Bind(p.close,p.Close);
            p.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_CLOSING_REVIEW_LAYOUT_OK");
        }
    }
}
