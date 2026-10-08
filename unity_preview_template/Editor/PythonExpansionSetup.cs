using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonExpansionSetup
    {
        static T Copy<T>(T source,Transform root,Transform target) where T:Component{return target.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>();}
        static void Bind(Button button,UnityEngine.Events.UnityAction action){button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,action);}
        [MenuItem("Cat Cafe/開発用/UI再生成/席数の増設",false,31)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var source=preview.GetComponent<PythonDayOffPanel>();var root=source.panel.transform.parent;var old=root.Find("Seat Expansion");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var p=preview.GetComponent<PythonExpansionPanel>() ?? preview.gameObject.AddComponent<PythonExpansionPanel>();p.panel=UnityEngine.Object.Instantiate(source.panel,root);p.panel.name="Seat Expansion";var s=source.panel.transform;var t=p.panel.transform;
            p.title=Copy(source.title,s,t);p.details=Copy(source.details,s,t);p.notice=Copy(source.notice,s,t);p.confirmationText=Copy(source.confirmationText,s,t);p.confirmation=t.Find(AnimationUtility.CalculateTransformPath(source.confirmation.transform,s)).gameObject;
            p.proceed=Copy(source.proceed,s,t);p.close=Copy(source.close,s,t);p.confirm=Copy(source.confirm,s,t);p.cancel=Copy(source.cancel,s,t);p.proceed.GetComponentInChildren<Text>().text="席を1つ増設する（確認へ）";
            Bind(p.proceed,p.Choose);Bind(p.close,p.Close);Bind(p.confirm,p.Confirm);Bind(p.cancel,p.Cancel);p.confirmation.SetActive(false);p.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PythonEventListSetup.Build();Debug.Log("CAT_CAFE_EXPANSION_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",f).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="expansion-test";state.can_set_shifts=true;state.closed=false;state.required_action="";state.seats=new[]{new PythonSeatState{seat_id="seat-1"},new PythonSeatState{seat_id="seat-2"}};
            state.expansion=new PythonExpansion{seats=2,max_seats=8,next_seats=3,has_next=true,can_expand=true,cost=500,funds_after=500,operating_cost=60,next_operating_cost=70,details="現在の席数：2席 / 上限：8席\n次の増設：2 → 3席\n増設費用：500\n資金：1000 → 500\n日次運営費：60 → 70\n\n増設条件\n営業準備中・必要な確認への回答済み・増設後に資金が残ること。\n4～5席：人気目標の第1段階達成後\n6～8席：第2段階達成後\n\n新しい席は購入直後から使えます。飼育できる猫の上限は変わりません。\n増設当日から、休業日も日次運営費を精算します。\n\n増設履歴\n増設の記録はありません。"};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",f).Invoke(client,new object[]{state});apply();var p=preview.GetComponent<PythonExpansionPanel>();var list=preview.GetComponent<PythonEventListPanel>();list.Open();if(list.entries.Length!=22 || !list.entries[15].interactable || !list.entries[15].GetComponentInChildren<Text>().text.Contains("2席"))throw new Exception("Expansion event entry failed");list.Expansion();if(list.panel.activeSelf || !p.panel.activeSelf || !p.proceed.interactable)throw new Exception("Expansion navigation failed");
            Canvas.ForceUpdateCanvases();Capture(preview,"Logs/SeatExpansion.png");p.Choose();if(!p.confirmationText.text.Contains("2 → 3") || !p.confirmationText.text.Contains("60 → 70"))throw new Exception("Expansion consequences missing");Canvas.ForceUpdateCanvases();if(p.confirmationText.preferredHeight>p.confirmationText.rectTransform.rect.height)throw new Exception("Expansion confirmation clipped");Capture(preview,"Logs/SeatExpansionConfirmation.png");p.Cancel();if(typeof(PythonStateClient).GetField("pending",f).GetValue(client)!=null)throw new Exception("Expansion cancel queued action");
            p.Choose();state.revision++;apply();if(p.confirmation.activeSelf)throw new Exception("Stale expansion confirmation remained");p.Disconnect();if(p.proceed.interactable || p.confirm.interactable)throw new Exception("Offline expansion enabled");apply();state.expansion.seats=3;state.expansion.next_seats=4;state.expansion.can_expand=false;state.expansion.reason="4席への増設は人気目標の第1段階達成後に解放されます。";state.expansion.details="現在の席数：3席\n増設履歴\n1日目：3席へ増設 / 費用 500";state.revision++;apply();if(p.proceed.interactable || !p.notice.text.Contains("第1段階") || !p.details.text.Contains("履歴"))throw new Exception("Expansion gate/history failed");
            var prep=preview.GetComponent<PythonPreparationPanel>();if(prep.dayOff!=null && prep.close.GetComponent<RectTransform>().anchoredPosition.x<prep.dayOff.GetComponent<RectTransform>().anchoredPosition.x+prep.dayOff.GetComponent<RectTransform>().rect.width)throw new Exception("Preparation day off overlapped close");
            p.Close();prep.Close();state.can_set_shifts=false;state.expansion.seats=8;state.expansion.has_next=false;state.expansion.reason="現在追加できる席はありません。";state.customers=new PythonCustomerState[0];state.waiting_count=0;state.seats=new PythonSeatState[8];for(int i=0;i<8;i++)state.seats[i]=new PythonSeatState{seat_id="seat-"+(i+1)};state.revision++;apply();var service=preview.GetComponent<PythonServiceScreen>();service.ShowSeats();if(!service.GetSeatCard(7).gameObject.activeSelf || !service.GetSeatCard(7).catName.text.Contains("8"))throw new Exception("Eighth seat not displayed");Canvas.ForceUpdateCanvases();Capture(preview,"Logs/ExpandedSeatList.png");
            state.instance_id="loaded-two-seat-game";state.seats=new[]{new PythonSeatState{seat_id="seat-1"},new PythonSeatState{seat_id="seat-2"}};apply();service.ShowSeats();if(service.GetSeatCard(2).gameObject.activeSelf || p.panel.activeSelf)throw new Exception("Old seats retained across load");state.can_set_shifts=true;state.expansion.can_expand=true;state.expansion.has_next=true;state.expansion.seats=2;state.expansion.next_seats=3;state.expansion.reason="";state.revision++;apply();p.Open();p.Choose();p.Confirm();var queued=(PythonShiftCommand)typeof(PythonStateClient).GetField("pending",f).GetValue(client);if(queued==null || queued.kind!="expand_seats" || p.proceed.interactable)throw new Exception("Expansion command queue failed");Debug.Log("CAT_CAFE_EXPANSION_DISPLAY_OK");
        }
        static void Capture(ContentPreview preview,string path){typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,path});}
    }
}
