using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
namespace CatCafePreview
{
    public class PythonReturnToTitle : MonoBehaviour
    {
        private Button entry;
        private Text hint;
        public void Return()
        { var client=GetComponent<PythonStateClient>();if(client!=null && client.CanLeaveResult)SceneManager.LoadScene("CatCafeTitle"); }
        public void RefreshControls()
        {
            if(entry==null){var root=transform.Find("Content Canvas/Background");if(root==null)return;entry=root.Find("Return To Title")?.GetComponent<Button>();hint=root.Find("Title Save Hint")?.GetComponent<Text>();}
            var client=GetComponent<PythonStateClient>();if(client==null)return;
            if(entry!=null)entry.interactable=client.CanLeaveResult;
            if(hint==null)return;
            var state=client.CurrentState;
            if(!client.CanLeaveResult)hint.text="操作を送信しています。完了までお待ちください。";
            else if(!client.CanManageSaves)hint.text="Python未接続です。再接続を待っています。";
            else if(state.game_over!=null)hint.text="経営が終了しました。結果を保存できます。";
            else if(!string.IsNullOrEmpty(state.required_action))hint.text="先に確認："+state.required_action;
            else if(state.player_interaction!=null && state.player_interaction.active)hint.text="猫との交流中です。交流を終えると営業できます。";
            else if(state.can_set_shifts && !state.closed && !Array.Exists(state.cats,cat=>cat.working))hint.text="出勤する猫がいません。猫カードで選んでください。";
            else if(client.Automatic)hint.text="自動進行中です。停止すると手動で操作できます。";
            else hint.text=state.closed ? "閉店しました。振り返りを確認し、保存して翌日へ。" : "進捗を残す場合は、先に保存してください。";
        }
    }
}
