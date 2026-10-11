using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;

namespace CatCafePreview
{
    public static class StandaloneGrowthSmoke
    {
        private static int checks;
        private static void Check(bool value, string message) { checks++; if (!value) throw new Exception("Growth: " + message); }
        private static CafeResponse Send(LocalCafeBackend b, string kind, string id = null, string choice = null, int slot = 1, string[] working = null)
        {
            var cmd = new PythonShiftCommand { request_id = Guid.NewGuid().ToString("N"), instance_id = b.Simulation.State.instance_id, expected_revision = b.Simulation.State.revision,
                kind = kind, cat_id = id, choice = choice, save_id = slot.ToString(), save_slot = slot, working_cats = working ?? new string[0] };
            CafeResponse reply = null; var request = b.Request("/commands", JsonUtility.ToJson(cmd), r => reply = r); while (request.MoveNext()) { } return reply;
        }
        private static void Good(LocalCafeBackend b, string kind, string id = null, string choice = null, int slot = 1, string[] working = null)
        { var reply = Send(b,kind,id,choice,slot,working); Check(reply.result == UnityWebRequest.Result.Success, kind + ": " + reply.error); }
        public static void ResolvePending(LocalCafeBackend b, string specialization = "service")
        {
            while (b.Simulation.GrowthWaiting)
            {
                var row = b.View().growth_choices[0]; string choice = row.kind == "resolve_growth" ? specialization : row.choices[0].choice;
                var reply = Send(b,row.kind,row.cat_id,choice);
                if (reply.result != UnityWebRequest.Result.Success) throw new Exception("Growth response failed: " + reply.error);
            }
        }
        private static string Capture(LocalCafeBackend b)
        { var s = JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State)); s.NormalizeJsonStrings(); s.instance_id = ""; s.revision = 0; return JsonUtility.ToJson(s); }
        private static void Resume(LocalCafeBackend b, int slot)
        { string before = Capture(b); Good(b,"save_game",slot:slot); Good(b,"load_game",slot:slot); Check(Capture(b)==before,"Save changed growth/active service"); }
        private static void Reject(LocalCafeBackend b, string kind, string id = null, string choice = null)
        { string before = JsonUtility.ToJson(b.Simulation.State); Check(Send(b,kind,id,choice).result==UnityWebRequest.Result.ProtocolError,"Accepted invalid "+kind); Check(before==JsonUtility.ToJson(b.Simulation.State),"Rejected operation changed state"); }
        public static int Run(string directory)
        {
            checks = 0; bool mastery = false, individual = false, second = false;
            foreach (string specialization in new[] { "service", "rest", "dispatch" })
            {
                var rules = JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
                rules.rules_id += "-growth-fixture-" + specialization; rules.growth.threshold = 2; rules.growth.mastery_threshold = 2;
                rules.growth.type_mastery_threshold = 2; rules.growth.second_mastery_threshold = 2; rules.growth.second_type_mastery_threshold = 2;
                rules.store_events.probability = 0;
                string path = Path.Combine(directory,specialization); var b = new LocalCafeBackend(path,rules);
                if (File.Exists(Path.Combine(path,"slot-4.json")))
                { Good(b,"load_game",slot:2); Check(b.Simulation.GrowthWaiting,"Restart lost pending growth"); Good(b,"load_game",slot:4); Check(b.Simulation.State.growth.Any(r=>r.specialization==specialization),"Restart lost specialization"); }
                Good(b,"new_game",choice:"free"); Reject(b,"resolve_growth",rules.cats[0].id,"service");
                bool savedPending = false;
                for (int day=1;day<=8;day++)
                {
                    ResolvePending(b,specialization);
                    if (b.Simulation.IntakeWaiting) Good(b,"resolve_intake",choice:"decline");
                    if (b.Simulation.RegularIntroductionWaiting) Good(b,"resolve_regular_introduction",rules.regular_introduction.cat.id,"decline");
                    foreach (var cat in b.Simulation.State.cats.Where(c=>c.activity=="missing" && c.missing_remaining==0).ToArray()) Good(b,"resolve_missing",cat.id);
                    var s=b.Simulation.State;
                    Good(b,"set_shifts",working:s.cats.Where((c,i)=>(i+s.day)%3!=0 && c.health=="healthy" && c.activity=="cafe").Select(c=>c.id).ToArray());
                    Good(b,"start_business");
                    while (true)
                    {
                        if (b.Simulation.GrowthWaiting)
                        {
                            if (!savedPending) { Resume(b,2); savedPending=true; }
                            else Resume(b,3);
                            Reject(b,"advance_business"); Reject(b,"day_off"); Reject(b,"set_shifts");
                            var option=b.View().growth_choices[0]; Reject(b,option.kind,option.cat_id,"invalid");
                            mastery |= option.kind=="resolve_growth_mastery"; individual |= option.kind=="resolve_growth_type_mastery";
                            ResolvePending(b,specialization); Resume(b,3);
                        }
                        if (b.Simulation.State.phase=="closed") break;
                        if (b.Simulation.State.tick==11) Resume(b,5);
                        Good(b,"advance_business");
                    }
                    second |= b.Simulation.State.growth.Any(r=>r.second_mastery!=null);
                    foreach (var cat in b.Simulation.State.cats.Where(c=>c.activity=="missing" && c.missing_remaining==0).ToArray()) Good(b,"resolve_missing",cat.id);
                    Good(b,"next_day");
                }
                Check(savedPending && b.Simulation.State.growth.Any(r=>r.specialization==specialization),"Fixture missed specialization"); Resume(b,4);
                string before=Capture(b); var fresh=new LocalCafeBackend(path,rules); Good(fresh,"load_game",slot:4); Check(Capture(fresh)==before,"Fresh backend lost growth");
                var forged=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State)); forged.NormalizeJsonStrings(); forged.growth[0].service++;
                bool failed=false; try { b.Simulation.Restore(forged); } catch(ArgumentException) { failed=true; }
                Check(failed && Capture(b)==before,"Forged experience accepted");
                forged=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State)); forged.NormalizeJsonStrings(); forged.growth_operations.Clear();
                failed=false;try { b.Simulation.Restore(forged); } catch(ArgumentException) { failed=true; }
                Check(failed && Capture(b)==before,"Missing growth replay accepted");
            }
            Check(mastery && individual && second,"Fixture missed mastery stages"); return checks;
        }
    }
}
