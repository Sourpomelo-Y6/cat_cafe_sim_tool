using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonPreparationLayout
    {
        static void Place(Component c,float x,float y,float w,float h){var r=c.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static GameObject Box(Transform p,string name,float x,float y,float w,float h,Color color){var g=new GameObject(name,typeof(RectTransform),typeof(Image));g.transform.SetParent(p,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);g.GetComponent<Image>().color=color;return g;}
        static Text Label(Transform p,string name,float x,float y,float w,float h,int size){var g=new GameObject(name,typeof(RectTransform),typeof(Text));g.transform.SetParent(p,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);var t=g.GetComponent<Text>();t.font=ContentPreviewLayout.LoadFont();t.fontSize=size;t.color=new Color(.16f,.23f,.19f);t.raycastTarget=false;t.supportRichText=false;return t;}
        static Button Button(Transform p,string name,string caption,float x,float y,float w,float h){var g=DefaultControls.CreateButton(new DefaultControls.Resources());g.name=name;g.transform.SetParent(p,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);var b=g.GetComponent<Button>();var t=b.GetComponentInChildren<Text>();t.font=ContentPreviewLayout.LoadFont();t.fontSize=16;t.text=caption;return b;}
        static void Bind(Button b,UnityEngine.Events.UnityAction call){b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,call);b.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);}
        static void Scroll(ScrollRect s,float x,float y,float w,float h){Place(s,x,y,w,h);Place(s.viewport,8,6,w-32,h-12);Place(s.verticalScrollbar,w-18,6,12,h-12);s.content.sizeDelta=new Vector2(w-32,s.content.sizeDelta.y);}
        public static void RefreshIfReady(){if(Object.FindObjectOfType<PythonPreparationPanel>()?.panel!=null)Build();}
        [MenuItem("Cat Cafe/開発用/UI再生成/営業準備の猫一覧",false,45)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var p=Object.FindObjectOfType<PythonPreparationPanel>();var roster=p.GetComponent<PythonPreparationRoster>() ?? p.gameObject.AddComponent<PythonPreparationRoster>();var card=p.title.transform.parent;var old=card.Find("Preparation Roster");if(old!=null)Object.DestroyImmediate(old.gameObject);old=card.Find("Preparation Tabs");if(old!=null)Object.DestroyImmediate(old.gameObject);
            p.cats.gameObject.SetActive(false);p.work.gameObject.SetActive(false);p.rest.gameObject.SetActive(false);p.catsDetails.GetComponentInParent<ScrollRect>(true).gameObject.SetActive(false);
            Place(p.title,24,16,892,40);var heading=card.Find("Goal Heading").GetComponent<Text>();Place(heading,24,148,224,28);heading.text="今日の猫・出勤予定";heading.fontSize=18;
            var visibility=p.GetComponent<PythonCatVisibility>();if(visibility?.preparationToggle!=null)Place(visibility.preparationToggle,260,148,212,28);
            var root=Box(card,"Preparation Roster",24,184,448,238,new Color(.93f,.94f,.89f));roster.scroll=root.AddComponent<ScrollRect>();roster.scroll.horizontal=false;roster.scroll.movementType=ScrollRect.MovementType.Clamped;var viewport=Box(root.transform,"Viewport",4,4,424,230,Color.white);viewport.AddComponent<Mask>().showMaskGraphic=false;var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(viewport.transform,false);roster.content=content.GetComponent<RectTransform>();Place(roster.content,0,0,424,600);roster.scroll.viewport=viewport.GetComponent<RectTransform>();roster.scroll.content=roster.content;
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.transform.SetParent(root.transform,false);Place(bar.GetComponent<RectTransform>(),432,4,12,230);roster.scroll.verticalScrollbar=bar.GetComponent<Scrollbar>();roster.scroll.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;
            roster.template=Box(content.transform,"Template",0,0,424,114,new Color(.92f,.95f,1f));var row=roster.template.transform;var portrait=PythonIntakeSetup.Portrait(row,8,8,68,96);portrait.transform.parent.name="Portrait";var select=Button(row,"Select","",0,0,424,74);select.GetComponent<Image>().color=Color.clear;Label(row,"Info",88,6,326,66,15);Button(row,"Work","出勤",88,78,154,28);Button(row,"Rest","休養",254,78,154,28);roster.template.SetActive(false);p.catsScroll=roster.scroll;
            roster.forecast=p.selectedDetails.GetComponentInParent<ScrollRect>(true);roster.goal=p.goalScroll;Scroll(roster.forecast,496,192,420,230);Scroll(roster.goal,496,192,420,230);p.selectedDetails.fontSize=16;p.details.fontSize=16;
            var tabs=new GameObject("Preparation Tabs",typeof(RectTransform));tabs.transform.SetParent(card,false);Place(tabs.GetComponent<RectTransform>(),496,148,420,32);roster.forecastTab=Button(tabs.transform,"Forecast","選択した猫・体調予測",0,0,200,32);roster.goalTab=Button(tabs.transform,"Goal","目標・営業の案内",220,0,200,32);Bind(roster.forecastTab,roster.Forecast);Bind(roster.goalTab,roster.Goal);roster.forecast.gameObject.SetActive(true);roster.goal.gameObject.SetActive(false);
            Place(p.propose,24,434,284,40);Place(p.confirmProposal,320,434,284,40);Place(p.cancelProposal,616,434,300,40);p.confirmProposal.GetComponentInChildren<Text>().text="休養案を確定";p.cancelProposal.GetComponentInChildren<Text>().text="休養案を取り消す";p.confirmProposal.GetComponentInChildren<Text>().fontSize=p.cancelProposal.GetComponentInChildren<Text>().fontSize=17;
            Scroll(p.notice.GetComponentInParent<ScrollRect>(true),24,484,892,58);Place(p.start,24,550,284,40);Place(p.dayOff,320,550,284,40);Place(p.close,616,550,300,40);p.close.GetComponentInChildren<Text>().text="営業画面へ戻る";Bind(p.start,p.StartBusiness);Bind(p.propose,p.ProposeRest);Bind(p.confirmProposal,p.ConfirmProposal);Bind(p.cancelProposal,p.CancelProposal);Bind(p.close,p.Close);
            p.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_PREPARATION_LAYOUT_OK");
        }
    }
}
