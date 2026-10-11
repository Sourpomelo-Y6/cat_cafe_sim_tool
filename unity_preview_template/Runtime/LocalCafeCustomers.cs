using System.Linq;

namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonCustomerProgress[] CustomerProgress()
        {
            var s = Simulation.State;
            return Enumerable.Range(1, Simulation.Rules.arrival_ticks.Length).Select(i => "guest-" + i).Concat(s.loyalty.Select(r => r.customer_id)).Concat(s.discontent.Select(r => r.customer_id)).Distinct().OrderBy(id => id, System.StringComparer.Ordinal).Select(id =>
            {
                var loyal = s.loyalty.Find(r => r.customer_id == id); var unhappy = s.discontent.Find(r => r.customer_id == id);
                var active = unhappy?.suspensions.LastOrDefault(p => p.day <= s.day && s.day <= p.until);
                return new PythonCustomerProgress { customer_id = id, loyalty = (float)(loyal?.score ?? 0), regular_day = loyal?.regular_day ?? 0,
                    extra_weekday = Simulation.ExtraWeekday(id), discontent = (float)Simulation.DiscontentScore(id, s.day), suspended_until = active?.until ?? 0, can_visit = Simulation.CustomerAvailable(id, s.day) };
            }).ToArray();
        }
    }
}
