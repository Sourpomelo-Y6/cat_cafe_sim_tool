using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonAdoption { public bool enabled, can_configure; public string reason; public PythonAdoptionEvent[] events; }
    [Serializable] public class PythonAdoptionEvent
    {
        public string event_id, cat_id, name, customer_id, customer_name, status, choice, accept_reason, decline_reason;
        public int day;
        public float guest_affinity, player_affinity, threshold;
        public bool can_accept, can_decline;
    }
    public class PythonAdoptionPanel : MonoBehaviour
    {
        public GameObject panel, confirmation;
        public Text settings, details, notice, confirmationDetails;
        public Dropdown events;
        public Button configure, accept, decline, close, confirmAccept, cancel;
        private PythonAdoption current;
        private PythonAdoptionEvent[] rows=new PythonAdoptionEvent[0];
        private string selected, confirming, confirmationIdentity, signature, instanceId;
        private bool showing;
        static string Identity(PythonAdoptionEvent row)
        { return row.event_id+"|"+row.cat_id+"|"+row.customer_id+"|"+row.day+"|"+row.guest_affinity+"|"+row.player_affinity; }
        public void Open(string catId)
        {
            if(!GetComponent<PythonStateClient>().CanManageSaves)return;
            selected=rows.LastOrDefault(r=>r.cat_id==catId)?.event_id;showing=true;ResetConfirmation();GetComponent<PythonCatDetailsPanel>().Close();Render();
        }
        public void Close() { showing=false;ResetConfirmation();panel.SetActive(false); }
        public void ResetConfirmation() { confirming=null;confirmationIdentity=null;confirmation.SetActive(false); }
        public void CancelAccept() { ResetConfirmation();RefreshControls(); }
        public void Configure() { GetComponent<PythonStateClient>().ToggleAdoption(); }
        public void Select(int index)
        { if(index>=0 && index<rows.Length)selected=rows[index].event_id;ResetConfirmation();RenderDetails(); }
        public void Decline() { if(selected!=null)GetComponent<PythonStateClient>().ResolveAdoption(selected,"decline"); }
        public void BeginAccept()
        {
            var row=Array.Find(rows,r=>r.event_id==selected);
            if(row==null || !row.can_accept || !GetComponent<PythonStateClient>().CanManageSaves)return;
            confirming=row.event_id;confirmationIdentity=Identity(row);confirmationDetails.text=row.name+"を "+row.customer_name+"へ譲渡します。\n\nこのゲームでは、カフェの接客・派遣・プレイヤーとの交流に戻せません。\n\n猫の詳細と関係の記録は残ります。\n譲渡する猫と相手を確認してから確定してください。";
            confirmation.SetActive(true);RefreshControls();
        }
        public void ConfirmAccept() { if(confirming!=null)GetComponent<PythonStateClient>().ResolveAdoption(confirming,"accept"); }
        public void Apply(PythonCafeState state)
        {
            bool hadRecords=rows.Length>0;current=state.adoption;rows=current?.events ?? new PythonAdoptionEvent[0];
            if(instanceId!=state.instance_id) { instanceId=state.instance_id;signature=null;selected=null;showing=false;ResetConfirmation(); }
            string next=string.Join("|",rows.Where(r=>r.status=="waiting").Select(r=>r.event_id));
            if(next!=signature)
            {
                signature=next;var waiting=rows.FirstOrDefault(r=>r.status=="waiting");
                if(waiting!=null) { selected=waiting.event_id;showing=true;GetComponent<PythonCatDetailsPanel>().Close(); }
            }
            if(hadRecords && rows.Length==0) { showing=false;selected=null; }
            var pending=Array.Find(rows,r=>r.event_id==confirming);
            if(confirming!=null && (pending==null || pending.status!="waiting" || Identity(pending)!=confirmationIdentity))ResetConfirmation();
            Render();
        }
        private void Render()
        {
            panel.SetActive(showing);if(!showing)return;
            events.options.Clear();foreach(var row in rows)events.options.Add(new Dropdown.OptionData(row.name+" → "+row.customer_name+"（"+(row.status=="waiting" ? "回答待ち" : row.choice=="accept" ? "譲渡成立" : "見送り")+"）"));
            if(rows.Length==0)events.options.Add(new Dropdown.OptionData("譲渡申し出はありません"));
            events.SetValueWithoutNotify(Math.Max(0,Array.FindIndex(rows,r=>r.event_id==selected)));events.RefreshShownValue();
            selected=events.value<rows.Length ? rows[events.value].event_id : null;RenderDetails();
        }
        private void RenderDetails()
        {
            bool enabled=current!=null && current.enabled;configure.GetComponentInChildren<Text>().text="申し出の発生："+(enabled ? "ON → OFFにする" : "OFF → ONにする");
            settings.text="営業準備中に変更できます。初期設定はOFFです。\nお客さんへの親しみが80以上、かつプレイヤー好感度より高いと申し出が発生します。\nOFFにしても過去の履歴は残ります。";
            var row=Array.Find(rows,r=>r.event_id==selected);
            details.text=row==null ? "申し出はありません。\n発生をONにすると、次の接客終了から条件を判定します。" : row.name+" → "+row.customer_name+" / "+row.day+"日目の申し出\nお客さんへの親しみ："+row.guest_affinity.ToString("0.#")+" / 発生基準："+row.threshold.ToString("0.#")+"\nプレイヤー好感度："+row.player_affinity.ToString("0.#")
                +"\n\n"+(row.status=="waiting" ? "譲渡すると、このゲームでは接客・派遣・交流に戻せません。\n見送るとカフェでの活動を続けます。" : row.choice=="accept" ? "譲渡が成立しました。猫の詳細と関係の記録は残っています。" : "申し出を見送りました。カフェでの活動を続けます。");
            notice.text=row!=null && row.status=="waiting" ? (!string.IsNullOrEmpty(row.accept_reason) ? row.accept_reason : !string.IsNullOrEmpty(row.decline_reason) ? row.decline_reason : "すべての申し出に回答するまで営業は停止します。") : current?.reason ?? "";
            if(string.IsNullOrEmpty(notice.text))notice.text="譲渡する場合は、次の画面で猫と相手を確認して確定します。";
            RefreshControls();
        }
        public void RefreshControls()
        {
            bool ready=GetComponent<PythonStateClient>().CanManageSaves;var row=Array.Find(rows,r=>r.event_id==selected);
            configure.interactable=ready && current!=null && current.can_configure && confirming==null;
            events.interactable=ready && rows.Length>0 && confirming==null;
            accept.interactable=ready && row!=null && row.can_accept && confirming==null;decline.interactable=ready && row!=null && row.can_decline && confirming==null;
            close.interactable=confirming==null && !rows.Any(r=>r.status=="waiting" && (r.can_accept || r.can_decline));
            var pending=Array.Find(rows,r=>r.event_id==confirming);confirmAccept.interactable=ready && pending!=null && pending.can_accept && Identity(pending)==confirmationIdentity;
            cancel.interactable=ready;
        }
        public void Disconnect()
        { signature=null;ResetConfirmation();configure.interactable=events.interactable=accept.interactable=decline.interactable=confirmAccept.interactable=false;close.interactable=true;if(panel.activeSelf)notice.text="Python未接続です。再接続後に申し出を確認してください。"; }
    }
}
