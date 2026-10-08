using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonVisitingCat
    {
        public string cat_id, content_cat_id, name, status, personality, features, trait, housing, history, reason, daily_reason, accept_reason;
        public int progress, required, presented_day, accepted_day;
        public float cost, funds_after;
        public bool can_interact, can_skip, can_accept;
    }
    public class PythonVisitingCatPanel : MonoBehaviour
    {
        public GameObject panel, confirmation;
        public RawImage picture;
        public Text title, details, notice, confirmationText;
        public Button open, interact, skip, accept, housing, close, confirm, cancel;
        private PythonCafeState current;
        private string identity, selection;
        private bool online;
        public void Open()
        {
            if(current?.visiting_cat==null || !current.can_set_shifts || current.closed || current.game_over!=null)return;
            ClearConfirmation();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();
            Canvas.ForceUpdateCanvases();details.GetComponentInParent<ScrollRect>().verticalNormalizedPosition=1;
        }
        public void Close() { ClearConfirmation();panel.SetActive(false);GetComponent<PythonPreparationPanel>()?.Open(); }
        public void Interact() { Choose("interact"); }
        public void Skip() { Choose("skip"); }
        public void Accept() { Choose("accept"); }
        public void Housing() { ClearConfirmation();GetComponent<PythonHousingPanel>().Open(); }
        private void Choose(string choice)
        {
            if(!online || !GetComponent<PythonStateClient>().CanRespondVisiting(choice))return;
            selection=choice;confirmation.SetActive(true);
            var cat=current.visiting_cat;
            confirmationText.text=choice=="accept" ? cat.name+"を迎えますか？\n費用 "+cat.cost.ToString("0.#")+" / 加入後の資金 "+cat.funds_after.ToString("0.#")+"\n健康・体力全回復・休養予定で加入します。"
                : choice=="interact" ? "今日は"+cat.name+"と交流しますか？\n費用はかかりません。交流は1日1回です。"
                : "今日は"+cat.name+"との交流を見送りますか？\nこれまでの進捗を保ち、翌日以降にまた会えます。";
            RefreshControls();
        }
        public void Confirm()
        {
            if(!online || selection==null || !GetComponent<PythonStateClient>().CanRespondVisiting(selection))return;
            var choice=selection;ClearConfirmation();GetComponent<PythonStateClient>().RespondVisiting(choice);
        }
        public void Cancel() { ClearConfirmation();RefreshControls(); }
        private void ClearConfirmation() { selection=null;confirmation.SetActive(false); }
        public void Apply(PythonCafeState state)
        {
            current=state;online=true;
            var cat=state.visiting_cat;
            string key=state.instance_id+"/"+state.day+"/"+cat?.cat_id+"/"+cat?.status+"/"+cat?.progress+"/"+cat?.history+"/"+state.revision;
            if(identity!=key){identity=key;ClearConfirmation();}
            if(cat==null || !state.can_set_shifts || state.closed || state.game_over!=null)panel.SetActive(false);
            open.gameObject.SetActive(cat!=null && state.can_set_shifts && !state.closed && state.game_over==null);
            Render();RefreshControls();
        }
        private void Render()
        {
            var cat=current?.visiting_cat;if(cat==null)return;
            open.GetComponentInChildren<Text>().text=cat.status=="accepted" ? "店先の猫の加入記録" : "店先に通う猫";
            title.text=cat.name+" · 店先での交流 "+cat.progress+"/"+cat.required+"回";
            picture.texture=GetComponent<ContentPreview>().CandidateCatTexture(cat.content_cat_id);picture.enabled=picture.texture!=null;
            if(picture.texture!=null)picture.GetComponent<AspectRatioFitter>().aspectRatio=(float)picture.texture.width/picture.texture.height;
            details.text="名前 / 個性："+cat.name+" / "+cat.personality+"\n特徴："+cat.features+"\n"+cat.trait+"\n\n飼育スペース："+cat.housing
                +"\n迎える費用："+cat.cost.ToString("0.#")+"\n所持金："+current.funds.ToString("0.#")+" → 迎えた後："+cat.funds_after.ToString("0.#")
                +"\n\n加入時は健康・体力全回復・休養予定です。\n疲労・ストレス・プレイヤー好感度は0で加入します。\n\n店先での記録\n"+(string.IsNullOrEmpty(cat.history) ? "まだ交流していません。" : cat.history);
            if(cat.status=="accepted")details.text+="\n"+cat.accepted_day+"日目に迎えました。現在の状態は猫の詳細で確認できます。";
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();var cat=current?.visiting_cat;
            bool ready=online && client.CanManageSaves && cat!=null;
            open.interactable=ready;interact.interactable=ready && selection==null && cat.can_interact;
            skip.interactable=ready && selection==null && cat.can_skip;accept.interactable=ready && selection==null && cat.can_accept;
            housing.interactable=ready && selection==null && (cat.status=="visiting" || cat.status=="ready");
            confirm.interactable=ready && selection!=null && client.CanRespondVisiting(selection);cancel.interactable=true;close.interactable=true;
            if(cat==null)return;
            notice.text=!online ? "Python未接続です。再接続後に交流・加入できます。"
                : !client.CanManageSaves ? "操作を送信しています。完了するまでお待ちください。"
                : cat.status=="untriggered" ? "人気第1段階を達成すると、翌日の準備から店先に通うようになります。"
                : cat.status=="accepted" ? cat.accepted_day+"日目に迎えました。猫一覧で出勤・休養を選べます。"
                : !string.IsNullOrEmpty(cat.reason) ? cat.reason
                : cat.status=="ready" ? (string.IsNullOrEmpty(cat.accept_reason) ? "十分に慣れています。迎えるか、後日の準備まで待てます。" : cat.accept_reason+" 進捗は保持されます。")
                : !string.IsNullOrEmpty(cat.daily_reason) ? cat.daily_reason : "あと"+(cat.required-cat.progress)+"回交流すると迎えられます。交流せず営業へ進むこともできます。";
        }
        public void Disconnect() { online=false;ClearConfirmation();RefreshControls(); }
    }
}
