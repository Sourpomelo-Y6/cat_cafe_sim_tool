using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonDispatchIntroductionSetup
    {
        static T Copy<T>(T source,Transform root,Transform target) where T:Component
        {return target.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>();}
        static void Place(RectTransform rect,float x,float y,float w,float h)
        {rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);}
        static void Bind(Button button,UnityEngine.Events.UnityAction action)
        {button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,action);}
        [MenuItem("Cat Cafe/開発用/UI再生成/派遣先からの猫紹介",false,28)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var source=preview.GetComponent<PythonRegularIntroductionPanel>();
            var root=source.panel.transform.parent;var old=root.Find("Dispatch Introduction");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var p=preview.GetComponent<PythonDispatchIntroductionPanel>() ?? preview.gameObject.AddComponent<PythonDispatchIntroductionPanel>();p.panel=UnityEngine.Object.Instantiate(source.panel,root);p.panel.name="Dispatch Introduction";var s=source.panel.transform;var t=p.panel.transform;
            p.picture=Copy(source.picture,s,t);p.title=Copy(source.title,s,t);p.details=Copy(source.details,s,t);p.notice=Copy(source.notice,s,t);p.confirmationText=Copy(source.confirmationText,s,t);
            p.confirmation=t.Find(AnimationUtility.CalculateTransformPath(source.confirmation.transform,s)).gameObject;p.accept=Copy(source.accept,s,t);p.decline=Copy(source.decline,s,t);p.housing=Copy(source.housing,s,t);p.close=Copy(source.close,s,t);p.confirm=Copy(source.confirm,s,t);p.cancel=Copy(source.cancel,s,t);
            p.records=UnityEngine.Object.Instantiate(preview.GetComponent<PythonGeneralDispatchPanel>().destinations,p.title.transform.parent);p.records.name="Records";Place(p.records.GetComponent<RectTransform>(),24,70,892,40);p.records.onValueChanged=new Dropdown.DropdownEvent();UnityEventTools.AddPersistentListener(p.records.onValueChanged,p.Select);
            Place(p.picture.transform.parent.GetComponent<RectTransform>(),24,120,272,272);var scroll=p.details.transform.parent.parent.GetComponent<RectTransform>();Place(scroll,316,120,600,302);Place(p.details.transform.parent.GetComponent<RectTransform>(),8,6,570,290);scroll.GetComponent<ScrollRect>().verticalScrollbar.GetComponent<RectTransform>().sizeDelta=new Vector2(12,290);
            Place(p.confirmationText.rectTransform,40,76,860,330);p.confirmationText.fontSize=19;Place(p.confirm.GetComponent<RectTransform>(),100,480,340,44);Place(p.cancel.GetComponent<RectTransform>(),500,480,340,44);
            Bind(p.accept,p.Accept);Bind(p.decline,p.Decline);Bind(p.housing,p.Housing);Bind(p.close,p.Close);Bind(p.confirm,p.Confirm);Bind(p.cancel,p.Cancel);
            p.confirmation.transform.SetAsLastSibling();p.confirmation.SetActive(false);p.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PythonEventListSetup.Build();Debug.Log("CAT_CAFE_DISPATCH_INTRODUCTION_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",f).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="dispatch-introduction-test";state.can_set_shifts=true;state.dispatch_introductions=new[]{new PythonDispatchIntroduction{event_id="dispatch-1-cat-mike",cat_id="dispatch-rescue-1",content_cat_id="dispatch-rescue-1",name="ユキ（派遣紹介1）",destination="商店街の交流会",sender_name="ミケ",status="scheduled",cost=200,funds_after=800,housing="5 / 6匹",personality="甘えん坊",features="白い毛・長毛",trait="特性：接客好き",preferences="好み：遊び 1\n好み：触れ合い 1"}};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",f).Invoke(client,new object[]{state});apply();var p=preview.GetComponent<PythonDispatchIntroductionPanel>();var list=preview.GetComponent<PythonEventListPanel>();list.Open();list.DispatchIntroduction();if(!p.panel.activeSelf || list.panel.activeSelf || p.accept.interactable || p.decline.interactable)throw new Exception("Scheduled introduction navigation failed");
            if(p.picture.texture!=ContentPreview.CatPlaceholderTexture())throw new Exception("Introduction silhouette missing");
            var row=state.dispatch_introductions[0];row.status="waiting";row.presented_day=3;row.can_accept=row.can_decline=true;state.required_action="派遣先からの猫紹介に回答してください。";state.revision++;apply();if(!p.accept.interactable || !p.decline.interactable || !p.details.text.Contains("商店街"))throw new Exception("Introduction conditions missing");
            p.Accept();if(!p.confirm.interactable || !p.confirmationText.text.Contains("200") || !p.confirmationText.text.Contains("800"))throw new Exception("Introduction confirmation failed");Canvas.ForceUpdateCanvases();if(p.confirmationText.preferredHeight>p.confirmationText.rectTransform.rect.height)throw new Exception("Introduction confirmation clipped");
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/DispatchIntroductionConfirmation.png"});p.Cancel();if(p.confirmation.activeSelf || typeof(PythonStateClient).GetField("pending",f).GetValue(client)!=null)throw new Exception("Introduction cancel queued command");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/DispatchIntroduction.png"});
            row.can_accept=false;row.accept_reason="飼育枠が不足しています。";state.revision++;apply();if(p.accept.interactable || !p.decline.interactable || !p.housing.interactable)throw new Exception("Introduction housing guard failed");p.Housing();if(!preview.GetComponent<PythonHousingPanel>().panel.activeSelf)throw new Exception("Housing navigation failed");preview.GetComponent<PythonHousingPanel>().Close();
            p.Decline();state.revision++;apply();if(p.confirmation.activeSelf)throw new Exception("Stale introduction confirmation remained");p.Disconnect();if(p.decline.interactable || p.confirm.interactable)throw new Exception("Offline introduction enabled");apply();
            list.Open();var scroll=list.panel.GetComponentInChildren<ScrollRect>();Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();if(list.entries.Length!=14 || scroll.content.rect.height<=scroll.viewport.rect.height)throw new Exception("Event list scrolling missing");var viewportBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,scroll.viewport);var buttonBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,list.entries[13].transform);if(buttonBounds.min.y<viewportBounds.min.y-1 || buttonBounds.max.y>viewportBounds.max.y+1)throw new Exception("Last event button unreachable");
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/EventListDispatchIntroduction.png"});list.DispatchIntroduction();if(list.panel.activeSelf || !p.panel.activeSelf)throw new Exception("Introduction navigation failed");
            if(p.accept.onClick.GetPersistentTarget(0)!=p || p.records.onValueChanged.GetPersistentTarget(0)!=p)throw new Exception("Introduction callbacks incorrect");
            row.status="accepted";row.resolved_day=3;row.can_accept=row.can_decline=false;state.revision++;apply();if(p.accept.interactable || !p.details.text.Contains("迎えました"))throw new Exception("Introduction history failed");p.Close();apply();if(p.panel.activeSelf)throw new Exception("Answered introduction reopened");state.instance_id="loaded-game";apply();if(p.panel.activeSelf)throw new Exception("Introduction retained across load");
            row.status="waiting";row.can_decline=true;state.revision++;apply();p.Decline();if(!p.confirmationText.text.Contains("派遣報酬"))throw new Exception("Decline consequence incorrect");p.Confirm();var queued=(PythonShiftCommand)typeof(PythonStateClient).GetField("pending",f).GetValue(client);if(queued==null || queued.kind!="resolve_dispatch_introduction" || queued.event_id!=row.event_id || queued.choice!="decline" || p.decline.interactable)throw new Exception("Introduction queue failed");Debug.Log("CAT_CAFE_DISPATCH_INTRODUCTION_DISPLAY_OK");
        }
    }
}
