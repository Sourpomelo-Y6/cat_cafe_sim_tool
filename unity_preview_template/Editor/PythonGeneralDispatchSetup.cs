using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonGeneralDispatchSetup
    {
        static T Copy<T>(T source,Transform root,Transform target) where T:Component
        {return target.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>();}
        static void Bind(Button button,UnityEngine.Events.UnityAction action)
        {button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,action);}
        [MenuItem("Cat Cafe/開発用/UI再生成/一般派遣・帰還",false,25)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var source=preview.GetComponent<PythonDispatchPanel>();
            var root=source.panel.transform.parent;var old=root.Find("General Dispatch");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var p=preview.GetComponent<PythonGeneralDispatchPanel>() ?? preview.gameObject.AddComponent<PythonGeneralDispatchPanel>();
            p.panel=UnityEngine.Object.Instantiate(source.panel,root);p.panel.name="General Dispatch";var s=source.panel.transform;var t=p.panel.transform;
            p.title=Copy(source.title,s,t);p.details=Copy(source.details,s,t);p.progress=Copy(source.progress,s,t);p.notice=Copy(source.notice,s,t);
            p.destinations=Copy(source.destinations,s,t);p.returns=Copy(source.returns,s,t);p.send=Copy(source.send,s,t);p.receive=Copy(source.receive,s,t);p.close=Copy(source.close,s,t);
            p.destinations.onValueChanged=new Dropdown.DropdownEvent();UnityEventTools.AddPersistentListener(p.destinations.onValueChanged,p.Select);
            p.returns.onValueChanged=new Dropdown.DropdownEvent();UnityEventTools.AddPersistentListener(p.returns.onValueChanged,p.Select);
            Bind(p.send,p.Send);Bind(p.receive,p.Receive);Bind(p.close,p.Close);p.send.GetComponentInChildren<Text>().text="選んだ猫をこの派遣先へ送る";
            p.details.fontSize=16;p.details.rectTransform.sizeDelta=new Vector2(892,134);
            p.notice.rectTransform.anchoredPosition=new Vector2(24,-492);p.notice.rectTransform.sizeDelta=new Vector2(892,54);
            p.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PythonEventListSetup.Build();Debug.Log("CAT_CAFE_GENERAL_DISPATCH_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="general-dispatch-test";
            state.general_dispatch=new PythonPatronDispatch{options=new[]{new PythonDispatchOption{cat_id=state.cats[0].cat_id,choice="neighborhood_visit",label="ミケ / 近所のお店への訪問",days=1,reward=120,detail="健康・体力あり・疲労60以下 / 店内に席数分の猫を残します。\n歓迎条件：長毛 一致\n帰還時のストレス ＋5\n用品報酬：ケア用品\n派遣先からの猫紹介の回答は未対応です。",can_select=true}},events=new PythonDispatchEvent[0]};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var p=preview.GetComponent<PythonGeneralDispatchPanel>();
            var list=preview.GetComponent<PythonEventListPanel>();list.Open();list.GeneralDispatch();
            if(list.panel.activeSelf || !p.panel.activeSelf || !p.send.interactable || !p.details.text.Contains("ケア用品"))throw new Exception("General dispatch navigation failed");
            if(p.send.onClick.GetPersistentTarget(0)!=p || p.destinations.onValueChanged.GetPersistentTarget(0)!=p)throw new Exception("Dispatch callbacks refer to patron panel");
            state.general_dispatch.options[0].can_select=false;state.general_dispatch.options[0].reason="在店していません。";
            state.general_dispatch.events=new[]{new PythonDispatchEvent{cat_id=state.cats[0].cat_id,choice="dispatch-1",label="ミケ / 近所のお店への訪問",status="waiting",reward=120,detail="用品報酬：ケア用品（未受取）",can_select=true}};state.required_action="帰還報酬を受け取ってください。";apply();
            if(!p.receive.interactable || p.send.interactable || !p.progress.text.Contains("120"))throw new Exception("General return guard failed");
            Canvas.ForceUpdateCanvases();if(p.details.preferredHeight>p.details.rectTransform.rect.height || p.progress.preferredHeight>p.progress.rectTransform.rect.height)throw new Exception("General dispatch text clipped");
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/GeneralDispatch.png"});
            p.Disconnect();if(p.receive.interactable)throw new Exception("Offline return enabled");apply();
            state.general_dispatch.events[0].status="resolved";state.general_dispatch.events[0].can_select=false;state.general_dispatch.events[0].detail="用品報酬：ケア用品（受取済み）";apply();
            if(p.receive.interactable || !p.progress.text.Contains("受取済み"))throw new Exception("General return history failed");
            p.Close();apply();if(p.panel.activeSelf)throw new Exception("Resolved return reopened");state.instance_id="loaded-game";apply();if(p.panel.activeSelf)throw new Exception("General dispatch retained across load");
            Debug.Log("CAT_CAFE_GENERAL_DISPATCH_DISPLAY_OK");
        }
    }
}
