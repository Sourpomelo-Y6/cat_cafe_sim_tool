using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonRestSpaceAction
    { public string kind,label,reason; public bool can_execute; public float cost,funds_after,recovery_before,recovery_after,stress_before,stress_after; }
    [Serializable] public class PythonRestSpace
    { public string status,details,notes; public float recovery_bonus,stress_recovery_bonus; public PythonRestSpaceAction[] actions; }
    public class PythonRestSpacePanel : MonoBehaviour
    {
        public GameObject panel,confirmation;
        public Text title,details,notice,confirmationText;
        public Button purchase,upgrade,soundproof,close,confirm,cancel;
        private PythonCafeState current;
        private string instance,identity,selected;
        private bool online;
        private PythonStateClient Client => GetComponent<PythonStateClient>();
        private PythonRestSpaceAction Action(string kind){return Array.Find(current?.rest_space?.actions ?? new PythonRestSpaceAction[0],r=>r.kind==kind);}
        private void Clear(){selected=null;confirmation.SetActive(false);}
        public void Open(){if(current?.rest_space==null || !Client.CanManageSaves)return;Clear();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();Canvas.ForceUpdateCanvases();details.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition=1;}
        public void Close(){Clear();panel.SetActive(false);}
        public void Purchase(){Choose("purchase_rest_space");}
        public void Upgrade(){Choose("upgrade_rest_space");}
        public void Soundproof(){Choose("soundproof_rest_space");}
        private void Choose(string kind)
        {
            if(!online || !Client.CanRestSpace(kind))return;
            var row=Action(kind);selected=kind;
            confirmationText.text="休養スペースを"+row.label+"しますか？\n費用："+row.cost.ToString("0.#")+"\n資金："+current.funds.ToString("0.#")+" → "+row.funds_after.ToString("0.#")
                +"\n追加の疲労回復：＋"+row.recovery_before.ToString("0.#")+" → ＋"+row.recovery_after.ToString("0.#")+"\n追加のストレス回復：＋"+row.stress_before.ToString("0.#")+" → ＋"+row.stress_after.ToString("0.#")
                +"\n\n"+current.rest_space.notes+"\n各操作は1回限りです。";
            confirmation.SetActive(true);confirmation.transform.SetAsLastSibling();RefreshControls();
        }
        public void Confirm(){if(!online || selected==null || !Client.CanRestSpace(selected))return;var kind=selected;Clear();Client.RestSpace(kind);}
        public void Cancel(){Clear();RefreshControls();}
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;Close();}
            var key=state.instance_id+"/"+state.revision;if(identity!=key){identity=key;Clear();}
            current=state;online=true;if(state.game_over!=null || state.rest_space==null)Close();Render();
        }
        private void Render(){title.text="休養スペース";details.text=current?.rest_space?.details ?? "休養設備の情報を取得していません。";details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(392,details.preferredHeight+12));RefreshControls();}
        public void RefreshControls()
        {
            purchase.interactable=online && selected==null && Client.CanRestSpace("purchase_rest_space");upgrade.interactable=online && selected==null && Client.CanRestSpace("upgrade_rest_space");soundproof.interactable=online && selected==null && Client.CanRestSpace("soundproof_rest_space");confirm.interactable=online && selected!=null && Client.CanRestSpace(selected);close.interactable=cancel.interactable=true;
            notice.text=!online ? "Python未接続です。再接続後に操作できます。" : !Client.CanManageSaves ? "操作を送信しています。" : "各操作の条件・費用・効果を確認し、確認画面で確定してください。";
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
