using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class CafeSeatPurchase { public int day,seats; public double cost; }
    [Serializable] public class GoalStageRule { public int days; public double target; }
    [Serializable] public class CafeGoalStage { public int started_day,resolved_day; public string status="active"; public bool continued; }
    public sealed partial class CafeSimulation
    {
        public bool GoalWaiting => State.game_over==null && State.goal.Count>0 && State.goal.Last().status!="active" && !State.goal.Last().continued;
        public int ClearedGoalDay(int stage) => State.goal.Count>=stage && State.goal[stage-1].status=="cleared" ? State.goal[stage-1].resolved_day : 0;
        private void SettleGoal()
        {
            if(State.goal.Count==0 || State.game_over!=null)return;var g=State.goal.Last();var r=Rules.goal_stages[State.goal.Count-1];if(g.status!="active" || State.day<g.started_day)return;
            g.status=State.popularity>=r.target?"cleared":State.day>=g.started_day+r.days-1?"expired":"active";if(g.status!="active"){g.resolved_day=State.day;CaptureClearResult();}
        }
        public string GoalResponseReason(string kind)
        {
            if(DispatchWaiting || DispatchChoiceWaiting)return "派遣の帰還結果を確認してください。";
            if(State.game_over!=null)return "経営終了後は目標を変更できません。";
            if(State.goal.Count==0)return "この営業は人気集計のみです。";
            if (ReservationWaiting) return "特別予約に回答してください。";
            if(CustomerTrustWaiting || State.cats.Any(c=>c.activity=="missing" && c.missing_remaining==0))return "帰還・信頼イベントを確認してください。";
            var g=State.goal.Last();
            if(kind=="continue_goal")return GoalWaiting?"":"確認待ちの目標結果はありません。";
            if(g.status!="cleared" || State.goal.Count>=Rules.goal_stages.Length)return "次に挑戦できる人気目標はありません。";
            if(RegularIntroductionWaiting || DispatchIntroductionWaiting || State.phase=="open")return "次の目標は他の結果を確認し、準備中または閉店後に選んでください。";
            return "";
        }
        private void RespondGoal(string kind)
        {
            string reason=GoalResponseReason(kind);if(reason!="")throw new InvalidOperationException(reason);
            if(kind=="continue_goal")State.goal.Last().continued=true;else State.goal.Add(new CafeGoalStage {started_day=State.day+(State.phase=="closed"?1:0)});State.revision++;
        }
        public string ExpansionReason()
        {
            string reason=ItemsReason();if(reason!="")return reason;
            if(State.seat_count>=2+Rules.expansion_costs.Length)return "現在追加できる席はありません。";
            int next=State.seat_count+1;if(next>=4 && next<=5 && ClearedGoalDay(1)==0)return "4・5席は人気目標の第1段階達成後に解放されます。";
            if(next>=6 && ClearedGoalDay(2)==0)return "6席以降は人気目標の第2段階達成後に解放されます。";
            return State.funds<=Rules.expansion_costs[State.seat_count-2]?"増設後に資金が残る必要があります。":"";
        }
        private void ExpandSeats()
        {
            string reason=ExpansionReason();if(reason!="")throw new InvalidOperationException(reason);double cost=Rules.expansion_costs[State.seat_count-2];State.seat_count++;
            State.expansion.Add(new CafeSeatPurchase {day=State.day,seats=State.seat_count,cost=cost});State.funds-=cost;State.expenses+=cost;State.expansion_expenses+=cost;
        }
        private void ValidateExpansion(CafeSnapshot s)
        {
            if(Rules.expansion_costs==null || s.expansion==null || s.seat_count<2 || s.seat_count>2+Rules.expansion_costs.Length || s.seat_count!=s.expansion.Count+2)throw new ArgumentException("Invalid seats");int previous=1;
            foreach(var p in s.expansion)
            {
                int index=s.expansion.IndexOf(p);int stage=p.seats<=5?1:2;var goal=s.goal!=null && s.goal.Count>=stage?s.goal[stage-1]:null;
                if(p==null || p.seats!=index+3 || p.cost!=Rules.expansion_costs[index] || p.day<previous || p.day>s.day || p.seats>=4 && (goal==null || goal.status!="cleared" || p.day<=goal.resolved_day))throw new ArgumentException("Invalid expansion history");previous=p.day;
            }
            if(s.expansion_expenses!=s.expansion.Where(p=>p.day==s.day).Sum(p=>p.cost))throw new ArgumentException("Invalid expansion expense");
            foreach(var h in s.history)if(h.seat_count!=2+s.expansion.Count(p=>p.day<=h.day) || h.expansion_expenses!=s.expansion.Where(p=>p.day==h.day).Sum(p=>p.cost))throw new ArgumentException("Expansion differs from daily history");
        }
        private void ValidateGoal(CafeSnapshot s)
        {
            if(Rules.goal_stages==null || s.goal==null || !new[]{"free","popularity"}.Contains(s.objective) || (s.objective=="free"?s.goal.Count!=0:s.goal.Count<1 || s.goal.Count>Rules.goal_stages.Length))throw new ArgumentException("Invalid goal mode");
            if (!Range(s.goal_popularity,0,Rules.popularity.cap) || s.phase!="closed" && s.goal_popularity!=0 || s.history.Any(h=>!Range(h.goal_popularity,0,Rules.popularity.cap))) throw new ArgumentException("Invalid closing popularity");
            for(int i=0;i<s.goal.Count;i++)
            {
                var g=s.goal[i];var rule=Rules.goal_stages[i];int end=s.phase=="closed"?s.day:s.day-1;string status="active";int resolved=0;
                if(g==null || g.started_day<1 || g.started_day>s.day+(s.phase=="closed"?1:0) || i==0 && g.started_day!=1 || i>0 && (s.goal[i-1].status!="cleared" || g.started_day<=s.goal[i-1].resolved_day || !s.goal[i-1].continued && g.started_day!=s.goal[i-1].resolved_day+1))throw new ArgumentException("Invalid goal start");
                for(int day=g.started_day;day<=end;day++)
                {
                    if(day==s.day && s.game_over!=null)break;
                    double popularity=day==s.day?s.goal_popularity:s.history[day-1].goal_popularity;
                    if(popularity>=rule.target || day>=g.started_day+rule.days-1){status=popularity>=rule.target?"cleared":"expired";resolved=day;break;}
                }
                if(g.status!=status || g.resolved_day!=resolved || status=="active" && g.continued || i<s.goal.Count-1 && status!="cleared")throw new ArgumentException("Goal differs from closing history");
            }
        }
    }
}
