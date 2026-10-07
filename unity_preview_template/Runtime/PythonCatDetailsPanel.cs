using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonCatDetailsPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title, details;
        public ScrollRect scroll;
        public Button interact, previousInteraction, dispatch;
        public void Dispatch() { GetComponent<PythonDispatchPanel>().Open(selectedId); }
        public Text interactionNotice;
        private string selectedId;
        private PythonCafeState current;
        public void Open(string catId)
        {
            var client=GetComponent<PythonStateClient>();
            if(!client.CanManageSaves || current == null) return;
            selectedId=catId;
            Render();
            scroll.verticalNormalizedPosition=1;
        }
        public void Close() { panel.SetActive(false); selectedId=null; }
        public void Interact()
        {
            var client=GetComponent<PythonStateClient>();var active=current==null ? null : current.player_interaction;
            if(active!=null && active.active && active.cat_id==selectedId)
                GetComponent<PythonPlayerPanel>().Open();
            else client.BeginPlayer(selectedId);
        }
        public void PreviousInteraction() { GetComponent<PythonPlayerPanel>().Open(); }
        public void Apply(PythonCafeState state)
        {
            current=state;
            if(selectedId != null) Render();
        }
        private void Render()
        {
            var cat=current.cats == null ? null : Array.Find(current.cats,row => row.cat_id==selectedId);
            if(cat == null) { Close(); return; }
            panel.SetActive(true);
            title.text=cat.name+"の詳細";
            details.text="体力："+cat.stamina.ToString("0.#")+" / "+cat.max_stamina.ToString("0.#")
                +"\n疲労："+cat.fatigue.ToString("0.#")+" / ストレス："+cat.stress.ToString("0.#")
                +"\n体調："+(string.IsNullOrEmpty(cat.health_label) ? cat.health_status : cat.health_label)
                +"\n活動："+(string.IsNullOrEmpty(cat.activity_label) ? cat.activity : cat.activity_label)
                +" / 予定："+(cat.working ? "出勤" : "休養")
                +"\nプレイヤー好感度："+cat.affinity.ToString("0.#")+" / 100"
                +"\n本日の交流：あと "+cat.remaining_sets+"セット"
                +"\n\n成長・習得の進捗\n"+(string.IsNullOrEmpty(cat.growth_details) ? "成長情報はありません。" : cat.growth_details);
            RefreshControls();
        }
        public void RefreshControls()
        {
            if(dispatch!=null)dispatch.interactable=GetComponent<PythonStateClient>().CanManageSaves && selectedId!=null;
            if(interact==null)return;
            var client=GetComponent<PythonStateClient>();
            var cat=current==null || current.cats==null ? null : Array.Find(current.cats,row=>row.cat_id==selectedId);
            var player=current==null ? null : current.player_interaction;
            bool resume=player!=null && player.active && player.cat_id==selectedId;
            interact.GetComponentInChildren<Text>().text=resume ? "交流を再開" : "猫と交流する";
            interact.interactable=client.CanManageSaves && cat!=null && (resume || cat.can_player_start);
            if(previousInteraction!=null)previousInteraction.interactable=client.CanManageSaves && player!=null && player.cat_id==selectedId;
            if(interactionNotice!=null)interactionNotice.text=cat==null ? "Python未接続です。" : resume ? "途中の交流を再開できます。" : cat.can_player_start ? "営業準備中に1日3セットまで。体力を使います。" : cat.player_reason;
        }
        public void Disconnect()
        {
            current=null;
            RefreshControls();
            if(panel.activeSelf) details.text="Python未接続です。再接続後に最新の情報を表示します。";
        }
    }
}
