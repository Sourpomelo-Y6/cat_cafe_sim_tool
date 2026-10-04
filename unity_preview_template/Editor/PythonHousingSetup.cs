using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview.Editor
{
    public static class PythonHousingSetup
    {
        [MenuItem("Cat Cafe/開発用/UI再生成/飼育スペース", false, 2)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); var root=preview.transform.Find("Content Canvas/Background");
            foreach(var name in new[]{"Housing Panel","Open Housing"}) { var old=root.Find(name); if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject); }
            var overlay=Box(root,"Housing Panel",0,174,1280,626,new Color(0,0,0,.55f));
            var card=Box(overlay.transform,"Housing Card",284,0,712,610,new Color(.98f,.97f,.93f,1));
            var panel=preview.GetComponent<PythonHousingPanel>()??preview.gameObject.AddComponent<PythonHousingPanel>(); panel.panel=overlay;
            Label(card.transform,"Title",30,22,652,50,25).text="飼育スペースの拡張";
            panel.details=Label(card.transform,"Details",30,86,652,420,18);
            panel.notice=Label(card.transform,"Notice",30,510,652,44,16);
            panel.open=Button(root,"Open Housing","飼育スペース",740,128,200,40);
            panel.expand=Button(card.transform,"Expand Housing","初回拡張する",142,558,188,44);
            panel.close=Button(card.transform,"Close Housing","閉じる",382,558,188,44);
            UnityEventTools.AddPersistentListener(panel.open.onClick,panel.Open);
            UnityEventTools.AddPersistentListener(panel.expand.onClick,panel.Expand);
            UnityEventTools.AddPersistentListener(panel.close.onClick,panel.Close);
            panel.open.interactable=panel.expand.interactable=false;
            panel.details.text="飼育スペースの状態と拡張費用を表示します。"; overlay.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("CAT_CAFE_HOUSING_UI_OK: saved housing panel / header entry / expand / close");
        }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width,height);
        }
        static GameObject Box(Transform parent,string name,float x,float y,float width,float height,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,width,height); go.GetComponent<Image>().color=color; return go;
        }
        static Text Label(Transform parent,string name,float x,float y,float width,float height,int size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,width,height); var text=go.GetComponent<Text>();
            text.font=ContentPreviewLayout.LoadFont(); text.fontSize=size; text.color=new Color(.18f,.16f,.14f,1); text.raycastTarget=false; return text;
        }
        static Button Button(Transform parent,string name,string caption,float x,float y,float width,float height)
        {
            var go=DefaultControls.CreateButton(new DefaultControls.Resources()); go.name=name; go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,width,height); var text=go.GetComponentInChildren<Text>();
            text.font=ContentPreviewLayout.LoadFont(); text.fontSize=22; text.text=caption; return go.GetComponent<Button>();
        }
        public static void VerifyAndCapture()
        {
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); preview.Initialize();
            var state=PythonServiceSetup.Sample(); state.can_set_shifts=true; state.tick=0; state.funds=1000;
            state.seats=new PythonSeatState[0]; state.customers=new PythonCustomerState[0];
            state.housing=new PythonHousingState {configured=true,count=6,capacity=6,free=0,stage=0,kind="purchase_housing",next_capacity=9,
                cost=500,funds_after=500,daily_cost=0,next_daily_cost=20,operating_cost=60,next_operating_cost=80,can_expand=true};
            var client=preview.GetComponent<PythonStateClient>(); var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});
            var panel=preview.GetComponent<PythonHousingPanel>(); panel.Open();
            if(!panel.panel.activeSelf || !panel.expand.interactable || !panel.details.text.Contains("500") || !panel.details.text.Contains("80"))throw new Exception("Housing display mismatch");
            Canvas.ForceUpdateCanvases(); typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)
                .Invoke(null,new object[]{preview,"Logs/HousingPanel.png"});
            state.housing.can_expand=false; state.housing.reason="購入後に資金が残る必要があります。"; preview.ApplyPythonState(state);
            if(panel.expand.interactable || !panel.notice.text.Contains("資金"))throw new Exception("Housing funds guard failed");
            panel.Disconnect(); if(panel.open.interactable || panel.expand.interactable)throw new Exception("Housing offline guard failed");
            panel.Close(); if(panel.panel.activeSelf)throw new Exception("Housing close failed");
            Debug.Log("CAT_CAFE_HOUSING_DISPLAY_OK: counts / costs / maintenance / insufficient funds / offline / close");
        }
        public static void BuildAndCapture() { Build(); VerifyAndCapture(); }
    }
}
