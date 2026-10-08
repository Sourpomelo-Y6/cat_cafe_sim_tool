using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonDispatchTroubleSetup
    {
        static T Copy<T>(T source,Transform root,Transform target) where T:Component
        {return target.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>();}
        static void Bind(Button button,UnityEngine.Events.UnityAction action)
        {button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,action);}
        [MenuItem("Cat Cafe/開発用/UI再生成/山あいの宿のトラブル",false,27)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var source=preview.GetComponent<PythonDispatchChoicePanel>();
            var root=source.panel.transform.parent;var old=root.Find("Dispatch Trouble");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var p=preview.GetComponent<PythonDispatchTroublePanel>() ?? preview.gameObject.AddComponent<PythonDispatchTroublePanel>();p.panel=UnityEngine.Object.Instantiate(source.panel,root);p.panel.name="Dispatch Trouble";var s=source.panel.transform;var t=p.panel.transform;
            p.title=Copy(source.title,s,t);p.details=Copy(source.details,s,t);p.notice=Copy(source.notice,s,t);p.confirmationText=Copy(source.confirmationText,s,t);p.records=Copy(source.records,s,t);
            p.confirmation=t.Find(AnimationUtility.CalculateTransformPath(source.confirmation.transform,s)).gameObject;p.first=Copy(source.first,s,t);p.second=Copy(source.second,s,t);p.close=Copy(source.close,s,t);p.confirm=Copy(source.confirm,s,t);p.cancel=Copy(source.cancel,s,t);
            p.records.onValueChanged=new Dropdown.DropdownEvent();UnityEventTools.AddPersistentListener(p.records.onValueChanged,p.Select);
            Bind(p.first,p.First);Bind(p.second,p.Second);Bind(p.close,p.Close);Bind(p.confirm,p.Confirm);Bind(p.cancel,p.Cancel);
            p.close.GetComponent<RectTransform>().anchoredPosition=new Vector2(480,-550);p.close.GetComponent<RectTransform>().sizeDelta=new Vector2(436,40);
            p.viewReturn=UnityEngine.Object.Instantiate(p.close,p.close.transform.parent);p.viewReturn.name="View Return";p.viewReturn.GetComponent<RectTransform>().anchoredPosition=new Vector2(24,-550);p.viewReturn.GetComponent<RectTransform>().sizeDelta=new Vector2(432,40);p.viewReturn.GetComponentInChildren<Text>().text="派遣・帰還を確認";Bind(p.viewReturn,p.ViewReturn);
            p.confirmation.transform.SetAsLastSibling();p.confirmation.SetActive(false);p.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PythonEventListSetup.Build();Debug.Log("CAT_CAFE_DISPATCH_TROUBLE_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",f).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="dispatch-trouble-test";state.dispatch_troubles=new[]{new PythonDispatchTrouble{event_id="dispatch-1-cat-tama",cat_id="cat-tama",label="タマ / 山あいの宿への訪問",status="scheduled",activity_status="travelling",description="出発時のトラブル発生確率：15% / 出発時のストレス：0\n派遣1日目の閉店で発生を判定します。",choices=new[]{new PythonDispatchTroubleOption{choice="search",label="捜索して連れ戻す",cost=100,funds_after=900,description="捜索後、すぐに帰還確認へ進めます。"},new PythonDispatchTroubleOption{choice="wait",label="帰還を待つ（費用なし）",funds_after=1000,remaining=2,description="費用なしで待ちます。家出した日から2日後の閉店で帰還確認へ進めます。"}}}};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",f).Invoke(client,new object[]{state});apply();var p=preview.GetComponent<PythonDispatchTroublePanel>();var list=preview.GetComponent<PythonEventListPanel>();list.Open();list.DispatchTrouble();if(list.panel.activeSelf || !p.panel.activeSelf || p.first.interactable)throw new Exception("Scheduled trouble navigation failed");
            var row=state.dispatch_troubles[0];row.status="waiting";row.activity_status="missing";row.missing_day=1;row.description="1日目、派遣中に猫が驚いて宿の周囲へ出ていきました。派遣は中断し、報酬は0です。\n\n捜索費：100 / 支払い後に資金が残る必要があります。\n費用なしで待つ場合：家出した日から2日後に帰還確認。\n帰還まで接客・交流・別の派遣には参加できません。\n帰還を確認するとストレスは30になります。人気低下や子猫の引き渡し費用はありません。\n\n現在の猫：行方不明 / 疲労 0 / ストレス 0\n帰還まで、あと2回の閉店です。\n現在の帰還報酬：0 / 現在の資金：1000";row.choices[0].can_select=row.choices[1].can_select=true;state.required_action="宿のトラブルに回答してください。";state.revision++;apply();p.First();if(!p.confirm.interactable || !p.confirmationText.text.Contains("900"))throw new Exception("Search cost confirmation failed");
            Canvas.ForceUpdateCanvases();if(p.confirmationText.preferredHeight>p.confirmationText.rectTransform.rect.height)throw new Exception("Trouble confirmation clipped");typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/DispatchTroubleConfirmation.png"});p.Cancel();
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/DispatchTrouble.png"});
            row.choices[0].can_select=false;row.choices[0].reason="捜索後に資金が残りません。費用なしの帰還待ちを選べます。";state.revision++;apply();if(p.first.interactable || !p.second.interactable)throw new Exception("Search funds boundary guard failed");p.Second();if(!p.confirmationText.text.Contains("2日後"))throw new Exception("Wait duration missing");state.revision++;apply();if(p.confirmation.activeSelf)throw new Exception("Stale trouble confirmation remained");p.Disconnect();if(p.second.interactable || p.viewReturn.interactable)throw new Exception("Offline trouble enabled");apply();
            if(p.first.onClick.GetPersistentTarget(0)!=p || p.records.onValueChanged.GetPersistentTarget(0)!=p)throw new Exception("Trouble callbacks incorrect");
            row.status="resolved";row.choice="search";row.choice_day=2;row.cost=100;row.activity_status="waiting";row.choices[0].can_select=row.choices[1].can_select=false;state.revision++;state.general_dispatch=new PythonPatronDispatch{options=new PythonDispatchOption[0],events=new[]{new PythonDispatchEvent{choice=row.event_id,cat_id=row.cat_id,label=row.label,status="waiting",can_select=true}}};apply();if(p.second.interactable || !p.details.text.Contains("費用 100"))throw new Exception("Trouble history failed");p.ViewReturn();if(p.panel.activeSelf || !preview.GetComponent<PythonGeneralDispatchPanel>().panel.activeSelf)throw new Exception("Trouble return navigation failed");
            row.status="not_triggered";state.revision++;apply();p.Open();if(p.first.interactable || !p.notice.text.Contains("発生しませんでした"))throw new Exception("No trouble guard failed");p.Close();apply();if(p.panel.activeSelf)throw new Exception("Resolved trouble reopened");
            state.instance_id="loaded-game";apply();if(p.panel.activeSelf)throw new Exception("Trouble retained across load");row.status="waiting";row.choices[1].can_select=true;state.revision++;apply();p.Second();p.Confirm();var queued=(PythonShiftCommand)typeof(PythonStateClient).GetField("pending",f).GetValue(client);if(queued==null || queued.kind!="resolve_dispatch_trouble" || queued.choice!="wait" || queued.event_id!=row.event_id || p.second.interactable)throw new Exception("Trouble command queue failed");Debug.Log("CAT_CAFE_DISPATCH_TROUBLE_DISPLAY_OK");
        }
    }
}
