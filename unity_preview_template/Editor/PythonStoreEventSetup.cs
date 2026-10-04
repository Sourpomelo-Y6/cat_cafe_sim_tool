using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview.Editor
{
    public static class PythonStoreEventSetup
    {
        [MenuItem("Cat Cafe/開発用/UI再生成/店舗イベント", false, 3)]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
            var root = preview.transform.Find("Content Canvas/Background");
            var old = root.Find("Store Event"); if(old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var overlay = Box(root, "Store Event", 0, 174, 1280, 626, new Color(0,0,0,.55f));
            var card = Box(overlay.transform, "Request Card", 284, 0, 712, 610, new Color(.98f,.97f,.93f,1));
            var panel = preview.GetComponent<PythonStoreEventPanel>() ?? preview.gameObject.AddComponent<PythonStoreEventPanel>();
            panel.panel = overlay;
            panel.title = Label(card.transform,"Title",30,22,652,54,25);
            panel.details = Label(card.transform,"Cat Details",30,86,652,400,19);
            panel.notice = Label(card.transform,"Response Notice",30,502,652,50,17);
            panel.notice.lineSpacing=1;
            panel.choices=new [] {Button(card.transform,"First Choice","選択肢1",20,558),Button(card.transform,"Second Choice","選択肢2",246,558),Button(card.transform,"Third Choice","選択肢3",472,558)};
            UnityEventTools.AddPersistentListener(panel.choices[0].onClick,panel.First);
            UnityEventTools.AddPersistentListener(panel.choices[1].onClick,panel.Second);
            UnityEventTools.AddPersistentListener(panel.choices[2].onClick,panel.Third);
            panel.title.text="店舗イベント";
            foreach(var button in panel.choices) button.interactable=false;
            overlay.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("CAT_CAFE_STORE_UI_OK: serialized event panel / details / three choices");
        }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width,height);
        }
        static GameObject Box(Transform parent,string name,float x,float y,float width,float height,Color color)
        {
            var box=new GameObject(name,typeof(RectTransform),typeof(Image)); box.transform.SetParent(parent,false);
            Place(box.GetComponent<RectTransform>(),x,y,width,height); box.GetComponent<Image>().color=color; return box;
        }
        static Text Label(Transform parent,string name,float x,float y,float width,float height,int fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,width,height); var text=go.GetComponent<Text>();
            text.font=ContentPreviewLayout.LoadFont(); text.fontSize=fontSize; text.color=new Color(.18f,.16f,.14f,1);
            text.alignment=TextAnchor.UpperLeft; text.lineSpacing=1.12f; text.raycastTarget=false; return text;
        }
        static Button Button(Transform parent,string name,string caption,float x,float y)
        {
            var go=DefaultControls.CreateButton(new DefaultControls.Resources()); go.name=name; go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,220,44); var text=go.GetComponentInChildren<Text>();
            text.font=ContentPreviewLayout.LoadFont(); text.fontSize=19; text.text=caption; return go.GetComponent<Button>();
        }
        public static void VerifyAndCapture()
        {
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); preview.Initialize();
            var client=preview.GetComponent<PythonStateClient>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample(); state.can_set_shifts=true; state.required_action="店舗イベントへの対応を選んでください。";
            state.store_event=new PythonStoreEvent { kind="trouble",title="設備トラブル",day=2,funds=1000, choices=new [] {
                new PythonEventChoice {choice="repair",label="修理する",detail="閉店時の運営費に100を追加し、全席で営業します。",can_select=true},
                new PythonEventChoice {choice="patch",label="応急処置する",detail="追加費用なし。今日は1席を使用停止します。",can_select=true},
                new PythonEventChoice {choice="close",label="休業して翌日へ",detail="来客なしで猫を休ませ、翌日へ進みます。日次運営費は発生します。",can_select=true}}};
            typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});
            var panel=preview.GetComponent<PythonStoreEventPanel>();
            if(!panel.panel.activeSelf || !panel.choices[0].interactable || client.CanProgressBusiness) throw new Exception("Event display guard failed");
            Canvas.ForceUpdateCanvases();
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/StoreEvent.png"});
            panel.First(); if(panel.choices[0].interactable || panel.choices[1].interactable) throw new Exception("Pending guard failed");
            typeof(PythonStateClient).GetField("pending",flags).SetValue(client,null);
            state.store_event.choices[2].can_select=false; state.store_event.choices[2].reason="先に受け入れ依頼に回答してください。"; preview.ApplyPythonState(state);
            if(panel.choices[2].interactable || !panel.details.text.Contains("受け入れ")) throw new Exception("Rule guard failed");
            panel.Disconnect(); foreach(var button in panel.choices) if(button.interactable) throw new Exception("Offline guard failed");
            state.store_event=null; preview.ApplyPythonState(state); if(panel.panel.activeSelf) throw new Exception("Event dismissal failed");
            Debug.Log("CAT_CAFE_STORE_DISPLAY_OK: details / manual progress guard / pending / rule / offline / dismissal");
        }
        public static void BuildAndCapture() { Build(); VerifyAndCapture(); }
    }
}
