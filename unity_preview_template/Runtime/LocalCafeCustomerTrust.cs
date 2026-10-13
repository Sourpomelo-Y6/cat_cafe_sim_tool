using System.Linq;

namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonCustomerTrust CustomerTrustView()
        {
            var s = Simulation.State;
            return new PythonCustomerTrust { popularity_loss = (float)Simulation.Rules.trust.popularity_loss,
                events = s.trust_events.Select(e => new PythonTrustEvent { event_id = e.id, customer_id = e.customer_id,
                    name = Details.CustomerName(e.customer_id), day = e.day, suspensions = e.suspensions, status = e.status, choice = e.choice,
                    outcome = e.outcome, resolved_day = e.resolved_day,
                    reason = e.outcome == "departed" ? e.choice == "ignore" ? "ignored" : "recovery_failed" : null,
                    can_respond = e.status == "waiting" && s.game_over == null }).ToArray() };
        }
    }
}
