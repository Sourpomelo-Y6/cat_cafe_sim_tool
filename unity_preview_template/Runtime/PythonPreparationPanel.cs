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
        public Button open, close, work, rest, start, dayOff, propose, confirmProposal, cancelProposal;
        private System.Collections.Generic.HashSet<string> draft;
        private int draftRevision;
        public bool HasProposal { get { return draft!=null; } }
        public PythonCafeState CurrentState=>current;
        public string SelectedCatId=>selectedId;
        public PythonCatState[] Rows=>VisibleCats;
        public bool Working(PythonCatState cat) { return draft==null ? cat.working : draft.Contains(cat.cat_id); }
        public void ProposeRest()
        {
            if(propose==null || !propose.interactable)return;
            draft=new System.Collections.Generic.HashSet<string>(current.rest_proposal ?? new string[0]);draftRevision=current.revision;Render();RefreshControls();
        }
        public void CancelProposal() { draft=null;Render();RefreshControls(); }
        public void ConfirmProposal()
        {
            if(confirmProposal==null || !confirmProposal.interactable)return;
            var ids=new string[draft.Count];draft.CopyTo(ids);Array.Sort(ids,StringComparer.Ordinal);draft=null;GetComponent<PythonStateClient>().SetShifts(ids);Render();RefreshControls();
        }
        public Dropdown cats;
        public ScrollRect goalScroll, catsScroll;
        private PythonCafeState current;
        private string identity, selectedId;
        private bool online,wasAutomatic;
        public void Apply(PythonCafeState state)
        {
            if(draft!=null && (current==null || current.instance_id!=state.instance_id || current.day!=state.day || draftRevision!=state.revision))draft=null;
            current=state;online=true;
            bool preparation=state.can_set_shifts && !state.closed && state.game_over==null;
            string key=state.instance_id+"/"+state.day;
            bool automatic=state.autoplay?.running==true;
            if(identity!=key || (wasAutomatic && !automatic))
            {
                identity=key;selectedId=null;
                panel.SetActive(!automatic && preparation && state.next_goal==null && string.IsNullOrEmpty(state.required_action));
            }
            wasAutomatic=automatic;
            if(!preparation || automatic)panel.SetActive(false);
            open.gameObject.SetActive(preparation && state.next_goal==null);
            Render();RefreshControls();
        }
        public void Open()
        {
            if(current==null || !current.can_set_shifts || current.closed || current.game_over!=null || GetComponent<PythonStateClient>().AutoPlayRunning)return;
            panel.SetActive(true);panel.transform.SetAsLastSibling();Render();
            Canvas.ForceUpdateCanvases();if(goalScroll!=null)goalScroll.verticalNormalizedPosition=1;if(catsScroll!=null)catsScroll.verticalNormalizedPosition=1;
        }
        public void Close() { draft=null;panel.SetActive(false);Render();RefreshControls(); }
        public void OpenDayOff() { if(dayOff!=null && dayOff.interactable)GetComponent<PythonDayOffPanel>()?.Open(); }
        public bool CanShift(string id,bool working){var cat=current?.cats==null?null:Array.Find(current.cats,c=>c.cat_id==id);return online && cat!=null && !GetComponent<PythonStateClient>().AutoPlayRunning && GetComponent<PythonStateClient>().CanChangeShifts && string.IsNullOrEmpty(current.required_action) && (working?!Working(cat) && cat.health_status=="healthy" && cat.activity=="cafe":Working(cat));}
        public void SelectCatId(string id){int index=Array.FindIndex(VisibleCats,c=>c.cat_id==id);if(index>=0)SelectCat(index);}
        public void ShiftCat(string id,bool working){if(!CanShift(id,working))return;SelectCatId(id);SetShift(working);}
        public void Work() { SetShift(true); }
        public void Rest() { SetShift(false); }
        private void SetShift(bool working)
        {
            if((working ? work : rest)==null || !(working ? work : rest).interactable)return;
            if(draft!=null){if(working)draft.Add(selectedId);else draft.Remove(selectedId);Render();RefreshControls();}
            else GetComponent<PythonStateClient>().SetCatShift(selectedId,working);
        }
        public void StartBusiness()
        { if(start!=null && start.interactable)GetComponent<PythonStateClient>().ProgressBusiness(); }
        private PythonCatState[] VisibleCats=>GetComponent<PythonCatVisibility>()?.Filter(current?.cats) ?? current?.cats ?? new PythonCatState[0];
        public void RefreshVisibility(){Render();RefreshControls();}
        public void SelectCat(int index)
        {
            if(current==null || index<0 || index>=VisibleCats.Length)return;
            selectedId=VisibleCats[index].cat_id;RenderSelected();RefreshControls();
        }
        private PythonCatState Selected { get { return current?.cats==null ? null : Array.Find(current.cats,cat=>cat.cat_id==selectedId); } }
        private void Render()
        {
            if(current==null)return;
            var rows=current.cats ?? new PythonCatState[0];
            if(title!=null)title.text=current.day+"日目・営業の準備";
            int working=Array.FindAll(rows,cat=>Working(cat)).Length;
            if(summary!=null)summary.text="出勤 "+working+"匹 / 休養・不在 "+(rows.Length-working)+"匹 / 席 "+(current.seats?.Length ?? 0)+"席 / 資金 "+current.funds.ToString("0.#")+"\n"+(string.IsNullOrEmpty(current.store_event_notice)?"猫の体調と疲労を確認し、今日の出勤を選びましょう。":"本日の通知："+current.store_event_label+"（「目標・営業の案内」で確認）");
            var option=Array.Find(current.new_game_options ?? new PythonNewGameOption[0],row=>row.choice==current.objective);
            var text=new StringBuilder("経営目標："+(current.objective_progress?.label ?? option?.label ?? "自由営業"));
            text.Append("\n"+(current.objective_progress?.details ?? option?.detail ?? "目標情報を取得していません。"));
            text.Append("\n\n営業開始後は「自動進行」で接客を進めます。もう一度押すと停止します。\n\n「休養案を作成」は健康な在店猫を疲労順（同値はID順）に最大2匹休養、残りを出勤にします。案は手動変更・確定・取消できます。疲れた猫は休養にして休ませましょう。病気・派遣中・行方不明・譲渡済みの猫は出勤できません。\n\n席数より多くの猫を出勤させることもできます。");
            if(option!=null)text.Append("\n\n標準の開始条件\n"+option.conditions);
            if(!string.IsNullOrEmpty(current.store_event_notice))text.Insert(0,current.store_event_notice+"\n\n再確認：イベント一覧の「店舗イベント」。支援物資の所持品は「用品」へ。\n\n");
            details.text=text.ToString();
            var visible=VisibleCats;
            if(cats!=null)
            {
                cats.ClearOptions();foreach(var cat in visible)cats.options.Add(new Dropdown.OptionData(cat.name+"（"+(Working(cat) ? "出勤" : "休養・不在")+"）"));
                int index=Array.FindIndex(visible,cat=>cat.cat_id==selectedId);index=Math.Max(0,index);
                selectedId=visible.Length>0 ? visible[index].cat_id : null;cats.SetValueWithoutNotify(index);cats.RefreshShownValue();
            }
            var roster=new StringBuilder();foreach(var cat in visible)roster.Append(cat.name+"："+(Working(cat) ? "出勤" : cat.activity=="cafe" && cat.health_status=="healthy" ? "休養" : cat.health_label+" / "+cat.activity_label)+"\n体力 "+cat.stamina.ToString("0.#")+"/"+cat.max_stamina.ToString("0.#")+" / 疲労 "+cat.fatigue.ToString("0.#")+"\n\n");
            if(catsDetails!=null)catsDetails.text=roster.ToString();
            RenderSelected();RenderNotice();
        }
        private void RenderSelected()
        {
            if(selectedDetails==null)return;var cat=Selected;
            if(cat==null){selectedDetails.text="選択できる猫がいません。";return;}
            string condition=PythonCatCard.Health(cat)+" / "+PythonCatCard.Activity(cat,false);
            selectedDetails.text=cat.name+"："+(Working(cat) ? "出勤" : "休養・不在")+" / "+condition
                +"\n体力 "+cat.stamina.ToString("0.#")+"/"+cat.max_stamina.ToString("0.#")+" / 疲労 "+cat.fatigue.ToString("0.#")+" / ストレス "+cat.stress.ToString("0.#")
                +"\n"+cat.shift_forecast
                +"\n"+(cat.health_status!="healthy" || cat.activity!="cafe" ? "体調・活動の状態により出勤できません。" : "一覧の「出勤」「休養」で切り替えます。");
        }
        private void RenderNotice()
        {
            if(!online){notice.text="Python未接続です。再接続後に出勤の変更と営業開始ができます。";return;}
            var client=GetComponent<PythonStateClient>();
            string offReason=client.DayOffBlockReason;
            if(!string.IsNullOrEmpty(offReason)){notice.text="休業できない理由："+offReason;return;}
            if(client.HasPendingResults){notice.text="接客結果の保存を再試行してから、営業準備を進めてください。";return;}
            if(client.AutoPlayRunning){notice.text="おまかせ実行中です。停止してから出勤を変更できます。";return;}
            if(current.player_interaction?.active==true){notice.text="猫との交流を終了してから、営業準備を進めてください。";return;}
            if(!client.CanLeaveResult){notice.text="操作を送信しています。完了するまでお待ちください。";return;}
            if(!string.IsNullOrEmpty(current.required_action)){notice.text="営業開始前に確認してください："+current.required_action;return;}
            if(draft!=null){notice.text="休養案（未確定）：一覧で変更し「休養案を確定」を押してください。閉じる・取消で破棄します。全員休養の場合は確定後に休業できます。";return;}
            if(!Array.Exists(current.cats ?? new PythonCatState[0],cat=>cat.working)){notice.text="出勤する猫がいません。一覧の「出勤」を押してください。";return;}
            notice.text="出勤する猫を確認したら「営業開始」を押してください。";if(CafeBackends.Standalone && (CafeBackends.Local as LocalCafeBackend).TrustConversationReason!="")notice.text+="\n"+(CafeBackends.Local as LocalCafeBackend).TrustConversationReason;
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();var cat=Selected;
            bool ready=online && !client.AutoPlayRunning && client.CanChangeShifts && cat!=null && string.IsNullOrEmpty(current.required_action);
            if(work!=null)work.interactable=ready && !Working(cat) && cat.health_status=="healthy" && cat.activity=="cafe";
            if(rest!=null)rest.interactable=ready && Working(cat);
            if(cats!=null)cats.interactable=online && client.CanManageSaves && VisibleCats.Length>0;
            if(start!=null)start.interactable=online && current!=null && current.can_set_shifts && !current.closed && current.game_over==null && client.CanProgressBusiness && draft==null;
            if(dayOff!=null)dayOff.interactable=draft==null && online && client.CanManageSaves && current!=null;
            bool proposalReady=online && !client.AutoPlayRunning && client.CanChangeShifts && current!=null && string.IsNullOrEmpty(current.required_action);
            if(propose!=null)propose.interactable=proposalReady && current.rest_proposal!=null;
            if(confirmProposal!=null)confirmProposal.interactable=proposalReady && draft!=null;
            if(cancelProposal!=null)cancelProposal.interactable=draft!=null;
            open.interactable=current!=null && !client.AutoPlayRunning;close.interactable=true;if(current!=null)RenderNotice();GetComponent<PythonPreparationRoster>()?.Refresh();
        }
        public void Disconnect() { online=false;draft=null;Render();RefreshControls(); }
    }
}
