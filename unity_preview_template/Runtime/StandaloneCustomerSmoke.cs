using System;
using System.IO;
using System.Linq;
using CatCafePreview.Standalone;
using UnityEngine;
using UnityEngine.Networking;

namespace CatCafePreview
{
    public static class StandaloneCustomerSmoke
    {
        [Serializable] private class Checkpoint
        {
            public CafeTrustCustomer[] trust_customers; public CafeTrustEvent[] trust_events;
            public CafeLoyalty[] loyalty; public CafeDiscontent[] discontent;
            public CafeArrival[] schedule; public PythonCustomerProgress[] progress;
            public CafeRegularIntroduction introduction; public CafeCat[] cats; public double recruitment_expenses;
        }
        private static int checks;
        private static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
        private static void Send(LocalCafeBackend b, string kind, string[] working = null, string choice = null, string cat = null, string eventId = null)
        {
            var cmd = new PythonShiftCommand { request_id = Guid.NewGuid().ToString("N"), instance_id = b.Simulation.State.instance_id, expected_revision = b.Simulation.State.revision,
                kind = kind, working_cats = working ?? new string[0], choice = choice, cat_id = cat, event_id = eventId, save_slot = 1, save_id = "1" };
            CafeResponse response = null; var request = b.Request("/commands", JsonUtility.ToJson(cmd), r => response = r);
            while (request.MoveNext()) { } Check(response.result == UnityWebRequest.Result.Success, "Customer smoke " + kind + ": " + response.error);
        }
        private static string Capture(LocalCafeBackend b)
            => JsonUtility.ToJson(new Checkpoint { trust_customers = b.Simulation.State.trust_customers.ToArray(), trust_events = b.Simulation.State.trust_events.ToArray(), loyalty = b.Simulation.State.loyalty.ToArray(), discontent = b.Simulation.State.discontent.ToArray(), schedule = b.Simulation.CustomerSchedule(b.Simulation.State.day), progress = b.View().customer_progress, introduction = b.Simulation.State.regular_introduction, cats = b.Simulation.State.cats.ToArray(), recruitment_expenses = b.Simulation.State.recruitment_expenses });
        private static void Resume(LocalCafeBackend b)
        {
            string before = Capture(b); Send(b, "save_game"); Send(b, "load_game");
            Check(Capture(b) == before, "Customer state, suspension or schedule changed on resume");
        }
        public static int Run(string directory)
        {
            checks = 0; bool regular = false, extra = false, suspended = false, returned = false;
            var rules = JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
            foreach (int seed in new[] { 0, 1, 42 })
            {
                var b = new LocalCafeBackend(Path.Combine(directory, "seed-" + seed));
                b.Simulation.Restore(new CafeSimulation(rules, seed).State);
                for (int day = 1; day <= 28; day++)
                {
                    var s = b.Simulation.State; Resume(b);
                    foreach (var missing in s.cats.Where(c => c.activity == "missing" && c.missing_remaining == 0).ToArray()) Send(b, "resolve_missing", cat: missing.id);
                    s = b.Simulation.State;
                    regular |= s.loyalty.Any(r => r.regular_day > 0);
                    foreach (var r in s.discontent)
                    {
                        suspended |= !b.Simulation.CustomerAvailable(r.customer_id, s.day);
                        var last = r.suspensions.LastOrDefault();
                        if (last != null && r.score_day <= last.until && last.until < s.day)
                        {
                            returned = true; Check(b.Simulation.DiscontentScore(r.customer_id, s.day) == rules.discontent.return_score && b.Simulation.CustomerAvailable(r.customer_id, s.day), "Expired suspension did not return");
                        }
                    }
                    extra |= s.loyalty.Any(r => r.regular_day > 0 && s.day > r.regular_day && b.Simulation.ExtraWeekday(r.customer_id) == (rules.start_weekday + s.day - 1) % 7 && b.Simulation.CustomerSchedule(s.day).Any(a => a.customer_id == r.customer_id && a.tick == 0));
                    foreach (var trust in s.trust_events.Where(e => e.status == "waiting").ToArray()) Send(b, "resolve_customer_trust", choice: seed == 1 ? "ignore" : "recover", eventId: trust.id);
                    if (b.Simulation.StoreEventWaiting)
                    {
                        string[] options = b.Simulation.CurrentStoreEvent.type == "trouble" ? new[] { "repair", "patch", "close" } : new[] { "full", "small", "decline" };
                        string choice = options[(seed + day) % 3]; if (choice == "close" && (b.Simulation.IntakeWaiting || b.Simulation.RegularIntroductionWaiting)) choice = "patch"; Send(b, "resolve_store_event", choice: choice);
                        if (choice == "close") continue;
                    }
                    if (b.Simulation.IntakeWaiting) Send(b, "resolve_intake", choice: seed == 1 ? "decline" : "accept");
                    if (b.Simulation.RegularIntroductionWaiting) Send(b, "resolve_regular_introduction", choice: seed == 1 ? "accept" : "decline", cat: b.Simulation.Rules.regular_introduction.cat.id);
                    s = b.Simulation.State;
                    string supply = b.Simulation.AvailableSupplies.FirstOrDefault(); var stressed = s.cats.FirstOrDefault(c => c.activity == "cafe" && c.stress > 0);
                    if (supply != null && stressed != null) Send(b, "use_item", choice: supply, cat: stressed.id);
                    var working = s.cats.Where((c, i) => c.health == "healthy" && c.activity == "cafe" && (i + day) % 3 != 0).Select(c => c.id).ToArray();
                    Send(b, "set_shifts", working);
                    if (working.Length == 0) { Send(b, "day_off"); continue; }
                    Send(b, "start_business");
                    while (b.Simulation.State.phase != "closed")
                    {
                        if (b.Simulation.State.tick == 11 || b.Simulation.State.tick == 25) Resume(b);
                        Send(b, "advance_business");
                    }
                    Resume(b); s = b.Simulation.State;
                    Check(s.game_over == null, "Unexpected customer-smoke game over");
                    foreach (var missing in s.cats.Where(c => c.activity == "missing" && c.missing_remaining == 0).ToArray()) Send(b, "resolve_missing", cat: missing.id);
                    Send(b, "next_day");
                }
                Check(b.Simulation.State.day == 29, "Customer smoke did not complete 28 days");
                Resume(b);
                foreach (var r in b.Simulation.State.discontent)
                {
                    var last = r.suspensions.LastOrDefault();
                    if (last != null && r.score_day <= last.until && last.until < b.Simulation.State.day)
                    {
                        returned = true; Check(b.Simulation.DiscontentScore(r.customer_id, b.Simulation.State.day) == rules.discontent.return_score && b.Simulation.CustomerAvailable(r.customer_id, b.Simulation.State.day), "Saved return state mismatch");
                    }
                }
                // A fresh backend reads the persisted customer state and arrival plan.
                string expected = Capture(b); var restarted = new LocalCafeBackend(Path.Combine(directory, "seed-" + seed)); Send(restarted, "load_game");
                Check(Capture(restarted) == expected, "Fresh backend lost customer state");
            }
            Check(regular && extra && suspended && returned, "Customer smoke missed regular/suspension/return coverage");
            return checks;
        }
    }
}
