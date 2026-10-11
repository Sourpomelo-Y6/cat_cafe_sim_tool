using System.Text.Json;
using CatCafePreview.Standalone;
static class IntakeChecks
{
    static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
    static string Encode<T>(T value) => JsonSerializer.Serialize(value, Options);
    static T Decode<T>(string text) => JsonSerializer.Deserialize<T>(text, Options);
    public static void Run(CafeRules rules, Action<bool,string> assert, string directory)
    {
        foreach (string choice in new[] { "accept", "decline" })
        {
            var sim = new CafeSimulation(rules);
            void Rejected(Action action)
            {
                string before = Encode(sim.State); bool failed = false;
                try { action(); } catch (ArgumentException) { failed = true; } catch (InvalidOperationException) { failed = true; }
                assert(failed && before == Encode(sim.State), "Invalid intake operation mutated state");
            }
            while (sim.State.day < rules.intake.day)
            {
                if (sim.StoreEventWaiting) sim.Execute("resolve_store_event", choice: sim.CurrentStoreEvent.type == "trouble" ? "patch" : "decline");
                sim.Execute("day_off");
            }
            assert(sim.IntakeWaiting, "Intake not presented by holiday");
            Rejected(() => sim.Execute("day_off")); Rejected(() => sim.Execute("start_business")); Rejected(() => sim.Execute("set_shifts", new[] { rules.cats[0].id }));
            if (sim.StoreEventWaiting) { Rejected(() => sim.Execute("resolve_intake", choice: choice)); sim.Execute("resolve_store_event", choice: sim.CurrentStoreEvent.type == "trouble" ? "patch" : "decline"); }
            string pending = Encode(sim.State); double funds = sim.State.funds;
            var missing = sim.State.cats[0]; missing.activity = "missing"; missing.missing_remaining = 0;
            Rejected(() => sim.Execute("resolve_intake", choice: choice));
            sim.Restore(Decode<CafeSnapshot>(pending));
            double original = sim.State.funds; sim.State.funds = rules.intake.cost;
            Rejected(() => sim.Execute("resolve_intake", choice: "accept")); sim.State.funds = original;
            sim.Execute("resolve_intake", choice: choice); sim.Validate(sim.State);
            assert(sim.State.funds == funds - (choice == "accept" ? rules.intake.cost : 0), "Incorrect intake expense");
            string answered = Encode(sim.State); sim.Execute("resolve_intake", choice: choice); assert(answered == Encode(sim.State), "Duplicate intake answer changed revision or state");
            Rejected(() => sim.Execute("resolve_intake", choice: choice == "accept" ? "decline" : "accept"));
            foreach (Action<CafeSnapshot> forge in new Action<CafeSnapshot>[] {
                s => s.intake_request.resolved_day++, s => s.intake_request.presented_day--,
                s => s.recruitment_expenses++, s => s.cats.Add(new CafeCat { id = "forged" }),
                s => s.intake_request.status = "waiting" })
            {
                var bad = Decode<CafeSnapshot>(answered); forge(bad); bool failed = false;
                try { sim.Restore(bad); } catch (ArgumentException) { failed = true; }
                assert(failed && answered == Encode(sim.State), "Invalid intake save accepted or changed live state");
            }
            bool failWrite = false;
            var store = new CafeSaveStore(Path.Combine(directory, choice), envelope => { if (failWrite) throw new IOException("injected disk failure"); return Encode(envelope); }, Decode<CafeSaveEnvelope>, sim.Validate);
            store.Save(1, sim.State); string disk = File.ReadAllText(Path.Combine(directory, choice, "slot-1.json")); failWrite = true;
            bool rejectedWrite = false; try { store.Save(1, sim.State); } catch (IOException) { rejectedWrite = true; }
            assert(rejectedWrite && disk == File.ReadAllText(Path.Combine(directory, choice, "slot-1.json")) && answered == Encode(sim.State), "Failed save changed intake state or prior save");
            failWrite = false; sim.Restore(store.Load(1).state); sim.Execute("resolve_intake", choice: choice);
            sim.Execute("day_off"); sim.Validate(sim.State);
            assert(sim.State.recruitment_expenses == 0 && sim.State.history.Last().recruitment_expenses == (choice == "accept" ? rules.intake.cost : 0), "Wrong intake history settlement");
        }
    }
}
