using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonBusinessHeader : MonoBehaviour
    {
        public bool Connected;public Text summary,hint;public Button progress,preparation,attention;public PythonAutoPanel auto;public ScrollRect guidance;
        private string lastHint;
        private enum Destination {None,Events,Assignment,Persist,Player,Closing,Goal}
        private Destination destination;
        public void OpenAttention()
        {
            if(attention==null || !attention.interactable)return;
            switch(destination){case Destination.Goal:var goal=GetComponent<PythonGoalPanel>();goal.Apply(GetComponent<PythonStateClient>().CurrentState);goal.panel.transform.SetAsLastSibling();break;case Destination.Events:GetComponent<PythonEventListPanel>()?.Open();break;case Destination.Assignment:GetComponent<PythonServiceAssignmentPanel>()?.Open();break;case Destination.Persist:GetComponent<PythonPersistPanel>()?.Open();break;case Destination.Player:GetComponent<PythonPlayerPanel>()?.Open();break;case Destination.Closing:GetComponent<PythonClosingPanel>()?.Open();break;}
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();var state=client.CurrentState;destination=Destination.None;
            if(state==null){summary.text="接続を待っています";hint.text="再接続後に営業状況を表示します。";if(attention!=null)attention.gameObject.SetActive(false);return;}
            string phase=state.game_over!=null ? "経営終了" : state.closed ? "閉店後" : state.can_set_shifts ? "営業準備" : "営業中";
            summary.text=!Connected ? "未接続 / 再接続を待っています" : state.day+"日目 / "+phase+" / 資金 "+state.funds.ToString("0.#");
            bool running=client.AutoPlayRunning || client.BatchAutomatic || client.BatchStopping;
            preparation.gameObject.SetActive(state.can_set_shifts && !state.closed && state.game_over==null && !running);
            progress.gameObject.SetActive(state.game_over==null && !running);progress.interactable=Connected && client.CanProgressBusiness;
            auto.toggle.gameObject.SetActive((!state.can_set_shifts && !state.closed && state.game_over==null && !running) || client.Automatic);
            auto.speed.gameObject.SetActive(auto.toggle.gameObject.activeSelf);
            var activity=GetComponent<PythonAutoPlayPanel>()?.activity;bool showing=activity!=null && activity.activeSelf;
            if(showing && !running && (client.HasPendingResults || state.player_interaction?.active==true || !string.IsNullOrEmpty(state.required_action) || state.service_assignment?.waiting==true)){activity.SetActive(false);showing=false;}
            hint.gameObject.SetActive(!showing);if(guidance!=null)guidance.gameObject.SetActive(!showing);
            var heading=GetComponent<PythonServiceScreen>().heading;heading.gameObject.SetActive(!showing);
            bool catView=!state.can_set_shifts && !state.closed && !GetComponent<PythonServiceScreen>().ShowingSeats;heading.rectTransform.anchoredPosition=new Vector2(catView ? 300 : 24,-92);heading.rectTransform.sizeDelta=new Vector2(catView ? 484 : 760,34);heading.fontSize=14;
            string caption="";
            if(!Connected)hint.text="未接続です。再接続後に営業を進められます。";
            else if(client.HasPendingResults){hint.text="接客結果の保存が完了していません。保存待ちの内容を確認し、再試行してください。";destination=Destination.Persist;caption="保存待ちを確認";}
            else if(running)hint.text="おまかせ実行中です。停止ボタンから中断できます。";
            else if(!client.CanSendCommands)hint.text=client.Automatic ? "接客を進めています。停止ボタンで自動進行を止められます。" : "操作を送信しています。完了までお待ちください。";
            else if(state.player_interaction?.active==true){hint.text="猫との交流中です。交流を終了してから営業を進めてください。";destination=Destination.Player;caption="交流に戻る";}
            else if(!string.IsNullOrEmpty(state.required_action)){hint.text="確認が必要です："+state.required_action;destination=state.goal_result!=null ? Destination.Goal : Destination.Events;caption=state.goal_result!=null ? "目標の結果を確認" : "回答待ちを確認";}
            else if(state.service_assignment?.waiting==true){hint.text="接客の割り当て待ちです。お客さん・猫・席を選んでください。";destination=Destination.Assignment;caption="接客を割り当てる";}
            else if(state.game_over!=null)hint.text="経営が終了しました。結果を確認してください。";
            else if(state.closed){hint.text="本日の営業は終了しました。閉店結果を確認し、保存して「翌日へ」。";destination=Destination.Closing;caption="閉店結果を確認";}
            else if(state.can_set_shifts && !Array.Exists(state.cats ?? new PythonCatState[0],cat=>cat.working))hint.text="出勤する猫がいません。「営業準備」で出勤する猫を選んでください。";
            else if(client.Automatic)hint.text="自動進行中です。接客状況を確認しながら、いつでも停止できます。";
            else if(!string.IsNullOrEmpty(GetComponent<PythonEventPresentation>()?.Message))hint.text=GetComponent<PythonEventPresentation>().Message;
            else hint.text=state.can_set_shifts ? "猫の出勤・休養を確認して「営業開始」。休養案は「営業準備」で作成できます。" : "「自動進行を開始」で接客を進めます。手動なら「1刻み進める」。";
            if(attention!=null){attention.gameObject.SetActive(destination!=Destination.None && !showing);attention.GetComponentInChildren<Text>().text=caption;attention.interactable=Connected && (destination==Destination.Persist ? client.CanSendCommands : client.CanManageSaves);}
            hint.color=destination==Destination.Persist || destination==Destination.Events || destination==Destination.Assignment ? new Color(.55f,.28f,.12f) : new Color(.16f,.23f,.19f);
            if(lastHint!=hint.text){lastHint=hint.text;if(guidance!=null){Canvas.ForceUpdateCanvases();guidance.verticalNormalizedPosition=1;}}
            auto.toggle.GetComponent<Image>().color=client.Automatic ? new Color(.76f,.35f,.23f) : new Color(.2f,.38f,.3f);auto.toggle.GetComponentInChildren<Text>().color=Color.white;
        }
    }
}
