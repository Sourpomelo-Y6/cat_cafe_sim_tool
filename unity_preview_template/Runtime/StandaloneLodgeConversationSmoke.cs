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
    public sealed class StandaloneLodgeConversationSmoke : MonoBehaviour
    {
        public static CafeRules Rules(){var r=StandaloneEncounterConversationSmoke.Rules();r.intake.day=r.regular_introduction.day=9999;r.starting_funds=20000;r.dispatch_trouble.base_probability=1;r.dispatch_trouble.stress_probability=0;r.growth.threshold=10000;return r;}
        public static CafeResponse Send(LocalCafeBackend b,string kind,string choice=null,string id=null,string cat="cat-tama",int slot=1)=>StandaloneDispatchConversationSmoke.Raw(b,Payload(b,kind,choice,id,cat,slot));
        static string Payload(LocalCafeBackend b,string kind,string choice,string id,string cat,int slot)=>JsonUtility.ToJson(new PythonShiftCommand{kind=kind,choice=choice,event_id=id,cat_id=cat,save_id=slot.ToString(),save_slot=slot,request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision});
        public static void Good(LocalCafeBackend b,string kind,string choice=null,string id=null,string cat="cat-tama",int slot=1){var v=Send(b,kind,choice,id,cat,slot);if(v.result!=UnityWebRequest.Result.Success)throw new Exception(kind+": "+v.error);}
        public static string Setup(LocalCafeBackend b){Good(b,"new_game","free");Good(b,"dispatch_general","mountain_lodge_visit");Good(b,"day_off");var id=b.Simulation.State.dispatches.Single().id;if(!b.BeginLodgeConversation(id))throw new Exception("No lodge event");return id;}
        public static void Finish(LocalCafeBackend b){var r=b.LodgeConversation;var p=r.Current;p.node=CafeLodgeDialogue.Lines(ReferenceEquals(p,r.returning),p.branch_id,"").Length;p.page_offset=0;CafeLodgeDialogue.Finish(r);b.RememberViewedConversations();}
        public static int BackendChecks(string path,bool restart)
        {
            int n=0;Action<bool,string> ck=(ok,label)=>{n++;if(!ok)throw new Exception(label);};
            foreach(string branch in new[]{"search","wait"})
            {
                string folder=Path.Combine(path,branch);var b=new LocalCafeBackend(folder,Rules());
                if(restart)foreach(int slot in new[]{1,2,3,4,5}){Good(b,"load_game",slot:slot);ck(b.LodgeConversation!=null,"Restart metadata missing");}
                string id=Setup(b);b.LodgeConversation.Current.node=1;b.LodgeConversation.Current.page_offset=3;Good(b,"save_game",slot:1);Good(b,"load_game",slot:1);ck(b.LodgeConversation.Current.node==1 && b.LodgeConversation.Current.page_offset==3,"Intro page lost");
                b.LodgeConversation.Current.node=3;b.LodgeConversation.Current.page_offset=0;Good(b,"save_game",slot:2);ck(Send(b,"day_off").result!=UnityWebRequest.Result.Success,"Unanswered advanced");
                string payload=Payload(b,"resolve_dispatch_trouble",branch,id,"cat-tama",1);double funds=b.Simulation.State.funds;var answer=StandaloneDispatchConversationSmoke.Raw(b,payload);ck(answer.result==UnityWebRequest.Result.Success,"Answer rejected");var row=b.LodgeConversation;ck(row.Current.branch_id==branch && row.Current.node==0 && b.LodgeResultPending,"Answer result missing");string before=JsonUtility.ToJson(b.Simulation.State);
                ck(StandaloneDispatchConversationSmoke.Raw(b,payload).text==answer.text && before==JsonUtility.ToJson(b.Simulation.State),"Request replay mutated state");ck(Send(b,"resolve_dispatch_trouble",branch,id).result!=UnityWebRequest.Result.Success && before==JsonUtility.ToJson(b.Simulation.State),"Second answer charged");ck(b.Simulation.State.funds==funds-(branch=="search"?100:0),"Cost changed");
                row.Current.node=1;Good(b,"save_game",slot:3);var fresh=new LocalCafeBackend(folder,Rules());Good(fresh,"load_game",slot:3);ck(fresh.LodgeConversation.Current.node==1 && fresh.LodgeResultPending,"Result resume lost");ck(Send(fresh,"day_off").result!=UnityWebRequest.Result.Success && fresh.ConversationProgressReason.Contains("山あいの宿"),"Hidden result unblocked");
                Finish(b);ck(!b.LodgeResultPending,"Read answer still blocks");
                if(branch=="wait")for(int d=0;d<2;d++){Good(b,"day_off");ck(b.Simulation.State.dispatches.Single().status==(d==0?"missing":"waiting"),"Wait countdown changed");}
                ck(b.BeginLodgeConversation(id) && b.LodgeConversation.returning!=null,"Return not startable");b.LodgeConversation.Current.node=1;Good(b,"save_game",slot:4);Good(b,"receive_general",id);ck(b.LodgeConversation.Current.branch_id=="received","Receipt result missing");before=JsonUtility.ToJson(b.Simulation.State);ck(Send(b,"receive_general",id).result!=UnityWebRequest.Result.Success && before==JsonUtility.ToJson(b.Simulation.State),"Duplicate receipt changed state");Finish(b);Good(b,"save_game",slot:5);ck(b.Replays.Load().entries.Count(e=>e.conversation_id.StartsWith(CafeLodgeDialogue.Id))==2,"Extra branches missing");
                var save=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(folder,"slot-2.json")));save.slot=6;save.lodge_conversations=null;save.lodge_conversation_selected=null;File.WriteAllText(Path.Combine(folder,"slot-6.json"),JsonUtility.ToJson(save));Good(b,"load_game",slot:6);ck(b.BeginLodgeConversation(id) && b.LodgeConversation.Current.node==0,"Old waiting cannot start");
                save=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(folder,"slot-5.json")));save.slot=7;save.lodge_conversations=null;save.lodge_conversation_selected=null;File.WriteAllText(Path.Combine(folder,"slot-7.json"),JsonUtility.ToJson(save));Good(b,"load_game",slot:7);ck(!b.BeginLodgeConversation(id) && !b.LodgeResultPending,"Old resolved fabricated reading");
                save=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(folder,"slot-3.json")));save.slot=8;save.lodge_conversations[0].response.branch_id=branch=="search"?"wait":"search";File.WriteAllText(Path.Combine(folder,"slot-8.json"),JsonUtility.ToJson(save));before=JsonUtility.ToJson(b.Simulation.State);ck(Send(b,"load_game",slot:8).result!=UnityWebRequest.Result.Success && before==JsonUtility.ToJson(b.Simulation.State),"Forged branch replaced state");
            }
            // Boundary funds fixture adjusts its own starting funds, keeping the accounting valid.
            var poorRules=Rules();var probe=new LocalCafeBackend(Path.Combine(path,"probe"),poorRules);Setup(probe);double spent=poorRules.starting_funds-probe.Simulation.State.funds;poorRules.starting_funds=100+spent;
            var poor=new LocalCafeBackend(Path.Combine(path,"poor"),poorRules);string poorId=Setup(poor);ck(poor.Simulation.State.funds==100 && poor.Simulation.DispatchTroubleReason(poorId,"search")!="" && poor.Simulation.DispatchTroubleReason(poorId,"wait")=="","Funds boundary invalid");Good(poor,"resolve_dispatch_trouble","wait",poorId);Finish(poor);ck(!poor.ConversationBlocksProgress,"Free wait result blocks schedule");Good(poor,"save_game",slot:2);
            var multiRules=Rules();multiRules.cats.Single(c=>c.id=="cat-mike").trait_id="outgoing";
            var multi=new LocalCafeBackend(Path.Combine(path,"multiple"),multiRules);Good(multi,"new_game","free");Good(multi,"dispatch_general","mountain_lodge_visit");Good(multi,"dispatch_general","mountain_lodge_visit",cat:"cat-mike");Good(multi,"day_off");
            var ids=multi.Simulation.State.dispatches.Select(e=>e.id).ToArray();foreach(var id in ids){ck(multi.BeginLodgeConversation(id),"Parallel event inaccessible");Good(multi,"resolve_dispatch_trouble","search",id, multi.Simulation.State.dispatches.Single(e=>e.id==id).cat_id);Finish(multi);}
            ck(multi.LodgeConversations.Length==2 && multi.Simulation.DispatchWaiting,"Parallel records mixed");
            foreach(var id in ids){ck(multi.BeginLodgeConversation(id),"Parallel return inaccessible");Good(multi,"receive_general",id,cat:multi.Simulation.State.dispatches.Single(e=>e.id==id).cat_id);Finish(multi);}
            ck(!multi.LodgeResultPending && !multi.Simulation.DispatchWaiting,"Parallel return still blocks");Good(multi,"save_game",slot:1);Good(multi,"load_game",slot:1);ck(multi.LodgeConversations.Length==2,"Parallel records lost on load");
            Good(multi,"dispatch_general","mountain_lodge_visit");Good(multi,"day_off");string next=multi.Simulation.State.dispatches.Last().id;ck(!ids.Contains(next) && multi.BeginLodgeConversation(next) && multi.LodgeConversations.Length==3,"Same cat new dispatch reused record");
            // Normal business, in addition to the day-off countdown exercised above.
            var business=new LocalCafeBackend(Path.Combine(path,"business"),Rules());string bid=Setup(business);Good(business,"resolve_dispatch_trouble","wait",bid);Finish(business);Good(business,"start_business");while(business.Simulation.State.phase!="closed")Good(business,"advance_business");ck(business.Simulation.State.dispatches.Single().remaining==1,"Business failed to advance wait");
            var overlapRules=Rules();overlapRules.intake.day=2;overlapRules.growth.threshold=1;overlapRules.growth.rest_xp=1;
            var overlap=new LocalCafeBackend(Path.Combine(path,"overlap"),overlapRules);string oid=Setup(overlap);ck(overlap.Simulation.IntakeWaiting || overlap.Simulation.GrowthWaiting,"Overlap fixture empty");Good(overlap,"resolve_dispatch_trouble","wait",oid);Finish(overlap);ck(!overlap.LodgeResultPending && Send(overlap,"day_off").result!=UnityWebRequest.Result.Success,"Lodge completion cleared other event gate");
            var goalRules=Rules();goalRules.goal_stages=new[]{new GoalStageRule{days=1,target=99999}};var goal=new LocalCafeBackend(Path.Combine(path,"goal"),goalRules);Good(goal,"new_game","popularity");Good(goal,"dispatch_general","mountain_lodge_visit");Good(goal,"day_off");string gid=goal.Simulation.State.dispatches.Single().id;ck(goal.BeginLodgeConversation(gid) && goal.OutcomePending,"Goal overlap absent");Good(goal,"resolve_dispatch_trouble","wait",gid);Finish(goal);ck(goal.OutcomePending && !goal.LodgeResultPending && Send(goal,"day_off").result!=UnityWebRequest.Result.Success,"Lodge completion consumed outcome");
            return n;
        }
        string output;LocalCafeBackend backend;int checks;static readonly BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install(){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--catcafe-lodge-conversation-check");if(i<0)return;var q=new GameObject("Lodge ADV QA").AddComponent<StandaloneLodgeConversationSmoke>();DontDestroyOnLoad(q);q.output=Path.GetFullPath(a[i+1]);Directory.CreateDirectory(q.output);Application.runInBackground=true;q.StartCoroutine(q.Guard());}
        void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        void Apply(PythonStateClient c){typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(c,new object[]{backend.View()});Canvas.ForceUpdateCanvases();}
        IEnumerator Guard(){var run=Run();while(true){bool more;try{more=run.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}if(!more)yield break;yield return run.Current;}}
        IEnumerator Run()
        {
            checks+=BackendChecks(Path.Combine(output,"cases"),Environment.GetCommandLineArgs().Contains("--lodge-restart"));
            backend=new LocalCafeBackend(Path.Combine(output,"ui"),Rules());CafeBackends.UseLocal(backend);SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);var c=FindObjectOfType<PythonStateClient>();string id=Setup(backend);Apply(c);yield return null;var ui=c.GetComponent<LodgeConversationPanel>();Check(ui.Showing,"UI not open");backend.LodgeConversation.Current.node=3;Apply(c);yield return null;
            var buttons=ui.Panel.GetComponentsInChildren<Button>(true);Check(buttons.Single(b=>b.name=="迎える").interactable && buttons.Single(b=>b.name=="見送る").interactable,"Choices disabled");ui.Answer(false);ui.Answer(true);yield return new WaitForSecondsRealtime(1);Check(backend.LodgeConversation.Current.branch_id=="wait" && backend.LodgeResultPending,"Repeated click changed answer");ui.Close();c.GetComponent<PythonEventListPanel>().ShowMenu(12);yield return null;Check(ui.Showing && !c.CanDayOff,"Result resume failed");
            Finish(backend);ui.Close();Apply(c);yield return null;Check(c.CanDayOff,"Read waiting result blocks schedule");Good(backend,"day_off");Apply(c);yield return null;Check(!ui.Showing && backend.Simulation.State.dispatches.Single().status=="missing","Waiting raised blocking ADV");Good(backend,"day_off");Apply(c);yield return null;Check(ui.Showing && backend.LodgeConversation.returning!=null,"Return ADV absent");backend.LodgeConversation.Current.node=2;Apply(c);yield return null;ui.Answer(true);ui.Answer(true);yield return new WaitForSecondsRealtime(1);Check(backend.Simulation.State.dispatches.Single().status=="resolved" && backend.LodgeResultPending,"UI receipt absent");
            var p=backend.LodgeConversation.Current;p.node=1;Good(backend,"save_game",slot:3);backend=new LocalCafeBackend(Path.Combine(output,"ui"),Rules());Good(backend,"load_game",slot:3);CafeBackends.UseLocal(backend);Apply(c);yield return null;Check(ui.Showing && backend.LodgeConversation.Current.node==1,"UI save resume lost");Finish(backend);ui.Close();Apply(c);yield return null;
            // Exercise the shared read-only Extra viewer against the completed live branch.
            string snapshot=JsonUtility.ToJson(backend.Simulation.State),metadata=JsonUtility.ToJson(backend.LodgeConversations),profile=JsonUtility.ToJson(backend.Replays.Load());
            string savePath=Path.Combine(output,"ui/slot-3.json"),saved=File.ReadAllText(savePath);
            SceneManager.LoadScene("CatCafeTitle");yield return new WaitForSecondsRealtime(2);var extra=FindObjectOfType<PythonTitleScreen>().GetComponent<TitleExtraPanel>();extra.Open();yield return null;
            extra.OpenReplay(CafeLodgeDialogue.Id+".response/search");Check(!extra.Replay.Showing,"Locked search replay opened");
            foreach(var key in new[]{CafeLodgeDialogue.Id+".response/wait",CafeLodgeDialogue.Id+".return/received"})
            {
                extra.OpenReplay(key);yield return null;var replay=extra.Replay;Check(replay.Showing,"Lodge Extra absent");Check(!replay.Panel.GetComponentsInChildren<Button>(true).Any(button=>button.gameObject.activeInHierarchy && new[]{"保存","読込","迎える","見送る"}.Contains(button.name)),"Replay exposes commands");replay.Answer(true);replay.Answer(false);replay.Next();replay.Next();yield return null;
                replay.Panel.GetComponentsInChildren<Button>(true).Single(button=>button.name=="バックログ").onClick.Invoke();yield return null;Check(replay.HistoryScroll.gameObject.activeInHierarchy,"Replay backlog absent");replay.Close();yield return null;
            }
            Check(snapshot==JsonUtility.ToJson(backend.Simulation.State) && metadata==JsonUtility.ToJson(backend.LodgeConversations) && saved==File.ReadAllText(savePath) && profile==JsonUtility.ToJson(backend.Replays.Load()),"Extra mutated state or saves");
            if(Environment.GetCommandLineArgs().Contains("--lodge-extra")){extra.Open();File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" lodge checks; Extra manual ready");yield break;}
            SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);c=FindObjectOfType<PythonStateClient>();
            var poorRules=Rules();var probe=new LocalCafeBackend(Path.Combine(output,"ui-poor-probe"),poorRules);Setup(probe);poorRules.starting_funds=100+poorRules.starting_funds-probe.Simulation.State.funds;
            backend=new LocalCafeBackend(Path.Combine(output,"ui-poor"),poorRules);string poorId=Setup(backend);CafeBackends.UseLocal(backend);Apply(c);yield return null;ui=c.GetComponent<LodgeConversationPanel>();backend.LodgeConversation.Current.node=3;Apply(c);yield return null;
            buttons=ui.Panel.GetComponentsInChildren<Button>(true);Check(backend.Simulation.State.funds==100 && !buttons.Single(button=>button.name=="迎える").interactable && buttons.Single(button=>button.name=="見送る").interactable,"Poor UI blocks free wait");ui.Answer(false);yield return new WaitForSecondsRealtime(1);Check(backend.LodgeConversation.Current.branch_id=="wait","Poor wait UI rejected");Finish(backend);ui.Close();Apply(c);yield return null;Check(!backend.ConversationBlocksProgress,"Poor read wait still blocks");
            string manual=Path.Combine(output,"manual");backend=new LocalCafeBackend(manual,Rules());if(Environment.GetCommandLineArgs().Contains("--lodge-manual-load"))Good(backend,"load_game",slot:9);else{Setup(backend);backend.LodgeConversation.Current.node=3;Good(backend,"save_game",slot:2);}CafeBackends.UseLocal(backend);Apply(c);yield return null;
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" lodge conversation checks; restart="+Environment.GetCommandLineArgs().Contains("--lodge-restart"));
        }
    }
}
