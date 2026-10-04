using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonGoalPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title, details, notice;
        public Button resume;
        public void Continue() { GetComponent<PythonStateClient>().ContinueGoal(); }
        public void Apply(PythonCafeState state)
        {
            var result=state.goal_result;
            panel.SetActive(result != null);
            if(result == null)return;
            title.text=result.status=="cleared" ? "人気目標を達成しました" : "人気目標の期限を迎えました";
            details.text="第" + result.stage + "段階の結果\n\n目標人気：" + result.target + "\n現在の人気：" + result.popularity
                + "\n結果：" + (result.status=="cleared" ? "達成" : "期限内未達")
                + "\n挑戦期間：" + result.started_day + "〜" + result.deadline + "日目\n結果確定：" + result.resolved_day + "日目"
                + "\n所持金：" + result.funds + "\n\n「継続営業」で結果を確認し、自由営業を続けます。"
                + "\n猫や資金、人気を引き継いで遊べます。";
            notice.text=result.can_continue ? "内容を確認して「継続営業」を選んでください。" : result.reason;
            RefreshControls();
        }
        public void RefreshControls() { resume.interactable=GetComponent<PythonStateClient>().CanContinueGoal; }
        public void Disconnect()
        {
            resume.interactable=false;
            if(panel.activeSelf)notice.text="Python未接続です。再接続後に結果を確認できます。";
        }
    }
}
