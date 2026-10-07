using System.Text;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonClosingPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text details, notice, title, financeDetails, goalDetails;
        public ScrollRect catsScroll, goalScroll;
        public Button open, save, next, close;
        private PythonCafeState current;
        private string identity, message;
        private bool online, wasClosed;
        public void Apply(PythonCafeState state)
        {
            current=state;online=true;
            bool closed=state.closed && state.game_over==null;
            string key=state.instance_id+"/"+state.day;
            if(identity!=key || (closed && !wasClosed)){identity=key;message="";ResetScroll();panel.SetActive(closed && string.IsNullOrEmpty(state.required_action));}
            wasClosed=closed;
            if(!closed)panel.SetActive(false);
            open.gameObject.SetActive(closed);
            Render();RefreshControls();
        }
        public void Open() { if(current==null || !current.closed || current.game_over!=null)return;panel.SetActive(true);panel.transform.SetAsLastSibling();Render();ResetScroll();RefreshControls(); }
        public void Close() { panel.SetActive(false); }
        public void Save() { GetComponent<PythonStateClient>().SaveGame(); }
        public void Next() { if(current!=null && current.closed && next.interactable)GetComponent<PythonStateClient>().ProgressBusiness(); }
        public void Notify(string value) { message=value;RefreshControls(); }
        private void Render()
        {
            if(current==null)return;
            if(title!=null)title.text=current.day+"日目・営業の振り返り";
            var finance=current.finance;
            string financial="本日の売上："+current.revenue.ToString("0.#");
            if(finance!=null)financial+="\n総収入："+finance.total_income.ToString("0.#")+"　支出："+finance.total_expenses.ToString("0.#")+"　収支："+finance.net_cash_flow.ToString("+0.#;-0.#;0")
                +"\n資金："+finance.opening_funds.ToString("0.#")+" → "+finance.closing_funds.ToString("0.#")+"\n総収入には売上以外の収入も含みます。";
            else financial+="\n収支情報を取得していません。";
            string goal=(current.objective_progress?.label ?? "経営目標")+"\n\n"+(current.objective_progress?.details ?? "目標情報を取得していません。");
            var cats=new StringBuilder("翌日の準備で、疲れた猫の休養と出勤する猫を確認しましょう。\n\n");
            foreach(var cat in current.cats ?? new PythonCatState[0])
                cats.Append(cat.name+" / "+cat.health_label+" / "+cat.activity_label+"\n体力："+cat.stamina.ToString("0.#")+"/"+cat.max_stamina.ToString("0.#")+" / 疲労："+cat.fatigue.ToString("0.#")+" / ストレス："+cat.stress.ToString("0.#")+"\n\n");
            if(financeDetails!=null && goalDetails!=null){financeDetails.text=financial;goalDetails.text=goal;details.text=cats.ToString();}
            else details.text=financial+"\n\n目標の進捗\n"+goal+"\n\n猫たちの状態\n"+cats;

        }
        private void ResetScroll()
        {
            Canvas.ForceUpdateCanvases();
            if(catsScroll!=null)catsScroll.verticalNormalizedPosition=1;
            if(goalScroll!=null)goalScroll.verticalNormalizedPosition=1;
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
            else notice.text=(string.IsNullOrEmpty(message) ? "" : message+"\n")+"保存は任意です。「翌日の準備へ」で出勤・休養を選び直せます。";
        }
        public void Disconnect() { online=false;RefreshControls(); }
    }
}
