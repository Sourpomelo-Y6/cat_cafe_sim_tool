using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class StandaloneAdoptionConversationSmoke : MonoBehaviour
    {
        public static CafeRules Rules()
        {
            var r=StandaloneEncounterConversationSmoke.Rules();r.intake.day=r.regular_introduction.day=9999;r.starting_funds=20000;r.growth.threshold=10000;r.exchange.ticks=1;r.exchange.affinity_enthusiastic=r.exchange.affinity_favorable=80;r.opening_ticks=2;r.arrival_ticks=new[]{0,0};r.patterns=Enumerable.Range(0,7).Select(i=>new WeekPattern{days=Enumerable.Range(0,7).ToArray()}).ToArray();r.max_probability=r.stress_per_service_tick=r.fatigue_per_service_tick=0;return r;
        }
        public static CafeResponse Send(LocalCafeBackend b,string kind,string choice=null,string id=null,int slot=1)
        {
            string cat=b.Simulation.State.adoption?.events.FirstOrDefault(e=>e.id==id)?.cat_id;
            return StandaloneDispatchConversationSmoke.Raw(b,JsonUtility.ToJson(new PythonShiftCommand{kind=kind,choice=choice,event_id=id,cat_id=cat,save_id=slot.ToString(),save_slot=slot,working_cats=new string[0],request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision}));
        }
        public static void Good(LocalCafeBackend b,string kind,string choice=null,string id=null,int slot=1){var v=Send(b,kind,choice,id,slot);if(v.result!=UnityWebRequest.Result.Success)throw new Exception(kind+": "+v.error);}
        public static string Setup(LocalCafeBackend b,string mode="free"){Good(b,"new_game",mode);Good(b,"configure_adoption","on");Good(b,"start_business");Good(b,"advance_business");string id=b.Simulation.State.adoption.events.First().id;if(!b.BeginAdoptionConversation(id))throw new Exception("No adoption conversation");return id;}
        public static void Finish(LocalCafeBackend b){var row=b.AdoptionConversation;row.progress.node=3;row.progress.page_offset=0;CafeAdoptionDialogue.Finish(row);b.RememberViewedConversations();}
        public static int BackendChecks(string root,bool restart)
        {
            int n=0;Action<bool,string> ck=(ok,label)=>{n++;if(!ok)throw new Exception(label);};
            foreach(string mode in new[]{"free","popularity","bond","patron"})foreach(string branch in new[]{"accept","decline"})
            {
                string path=Path.Combine(root,mode+"-"+branch);var b=new LocalCafeBackend(path,Rules());
                if(restart)foreach(int slot in new[]{1,2,3,4}){Good(b,"load_game",slot:slot);ck(b.AdoptionConversations.Length>0,"Restart metadata missing");}
                string priorExtra=JsonUtility.ToJson(b.Replays.Load());string id=Setup(b,mode);ck(b.Simulation.State.adoption.events.Count==2,"Multiple offers absent");
                var row=b.AdoptionConversation;row.progress.node=1;row.progress.page_offset=3;Good(b,"save_game",slot:1);Good(b,"load_game",slot:1);ck(b.AdoptionConversation.progress.node==1 && b.AdoptionConversation.progress.page_offset==3,"Intro resume lost");
                ck(Send(b,"next_day").result!=UnityWebRequest.Result.Success && b.ConversationProgressReason.Contains("譲渡"),"Unanswered advanced without reason");
                var e=b.Simulation.State.adoption.events.Single(x=>x.id==id);double funds=b.Simulation.State.funds;string relations=string.Join("|",b.Simulation.State.relationships.Select(r=>r.cat_id+"/"+r.customer_id+"/"+r.affinity));
                Good(b,"resolve_adoption",branch,id);ck(b.AdoptionConversation.progress.branch_id==branch && b.AdoptionResultPending,"Missing result");
                string snapshot=JsonUtility.ToJson(b.Simulation.State);ck(Send(b,"resolve_adoption",branch,id).result!=UnityWebRequest.Result.Success && snapshot==JsonUtility.ToJson(b.Simulation.State),"Duplicate changed state");
                ck(b.Simulation.State.funds==funds && relations==string.Join("|",b.Simulation.State.relationships.Select(r=>r.cat_id+"/"+r.customer_id+"/"+r.affinity)),"Money or relations changed");
                b.AdoptionConversation.progress.node=1;Good(b,"save_game",slot:2);Good(b,"load_game",slot:2);ck(b.AdoptionResultPending && b.AdoptionConversation.progress.node==1,"Result resume lost");
                ck(priorExtra==JsonUtility.ToJson(b.Replays.Load()),"Unread Extra changed unlocks");Finish(b);
                ck(b.Replays.Load().entries.Any(x=>x.conversation_id==CafeAdoptionDialogue.Id && x.branch_id==branch),"Completed Extra absent");
                var replay=b.Replays.Load().entries.Single(x=>x.conversation_id==CafeAdoptionDialogue.Id && x.branch_id==branch);ck(replay.cat_name==b.Simulation.Definition(e.cat_id).name && replay.lines.Any(l=>l.text.Contains(b.Details.CustomerName(e.customer_id))),"Replay identity mixed");
                var other=b.Simulation.State.adoption.events.Single(x=>x.id!=id);ck(b.BeginAdoptionConversation(other.id),"Second inaccessible");Good(b,"resolve_adoption","decline",other.id);Finish(b);ck(!b.AdoptionResultPending && !b.Simulation.AdoptionWaiting,"Second still pending");
                ck(b.Simulation.ActiveCatCount(b.Simulation.State)==(branch=="accept"?4:5),"Housing roster mismatch");Good(b,"save_game",slot:3);
                // Metadata-free saves retain answers but never fabricate observations.
                foreach(int slot in new[]{1,3})
                {
                    var save=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(path,"slot-"+slot+".json")));save.slot=6;save.adoption_conversations=null;save.adoption_conversation_selected=null;File.WriteAllText(Path.Combine(path,"slot-6.json"),JsonUtility.ToJson(save));Good(b,"load_game",slot:6);
                    ck(b.BeginAdoptionConversation(id)==(slot==1),"Legacy waiting/resolved confused");ck(!b.AdoptionResultPending,"Legacy fabricated unread result");
                }
                Good(b,"load_game",slot:2);var bad=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(path,"slot-2.json")));bad.slot=7;bad.adoption_conversations[0].progress.branch_id=branch=="accept"?"decline":"accept";File.WriteAllText(Path.Combine(path,"slot-7.json"),JsonUtility.ToJson(bad));snapshot=JsonUtility.ToJson(b.Simulation.State);ck(Send(b,"load_game",slot:7).result!=UnityWebRequest.Result.Success && snapshot==JsonUtility.ToJson(b.Simulation.State),"Forged branch accepted");
                Good(b,"load_game",slot:3);Good(b,"next_day");Good(b,"start_business");Good(b,"advance_business");var next=b.Simulation.State.adoption.events.FirstOrDefault(x=>x.status=="waiting");ck(next!=null && next.id!=id && next.id!=other.id,"Next day offer missing");ck(b.BeginAdoptionConversation(next.id) && b.AdoptionConversation.progress.node==0,"Next day reused progress");Good(b,"save_game",slot:4);
            }
            var overlapRules=Rules();overlapRules.growth.threshold=1;overlapRules.goal_stages=new[]{new GoalStageRule{days=1,target=1}};
            var overlap=new LocalCafeBackend(Path.Combine(root,"overlap"),overlapRules);Setup(overlap,"popularity");
            ck(overlap.Simulation.GrowthWaiting,"Growth overlap absent");
            foreach(var offer in overlap.Simulation.State.adoption.events.ToArray()){ck(overlap.BeginAdoptionConversation(offer.id),"Overlap offer absent");Good(overlap,"resolve_adoption","decline",offer.id);Finish(overlap);}
            ck(overlap.Simulation.GrowthWaiting && !overlap.AdoptionResultPending,"Adoption consumed growth");
            while(overlap.View().growth_choices.Length>0){var g=overlap.View().growth_choices[0];StandaloneLodgeConversationSmoke.Good(overlap,g.kind,g.choices[0].choice,cat:g.cat_id);}
            ck(overlap.Simulation.GoalWaiting,"Goal overlap absent");ck(overlap.PopularityEndingPending && !overlap.AdoptionResultPending,"Adoption consumed ending");
            ck(Send(overlap,"next_day").result!=UnityWebRequest.Result.Success,"Unread ending advanced");
            return n;
        }
        string output;LocalCafeBackend backend;int checks;static readonly BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install(){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--catcafe-adoption-adv-check");if(i<0)return;var q=new GameObject("Adoption ADV QA").AddComponent<StandaloneAdoptionConversationSmoke>();DontDestroyOnLoad(q);q.output=Path.GetFullPath(a[i+1]);Directory.CreateDirectory(q.output);Application.runInBackground=true;q.StartCoroutine(q.Guard());}
        void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        void Apply(PythonStateClient c){typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(c,new object[]{backend.View()});Canvas.ForceUpdateCanvases();}
        IEnumerator Guard(){var run=Run();while(true){bool more;try{more=run.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}if(!more)yield break;yield return run.Current;}}
        IEnumerator Run()
        {
            checks+=BackendChecks(Path.Combine(output,"cases"),Environment.GetCommandLineArgs().Contains("--adoption-restart"));
            Screen.SetResolution(1280,800,false);backend=new LocalCafeBackend(Path.Combine(output,"ui"),Rules());CafeBackends.UseLocal(backend);SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);var c=FindObjectOfType<PythonStateClient>();string id=Setup(backend);Apply(c);yield return null;var ui=c.GetComponent<AdoptionConversationPanel>();var original=c.GetComponent<PythonAdoptionPanel>();Check(ui.Showing,"UI absent");
            ui.Close();c.GetComponent<PythonEventListPanel>().ShowMenu(5);yield return null;Check(ui.Showing,"Intro cannot resume");backend.AdoptionConversation.progress.node=4;Apply(c);yield return null;
            string snapshot=JsonUtility.ToJson(backend.Simulation.State),metadata=JsonUtility.ToJson(backend.AdoptionConversation);ui.Answer(true);yield return null;Check(original.Confirming && original.confirmation.activeInHierarchy,"Confirmation absent");original.CancelAccept();yield return null;Check(!original.Confirming && snapshot==JsonUtility.ToJson(backend.Simulation.State) && metadata==JsonUtility.ToJson(backend.AdoptionConversation),"Cancel mutated answer");
            ui.Answer(true);yield return null;original.ConfirmAccept();original.ConfirmAccept();yield return new WaitForSecondsRealtime(1);Check(backend.AdoptionConversation.progress.branch_id=="accept" && backend.AdoptionResultPending,"Confirm failed");Check(ui.Panel.GetComponentsInChildren<Text>().Any(t=>t.text.Contains(backend.Simulation.Definition(backend.Simulation.State.adoption.events.Single(e=>e.id==id).cat_id).name)),"Adopted name absent");
            ui.Close();c.GetComponent<PythonEventListPanel>().ShowMenu(5);yield return null;Check(ui.Showing && !c.CanDayOff,"Result reopen failed");backend.AdoptionConversation.progress.node=1;Good(backend,"save_game",slot:8);backend=new LocalCafeBackend(Path.Combine(output,"ui"),Rules());Good(backend,"load_game",slot:8);CafeBackends.UseLocal(backend);Apply(c);yield return null;Check(backend.AdoptionConversation.progress.node==1 && ui.Showing,"UI restart failed");
            Finish(backend);ui.Close();Apply(c);yield return null;var other=backend.Simulation.State.adoption.events.Single(e=>e.status=="waiting");Check(ui.OpenEvent(other.id),"Other UI inaccessible");backend.AdoptionConversation.progress.node=4;Apply(c);yield return null;ui.Answer(false);ui.Answer(true);yield return new WaitForSecondsRealtime(1);Check(backend.AdoptionConversation.progress.branch_id=="decline" && !original.Confirming,"Decline repeated click changed choice");Finish(backend);ui.Close();Apply(c);yield return null;
            SceneManager.LoadScene("CatCafeTitle");yield return new WaitForSecondsRealtime(2);var extra=FindObjectOfType<PythonTitleScreen>().GetComponent<TitleExtraPanel>();extra.Open();yield return null;snapshot=JsonUtility.ToJson(backend.Simulation.State);string all=JsonUtility.ToJson(backend.AdoptionConversations);foreach(string branch in new[]{"accept","decline"}){extra.OpenReplay(CafeAdoptionDialogue.Id+"/"+branch);yield return null;Check(extra.Replay.Showing,"Extra absent");extra.Replay.Answer(true);extra.Replay.Next();extra.Replay.Close();}Check(snapshot==JsonUtility.ToJson(backend.Simulation.State) && all==JsonUtility.ToJson(backend.AdoptionConversations),"Extra changed gameplay");extra.Hide();SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);c=FindObjectOfType<PythonStateClient>();
            backend=new LocalCafeBackend(Path.Combine(output,"manual"),Rules());if(Environment.GetCommandLineArgs().Contains("--adoption-manual-load"))Good(backend,"load_game",slot:9);else{Setup(backend);backend.AdoptionConversation.progress.node=4;Good(backend,"save_game",slot:2);}Screen.SetResolution(Environment.GetCommandLineArgs().Contains("--adoption-restart")?1024:1280,Environment.GetCommandLineArgs().Contains("--adoption-restart")?768:800,false);CafeBackends.UseLocal(backend);Apply(c);yield return new WaitForSecondsRealtime(1);ScreenCapture.CaptureScreenshot(Path.Combine(output,"manual-ready.png"));File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" adoption ADV checks; restart="+Environment.GetCommandLineArgs().Contains("--adoption-restart"));
        }
    }
}
