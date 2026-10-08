using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonPatronDispatch { public PythonDispatchOption[] options; public PythonDispatchEvent[] events; }
    [Serializable] public class PythonDispatchOption
    { public string cat_id, choice, label, detail, reason; public int days; public float reward, satisfaction; public bool can_select; }
    [Serializable] public class PythonDispatchEvent
    { public string cat_id, choice, label, status, reason, detail; public int remaining; public float reward, satisfaction; public bool can_select, needs_response; }
    public class PythonDispatchPanel : MonoBehaviour
    {
        public GameObject panel;
        public Dropdown destinations, returns;
        public Text title, details, progress, notice;
        public Button send, receive, close;
        private PythonCafeState state;
        private PythonDispatchOption[] options=new PythonDispatchOption[0];
        private PythonDispatchEvent[] events=new PythonDispatchEvent[0];
        private string catId, instanceId, lastWaiting;
        private bool showing;
        public void Open(string id)
        {
            if(!GetComponent<PythonStateClient>().CanManageSaves)return;
            catId=id;showing=true;GetComponent<PythonCatDetailsPanel>().Close();Render();
        }
        public void OpenOption(string catId,string destination){Open(catId);int index=Array.FindIndex(options,r=>r.cat_id==catId && r.choice==destination);if(index>=0){destinations.SetValueWithoutNotify(index);RenderDetails();}panel.transform.SetAsLastSibling();}
        public void Close() { showing=false;panel.SetActive(false); }
        public void Select(int _) { RenderDetails(); }
        public void Send() { if(destinations.value>=0 && destinations.value<options.Length)GetComponent<PythonStateClient>().DispatchPatron(catId,options[destinations.value].choice); }
        public void Receive() { if(returns.value>=0 && returns.value<events.Length)GetComponent<PythonStateClient>().ReceivePatron(events[returns.value].choice); }
        public void Apply(PythonCafeState value)
        {
            state=value;
            if(instanceId!=state.instance_id) { instanceId=state.instance_id;showing=false;catId=null;lastWaiting=null; }
            var waiting=(state.patron_dispatch?.events ?? new PythonDispatchEvent[0]).FirstOrDefault(r=>r.status=="waiting");
            if(waiting!=null && waiting.choice!=lastWaiting)
            { lastWaiting=waiting.choice;catId=waiting.cat_id;showing=true;GetComponent<PythonCatDetailsPanel>().Close(); }
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
            options=(state.patron_dispatch?.options ?? new PythonDispatchOption[0]).Where(r=>r.cat_id==catId).ToArray();
            var old=events.Length>returns.value ? events[returns.value].choice : "";
            events=state.patron_dispatch?.events ?? new PythonDispatchEvent[0];
            Fill(destinations,options.Select(r=>r.label).ToArray(),destinations.value);
            int pending=Array.FindIndex(events,r=>r.status=="waiting");
            Fill(returns,events.Select(r=>r.label+"（"+(r.status=="travelling" ? "派遣中" : r.status=="waiting" ? "帰還待ち" : "受取済み")+"）").ToArray(),pending>=0 ? pending : Math.Max(0,Array.FindIndex(events,r=>r.choice==old)));
            var cat=Array.Find(state.cats,r=>r.cat_id==catId);
            title.text=(cat==null ? "猫" : cat.name)+"の有力者訪問";RenderDetails();
        }
        private void RenderDetails()
        {
            if(state==null)return;
            var option=destinations.value>=0 && destinations.value<options.Length ? options[destinations.value] : null;
            details.text=option==null ? "有力者目標を選んだゲームで派遣できます。" : option.days+"回の閉店で帰還 / 報酬 "+option.reward.ToString("0.#")+"\n満足度加点 ＋"+option.satisfaction.ToString("0.#")+"（目標値が上限）\n"+option.detail;
            var ev=returns.value>=0 && returns.value<events.Length ? events[returns.value] : null;
            progress.text=ev==null ? "派遣の記録はありません。" : (ev.status=="travelling" ? "あと "+ev.remaining+"回の閉店で帰還" : ev.status=="waiting" ? "帰還しました。報酬を受け取ってください。" : "帰還・報酬の受け取り済み")+"\n報酬 "+ev.reward.ToString("0.#")+" / 出発時の満足度加点 ＋"+ev.satisfaction.ToString("0.#");
            progress.text+="\n"+(state.objective_progress?.details ?? "");
            notice.text=ev!=null && !string.IsNullOrEmpty(ev.reason) ? ev.reason : option?.reason ?? "";
            if(string.IsNullOrEmpty(notice.text))notice.text="条件と加点は出発時に固定します。帰還後は出勤設定を確認してください。";
            RefreshControls();
        }
        public void RefreshControls()
        {
            bool ready=GetComponent<PythonStateClient>().CanManageSaves;
            destinations.interactable=ready && options.Length>0;returns.interactable=ready && events.Length>0;
            send.interactable=ready && destinations.value>=0 && destinations.value<options.Length && options[destinations.value].can_select;
            receive.interactable=ready && returns.value>=0 && returns.value<events.Length && events[returns.value].can_select;
            close.interactable=!events.Any(r=>r.status=="waiting");
        }
        public void Disconnect()
        { send.interactable=receive.interactable=destinations.interactable=returns.interactable=false;if(panel.activeSelf)notice.text="Python未接続です。再接続後に派遣・帰還を確認できます。"; }
    }
}
