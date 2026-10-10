using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonFinanceHistoryLayout
    {
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Remove(Transform parent,string name){var old=parent.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);}
        static Text Label(Transform parent,string name,float x,float y,float w,float h,int size){Remove(parent,name);var g=new GameObject(name,typeof(RectTransform),typeof(Text));g.transform.SetParent(parent,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);var t=g.GetComponent<Text>();t.font=ContentPreviewLayout.LoadFont();t.fontSize=size;t.color=new Color(.16f,.21f,.18f);t.raycastTarget=false;t.supportRichText=false;return t;}
        static Button Button(Transform parent,string name,string caption,float x,float y,float w,float h){Remove(parent,name);var g=DefaultControls.CreateButton(new DefaultControls.Resources());g.name=name;g.transform.SetParent(parent,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);var t=g.GetComponentInChildren<Text>();t.font=ContentPreviewLayout.LoadFont();t.fontSize=16;t.text=caption;t.supportRichText=false;return g.GetComponent<Button>();}
        static ScrollRect Scroll(Transform parent,string name,float x,float y,float w,float h)
        {
            Remove(parent,name);var g=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(ScrollRect));g.transform.SetParent(parent,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);g.GetComponent<Image>().color=new Color(.93f,.94f,.89f);
            var view=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(Mask));view.transform.SetParent(g.transform,false);Place(view.GetComponent<RectTransform>(),6,6,w-30,h-12);view.GetComponent<Mask>().showMaskGraphic=false;
            var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(view.transform,false);Place(content.GetComponent<RectTransform>(),0,0,w-30,h-12);
            var s=g.GetComponent<ScrollRect>();s.horizontal=false;s.movementType=ScrollRect.MovementType.Clamped;s.viewport=view.GetComponent<RectTransform>();s.content=content.GetComponent<RectTransform>();
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.transform.SetParent(g.transform,false);Place(bar.GetComponent<RectTransform>(),w-18,6,12,h-12);s.verticalScrollbar=bar.GetComponent<Scrollbar>();s.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;return s;
        }
        public static void RefreshIfReady(ContentPreview preview)
        {
            var p=preview.GetComponent<PythonFinanceHistoryPanel>();if(p==null || p.panel==null)return;var card=p.close.transform.parent;
            p.modes=new Button[2];for(int i=0;i<2;i++){p.modes[i]=Button(card,"History Mode "+i,i==0 ? "収支" : "営業成果",320+i*302,78,294,40);UnityEventTools.AddIntPersistentListener(p.modes[i].onClick,p.Mode,i);}
            p.sections=new Button[3];var labels=new[]{"当日の内訳","前日比較","記録期間"};for(int i=0;i<3;i++){p.sections[i]=Button(card,"History Section "+i,labels[i],320+i*202,254,192,34);UnityEventTools.AddIntPersistentListener(p.sections[i].onClick,p.SelectSection,i);}
            p.summary=Label(card,"History Summary",320,130,596,120,17);p.summary.resizeTextForBestFit=true;p.summary.resizeTextMinSize=14;p.summary.resizeTextMaxSize=17;
            var list=Scroll(card,"History Dates",24,130,280,360);p.listContent=list.content;p.entryPrefab=Button(card,"History Date Template","日付",0,0,246,50);p.entryPrefab.gameObject.SetActive(false);
            Place(p.previous.GetComponent<RectTransform>(),24,500,132,40);Place(p.next.GetComponent<RectTransform>(),172,500,132,40);p.previous.GetComponentInChildren<Text>().fontSize=p.next.GetComponentInChildren<Text>().fontSize=16;
            var detail=p.details.GetComponentInParent<ScrollRect>(true);Place(detail.GetComponent<RectTransform>(),320,294,596,196);Place(detail.viewport,6,6,566,184);Place(detail.verticalScrollbar.GetComponent<RectTransform>(),578,6,12,184);p.details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,566);p.details.fontSize=17;
            var oldNotice=p.notice.GetComponentInParent<ScrollRect>(true);if(oldNotice!=null)Object.DestroyImmediate(oldNotice.gameObject);else Object.DestroyImmediate(p.notice.gameObject);
            var notes=Scroll(card,"History Notes",320,500,596,40);Object.DestroyImmediate(notes.content.gameObject);p.notice=Label(notes.viewport,"Text",0,0,566,28,13);p.notice.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;notes.content=p.notice.rectTransform;
            p.panel.SetActive(false);
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/収支・営業成果の履歴整理")]
        public static void Build(){var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");RefreshIfReady(Object.FindObjectOfType<ContentPreview>());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
    }
}
