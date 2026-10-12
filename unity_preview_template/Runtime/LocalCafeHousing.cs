namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonHousingState HousingView()
        {
            var s = Simulation.State; var r = Simulation.Rules; int stage = s.housing.upgraded_day > 0 ? 2 : s.housing.purchased_day > 0 ? 1 : 0;
            string kind = stage == 0 ? "purchase_housing" : "upgrade_housing"; var next = stage == 0 ? r.housing : r.housing_upgrade;
            double daily = Simulation.HousingDailyCost(s,s.day); int capacity = Simulation.HousingCapacity(s,s.day);
            return new PythonHousingState { configured = true, count = Simulation.ActiveCatCount(s), capacity = capacity, free = capacity-Simulation.ActiveCatCount(s), stage = stage, kind = kind,
                can_expand = Simulation.HousingReason(kind) == "", reason = Simulation.HousingReason(kind), cost = (float)next.cost,
                funds_after = (float)(s.funds-next.cost), daily_cost = (float)daily, next_daily_cost = (float)next.daily_cost,
                next_capacity = stage == 2 ? capacity : capacity+next.capacity_bonus,
                operating_cost = (float)(r.base_cost+s.seat_count*r.per_seat_cost+daily), next_operating_cost = (float)(r.base_cost+s.seat_count*r.per_seat_cost+next.daily_cost) };
        }
    }
}
