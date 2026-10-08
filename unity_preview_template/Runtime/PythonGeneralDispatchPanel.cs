using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{

    public class PythonGeneralDispatchPanel : MonoBehaviour
    {
        public GameObject panel;
        public Dropdown destinations, returns;
        public Text title, details, progress, notice;
        public Button send, receive, close;
        private PythonCafeState state;
        private PythonDispatchOption[] options=new PythonDispatchOption[0];
        private PythonDispatchEvent[] events=new PythonDispatchEvent[0];
        private string instanceId, lastWaiting;
        private bool showing, focusWaiting;
        public void Open(string id)
        {
            if(!GetComponent<PythonStateClient>().CanManageSaves)return;
            showing=true;GetComponent<PythonCatDetailsPanel>().Close();Render();
        }
        public void Close() { showing=false;panel.SetActive(false); }
        public void Select(int _) { RenderDetails(); }
        public void Send() { if(destinations.value<options.Length)GetComponent<PythonStateClient>().DispatchGeneral(options[destinations.value].cat_id,options[destinations.value].choice); }
        public void Receive() { if(returns.value<events.Length)GetComponent<PythonStateClient>().ReceiveGeneral(events[returns.value].choice); }
        public void Apply(PythonCafeState value)
        {
            state=value;
            if(instanceId!=state.instance_id) { instanceId=state.instance_id;showing=false;lastWaiting=null;focusWaiting=false;destinations.SetValueWithoutNotify(0);returns.SetValueWithoutNotify(0); }
            var waiting=(state.general_dispatch?.events ?? new PythonDispatchEvent[0]).FirstOrDefault(r=>r.needs_response || r.status=="waiting" || r.status=="missing");
            if(waiting!=null && waiting.choice!=lastWaiting)
            { lastWaiting=waiting.choice;showing=true;focusWaiting=true;GetComponent<PythonCatDetailsPanel>().Close(); }
            if(waiting==null)lastWaiting=null;
            Render();
        }
        static void Fill(Dropdown dropdown,string[] captions,int index)
        {
            dropdown.options.Clear();foreach(var caption in captions)dropdown.options.Add(new Dropdown.OptionData(caption));
            if(captions.Length==0)dropdown.options.Add(new Dropdown.OptionData("記録・選択肢なし"));
            dropdown.SetValueWithoutNotify(Math.Max(0,Math.Min(index,captions.Length-1)));dropdown.RefreshShownValue();
        }
        private void Render()
        {
            panel.SetActive(showing);if(!showing || state==null)return;
            options=state.general_dispatch?.options ?? new PythonDispatchOption[0];
            var old=events.Length>returns.value ? events[returns.value].choice : "";
            events=state.general_dispatch?.events ?? new PythonDispatchEvent[0];
            Fill(destinations,options.Select(r=>r.label).ToArray(),destinations.value);
            int pending=Array.FindIndex(events,r=>r.needs_response || r.status=="waiting" || r.status=="missing");
            Fill(returns,events.Select(r=>r.label+"（"+(r.status=="travelling" ? (r.needs_response ? "選択待ち" : "派遣中") : r.status=="waiting" ? "帰還待ち" : r.status=="missing" ? "行方不明" : r.status=="resolved" ? "受取済み" : "対応待ち")+"）").ToArray(),focusWaiting && pending>=0 ? pending : Array.Exists(events,r=>r.choice==old) ? Array.FindIndex(events,r=>r.choice==old) : Math.Max(0,pending));
            focusWaiting=false;title.text="一般派遣・帰還";RenderDetails();
        }
        private void RenderDetails()
        {
            if(state==null)return;
            var option=destinations.value<options.Length ? options[destinations.value] : null;
            details.text=option==null ? "派遣先はありません。" : option.days+"回の閉店で帰還 / 報酬 "+option.reward.ToString("0.#")+"\n"+option.detail;
            var ev=returns.value<events.Length ? events[returns.value] : null;
            progress.text=ev==null ? "派遣の記録はありません。" : (ev.status=="travelling" ? "あと "+ev.remaining+"回の閉店で帰還" : ev.status=="waiting" ? "帰還しました。報酬を受け取ってください。" : ev.status=="resolved" ? "帰還・報酬の受け取り済み" : "宿のトラブルへの対応待ち")+"\n報酬 "+ev.reward.ToString("0.#")+"\n"+ev.detail;

            notice.text=(option?.reason ?? "")+(ev!=null && !string.IsNullOrEmpty(ev.reason) ? "\n"+ev.reason : "");
            if(string.IsNullOrEmpty(notice.text))notice.text="条件と報酬は出発時に固定します。帰還後は出勤設定を確認してください。";
            RefreshControls();
        }
        public void RefreshControls()
        {
            bool ready=GetComponent<PythonStateClient>().CanManageSaves;
            destinations.interactable=ready && options.Length>0;returns.interactable=ready && events.Length>0;
            send.interactable=ready && destinations.value<options.Length && options[destinations.value].can_select;
            receive.interactable=ready && returns.value<events.Length && events[returns.value].can_select;
            close.interactable=true;
        }
        public void Disconnect()
        { send.interactable=receive.interactable=destinations.interactable=returns.interactable=false;if(panel.activeSelf)notice.text="Python未接続です。再接続後に派遣・帰還を確認できます。"; }
    }
}
