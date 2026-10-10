using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonBusinessDashboardLayout
    {
        static void Place(Component c,float x,float y,float w,float h){var r=c.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static GameObject Box(Transform parent,string name,float x,float y,float w,float h){var g=new GameObject(name,typeof(RectTransform),typeof(Image));g.transform.SetParent(parent,false);Place(g.transform,x,y,w,h);g.GetComponent<Image>().color=new Color(.97f,.96f,.91f);return g;}
        public static void Configure(ContentPreview preview)
        {
            var root=preview.transform.Find("Content Canvas/Background");var header=preview.GetComponent<PythonBusinessHeader>();
            foreach(var name in new[]{"Business Guidance","Business Attention"}){var old=root.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);}
            root.Find("Title Save Hint").gameObject.SetActive(false);
            var area=Box(root,"Business Guidance",24,130,760,42);header.guidance=area.AddComponent<ScrollRect>();header.guidance.horizontal=false;header.guidance.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Box(area.transform,"Viewport",4,2,730,38);viewport.AddComponent<Mask>().showMaskGraphic=false;header.guidance.viewport=viewport.GetComponent<RectTransform>();
            var text=new GameObject("Notice",typeof(RectTransform),typeof(Text),typeof(ContentSizeFitter));text.transform.SetParent(viewport.transform,false);Place(text.transform,0,0,730,38);header.hint=text.GetComponent<Text>();header.hint.font=ContentPreviewLayout.LoadFont();header.hint.fontSize=14;header.hint.supportRichText=false;header.hint.color=new Color(.16f,.23f,.19f);text.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;header.guidance.content=header.hint.rectTransform;
            var scrollbar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());scrollbar.transform.SetParent(area.transform,false);Place(scrollbar.transform,742,2,12,38);header.guidance.verticalScrollbar=scrollbar.GetComponent<Scrollbar>();header.guidance.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;
            var button=DefaultControls.CreateButton(new DefaultControls.Resources());button.name="Business Attention";button.transform.SetParent(root,false);Place(button.transform,824,130,432,42);header.attention=button.GetComponent<Button>();var label=button.GetComponentInChildren<Text>();label.font=ContentPreviewLayout.LoadFont();label.fontSize=17;UnityEventTools.AddPersistentListener(header.attention.onClick,header.OpenAttention);header.attention.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);button.SetActive(false);
            var service=preview.GetComponent<PythonServiceScreen>();Place(service.seatsTab,824,92,210,32);Place(service.catsTab,1046,92,210,32);service.serviceSummary.fontSize=16;
            header.summary.fontSize=18;
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/営業状況と進行の案内",false,46)]
        public static void Build(){PythonBusinessHeaderSetup.Build();}
    }
}
