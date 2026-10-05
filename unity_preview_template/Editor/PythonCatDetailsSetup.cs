using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonCatDetailsSetup
    {
        static RectTransform Place(GameObject go,Transform parent,float x,float y,float w,float h)
        {
            go.transform.SetParent(parent,false); var rect=go.GetComponent<RectTransform>();
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(w,h); return rect;
        }
        static GameObject Box(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));
            Place(go,parent,x,y,w,h); go.GetComponent<Image>().color=color; return go;
        }
        static Text Label(Transform parent,string name,float x,float y,float w,float h,int size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text)); Place(go,parent,x,y,w,h);
            var text=go.GetComponent<Text>(); text.font=ContentPreviewLayout.LoadFont();text.fontSize=size;
            text.color=new Color(.16f,.21f,.18f);text.raycastTarget=false;text.supportRichText=false;
            return text;
        }
        static void AddCardButton(PythonCatCard card)
        {
            if(card.details != null) return;
            var button=Box(card.transform,"Open Cat Details",0,0,242,42,new Color(1,1,1,0)).AddComponent<Button>();
            card.details=button;
            var hint=Label(button.transform,"Details Hint",180,4,50,30,14);hint.text="詳細";
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/猫の詳細",false,6)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();
            var root=preview.transform.Find("Content Canvas/Background");
            var old=root.Find("Cat Details");if(old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var overlay=Box(root,"Cat Details",0,174,1280,626,new Color(0,0,0,.55f));
            var card=Box(overlay.transform,"Details Card",210,8,860,600,new Color(.98f,.97f,.93f,1));
            var panel=preview.GetComponent<PythonCatDetailsPanel>() ?? preview.gameObject.AddComponent<PythonCatDetailsPanel>();
            panel.panel=overlay;panel.title=Label(card.transform,"Title",24,20,670,48,25);
            var close=DefaultControls.CreateButton(new DefaultControls.Resources());close.name="Close";
            Place(close,card.transform,724,20,112,40);
            var closeText=close.GetComponentInChildren<Text>();closeText.font=ContentPreviewLayout.LoadFont();closeText.fontSize=18;closeText.text="閉じる";
            UnityEventTools.AddPersistentListener(close.GetComponent<Button>().onClick,panel.Close);
            var area=Box(card.transform,"Details Scroll",24,80,812,490,Color.clear);
            panel.scroll=area.AddComponent<ScrollRect>();panel.scroll.horizontal=false;panel.scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Box(area.transform,"Viewport",0,0,786,490,Color.white);viewport.AddComponent<Mask>().showMaskGraphic=false;
            panel.details=Label(viewport.transform,"Details Text",0,0,786,490,20);
            var fitter=panel.details.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            panel.scroll.viewport=viewport.GetComponent<RectTransform>();panel.scroll.content=panel.details.rectTransform;
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.name="Vertical Scrollbar";
            Place(bar,area.transform,792,0,20,490);var scrollbar=bar.GetComponent<Scrollbar>();
            scrollbar.direction=Scrollbar.Direction.BottomToTop;panel.scroll.verticalScrollbar=scrollbar;
            var prefabPath="Assets/CatCafePreview/Prefabs/PythonCatCard.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
            try { AddCardButton(prefab.GetComponent<PythonCatCard>());PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            foreach(var item in preview.GetComponentsInChildren<PythonCatCard>(true))
            { AddCardButton(item);PrefabUtility.RecordPrefabInstancePropertyModifications(item); }
            overlay.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("CAT_CAFE_DETAILS_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();
            var client=preview.GetComponent<PythonStateClient>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.cats[0].health_label="健康";state.cats[0].activity_label="在店";
            state.cats[0].growth_details="得意：接客\n得意な交流：遊び・触れ合い\n得意な行動：ねこじゃらし・ブラッシング\n個別行動熟練まであと 12回\n2つ目の得意な交流：静かな交流 あと 20回";
            typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});
            var panel=preview.GetComponent<PythonCatDetailsPanel>();
            var roster=preview.GetComponent<PythonCatRoster>();
            if(roster.GetCard(0).details==null || !roster.GetCard(0).details.interactable) throw new Exception("Details entry missing");
            roster.GetCard(0).details.onClick.Invoke();
            if(!panel.panel.activeSelf || !panel.title.text.Contains("ミケ") || !panel.details.text.Contains("あと 12回")) throw new Exception("Details display failed");
            Canvas.ForceUpdateCanvases();
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/CatDetails.png"});
            Array.Reverse(state.cats);state.cats[4].stamina=32;preview.ApplyPythonState(state);
            if(!panel.title.text.Contains("ミケ") || !panel.details.text.Contains("32 / 100")) throw new Exception("Stable selection failed");
            state.cats[4].growth_details=new string('長',4000);preview.ApplyPythonState(state);Canvas.ForceUpdateCanvases();
            if(panel.scroll.content.rect.height<=panel.scroll.viewport.rect.height) throw new Exception("Long details scrolling failed");
            panel.Disconnect();if(!panel.details.text.Contains("未接続")) throw new Exception("Disconnect failed");
            preview.ApplyPythonState(state);if(!panel.details.text.Contains("32 / 100")) throw new Exception("Reconnect failed");
            panel.Open(state.cats[0].cat_id);if(!panel.title.text.Contains(state.cats[0].name)) throw new Exception("Switch failed");
            panel.Close();if(panel.panel.activeSelf) throw new Exception("Close failed");
            panel.Open("missing");if(panel.panel.activeSelf) throw new Exception("Missing cat failed");
            Debug.Log("CAT_CAFE_DETAILS_DISPLAY_OK: entry / progress / stable ID / refresh / offline / reconnect / close / missing cat");
        }
    }
}
