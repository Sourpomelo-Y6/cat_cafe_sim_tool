using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonExpansion ExpansionView()
        {
            var s=Simulation.State;var r=Simulation.Rules;bool next=s.seat_count<2+r.expansion_costs.Length;double cost=next?r.expansion_costs[s.seat_count-2]:0,daily=Simulation.HousingDailyCost(s,s.day);
            return new PythonExpansion {has_next=next,seats=s.seat_count,next_seats=next?s.seat_count+1:s.seat_count,max_seats=2+r.expansion_costs.Length,cost=(float)cost,funds_after=(float)(s.funds-cost),can_expand=Simulation.ExpansionReason()=="",reason=Simulation.ExpansionReason(),
                operating_cost=(float)(Simulation.OperatingCostEstimate),next_operating_cost=(float)(Simulation.OperatingCostEstimate+(next?r.per_seat_cost:0)),details="増設した当日から席・運営費を反映。4席以降の増設客は購入翌日から来店。"};
        }
        private PythonNextGoal NextGoalView()
        {
            var s=Simulation.State;if(s.goal.Count==0 || s.goal.Last().status!="cleared" || s.goal.Count>=Simulation.Rules.goal_stages.Length)return null;var r=Simulation.Rules.goal_stages[s.goal.Count];
            int start=s.day+(s.phase=="closed"?1:0);
            return new PythonNextGoal {started_day=start,deadline=start+r.days-1,stage=s.goal.Count+1,days=r.days,target=(float)r.target,can_advance=Simulation.GoalResponseReason("advance_goal")=="",reason=Simulation.GoalResponseReason("advance_goal")};
        }
        private PythonGoalResult GoalResultView()
        {
            var s=Simulation.State;
            if(Simulation.PatronWaiting || Simulation.BondWaiting)
            {
                bool patron=Simulation.PatronWaiting && !Simulation.BondWaiting;string mode=patron?"patron":"bond",reason=patron?Simulation.PatronContinueReason():Simulation.BondContinueReason();
                var record=ClearResultsView().rows.FirstOrDefault(row=>row.mode==mode);string summary=patron?PatronGoalView().details:BondGoalView().details;
                return new PythonGoalResult{pending=true,status="cleared",kind=patron?"continue_patron":"continue_bond_goal",title=patron?"有力者の満足度目標を達成しました":"好感度目標を達成しました",resolved_day=patron?s.patron.resolved_day:s.bond_goal.resolved_day,funds=(float)s.funds,can_continue=reason=="",reason=reason,summary=summary,details=summary+(record==null?"":"\n\nクリア時の成果\n"+record.details)+"\n\n結果を確認して継続営業できます。"};
            }
            if(!Simulation.GoalWaiting)return null;var g=s.goal.Last();var r=Simulation.Rules.goal_stages[s.goal.Count-1];
            return new PythonGoalResult {pending=Simulation.GoalWaiting,stage=s.goal.Count,started_day=g.started_day,deadline=g.started_day+r.days-1,resolved_day=g.resolved_day,status=g.status,target=(float)r.target,popularity=(float)s.popularity,funds=(float)s.funds,can_continue=Simulation.GoalResponseReason("continue_goal")=="",kind="continue_goal",title=EndingView()?.title??"人気目標の結果",summary="人気 "+Number(s.popularity)+" / "+Number(r.target),details="結果："+(g.status=="cleared"?"達成":"期限内未達")+" / 確定日："+g.resolved_day+"日目。"+(s.clear_results.Any(row=>row.mode=="popularity")?"\nクリア時の成果\n"+ClearResultsView().rows.Single(row=>row.mode=="popularity").details:"")};
        }
    }
}
