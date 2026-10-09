using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonPersistSetup
    {
        static T Copy<T>(T source,Transform root,Transform target) where T:Component=>target.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>();
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Bind(Button b,UnityEngine.Events.UnityAction action){b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,action);b.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);}
        [MenuItem("Cat Cafe/開発用/UI再生成/交流結果の保存再試行",false,43)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var source=preview.GetComponent<PythonDayOffPanel>();var root=source.panel.transform.parent;
            foreach(var name in new[]{"Pending Results","Pending Results Activity"}){var old=root.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            var p=preview.GetComponent<PythonPersistPanel>() ?? preview.gameObject.AddComponent<PythonPersistPanel>();p.panel=UnityEngine.Object.Instantiate(source.panel,root);p.panel.name="Pending Results";var s=source.panel.transform;var t=p.panel.transform;p.title=Copy(source.title,s,t);p.details=Copy(source.details,s,t);p.notice=Copy(source.notice,s,t);p.retry=Copy(source.proceed,s,t);p.close=Copy(source.close,s,t);UnityEngine.Object.DestroyImmediate(t.Find(AnimationUtility.CalculateTransformPath(source.confirmation.transform,s)).gameObject);
            Bind(p.retry,p.Retry);Bind(p.close,p.Close);p.retry.GetComponentInChildren<Text>().text="保存を再試行";p.close.GetComponentInChildren<Text>().text="営業画面へ";p.details.fontSize=18;p.notice.fontSize=15;p.notice.supportRichText=false;p.panel.SetActive(false);
            var auto=preview.GetComponent<PythonAutoPlayPanel>();p.activity=UnityEngine.Object.Instantiate(auto.activity,auto.activity.transform.parent);p.activity.name="Pending Results Activity";p.activityStatus=Copy(auto.activityStatus,auto.activity.transform,p.activity.transform);p.activityDetails=Copy(auto.activityDetails,auto.activity.transform,p.activity.transform);UnityEngine.Object.DestroyImmediate(Copy(auto.activityStop,auto.activity.transform,p.activity.transform).gameObject);Place(p.activityStatus.rectTransform,12,0,468,40);Place(p.activityDetails.GetComponent<RectTransform>(),492,0,256,40);p.activityDetails.GetComponentInChildren<Text>().text="未保存の結果・再試行";Bind(p.activityDetails,p.Open);p.activity.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_PERSIST_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="pending-ui";state.pending_results=new PythonPendingResults{count=2,reason="ディスクに空き容量がありません。",notes="会計・体力・報酬は反映済みです。再試行は未保存の結果だけを処理します。",rows=new[]{new PythonPendingResult{session_id="one",details="猫：タマ / お客：佐藤さん\n親しみ：0 → 1.5（増減 +1.5）\n交流ID：one"},new PythonPendingResult{session_id="two",details="猫：ミケ / お客：田中さん\n親しみ：3 → 4（増減 +1）\n交流ID：two"}}};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var p=preview.GetComponent<PythonPersistPanel>();if(!p.panel.activeSelf || !p.activity.activeSelf || !p.retry.interactable || client.CanManageSaves || client.CanChangeShifts || client.CanProgressBusiness || !p.details.text.Contains("空き容量") || !p.details.text.Contains("ミケ"))throw new Exception("Pending display or gate failed");Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/PendingResults.png"});
            p.Close();apply();if(p.panel.activeSelf || !p.activity.activeSelf)throw new Exception("Closed recovery window reopened");p.activityDetails.onClick.Invoke();p.Disconnect();if(p.retry.interactable || !p.notice.text.Contains("再接続"))throw new Exception("Offline retry enabled");apply();p.retry.onClick.Invoke();var command=(PythonShiftCommand)typeof(PythonStateClient).GetField("pending",flags).GetValue(client);if(command?.kind!="persist_retry" || command.working_cats.Length!=0 || p.retry.interactable)throw new Exception("Retry command incorrect");p.Retry();if(!ReferenceEquals(command,typeof(PythonStateClient).GetField("pending",flags).GetValue(client)))throw new Exception("Duplicate retry queued");typeof(PythonStateClient).GetField("pending",flags).SetValue(client,null);
            state.pending_results.count=0;apply();if(p.activity.activeSelf || p.retry.interactable || !client.CanManageSaves || !p.details.text.Contains("ありません"))throw new Exception("Recovery did not unlock");state.instance_id="new-game";apply();if(p.panel.activeSelf)throw new Exception("Loaded game kept recovery modal");Debug.Log("CAT_CAFE_PERSIST_DISPLAY_OK");
        }
    }
}
