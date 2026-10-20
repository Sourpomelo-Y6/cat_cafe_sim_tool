using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;

namespace CatCafePreview
{
    // Isolated accelerated fixtures. Production Resources/rules is never changed.
    public sealed class StandaloneTrustConversationSmoke : MonoBehaviour
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
        public static void FinishAll(LocalCafeBackend b){foreach(var row in b.TrustConversations){while(row.Current!=null && !row.Current.completed && row.Current.branch_id!="introduction"){row.Current.node=3;row.Current.page_offset=0;CafeTrustDialogue.Finish(row);}}b.RememberViewedConversations();}
        public static CafeRules FixtureRules(string outcome="normal"){
            var r=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);r.starting_funds=20000;r.growth.threshold=10000;r.intake.day=r.regular_introduction.day=9999;r.arrival_ticks=new[]{0,0};r.queue_capacity=1;r.patterns=new[]{new WeekPattern{days=new[]{0,2}},new WeekPattern{days=Enumerable.Range(0,7).ToArray()}};r.discontent.waiting_gain=100;r.discontent.suspension_days=1;r.store_events.probability=0;r.satisfaction.satisfied_score=outcome=="recovered" || outcome=="repeat"?1:5;r.satisfaction.dissatisfied_score=outcome=="departed"?4:0;return r;
        }
        public static int Run(string directory)
        {
            checks = 0;
            foreach (string outcome in new[] { "ignore", "recovered", "departed", "normal", "unanswered", "repeat" })
            {
                var rules = JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
                rules.starting_funds=20000; rules.growth.threshold=10000; rules.intake.day=rules.regular_introduction.day=9999;
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
                if(File.Exists(Path.Combine(path,"slot-6.json"))){Good(b,"load_game",slot:6);Check(b.TrustResultPending,"Restart lost unread service result");}
                if(File.Exists(Path.Combine(path,"slot-9.json"))){Good(b,"load_game",slot:9);Check(b.TrustConversation.Current.node==1 && b.TrustResultPending,"Restart lost partial response result");}
                Good(b, "new_game", "free"); Reject(b, "resolve_customer_trust", "recover", "unknown");
                bool warned = false, recovering = false;
                for (int day = 1; day <= (outcome == "repeat" ? 6 : 5); day++)
                {
                    var s = b.Simulation.State;
                    foreach (var c in s.cats.Where(c => c.activity == "missing" && c.missing_remaining == 0).ToArray()) Good(b, "resolve_missing", cat: c.id);
                    foreach (var e in s.trust_events.Where(e => e.status == "waiting").ToArray())
                    {
                        warned = true; Check(b.BeginTrustConversation(e.id),"Warning ADV absent");b.TrustConversation.response.node=1;b.TrustConversation.response.page_offset=2; Resume(b, 2); Check(b.TrustConversation.Current.node==1 && b.TrustConversation.Current.page_offset==2,"Intro position lost"); Check(b.View().customer_trust.events.Single(v => v.event_id == e.id).can_respond, "Warning projection disabled");
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
                        FinishAll(b);Check(!b.TrustResultPending,"Read response blocks"); recovering |= choice == "recover";
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
                        Check(!b.TrustBlocksProgress,"Mid-service trust blocks remaining seats/closing");Good(b, "advance_business");
                    }
                    if(b.TrustResultPending){Check(b.TrustSafe,"Unsafe closed result");Resume(b,6);Check(Send(b,"next_day").result!=UnityWebRequest.Result.Success,"Unread result advanced date");FinishAll(b);}
                    if ((outcome == "recovered" || outcome == "departed") && b.Simulation.State.trust_events.Any(e => e.status == "resolved")) break;
                    Good(b, "next_day");
                }
                Check(warned, "Fixture never produced warning"); Resume(b, 4);
                var row = b.Simulation.State.trust_customers.Single(r => r.customer_id == "guest-2");
                string expected = outcome == "ignore" || outcome == "departed" ? "departed" : outcome == "recovered" ? "stable" : outcome == "normal" ? "recovery" : "waiting";
                Check(row.status == expected, "Wrong outcome " + outcome + ": " + row.status);
                if (outcome == "repeat") Check(b.Simulation.State.trust_events.Count >= 2, "No repeated warning after recovery");
                if (row.status == "departed") Check(!b.Simulation.CustomerSchedule(b.Simulation.State.day + 14).Any(a => a.customer_id == row.customer_id), "Departed customer returned");
                // Old saves preserve state and never infer viewed history.
                foreach(int slot in (outcome=="unanswered"?new[]{2,4}:new[]{2,3,4})){
                    var old=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(path,"slot-"+slot+".json")));old.slot=8;old.trust_conversations=null;old.trust_conversation_selected=null;File.WriteAllText(Path.Combine(path,"slot-8.json"),JsonUtility.ToJson(old));
                    var legacy=new LocalCafeBackend(path,rules);Good(legacy,"load_game",slot:8);Check(!legacy.TrustResultPending,"Legacy fabricated unread result");Check(legacy.TrustConversations.All(x=>x.response==null || x.response.branch_id=="introduction"),"Legacy fabricated old response");
                    Check(legacy.TrustConversations.All(x=>x.result==null),"Legacy fabricated old outcome");
                }
                var fresh = new LocalCafeBackend(path, rules); Good(fresh, "load_game", slot: 4); Check(Capture(fresh) == Capture(b), "Fresh backend changed trust result");
            }
            var multiRules=FixtureRules();multiRules.arrival_ticks=new[]{0,0,0,0};var multi=new LocalCafeBackend(Path.Combine(directory,"multiple"),multiRules);
            while(!multi.Simulation.CustomerTrustWaiting && multi.Simulation.State.day<8){multi.Simulation.Execute("start_business");while(multi.Simulation.State.phase!="closed")multi.Simulation.Execute("advance_business");multi.Simulation.Execute("next_day");multi.View();}
            var pending=multi.Simulation.State.trust_events.Where(e=>e.status=="waiting").ToArray();Check(pending.Length>=2,"Multiple warnings not produced");
            foreach(var e in pending){Check(multi.BeginTrustConversation(e.id),"Multiple row unavailable");string answer=e==pending[0]?"recover":"ignore";double popularity=multi.Simulation.State.popularity;Good(multi,"resolve_customer_trust",answer,e.id);Check(multi.TrustConversation.event_id==e.id && multi.TrustConversation.Current.branch_id==answer,"Multiple answers mixed");Check(multi.Simulation.State.popularity==popularity-(answer=="ignore"?multiRules.trust.popularity_loss:0),"Multiple loss differs");FinishAll(multi);}
            Check(multi.TrustConversations.Select(r=>r.event_id).Distinct().Count()==pending.Length,"Multiple rows merged");Resume(multi,7);
            var poor=new LocalCafeBackend(Path.Combine(directory,"normal"),FixtureRules());Good(poor,"load_game",slot:2);poor.Simulation.State.popularity=5;string poorId=poor.Simulation.State.trust_events.Last().id;Good(poor,"resolve_customer_trust","ignore",poorId);Check(poor.Simulation.State.popularity==0 && poor.Simulation.State.game_over=="popularity" && poor.OutcomePending && poor.TrustResultPending,"Popularity ending bypassed trust result");Resume(poor,7);string fixedState=Capture(poor);FinishAll(poor);Check(poor.OutcomePending && !poor.TrustResultPending && fixedState==Capture(poor),"Viewing repeated popularity/end judgment");
            // Validate display metadata before replacing live state.
            var bad=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(directory,"normal/slot-7.json")));bad.slot=8;bad.trust_conversations[0].response.branch_id="forged";File.WriteAllText(Path.Combine(directory,"normal/slot-8.json"),JsonUtility.ToJson(bad));
            string live=Capture(poor);Check(Send(poor,"load_game",slot:8).result!=UnityWebRequest.Result.Success && Capture(poor)==live,"Forged conversation replaced live state");
            return checks;
        }
        string output;LocalCafeBackend backend;
        static readonly System.Reflection.BindingFlags Hidden=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--catcafe-trust-adv-check");if(i<0)return;var q=new GameObject("Trust ADV QA").AddComponent<StandaloneTrustConversationSmoke>();DontDestroyOnLoad(q);q.output=Path.GetFullPath(a[i+1]);Directory.CreateDirectory(q.output);Application.runInBackground=true;q.StartCoroutine(q.Guard());}
        void Apply(PythonStateClient c){typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(c,new object[]{backend.View()});}
        System.Collections.IEnumerator Guard(){var run=Player();while(true){bool next;try{next=run.MoveNext();}catch(Exception ex){File.WriteAllText(Path.Combine(output,"failure.txt"),ex.ToString());Debug.LogException(ex);yield break;}if(!next)yield break;yield return run.Current;}}
        System.Collections.IEnumerator Player(){
            yield return new WaitForSecondsRealtime(1);int n=Run(Path.Combine(output,"backend"));
            backend=new LocalCafeBackend(Path.Combine(output,"backend/normal"),FixtureRules());Good(backend,"load_game",slot:2);CafeBackends.UseLocal(backend);UnityEngine.SceneManagement.SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
            var c=FindObjectOfType<PythonStateClient>();Apply(c);yield return null;var ui=c.GetComponent<TrustConversationPanel>();var owner=c.GetComponent<PythonCustomerTrustPanel>();Check(ui.Showing,"Trust UI absent");ui.Close();c.GetComponent<PythonEventListPanel>().ShowMenu(9);yield return null;Check(ui.Showing,"List resume failed");
            backend.TrustConversation.Current.node=3;backend.TrustConversation.Current.page_offset=0;Apply(c);yield return null;
            string before=JsonUtility.ToJson(backend.Simulation.State),meta=JsonUtility.ToJson(backend.TrustConversation);ui.Answer(false);yield return null;Check(owner.Confirming && owner.confirmation.activeInHierarchy,"Final confirmation absent");owner.Cancel();yield return null;Check(!owner.Confirming && before==JsonUtility.ToJson(backend.Simulation.State) && meta==JsonUtility.ToJson(backend.TrustConversation),"Cancel changed trust");
            ui.Answer(false);yield return null;owner.Confirm();owner.Confirm();yield return new WaitForSecondsRealtime(1);Check(backend.TrustConversation.response.branch_id=="ignore" && backend.TrustResultPending,"Ignore result absent");Check(backend.Simulation.State.popularity==Math.Max(0,JsonUtility.FromJson<CafeSnapshot>(before).popularity-backend.Simulation.Rules.trust.popularity_loss),"Repeated loss");
            ui.Close();c.GetComponent<PythonEventListPanel>().ShowMenu(9);yield return null;Check(ui.Showing,"Result list resume failed");backend.TrustConversation.Current.node=1;Good(backend,"save_game",slot:9);backend=new LocalCafeBackend(Path.Combine(output,"backend/normal"),FixtureRules());Good(backend,"load_game",slot:9);CafeBackends.UseLocal(backend);Apply(c);yield return null;Check(ui.Showing && backend.TrustConversation.Current.node==1,"Result reload failed");FinishAll(backend);ui.Close();Apply(c);yield return null;
            Good(backend,"load_game",slot:7);Apply(c);yield return null;Check(ui.Showing && backend.OutcomePending && !c.GetComponent<OutcomeConversationPanel>().Showing,"Trust must precede business ending");
            var growthView=backend.View();growthView.growth_choices=new[]{new PythonGrowthChoice()};c.GetComponent<PythonGrowthPanel>().Apply(growthView);Check(!c.GetComponent<PythonGrowthPanel>().panel.activeSelf,"Growth overlaid trust result");
            backend.TrustConversation.Current.node=3;Apply(c);yield return null;ui.Next();yield return null;Check(!backend.TrustResultPending && c.GetComponent<OutcomeConversationPanel>().Showing,"Business ending not resumed after trust");
            UnityEngine.SceneManagement.SceneManager.LoadScene("CatCafeTitle");yield return new WaitForSecondsRealtime(2);var extra=FindObjectOfType<PythonTitleScreen>().GetComponent<TitleExtraPanel>();foreach(string outcome in new[]{"recovered","departed"})backend.Replays.Merge(new LocalCafeBackend(Path.Combine(output,"backend/"+outcome),FixtureRules(outcome)).Replays.Load().entries);extra.Open();yield return null;before=JsonUtility.ToJson(backend.Simulation.State);meta=JsonUtility.ToJson(backend.TrustConversations);foreach(string branch in new[]{"response/recover","response/ignore","result/recovered","result/departed"}){extra.OpenReplay(CafeTrustDialogue.Id+"."+branch);yield return null;Check(extra.Replay.Showing,"Extra absent");extra.Replay.Answer(true);extra.Replay.Next();extra.Replay.Close();}Check(before==JsonUtility.ToJson(backend.Simulation.State) && meta==JsonUtility.ToJson(backend.TrustConversations),"Extra mutated state");extra.Hide();
            // Leave a reviewable choice screen for native mouse verification.
            backend=new LocalCafeBackend(Path.Combine(output,"backend/normal"),FixtureRules());Good(backend,"load_game",slot:Environment.GetCommandLineArgs().Contains("--trust-manual-load")?10:2);backend.PrepareTrustConversation();if(!Environment.GetCommandLineArgs().Contains("--trust-manual-load")){backend.TrustConversation.Current.node=3;backend.TrustConversation.Current.page_offset=0;}CafeBackends.UseLocal(backend);Screen.SetResolution(Environment.GetCommandLineArgs().Contains("--trust-restart")?1024:1280,Environment.GetCommandLineArgs().Contains("--trust-restart")?768:800,false);UnityEngine.SceneManagement.SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);c=FindObjectOfType<PythonStateClient>();Apply(c);yield return null;File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" trust ADV checks; backend="+n);ScreenCapture.CaptureScreenshot(Path.Combine(output,"manual-ready.png"));
        }
    }
}
