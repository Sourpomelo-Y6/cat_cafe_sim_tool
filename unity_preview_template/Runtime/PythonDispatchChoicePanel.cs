using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonDispatchChanges
    { public float fatigue_before, fatigue_after, stress_before, stress_after; }
    [Serializable] public class PythonDispatchEncounterOption
    {
        public string choice, label, reason;
        public float reward_delta, fatigue, stress, return_reward;
        public bool can_select;
        public PythonDispatchChanges changes;
    }
    [Serializable] public class PythonDispatchEncounter
    {
        public string event_id, cat_id, label, title, status, selected_label;
        public int occurred_day, resolved_day;
        public float return_reward;
        public bool has_stress;
        public PythonDispatchChanges changes;
        public PythonDispatchEncounterOption[] choices;
    }
    public class PythonDispatchChoicePanel : MonoBehaviour
    {
        public GameObject panel, confirmation;
        public Dropdown records;
        public Text title, details, notice, confirmationText;
        public Button first, second, close, confirm, cancel;
        private PythonDispatchEncounter[] events=new PythonDispatchEncounter[0];
        private string instance, identity, waiting, selection;
        private bool online;
        private PythonDispatchEncounter Row { get { return records.value<events.Length ? events[records.value] : null; } }
        public EncounterConversationPanel Dialogue=>GetComponent<EncounterConversationPanel>()??gameObject.AddComponent<EncounterConversationPanel>();
        public void HideForConversation(){Clear();panel.SetActive(false);}
        public void Open()
        {
            if(events.Length==0 || !GetComponent<PythonStateClient>().CanManageSaves)return;
            if(CafeBackends.Standalone)
            {
                var b=CafeBackends.Local as LocalCafeBackend;Dialogue.Apply(GetComponent<PythonStateClient>().CurrentState,this);
                if(b.EncounterResultPending){Dialogue.Open();return;}
                if(Row!=null && Dialogue.OpenEvent(Row.event_id))return;
            }
            Clear();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();ResetScroll();
        }
        public void Close(){Clear();panel.SetActive(false);if(CafeBackends.Standalone && GetComponent<EncounterConversationPanel>()?.Showing==true)Dialogue.Close();}
        private void Clear(){selection=null;confirmation.SetActive(false);}
        public void Select(int _){Clear();Render();ResetScroll();if(panel.activeSelf && CafeBackends.Standalone && Row!=null)Dialogue.OpenEvent(Row.event_id);}
        private void ResetScroll(){var scroll=details.transform.parent.parent.GetComponent<ScrollRect>();if(scroll!=null)scroll.verticalNormalizedPosition=1;}
        public void First(){Choose(0);} public void Second(){Choose(1);}
        static string Number(float value){return value.ToString("0.#");}
        static string Delta(float value){return (value>0 ? "＋" : value<0 ? "−" : "")+Number(Math.Abs(value));}
        private static string Effects(PythonDispatchEncounter row,PythonDispatchEncounterOption option)
        {
            var change=option.changes;
            return "帰還報酬の変化："+Delta(option.reward_delta)+(row.status=="waiting" ? " / 選択後の帰還報酬：" : " / 現在の帰還報酬：")+Number(option.return_reward)
                +"\n疲労："+(change==null ? Delta(option.fatigue) : Number(change.fatigue_before)+" → "+Number(change.fatigue_after))
                +"\nストレス："+(!row.has_stress ? "この営業では対象外" : change==null ? Delta(option.stress) : Number(change.stress_before)+" → "+Number(change.stress_after));
        }
        private void Choose(int index)
        {
            var row=Row;if(!online || row?.choices==null || index>=row.choices.Length)return;var option=row.choices[index];
            if(!GetComponent<PythonStateClient>().CanRespondDispatchChoice(row.event_id,option.choice))return;
            selection=option.choice;confirmation.SetActive(true);confirmationText.text=row.label+"\n"+option.label+"を選びますか？\n\n"+Effects(row,option)+"\n\n報酬は帰還時に受け取ります。";RefreshControls();
        }
        public void Confirm()
        {
            var row=Row;if(!online || selection==null || row==null || !GetComponent<PythonStateClient>().CanRespondDispatchChoice(row.event_id,selection))return;
            string eventId=row.event_id,choice=selection;Clear();GetComponent<PythonStateClient>().RespondDispatchChoice(eventId,choice);
        }
        public void Cancel(){Clear();RefreshControls();}
        public void Apply(PythonCafeState state)
        {
            string old=Row?.event_id;
            if(instance!=state.instance_id){instance=state.instance_id;old=null;waiting=null;Close();}
            online=true;var key=state.instance_id+"/"+state.revision;if(identity!=key){identity=key;Clear();}
            events=state.dispatch_choices ?? new PythonDispatchEncounter[0];
            var pending=events.FirstOrDefault(e=>e.status=="waiting");bool newlyWaiting=pending!=null && pending.event_id!=waiting;waiting=pending?.event_id;
            int index=Array.FindIndex(events,e=>e.event_id==(newlyWaiting ? pending.event_id : old));if(index<0)index=0;
            records.options.Clear();foreach(var ev in events)records.options.Add(new Dropdown.OptionData(ev.label+"（"+(ev.status=="waiting" ? "回答待ち" : ev.status=="scheduled" ? "発生前" : "回答済み")+"）"));
            if(events.Length==0)records.options.Add(new Dropdown.OptionData("記録なし"));records.SetValueWithoutNotify(index);records.RefreshShownValue();
            if(newlyWaiting){panel.SetActive(true);panel.transform.SetAsLastSibling();}
            if(events.Length==0 || state.game_over!=null)panel.SetActive(false);Render();if(newlyWaiting)ResetScroll();if(CafeBackends.Standalone){Dialogue.Apply(state,this);if(Dialogue.Showing || GetComponents<AcquisitionConversationPanel>().Any(v=>v.Showing))HideForConversation();}
        }
        private void Render()
        {
            var row=Row;if(row==null){RefreshControls();return;}title.text="派遣中の選択イベント";
            details.text=row.label+"\n"+row.title+"\n\n"+(row.status=="scheduled" ? "派遣1日目の閉店後に発生します。" : row.status=="waiting" ? row.occurred_day+"日目に発生 / 回答待ち" : row.resolved_day+"日目に回答済み："+row.selected_label);
            foreach(var option in row.choices ?? new PythonDispatchEncounterOption[0])details.text+="\n\n"+option.label+"\n"+Effects(row,option);
            if(row.changes!=null)details.text+="\n\n回答時の実際の変化\n疲労："+Number(row.changes.fatigue_before)+" → "+Number(row.changes.fatigue_after)
                +(row.has_stress ? "\nストレス："+Number(row.changes.stress_before)+" → "+Number(row.changes.stress_after) : "")+"\n現在の帰還報酬："+Number(row.return_reward);
            details.text+="\n\n疲労・ストレスの変化は回答時に反映します。報酬は帰還時に受け取ります。回答後は選択を変更できません。";
            var scroll=details.transform.parent.parent.GetComponent<ScrollRect>();if(scroll!=null)details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(276,details.preferredHeight+12));
            RefreshControls();
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();var row=Row;bool ready=online && client.CanManageSaves;
            var buttons=new[]{first,second};for(int i=0;i<buttons.Length;i++){var option=row?.choices!=null && i<row.choices.Length ? row.choices[i] : null;buttons[i].GetComponentInChildren<Text>().text=option?.label ?? "選択肢なし";buttons[i].interactable=ready && selection==null && option!=null && option.can_select;}
            confirm.interactable=ready && selection!=null && row!=null && client.CanRespondDispatchChoice(row.event_id,selection);records.interactable=ready && events.Length>0 && selection==null;close.interactable=cancel.interactable=true;
            notice.text=!online ? "Python未接続です。再接続後に回答できます。" : !client.CanManageSaves ? "操作を送信しています。完了するまでお待ちください。" : row==null ? "派遣イベントの記録はありません。"
                : row.status=="resolved" ? "回答済みです。記録を確認できます。" : row.status=="scheduled" ? "まだ発生していません。" : row.choices?.FirstOrDefault(o=>!string.IsNullOrEmpty(o.reason))?.reason ?? "選択肢を選び、効果を確認して回答してください。";
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
