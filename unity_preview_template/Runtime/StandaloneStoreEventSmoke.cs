using System;
using System.IO;
using System.Linq;
using CatCafePreview.Standalone;
using UnityEngine;
using UnityEngine.Networking;

namespace CatCafePreview
{
    public static class StandaloneStoreEventSmoke
    {
        private static int checks;
        private static void Check(bool valid, string message) { checks++; if (!valid) throw new Exception(message); }
        private static CafeResponse Request(LocalCafeBackend b, string route, string payload = null)
        {
            CafeResponse response = null; var request = b.Request(route, payload, r => response = r);
            while (request.MoveNext()) { } return response;
        }
        private static string Payload(LocalCafeBackend b, string kind, string choice = null, string cat = null)
            => JsonUtility.ToJson(new PythonShiftCommand { request_id = Guid.NewGuid().ToString("N"), instance_id = b.Simulation.State.instance_id, expected_revision = b.Simulation.State.revision, kind = kind, choice = choice, cat_id = cat, save_slot = 1, save_id = "1" });
        private static void Send(LocalCafeBackend b, string kind, string choice = null, string cat = null)
        { var reply = Request(b, "/commands", Payload(b, kind, choice, cat)); Check(reply.result == UnityWebRequest.Result.Success, "Store smoke " + kind + ": " + reply.error); }
        public static int Run(string directory)
        {
            checks = 0;
            var rules = JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
            foreach (string type in new[] { "festival", "rain", "supplies", "trouble", "support", null })
            {
                int seed = Enumerable.Range(0, 1000).First(i => new CafeSimulation(rules, i).CurrentStoreEvent.type == type);
                foreach (string choice in type == "trouble" ? new[] { "repair", "patch", "close" } : type == "support" ? new[] { "full", "small", "decline" } : new string[] { null })
                {
                    var b = new LocalCafeBackend(Path.Combine(directory, (type ?? "none") + "-" + (choice ?? "auto")));
                    b.Simulation.Restore(new CafeSimulation(rules, seed).State);
                    string initial = JsonUtility.ToJson(b.Simulation.State);
                    if (b.Simulation.StoreEventWaiting)
                    {
                        Check(Request(b, "/commands", Payload(b, "start_business")).result == UnityWebRequest.Result.ProtocolError && JsonUtility.ToJson(b.Simulation.State) == initial, "Unanswered event advanced");
                        Check(Request(b, "/commands", Payload(b, "resolve_store_event", "bad")).result == UnityWebRequest.Result.ProtocolError && JsonUtility.ToJson(b.Simulation.State) == initial, "Bad answer mutated state");
                    }
                    Send(b, "save_game"); Send(b, "load_game");
                    Check(b.Simulation.CurrentStoreEvent.type == type && b.Simulation.StoreEventWaiting == (choice != null), "Pending event restore mismatch");
                    if (choice != null)
                    {
                        string answer = Payload(b, "resolve_store_event", choice);
                        var response = Request(b, "/commands", answer); Check(response.result == UnityWebRequest.Result.Success, "Event answer failed");
                        string settled = JsonUtility.ToJson(b.Simulation.State);
                        Check(Request(b, "/commands", answer).text == response.text && JsonUtility.ToJson(b.Simulation.State) == settled, "Duplicate event answer applied twice");
                        Send(b, "save_game"); Send(b, "load_game");
                        if (choice == "close") { Check(b.Simulation.State.day == 2 && b.Simulation.State.history[0].expenses == 60, "Trouble closure mismatch"); continue; }
                        Check(b.Simulation.CurrentStoreEvent.choice == choice, "Resolved event restore mismatch");
                        Check(Request(b, "/commands", Payload(b, "resolve_store_event", choice)).result == UnityWebRequest.Result.ProtocolError, "Resolved event accepted a second answer");
                    }
                    Send(b, "start_business");
                    while (b.Simulation.State.phase != "closed")
                    {
                        if (choice == "patch") Check(b.Simulation.State.exchanges.All(e => e.seat == 0), "Patched seat assigned");
                        Send(b, "advance_business");
                    }
                    double funds = b.Simulation.State.funds, popularity = b.Simulation.State.popularity;
                    Check(b.View().seats.Count(s => s.disabled) == (choice == "patch" ? 1 : 0), "Seat projection mismatch");
                    Send(b, "save_game"); Send(b, "load_game");
                    Check(b.Simulation.State.funds == funds && b.Simulation.State.popularity == popularity, "Closed event reapplied");
                    if (type == "supplies")
                    {
                        Check(b.View().items.count == 1, "Supply not received exactly once"); Send(b, "next_day");
                        if (b.Simulation.StoreEventWaiting) Send(b, "resolve_store_event", b.Simulation.CurrentStoreEvent.type == "trouble" ? "repair" : "decline");
                        var cat = b.Simulation.State.cats.First(c => c.stress > 0 && c.activity == "cafe");
                        string source = b.Simulation.AvailableSupplies[0]; double before = cat.stress;
                        Send(b, "use_item", source, cat.id); Check(cat.stress == Math.Max(0, before - rules.store_events.supply.stress_relief), "Supply relief mismatch");
                        Send(b, "save_game"); Send(b, "load_game"); Check(b.View().items.count == 0, "Used supply returned on load");
                        Check(Request(b, "/commands", Payload(b, "use_item", source, cat.id)).result == UnityWebRequest.Result.ProtocolError, "Supply used twice");
                    }
                }
            }
            return checks;
        }
    }
}
