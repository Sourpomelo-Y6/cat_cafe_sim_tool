using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonCustomerTrustSetup
    {
        static T Copy<T>(T s,Transform root,Transform target) where T:Component{return target.Find(AnimationUtility.CalculateTransformPath(s.transform,root)).GetComponent<T>();}
        static void Bind(Button b,UnityEngine.Events.UnityAction action){b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,action);}
        [MenuItem("Cat Cafe/開発用/UI再生成/顧客の信頼回復",false,24)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var s=preview.GetComponent<PythonReservationPanel>();var root=s.panel.transform.parent;var old=root.Find("Customer Trust");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var p=preview.GetComponent<PythonCustomerTrustPanel>() ?? preview.gameObject.AddComponent<PythonCustomerTrustPanel>();p.panel=UnityEngine.Object.Instantiate(s.panel,root);p.panel.name="Customer Trust";var t=p.panel.transform;var o=s.panel.transform;
            p.title=Copy(s.title,o,t);p.details=Copy(s.details,o,t);p.notice=Copy(s.notice,o,t);p.confirmationText=Copy(s.confirmationText,o,t);p.confirmation=t.Find(AnimationUtility.CalculateTransformPath(s.confirmation.transform,o)).gameObject;
            p.recover=Copy(s.accept,o,t);p.ignore=Copy(s.decline,o,t);p.close=Copy(s.close,o,t);p.confirm=Copy(s.confirm,o,t);p.cancel=Copy(s.cancel,o,t);
            p.recover.GetComponentInChildren<Text>().text="信頼回復に取り組む（確認へ）";p.ignore.GetComponentInChildren<Text>().text="対応しない（確認へ）";
            Bind(p.recover,p.Recover);Bind(p.ignore,p.Ignore);Bind(p.close,p.Close);Bind(p.confirm,p.Confirm);Bind(p.cancel,p.Cancel);p.panel.SetActive(false);p.confirmation.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PythonEventListSetup.Build();Debug.Log("CAT_CAFE_TRUST_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",f).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="trust-test";state.required_action="信頼回復に回答してください。";state.customer_trust=new PythonCustomerTrust{popularity_loss=10,events=new[]{new PythonTrustEvent{event_id="trust-4-guest-1",customer_id="guest-1",name="常連のお客さん",day=4,suspensions=2,status="waiting",can_respond=true}}};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",f).Invoke(client,new object[]{state});apply();var p=preview.GetComponent<PythonCustomerTrustPanel>();if(!p.panel.activeSelf || !p.recover.interactable)throw new Exception("Trust display failed");p.Ignore();if(!p.confirmationText.text.Contains("永久離脱") || !p.confirmationText.text.Contains("10"))throw new Exception("Trust consequence missing");p.Cancel();
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/CustomerTrust.png"});
            p.Recover();state.revision++;apply();if(p.confirmation.activeSelf)throw new Exception("Trust stale confirmation remained");p.Disconnect();if(p.recover.interactable)throw new Exception("Offline trust enabled");apply();var list=preview.GetComponent<PythonEventListPanel>();list.Open();list.Trust();if(list.panel.activeSelf || !p.panel.activeSelf)throw new Exception("Trust navigation failed");
            state.customer_trust.events[0].status="resolved";state.customer_trust.events[0].outcome="departed";state.customer_trust.events[0].can_respond=false;apply();if(p.recover.interactable || !p.details.text.Contains("永久離脱"))throw new Exception("Trust history failed");state.customer_trust=null;apply();if(p.panel.activeSelf)throw new Exception("Legacy trust visible");Debug.Log("CAT_CAFE_TRUST_DISPLAY_OK");
        }
    }
}
