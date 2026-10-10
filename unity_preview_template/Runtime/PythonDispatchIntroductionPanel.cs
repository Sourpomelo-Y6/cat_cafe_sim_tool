using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonDispatchIntroduction
    {
        public string event_id, destination, sender_name, cat_id, content_cat_id, name, status, personality, features, trait, preferences, housing, response_reason, accept_reason;
        public int presented_day, resolved_day;
        public float cost, funds_after;
        public bool can_accept, can_decline;
    }
    public class PythonDispatchIntroductionPanel : MonoBehaviour
    {
        public GameObject panel, confirmation;
        public RawImage picture;
        public Dropdown records;
        public Text title, details, notice, confirmationText;
        public Button accept, decline, housing, close, confirm, cancel;
        private PythonCafeState current;
        private string identity, selection, lastWaiting, instance;
        private bool online;
        private PythonDispatchIntroduction[] events=new PythonDispatchIntroduction[0];
        public PythonDispatchIntroduction Row { get { return records.value<events.Length ? events[records.value] : null; } }
        public void Select(int _){Clear();Render();RefreshControls();GetComponent<PythonCatAcquisitionNavigation>()?.Refresh();Canvas.ForceUpdateCanvases();details.GetComponentInParent<ScrollRect>().verticalNormalizedPosition=1;}
        public void Open()
        {
            if(Row==null || !GetComponent<PythonStateClient>().CanManageSaves)return;
            Clear();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();
            Canvas.ForceUpdateCanvases();details.GetComponentInParent<ScrollRect>().verticalNormalizedPosition=1;
        }
        public void Close(){Clear();panel.SetActive(false);}
        public void Accept(){Choose("accept");} public void Decline(){Choose("decline");}
        public void Housing(){Clear();GetComponent<PythonCatAcquisitionNavigation>().OpenHousing(panel);}
        private void Choose(string choice)
        {
            if(!online || !GetComponent<PythonStateClient>().CanRespondDispatchIntroduction(Row?.event_id,choice))return;
            selection=choice;confirmation.SetActive(true);var row=Row;
            confirmationText.text=choice=="accept" ? row.destination+"から紹介された"+row.name+"を迎えますか？\n初期費用："+row.cost.ToString("0.#")+" / 迎えた後の資金："+row.funds_after.ToString("0.#")+"\n健康・体力全回復・休養予定で加入します。"
                : row.name+"の受け入れを見送りますか？\n費用・人気・派遣報酬は変わりません。\nこの候補への回答は変更できません。";
            RefreshControls();
        }
        public void Confirm(){if(!online || selection==null || !GetComponent<PythonStateClient>().CanRespondDispatchIntroduction(Row?.event_id,selection))return;string choice=selection,eventId=Row.event_id;if(choice=="accept")GetComponent<PythonCatAcquisitionNavigation>()?.Watch(Row.cat_id);Clear();GetComponent<PythonStateClient>().RespondDispatchIntroduction(eventId,choice);}
        public void Cancel(){Clear();RefreshControls();}
        private void Clear(){selection=null;confirmation.SetActive(false);}
        public void Apply(PythonCafeState state)
        {
            string old=Row?.event_id;
            if(instance!=state.instance_id){instance=state.instance_id;old=null;panel.SetActive(false);lastWaiting=null;Clear();}
            current=state;online=true;string key=state.instance_id+"/"+state.revision;
            if(identity!=key){identity=key;Clear();}
            events=state.dispatch_introductions ?? new PythonDispatchIntroduction[0];
            var pending=events.FirstOrDefault(e=>e.status=="waiting");bool newlyWaiting=pending!=null && pending.event_id!=lastWaiting;lastWaiting=pending?.event_id;
            int index=Array.FindIndex(events,e=>e.event_id==(newlyWaiting ? pending.event_id : old));if(index<0)index=0;
            records.options.Clear();foreach(var ev in events)records.options.Add(new Dropdown.OptionData(ev.destination+" / "+ev.name+"（"+(ev.status=="scheduled" ? "帰還後に紹介" : ev.status=="waiting" ? "回答待ち" : ev.status=="accepted" ? "加入済み" : "見送り済み")+"）"));
            if(events.Length==0)records.options.Add(new Dropdown.OptionData("紹介記録なし"));records.SetValueWithoutNotify(index);records.RefreshShownValue();
            if(newlyWaiting){panel.SetActive(true);panel.transform.SetAsLastSibling();}
            if(events.Length==0 || state.game_over!=null)panel.SetActive(false);Render();RefreshControls();if(newlyWaiting){Canvas.ForceUpdateCanvases();details.GetComponentInParent<ScrollRect>().verticalNormalizedPosition=1;}
        }
        private void Render()
        {
            var row=Row;if(row==null)return;
            title.text="派遣先からの猫紹介";
            picture.texture=GetComponent<ContentPreview>().CandidateCatTexture(row.content_cat_id);picture.enabled=picture.texture!=null;
            if(picture.texture!=null)picture.GetComponent<AspectRatioFitter>().aspectRatio=(float)picture.texture.width/picture.texture.height;
            details.text="派遣先："+row.destination+" / 派遣した猫："+row.sender_name+"\n\n名前 / 個性："+row.name+" / "+row.personality+"\n特徴："+row.features+"\n"+row.trait+"\n\n飼育スペース："+row.housing
                +"\n初期費用："+row.cost.ToString("0.#")+"\n所持金："+current.funds.ToString("0.#")+" → 迎えた後："+row.funds_after.ToString("0.#")
                +"\n\n加入時は健康・体力全回復・休養予定。\n疲労・ストレス・プレイヤー好感度は0です。\n\n"+row.preferences
                +(row.presented_day>0 ? "\n\n"+row.presented_day+"日目に紹介が届きました。" : "")
                +(row.resolved_day>0 ? "\n"+row.resolved_day+"日目に"+(row.status=="accepted" ? "迎えました。" : "見送りました。") : "");
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();var row=Row;bool ready=online && client.CanManageSaves && row!=null;
            accept.interactable=ready && selection==null && row.can_accept;decline.interactable=ready && selection==null && row.can_decline;
            housing.interactable=ready && selection==null && row.status=="waiting";
            confirm.interactable=ready && selection!=null && client.CanRespondDispatchIntroduction(Row?.event_id,selection);records.interactable=ready && selection==null && events.Length>0;cancel.interactable=close.interactable=true;
            if(row==null){notice.text="派遣先からの紹介記録はありません。";return;}
            notice.text=!online ? "Python未接続です。再接続後に回答できます。" : !client.CanManageSaves ? "操作を送信しています。完了するまでお待ちください。"
                : row.status=="scheduled" ? "帰還を確認した後、営業準備中に紹介への回答ができます。"
                : row.status=="accepted" ? row.resolved_day+"日目に迎えました。猫一覧から現在の状態を確認できます。"
                : row.status=="declined" ? row.resolved_day+"日目に見送りました。この候補への回答は変更できません。"
                : !string.IsNullOrEmpty(row.response_reason) ? row.response_reason
                : !string.IsNullOrEmpty(row.accept_reason) ? row.accept_reason+" 見送ることができます。" : "迎えるか見送るかを選ぶと、営業準備を続けられます。";
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
