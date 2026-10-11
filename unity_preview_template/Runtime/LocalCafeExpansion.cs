using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonExpansion ExpansionView()
        {
            var s=Simulation.State;var r=Simulation.Rules;bool next=s.seat_count<2+r.expansion_costs.Length;double cost=next?r.expansion_costs[s.seat_count-2]:0,daily=Simulation.HousingDailyCost(s,s.day);
            return new PythonExpansion {has_next=next,seats=s.seat_count,next_seats=next?s.seat_count+1:s.seat_count,max_seats=2+r.expansion_costs.Length,cost=(float)cost,funds_after=(float)(s.funds-cost),can_expand=Simulation.ExpansionReason()=="",reason=Simulation.ExpansionReason(),
                operating_cost=(float)(r.base_cost+r.per_seat_cost*s.seat_count+daily),next_operating_cost=(float)(r.base_cost+r.per_seat_cost*(s.seat_count+(next?1:0))+daily),details="増設した当日から席・運営費を反映。4席以降の増設客は購入翌日から来店。"};
        }
        private PythonNextGoal NextGoalView()
        {
            var s=Simulation.State;if(s.goal.Count==0 || s.goal.Count>=Simulation.Rules.goal_stages.Length)return null;var r=Simulation.Rules.goal_stages[s.goal.Count];
            return new PythonNextGoal {stage=s.goal.Count+1,days=r.days,target=(float)r.target,can_advance=Simulation.GoalResponseReason("advance_goal")=="",reason=Simulation.GoalResponseReason("advance_goal")};
        }
        private PythonGoalResult GoalResultView()
        {
            var s=Simulation.State;if(s.goal.Count==0)return null;var g=s.goal.Last();var r=Simulation.Rules.goal_stages[s.goal.Count-1];
            return new PythonGoalResult {pending=Simulation.GoalWaiting,stage=s.goal.Count,started_day=g.started_day,deadline=g.started_day+r.days-1,resolved_day=g.resolved_day,status=g.status,target=(float)r.target,popularity=(float)s.popularity,funds=(float)s.funds,can_continue=Simulation.GoalResponseReason("continue_goal")=="",details="人気目標の達成で席数と一般客を段階解放。高度な接客・VIP等は未移植。"};
        }
    }
}
