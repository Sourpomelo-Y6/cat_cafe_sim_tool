using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonObjectiveReviewLayout
    {
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Remove(Transform root,string name){var old=root.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);}
        static Text Label(Transform root,string name,string caption,float x,float y,float w,float h,int size){Remove(root,name);var g=new GameObject(name,typeof(RectTransform),typeof(Text));g.transform.SetParent(root,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);var t=g.GetComponent<Text>();t.text=caption;t.font=ContentPreviewLayout.LoadFont();t.fontSize=size;t.color=new Color(.16f,.21f,.18f);t.supportRichText=false;t.raycastTarget=false;return t;}
        static ScrollRect Scroll(Transform root,string name,float x,float y,float w,float h)
        {
            Remove(root,name);var g=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(ScrollRect));g.transform.SetParent(root,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);g.GetComponent<Image>().color=new Color(.93f,.94f,.89f);
            var view=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(Mask));view.transform.SetParent(g.transform,false);Place(view.GetComponent<RectTransform>(),6,6,w-30,h-12);view.GetComponent<Mask>().showMaskGraphic=false;
            var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(view.transform,false);Place(content.GetComponent<RectTransform>(),0,0,w-30,h-12);
            var s=g.GetComponent<ScrollRect>();s.horizontal=false;s.movementType=ScrollRect.MovementType.Clamped;s.viewport=view.GetComponent<RectTransform>();s.content=content.GetComponent<RectTransform>();
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.transform.SetParent(g.transform,false);Place(bar.GetComponent<RectTransform>(),w-18,6,12,h-12);s.verticalScrollbar=bar.GetComponent<Scrollbar>();s.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;return s;
        }
        static Text TextScroll(Transform root,string name,float x,float y,float w,float h,int size){var s=Scroll(root,name,x,y,w,h);Object.DestroyImmediate(s.content.gameObject);var t=Label(s.viewport,"Text","",0,0,w-30,h-12,size);t.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;s.content=t.rectTransform;return t;}
        static void Resize(Text text,float x,float y,float w,float h,int size){var s=text.GetComponentInParent<ScrollRect>(true);Place(s.GetComponent<RectTransform>(),x,y,w,h);Place(s.viewport,6,6,w-30,h-12);Place(s.verticalScrollbar.GetComponent<RectTransform>(),w-18,6,12,h-12);text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,w-30);text.fontSize=size;}
        static Text Notice(Text old,Transform card,string name){var scroll=old.GetComponentInParent<ScrollRect>(true);if(scroll!=null)Object.DestroyImmediate(scroll.gameObject);else Object.DestroyImmediate(old.gameObject);return TextScroll(card,name,24,490,892,54,14);}
        public static void RefreshIfReady(ContentPreview preview)
        {
            var start=preview.GetComponent<PythonObjectiveStartPanel>();if(start?.panel!=null)
            {
                var card=start.close.transform.parent;start.summary=TextScroll(card,"Objective Progress",24,130,280,270,18);start.actionGuide=Label(card,"Objective Action Guide","",24,414,280,66,15);Resize(start.details,320,130,596,352,18);start.notice=Notice(start.notice,card,"Objective Notice");start.panel.SetActive(false);
            }
            var goal=preview.GetComponent<PythonGoalPanel>();if(goal?.panel!=null)
            {
                var card=goal.resume.transform.parent;Place(card.GetComponent<RectTransform>(),170,8,940,600);Place(goal.title.rectTransform,24,18,892,48);goal.title.fontSize=25;
                goal.summary=TextScroll(card,"Outcome Summary",24,78,892,100,18);goal.nextSummary=TextScroll(card,"Outcome Next",616,190,300,290,17);Resize(goal.details,24,190,572,290,18);goal.notice=Notice(goal.notice,card,"Outcome Notice");
                Place(goal.resume.GetComponent<RectTransform>(),24,550,430,40);Place(goal.close.GetComponent<RectTransform>(),24,550,430,40);Place(goal.advance.GetComponent<RectTransform>(),478,550,438,40);goal.resume.GetComponentInChildren<Text>().fontSize=goal.close.GetComponentInChildren<Text>().fontSize=goal.advance.GetComponentInChildren<Text>().fontSize=17;goal.panel.SetActive(false);
            }
            var clear=preview.GetComponent<PythonClearResultsPanel>();if(clear?.panel!=null)
            {
                var card=clear.close.transform.parent;
                foreach(var name in new[]{"History Mode 0","History Mode 1","History Section 0","History Section 1","History Section 2","History Summary","History Dates","History Date Template"})Remove(card,name);
                clear.goals.gameObject.SetActive(false);Label(card,"Clear List Heading","達成済みの目標",24,78,280,40,18);Label(card,"Clear Detail Heading","達成した時点の成果",320,78,596,40,18);
                var s=Scroll(card,"Clear Record List",24,130,280,352);clear.recordContent=s.content;Remove(card,"Clear Record Template");var g=DefaultControls.CreateButton(new DefaultControls.Resources());g.name="Clear Record Template";g.transform.SetParent(card,false);Place(g.GetComponent<RectTransform>(),0,0,246,80);clear.entryPrefab=g.GetComponent<Button>();var t=g.GetComponentInChildren<Text>();t.font=ContentPreviewLayout.LoadFont();t.fontSize=17;t.supportRichText=false;g.SetActive(false);
                Resize(clear.details,320,130,596,352,18);clear.notice=Notice(clear.notice,card,"Clear Record Notice");clear.panel.SetActive(false);
            }
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/目標の進捗・結果・クリア記録")]
        public static void Build(){var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");RefreshIfReady(Object.FindObjectOfType<ContentPreview>());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
    }
}
