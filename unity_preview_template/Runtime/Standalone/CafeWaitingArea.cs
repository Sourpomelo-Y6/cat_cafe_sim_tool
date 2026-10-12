using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class WaitingAreaRules { public int queue_bonus,wait_bonus; public double cost,daily_cost; }
    [Serializable] public class CafeWaitingArea { public int purchased_day,upgraded_day; }
    public sealed partial class CafeSimulation
    {
        private WaitingAreaRules WaitingAreaAt(CafeSnapshot s,int day) => s.waiting_area.upgraded_day>0 && s.waiting_area.upgraded_day<=day?Rules.waiting_area_upgrade:s.waiting_area.purchased_day>0 && s.waiting_area.purchased_day<=day?Rules.waiting_area:null;
        public int QueueCapacity => Rules.queue_capacity+(WaitingAreaAt(State,State.day)?.queue_bonus??0);
        public int MaxWaitTicks => Rules.max_wait_ticks+(WaitingAreaAt(State,State.day)?.wait_bonus??0);
        public double WaitingAreaDailyCost(CafeSnapshot s,int day) => WaitingAreaAt(s,day)?.daily_cost??0;
        public double OperatingCostEstimate => Rules.base_cost+State.seat_count*Rules.per_seat_cost+HousingDailyCost(State,State.day)+WaitingAreaDailyCost(State,State.day)+(CurrentStoreEvent.choice=="repair"?Rules.store_events.trouble_cost:0);
        public string WaitingAreaReason(string kind)
        {
            string reason=ItemsReason();if(reason!="")return reason.Replace("用品","待合スペース");
            var r=State.waiting_area;
            if(kind=="purchase_waiting_area")
            {
                if(r.purchased_day>0)return "待合スペースは購入済みです。";
                if(ClearedGoalDay(1)==0 || ClearedGoalDay(1)>=State.day)return "待合スペースは人気第1段階達成翌日から解放されます。";
            }
            else if(kind=="upgrade_waiting_area")
            {
                if(r.purchased_day==0)return "先に待合スペースを購入してください。";
                if(r.upgraded_day>0)return "待合スペースは2段階目まで強化済みです。";
                if(ClearedGoalDay(2)==0 || ClearedGoalDay(2)>=State.day)return "待合スペースの2段階目は人気第2段階達成翌日から解放されます。";
            }
            else return "待合スペースの操作が不正です。";
            return State.funds<=WaitingAreaRule(kind).cost?"購入・強化後に資金が残る必要があります。":"";
        }
        public WaitingAreaRules WaitingAreaRule(string kind) => kind=="purchase_waiting_area"?Rules.waiting_area:kind=="upgrade_waiting_area"?Rules.waiting_area_upgrade:throw new ArgumentException("Invalid waiting area action");
        private void ChangeWaitingArea(string kind)
        {
            string reason=WaitingAreaReason(kind);if(reason!="")throw new InvalidOperationException(reason);
            double cost=WaitingAreaRule(kind).cost;
            if(kind=="purchase_waiting_area")State.waiting_area.purchased_day=State.day;else State.waiting_area.upgraded_day=State.day;
            State.funds-=cost;State.expenses+=cost;State.waiting_area_expenses+=cost;
        }
        private double WaitingAreaExpenses(CafeSnapshot s,int day) => (s.waiting_area.purchased_day==day?Rules.waiting_area.cost:0)+(s.waiting_area.upgraded_day==day?Rules.waiting_area_upgrade.cost:0);
        private void ValidateWaitingArea(CafeSnapshot s)
        {
            var r=s.waiting_area;var basic=Rules.waiting_area;var upgrade=Rules.waiting_area_upgrade;
            if(r==null || basic==null || upgrade==null || !Range(basic.cost,double.Epsilon,double.MaxValue) || !Range(upgrade.cost,double.Epsilon,double.MaxValue)
                || !Range(basic.daily_cost,double.Epsilon,double.MaxValue) || !Range(upgrade.daily_cost,double.Epsilon,double.MaxValue)
                || basic.queue_bonus<1 || basic.wait_bonus<1 || upgrade.queue_bonus<=basic.queue_bonus || upgrade.wait_bonus<=basic.wait_bonus
                || r.purchased_day<0 || r.purchased_day>s.day || r.upgraded_day<0 || r.upgraded_day>s.day
                || r.purchased_day>0 && (s.goal.Count<1 || s.goal[0].status!="cleared" || r.purchased_day<=s.goal[0].resolved_day)
                || r.upgraded_day>0 && (r.purchased_day==0 || r.upgraded_day<r.purchased_day || s.goal.Count<2 || s.goal[1].status!="cleared" || r.upgraded_day<=s.goal[1].resolved_day)
                || s.waiting_area_expenses!=WaitingAreaExpenses(s,s.day) || s.waiting_area_cost!=(s.phase=="closed"?WaitingAreaDailyCost(s,s.day):0)
                || s.expenses<s.waiting_area_expenses+s.waiting_area_cost || s.queue.Count>Rules.queue_capacity+(WaitingAreaAt(s,s.day)?.queue_bonus??0))throw new ArgumentException("Invalid waiting area state or finance");
            foreach(var h in s.history)if(h.waiting_area_expenses!=WaitingAreaExpenses(s,h.day) || h.waiting_area_cost!=WaitingAreaDailyCost(s,h.day) || h.expenses<h.waiting_area_expenses+h.waiting_area_cost)throw new ArgumentException("Waiting area finance differs from history");
        }
    }
}
