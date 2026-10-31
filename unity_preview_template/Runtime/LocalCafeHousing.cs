namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonHousingState HousingView()
        {
            var s = Simulation.State; int stage = Simulation.HousingStage(s,s.day);
            string kind = Simulation.NextHousingKind; var next = Simulation.NextHousingRules;
            double daily = Simulation.HousingDailyCost(s,s.day); int capacity = Simulation.HousingCapacity(s,s.day);
            return new PythonHousingState { configured = true, count = Simulation.ActiveCatCount(s), capacity = capacity, free = capacity-Simulation.ActiveCatCount(s), stage = stage, kind = kind,
                max_stage = Simulation.HousingMaxStage, max_capacity = Simulation.Rules.housing.initial_capacity+Simulation.Rules.housing.capacity_bonus+Simulation.Rules.housing_upgrade.capacity_bonus+System.Linq.Enumerable.Sum(Simulation.Rules.housing_extensions ?? new CatCafePreview.Standalone.HousingRules[0],e=>e.capacity_bonus),
                can_expand = next!=null && Simulation.HousingReason(kind) == "", reason = next==null ? "飼育スペースは最大まで拡張済みです。" : Simulation.HousingReason(kind), cost = (float)(next?.cost ?? 0),
                funds_after = (float)(s.funds-(next?.cost ?? 0)), daily_cost = (float)daily, next_daily_cost = (float)(next?.daily_cost ?? daily),
                next_capacity = capacity+(next?.capacity_bonus ?? 0),
                operating_cost = (float)(Simulation.OperatingCostEstimate), next_operating_cost = (float)(Simulation.OperatingCostEstimate-daily+(next?.daily_cost ?? daily)) };
        }
    }
}
