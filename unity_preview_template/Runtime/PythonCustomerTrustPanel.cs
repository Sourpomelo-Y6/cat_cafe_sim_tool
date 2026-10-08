using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonCustomerTrust{public float popularity_loss;public PythonTrustEvent[] events;}
    [Serializable] public class PythonTrustEvent{public string event_id,customer_id,name,status,outcome,choice,reason;public int day,suspensions,resolved_day;public bool can_respond;}
    public class PythonCustomerTrustPanel : MonoBehaviour
    {
        public GameObject panel,confirmation;public Text title,details,notice,confirmationText;public Button recover,ignore,close,confirm,cancel;
        private PythonCafeState current;private string instance,identity,waiting,selection;private bool online;
        private PythonTrustEvent Pending{get{return current?.customer_trust?.events?.FirstOrDefault(e=>e.status=="waiting");}}
        public void Open(){if(current?.customer_trust==null || !GetComponent<PythonStateClient>().CanManageSaves)return;Clear();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();}
        public void Close(){Clear();panel.SetActive(false);}private void Clear(){selection=null;confirmation.SetActive(false);}
        public void Recover(){Choose("recover");}public void Ignore(){Choose("ignore");}
        private void Choose(string choice)
        {
            var row=Pending;if(!online || row==null || !GetComponent<PythonStateClient>().CanRespondTrust(row.event_id))return;
            selection=choice;confirmation.SetActive(true);
            confirmationText.text=choice=="recover" ? row.name+"の信頼回復に取り組みますか？\n費用はかかりません。停止期間後の接客が\n「満足」なら回復、「不満」なら永久離脱します。" : row.name+"への対応をしませんか？\n永久離脱し、人気が"+current.customer_trust.popularity_loss.ToString("0.#")+"下がります。\n費用はかかりません。";RefreshControls();
        }
        public void Confirm(){var row=Pending;if(!online || selection==null || row==null || !GetComponent<PythonStateClient>().CanRespondTrust(row.event_id))return;var choice=selection;Clear();GetComponent<PythonStateClient>().RespondTrust(row.event_id,choice);}
        public void Cancel(){Clear();RefreshControls();}
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;waiting=null;Close();}current=state;online=true;
            var key=state.instance_id+"/"+state.revision+"/"+Pending?.event_id;if(identity!=key){identity=key;Clear();}
            var next=Pending?.event_id;if(next!=null && waiting!=next){panel.SetActive(true);panel.transform.SetAsLastSibling();}waiting=next;
            if(state.customer_trust==null || state.game_over!=null)panel.SetActive(false);Render();RefreshControls();
        }
        static string Status(PythonTrustEvent row){return row.outcome=="recovered" ? "回復" : row.outcome=="departed" ? "永久離脱" : row.status=="waiting" ? "回答待ち" : "信頼回復中";}
        private void Render()
        {
            if(current?.customer_trust==null)return;title.text="お客さんの信頼回復";
            details.text="来店停止を繰り返したお客さんの対応方針を選びます。\n費用はかかりません。\n\n信頼回復に取り組む：停止期間後の接客が「満足」なら回復、\n「不満」なら永久離脱します。\n対応しない：永久離脱し、人気が"+current.customer_trust.popularity_loss.ToString("0.#")+"下がります。\n\n"
                +(Pending==null ? "回答待ちはありません。" : "回答するお客さん："+Pending.name+" / 来店停止 "+Pending.suspensions+"回")+"\n\n履歴\n"+string.Join("\n",(current.customer_trust.events ?? new PythonTrustEvent[0]).Select(e=>e.day+"日目："+e.name+" / 停止 "+e.suspensions+"回 / "+Status(e)));
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();var row=Pending;bool ready=online && row!=null && client.CanRespondTrust(row.event_id);
            recover.interactable=ignore.interactable=ready && selection==null;confirm.interactable=ready && selection!=null;close.interactable=cancel.interactable=true;
            notice.text=!online ? "Python未接続です。再接続後に回答できます。" : !client.CanManageSaves ? "操作を送信しています。完了するまでお待ちください。" : row==null ? "回答待ちの信頼回復イベントはありません。" : !string.IsNullOrEmpty(row.reason) ? row.reason : row.name+"が離脱を考えています。対応方針を選んでください。";
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
