using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneIntakeSmoke
    {
        private static int checks;
        private static void Check(bool value, string message) { checks++; if (!value) throw new Exception("Intake: " + message); }
        private static CafeResponse Send(LocalCafeBackend b, string kind, string choice = null, int slot = 1, string[] working = null)
        {
            var c = new PythonShiftCommand { request_id = Guid.NewGuid().ToString("N"), instance_id = b.Simulation.State.instance_id, expected_revision = b.Simulation.State.revision, kind = kind, choice = choice, save_slot = slot, save_id = slot.ToString(), working_cats = working ?? new string[0] };
            CafeResponse result = null; var request = b.Request("/commands", JsonUtility.ToJson(c), r => result = r); while (request.MoveNext()) { } return result;
        }
        private static void Good(LocalCafeBackend b, string kind, string choice = null, int slot = 1, string[] working = null) { var result = Send(b, kind, choice, slot, working); Check(result.result == UnityWebRequest.Result.Success, kind + ": " + result.error); }
        private static string Capture(LocalCafeBackend b)
        {
            var s = JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State)); s.NormalizeJsonStrings(); s.revision = 0; s.instance_id = ""; return JsonUtility.ToJson(s);
        }
        private static void Resume(LocalCafeBackend b, int slot)
        {
            string before = Capture(b); Good(b, "save_game", slot: slot); Good(b, "load_game", slot: slot); Check(before == Capture(b), "Resume changed gameplay");
        }
        private static void Reject(LocalCafeBackend b, string kind, string choice = null)
        {
            string before = JsonUtility.ToJson(b.Simulation.State); Check(Send(b, kind, choice).result == UnityWebRequest.Result.ProtocolError, "Accepted invalid " + kind); Check(before == JsonUtility.ToJson(b.Simulation.State), "Rejected command changed state");
        }
        public static int Run(string directory)
        {
            checks = 0;
            foreach (string choice in new[] { "accept", "decline" })
            {
                string path = Path.Combine(directory, choice); var b = new LocalCafeBackend(path);
                if (File.Exists(Path.Combine(path, "slot-4.json")))
                {
                    Good(b, "load_game", slot: 2); Check(b.Simulation.IntakeWaiting, "Restart lost pending request");
                    Good(b, "load_game", slot: 3); string answered = Capture(b); Good(b, "resolve_intake", choice); Check(answered == Capture(b), "Restart duplicated response");
                    Good(b, "load_game", slot: 4); Check(b.Simulation.State.day == 5 && b.Simulation.State.recruitment_expenses == 0, "Restart lost next day");
                }
                Good(b, "new_game", "free"); Resume(b, 1); Reject(b, "resolve_intake", choice);
                while (b.Simulation.State.day < 4)
                {
                    if (b.Simulation.StoreEventWaiting) Good(b, "resolve_store_event", b.Simulation.CurrentStoreEvent.type == "trouble" ? "patch" : "decline");
                    Good(b, "day_off");
                }
                Check(b.Simulation.IntakeWaiting && b.View().intake_request.status == "waiting", "No day-four request");
                Resume(b, 2); Reject(b, "start_business"); Reject(b, "day_off"); Reject(b, "set_shifts"); Reject(b, "resolve_intake", "invalid");
                if (b.Simulation.StoreEventWaiting) { Reject(b, "resolve_intake", choice); Good(b, "resolve_store_event", b.Simulation.CurrentStoreEvent.type == "trouble" ? "patch" : "decline"); Resume(b, 2); }
                Check(b.View().intake_request.can_decline && b.View().intake_request.can_accept, "Response projection disabled");
                double funds = b.Simulation.State.funds; Good(b, "resolve_intake", choice);
                var s = b.Simulation.State; bool accepted = choice == "accept";
                Check(s.cats.Count == (accepted ? 6 : 5) && s.funds == funds - (accepted ? b.Simulation.Rules.intake.cost : 0), "Roster or expense mismatch");
                Check(s.recruitment_expenses == (accepted ? b.Simulation.Rules.intake.cost : 0), "Daily recruitment expense mismatch");
                if (accepted)
                {
                    var cat = s.cats.Last(); Check(!cat.working && cat.health == "healthy" && cat.stamina == b.Simulation.Rules.max_stamina && cat.fatigue == 0 && cat.stress == 0 && cat.activity == "cafe", "Invalid joined cat");
                    Check(b.View().cats.Last().name == "ナナ", "Joined profile missing");
                }
                Resume(b, 3); string after = Capture(b); Good(b, "resolve_intake", choice); Check(Capture(b) == after, "Duplicate answer changed gameplay"); Reject(b, "resolve_intake", accepted ? "decline" : "accept");
                Good(b, "set_shifts", working: s.cats.Select(c => c.id).ToArray()); Good(b, "start_business");
                while (b.Simulation.State.phase != "closed") { if (b.Simulation.State.tick == 11) Resume(b, 1); Good(b, "advance_business"); }
                Resume(b, 1); Good(b, "next_day"); Resume(b, 4);
                Check(b.Simulation.State.history[3].recruitment_expenses == (accepted ? b.Simulation.Rules.intake.cost : 0) && b.Simulation.State.recruitment_expenses == 0, "Recruitment expense repeated next day");
                var fresh = new LocalCafeBackend(path); Good(fresh, "load_game", slot: 4); Check(Capture(fresh) == Capture(b), "Fresh backend lost intake state");
            }
            // Exact-cost funds must reject acceptance without mutating state; decline remains available.
            var low = new LocalCafeBackend(Path.Combine(directory, "low")); var rules = low.Simulation.Rules;
            while (low.Simulation.State.day < 4) { if (low.Simulation.StoreEventWaiting) Good(low, "resolve_store_event", low.Simulation.CurrentStoreEvent.type == "trouble" ? "patch" : "decline"); Good(low, "day_off"); }
            if (low.Simulation.StoreEventWaiting) Good(low, "resolve_store_event", low.Simulation.CurrentStoreEvent.type == "trouble" ? "patch" : "decline");
            var returning = low.Simulation.State.cats[0]; returning.activity = "missing"; returning.missing_remaining = 0;
            Check(!low.View().intake_request.can_accept && !low.View().intake_request.can_decline, "Return must be resolved first");
            Reject(low, "resolve_intake", "accept");
            var returnCommand = new PythonShiftCommand { request_id = Guid.NewGuid().ToString("N"), instance_id = low.Simulation.State.instance_id, expected_revision = low.Simulation.State.revision, kind = "resolve_missing", cat_id = returning.id };
            CafeResponse returnReply = null; var returnRequest = low.Request("/commands", JsonUtility.ToJson(returnCommand), result => returnReply = result); while (returnRequest.MoveNext()) { }
            Check(returnReply.result == UnityWebRequest.Result.Success && low.View().intake_request.can_decline, "Cannot resolve return while intake waits");
            low.Simulation.State.funds = rules.intake.cost; Check(!low.View().intake_request.can_accept && low.View().intake_request.can_decline, "Insufficient-funds projection"); Reject(low, "resolve_intake", "accept"); Good(low, "resolve_intake", "decline");
            return checks;
        }
    }
}
