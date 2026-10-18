using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class StandaloneUIFixSmoke:MonoBehaviour
    {
        static readonly BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
        string output;int checks;bool failed;LocalCafeBackend backend;
        void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        static CafeResponse Send(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1)
        {
            var s=b.Simulation.State;var c=new PythonShiftCommand{kind=kind,choice=choice,cat_id=cat,save_id=slot.ToString(),save_slot=slot,request_id=Guid.NewGuid().ToString("N"),instance_id=s.instance_id,expected_revision=s.revision,working_cats=new string[0],autoplay_objective="popularity",autoplay_mode="clear",autoplay_days=10};
            return StandaloneDispatchConversationSmoke.Raw(b,JsonUtility.ToJson(c));
        }
        static void Good(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1){var r=Send(b,kind,choice,cat,slot);if(r.result!=UnityWebRequest.Result.Success)throw new Exception(kind+": "+r.text);}
        static LocalCafeBackend Create(string path,bool ending=false)
        {
            var r=StandaloneEncounterConversationSmoke.Rules();r.growth.threshold=1;r.growth.rest_xp=1;r.growth.service_xp=1;r.intake.day=9999;
            if(ending){r.opening_ticks=8;r.arrival_ticks=new[]{0};r.patterns=new[]{new WeekPattern{days=Enumerable.Range(0,7).ToArray()}};r.goal_stages=new[]{new GoalStageRule{days=20,target=1}};r.growth.service_xp=0;}
            return new LocalCafeBackend(path,r);
        }
        void BackendCases()
        {
            if(File.Exists(Path.Combine(output,"overlap","slot-8.json"))){var restored=Create(Path.Combine(output,"overlap"),true);Good(restored,"load_game",slot:8);Check(restored.PopularityEnding.progress.node==4 && restored.PopularityEnding.progress.page_offset>0,"Process restart lost second page");}
            checks+=StandaloneEndingSmoke.BackendChecks(Path.Combine(output,"ending-regression"),false);
            string path=Path.Combine(output,"pending");var b=Create(path);Good(b,"new_game","popularity");Good(b,"start_autoplay");
            int n=0;while(!b.Simulation.GrowthWaiting && b.Simulation.State.autoplay.running && n++<100)Good(b,"step_autoplay");
            Check(b.Simulation.GrowthWaiting && b.Simulation.State.autoplay.running,"Real autoplay did not yield growth before next step");
            var row=b.View().growth_choices[0];Check(Send(b,row.kind,row.choices[0].choice,row.cat_id).result!=UnityWebRequest.Result.Success,"Executing autoplay allowed answer");
            Good(b,"save_game");Check(b.View().autoplay.paused,"Save did not pause");
            b=Create(path);Good(b,"load_game");Check(Send(b,"resume_autoplay").result!=UnityWebRequest.Result.Success,"Resume bypassed growth wait");Check(Send(b,"next_day").result!=UnityWebRequest.Result.Success,"Paused run allowed day progress");int operations=b.Simulation.State.autoplay.operations;
            while(b.View().growth_choices.Length>0){row=b.View().growth_choices[0];Good(b,row.kind,row.choices[0].choice,row.cat_id);}
            Check(b.View().autoplay.paused && b.Simulation.State.autoplay.operations==operations,"Answer resumed autoplay");
            Good(b,"save_game",slot:2);b=Create(path);Good(b,"load_game",slot:2);Check(!b.Simulation.GrowthWaiting && b.View().autoplay.paused,"Restart lost answer or pause");
            Good(b,"resume_autoplay");Good(b,"pause_autoplay");Good(b,"stop_autoplay");Check(!b.View().autoplay.running,"Cancel retained run");
            var legacy=JsonUtility.FromJson<CafeConversationProgress>("{\"node\":2}");Check(legacy.page_offset==0,"Old progress offset default changed");
            backend=Create(Path.Combine(output,"overlap"),true);Good(backend,"new_game","popularity");Good(backend,"start_autoplay");
            // Accelerated fixture: resting cats grow when the final goal resolves at closing.
            n=0;while(!backend.Simulation.GoalWaiting && backend.Simulation.State.autoplay.running && n++<200)Good(backend,"step_autoplay");Good(backend,"save_game");
            Check(backend.Simulation.GrowthWaiting && backend.PopularityEndingPending && backend.View().autoplay.paused,"Growth/ending overlap absent");
        }
        void Fail(Exception e){if(failed)return;failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);}
        void Log(string m,string trace,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)Fail(new Exception(m+"\n"+trace));}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--catcafe-ui-fix-check");if(i<0)return;
            var q=new GameObject("UI fix verification").AddComponent<StandaloneUIFixSmoke>();DontDestroyOnLoad(q);q.output=Path.GetFullPath(a[i+1]);Directory.CreateDirectory(q.output);Application.runInBackground=true;Application.logMessageReceived+=q.Log;
            try{q.BackendCases();q.StartCoroutine(q.Guard());}catch(Exception e){q.Fail(e);}
        }
        IEnumerator Guard(){var run=Run();while(true){bool more;try{more=run.MoveNext();}catch(Exception e){Fail(e);yield break;}if(!more)yield break;yield return run.Current;}}
        void Apply(PythonStateClient c){typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(c,new object[]{JsonUtility.FromJson<PythonCafeState>(JsonUtility.ToJson(backend.View()))});Canvas.ForceUpdateCanvases();}
        void Shot(string name){ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));}
        IEnumerator Run()
        {
            Screen.SetResolution(1280,800,false);CafeBackends.UseLocal(backend);SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
            var p=FindObjectOfType<ContentPreview>();var c=p.GetComponent<PythonStateClient>();var g=p.GetComponent<PythonGrowthPanel>();Apply(c);
            var few=JsonUtility.FromJson<PythonCafeState>(JsonUtility.ToJson(backend.View()));few.growth_choices[0].choices=few.growth_choices[0].choices.Take(1).ToArray();g.Apply(few);g.panel.SetActive(false);g.Apply(backend.View());
            Check(c.AutoPlayPaused && c.CanRespondGrowth(0) && !g.notice.text.Contains("実行中です"),"Paused controls disagree");
            Check(p.GetComponent<PopularityEndingPanel>()?.Showing!=true,"Ending covered pending growth");
            for(int i=0;i<3;i++)Check(g.choiceDetails[i].gameObject.activeInHierarchy && g.choiceDetails[i].text.Contains("効果・費用"),"Missing growth explanation "+i);
            Shot("GrowthPaused");yield return new WaitForEndOfFrame();
            if(Environment.GetCommandLineArgs().Contains("--ui-fix-manual")){int waiting=backend.View().growth_choices.Length;File.WriteAllText(Path.Combine(output,"manual-ready.txt"),"Click a growth choice in the visible player.");while(backend.View().growth_choices.Length==waiting)yield return null;while(!c.CanSendCommands)yield return null;yield return null;Check(c.AutoPlayPaused,"Manual answer restarted autoplay");File.WriteAllText(Path.Combine(output,"manual-answer.txt"),"PASS: physical UI answer resolved a growth choice while paused.");}
            int guard=0;while(backend.Simulation.GrowthWaiting && guard++<20)
            {
                Apply(c);int before=backend.View().growth_choices.Length;g.choices[0].onClick.Invoke();g.choices[0].onClick.Invoke();
                Check(!g.choices[0].interactable,"Repeated answer left button enabled");yield return new WaitForSecondsRealtime(2.2f);
                Check(backend.View().growth_choices.Length<before,"UI answer did not resolve growth");
            }
            Check(!backend.Simulation.GrowthWaiting && c.AutoPlayPaused,"UI answers failed or auto resumed");Apply(c);yield return null;
            var ui=p.GetComponent<PopularityEndingPanel>();Check(ui!=null && ui.Showing,"Ending did not open after growth");
            var options=p.GetComponent<GameOptionsPanel>();Check(options.CanOpen,"Paused ending cannot open settings");options.Open();Check(GameOptionsPanel.IsOpen,"Settings did not open");options.Cancel();
            var progress=backend.PopularityEnding.progress;CafeOptions.Current.instant=false;CafeOptions.Current.charactersPerSecond=10;typeof(AcquisitionConversationPanel).GetField("revealed",Hidden).SetValue(ui,0f);ui.Refresh();yield return new WaitForSecondsRealtime(.3f);ui.Next();Check(progress.node==0,"Full text skipped a node");ui.Next();Check(progress.node==0,"Same-frame click skipped a node");yield return null;ui.Next();Check(progress.node==1,"Next did not advance after full text");
            progress.node=4;progress.page_offset=0;ui.Refresh();yield return null;
            var body=(Text)typeof(AcquisitionConversationPanel).GetField("body",Hidden).GetValue(ui);
            var notice=(Text)typeof(AcquisitionConversationPanel).GetField("hint",Hidden).GetValue(ui);
            CafeOptions.Current.instant=true;ui.Refresh();Canvas.ForceUpdateCanvases();
            Check(!notice.gameObject.activeSelf,"Ending annotation visible");
            string all="";int pages=0;while(progress.node==4 && pages++<10)
            {
                ui.Refresh();Canvas.ForceUpdateCanvases();var gen=new TextGenerator();var settings=body.GetGenerationSettings(new Vector2(body.rectTransform.rect.width,10000));settings.verticalOverflow=VerticalWrapMode.Overflow;gen.Populate(body.text,settings);
                Check(gen.lineCount<=3 && body.preferredHeight<=ui.Panel.GetComponentsInChildren<ScrollRect>(true).First(s=>s.content==body.rectTransform).viewport.rect.height+1,"Ending page exceeds three visible lines");
                if(pages==1){Shot("EndingThreeLines");yield return new WaitForEndOfFrame();}
                all+=body.text;ui.Next();yield return null;
            }
            Check(pages>=2 && all.Replace("\n","").Replace("\r","")==CafePopularityEnding.Lines(CafePopularityEnding.Record(backend.Simulation.State),"猫")[4].text.Replace("\n","").Replace("\r",""),"Pagination lost or duplicated text");
            progress.node=4;progress.page_offset=0;ui.Refresh();yield return null;ui.Next();yield return null;
            int offset=progress.page_offset;Check(offset>0,"No second page offset");Good(backend,"save_game",slot:8);Good(backend,"load_game",slot:8);Apply(c);yield return null;ui.Open();Check(backend.PopularityEnding.progress.node==4 && backend.PopularityEnding.progress.page_offset==offset,"Save lost page position");
            ui.Close();yield return null;ui.Open();Check(backend.PopularityEnding.progress.page_offset==offset,"Reopen lost page position");
            ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="バックログ").onClick.Invoke();yield return null;Check(ui.HistoryScroll.content.GetComponent<Text>().text.Contains("営業の記録"),"Backlog lost node");
            yield return null;ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="バックログ").onClick.Invoke();yield return null;
            backend.PopularityEnding.progress.node=CafePopularityEnding.Lines(CafePopularityEnding.Record(backend.Simulation.State),"猫").Length;backend.PopularityEnding.progress.page_offset=0;ui.Refresh();yield return null;ui.Next();yield return null;
            Good(backend,"stop_autoplay");Good(backend,"continue_goal");Apply(c);yield return null;
            Check(backend.View().sidebar_progress==null && backend.Simulation.State.clear_results.Count==1,"Completed popularity remained or record lost");
            Check(!p.DisplayText.Contains("コハク") && !p.DisplayText.Contains("期限：") && !p.DisplayText.Contains("目標と進捗"),"Legacy sidebar retained cat or completed goal");
            var nav=p.GetComponent<PythonCatAcquisitionNavigation>();Check(nav.joinedEntry==null || !nav.joinedEntry.gameObject.activeSelf,"Joined-cat shortcut visible");
            Good(backend,"next_day");Apply(c);yield return null;
            Check(!p.DisplayText.Contains("コハク") && !p.DisplayText.Contains("期限：") && !p.DisplayText.Contains("目標と進捗"),"Preparation sidebar retained legacy text");
            c.StopAllCoroutines();var ev=p.GetComponent<PythonEventListPanel>();var counts=JsonUtility.FromJson<PythonCafeState>(JsonUtility.ToJson(backend.View()));counts.growth_choices=new PythonGrowthChoice[999];ev.Apply(counts);p.GetComponent<PythonBusinessHeader>().RefreshControls();
            var root=p.transform.Find("Content Canvas/Background");var buttons=new[]{p.GetSceneButton(3),root.Find("Open Save Browser").GetComponent<Button>(),root.Find("Return To Title").GetComponent<Button>(),p.GetComponent<PythonMenuPanel>().open,ev.open,p.GetComponent<PythonHousingPanel>().open};
            for(int i=0;i<buttons.Length;i++){var r=buttons[i].GetComponent<RectTransform>();Check(r.sizeDelta==new Vector2(148,38) && r.anchoredPosition==new Vector2(804+i%3*152,-(4+i/3*40)),"Header grid changed with count");}
            Check(ev.PendingCount>=999 && ev.open.GetComponentInChildren<Text>().text.Contains(ev.PendingCount.ToString()),"Count fixture absent");Shot("BusinessHeader");yield return new WaitForEndOfFrame();
            Screen.SetResolution(1600,900,false);yield return new WaitForSecondsRealtime(.5f);ev.Apply(counts);p.GetComponent<PythonBusinessHeader>().RefreshControls();Shot("BusinessHeaderWide");yield return new WaitForEndOfFrame();ev.Apply(backend.View());
            Good(backend,"start_objective","bond");Check(backend.View().sidebar_progress.details.Contains("好感度") && !backend.View().sidebar_progress.details.Contains("人気目標"),"Other active objective hidden with cleared popularity");
            SceneManager.LoadScene("CatCafeTitle");yield return new WaitForSecondsRealtime(2);var extra=FindObjectOfType<PythonTitleScreen>().GetComponent<TitleExtraPanel>();extra.Open();extra.Panel.transform.Find("エンディング").GetComponent<Button>().onClick.Invoke();extra.OpenReplay(CafePopularityEnding.Id+"/"+CafePopularityEnding.Branch);yield return null;Check(extra.Replay.Showing,"Extra ending unavailable");extra.Replay.Close();
            var record=backend.Replays.Load().entries.Single(e=>e.conversation_id==CafePopularityEnding.Id);var wrap=JsonUtility.FromJson<CafeReplayEntry>(JsonUtility.ToJson(record));wrap.lines=new[]{new CafeConversationLine("折り返し検証",new string('猫',200))};extra.Replay.Play(wrap,extra.Replay.Panel,extra.Entry.GetComponentInChildren<Text>().font,()=>{});CafeOptions.Current.instant=true;
            var replayProgress=(CafeConversationProgress)typeof(AcquisitionConversationPanel).GetField("progress",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(extra.Replay);var replayBody=(Text)typeof(AcquisitionConversationPanel).GetField("body",Hidden).GetValue(extra.Replay);string read="";int wrapPages=0;
            while(replayProgress.node==0 && wrapPages++<10){extra.Replay.Refresh();Canvas.ForceUpdateCanvases();var gen=new TextGenerator();var settings=replayBody.GetGenerationSettings(new Vector2(replayBody.rectTransform.rect.width,10000));settings.verticalOverflow=VerticalWrapMode.Overflow;gen.Populate(replayBody.text,settings);Check(gen.lineCount<=3,"Wrapped page exceeds three lines");read+=replayBody.text;if(wrapPages==1){Shot("EndingWrappedWide");yield return new WaitForEndOfFrame();}extra.Replay.Next();yield return null;}Check(read==new string('猫',200) && wrapPages>=2,"Wrapped pages lost text");extra.Replay.Close();
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" UI fix checks; zero Unity errors.");
            if(!Environment.GetCommandLineArgs().Contains("--ui-fix-keep-open"))Application.Quit(0);
        }
    }
}
