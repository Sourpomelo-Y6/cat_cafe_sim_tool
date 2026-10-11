using System.Text.Json;
using CatCafePreview.Standalone;
static class RegularIntroductionChecks
{
    static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
    static string Encode<T>(T value) => JsonSerializer.Serialize(value, Options);
    static T Decode<T>(string text) => JsonSerializer.Deserialize<T>(text, Options);
    public static void Run(CafeRules defaults, Action<bool,string> assert, string directory)
    {
        // Accelerated fixture rules are isolated from production defaults.
        foreach (bool regularFirst in new[] { false, true })
        {
            var rules = Decode<CafeRules>(Encode(defaults)); rules.loyalty.threshold = 25; rules.intake.day = 2;
            var sim = new CafeSimulation(rules);
            void Rejected(Action action)
            {
                string before = Encode(sim.State); bool failed = false;
                try { action(); } catch (ArgumentException) { failed = true; } catch (InvalidOperationException) { failed = true; }
                assert(failed && before == Encode(sim.State), "Invalid introduction operation mutated state");
            }
            Rejected(() => sim.Execute("resolve_regular_introduction", choice: "accept"));
            sim.Execute("start_business"); while (sim.State.phase != "closed") sim.Execute("advance_business");
            assert(sim.State.regular_introduction.status == "untriggered" && sim.State.loyalty.Any(c => c.regular_day == 1), "Introduction must wait until next preparation");
            var first = sim.State.loyalty.Where(c => c.regular_day > 0).OrderBy(c => c.regular_day).ThenBy(c => c.customer_id, StringComparer.Ordinal).First();
            sim.Execute("next_day"); sim.Validate(sim.State);
            assert(sim.RegularIntroductionWaiting && sim.IntakeWaiting && sim.State.regular_introduction.customer_id == first.customer_id, "First regular or coexisting intake mismatch");
            Rejected(() => sim.Execute("start_business")); Rejected(() => sim.Execute("day_off")); Rejected(() => sim.Execute("set_shifts", new[] { rules.cats[0].id })); Rejected(() => sim.Execute("resolve_regular_introduction", choice: "invalid"));
            if (sim.StoreEventWaiting) { Rejected(() => sim.Execute("resolve_regular_introduction", choice: "decline")); sim.Execute("resolve_store_event", choice: sim.CurrentStoreEvent.type == "trouble" ? "patch" : "decline"); }
            string pending = Encode(sim.State);
            sim.State.cats[0].activity = "missing"; sim.State.cats[0].missing_remaining = 0;
            Rejected(() => sim.Execute("resolve_regular_introduction", choice: "decline")); sim.Restore(Decode<CafeSnapshot>(pending));
            double funds = sim.State.funds; sim.State.funds = rules.regular_introduction.cost;
            Rejected(() => sim.Execute("resolve_regular_introduction", choice: "accept")); sim.State.funds = funds;
            string firstKind = regularFirst ? "resolve_regular_introduction" : "resolve_intake";
            string otherKind = regularFirst ? "resolve_intake" : "resolve_regular_introduction";
            sim.Execute(firstKind, choice: "accept"); sim.Validate(sim.State);
            Rejected(() => sim.Execute(otherKind, choice: "accept")); sim.Execute(otherKind, choice: "decline"); sim.Validate(sim.State);
            assert(sim.State.cats.Count == 6 && sim.State.funds == funds - rules.regular_introduction.cost && sim.State.recruitment_expenses == rules.regular_introduction.cost, "Shared admission capacity or fees mismatch");
            assert(sim.State.cats.Last().id == (regularFirst ? rules.regular_introduction.cat.id : rules.intake.cat.id) && !sim.State.cats.Last().working, "Joined cat invalid");
            string answered = Encode(sim.State);
            sim.Execute("resolve_regular_introduction", choice: regularFirst ? "accept" : "decline"); assert(answered == Encode(sim.State), "Repeated introduction changed state or revision");
            Rejected(() => sim.Execute("resolve_regular_introduction", choice: regularFirst ? "decline" : "accept"));
            foreach (Action<CafeSnapshot> forge in new Action<CafeSnapshot>[] {
                s => s.regular_introduction.customer_id = "forged", s => s.regular_introduction.regular_day++,
                s => s.regular_introduction.presented_day++, s => s.regular_introduction.resolved_day++,
                s => s.regular_introduction.status = "untriggered", s => s.recruitment_expenses++,
                s => s.cats.Add(new CafeCat { id = rules.regular_introduction.cat.id }) })
            {
                var bad = Decode<CafeSnapshot>(answered); forge(bad); bool failed = false;
                try { sim.Restore(bad); } catch (ArgumentException) { failed = true; }
                assert(failed && answered == Encode(sim.State), "Forged introduction save changed live state");
            }
            bool failWrite = false;
            var store = new CafeSaveStore(Path.Combine(directory, regularFirst.ToString()), envelope => { if (failWrite) throw new IOException("injected failure"); return Encode(envelope); }, Decode<CafeSaveEnvelope>, sim.Validate);
            store.Save(1, sim.State); string disk = File.ReadAllText(Path.Combine(directory, regularFirst.ToString(), "slot-1.json")); failWrite = true;
            bool failedWrite = false; try { store.Save(1, sim.State); } catch (IOException) { failedWrite = true; }
            assert(failedWrite && answered == Encode(sim.State) && disk == File.ReadAllText(Path.Combine(directory, regularFirst.ToString(), "slot-1.json")), "Failed save changed introduction or prior save");
            failWrite = false; sim.Restore(store.Load(1).state); sim.Execute("day_off"); sim.Validate(sim.State);
            assert(sim.State.recruitment_expenses == 0 && sim.State.history.Last().recruitment_expenses == rules.regular_introduction.cost, "Introduction cost repeated next day");
        }
    }
}
