using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonBusinessHeader : MonoBehaviour
    {
        public bool Connected;public Text summary,hint;public Button progress,preparation;public PythonAutoPanel auto;
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();var state=client.CurrentState;
            if(state==null){summary.text="接続を待っています";return;}
            string phase=state.game_over!=null ? "経営終了" : state.closed ? "閉店後" : state.can_set_shifts ? "営業準備" : "営業中";
            summary.text=!Connected ? "未接続 / 再接続を待っています" : state.day+"日目 / "+phase+" / 資金 "+state.funds.ToString("0.#")+(phase=="営業中" ? " / "+state.tick+"刻" : "");
            bool running=client.AutoPlayRunning || client.BatchAutomatic || client.BatchStopping;
            preparation.gameObject.SetActive(state.can_set_shifts && !state.closed && state.game_over==null && !running);
            progress.gameObject.SetActive(state.game_over==null && !running);
            auto.toggle.gameObject.SetActive((!state.can_set_shifts && !state.closed && state.game_over==null && !running) || client.Automatic);
            auto.speed.gameObject.SetActive(auto.toggle.gameObject.activeSelf);
            var activity=GetComponent<PythonAutoPlayPanel>()?.activity;bool showing=activity!=null && activity.activeSelf;
            hint.gameObject.SetActive(!showing);
            var heading=GetComponent<PythonServiceScreen>().heading;heading.gameObject.SetActive(!showing);
            heading.rectTransform.anchoredPosition=new Vector2(24,-92);heading.rectTransform.sizeDelta=new Vector2(760,34);heading.fontSize=14;
            if(!client.CanManageSaves && !client.Automatic && !running)return;
            if(string.IsNullOrEmpty(state.required_action) && !client.Automatic && state.game_over==null && !client.HasPendingResults
                && state.player_interaction?.active!=true && string.IsNullOrEmpty(GetComponent<PythonEventPresentation>()?.Message)
                && (!state.can_set_shifts || state.closed || Array.Exists(state.cats,cat=>cat.working)))
                hint.text=state.closed ? "閉店結果を確認し、保存して「翌日へ」。" : state.can_set_shifts ? "猫の出勤・休養を確認して「営業開始」。休養案は「営業準備」で作成できます。" : "「自動進行を開始」で接客を進めます。手動なら「1刻み進める」。";
        }
    }
}
