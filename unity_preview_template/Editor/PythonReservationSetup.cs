using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonReservationSetup
    {
        static T Copy<T>(T source,Transform root,Transform copy) where T:Component{return copy.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>();}
        static void Bind(Button button,UnityEngine.Events.UnityAction action){button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,action);}
        [MenuItem("Cat Cafe/開発用/UI再生成/特別予約",false,23)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var source=preview.GetComponent<PythonRegularIntroductionPanel>();var root=source.panel.transform.parent;
            var old=root.Find("Reservation");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);var panel=preview.GetComponent<PythonReservationPanel>() ?? preview.gameObject.AddComponent<PythonReservationPanel>();
            panel.panel=UnityEngine.Object.Instantiate(source.panel,root);panel.panel.name="Reservation";var target=panel.panel.transform;var origin=source.panel.transform;
            panel.title=Copy(source.title,origin,target);panel.details=Copy(source.details,origin,target);panel.notice=Copy(source.notice,origin,target);panel.confirmationText=Copy(source.confirmationText,origin,target);
            panel.confirmation=target.Find(AnimationUtility.CalculateTransformPath(source.confirmation.transform,origin)).gameObject;
            panel.accept=Copy(source.accept,origin,target);panel.decline=Copy(source.decline,origin,target);panel.close=Copy(source.close,origin,target);panel.confirm=Copy(source.confirm,origin,target);panel.cancel=Copy(source.cancel,origin,target);
            UnityEngine.Object.DestroyImmediate(Copy(source.picture,origin,target).transform.parent.gameObject);UnityEngine.Object.DestroyImmediate(Copy(source.housing,origin,target).gameObject);
            var scroll=panel.details.transform.parent.parent.GetComponent<RectTransform>();scroll.anchoredPosition=new Vector2(24,-86);scroll.sizeDelta=new Vector2(892,336);
            var rect=panel.details.transform.parent.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(862,324);panel.details.rectTransform.sizeDelta=new Vector2(862,324);
            scroll.GetComponent<ScrollRect>().verticalScrollbar.GetComponent<RectTransform>().anchoredPosition=new Vector2(874,-6);
            panel.close.GetComponent<RectTransform>().anchoredPosition=new Vector2(24,-550);panel.close.GetComponent<RectTransform>().sizeDelta=new Vector2(892,40);
            panel.accept.GetComponentInChildren<Text>().text="引き受ける（確認へ）";panel.decline.GetComponentInChildren<Text>().text="辞退する（確認へ）";
            Bind(panel.accept,panel.Accept);Bind(panel.decline,panel.Decline);Bind(panel.close,panel.Close);Bind(panel.confirm,panel.Confirm);Bind(panel.cancel,panel.Cancel);
            panel.confirmation.SetActive(false);panel.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PythonEventListSetup.Build();Debug.Log("CAT_CAFE_RESERVATION_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="reservation-test";state.can_set_shifts=true;state.required_action="予約への回答が必要です。";state.reservation=new PythonReservation{event_id="reservation-3",name="長毛猫との時間を望む予約客",description="長毛の猫とゆっくり過ごしたいお客さん",status="waiting",offered_day=3,visit_day=4,open_up_count=2,bonus=100,popularity_bonus=5,can_accept=true,can_decline=true};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var panel=preview.GetComponent<PythonReservationPanel>();
            if(!panel.panel.activeSelf || !panel.accept.interactable || !panel.details.text.Contains("4日目"))throw new Exception("Reservation display failed");
            panel.Accept();if(!panel.confirmation.activeSelf || !panel.confirmationText.text.Contains("休業"))throw new Exception("Reservation confirmation failed");panel.Cancel();
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/Reservation.png"});
            panel.Decline();state.revision++;apply();if(panel.confirmation.activeSelf)throw new Exception("Stale reservation confirmation remained");panel.Disconnect();if(panel.accept.interactable || panel.confirm.interactable)throw new Exception("Offline reservation enabled");apply();
            var list=preview.GetComponent<PythonEventListPanel>();list.Open();list.Reservation();if(list.panel.activeSelf || !panel.panel.activeSelf)throw new Exception("Reservation navigation failed");
            state.reservation.status="accepted";state.reservation.result="success";state.reservation.resolved_day=3;state.reservation.can_accept=state.reservation.can_decline=false;apply();if(panel.accept.interactable || !panel.details.text.Contains("条件達成"))throw new Exception("Reservation history failed");
            state.reservation=null;apply();if(panel.panel.activeSelf)throw new Exception("Legacy reservation visible");Debug.Log("CAT_CAFE_RESERVATION_DISPLAY_OK");
        }
    }
}
