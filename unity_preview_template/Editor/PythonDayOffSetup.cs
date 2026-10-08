using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonDayOffSetup
    {
        static T Copy<T>(T source,Transform root,Transform target) where T:Component{return target.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>();}
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Bind(Button button,UnityEngine.Events.UnityAction action){button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,action);}
        [MenuItem("Cat Cafe/開発用/UI再生成/通常の休業",false,30)]
        public static void Build()
        {
            PythonPreparationSetup.Build();var scene=EditorSceneManager.GetActiveScene();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var source=preview.GetComponent<PythonDispatchChoicePanel>();var root=source.panel.transform.parent;var old=root.Find("Day Off");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var p=preview.GetComponent<PythonDayOffPanel>() ?? preview.gameObject.AddComponent<PythonDayOffPanel>();p.panel=UnityEngine.Object.Instantiate(source.panel,root);p.panel.name="Day Off";var s=source.panel.transform;var t=p.panel.transform;
            p.title=Copy(source.title,s,t);p.details=Copy(source.details,s,t);p.notice=Copy(source.notice,s,t);p.confirmationText=Copy(source.confirmationText,s,t);p.confirmation=t.Find(AnimationUtility.CalculateTransformPath(source.confirmation.transform,s)).gameObject;
            p.proceed=Copy(source.first,s,t);p.close=Copy(source.close,s,t);p.confirm=Copy(source.confirm,s,t);p.cancel=Copy(source.cancel,s,t);Copy(source.second,s,t).gameObject.SetActive(false);Copy(source.records,s,t).gameObject.SetActive(false);
            var scroll=p.details.transform.parent.parent.GetComponent<ScrollRect>();Place(scroll.GetComponent<RectTransform>(),24,78,892,404);Place(scroll.viewport,6,6,862,392);scroll.verticalScrollbar.GetComponent<RectTransform>().sizeDelta=new Vector2(12,392);
            Place(p.proceed.GetComponent<RectTransform>(),24,550,432,40);Place(p.close.GetComponent<RectTransform>(),480,550,436,40);p.proceed.GetComponentInChildren<Text>().text="この内容で休業する（確認へ）";
            Bind(p.proceed,p.Choose);Bind(p.close,p.Close);Bind(p.confirm,p.Confirm);Bind(p.cancel,p.Cancel);p.confirmation.SetActive(false);p.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_DAY_OFF_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",f).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="day-off-test";state.can_set_shifts=true;state.day=1;state.closed=false;state.required_action="";state.day_off=new PythonDayOff{can_select=true,cost=100,funds_after=900,day_after=2,details="来客・接客はありません。日次運営費は発生します。\n日程：1日目 → 2日目\n資金：1000 → 900\n\nミケ\n体力 70 → 100 / 疲労 30 → 10 / ストレス 20 → 0\n健康：健康 → 健康"};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",f).Invoke(client,new object[]{state});apply();var prep=preview.GetComponent<PythonPreparationPanel>();var p=preview.GetComponent<PythonDayOffPanel>();Canvas.ForceUpdateCanvases();foreach(var button in new[]{prep.start,prep.dayOff,prep.close}){var label=button.GetComponentInChildren<Text>();if(label.preferredWidth>label.rectTransform.rect.width)throw new Exception("Preparation button clipped");}typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/PreparationDayOff.png"});prep.OpenDayOff();if(!p.panel.activeSelf || !p.proceed.interactable || !p.details.text.Contains("30 → 10"))throw new Exception("Day off preparation entry failed");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/DayOff.png"});
            p.Choose();if(!p.confirmation.activeSelf || !p.confirmationText.text.Contains("900") || !p.confirmationText.text.Contains("2日目"))throw new Exception("Day off confirmation missing");Canvas.ForceUpdateCanvases();if(p.confirmationText.preferredHeight>p.confirmationText.rectTransform.rect.height)throw new Exception("Day off confirmation clipped");typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/DayOffConfirmation.png"});p.Cancel();if(typeof(PythonStateClient).GetField("pending",f).GetValue(client)!=null)throw new Exception("Day off cancel changed state");
            p.Choose();state.revision++;apply();if(p.confirmation.activeSelf)throw new Exception("Stale day off confirmation remained");p.Disconnect();if(p.proceed.interactable || p.confirm.interactable)throw new Exception("Offline day off enabled");apply();state.day_off.can_select=false;state.day_off.reason="受け入れた特別予約があるため休業できません。";state.revision++;apply();if(p.proceed.interactable || !p.details.text.Contains("特別予約"))throw new Exception("Reserved day off enabled");
            state.day_off.can_select=true;state.day_off.reason="";state.day_off.ends_game=true;state.day_off.funds_after=0;state.revision++;apply();p.Choose();if(!p.confirmationText.text.Contains("営業が終了"))throw new Exception("Ending warning missing");p.Cancel();state.day++;apply();if(p.panel.activeSelf)throw new Exception("Day off retained across next day");state.instance_id="loaded";apply();if(p.panel.activeSelf)throw new Exception("Day off retained across load");state.can_set_shifts=false;apply();if(prep.dayOff.gameObject.activeInHierarchy)throw new Exception("Day off entry remained in business");
            state.can_set_shifts=true;state.day_off.ends_game=false;apply();p.Open();p.Choose();p.Confirm();var queued=(PythonShiftCommand)typeof(PythonStateClient).GetField("pending",f).GetValue(client);if(queued==null || queued.kind!="day_off" || p.proceed.interactable)throw new Exception("Day off command queue failed");Debug.Log("CAT_CAFE_DAY_OFF_DISPLAY_OK");
        }
    }
}
