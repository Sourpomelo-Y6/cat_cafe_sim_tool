using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonPlayerPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title, summary, history, notice;
        public Dropdown targets;
        public Button[] actions;
        public Button finish;
        public ScrollRect historyScroll;
        private PythonPlayerInteraction current;
        private string sessionId, instanceId;
        private bool showing;
        public void Open()
        {
            if(current==null || !GetComponent<PythonStateClient>().CanManageSaves)return;
            Restore();
        }
        public void Restore()
        {
            if(current==null)return;
            showing=true;GetComponent<PythonCatDetailsPanel>().Close();Render();
        }
        public void Close() { showing=false;panel.SetActive(false); }
        public void Finish() { GetComponent<PythonStateClient>().FinishPlayer(); }
        public void SelectAction(int index)
        {
            if(current==null || current.actions==null || index>=current.actions.Length)return;
            string target=current.targets!=null && targets.value<current.targets.Length ? current.targets[targets.value].choice : "";
            GetComponent<PythonStateClient>().PlayerAction(current.actions[index].choice,target);
        }
        public void Apply(PythonCafeState state)
        {
            current=state.player_interaction;
            if(current==null) { showing=false;sessionId=null;panel.SetActive(false);return; }
            if(sessionId!=current.session_id || instanceId!=state.instance_id)
            { showing=true;sessionId=current.session_id;instanceId=state.instance_id;historyScroll.verticalNormalizedPosition=1; }
            if(showing)GetComponent<PythonCatDetailsPanel>().Close();
            Render();
        }
        private void Render()
        {
            panel.SetActive(showing);
            title.text=current.name+(current.active ? "と交流中" : "との交流結果");
            summary.text="種類："+current.mode+" / 残り "+current.remaining_ticks+"ターン\n体力 "+current.stamina.ToString("0.#")+" / 関心 "+current.engagement.ToString("0.#")+" / テンション "+current.tension.ToString("0.#")
                +"\n好感度 "+current.affinity_before.ToString("0.#")+" → "+current.affinity_after.ToString("0.#")+(current.active ? "（見込み）" : "（確定）")
                +" / 本日あと "+current.remaining_sets+"セット";
            history.text=string.IsNullOrEmpty(current.history) ? "行動を選ぶと猫の反応を表示します。" : current.history;
            string selected=targets.options.Count>0 ? targets.options[targets.value].text : "";
            targets.options.Clear();
            foreach(var row in current.targets ?? new PythonEventChoice[0])targets.options.Add(new Dropdown.OptionData(row.label));
            targets.SetValueWithoutNotify(Math.Max(0,targets.options.FindIndex(row=>row.text==selected)));targets.RefreshShownValue();
            notice.text=current.active ? (string.IsNullOrEmpty(current.reason) ? "好感度は終了時に確定します。営業時間は進みません。" : current.reason) : current.reason;
            for(int i=0;i<actions.Length;i++)
            {
                bool exists=current.actions!=null && i<current.actions.Length;
                actions[i].gameObject.SetActive(exists);
                if(exists)actions[i].GetComponentInChildren<Text>().text=current.actions[i].label;
            }
            RefreshControls();
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();
            for(int i=0;i<actions.Length;i++)actions[i].interactable=current!=null && current.actions!=null && i<current.actions.Length && client.CanPlayerAction(current.actions[i].choice);
            targets.interactable=client.CanPlayerAction("switch");finish.interactable=client.CanFinishPlayer;
        }
        public void Disconnect()
        {
            foreach(var button in actions)button.interactable=false;
            targets.interactable=finish.interactable=false;
            if(panel.activeSelf)notice.text="Python未接続です。再接続後に交流を再開できます。";
        }
    }
}
