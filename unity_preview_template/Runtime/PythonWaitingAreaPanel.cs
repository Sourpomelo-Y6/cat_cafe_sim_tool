using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonWaitingAreaAction
    { public string kind,label,reason; public bool can_execute,completed; public float cost,funds_after,capacity_before,capacity_after,wait_before,wait_after,daily_before,daily_after,operating_before,operating_after; }
    [Serializable] public class PythonWaitingArea
    { public string status,details,notes; public int capacity,max_wait_ticks; public float daily_cost,operating_cost; public PythonWaitingAreaAction[] actions; }
    public class PythonWaitingAreaPanel : MonoBehaviour
    {
        public GameObject panel,confirmation;
        public Text title,details,notice,confirmationText,summary;
        public Button purchase,upgrade,close,confirm,cancel;
        private PythonCafeState current;
        private string instance,identity,selected;
        private bool online;
        private PythonStateClient Client => GetComponent<PythonStateClient>();
        private PythonWaitingAreaAction Action(string kind){return Array.Find(current?.waiting_area?.actions ?? new PythonWaitingAreaAction[0],r=>r.kind==kind);}
        private void Clear(){selected=null;confirmation.SetActive(false);}
        public void Open(){if(current?.waiting_area==null || !Client.CanManageSaves)return;Clear();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();Canvas.ForceUpdateCanvases();details.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition=1;}
        public void Close(){Clear();panel.SetActive(false);}
        public void Purchase(){Choose("purchase_waiting_area");}
        public void Upgrade(){Choose("upgrade_waiting_area");}
        private void Choose(string kind)
        {
            if(!online || !Client.CanWaitingArea(kind))return;
            var row=Action(kind);selected=kind;
            confirmationText.text="待合スペースを"+row.label+"しますか？\n費用："+row.cost.ToString("0.#")+"\n資金："+current.funds.ToString("0.#")+" → "+row.funds_after.ToString("0.#")
                +"\n待機上限："+row.capacity_before.ToString("0")+" → "+row.capacity_after.ToString("0")+"人\n待機猶予："+row.wait_before.ToString("0")+" → "+row.wait_after.ToString("0")+"刻\n待合の維持費："+row.daily_before.ToString("0.#")+" → "+row.daily_after.ToString("0.#")+"\n日次運営費合計："+row.operating_before.ToString("0.#")+" → "+row.operating_after.ToString("0.#")
                +"\n\n"+current.waiting_area.notes;
            confirmation.SetActive(true);confirmation.transform.SetAsLastSibling();RefreshControls();
        }
        public void Confirm(){if(!online || selected==null || !Client.CanWaitingArea(selected))return;var kind=selected;Clear();Client.WaitingArea(kind);}
        public void Cancel(){Clear();RefreshControls();}
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;Close();}
            var key=state.instance_id+"/"+state.revision;if(identity!=key){identity=key;Clear();}
            current=state;online=true;if(state.game_over!=null || state.waiting_area==null)Close();Render();
        }
        private void Render(){title.text="待合スペース";if(summary!=null)summary.text="現在："+(current?.waiting_area?.status ?? "情報なし")+" / 資金："+(current?.funds ?? 0).ToString("0.#")+"\n待機上限："+(current?.waiting_area?.capacity ?? 0)+"人 / 待機猶予："+(current?.waiting_area?.max_wait_ticks ?? 0)+"刻 / 維持費："+(current?.waiting_area?.daily_cost ?? 0).ToString("0.#")+" / 日";details.text="購入・強化の費用と条件\n"+string.Join("\n\n",Array.ConvertAll(current?.waiting_area?.actions ?? new PythonWaitingAreaAction[0],a=>a.label+"：費用 "+a.cost.ToString("0.#")+" / 購入後の資金 "+a.funds_after.ToString("0.#")+"\n待機上限 "+a.capacity_before.ToString("0")+" → "+a.capacity_after.ToString("0")+"人 / 待機猶予 "+a.wait_before.ToString("0")+" → "+a.wait_after.ToString("0")+"刻\n維持費 "+a.daily_before.ToString("0.#")+" → "+a.daily_after.ToString("0.#")+" / 日次運営費合計 "+a.operating_before.ToString("0.#")+" → "+a.operating_after.ToString("0.#")+"\n"+(a.can_execute?"実行できます。":"実行できない理由："+a.reason)))+"\n\n設備の詳細・解放条件・履歴\n"+(current?.waiting_area?.details ?? "待合の情報を取得していません。");details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(392,details.preferredHeight+12));RefreshControls();}
        public void RefreshControls()
        {
            purchase.GetComponentInChildren<Text>().text="購入"+(Action("purchase_waiting_area")==null?"":" "+Action("purchase_waiting_area").cost.ToString("0.#"));
            upgrade.GetComponentInChildren<Text>().text="強化"+(Action("upgrade_waiting_area")==null?"":" "+Action("upgrade_waiting_area").cost.ToString("0.#"));
            purchase.interactable=online && selected==null && Client.CanWaitingArea("purchase_waiting_area");upgrade.interactable=online && selected==null && Client.CanWaitingArea("upgrade_waiting_area");confirm.interactable=online && selected!=null && Client.CanWaitingArea(selected);close.interactable=cancel.interactable=true;
            notice.text=!online ? "Python未接続です。再接続後に操作できます。" : !Client.CanManageSaves ? "操作を送信しています。" : "各操作の条件・費用・効果を確認し、確認画面で確定してください。";
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
