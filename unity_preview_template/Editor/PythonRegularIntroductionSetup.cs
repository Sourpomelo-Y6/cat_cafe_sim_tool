using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonRegularIntroductionSetup
    {
        static T Copy<T>(T source,Transform root,Transform copy) where T:Component
        { return copy.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>(); }
        static void Bind(Button button,UnityEngine.Events.UnityAction action)
        { button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,action); }
        [MenuItem("Cat Cafe/開発用/UI再生成/常連からの猫紹介",false,22)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var source=preview.GetComponent<PythonVisitingCatPanel>();
            var root=source.panel.transform.parent;var old=root.Find("Regular Introduction");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var panel=preview.GetComponent<PythonRegularIntroductionPanel>() ?? preview.gameObject.AddComponent<PythonRegularIntroductionPanel>();
            panel.panel=UnityEngine.Object.Instantiate(source.panel,root);panel.panel.name="Regular Introduction";var target=panel.panel.transform;var origin=source.panel.transform;
            panel.picture=Copy(source.picture,origin,target);panel.title=Copy(source.title,origin,target);panel.details=Copy(source.details,origin,target);panel.notice=Copy(source.notice,origin,target);panel.confirmationText=Copy(source.confirmationText,origin,target);
            panel.confirmation=target.Find(AnimationUtility.CalculateTransformPath(source.confirmation.transform,origin)).gameObject;
            panel.accept=Copy(source.accept,origin,target);panel.decline=Copy(source.skip,origin,target);panel.housing=Copy(source.housing,origin,target);panel.close=Copy(source.close,origin,target);panel.confirm=Copy(source.confirm,origin,target);panel.cancel=Copy(source.cancel,origin,target);
            var unused=Copy(source.interact,origin,target);UnityEngine.Object.DestroyImmediate(unused.gameObject);
            var rect=panel.decline.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(24,-434);rect.sizeDelta=new Vector2(432,40);
            rect=panel.accept.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(480,-434);rect.sizeDelta=new Vector2(436,40);
            panel.decline.GetComponentInChildren<Text>().text="見送る（確認へ）";panel.close.GetComponentInChildren<Text>().text="閉じる";
            Bind(panel.accept,panel.Accept);Bind(panel.decline,panel.Decline);Bind(panel.housing,panel.Housing);Bind(panel.close,panel.Close);Bind(panel.confirm,panel.Confirm);Bind(panel.cancel,panel.Cancel);
            panel.confirmation.SetActive(false);panel.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            PythonEventListSetup.Build();Debug.Log("CAT_CAFE_REGULAR_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="regular-test";state.can_set_shifts=true;state.required_action="常連からの猫紹介に回答してください。";
            var row=new PythonRegularIntroduction{cat_id="regular-cat-1",content_cat_id="regular-cat-1",name="リン",customer_name="常連のお客さん",status="waiting",presented_day=2,cost=200,funds_after=800,personality="穏やかな甘えん坊",housing="在籍5匹 / 空き1匹",can_accept=true,can_decline=true};state.regular_introduction=row;
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var panel=preview.GetComponent<PythonRegularIntroductionPanel>();
            if(!panel.panel.activeSelf || !panel.accept.interactable || panel.picture.texture==null)throw new Exception("Regular introduction display failed");
            if(panel.accept.onClick.GetPersistentTarget(0)!=panel || panel.accept.onClick.GetPersistentMethodName(0)!="Accept")throw new Exception("Regular button binding failed");
            panel.Accept();if(!panel.confirmation.activeSelf || !panel.confirmationText.text.Contains("200"))throw new Exception("Regular confirmation failed");panel.Cancel();if(panel.confirmation.activeSelf)throw new Exception("Regular cancel failed");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/RegularIntroduction.png"});
            panel.Accept();state.revision++;apply();if(panel.confirmation.activeSelf)throw new Exception("Stale regular confirmation remained");
            row.can_accept=false;row.accept_reason="飼育スペースが足りません。";apply();if(panel.accept.interactable || !panel.decline.interactable || !panel.notice.text.Contains("飼育スペース"))throw new Exception("Admission guard failed");
            panel.Disconnect();if(panel.decline.interactable || panel.confirm.interactable)throw new Exception("Regular offline guard failed");apply();
            var list=preview.GetComponent<PythonEventListPanel>();list.Open();list.Regular();if(list.panel.activeSelf || !panel.panel.activeSelf)throw new Exception("Regular event list navigation failed");
            row.status="declined";row.resolved_day=2;row.can_decline=false;state.required_action="";apply();panel.Open();if(!panel.notice.text.Contains("見送りました"))throw new Exception("Regular history missing");
            state.regular_introduction=null;apply();if(panel.panel.activeSelf)throw new Exception("Legacy regular panel visible");Debug.Log("CAT_CAFE_REGULAR_DISPLAY_OK");
        }
    }
}
