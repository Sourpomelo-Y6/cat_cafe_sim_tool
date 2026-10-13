using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    // Opt-in Player QA uses real scene clients, UGUI callbacks, coroutines and disk saves.
    // Normal launches do not create this component or override the user's save directory.
    public class StandaloneScreenSmoke : MonoBehaviour
    {
        private string output,importPath;
        private LocalCafeBackend backend;
        private bool nativeDefault;
        private RestReferences pythonReference;
        private readonly List<string> checks=new List<string>(),errors=new List<string>();
        private PythonStateClient Client=>FindObjectOfType<PythonStateClient>();
        private void Check(bool condition,string label){if(!condition)throw new Exception(label);checks.Add(label);}
        private static string Argument(string name){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);return i>=0 && i+1<args.Length?args[i+1]:null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var path=Argument("--catcafe-ui-check");if(string.IsNullOrEmpty(path))return;
            Application.runInBackground=true;var go=new GameObject("Standalone screen verification");DontDestroyOnLoad(go);var runner=go.AddComponent<StandaloneScreenSmoke>();
            runner.output=Path.GetFullPath(path);Directory.CreateDirectory(runner.output);runner.importPath=Argument("--catcafe-import-fixture");
            runner.nativeDefault=CafeBackends.Standalone;runner.backend=new LocalCafeBackend(Argument("--catcafe-ui-save-directory") ?? Path.Combine(runner.output,"saves"));CafeBackends.UseLocal(runner.backend);
            Application.logMessageReceived+=runner.Log;runner.StartCoroutine(runner.Guard(runner.Run()));
        }
        private void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(message+"\n"+stack);}
        private IEnumerator Guard(IEnumerator root)
        {
            var stack=new Stack<IEnumerator>();stack.Push(root);string failure=null;
            while(stack.Count>0)
            {
                object value=null;bool more=false;
                try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception ex){failure=ex.ToString();break;}
                if(!more){stack.Pop();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
            }
            if(errors.Count>0 && failure==null)failure=string.Join("\n",errors);
            File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Result{success=failure==null,checks=checks.ToArray(),errors=errors.ToArray(),failure=failure},true));
            Application.Quit(failure==null?0:1);
        }
        [Serializable] private class Result{public bool success;public string[] checks,errors;public string failure;}
        private IEnumerator Wait(Func<bool> ready,string label)
        {float deadline=Time.realtimeSinceStartup+15;while(!ready() && Time.realtimeSinceStartup<deadline)yield return null;Check(ready(),label);}
        private IEnumerator Click(Button button,string label)
        {
            yield return Wait(()=>button!=null && button.IsActive() && button.interactable,label+" available");
            ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.2f);yield return null;var canvases=FindObjectsOfType<Canvas>().Where(c=>c.isRootCanvas).ToArray();var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var distances=canvases.Select(c=>c.planeDistance).ToArray();
            var camera=new GameObject("QA capture camera").AddComponent<Camera>();var target=new RenderTexture(Screen.width,Screen.height,24);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceCamera;canvases[i].worldCamera=camera;canvases[i].planeDistance=1;}
            Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());RenderTexture.active=previous;
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];}Destroy(texture);Destroy(camera.gameObject);target.Release();Destroy(target);yield return null;
        }
        private void RestoreFixture(CafeSnapshot state){backend.Simulation.Restore(state);typeof(LocalCafeBackend).GetMethod("ResetJournal",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(backend,null);}
        private IEnumerator Changed(int revision){yield return Wait(()=>Client!=null && Client.CurrentState!=null && Client.CurrentState.revision!=revision && Client.CanManageSaves,"command completed");}
        [Serializable] private class RestReferences{public RestCase[] cases;public PythonDay day;}
        [Serializable] private class PythonDay{public double funds,revenue,expenses,popularity;public int completed;public PythonDayCat[] cats;}
        [Serializable] private class PythonDayCat{public string id;public double stamina,fatigue,stress;}
        [Serializable] private class RestCase{public RestCat[] cats;public string[] expected;}
        [Serializable] private class RestCat{public string id,health,activity;public double fatigue;}
        private void CompareRestProposals()
        {
            string path=Argument("--catcafe-rest-reference");if(string.IsNullOrEmpty(path))return;
            var reference=JsonUtility.FromJson<RestReferences>(File.ReadAllText(path));pythonReference=reference;var candidate=new LocalCafeBackend(Path.Combine(output,"projection-only"),backend.Simulation.Rules);
            foreach(var row in reference.cases)
            {
                foreach(var input in row.cats){var cat=candidate.Simulation.State.cats.Single(c=>c.id==input.id);cat.fatigue=input.fatigue;cat.health=input.health;cat.activity=input.activity;}
                Check(candidate.View().rest_proposal.SequenceEqual(row.expected),"rest proposal Python reference");
            }
        }
        private void ComparePythonDay()
        {
            if(pythonReference?.day==null)return;var expected=pythonReference.day;var state=backend.Simulation.State;
            Check(state.funds==expected.funds && state.revenue==expected.revenue && state.expenses==expected.expenses && state.completed==expected.completed && state.popularity==expected.popularity,"UI full day matches original complete Python settlement");
            foreach(var row in expected.cats){var cat=state.cats.Single(c=>c.id==row.id);Check(Math.Abs(cat.stamina-row.stamina)<1e-9 && Math.Abs(cat.fatigue-row.fatigue)<1e-9 && Math.Abs(cat.stress-row.stress)<1e-9,"UI full day matches Python cat condition");}
        }
        private IEnumerator Run()
        {
            Check(nativeDefault,"standalone default before QA override");Check(CafeBackends.Standalone,"standalone default");CompareRestProposals();var referencePath=Argument("--catcafe-details-reference");if(!string.IsNullOrEmpty(referencePath))checks.Add("Unity details references: "+StandaloneDetailsSmoke.Run(Path.Combine(output,"details-checks"),referencePath));
            yield return Wait(()=>FindObjectOfType<PythonTitleScreen>()?.CanManageSaves==true,"title ready without server");
            var title=FindObjectOfType<PythonTitleScreen>();yield return Capture("01-title");
            if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--catcafe-ui-resume-only"))
            {
                title.ShowSaves();var saved=title.GetComponent<PythonSaveBrowser>();saved.OpenLoad();saved.Select(1);yield return Click(saved.action,"restart title load review");yield return Click(saved.confirm,"restart title load");
                yield return Wait(()=>Client?.CanManageSaves==true,"restart business ready");Check(Client.CurrentState.day==1 && Client.CurrentState.closed && Client.CurrentState.funds==1730,"cross process restores known settlement");
                var closePanel=Client.GetComponent<PythonClosingPanel>();int old=Client.CurrentState.revision;yield return Click(closePanel.next,"restart next day");yield return Changed(old);Check(Client.CurrentState.day==2 && Client.CurrentState.can_set_shifts,"restart continues next day");yield return Capture("09-restart");yield break;
            }
            yield return Click(title.newGame,"new game");title.objectives.value=Array.FindIndex(title.CurrentState.new_game_options,r=>r.choice=="free");
            yield return Click(title.startGame,"new game confirmation");Check(title.conditions.text.Contains("開始の確認"),"new game review");yield return Click(title.startGame,"confirmed new game");
            yield return Wait(()=>Client?.CanManageSaves==true,"business scene ready");Check(Client.CurrentState.day==1 && Client.CurrentState.can_set_shifts,"initial preparation");Check(Client.CurrentState.rest_proposal.SequenceEqual(new[]{"cat-mugi","cat-sora","cat-tama"}),"rest proposal matches Python initial tie ordering");
            var reference=new CafeSimulation(backend.Simulation.Rules,backend.Simulation.State.seed,"free");
            var owner=Client.gameObject;var prep=owner.GetComponent<PythonPreparationPanel>();prep.Open();yield return Capture("02-preparation");
            prep.SelectCat(0);int revision=Client.CurrentState.revision;yield return Click(owner.GetComponent<PythonPreparationRoster>().RestButton(0),"rest first cat");yield return Changed(revision);Check(!Client.CurrentState.cats[0].working,"rest command mapped");
            revision=Client.CurrentState.revision;yield return Click(owner.GetComponent<PythonPreparationRoster>().WorkButton(0),"work first cat");yield return Changed(revision);Check(Client.CurrentState.cats[0].working,"work command mapped");
            reference.Execute("set_shifts",working:reference.State.cats.Where(c=>c.id!=reference.State.cats[0].id && c.working).Select(c=>c.id).ToArray());reference.Execute("set_shifts",working:backend.Simulation.State.cats.Where(c=>c.working).Select(c=>c.id).ToArray());reference.Execute("start_business");
            var details=owner.GetComponent<PythonCatDetailsPanel>();details.Open(Client.CurrentState.cats[0].cat_id);Check(details.panel.activeSelf && !string.IsNullOrEmpty(details.details.text),"cat details rendered");yield return Capture("03-cat-details");foreach(int tab in new[]{1,2,3,4}){yield return Click(details.recordTabs[tab],"detail tab "+tab);Check(!string.IsNullOrWhiteSpace(details.details.text),"detail tab content "+tab);yield return Capture("03-details-tab-"+tab);if(tab==4){Canvas.ForceUpdateCanvases();Check(details.scroll.content.rect.height>details.scroll.viewport.rect.height,"long profile scrolls");details.scroll.verticalNormalizedPosition=0;yield return Capture("03-profile-end");}}details.Close();
            var events=owner.GetComponent<PythonEventListPanel>();events.Open();Check(events.panel.activeSelf,"event list rendered");events.Close();
            revision=Client.CurrentState.revision;yield return Click(prep.start,"start business");yield return Changed(revision);Check(!Client.CurrentState.can_set_shifts && !Client.CurrentState.closed,"business started");
            prep.Close();yield return Capture("04-service");
            for(int i=0;i<200 && !Client.CurrentState.closed;i++)
            {
                Check(string.IsNullOrEmpty(Client.CurrentState.required_action),"free day no unanswered event");revision=Client.CurrentState.revision;yield return Click(owner.GetComponent<ContentPreview>().GetSceneButton(2),"advance service");yield return Changed(revision);
                if(Client.CurrentState.tick==11){var service=owner.GetComponent<PythonServiceScreen>();Check(service.OccupiedCount>0,"occupied seats rendered");yield return Capture("04-active-service");var card=service.GetSeatCard(0);yield return Click(card.details,"active seat cat details");Check(details.panel.activeSelf,"active seat opens cat details");details.Close();}
            }
            Check(Client.CurrentState.closed,"full day closes");while(reference.State.phase!="closed")reference.Execute("advance_business");Check(CafeLegacyImport.GameDigest(reference.State)==CafeLegacyImport.GameDigest(backend.Simulation.State),"UI full day matches independent simulation");ComparePythonDay();var closing=owner.GetComponent<PythonClosingPanel>();Check(closing.panel.activeSelf,"closing panel opened");yield return Capture("05-closing");details.Open(Client.CurrentState.cats[4].cat_id);details.DailyRecords();Check(details.details.text.Contains("日目：1") && details.details.text.Contains("体力消耗：83"),"closing cat net stamina history");yield return Capture("05-cat-history");details.Close();
            var browser=owner.GetComponent<PythonSaveBrowser>();yield return Click(closing.save,"open closing save");browser.Select(1);yield return Click(browser.action,"save slot 2 review");Check(browser.confirmation.activeSelf,"save confirmation");revision=Client.CurrentState.revision;yield return Click(browser.confirm,"save slot 2");yield return Changed(revision);Check(File.Exists(Path.Combine(output,"saves/slot-2.json")),"selected slot saved");browser.Close();
            revision=Client.CurrentState.revision;yield return Click(closing.next,"next day");yield return Changed(revision);Check(Client.CurrentState.day==2 && Client.CurrentState.can_set_shifts,"next day preparation");Check(Client.CurrentState.cats[4].records.history.Contains("体力消耗：83"),"known closed net stamina survives next day");
            browser.OpenLoad();browser.Select(1);yield return Click(browser.action,"load slot 2 review");yield return Click(browser.confirm,"load slot 2");yield return Wait(()=>Client.CanManageSaves && Client.CurrentState.day==1 && Client.CurrentState.closed,"business load restores closing");
            owner.GetComponent<PythonReturnToTitle>().Return();yield return Wait(()=>FindObjectOfType<PythonTitleScreen>()?.CanManageSaves==true,"return to title");
            Check(ReferenceEquals(CafeBackends.Local,backend),"session shared across scenes");title=FindObjectOfType<PythonTitleScreen>();title.ShowSaves();var titleBrowser=title.GetComponent<PythonSaveBrowser>();titleBrowser.OpenLoad();titleBrowser.Select(1);yield return Wait(()=>titleBrowser.action.interactable,"title save list ready");yield return Click(titleBrowser.action,"title load review");yield return Click(titleBrowser.confirm,"title load");yield return Wait(()=>Client?.CanManageSaves==true,"title resume to business");Check(Client.CurrentState.day==1 && Client.CurrentState.closed,"title resume preserves saved day");
            if(!string.IsNullOrEmpty(importPath))
            {
                owner=Client.gameObject;var legacy=owner.GetComponent<PythonLegacySavePanel>();legacy.Open();legacy.path.text=importPath;yield return Click(legacy.inspect,"legacy preview");yield return Wait(()=>legacy.resume.interactable,"converted import preview ready");Check(legacy.slots.options.Count>0,"empty import slots exposed");yield return Capture("06-import");
                legacy.path.text=importPath+"x";Check(!legacy.resume.interactable,"changed path invalidates preview");legacy.path.text=importPath;yield return Click(legacy.inspect,"preview again");yield return Wait(()=>legacy.resume.interactable,"preview refreshed");
                string oldId=Client.CurrentState.instance_id;yield return Click(legacy.resume,"import empty slot");yield return Wait(()=>Client.CanManageSaves && Client.CurrentState.instance_id!=oldId,"import command completed");Check(backend.Simulation.State.import_history!=null,"legacy migration anchor retained");Check(!legacy.panel.activeSelf,"import panel closes after success");
            }
            // Establish a deterministic unanswered event, then exercise its actual UI/save routes.
            var eventState=new CafeSimulation(backend.Simulation.Rules,Enumerable.Range(0,1000).First(i=>new CafeSimulation(backend.Simulation.Rules,i).CurrentStoreEvent.type=="trouble")).State;
            RestoreFixture(eventState);yield return Wait(()=>Client.CurrentState.instance_id==backend.Simulation.State.instance_id && Client.CanManageSaves,"event fixture displayed");owner=Client.gameObject;
            var store=owner.GetComponent<PythonStoreEventPanel>();Check(store.panel.activeSelf && !Client.CanProgressBusiness,"unanswered event blocks service");yield return Capture("08-event");
            browser=owner.GetComponent<PythonSaveBrowser>();browser.OpenSave();browser.Select(3);yield return Click(browser.action,"event save review");revision=Client.CurrentState.revision;yield return Click(browser.confirm,"save unanswered event");yield return Changed(revision);browser.Close();
            revision=Client.CurrentState.revision;yield return Click(store.choices[0],"answer event");yield return Changed(revision);Check(!backend.Simulation.StoreEventWaiting,"event answered once");
            browser.OpenLoad();browser.Select(3);yield return Click(browser.action,"event reload review");yield return Click(browser.confirm,"reload unanswered event");yield return Wait(()=>Client.CanManageSaves && backend.Simulation.StoreEventWaiting,"reload restores unanswered event");
            revision=Client.CurrentState.revision;yield return Click(store.choices[0],"answer restored event");yield return Changed(revision);Check(!backend.Simulation.StoreEventWaiting,"restored event answer accepted");
            // A paused native autoplay save needs an explicit resume command, not only a UI flag.
            RestoreFixture(new CafeSimulation(backend.Simulation.Rules,0,"bond").State);backend.Simulation.StartAutoplay("bond","basic",1);backend.Simulation.PauseAutoplay();yield return Wait(()=>Client.CurrentState.autoplay?.paused==true,"paused autoplay displayed");
            var auto=owner.GetComponent<PythonAutoPlayPanel>();auto.Open();yield return Click(auto.proceed,"resume paused autoplay");yield return Wait(()=>backend.Simulation.State.autoplay.paused==false,"native autoplay resumed");Client.StopAutoPlay();yield return Wait(()=>Client.CanManageSaves && !Client.AutoPlayRunning,"autoplay stopped through UI client");
            Check(CafeBackends.Standalone,"all UI operations remain standalone");yield return Capture("07-resumed");
        }
    }
}
