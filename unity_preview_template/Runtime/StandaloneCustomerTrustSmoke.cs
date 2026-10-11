using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;

namespace CatCafePreview
{
    // Isolated accelerated fixtures. Production Resources/rules is never changed.
    public static class StandaloneCustomerTrustSmoke
    {
        private static int checks;
        private static void Check(bool value, string message) { checks++; if (!value) throw new Exception("Customer trust: " + message); }
        private static CafeResponse Send(LocalCafeBackend b, string kind, string choice = null, string eventId = null, int slot = 1, string[] working = null, string cat = null)
        {
            var cmd = new PythonShiftCommand { request_id = Guid.NewGuid().ToString("N"), instance_id = b.Simulation.State.instance_id,
                expected_revision = b.Simulation.State.revision, kind = kind, choice = choice, event_id = eventId, save_slot = slot,
                save_id = slot.ToString(), working_cats = working ?? new string[0], cat_id = cat };
            CafeResponse reply = null; var request = b.Request("/commands", JsonUtility.ToJson(cmd), r => reply = r); while (request.MoveNext()) { } return reply;
        }
        private static void Good(LocalCafeBackend b, string kind, string choice = null, string id = null, int slot = 1, string[] working = null, string cat = null)
        { var reply = Send(b, kind, choice, id, slot, working, cat); Check(reply.result == UnityWebRequest.Result.Success, kind + ": " + reply.error); }
        private static string Capture(LocalCafeBackend b)
        {
            var s = JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State)); s.NormalizeJsonStrings(); s.instance_id = ""; s.revision = 0; return JsonUtility.ToJson(s);
        }
        private static void Resume(LocalCafeBackend b, int slot)
        { string before = Capture(b); Good(b, "save_game", slot: slot); Good(b, "load_game", slot: slot); Check(Capture(b) == before, "Save/load changed gameplay"); }
        private static void Reject(LocalCafeBackend b, string kind, string choice = null, string id = null)
        { string before = JsonUtility.ToJson(b.Simulation.State); Check(Send(b, kind, choice, id).result == UnityWebRequest.Result.ProtocolError, "Accepted invalid " + kind); Check(before == JsonUtility.ToJson(b.Simulation.State), "Rejected command mutated state"); }
        public static int Run(string directory)
        {
            checks = 0;
            foreach (string outcome in new[] { "ignore", "recovered", "departed", "normal", "unanswered", "repeat" })
            {
                var rules = JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
                rules.rules_id += "-trust-fixture-" + outcome;
                rules.arrival_ticks = new[] { 0, 0 }; rules.queue_capacity = 1;
                rules.patterns = new[] { new WeekPattern { days = new[] { 0, 2 } }, new WeekPattern { days = Enumerable.Range(0, 7).ToArray() } };
                rules.discontent.waiting_gain = 100; rules.discontent.suspension_days = 1; rules.store_events.probability = 0;
                rules.satisfaction.satisfied_score = outcome == "recovered" || outcome == "repeat" ? 1 : 5;
                rules.satisfaction.dissatisfied_score = outcome == "departed" ? 4 : 0;
                string path = Path.Combine(directory, outcome); var b = new LocalCafeBackend(path, rules);
                if (File.Exists(Path.Combine(path, "slot-4.json")))
                {
                    Good(b, "load_game", slot: 2); Check(b.Simulation.CustomerTrustWaiting && b.View().customer_trust.events.Any(e => e.can_respond), "Restart lost pending warning");
                    Good(b, "load_game", slot: 4); string before = Capture(b); var e = b.Simulation.State.trust_events.Last();
                    if (e.choice != null) { Good(b, "resolve_customer_trust", e.choice, e.id); Check(before == Capture(b), "Restart repeated popularity loss"); }
                }
                Good(b, "new_game", "free"); Reject(b, "resolve_customer_trust", "recover", "unknown");
                bool warned = false, recovering = false;
                for (int day = 1; day <= (outcome == "repeat" ? 6 : 5); day++)
                {
                    var s = b.Simulation.State;
                    foreach (var c in s.cats.Where(c => c.activity == "missing" && c.missing_remaining == 0).ToArray()) Good(b, "resolve_missing", cat: c.id);
                    foreach (var e in s.trust_events.Where(e => e.status == "waiting").ToArray())
                    {
                        warned = true; Resume(b, 2); Check(b.View().customer_trust.events.Single(v => v.event_id == e.id).can_respond, "Warning projection disabled");
                        Reject(b, "start_business"); Reject(b, "set_shifts"); Reject(b, "day_off"); Reject(b, "resolve_store_event", "decline");
                        if (b.Simulation.IntakeWaiting) Reject(b, "resolve_intake", "decline");
                        Reject(b, "resolve_customer_trust", "invalid", e.id);
                        if (outcome == "unanswered") break;
                        s = b.Simulation.State; double popularity = s.popularity, funds = s.funds;
                        string choice = outcome == "ignore" ? "ignore" : "recover";
                        Good(b, "resolve_customer_trust", choice, e.id);
                        Check(s.popularity == popularity - (choice == "ignore" ? rules.trust.popularity_loss : 0) && s.funds == funds, "Wrong trust cost/popularity");
                        string before = Capture(b); Good(b, "resolve_customer_trust", choice, e.id); Check(Capture(b) == before, "Repeated answer changed state");
                        Reject(b, "resolve_customer_trust", choice == "ignore" ? "recover" : "ignore", e.id); Resume(b, 3);
                        recovering |= choice == "recover";
                    }
                    if (outcome == "unanswered" && warned) break;
                    if (b.Simulation.IntakeWaiting) Good(b, "resolve_intake", "decline");
                    if (b.Simulation.RegularIntroductionWaiting) Good(b, "resolve_regular_introduction", "decline", cat: rules.regular_introduction.cat.id);
                    s = b.Simulation.State;
                    Good(b, "set_shifts", working: s.cats.Where((c,i) => (i+s.day)%3!=0 && c.health == "healthy" && c.activity == "cafe").Select(c => c.id).ToArray());
                    Good(b, "start_business");
                    while (b.Simulation.State.phase != "closed")
                    {
                        if (recovering && b.Simulation.State.tick == 11) Resume(b, 5);
                        Good(b, "advance_business");
                    }
                    if ((outcome == "recovered" || outcome == "departed") && b.Simulation.State.trust_events.Any(e => e.status == "resolved")) break;
                    Good(b, "next_day");
                }
                Check(warned, "Fixture never produced warning"); Resume(b, 4);
                var row = b.Simulation.State.trust_customers.Single(r => r.customer_id == "guest-2");
                string expected = outcome == "ignore" || outcome == "departed" ? "departed" : outcome == "recovered" ? "stable" : outcome == "normal" ? "recovery" : "waiting";
                Check(row.status == expected, "Wrong outcome " + outcome + ": " + row.status);
                if (outcome == "repeat") Check(b.Simulation.State.trust_events.Count >= 2, "No repeated warning after recovery");
                if (row.status == "departed") Check(!b.Simulation.CustomerSchedule(b.Simulation.State.day + 14).Any(a => a.customer_id == row.customer_id), "Departed customer returned");
                var fresh = new LocalCafeBackend(path, rules); Good(fresh, "load_game", slot: 4); Check(Capture(fresh) == Capture(b), "Fresh backend changed trust result");
            }
            return checks;
        }
    }
}
