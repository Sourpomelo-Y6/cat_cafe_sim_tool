using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonRegularIntroduction
    {
        public string cat_id, content_cat_id, name, status, customer_id, customer_name, personality, features, trait, preferences, housing, response_reason, accept_reason;
        public int presented_day, resolved_day;
        public float cost, funds_after, threshold;
        public bool can_accept, can_decline;
    }
    public class PythonRegularIntroductionPanel : MonoBehaviour
    {
        public GameObject panel, confirmation;
        public RawImage picture;
        public Text title, details, notice, confirmationText;
        public Button accept, decline, housing, close, confirm, cancel;
        private PythonCafeState current;
        private string identity, selection, lastWaiting, instance;
        private bool online;
        public void Open()
        {
            if(CafeBackends.Standalone && (CafeBackends.Local as LocalCafeBackend)?.RegularConversation?.completed==false){GetComponent<RegularConversationPanel>().Open();return;}
            if(current?.regular_introduction==null || !GetComponent<PythonStateClient>().CanManageSaves)return;
            Clear();if(CafeBackends.Standalone)GetComponent<RegularConversationPanel>()?.ShowOriginal();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();RefreshControls();
            Canvas.ForceUpdateCanvases();details.GetComponentInParent<ScrollRect>().verticalNormalizedPosition=1;
        }
        public void Close(){if(CafeBackends.Standalone && GetComponent<RegularConversationPanel>()!=null){GetComponent<RegularConversationPanel>().Close();return;}Clear();panel.SetActive(false);}
        public void Accept(){Choose("accept");} public void Decline(){Choose("decline");}
        public void Housing(){Clear();GetComponent<PythonCatAcquisitionNavigation>().OpenHousing(panel);}
        private void Choose(string choice)
        {
            if(!online || !GetComponent<PythonStateClient>().CanRespondRegular(choice))return;
            selection=choice;confirmation.SetActive(true);var row=current.regular_introduction;
            confirmationText.text=choice=="accept" ? row.customer_name+"から紹介された"+row.name+"を迎えますか？\n初期費用："+row.cost.ToString("0.#")+" / 迎えた後の資金："+row.funds_after.ToString("0.#")+"\n健康・体力全回復・休養予定で加入します。"
                : row.name+"の受け入れを見送りますか？\n費用・人気・常連度は変わりません。\nこの紹介は再発生しません。";
            RefreshControls();
        }
        public void Confirm(){if(!online || selection==null || !GetComponent<PythonStateClient>().CanRespondRegular(selection))return;var choice=selection;if(choice=="accept")GetComponent<PythonCatAcquisitionNavigation>()?.Watch(current.regular_introduction.cat_id);Clear();GetComponent<PythonStateClient>().RespondRegular(choice);}
        public void Cancel(){Clear();RefreshControls();}
        private void Clear(){selection=null;confirmation.SetActive(false);}
        public void Apply(PythonCafeState state)
        {
            if(CafeBackends.Standalone){current=state;online=true;var dialogue=GetComponent<RegularConversationPanel>() ?? gameObject.AddComponent<RegularConversationPanel>();dialogue.ApplyRegular(state,this);return;}
            if(instance!=state.instance_id){instance=state.instance_id;panel.SetActive(false);lastWaiting=null;Clear();}
            var row=state.regular_introduction;current=state;online=true;
            string key=state.instance_id+"/"+state.revision+"/"+state.day+"/"+row?.cat_id+"/"+row?.status;
            if(identity!=key){identity=key;Clear();}
            string waiting=row?.status=="waiting" ? state.instance_id+"/"+row.cat_id+"/"+row.presented_day : null;
            if(waiting!=null && waiting!=lastWaiting){panel.SetActive(true);panel.transform.SetAsLastSibling();}
            lastWaiting=waiting;
            if(row==null || state.game_over!=null)panel.SetActive(false);
            Render();RefreshControls();
        }
        private void Render()
        {
            var row=current?.regular_introduction;if(row==null)return;
            title.text=string.IsNullOrEmpty(row.customer_name) ? "常連からの猫紹介 · まだ届いていません" : row.customer_name+"から "+row.name+"の紹介";
            picture.texture=GetComponent<ContentPreview>().CandidateCatTexture(row.content_cat_id);picture.enabled=picture.texture!=null;
            if(picture.texture!=null)picture.GetComponent<AspectRatioFitter>().aspectRatio=(float)picture.texture.width/picture.texture.height;
            details.text="名前 / 個性："+row.name+" / "+row.personality+"\n特徴："+row.features+"\n"+row.trait+"\n\n飼育スペース："+row.housing
                +"\n初期費用："+row.cost.ToString("0.#")+"\n所持金："+current.funds.ToString("0.#")+" → 迎えた後："+row.funds_after.ToString("0.#")
                +"\n\n加入時は健康・体力全回復・休養予定。\n疲労・ストレス・プレイヤー好感度は0です。\n\n"+row.preferences
                +(row.presented_day>0 ? "\n\n"+row.presented_day+"日目に紹介が届きました。" : "")
                +(row.resolved_day>0 ? "\n"+row.resolved_day+"日目に"+(row.status=="accepted" ? "迎えました。" : "見送りました。") : "");
        }
        public void RefreshControls()
        {
            if(CafeBackends.Standalone && GetComponent<RegularConversationPanel>()!=null && !GetComponent<RegularConversationPanel>().OriginalShowing){GetComponent<RegularConversationPanel>().Refresh();return;}
            var client=GetComponent<PythonStateClient>();var row=current?.regular_introduction;bool ready=online && client.CanManageSaves && row!=null;
            accept.interactable=ready && selection==null && row.can_accept;decline.interactable=ready && selection==null && row.can_decline;
            housing.interactable=ready && selection==null && row.status=="waiting";
            confirm.interactable=ready && selection!=null && client.CanRespondRegular(selection);cancel.interactable=close.interactable=true;
            if(row==null)return;
            notice.text=!online ? "Python未接続です。再接続後に回答できます。" : !client.CanManageSaves ? "操作を送信しています。完了するまでお待ちください。"
                : row.status=="untriggered" ? "常連度"+row.threshold.ToString("0.#")+"に達したお客さんから、翌日の準備中に一度だけ紹介が届きます。"
                : row.status=="accepted" ? row.resolved_day+"日目に迎えました。猫一覧から現在の状態を確認できます。"
                : row.status=="declined" ? row.resolved_day+"日目に見送りました。この紹介は再発生しません。"
                : !string.IsNullOrEmpty(row.response_reason) ? row.response_reason
                : !string.IsNullOrEmpty(row.accept_reason) ? row.accept_reason+" 見送ることができます。" : "迎えるか見送るかを選ぶと、営業準備を続けられます。";
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
