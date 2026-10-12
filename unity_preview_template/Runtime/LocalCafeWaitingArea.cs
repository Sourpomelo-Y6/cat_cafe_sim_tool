using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonWaitingArea WaitingAreaView()
        {
            var s=Simulation.State;var r=s.waiting_area;int capacity=Simulation.QueueCapacity,wait=Simulation.MaxWaitTicks;double upkeep=Simulation.WaitingAreaDailyCost(s,s.day),operating=Simulation.OperatingCostEstimate;
            return new PythonWaitingArea {status=r.upgraded_day>0?"2段階目まで強化済み":r.purchased_day>0?"購入済み":"未購入",capacity=capacity,max_wait_ticks=wait,daily_cost=(float)upkeep,operating_cost=(float)operating,
                details="購入日："+r.purchased_day+" / 強化日："+r.upgraded_day+" / 本日の購入・強化費："+s.waiting_area_expenses+" / 本日の精算済み維持費："+s.waiting_area_cost,
                notes="購入・強化当日から待機上限と猶予へ適用します。維持費は当日から、休業日にも発生します。人気第1段階達成翌日から購入、第2段階達成翌日から強化。席数・飼育枠は増えません。各操作は1回限りです。",
                actions=new[]{"purchase_waiting_area","upgrade_waiting_area"}.Select(kind=>{var rule=Simulation.WaitingAreaRule(kind);bool completed=kind=="purchase_waiting_area"?r.purchased_day>0:r.upgraded_day>0;double dailyAfter=completed?upkeep:rule.daily_cost;string reason=Simulation.WaitingAreaReason(kind);return new PythonWaitingAreaAction {
                    kind=kind,label=kind=="purchase_waiting_area"?"待合スペースを購入（1段階目）":"待合スペースを強化（2段階目）",completed=completed,reason=reason,can_execute=reason=="",cost=(float)rule.cost,funds_after=(float)(completed?s.funds:s.funds-rule.cost),capacity_before=capacity,capacity_after=completed?capacity:Simulation.Rules.queue_capacity+rule.queue_bonus,wait_before=wait,wait_after=completed?wait:Simulation.Rules.max_wait_ticks+rule.wait_bonus,daily_before=(float)upkeep,daily_after=(float)dailyAfter,operating_before=(float)operating,operating_after=(float)(operating-upkeep+dailyAfter)};}).ToArray()};
        }
    }
}
