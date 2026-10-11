using System;
using System.Collections.Generic;
using System.Linq;

namespace CatCafePreview.Standalone
{
    public sealed partial class CafeSimulation
    {
        public bool CustomerTrustWaiting => State.trust_events.Any(e => e.status == "waiting");
        internal void PresentCustomerTrust()
        {
            foreach (var d in State.discontent.OrderBy(r => r.customer_id, StringComparer.Ordinal))
            {
                var row = State.trust_customers.Find(r => r.customer_id == d.customer_id);
                if (d.suspensions.Count < Rules.trust.warning_suspensions || row != null && row.status != "stable") continue;
                var e = new CafeTrustEvent { id = "trust-" + State.day + "-" + d.customer_id, customer_id = d.customer_id,
                    day = State.day, suspensions = d.suspensions.Count, status = "waiting" };
                State.trust_events.Add(e);
                if (row == null) { row = new CafeTrustCustomer { customer_id = d.customer_id }; State.trust_customers.Add(row); }
                row.status = "waiting"; row.event_id = e.id; row.departed_day = 0; row.reason = null;
            }
        }
        private void ResolveCustomerTrust(string id, string choice)
        {
            var e = State.trust_events.Find(r => r.id == id);
            if (e == null || choice != "recover" && choice != "ignore") throw new ArgumentException("信頼回復イベントと回答を選んでください。");
            if (e.status != "waiting")
            {
                if (e.choice != choice) throw new InvalidOperationException("回答済みの信頼回復イベントは変更できません。");
                return;
            }
            if (choice == "recover") { e.status = "recovery"; State.trust_customers.Find(r => r.customer_id == e.customer_id).status = "recovery"; }
            else DepartTrust(e, "ignored");
            e.choice = choice; State.revision++;
        }
        private void DepartTrust(CafeTrustEvent e, string reason)
        {
            var row = State.trust_customers.Find(r => r.customer_id == e.customer_id);
            row.status = "departed"; row.departed_day = State.day; row.reason = reason;
            e.status = "resolved"; e.resolved_day = State.day; e.outcome = "departed";
            State.popularity = Math.Max(0, State.popularity - Rules.trust.popularity_loss); CheckEnd();
        }
        internal void ApplyTrustService(string customer, string satisfaction)
        {
            var row = State.trust_customers.Find(r => r.customer_id == customer);
            if (row?.status != "recovery") return;
            var e = State.trust_events.Find(r => r.id == row.event_id);
            if (satisfaction == "satisfied")
            {
                var d = State.discontent.Find(r => r.customer_id == customer);
                d.score = Rules.trust.recovery_score; d.score_day = State.day;
                row.status = "stable"; row.event_id = null; row.reason = null;
                e.status = "resolved"; e.resolved_day = State.day; e.outcome = "recovered";
            }
            else if (satisfaction == "dissatisfied") DepartTrust(e, "recovery_failed");
        }
        private bool TrustAvailable(string customer, int day)
        {
            var row = State.trust_customers.Find(r => r.customer_id == customer);
            return row?.status != "departed" || day < row.departed_day || day == row.departed_day && row.reason == "recovery_failed";
        }
        private void ReplayTrustRecovery(CafeSnapshot s, List<CafeDiscontent> rows, int day, string customer, string satisfaction)
        {
            if (satisfaction != "satisfied" || !s.trust_events.Any(e => e.customer_id == customer && e.outcome == "recovered" && e.resolved_day == day)) return;
            var d = rows.Find(r => r.customer_id == customer);
            if (d == null) throw new ArgumentException("Trust recovery has no discontent history");
            d.score = Rules.trust.recovery_score; d.score_day = day;
        }
        private void ValidateTrust(CafeSnapshot s)
        {
            if (Rules.trust == null || Rules.trust.warning_suspensions < 2 || !Range(Rules.trust.recovery_score, 0, Rules.discontent.threshold) || Rules.trust.recovery_score >= Rules.discontent.threshold || !Range(Rules.trust.popularity_loss, 0, double.MaxValue)
                || s.trust_customers == null || s.trust_events == null || s.discontent == null || s.trust_customers.Any(r => r == null) || s.trust_events.Any(e => e == null)
                || s.trust_customers.Select(r => r.customer_id).Distinct().Count() != s.trust_customers.Count || s.trust_events.Select(e => e.id).Distinct().Count() != s.trust_events.Count) throw new ArgumentException("Invalid customer trust data");
            foreach (var e in s.trust_events)
            {
                var d = s.discontent.Find(r => r.customer_id == e.customer_id);
                if (e.day < 1 || e.day > s.day || e.id != "trust-" + e.day + "-" + e.customer_id || !s.trust_customers.Any(r => r.customer_id == e.customer_id)
                    || e.suspensions < Rules.trust.warning_suspensions || d?.suspensions == null || e.suspensions != d.suspensions.Count(p => p.day < e.day)) throw new ArgumentException("Invalid trust warning");
                if (e.status == "waiting" ? e.choice != null || e.resolved_day != 0 || e.outcome != null
                    : e.status == "recovery" ? e.choice != "recover" || e.resolved_day != 0 || e.outcome != null
                    : e.status != "resolved" || !new[] { "recover", "ignore" }.Contains(e.choice) || e.resolved_day < e.day || e.resolved_day > s.day || !new[] { "recovered", "departed" }.Contains(e.outcome)) throw new ArgumentException("Invalid trust result");
                if (e.status == "resolved")
                {
                    if (e.choice == "ignore" && e.outcome != "departed") throw new ArgumentException("Ignored trust cannot recover");
                    if (e.choice == "recover")
                    {
                        string satisfaction = e.resolved_day == s.day ? s.visits.Find(v => v.id == e.customer_id)?.satisfaction?.level
                            : s.history.Find(h => h.day == e.resolved_day)?.customer_records?.Find(r => r.customer_id == e.customer_id)?.satisfaction;
                        if (satisfaction != (e.outcome == "recovered" ? "satisfied" : "dissatisfied")) throw new ArgumentException("Trust result disagrees with service");
                    }
                }
            }
            // Present runs at every next-day preparation. A recovered customer
            // warns again the following day even without a new suspension.
            foreach (var d in s.discontent.Where(d => d.suspensions != null && d.suspensions.Count >= Rules.trust.warning_suspensions))
            {
                int expectedDay = d.suspensions[Rules.trust.warning_suspensions - 1].day + 1;
                foreach (var e in s.trust_events.Where(e => e.customer_id == d.customer_id).OrderBy(e => e.day))
                {
                    if (e.day != expectedDay) throw new ArgumentException("Trust warning presentation disagrees with history");
                    expectedDay = e.outcome == "recovered" ? e.resolved_day + 1 : int.MaxValue;
                }
                if (expectedDay <= s.day) throw new ArgumentException("Missing trust warning");
            }
            foreach (var r in s.trust_customers)
            {
                var events = s.trust_events.Where(e => e.customer_id == r.customer_id).OrderBy(e => e.day).ToArray(); var latest = events.LastOrDefault();
                if (string.IsNullOrEmpty(r.customer_id) || latest == null || !new[] { "waiting", "recovery", "stable", "departed" }.Contains(r.status)) throw new ArgumentException("Invalid trust customer");
                for (int i = 1; i < events.Length; i++)
                    if (events[i - 1].outcome != "recovered" || events[i - 1].resolved_day >= events[i].day || events[i - 1].suspensions > events[i].suspensions) throw new ArgumentException("Invalid repeated warning");
                if (r.status != "departed" && (r.departed_day != 0 || r.reason != null)
                    || (r.status == "waiting" || r.status == "recovery") && (r.event_id != latest.id || r.status != latest.status)
                    || r.status == "stable" && (r.event_id != null || latest.outcome != "recovered")
                    || r.status == "departed" && (r.event_id != latest.id || latest.outcome != "departed" || r.departed_day != latest.resolved_day || r.reason != (latest.choice == "ignore" ? "ignored" : "recovery_failed"))) throw new ArgumentException("Trust customer disagrees with latest event");
            }
        }
    }
}
