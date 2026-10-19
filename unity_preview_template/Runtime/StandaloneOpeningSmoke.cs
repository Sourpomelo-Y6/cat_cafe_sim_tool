using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class StandaloneOpeningSmoke:MonoBehaviour
    {
        string output;int checks;bool failed;LocalCafeBackend backend;
        static void Good(LocalCafeBackend b,string kind,string choice=null,int slot=1)=>StandaloneDispatchConversationSmoke.Good(b,kind,choice,slot:slot);
        static CafeResponse Send(LocalCafeBackend b,string kind,string choice=null,int slot=1)=>StandaloneDispatchConversationSmoke.Raw(b,JsonUtility.ToJson(new PythonShiftCommand{kind=kind,choice=choice,save_id=slot.ToString(),save_slot=slot,request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,working_cats=new string[0]}));
        static string Facts(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.instance_id="";s.revision=0;return JsonUtility.ToJson(s);}
        public static int BackendChecks(string path,bool restart)
        {
            int n=0;Action<bool,string> check=(ok,label)=>{n++;if(!ok)throw new Exception(label);};
            if(restart){var loaded=new LocalCafeBackend(Path.Combine(path,"restart"),playOpening:true);Good(loaded,"load_game",slot:2);check(loaded.OpeningPending && loaded.Opening.progress.node==2 && loaded.Opening.progress.page_offset==3,"Process restart lost page");check(!loaded.Replays.Load().entries.Any(e=>e.conversation_id==CafeOpening.Id),"Partial restart unlocked Extra");}
            check(CafeBackends.OpeningEnabled(new string[0]),"Normal title skipped");check(!CafeBackends.OpeningEnabled(new[]{"--catcafe-debug"}),"Debug auto played");check(CafeBackends.OpeningEnabled(new[]{"--catcafe-debug","--catcafe-opening"}),"Explicit debug playback skipped");
            foreach(var mode in CafeOpening.Modes)
            {
                string folder=Path.Combine(path,mode+"-"+Guid.NewGuid().ToString("N"));var b=new LocalCafeBackend(folder,playOpening:true);Good(b,"new_game",mode);string before=Facts(b);check(b.OpeningPending,"New start missing "+mode);
                Good(b,"load_game");check(b.OpeningPending && b.Opening.mode==mode && before==Facts(b),"Initial autosave lost opening");
                foreach(var kind in new[]{"start_business","advance_business","day_off","next_day","start_autoplay","step_autoplay","set_shifts","player_start"})check(Send(b,kind).result==UnityWebRequest.Result.ProtocolError && before==Facts(b),"Opening advanced/mutated: "+kind);
                string invalidBefore=Facts(b);check(Send(b,"new_game","invalid").result==UnityWebRequest.Result.ProtocolError && invalidBefore==Facts(b) && b.OpeningPending,"Invalid new start changed state");
                var lines=CafeOpening.Lines(b.Opening);check(b.Opening.cat_ids.SequenceEqual(b.Simulation.State.cats.Where(c=>c.activity=="cafe").Select(c=>c.id)),"Roster fabricated");
                for(int node=0;node<=lines.Length;node++)
                {
                    b.Opening.progress.node=node;b.Opening.progress.page_offset=node<lines.Length?3:0;Good(b,"save_game",slot:3);Good(b,"load_game",slot:3);
                    check(b.Opening.progress.node==node && b.Opening.progress.page_offset==(node<lines.Length?3:0) && !b.Opening.progress.completed && before==Facts(b),"Saved page/state differs "+mode+"/"+node);
                    check(!b.Replays.Load().entries.Any(e=>e.conversation_id==CafeOpening.Id),"Partial viewing unlocked");
                }
                b.FinishOpening(false);b.FinishOpening(false);check(!b.ConversationBlocksProgress && b.Opening.outcome=="viewed" && before==Facts(b),"Completion changed gameplay");Good(b,"save_game",slot:4);Good(b,"load_game",slot:4);check(!b.OpeningPending && b.Replays.Load().entries.Count(e=>e.conversation_id==CafeOpening.Id)==1,"Completed load replayed/duplicated");Good(b,"day_off");check(b.Simulation.State.day==2,"Completed opening blocked day off");
                var corrupt=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(folder,"slot-4.json")));corrupt.slot=10;corrupt.opening.progress.node=-1;File.WriteAllText(Path.Combine(folder,"slot-10.json"),JsonUtility.ToJson(corrupt));string prior=Facts(b);check(Send(b,"load_game",slot:10).result==UnityWebRequest.Result.ProtocolError && prior==Facts(b) && !b.OpeningPending,"Invalid load changed live game");
                foreach(int node in new[]{0,2}){Good(b,"new_game",mode);before=Facts(b);b.Opening.progress.node=node;b.FinishOpening(true);check(before==Facts(b) && b.Opening.outcome=="skipped","Skip mutated state");Good(b,"save_game",slot:5);Good(b,"load_game",slot:5);check(!b.OpeningPending && b.Opening.progress.viewed_branches.Length==0,"Skip replayed/unlocked");}
                var fresh=new LocalCafeBackend(Path.Combine(folder,"skip-only"),playOpening:true);Good(fresh,"new_game",mode);fresh.Opening.progress.node=2;fresh.FinishOpening(true);fresh.RememberViewedConversations();check(fresh.Replays.Load().entries.Length==0,"Skip unlocked unseen Extra");
                var old=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(folder,"slot-4.json")));old.slot=6;old.opening=null;File.WriteAllText(Path.Combine(folder,"slot-6.json"),JsonUtility.ToJson(old));Good(b,"load_game",slot:6);check(b.Opening==null && !b.ConversationBlocksProgress,"Legacy opening injected");
                Good(b,"new_game",mode);b.Opening.progress.node=-1;bool bad=false;try{CafeOpening.Validate(b.Opening,b.Simulation.State);}catch(ArgumentException){bad=true;}check(bad,"Invalid page accepted");b.Opening.progress.node=0;
                Good(b,"new_game",mode);check(b.OpeningPending && b.Opening.progress.node==0 && b.Replays.Load().entries.Length==1,"New start retained prior progress/lost archive");
            }
            var debug=new LocalCafeBackend(Path.Combine(path,"debug-"+Guid.NewGuid().ToString("N")));Good(debug,"new_game","free");check(!debug.OpeningPending && debug.Opening.outcome=="debug_omitted" && debug.Replays.Load().entries.Length==0,"Default debug playback/unlock");Good(debug,"day_off");
            var retry=new LocalCafeBackend(Path.Combine(path,"retry-"+Guid.NewGuid().ToString("N")),playOpening:true);string payload=JsonUtility.ToJson(new PythonShiftCommand{kind="new_game",choice="free",request_id=Guid.NewGuid().ToString("N"),instance_id=retry.Simulation.State.instance_id,expected_revision=retry.Simulation.State.revision,working_cats=new string[0]});var reply=StandaloneDispatchConversationSmoke.Raw(retry,payload);retry.Opening.progress.node=2;string retryFacts=Facts(retry);check(StandaloneDispatchConversationSmoke.Raw(retry,payload).text==reply.text && retry.Opening.progress.node==2 && Facts(retry)==retryFacts,"New start retry initialized twice");
            var restartCase=new LocalCafeBackend(Path.Combine(path,"restart"),playOpening:true);Good(restartCase,"new_game","free");restartCase.Opening.progress.node=2;restartCase.Opening.progress.page_offset=3;Good(restartCase,"save_game",slot:2);
            string block=Path.Combine(path,"blocked-"+Guid.NewGuid().ToString("N"));File.WriteAllText(block,"fixture");var broken=new LocalCafeBackend(block,playOpening:true);string unchanged=Facts(broken);check(Send(broken,"new_game","free").result==UnityWebRequest.Result.ProtocolError && broken.Opening==null && Facts(broken)==unchanged,"Failed creation started opening");
            var missing=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);foreach(var c in missing.cats)c.name="名前"+c.id;var custom=new LocalCafeBackend(Path.Combine(path,"names-"+Guid.NewGuid().ToString("N")),missing,true);Good(custom,"new_game","free");check(custom.Opening.cat_names.All(name=>name.StartsWith("名前")),"Names hard coded");
            custom.Simulation.State.cats.ForEach(c=>c.activity="missing");var absent=CafeOpening.Start(custom.Simulation.State,"free",id=>custom.Simulation.Definition(id).name,true);check(absent.cat_ids.Length==0 && CafeOpening.Lines(absent).Any(l=>l.text.Contains("静かな店内")),"Absent cat invented");return n;
        }
        void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        void Fail(Exception e){if(failed)return;failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);}
        void Log(string m,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)Fail(new Exception(m+"\n"+stack));}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-opening-check");if(i<0)return;var qa=new GameObject("Opening verification").AddComponent<StandaloneOpeningSmoke>();DontDestroyOnLoad(qa);qa.output=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(qa.output);Application.runInBackground=true;Application.logMessageReceived+=qa.Log;
            try{qa.checks=BackendChecks(Path.Combine(qa.output,"cases"),args.Contains("--opening-restart"));qa.StartCoroutine(qa.Guard());}catch(Exception e){qa.Fail(e);}
        }
        IEnumerator Guard(){var run=Run();while(true){bool more;try{more=run.MoveNext();}catch(Exception e){Fail(e);yield break;}if(!more)yield break;yield return run.Current;}}
        IEnumerator Run()
        {
            Screen.SetResolution(1280,800,false);backend=new LocalCafeBackend(Path.Combine(output,"ui-"+Guid.NewGuid().ToString("N")),playOpening:true);CafeBackends.UseLocal(backend);SceneManager.LoadScene("CatCafeTitle");yield return new WaitForSecondsRealtime(2);
            var title=FindObjectOfType<PythonTitleScreen>();title.newGame.onClick.Invoke();title.objectives.value=Array.FindIndex(title.CurrentState.new_game_options,o=>o.choice=="free");title.startGame.onClick.Invoke();yield return null;Check(title.ConfirmingStart,"Start confirmation absent");title.startGame.onClick.Invoke();yield return new WaitForSecondsRealtime(3);
            var preview=FindObjectOfType<ContentPreview>();var ui=preview.GetComponent<OpeningPanel>();var client=preview.GetComponent<PythonStateClient>();Check(ui!=null && ui.Showing,"Title did not open opening");string before=Facts(backend);ScreenCapture.CaptureScreenshot(Path.Combine(output,"opening.png"));yield return new WaitForEndOfFrame();
            ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="バックログ").onClick.Invoke();yield return null;Check(ui.HistoryScroll.gameObject.activeInHierarchy,"Backlog absent");ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="バックログ").onClick.Invoke();yield return null;
            bool original=CafeOptions.Current.instant;CafeOptions.Current.instant=true;int steps=0;while(ui.Showing && steps++<200){ui.Next();yield return null;}CafeOptions.Current.instant=original;Check(!ui.Showing && backend.Opening.outcome=="viewed" && before==Facts(backend),"Natural reading changed rules/failed completion");
            var prep=preview.GetComponent<PythonPreparationPanel>();Check(prep.panel.activeSelf && prep.start.interactable && prep.dayOff.interactable,"Normal controls blocked");ScreenCapture.CaptureScreenshot(Path.Combine(output,"preparation.png"));yield return new WaitForEndOfFrame();prep.start.onClick.Invoke();yield return new WaitForSecondsRealtime(2);Check(backend.Simulation.State.phase=="open","Visible start failed");
            Good(backend,"new_game","free");yield return new WaitForSecondsRealtime(2.2f);ui=preview.GetComponent<OpeningPanel>();Check(ui.Showing,"New game opening absent");ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="スキップ").onClick.Invoke();yield return null;Check(backend.Opening.outcome=="skipped" && prep.dayOff.interactable,"Skip blocked prep");prep.dayOff.onClick.Invoke();yield return null;Check(preview.GetComponent<PythonDayOffPanel>().panel.activeSelf,"Day off panel blocked");
            Good(backend,"new_game","free");yield return new WaitForSecondsRealtime(2.2f);ui=preview.GetComponent<OpeningPanel>();backend.Opening.progress.node=2;ui.Refresh();Good(backend,"save_game",slot:7);Good(backend,"load_game",slot:7);yield return new WaitForSecondsRealtime(2.2f);Check(ui.Showing && backend.Opening.progress.node==2,"UI save/load lost node");
            ui.Close();yield return null;SceneManager.LoadScene("CatCafeTitle");yield return new WaitForSecondsRealtime(2);var extra=FindObjectOfType<PythonTitleScreen>().GetComponent<TitleExtraPanel>();extra.Open();extra.OpenReplay(CafeOpening.Id+"/free");yield return null;Check(extra.Replay.Showing,"Opening Extra unavailable");extra.Replay.Close();
            // Render a genuinely unregistered content ID without changing game rules.
            var catalogField=typeof(CafeDetailPresentation).GetField("catalog",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var catalog=(CafePresentationCatalog)catalogField.GetValue(backend.Details);var mapping=catalog.content_ids[0];string oldImage=mapping.label;mapping.label="opening-unregistered-image";
            Good(backend,"new_game","free");SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);preview=FindObjectOfType<ContentPreview>();ui=preview.GetComponent<OpeningPanel>();
            backend.Opening.progress.node=2+2*Array.IndexOf(backend.Opening.cat_ids,mapping.id);ui.Refresh();yield return null;
            var portrait=(RawImage)typeof(AcquisitionConversationPanel).GetField("portrait",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(ui);
            Check(portrait.texture==ContentPreview.CatPlaceholderTexture(),"Unregistered image did not fall back");ScreenCapture.CaptureScreenshot(Path.Combine(output,"missing-image.png"));yield return new WaitForEndOfFrame();mapping.label=oldImage;ui.Close();yield return null;
            // Leave an isolated opening visible for native screen checks.
            Good(backend,"new_game","free");SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);File.WriteAllText(Path.Combine(output,"result.txt"),"PASS "+checks+" opening checks; zero Unity errors");
            if(!Environment.GetCommandLineArgs().Contains("--opening-manual"))Application.Quit(0);
        }
    }
}
