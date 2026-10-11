using System.Text.Json;
using CatCafePreview.Standalone;

static class TrustChecks
{
    static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
    static string Encode<T>(T value) => JsonSerializer.Serialize(value, Options);
    static T Decode<T>(string text) => JsonSerializer.Deserialize<T>(text, Options);
    public static void CheckFinal(CafeSimulation sim, Action<bool,string> assert)
    {
        var row = sim.State.trust_customers.Single(r => r.customer_id == "guest-2");
        var e = sim.State.trust_events.Last(r => r.customer_id == row.customer_id);
        if (sim.CustomerTrustWaiting)
        {
            foreach (string kind in new[] { "start_business", "set_shifts", "day_off", "next_day", "resolve_intake", "resolve_regular_introduction", "use_item" })
            {
                string before = Encode(sim.State); bool failed = false;
                try { sim.Execute(kind, choice: "decline"); } catch (InvalidOperationException) { failed = true; } catch (ArgumentException) { failed = true; }
                // Already answered intake/introduction is idempotent even while trust waits.
                bool replay = kind == "resolve_intake" && sim.State.intake_request.status == "declined" || kind == "resolve_regular_introduction" && sim.State.regular_introduction.status == "declined";
                assert((failed || replay) && before == Encode(sim.State), "Pending warning did not protect " + kind);
            }
        }
        if (row.status == "departed")
        {
            assert(!sim.CustomerAvailable(row.customer_id, row.departed_day + 1), "Permanent departure returned");
            // Discontent suspension is independently active on the ignored day.
            assert(sim.CustomerAvailable(row.customer_id, row.departed_day - 1) == !sim.State.discontent.Single(d => d.customer_id == row.customer_id).suspensions.Any(p => p.day < row.departed_day - 1 && row.departed_day - 1 <= p.until), "Historical availability changed");
        }
        if (row.status == "waiting")
        {
            var zero = new CafeSimulation(sim.Rules); zero.Restore(Decode<CafeSnapshot>(Encode(sim.State))); zero.State.popularity = 5;
            zero.Execute("resolve_customer_trust", choice: "ignore", source: e.id);
            assert(zero.State.popularity == 0 && zero.State.game_over == "popularity", "Departure did not clamp popularity/end business");
        }
        if (e.choice != null)
        {
            string before = Encode(sim.State); sim.Execute("resolve_customer_trust", choice: e.choice, source: e.id);
            assert(before == Encode(sim.State), "Repeated trust answer mutated state");
            bool failed = false; try { sim.Execute("resolve_customer_trust", choice: e.choice == "recover" ? "ignore" : "recover", source: e.id); } catch (InvalidOperationException) { failed = true; }
            assert(failed && before == Encode(sim.State), "Changed answer was accepted");
        }
        foreach (Action<CafeSnapshot> forge in new Action<CafeSnapshot>[] {
            s => s.trust_events.Last().id = "forged", s => s.trust_events.Last().suspensions++,
            s => s.trust_events.Last().resolved_day = s.day + 1, s => s.trust_customers.Last().event_id = "forged",
            s => s.trust_customers.Last().departed_day = s.day + 1,
            s => s.trust_events.Clear(), s => s.trust_customers.Clear(), s => { s.trust_events.Clear(); s.trust_customers.Clear(); } })
        {
            string before = Encode(sim.State); var bad = Decode<CafeSnapshot>(before); forge(bad); bool failed = false;
            try { sim.Restore(bad); } catch (ArgumentException) { failed = true; }
            assert(failed && before == Encode(sim.State), "Forged trust load mutated state");
        }
    }
}
