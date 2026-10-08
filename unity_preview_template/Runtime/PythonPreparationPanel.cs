using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonPreparationPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text details, notice, title, summary, selectedDetails, catsDetails;
        public Button open, close, work, rest, start, dayOff;
        public Dropdown cats;
        public ScrollRect goalScroll, catsScroll;
        private PythonCafeState current;
        private string identity, selectedId;
        private bool online;
        public void Apply(PythonCafeState state)
        {
            current=state;online=true;
            bool preparation=state.can_set_shifts && !state.closed && state.game_over==null;
            string key=state.instance_id+"/"+state.day;
            if(identity!=key)
            {
                identity=key;selectedId=null;
                panel.SetActive(preparation && state.next_goal==null && string.IsNullOrEmpty(state.required_action));
            }
            if(!preparation)panel.SetActive(false);
            open.gameObject.SetActive(preparation && state.next_goal==null);
            Render();RefreshControls();
        }
        public void Open()
        {
            if(current==null || !current.can_set_shifts || current.closed || current.game_over!=null)return;
            panel.SetActive(true);panel.transform.SetAsLastSibling();Render();
            Canvas.ForceUpdateCanvases();if(goalScroll!=null)goalScroll.verticalNormalizedPosition=1;if(catsScroll!=null)catsScroll.verticalNormalizedPosition=1;
        }
        public void Close() { panel.SetActive(false); }
        public void OpenDayOff() { if(dayOff!=null && dayOff.interactable)GetComponent<PythonDayOffPanel>()?.Open(); }
        public void Work() { SetShift(true); }
        public void Rest() { SetShift(false); }
        private void SetShift(bool working)
        {
            if((working ? work : rest)==null || !(working ? work : rest).interactable)return;
            GetComponent<PythonStateClient>().SetCatShift(selectedId,working);
        }
        public void StartBusiness()
        { if(start!=null && start.interactable)GetComponent<PythonStateClient>().ProgressBusiness(); }
        public void SelectCat(int index)
        {
            if(current==null || current.cats==null || index<0 || index>=current.cats.Length)return;
            selectedId=current.cats[index].cat_id;RenderSelected();RefreshControls();
        }
        private PythonCatState Selected { get { return current?.cats==null ? null : Array.Find(current.cats,cat=>cat.cat_id==selectedId); } }
        private void Render()
        {
            if(current==null)return;
            var rows=current.cats ?? new PythonCatState[0];
            if(title!=null)title.text=current.day+"日目・営業の準備";
            int working=Array.FindAll(rows,cat=>cat.working).Length;
            if(summary!=null)summary.text="出勤 "+working+"匹 / 休養・不在 "+(rows.Length-working)+"匹 / 席 "+(current.seats?.Length ?? 0)+"席 / 資金 "+current.funds.ToString("0.#")+"\n猫の体調と疲労を確認し、今日の出勤を選びましょう。";
            var option=Array.Find(current.new_game_options ?? new PythonNewGameOption[0],row=>row.choice==current.objective);
            var text=new StringBuilder("経営目標："+(current.objective_progress?.label ?? option?.label ?? "自由営業"));
            text.Append("\n"+(current.objective_progress?.details ?? option?.detail ?? "目標情報を取得していません。"));
            text.Append("\n\n営業開始後は「自動進行」で接客を進めます。もう一度押すと停止します。\n\n疲れた猫は休養にして休ませましょう。病気・派遣中・行方不明・譲渡済みの猫は出勤できません。\n\n席数より多くの猫を出勤させることもできます。");
            if(option!=null)text.Append("\n\n標準の開始条件\n"+option.conditions);
            details.text=text.ToString();
            if(cats!=null)
            {
                cats.ClearOptions();foreach(var cat in rows)cats.options.Add(new Dropdown.OptionData(cat.name+"（"+(cat.working ? "出勤" : "休養・不在")+"）"));
                int index=Array.FindIndex(rows,cat=>cat.cat_id==selectedId);index=Math.Max(0,index);
                selectedId=rows.Length>0 ? rows[index].cat_id : null;cats.SetValueWithoutNotify(index);cats.RefreshShownValue();
            }
            var roster=new StringBuilder();foreach(var cat in rows)roster.Append(cat.name+"："+(cat.working ? "出勤" : cat.activity=="cafe" && cat.health_status=="healthy" ? "休養" : cat.health_label+" / "+cat.activity_label)+"\n体力 "+cat.stamina.ToString("0.#")+"/"+cat.max_stamina.ToString("0.#")+" / 疲労 "+cat.fatigue.ToString("0.#")+"\n\n");
            if(catsDetails!=null)catsDetails.text=roster.ToString();
            RenderSelected();RenderNotice();
        }
        private void RenderSelected()
        {
            if(selectedDetails==null)return;var cat=Selected;
            if(cat==null){selectedDetails.text="選択できる猫がいません。";return;}
            string condition=cat.health_label+" / "+cat.activity_label;
            selectedDetails.text=cat.name+"："+(cat.working ? "出勤" : "休養・不在")+" / "+condition
                +"\n体力 "+cat.stamina.ToString("0.#")+"/"+cat.max_stamina.ToString("0.#")+" / 疲労 "+cat.fatigue.ToString("0.#")+" / ストレス "+cat.stress.ToString("0.#")
                +"\n"+(cat.health_status!="healthy" || cat.activity!="cafe" ? "体調・活動の状態により出勤できません。" : "「出勤にする」「休養にする」で切り替えます。");
        }
        private void RenderNotice()
        {
            if(!online){notice.text="Python未接続です。再接続後に出勤の変更と営業開始ができます。";return;}
            var client=GetComponent<PythonStateClient>();
            if(!client.CanLeaveResult){notice.text="操作を送信しています。完了するまでお待ちください。";return;}
            if(!string.IsNullOrEmpty(current.required_action)){notice.text="営業開始前に確認してください："+current.required_action;return;}
            if(!Array.Exists(current.cats ?? new PythonCatState[0],cat=>cat.working)){notice.text="出勤する猫がいません。猫を選んで「出勤にする」を押してください。";return;}
            notice.text="出勤する猫を確認したら「営業開始」を押してください。";
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();var cat=Selected;
            bool ready=online && client.CanChangeShifts && cat!=null && string.IsNullOrEmpty(current.required_action);
            if(work!=null)work.interactable=ready && !cat.working && cat.health_status=="healthy" && cat.activity=="cafe";
            if(rest!=null)rest.interactable=ready && cat.working;
            if(cats!=null)cats.interactable=online && client.CanManageSaves && current?.cats!=null && current.cats.Length>0;
            if(start!=null)start.interactable=online && current!=null && current.can_set_shifts && !current.closed && current.game_over==null && client.CanProgressBusiness;
            if(dayOff!=null)dayOff.interactable=online && client.CanManageSaves && current!=null && current.day_off!=null;
            open.interactable=current!=null;close.interactable=true;if(current!=null)RenderNotice();
        }
        public void Disconnect() { online=false;RefreshControls(); }
    }
}
