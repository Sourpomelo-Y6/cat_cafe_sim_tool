using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonGoalPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title, details, notice,summary,nextSummary;
        private string instance;private bool online;private PythonCafeState current;
        public Button resume, advance, openNext, close;
        private bool showingNext;
        public void Continue() { if(CafeBackends.Standalone && (CafeBackends.Local as LocalCafeBackend).ReservationResultPending){GetComponent<PythonReservationPanel>().Dialogue.OpenPending();return;} if(CafeBackends.Standalone && (CafeBackends.Local as LocalCafeBackend).TrustResultPending){GetComponent<PythonCustomerTrustPanel>().Dialogue.OpenPending();return;} if(CafeBackends.Standalone && current?.goal_result?.kind=="continue_goal" && (CafeBackends.Local as LocalCafeBackend)?.OutcomePending==true){OutcomeConversationPanel.Ensure(gameObject).Open();return;} if(CafeBackends.Standalone && ((current?.goal_result?.kind=="continue_goal" && (CafeBackends.Local as LocalCafeBackend)?.PopularityEndingPending==true) || (current?.goal_result?.kind=="continue_patron" && (CafeBackends.Local as LocalCafeBackend)?.PatronEndingPending==true) || (current?.goal_result?.kind=="continue_bond_goal" && (CafeBackends.Local as LocalCafeBackend)?.BondEndingPending==true)))GetComponent<PopularityEndingPanel>()?.Open();else GetComponent<PythonStateClient>().ContinueGoal(); }
        public void Advance() { GetComponent<PythonStateClient>().AdvanceGoal(); }
        public void OpenNext() { if(!GetComponent<PythonStateClient>().CanManageSaves)return;showingNext=true; Apply(GetComponent<PythonStateClient>().CurrentState); }
        public void Close() { showingNext=false; Apply(GetComponent<PythonStateClient>().CurrentState); }
        public void Apply(PythonCafeState state)
        {
            if(state==null)return;if(instance!=state.instance_id){instance=state.instance_id;showingNext=false;}current=state;online=true;
            if(Application.isPlaying && CafeBackends.Standalone && ((CafeBackends.Local as LocalCafeBackend).EquipmentBlocksProgress || (CafeBackends.Local as LocalCafeBackend).ReservationBlocksProgress)){panel.SetActive(false);GetComponent<PopularityEndingPanel>()?.Apply(state,this);return;}
            var result=state.goal_result;
            var next=state.next_goal;
            if(next==null || result!=null)showingNext=false;
            if(openNext!=null)openNext.gameObject.SetActive(next!=null && result==null);
            panel.SetActive(result != null || showingNext);
            if(advance!=null)advance.gameObject.SetActive(next!=null && (result==null || string.IsNullOrEmpty(result.kind) || result.kind=="continue_goal"));
            if(close!=null)close.gameObject.SetActive(result==null);
            resume.gameObject.SetActive(result!=null);
            if(!panel.activeSelf) { RefreshControls(); if(Application.isPlaying && CafeBackends.Standalone)GetComponent<PopularityEndingPanel>()?.Apply(state,this); return; }
            string previousDetails=details.text;
            if(result==null)
            {
                title.text="次の人気目標へ";
                details.text=NextDetails(next)+"\n\n猫や資金、人気を引き継いで挑戦します。";
                notice.text=next.can_advance ? "内容を確認して「次の人気目標へ」を選んでください。" : next.reason;
            }
            else
            {
                title.text=!string.IsNullOrEmpty(result.title) ? result.title : result.status=="cleared" ? "人気目標を達成しました" : "人気目標の期限を迎えました";
                details.text=!string.IsNullOrEmpty(result.details) ? result.details : "第" + result.stage + "段階の結果\n\n目標人気：" + result.target + "\n現在の人気：" + result.popularity
                    + "\n結果：" + (result.status=="cleared" ? "達成" : "期限内未達")
                    + "\n挑戦期間：" + result.started_day + "〜" + result.deadline + "日目\n結果確定：" + result.resolved_day + "日目"
                    + "\n所持金：" + result.funds + "\n\n「継続営業」で結果を確認し、自由営業を続けます。"
                    + "\n猫や資金、人気を引き継いで遊べます。";
                notice.text=result.can_continue ? "内容を確認して「継続営業」を選んでください。" : result.reason;
                if(next!=null && (string.IsNullOrEmpty(result.kind) || result.kind=="continue_goal"))
                {
                    details.text = details.text+"\n\n"+NextDetails(next);
                    notice.text=next.can_advance ? "次の目標に挑戦するか、継続営業を選んでください。" : next.reason;
                }
            }
            if(previousDetails!=details.text)
            {
                var scroll=details.GetComponentInParent<ScrollRect>();
                if(scroll!=null) { scroll.content.anchoredPosition=Vector2.zero; scroll.verticalNormalizedPosition=1; }
            }
            if(summary!=null)summary.text=result==null ? "次の段階への挑戦を確認します。" : "結果："+(result.status=="cleared" ? "達成" : "期限内未達")+" / 確定："+result.resolved_day+"日目\n"+(result.summary ?? (string.IsNullOrEmpty(result.kind) || result.kind=="continue_goal" ? "人気 "+result.popularity+" / 目標 "+result.target : "詳しい達成値は結果欄に表示します。"));
            if(nextSummary!=null)nextSummary.text=next!=null && (result==null || string.IsNullOrEmpty(result.kind) || result.kind=="continue_goal") ? NextDetails(next)+"\n\n現在の猫・資金・人気を引き継ぎます。\n"+(next.can_advance ? "次の段階に進めます。" : next.reason) : "継続営業で結果を確認します。\n\n猫や資金を引き継いで遊べます。";
            RefreshControls();
            if(Application.isPlaying && CafeBackends.Standalone)OutcomeConversationPanel.Ensure(gameObject).ApplyOutcome(state);
            if(Application.isPlaying && CafeBackends.Standalone)(GetComponent<PopularityEndingPanel>()??gameObject.AddComponent<PopularityEndingPanel>()).Apply(state,this);
        }
        private static string NextDetails(PythonNextGoal next)
        { return "次の目標：第"+next.stage+"段階\n目標人気："+next.target+"\n挑戦期間："+next.days+"日\n開始："+next.started_day+"日目 / 期限："+next.deadline+"日目の閉店まで"; }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();
            resume.interactable=online && client.CanContinueGoal;
            bool ending=CafeBackends.Standalone && ((current?.goal_result?.kind=="continue_goal" && (CafeBackends.Local as LocalCafeBackend)?.OutcomePending==true) || (current?.goal_result?.kind=="continue_goal" && (CafeBackends.Local as LocalCafeBackend)?.PopularityEndingPending==true) || (current?.goal_result?.kind=="continue_patron" && (CafeBackends.Local as LocalCafeBackend)?.PatronEndingPending==true) || (current?.goal_result?.kind=="continue_bond_goal" && (CafeBackends.Local as LocalCafeBackend)?.BondEndingPending==true));if(ending)resume.interactable=online && client.CanManageSaves;resume.GetComponentInChildren<Text>().text=ending?"エンディングを再開":"結果を確認して継続営業";
            if(advance!=null)advance.GetComponentInChildren<Text>().text=current?.next_goal!=null ? "第"+current.next_goal.stage+"段階へ挑戦" : "次の人気目標へ";
            if(panel.activeSelf && notice!=null && !client.CanManageSaves)notice.text=!online ? "Python未接続です。再接続後に結果を確認できます。" : client.HasPendingResults ? "未保存の接客結果を先に確定してください。" : "操作の完了をお待ちください。";
            if(advance!=null)advance.interactable=online && client.CanAdvanceGoal;
            if(openNext!=null)openNext.interactable=online && client.CanManageSaves;
        }
        public void Disconnect()
        {
            online=false;
            resume.interactable=false;
            if(advance!=null)advance.interactable=false;
            if(openNext!=null)openNext.interactable=false;
            if(panel.activeSelf)notice.text="Python未接続です。再接続後に結果を確認できます。";
        }
    }
}
