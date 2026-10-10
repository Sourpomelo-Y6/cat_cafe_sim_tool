using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonHousingPanel : MonoBehaviour
    {
        public GameObject panel,confirmation;
        public Text details,notice,summary,confirmationText;
        public Button open,expand,close,confirm,cancel;
        private PythonCafeState current;
        private string instance,identity;
        private bool online,choosing;
        private PythonStateClient Client=>GetComponent<PythonStateClient>();
        private static string N(float value)=>value.ToString("0.#");
        private void Clear(){choosing=false;if(confirmation!=null)confirmation.SetActive(false);}
        public void Open(){if(Client.CurrentState?.housing==null || !Client.CanManageSaves)return;GetComponent<PythonMenuPanel>()?.Close();Clear();panel.SetActive(true);panel.transform.SetAsLastSibling();Apply(Client.CurrentState);Canvas.ForceUpdateCanvases();var scroll=details.GetComponentInParent<ScrollRect>();if(scroll!=null)scroll.verticalNormalizedPosition=1;}
        public void Close(){Clear();panel.SetActive(false);}
        public void Expand()
        {
            if(!online || !Client.CanExpandHousing || confirmation==null)return;
            var h=current.housing;choosing=true;confirmationText.text="飼育スペースを拡張しますか？\n上限："+h.capacity+" → "+h.next_capacity+"匹\n費用："+N(h.cost)+"\n資金："+N(current.funds)+" → "+N(h.funds_after)+"\n飼育維持費："+N(h.daily_cost)+" → "+N(h.next_daily_cost)+" / 日\n日次運営費合計："+N(h.operating_cost)+" → "+N(h.next_operating_cost)+"\n\n維持費は拡張当日から、休業日も精算します。\n接客席数は変わりません。";confirmation.SetActive(true);confirmation.transform.SetAsLastSibling();RefreshControls();
        }
        public void Confirm(){if(!online || !choosing || !Client.CanExpandHousing)return;Clear();Client.ExpandHousing();}
        public void Cancel(){Clear();RefreshControls();}
        public void Apply(PythonCafeState state)
        {
            if(instance!=state?.instance_id){instance=state?.instance_id;Close();}
            string key=state?.instance_id+"/"+state?.revision;if(identity!=key){identity=key;Clear();}
            current=state;online=state!=null;var h=state?.housing;
            if(h==null){Close();details.text="飼育スペースの情報を取得してください。";if(summary!=null)summary.text="情報なし";RefreshControls();return;}
            if(state.game_over!=null)Close();
            if(summary!=null)summary.text="在籍："+h.count+"匹 / "+(h.configured?"上限："+h.capacity+"匹 / 空き："+h.free+"匹":"上限なし（従来ルール）")+"\n資金："+N(state.funds)+" / 飼育維持費："+N(h.daily_cost)+" / 日";
            details.text="現在の状態\n在籍："+h.count+"匹";
            if(!h.configured)details.text+="\n上限なし（従来ルール）";
            else
            {
                details.text+="\n上限："+h.capacity+"匹 / 空き："+h.free+"匹\n拡張段階："+h.stage+" / 2\n現在の飼育維持費："+N(h.daily_cost)+" / 日";
                details.text+=string.IsNullOrEmpty(h.kind)?"\n\n追加の拡張はありません。":"\n\n次の拡張と費用\n上限："+h.capacity+" → "+h.next_capacity+"匹\n費用："+N(h.cost)+"\n資金："+N(state.funds)+" → "+N(h.funds_after)+"\n飼育維持費："+N(h.daily_cost)+" → "+N(h.next_daily_cost)+" / 日\n日次運営費合計："+N(h.operating_cost)+" → "+N(h.next_operating_cost);
            }
            details.text+="\n\n拡張条件\n"+(h.can_expand?"拡張できます。各段階は1回ずつ利用できます。":"拡張できない理由："+h.reason)+"\n\n維持費は拡張当日から、休業日も精算します。\n派遣中・家出中の猫も在籍に含み、譲渡済みは除きます。\n接客席数は変わりません。";
            details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(300,details.preferredHeight+12));
            expand.GetComponentInChildren<Text>().text=string.IsNullOrEmpty(h.kind)?"追加の拡張はありません":h.next_capacity+"匹へ拡張 / 費用 "+N(h.cost)+"（確認）";RefreshControls();
        }
        public void RefreshControls(){open.interactable=Client.CanManageSaves;expand.interactable=online && !choosing && Client.CanExpandHousing;if(confirm!=null)confirm.interactable=online && choosing && Client.CanExpandHousing;close.interactable=true;if(cancel!=null)cancel.interactable=true;notice.text=!online?"Python未接続です。再接続後に拡張できます。":!Client.CanManageSaves?"操作を送信しています。完了するまでお待ちください。":"費用・維持費・条件を確認し、確認画面で確定してください。";}
        public void Disconnect(){online=false;Clear();RefreshControls();open.interactable=false;}
    }
}
