using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonGrowthPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title, details, notice;
        public Button[] choices;
        public void First() { GetComponent<PythonStateClient>().RespondGrowth(0); }
        public void Second() { GetComponent<PythonStateClient>().RespondGrowth(1); }
        public void Third() { GetComponent<PythonStateClient>().RespondGrowth(2); }
        public void Fourth() { GetComponent<PythonStateClient>().RespondGrowth(3); }
        public void Apply(PythonCafeState state)
        {
            var rows=state.growth_choices;
            panel.SetActive(rows != null && rows.Length>0);
            if(!panel.activeSelf) return;
            var ev=rows[0];
            bool mastery=ev.kind == "resolve_growth_mastery";
            title.text=ev.name+"の"+(string.IsNullOrEmpty(ev.title) ? "得意分野" : ev.title)+"を選択";
            details.text="選択待ち："+rows.Length+"匹 / 経験："+ev.experience+"（必要："+ev.threshold+"）\n接客："+ev.service+" / 休養："+ev.rest+" / 派遣："+ev.dispatch+"\n\n";
            if(mastery) details.text="選択待ち："+rows.Length+"匹 / 接客実績："+ev.experience+"（必要："+ev.threshold+"）\n遊び："+ev.play+" / 触れ合い："+ev.contact+" / 静かな交流："+ev.quiet+"\n習得済み："+(string.IsNullOrEmpty(ev.learned) ? "なし" : ev.learned)+"\n\n";
            bool individual=ev.kind == "resolve_growth_type_mastery";
            details.fontSize=individual ? 17 : 19;
            details.lineSpacing=individual ? 1 : 1.12f;
            if(individual) details.text="分類："+ev.group+" / 選択待ち："+rows.Length+"件\n対象実績："+ev.experience+"（必要："+ev.threshold+"）\n習得済み："+(string.IsNullOrEmpty(ev.learned) ? "なし" : ev.learned)+"\n"+ev.effect+"\n\n";
            for(int i=0;i<choices.Length;i++)
            {
                bool exists=ev.choices != null && i<ev.choices.Length;
                choices[i].gameObject.SetActive(exists);
                if(!exists) continue;
                var option=ev.choices[i];
                choices[i].GetComponentInChildren<Text>().text=option.label;
                details.text+=option.label+"\n"+option.detail+"\n"+(option.can_select ? "" : option.reason+"\n")+(individual ? "" : "\n");
            }
            notice.text="1つ選ぶと確定します。選び直しはできません。\n選択後の自動進行は手動で再開してください。";
            RefreshControls();
        }
        public void RefreshControls()
        { for(int i=0;i<choices.Length;i++) choices[i].interactable=GetComponent<PythonStateClient>().CanRespondGrowth(i); }
        public void Disconnect()
        {
            foreach(var button in choices) button.interactable=false;
            if(panel.activeSelf) notice.text="Python未接続です。再接続後に回答できます。";
        }
    }
}
