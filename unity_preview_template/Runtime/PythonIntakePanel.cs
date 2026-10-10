using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonIntakePanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title, details, notice;
        public Button accept, decline;
        public RawImage picture;
        public Text[] choiceDetails;
        public GameObject confirmation;
        public Text confirmationText;
        public Button confirm,cancel,housing;
        private string key, prompt,identity;
        private bool? selection;
        public void Accept() { Choose(true); }
        public void Decline() { Choose(false); }
        void Choose(bool answer)
        {
            var client=GetComponent<PythonStateClient>();if(!client.CanRespondIntake(answer) || confirmation==null)return;
            var q=client.CurrentState.intake_request;selection=answer;confirmationText.text=answer?q.name+"を迎えますか？\n初期費用："+q.cost+" / 資金："+client.CurrentState.funds+" → "+(client.CurrentState.funds-q.cost)+"\n飼育スペース："+q.housing+"\n健康・体力全回復・休養予定で加入します。":q.name+"の依頼を見送りますか？\n費用や人気への影響はありません。この依頼は再発生しません。";
            confirmation.SetActive(true);confirmation.transform.SetAsLastSibling();RefreshControls();
        }
        public void Cancel(){selection=null;if(confirmation!=null)confirmation.SetActive(false);RefreshControls();}
        public void Confirm(){var client=GetComponent<PythonStateClient>();if(!selection.HasValue || !client.CanRespondIntake(selection.Value))return;bool answer=selection.Value;if(answer)GetComponent<PythonCatAcquisitionNavigation>()?.Watch(client.CurrentState.intake_request.cat_id);Cancel();client.RespondIntake(answer);}
        public void Housing(){Cancel();GetComponent<PythonCatAcquisitionNavigation>().OpenHousing(panel);}
        public void Apply(PythonCafeState state)
        {
            var intake = state.intake_request;
            string revision=state.instance_id+"/"+state.revision;if(identity!=revision){identity=revision;selection=null;if(confirmation!=null)confirmation.SetActive(false);}
            panel.SetActive(intake != null && intake.status == "waiting");
            if (!panel.activeSelf) return;
            if (picture != null)
            {
                picture.texture = GetComponent<ContentPreview>().CandidateCatTexture(intake.cat_id);
                picture.enabled = picture.texture != null;
                var aspect = picture.GetComponent<AspectRatioFitter>();
                if (aspect != null && picture.texture != null) aspect.aspectRatio = (float)picture.texture.width / picture.texture.height;
            }
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
            accept.interactable = !selection.HasValue && client.CanRespondIntake(true);
            decline.interactable = !selection.HasValue && client.CanRespondIntake(false);
            if(confirm!=null)confirm.interactable=selection.HasValue && client.CanRespondIntake(selection.Value);
            if(housing!=null)housing.interactable=!selection.HasValue && client.CanManageSaves && client.CurrentState?.housing!=null;
            if(panel.activeSelf)notice.text=PythonEventPresentation.Notice(client,prompt);
        }
        public void Disconnect()
        {
            selection=null;if(confirmation!=null)confirmation.SetActive(false);if(confirm!=null)confirm.interactable=false;if(housing!=null)housing.interactable=false;
            accept.interactable = decline.interactable = false;
            if(panel.activeSelf) notice.text = "Python未接続です。再接続後に回答できます。";
        }
    }
}
