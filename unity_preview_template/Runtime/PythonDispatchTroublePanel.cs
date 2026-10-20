using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonDispatchTroubleOption
    {
        public string choice, label, reason, description;
        public float cost, funds_after;
        public int remaining;
        public bool can_select;
    }
    [Serializable] public class PythonDispatchTrouble
    {
        public string event_id, cat_id, label, status, activity_status, description, choice;
        public int missing_day, choice_day;
        public float cost, return_reward;
        public PythonDispatchTroubleOption[] choices;
    }
    public class PythonDispatchTroublePanel : MonoBehaviour
    {
        public GameObject panel, confirmation;
        public Dropdown records;
        public Text title, details, notice, confirmationText;
        public Button first, second, close, confirm, cancel, viewReturn;
        private PythonDispatchTrouble[] events=new PythonDispatchTrouble[0];
        private string instance, identity, waiting, selection;
        private bool online;
        private PythonDispatchTrouble Row { get { return records.value<events.Length ? events[records.value] : null; } }
        public LodgeConversationPanel Dialogue=>GetComponent<LodgeConversationPanel>()??gameObject.AddComponent<LodgeConversationPanel>();
        public void HideForConversation(){Clear();panel.SetActive(false);}
        public void Open(){if(CafeBackends.Standalone){Dialogue.Apply(GetComponent<PythonStateClient>().CurrentState,this);var b=CafeBackends.Local as LocalCafeBackend;if(b.LodgeResultPending){Dialogue.Open();return;}if(Row!=null && Dialogue.OpenEvent(Row.event_id))return;}if(events.Length==0 || !GetComponent<PythonStateClient>().CanManageSaves)return;Clear();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();ResetScroll();}
        public void Close(){Clear();panel.SetActive(false);if(GetComponent<LodgeConversationPanel>()?.Showing==true)Dialogue.Close();}
        private void Clear(){selection=null;confirmation.SetActive(false);}
        public void Select(int _){Clear();Render();ResetScroll();if(CafeBackends.Standalone && panel.activeSelf && Row!=null)Dialogue.OpenEvent(Row.event_id);}
        private void ResetScroll(){var scroll=details.transform.parent.parent.GetComponent<ScrollRect>();if(scroll!=null)scroll.verticalNormalizedPosition=1;}
        public void First(){Choose(0);} public void Second(){Choose(1);}
        static string Number(float value){return value.ToString("0.#");}
        private static string Effects(PythonDispatchTrouble row,PythonDispatchTroubleOption option)
        {
            return "費用："+Number(option.cost)+" / 支払い後の資金："+Number(option.funds_after)+"\n"+option.description
                +"\nトラブルが発生した派遣の報酬は0です。";
        }
        public void ViewReturn()
        {
            var row=Row;if(!online || row==null || !GetComponent<PythonStateClient>().CanManageSaves)return;
            Close();var general=GetComponent<PythonGeneralDispatchPanel>();general.OpenReturn(row.event_id);general.panel.transform.SetAsLastSibling();
        }
        private void Choose(int index)
        {
            var row=Row;if(!online || row?.choices==null || index>=row.choices.Length)return;var option=row.choices[index];
            if(!GetComponent<PythonStateClient>().CanRespondDispatchTrouble(row.event_id,option.choice))return;
            selection=option.choice;confirmation.SetActive(true);confirmationText.text=row.label+"\n"+option.label+"を選びますか？\n\n"+Effects(row,option);RefreshControls();
        }
        public void Confirm()
        {
            var row=Row;if(!online || selection==null || row==null || !GetComponent<PythonStateClient>().CanRespondDispatchTrouble(row.event_id,selection))return;
            string eventId=row.event_id,choice=selection;Clear();GetComponent<PythonStateClient>().RespondDispatchTrouble(eventId,choice);
        }
        public void Cancel(){Clear();RefreshControls();}
        public void Apply(PythonCafeState state)
        {
            string old=Row?.event_id;
            if(instance!=state.instance_id){instance=state.instance_id;old=null;waiting=null;Close();}
            online=true;var key=state.instance_id+"/"+state.revision;if(identity!=key){identity=key;Clear();}
            events=state.dispatch_troubles ?? new PythonDispatchTrouble[0];
            var pending=events.FirstOrDefault(e=>e.status=="waiting");bool newlyWaiting=pending!=null && pending.event_id!=waiting;waiting=pending?.event_id;
            int index=Array.FindIndex(events,e=>e.event_id==(newlyWaiting ? pending.event_id : old));if(index<0)index=0;
            records.options.Clear();foreach(var ev in events)records.options.Add(new Dropdown.OptionData(ev.label+"（"+(ev.status=="waiting" ? "回答待ち" : ev.status=="scheduled" ? "発生前" : ev.status=="not_triggered" ? "発生なし" : "回答済み")+"）"));
            if(events.Length==0)records.options.Add(new Dropdown.OptionData("記録なし"));records.SetValueWithoutNotify(index);records.RefreshShownValue();
            if(newlyWaiting){panel.SetActive(true);panel.transform.SetAsLastSibling();}
            if(events.Length==0 || state.game_over!=null)panel.SetActive(false);Render();if(newlyWaiting)ResetScroll();if(CafeBackends.Standalone)Dialogue.Apply(state,this);
        }
        private void Render()
        {
            var row=Row;if(row==null){RefreshControls();return;}title.text="山あいの宿・トラブル対応";
            details.text=row.label+"\n\n"+row.description;
            if(row.status=="resolved")details.text+="\n\n"+row.choice_day+"日目に回答："+(row.choice=="search" ? "捜索して連れ戻す" : "帰還を待つ")+" / 費用 "+Number(row.cost)+"\n回答は変更できません。帰還は派遣・帰還画面で確認します。";
            else if(row.status=="waiting")foreach(var option in row.choices ?? new PythonDispatchTroubleOption[0])details.text+="\n\n"+option.label+"\n"+Effects(row,option);
            var scroll=details.transform.parent.parent.GetComponent<ScrollRect>();if(scroll!=null)details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(276,details.preferredHeight+12));
            RefreshControls();
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();var row=Row;bool ready=online && client.CanManageSaves;
            var buttons=new[]{first,second};for(int i=0;i<buttons.Length;i++){var option=row?.choices!=null && i<row.choices.Length ? row.choices[i] : null;buttons[i].GetComponentInChildren<Text>().text=option?.label ?? "選択肢なし";buttons[i].interactable=ready && selection==null && option!=null && option.can_select;}
            confirm.interactable=ready && selection!=null && row!=null && client.CanRespondDispatchTrouble(row.event_id,selection);records.interactable=ready && events.Length>0 && selection==null;viewReturn.interactable=ready && row!=null && selection==null;close.interactable=cancel.interactable=true;
            notice.text=!online ? "Python未接続です。再接続後に回答できます。" : !client.CanManageSaves ? "操作を送信しています。完了するまでお待ちください。" : row==null ? "宿のトラブルの記録はありません。"
                : row.status=="resolved" ? "回答済みです。記録を確認できます。" : row.status=="scheduled" ? "まだ発生していません。" : row.status=="not_triggered" ? "トラブルは発生しませんでした。" : row.choices?.FirstOrDefault(o=>!string.IsNullOrEmpty(o.reason))?.reason ?? "対応を選び、費用と帰還条件を確認して回答してください。";
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
