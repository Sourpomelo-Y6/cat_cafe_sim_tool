using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonReservation
    {
        public string event_id, status, customer_id, name, description, result, reason, feature_label;
        public int offered_day, visit_day, resolved_day, open_up_count, arrival_tick;
        public float bonus, popularity_bonus;
        public bool can_accept, can_decline;
    }
    public class PythonReservationPanel : MonoBehaviour
    {
        public GameObject panel, confirmation;
        public Text title, details, notice, confirmationText;
        public Button accept, decline, close, confirm, cancel;
        private PythonCafeState current;
        private string identity, instance, waiting, selection;
        private bool online;
        public bool Confirming=>selection!=null;
        public ReservationConversationPanel Dialogue=>GetComponent<ReservationConversationPanel>()??gameObject.AddComponent<ReservationConversationPanel>();
        public void BeginConversationAnswer(string choice){Choose(choice);}

        public void Open(){if(current?.reservation==null || !GetComponent<PythonStateClient>().CanManageSaves)return;Clear();if(CafeBackends.Standalone && Dialogue.OpenPending())return;panel.SetActive(true);panel.transform.SetAsLastSibling();Render();}
        public void Close(){Clear();panel.SetActive(false);if(GetComponent<ReservationConversationPanel>()?.Showing==true)Dialogue.Close();}
        public void Accept(){Choose("accept");} public void Decline(){Choose("decline");}
        private void Clear(){selection=null;confirmation.SetActive(false);}
        private void Choose(string choice)
        {
            if(!online || !GetComponent<PythonStateClient>().CanRespondReservation(choice))return;
            selection=choice;if(CafeBackends.Standalone){panel.SetActive(true);panel.transform.SetAsLastSibling();}confirmation.SetActive(true);confirmation.transform.SetAsLastSibling();var row=current.reservation;
            confirmationText.resizeTextForBestFit=true;confirmationText.resizeTextMinSize=16;confirmationText.resizeTextMaxSize=confirmationText.fontSize;
            confirmationText.text=choice=="accept" ? row.name+"の予約を引き受けますか？\n来店日："+row.visit_day+"日目\n"+(row.feature_label??"長毛")+"の猫を担当 / 心を開く "+row.open_up_count+"回以上\n来店日は休業できません。来店刻み："+row.arrival_tick+"\n条件達成時：追加料金 ＋"+row.bonus.ToString("0.#")+" / 人気 ＋"+row.popularity_bonus.ToString("0.#")+"\n回答時の資金・人気変化なし。取消では回答しません。" : "この予約を辞退しますか？\nペナルティ・資金・人気の変化はありません。この予約の来店と追加報酬はなくなります。\n取消では回答しません。";
            RefreshControls();
        }
        public void Confirm(){if(!online || selection==null || !GetComponent<PythonStateClient>().CanRespondReservation(selection))return;var choice=selection;Clear();GetComponent<PythonStateClient>().RespondReservation(choice);}
        public void Cancel(){Clear();if(CafeBackends.Standalone)panel.SetActive(false);RefreshControls();if(CafeBackends.Standalone)GetComponent<ReservationConversationPanel>()?.Refresh();}
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;waiting=null;Close();}
            current=state;online=true;var row=state.reservation;var key=state.instance_id+"/"+state.revision+"/"+state.day+"/"+row?.event_id+"/"+row?.status;
            if(identity!=key){identity=key;Clear();}
            string next=row?.status=="waiting" ? row.event_id : null;
            if(next!=null && waiting!=next){panel.SetActive(true);panel.transform.SetAsLastSibling();}waiting=next;
            if(row==null || state.game_over!=null)panel.SetActive(false);Render();RefreshControls();if(Application.isPlaying && CafeBackends.Standalone)Dialogue.Apply(state,this);
        }
        private void Render()
        {
            var row=current?.reservation;if(row==null)return;title.text="特別予約："+row.name;
            details.text=row.description+"\n\n"+(row.visit_day>0 ? "依頼日："+row.offered_day+"日目 / 来店日："+row.visit_day+"日目 / 来店刻み："+row.arrival_tick : "人気第2段階の達成後、翌日の準備に一度だけ依頼が届きます。")
                +"\n\n条件："+(row.feature_label??"長毛")+"の猫を担当 / 心を開く "+row.open_up_count+"回以上\n条件達成：追加料金 ＋"+row.bonus.ToString("0.#")+" / 人気 ＋"+row.popularity_bonus.ToString("0.#")
                +"\n\n辞退してもペナルティはありません。\n引き受けると来店日は休業できません。\n予約への回答では資金は変わりません。"
                +(row.resolved_day>0 ? "\n\n"+row.resolved_day+"日目に"+(row.status=="accepted" ? "引き受けました。" : "辞退しました。") : "")
                +(string.IsNullOrEmpty(row.result) ? "" : "\n接客結果："+(row.result=="success" ? "条件達成" : row.result=="failure" ? "条件未達" : "未接客"));
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();var row=current?.reservation;bool ready=online && client.CanManageSaves && row!=null;
            accept.interactable=ready && selection==null && row.can_accept;decline.interactable=ready && selection==null && row.can_decline;
            confirm.interactable=ready && selection!=null && client.CanRespondReservation(selection);close.interactable=cancel.interactable=true;
            if(row==null)return;notice.text=!online ? "Python未接続です。再接続後に回答できます。" : !client.CanManageSaves ? "操作を送信しています。完了するまでお待ちください。"
                : !string.IsNullOrEmpty(row.reason) ? row.reason : row.status=="untriggered" ? "人気第2段階を達成すると予約が解放されます。" : row.status=="waiting" ? "引き受けるか辞退するかを選ぶと、営業準備を続けられます。" : row.status=="declined" ? "予約を辞退しました。" : "予約を引き受けました。来店日と担当猫の条件を確認してください。";
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
