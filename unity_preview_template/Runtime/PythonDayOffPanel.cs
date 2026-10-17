using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonDayOff
    {public bool can_select,ends_game;public string reason,details;public int day_after;public float cost,funds_after;}
    public class PythonDayOffPanel : MonoBehaviour
    {
        public GameObject panel,confirmation;
        public Text title,details,notice,confirmationText;
        public Button proceed,close,confirm,cancel;
        private PythonCafeState current;
        private string identity,dayIdentity;
        private bool online,choosing;
        public void Open()
        {
            if(current?.day_off==null || !GetComponent<PythonStateClient>().CanManageSaves)return;
            EnsureResolveButton();Clear();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();
            Canvas.ForceUpdateCanvases();details.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition=1;
        }
        public void ResolveBlock(){Close();if(current?.player_interaction?.active==true)GetComponent<PythonPlayerPanel>()?.Open();else GetComponent<PythonEventListPanel>()?.Open();}
        private Button resolveBlock;
        private void Clear(){choosing=false;confirmation.SetActive(false);}
        public void Close(){Clear();panel.SetActive(false);}
        public void Choose()
        {
            if(!online || !GetComponent<PythonStateClient>().CanDayOff)return;
            choosing=true;confirmation.SetActive(true);var row=current.day_off;
            confirmationText.text=current.day+"日目を休業しますか？\n来客・接客はありません。\n今回の支出："+row.cost.ToString("0.#")+"\n資金："+current.funds.ToString("0.#")+" → "+row.funds_after.ToString("0.#")
                +"\n\n"+(row.ends_game ? "休業の精算で営業が終了します。翌日には進みません。" : row.day_after+"日目の準備へ進みます。\n目標の期限・派遣・療養の日程も進みます。\n新しい確認待ちが発生する場合があります。");RefreshControls();
        }
        public void Confirm(){if(!online || !choosing || !GetComponent<PythonStateClient>().CanDayOff)return;Clear();GetComponent<PythonStateClient>().DayOff();}
        public void Cancel(){Clear();RefreshControls();}
        public void Apply(PythonCafeState state)
        {
            string dayKey=state.instance_id+"/"+state.day;if(dayIdentity!=dayKey){dayIdentity=dayKey;Close();}
            string key=state.instance_id+"/"+state.revision;if(identity!=key){identity=key;Clear();}
            current=state;online=true;if(!state.can_set_shifts || state.closed || state.game_over!=null || state.day_off==null)Close();Render();
        }
        private void EnsureResolveButton()
        {
            if(resolveBlock!=null)return;resolveBlock=Instantiate(close,close.transform.parent);resolveBlock.name="Resolve day off block";resolveBlock.onClick=new Button.ButtonClickedEvent();resolveBlock.onClick.AddListener(ResolveBlock);var rect=resolveBlock.GetComponent<RectTransform>();rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,210);var closeRect=close.GetComponent<RectTransform>();closeRect.anchoredPosition+=new Vector2(226,0);closeRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,210);
        }
        private void Render()
        {
            if(current==null)return;title.text=current.day+"日目・今日は休業する";
            var row=current.day_off;details.text=row==null ? "休業の情報を取得していません。" : GetComponent<PythonStateClient>().CanDayOff ? row.details : "休業できません。\n\n"+GetComponent<PythonStateClient>().DayOffBlockReason;
            details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(392,details.preferredHeight+12));RefreshControls();
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();
            if(current!=null){details.text=client.CanDayOff?current.day_off?.details:"休業できません。\n\n"+client.DayOffBlockReason;details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(392,details.preferredHeight+12));}
            proceed.interactable=online && !choosing && client.CanDayOff;confirm.interactable=online && choosing && client.CanDayOff;close.interactable=cancel.interactable=true;
            if(resolveBlock!=null){resolveBlock.gameObject.SetActive(!client.CanDayOff);resolveBlock.interactable=online && client.CanManageSaves;resolveBlock.GetComponentInChildren<Text>().text=current?.player_interaction?.active==true?"交流へ戻る":"イベント一覧で確認";}
            notice.text=!online ? "Python未接続です。再接続後に休業できます。" : !client.CanManageSaves ? "操作を送信しています。完了するまでお待ちください。" : client.DayOffBlockReason;
            if(online && client.CanManageSaves && string.IsNullOrEmpty(notice.text))notice.text="費用と猫の変化を確認し、確認画面で休業を確定してください。";
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
