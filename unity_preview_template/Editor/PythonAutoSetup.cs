using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview.Editor
{
    public static class PythonAutoSetup
    {
        [MenuItem("Cat Cafe/開発用/UI再生成/自動営業の操作", false, 5)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); var root=preview.transform.Find("Content Canvas/Background");
            foreach(var name in new[]{"Automatic Business","Business Speed"}) { var old=root.Find(name); if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject); }
            var panel=preview.GetComponent<PythonAutoPanel>()??preview.gameObject.AddComponent<PythonAutoPanel>();
            panel.toggle=Button(root,"Automatic Business","自動進行",964,154,18);
            panel.speed=Button(root,"Business Speed","通常：1秒",1130,126,16);
            UnityEventTools.AddPersistentListener(panel.toggle.onClick,panel.Toggle);
            UnityEventTools.AddPersistentListener(panel.speed.onClick,panel.ChangeSpeed);
            panel.toggle.interactable=false;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("CAT_CAFE_AUTO_UI_OK: saved automatic toggle and speed controls");
        }
        static Button Button(Transform parent,string name,string caption,float x,float width,int fontSize)
        {
            var go=DefaultControls.CreateButton(new DefaultControls.Resources()); go.name=name; go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(x,-128); rect.sizeDelta=new Vector2(width,40);
            var text=go.GetComponentInChildren<Text>(); text.font=ContentPreviewLayout.LoadFont(); text.fontSize=fontSize; text.text=caption; return go.GetComponent<Button>();
        }
        public static void BuildAndCapture()
        {
            Build(); var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); preview.Initialize();
            var client=preview.GetComponent<PythonStateClient>(); var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample(); var panel=preview.GetComponent<PythonAutoPanel>();
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});
            apply(); panel.Toggle(); if(!client.Automatic || !panel.toggle.interactable)throw new Exception("Automatic start failed");
            Canvas.ForceUpdateCanvases(); typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/AutomaticBusiness.png"});
            state.required_action="依頼に回答してください。"; apply(); if(client.Automatic || panel.toggle.interactable)throw new Exception("Required event did not stop automatic progress");
            state.required_action=""; apply(); panel.Toggle(); state=JsonUtility.FromJson<PythonCafeState>(JsonUtility.ToJson(state)); state.instance_id="restarted"; apply(); if(client.Automatic)throw new Exception("Server restart did not stop automatic progress");
            panel.Toggle(); state.closed=true; apply(); if(client.Automatic || panel.toggle.interactable)throw new Exception("Closed state did not stop automatic progress");
            state.closed=false; state.can_set_shifts=true; apply(); if(panel.toggle.interactable)throw new Exception("Automatic preparation must stay manual");
            Debug.Log("CAT_CAFE_AUTO_DISPLAY_OK: start / event stop / server restart stop / close stop / preparation disabled");
        }
    }
}
