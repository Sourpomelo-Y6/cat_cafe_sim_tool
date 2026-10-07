using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonIntakePanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title, details, notice;
        public Button accept, decline;
        public Text[] choiceDetails;
        private string key, prompt;
        public void Accept() { GetComponent<PythonStateClient>().RespondIntake(true); }
        public void Decline() { GetComponent<PythonStateClient>().RespondIntake(false); }
        public void Apply(PythonCafeState state)
        {
            var intake = state.intake_request;
            panel.SetActive(intake != null && intake.status == "waiting");
            if (!panel.activeSelf) return;
            title.text = "保護猫の受け入れ依頼：" + intake.name;
            details.text = intake.day + "日目に届いた依頼\n\n名前 / 個性：" + intake.name + " / " + intake.personality
                + "\n特徴：" + intake.features + "\n" + intake.trait + "\n飼育スペース：" + intake.housing
                + "\n\n初期費用：" + intake.cost + "\n所持金：" + state.funds + " → 受け入れ後：" + (state.funds-intake.cost)
                + "\n加入時は健康・体力全回復・休養予定です。\n見送っても費用や人気への影響はありません。\n見送った依頼は再発生しません。";
            prompt = !string.IsNullOrEmpty(intake.response_reason) ? intake.response_reason
                : !string.IsNullOrEmpty(intake.accept_reason) ? intake.accept_reason + "\nこの依頼は見送ることができます。"
                : "迎えるか見送るかを選ぶと、営業準備を続けられます。";
            PythonEventPresentation.Choices(choiceDetails,new[]{
                new PythonEventChoice{can_select=intake.can_accept,reason=!string.IsNullOrEmpty(intake.response_reason) ? intake.response_reason : intake.accept_reason,detail="初期費用："+intake.cost+" / 所持金："+state.funds+" → "+(state.funds-intake.cost)+"\n健康・体力全回復・休養予定で加入します。"},
                new PythonEventChoice{can_select=intake.can_decline,reason=intake.response_reason,detail="費用や人気への影響はありません。この依頼は再発生しません。"}});
            string nextKey=state.instance_id+"/"+intake.cat_id+"/"+intake.day;if(key!=nextKey){key=nextKey;PythonEventPresentation.Reset(details,choiceDetails);}
            RefreshControls();
        }
        public void RefreshControls()
        {
            var client = GetComponent<PythonStateClient>();
            accept.interactable = client.CanRespondIntake(true);
            decline.interactable = client.CanRespondIntake(false);
            if(panel.activeSelf)notice.text=PythonEventPresentation.Notice(client,prompt);
        }
        public void Disconnect()
        {
            accept.interactable = decline.interactable = false;
            if(panel.activeSelf) notice.text = "Python未接続です。再接続後に回答できます。";
        }
    }
}
