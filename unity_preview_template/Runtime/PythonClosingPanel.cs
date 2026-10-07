using System.Text;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonClosingPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text details, notice;
        public Button open, save, next, close;
        private PythonCafeState current;
        private string identity, message;
        private bool online, wasClosed;
        public void Apply(PythonCafeState state)
        {
            current=state;online=true;
            bool closed=state.closed && state.game_over==null;
            string key=state.instance_id+"/"+state.day;
            if(identity!=key || (closed && !wasClosed)){identity=key;message="";panel.SetActive(closed && string.IsNullOrEmpty(state.required_action));}
            wasClosed=closed;
            if(!closed)panel.SetActive(false);
            open.gameObject.SetActive(closed);
            Render();RefreshControls();
        }
        public void Open() { if(current==null || !current.closed || current.game_over!=null)return;panel.SetActive(true);panel.transform.SetAsLastSibling();Render();RefreshControls(); }
        public void Close() { panel.SetActive(false); }
        public void Save() { GetComponent<PythonStateClient>().SaveGame(); }
        public void Next() { if(current!=null && current.closed && next.interactable)GetComponent<PythonStateClient>().ProgressBusiness(); }
        public void Notify(string value) { message=value;RefreshControls(); }
        private void Render()
        {
            if(current==null)return;
            var text=new StringBuilder(current.day+"日目の振り返り\n\n売上："+current.revenue.ToString("0.#"));
            var finance=current.finance;
            if(finance!=null)text.Append("\n総収入："+finance.total_income.ToString("0.#")+" / 支出："+finance.total_expenses.ToString("0.#")+" / 収支："+finance.net_cash_flow.ToString("+0.#;-0.#;0")+"\n開始資金："+finance.opening_funds.ToString("0.#")+" → 終了資金："+finance.closing_funds.ToString("0.#"));
            else text.Append("\n収支情報を取得していません。");
            text.Append("\n\n目標の進捗\n"+(current.objective_progress?.details ?? "目標情報を取得していません。"));
            text.Append("\n\n猫たちの状態\n翌日の準備で疲れた猫を「休養」にして、出勤する猫を選び直しましょう。\n\n");
            foreach(var cat in current.cats)
                text.Append(cat.name+" / "+cat.health_label+" / "+cat.activity_label+"\n体力："+cat.stamina.ToString("0.#")+"/"+cat.max_stamina.ToString("0.#")+" / 疲労："+cat.fatigue.ToString("0.#")+" / ストレス："+cat.stress.ToString("0.#")+"\n\n");
            details.text=text.ToString();
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();
            save.interactable=client.CanManageSaves && current!=null && current.closed && current.game_over==null;
            next.interactable=client.CanProgressBusiness && current!=null && current.closed && current.game_over==null;
            close.interactable=true;open.interactable=current!=null;
            if(!online)notice.text="Python未接続です。再接続後に保存・翌日への移動ができます。";
            else if(!client.CanLeaveResult)notice.text="操作を送信しています。完了するまでお待ちください。";
            else if(current!=null && !string.IsNullOrEmpty(current.required_action))notice.text="翌日へ進む前に確認してください："+current.required_action;
            else notice.text=(string.IsNullOrEmpty(message) ? "" : message+"\n")+"進捗を残す場合は「保存」してから「翌日の準備へ」を押してください。";
        }
        public void Disconnect() { online=false;RefreshControls(); }
    }
}
