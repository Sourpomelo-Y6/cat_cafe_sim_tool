using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonExpansion
    {public bool can_expand,has_next;public string reason,details;public int seats,next_seats,max_seats;public float cost,funds_after,operating_cost,next_operating_cost;}
    public class PythonExpansionPanel : MonoBehaviour
    {
        public GameObject panel,confirmation;
        public Text title,details,notice,confirmationText,summary;
        public Button proceed,close,confirm,cancel;
        private PythonCafeState current;
        private string identity,dayIdentity;
        private bool online,choosing;
        public void Open()
        {
            if(current?.expansion==null || !GetComponent<PythonStateClient>().CanManageSaves)return;
            Clear();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();
            Canvas.ForceUpdateCanvases();details.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition=1;
        }
        private void Clear(){choosing=false;confirmation.SetActive(false);}
        public void Close(){Clear();panel.SetActive(false);}
        public void Choose()
        {
            if(!online || !GetComponent<PythonStateClient>().CanExpandSeats)return;
            choosing=true;confirmation.SetActive(true);var row=current.expansion;
            confirmationText.text="席を増設しますか？\n席数："+row.seats+" → "+row.next_seats+"席\n費用："+row.cost.ToString("0.#")+"\n資金："+current.funds.ToString("0.#")+" → "+row.funds_after.ToString("0.#")
                +"\n日次運営費："+row.operating_cost.ToString("0.#")+" → "+row.next_operating_cost.ToString("0.#")+"\n\n新しい席は購入直後から使えます。\n飼育できる猫の上限は変わりません。\n増設当日から、休業日も日次運営費を精算します。";RefreshControls();
        }
        public void Confirm(){if(!online || !choosing || !GetComponent<PythonStateClient>().CanExpandSeats)return;Clear();GetComponent<PythonStateClient>().ExpandSeats();}
        public void Cancel(){Clear();RefreshControls();}
        public void Apply(PythonCafeState state)
        {
            string dayKey=state.instance_id;if(dayIdentity!=dayKey){dayIdentity=dayKey;Close();}
            string key=state.instance_id+"/"+state.revision;if(identity!=key){identity=key;Clear();}
            current=state;online=true;if(state.game_over!=null || state.expansion==null)Close();Render();
        }
        private void Render()
        {
            if(current==null)return;title.text="席数の増設";
            var row=current.expansion;if(summary!=null)summary.text=row==null?"情報なし":"現在："+row.seats+"席 / 上限："+row.max_seats+"席 / 資金："+current.funds.ToString("0.#")+"\n"+(row.has_next?"次の増設："+row.next_seats+"席 / 費用："+row.cost.ToString("0.#")+" / 購入後の資金："+row.funds_after.ToString("0.#"):"増設できる席はありません。");details.text=row==null ? "席数の情報を取得していません。" : row.details;
            details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(392,details.preferredHeight+12));RefreshControls();
        }
        public void RefreshControls()
        {
            proceed.GetComponentInChildren<Text>().text=current?.expansion?.has_next==true?current.expansion.next_seats+"席へ増設 / 費用 "+current.expansion.cost.ToString("0.#")+"（確認）":"増設できる席はありません";
            var client=GetComponent<PythonStateClient>();proceed.interactable=online && !choosing && client.CanExpandSeats;confirm.interactable=online && choosing && client.CanExpandSeats;close.interactable=cancel.interactable=true;
            notice.text=!online ? "Python未接続です。再接続後に増設できます。" : !client.CanManageSaves ? "操作を送信しています。完了するまでお待ちください。" : current?.expansion?.reason ?? "";
            if(online && client.CanManageSaves && string.IsNullOrEmpty(notice.text))notice.text="費用・日次運営費・条件を確認し、確認画面で増設を確定してください。";
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
