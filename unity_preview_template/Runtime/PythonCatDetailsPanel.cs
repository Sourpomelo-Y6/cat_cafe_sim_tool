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
                +"\n\n成長・習得の進捗\n"+(string.IsNullOrEmpty(cat.growth_details) ? "成長情報はありません。" : cat.growth_details);
        }
        public void Disconnect()
        {
            current=null;
            if(panel.activeSelf) details.text="Python未接続です。再接続後に最新の情報を表示します。";
        }
    }
}
