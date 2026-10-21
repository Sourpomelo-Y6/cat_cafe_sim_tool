using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneRegularIntroductionSmoke
    {
        private static int checks;
        private static void Check(bool value, string message) { checks++; if (!value) throw new Exception("Regular introduction: " + message); }
        private static CafeResponse Send(LocalCafeBackend b, string kind, string choice = null, int slot = 1, string[] working = null, string catId = null)
        {
            var command = new PythonShiftCommand { request_id = Guid.NewGuid().ToString("N"), instance_id = b.Simulation.State.instance_id, expected_revision = b.Simulation.State.revision,
                kind = kind, choice = choice, save_slot = slot, save_id = slot.ToString(), working_cats = working ?? new string[0], cat_id = catId ?? (kind == "resolve_regular_introduction" ? b.Simulation.Rules.regular_introduction.cat.id : "") };
            CafeResponse response = null; var request = b.Request("/commands", JsonUtility.ToJson(command), result => response = result); while (request.MoveNext()) { } return response;
        }
        private static void Good(LocalCafeBackend b, string kind, string choice = null, int slot = 1, string[] working = null)
        { var reply = Send(b, kind, choice, slot, working); Check(reply.result == UnityWebRequest.Result.Success, kind + ": " + reply.error); }
        private static string Capture(LocalCafeBackend b)
        {
            var s = JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State)); s.NormalizeJsonStrings(); s.instance_id = ""; s.revision = 0; return JsonUtility.ToJson(s);
        }
        private static void Resume(LocalCafeBackend b, int slot)
        { string before = Capture(b); Good(b, "save_game", slot: slot); Good(b, "load_game", slot: slot); Check(Capture(b) == before, "Resume changed gameplay"); }
        private static void Reject(LocalCafeBackend b, string kind, string choice = null, string catId = null)
        { string before = JsonUtility.ToJson(b.Simulation.State); Check(Send(b, kind, choice, catId: catId).result == UnityWebRequest.Result.ProtocolError, "Accepted invalid " + kind); Check(before == JsonUtility.ToJson(b.Simulation.State), "Rejected command mutated state"); }
        public static int Run(string directory)
        {
            checks = 0;
            foreach (string choice in new[] { "accept", "decline" })
            {
                string path = Path.Combine(directory, choice); var b = new LocalCafeBackend(path);
                if (File.Exists(Path.Combine(path, "slot-4.json")))
                {
                    Good(b, "load_game", slot: 2); Check(b.Simulation.RegularIntroductionWaiting && b.View().regular_introduction.status == "waiting", "Restart lost pending introduction");
                    Good(b, "load_game", slot: 3); string answered = Capture(b); Good(b, "resolve_regular_introduction", choice); Check(Capture(b) == answered, "Restart duplicated introduction");
                    Good(b, "load_game", slot: 4); Check(b.Simulation.State.recruitment_expenses == 0, "Restart repeated recruitment fee");
                }
                Good(b, "new_game", "free"); Reject(b, "resolve_regular_introduction", choice);
                while (!b.Simulation.RegularIntroductionWaiting)
                {
                    Check(b.Simulation.State.day <= 14, "Introduction failed to trigger");
                    if (b.Simulation.StoreEventWaiting) Good(b, "resolve_store_event", b.Simulation.CurrentStoreEvent.type == "trouble" ? "patch" : "decline");
                    if (b.Simulation.IntakeWaiting) Good(b, "resolve_intake", "decline");
                    var s = b.Simulation.State;
                    Good(b, "set_shifts", working: s.cats.Where((c, i) => c.health == "healthy" && c.activity == "cafe" && (i + s.day) % 3 != 0).Select(c => c.id).ToArray());
                    Good(b, "start_business"); while (b.Simulation.State.phase != "closed") Good(b, "advance_business");
                    Check(b.Simulation.State.regular_introduction.status == "untriggered", "Introduction presented during business");
                    foreach (var c in b.Simulation.State.cats.Where(c => c.activity == "missing" && c.missing_remaining == 0).ToArray())
                        Check(Send(b, "resolve_missing", catId: c.id).result == UnityWebRequest.Result.Success, "Return failed");
                    Good(b, "next_day");
                }
                if (b.Simulation.StoreEventWaiting) Good(b, "resolve_store_event", b.Simulation.CurrentStoreEvent.type == "trouble" ? "patch" : "decline");
                Resume(b, 2); var view = b.View().regular_introduction;
                Check(view.can_accept && view.can_decline && view.name == "マロン" && view.features == "茶トラ・短毛", "Introduction profile projection mismatch");
                Check(b.Simulation.State.regular_introduction.presented_day == b.Simulation.State.regular_introduction.regular_day + 1, "Incorrect presentation date");
                Reject(b, "start_business"); Reject(b, "day_off"); Reject(b, "set_shifts"); Reject(b, "resolve_regular_introduction", "invalid"); Reject(b, "resolve_regular_introduction", choice, "wrong-cat");
                string pending = Capture(b); var returning = b.Simulation.State.cats[0]; returning.activity = "missing"; returning.missing_remaining = 0;
                Check(!b.View().regular_introduction.can_decline, "Return did not gate response"); Reject(b, "resolve_regular_introduction", choice);
                Check(Send(b, "resolve_missing", choice: b.View().missing_cats.Single(e=>e.cat_id==returning.id && e.status=="waiting").choice, catId: returning.id).result == UnityWebRequest.Result.Success && b.View().regular_introduction.can_decline, "Return and introduction deadlock");
                Good(b, "load_game", slot: 2); Check(Capture(b) == pending, "Pending restore mismatch");
                double funds = b.Simulation.State.funds; b.Simulation.State.funds = b.Simulation.Rules.regular_introduction.cost;
                Check(!b.View().regular_introduction.can_accept && b.View().regular_introduction.can_decline, "Poor customer offer cannot be declined"); Reject(b, "resolve_regular_introduction", "accept"); b.Simulation.State.funds = funds;
                Good(b, "resolve_regular_introduction", choice); bool accepted = choice == "accept";
                var state = b.Simulation.State;
                Check(state.cats.Count == (accepted ? 6 : 5) && state.funds == funds - (accepted ? b.Simulation.Rules.regular_introduction.cost : 0), "Introduction admission mismatch");
                if (accepted) { var cat = state.cats.Last(); Check(cat.id == view.cat_id && !cat.working && cat.health == "healthy" && cat.stamina == b.Simulation.Rules.max_stamina && cat.fatigue == 0 && cat.stress == 0, "Invalid joined cat"); }
                Resume(b, 3); string after = Capture(b); Good(b, "resolve_regular_introduction", choice); Check(Capture(b) == after, "Duplicate answer changed state"); Reject(b, "resolve_regular_introduction", accepted ? "decline" : "accept");
                Good(b, "set_shifts", working: b.Simulation.State.cats.Where(c => c.health == "healthy" && c.activity == "cafe").Select(c => c.id).ToArray());
                Good(b, "start_business"); while (b.Simulation.State.phase != "closed") { if (b.Simulation.State.tick == 11) Resume(b, 1); Good(b, "advance_business"); }
                Resume(b, 1); int presented = b.Simulation.State.regular_introduction.presented_day;
                foreach (var c in b.Simulation.State.cats.Where(c => c.activity == "missing" && c.missing_remaining == 0).ToArray()) Check(Send(b, "resolve_missing", catId: c.id).result == UnityWebRequest.Result.Success, "Return failed");
                Good(b, "next_day"); Resume(b, 4);
                Check(b.Simulation.State.recruitment_expenses == 0 && b.Simulation.State.history[presented - 1].recruitment_expenses == (accepted ? b.Simulation.Rules.regular_introduction.cost : 0), "History recruitment fee mismatch");
                var fresh = new LocalCafeBackend(path); Good(fresh, "load_game", slot: 4); Check(Capture(fresh) == Capture(b), "Fresh backend lost joined cat");
            }
            return checks;
        }
    }
}
