using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonDispatchChoiceSetup
    {
        static T Copy<T>(T source,Transform root,Transform target) where T:Component
        {return target.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>();}
        static void Place(RectTransform rect,float x,float y,float w,float h)
        {rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);}
        static void Bind(Button button,UnityEngine.Events.UnityAction action)
        {button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,action);}
        [MenuItem("Cat Cafe/開発用/UI再生成/派遣中の選択イベント",false,26)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();
            var source=preview.GetComponent<PythonReservationPanel>();var root=source.panel.transform.parent;var old=root.Find("Dispatch Choice");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var p=preview.GetComponent<PythonDispatchChoicePanel>() ?? preview.gameObject.AddComponent<PythonDispatchChoicePanel>();p.panel=UnityEngine.Object.Instantiate(source.panel,root);p.panel.name="Dispatch Choice";
            var t=p.panel.transform;var s=source.panel.transform;p.title=Copy(source.title,s,t);p.details=Copy(source.details,s,t);p.notice=Copy(source.notice,s,t);p.confirmationText=Copy(source.confirmationText,s,t);
            p.confirmation=t.Find(AnimationUtility.CalculateTransformPath(source.confirmation.transform,s)).gameObject;
            p.first=Copy(source.accept,s,t);p.second=Copy(source.decline,s,t);p.close=Copy(source.close,s,t);p.confirm=Copy(source.confirm,s,t);p.cancel=Copy(source.cancel,s,t);
            p.records=UnityEngine.Object.Instantiate(preview.GetComponent<PythonGeneralDispatchPanel>().destinations,p.title.transform.parent);p.records.name="Records";Place(p.records.GetComponent<RectTransform>(),24,64,892,40);
            p.records.onValueChanged=new Dropdown.DropdownEvent();UnityEventTools.AddPersistentListener(p.records.onValueChanged,p.Select);
            var scroll=p.details.transform.parent.parent.GetComponent<RectTransform>();Place(scroll,24,116,892,304);Place(p.details.transform.parent.GetComponent<RectTransform>(),6,6,862,292);p.details.rectTransform.sizeDelta=new Vector2(862,292);p.details.fontSize=17;
            var sr=scroll.GetComponent<ScrollRect>();sr.content=p.details.rectTransform;sr.verticalScrollbar.GetComponent<RectTransform>().sizeDelta=new Vector2(12,292);
            Place(p.first.GetComponent<RectTransform>(),24,434,432,44);Place(p.second.GetComponent<RectTransform>(),480,434,436,44);
            p.first.GetComponentInChildren<Text>().fontSize=p.second.GetComponentInChildren<Text>().fontSize=16;
            Place(p.confirmationText.rectTransform,40,64,860,360);p.confirmationText.fontSize=19;Place(p.confirm.GetComponent<RectTransform>(),100,480,340,44);Place(p.cancel.GetComponent<RectTransform>(),500,480,340,44);
            Bind(p.first,p.First);Bind(p.second,p.Second);Bind(p.close,p.Close);Bind(p.confirm,p.Confirm);Bind(p.cancel,p.Cancel);
            p.confirmation.transform.SetAsLastSibling();p.confirmation.SetActive(false);p.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PythonEventListSetup.Build();Debug.Log("CAT_CAFE_DISPATCH_CHOICE_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="dispatch-choice-test";state.dispatch_choices=new[]{new PythonDispatchEncounter{event_id="dispatch-1-cat-sora",cat_id="cat-sora",label="ソラ / 商店街の交流会",title="追加の交流をお願いされました",status="scheduled",has_stress=true,return_reward=260,choices=new[]{new PythonDispatchEncounterOption{choice="accept",label="追加の仕事を引き受ける",reward_delta=100,fatigue=10,stress=5,return_reward=360},new PythonDispatchEncounterOption{choice="decline",label="予定どおりの仕事を終える",return_reward=260}}}};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var p=preview.GetComponent<PythonDispatchChoicePanel>();var list=preview.GetComponent<PythonEventListPanel>();list.Open();list.DispatchChoice();
            if(list.panel.activeSelf || !p.panel.activeSelf || p.first.interactable)throw new Exception("Scheduled choice navigation failed");
            var row=state.dispatch_choices[0];row.status="waiting";row.occurred_day=1;row.choices[0].can_select=row.choices[1].can_select=true;row.choices[0].changes=new PythonDispatchChanges{fatigue_before=95,fatigue_after=100,stress_before=98,stress_after=100};state.required_action="派遣中の選択イベントに回答してください。";state.revision++;apply();
            if(!p.first.interactable || !p.second.interactable || !p.details.text.Contains("95 → 100"))throw new Exception("Waiting choice effects failed");p.First();if(!p.confirmation.activeSelf || !p.confirm.interactable || !p.confirmationText.text.Contains("360"))throw new Exception("Choice confirmation failed");
            Canvas.ForceUpdateCanvases();if(p.confirmationText.preferredHeight>p.confirmationText.rectTransform.rect.height)throw new Exception("Choice confirmation clipped");
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/DispatchChoiceConfirmation.png"});
            p.Cancel();if(p.confirmation.activeSelf || typeof(PythonStateClient).GetField("pending",flags).GetValue(client)!=null)throw new Exception("Choice cancel queued action");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/DispatchChoice.png"});
            p.Second();state.revision++;apply();if(p.confirmation.activeSelf)throw new Exception("Stale choice confirmation remained");p.Disconnect();if(p.first.interactable || p.confirm.interactable)throw new Exception("Offline choice enabled");apply();
            if(p.first.onClick.GetPersistentTarget(0)!=p || p.records.onValueChanged.GetPersistentTarget(0)!=p)throw new Exception("Choice callbacks incorrectly bound");
            list.Open();preview.RefreshPythonControls();if(list.panel.transform.GetSiblingIndex()!=list.panel.transform.parent.childCount-1)throw new Exception("Choice covered event list");list.DispatchChoice();
            row.status="resolved";row.resolved_day=1;row.selected_label=row.choices[0].label;row.changes=row.choices[0].changes;row.choices[0].can_select=row.choices[1].can_select=false;state.revision++;apply();if(p.first.interactable || !p.details.text.Contains("回答済み"))throw new Exception("Choice history failed");
            p.Close();apply();if(p.panel.activeSelf)throw new Exception("Answered choice reopened");state.instance_id="loaded-game";apply();if(p.panel.activeSelf)throw new Exception("Choice persisted across load");var legacy=JsonUtility.FromJson<PythonDispatchEncounter>("{\"has_stress\":false,\"changes\":{\"fatigue_before\":0,\"fatigue_after\":0,\"stress_before\":null,\"stress_after\":null}}");if(legacy.has_stress || legacy.changes==null)throw new Exception("Legacy choice state parse failed");
            row.status="waiting";row.has_stress=false;row.choices[0].can_select=row.choices[1].can_select=true;state.revision++;apply();p.First();if(!p.confirmationText.text.Contains("対象外"))throw new Exception("Legacy stress preview failed");p.Confirm();var queued=(PythonShiftCommand)typeof(PythonStateClient).GetField("pending",flags).GetValue(client);
            if(queued==null || queued.kind!="resolve_dispatch_choice" || queued.event_id!=row.event_id || queued.choice!="accept" || p.first.interactable || p.second.interactable)throw new Exception("Dispatch choice queue failed");
            Debug.Log("CAT_CAFE_DISPATCH_CHOICE_DISPLAY_OK");
        }
    }
}
