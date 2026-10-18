using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class StandaloneExtraConversationSmoke : MonoBehaviour
    {
        static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        string output;int checks;bool completed,failed;LocalCafeBackend backend;readonly List<string> errors=new List<string>();
        public static LocalCafeBackend Create(string path){var r=StandaloneEncounterConversationSmoke.Rules();return new LocalCafeBackend(path,r);}
        static void Good(LocalCafeBackend b,string kind,string choice=null,int slot=1)=>StandaloneDispatchConversationSmoke.Good(b,kind,choice,slot:slot);
        public static int BackendChecks(string folder,bool restart)
        {
            int n=0;Action<bool,string> check=(ok,label)=>{n++;if(!ok)throw new Exception(label);};var b=Create(folder);
            if(restart)check(b.Replays.Load().entries.Length==2,"Profile lost after restart");
            foreach(string answer in new[]{"accept","decline"})
            {
                Good(b,"new_game","free");while(b.Simulation.State.day<4)Good(b,"day_off");b.BeginIntakeConversation();Good(b,"resolve_intake",answer);var p=b.Conversation;p.node=4;CafeIntakeConversation.Finish(p);b.RememberViewedConversations();Good(b,"save_game",slot:answer=="accept"?3:4);
            }
            check(b.Replays.Load().entries.Length==2,"Cross-game branch union");Good(b,"load_game",slot:3);check(b.Replays.Load().entries.Length==2,"Older slot erased profile");
            string import=Path.Combine(folder,"migration");Directory.CreateDirectory(import);foreach(int slot in new[]{3,4})File.Copy(Path.Combine(folder,"slot-"+slot+".json"),Path.Combine(import,"slot-"+slot+".json"),true);
            var fresh=Create(import);string before=JsonUtility.ToJson(fresh.Simulation.State);string saveBefore=File.ReadAllText(Path.Combine(import,"slot-3.json"));fresh.ImportViewedSaves();check(fresh.Replays.Load().entries.Length==2 && before==JsonUtility.ToJson(fresh.Simulation.State) && saveBefore==File.ReadAllText(Path.Combine(import,"slot-3.json")),"Migration mutated state or slots");fresh.ImportViewedSaves();check(fresh.Replays.Load().entries.Length==2,"Migration duplicated unlocks");
            var invalid=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(import,"slot-3.json")));invalid.slot=5;invalid.conversation.node=999;File.WriteAllText(Path.Combine(import,"slot-5.json"),JsonUtility.ToJson(invalid));fresh.ImportViewedSaves();check(fresh.ReplayImportNotice.Contains("1件") && fresh.Replays.Load().entries.Length==2,"Invalid migration accepted");
            return n;
        }
        void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){errors.Add(message+"\n"+stack);if(completed)Fail(new Exception(message));}}
        void Fail(Exception e){if(failed)return;failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-extra-conversation-check");if(i<0)return;
            var qa=new GameObject("Extra verification").AddComponent<StandaloneExtraConversationSmoke>();DontDestroyOnLoad(qa);qa.output=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(qa.output);Application.runInBackground=true;Application.logMessageReceived+=qa.Log;
            try{qa.checks+=BackendChecks(Path.Combine(qa.output,"cases"),args.Contains("--extra-restart"));qa.StartCoroutine(qa.Guard());}catch(Exception e){qa.Fail(e);}
        }
        IEnumerator Guard(){var run=Run();while(true){bool more;try{if(errors.Count>0)throw new Exception(string.Join("\n",errors));more=run.MoveNext();}catch(Exception e){Fail(e);yield break;}if(!more)yield break;yield return run.Current;}}
        IEnumerator Run()
        {
            backend=Create(Path.Combine(output,"cases"));CafeBackends.UseLocal(backend);SceneManager.LoadScene("CatCafeTitle");yield return new WaitForSecondsRealtime(2);
            var title=FindObjectOfType<PythonTitleScreen>();var extra=title.GetComponent<TitleExtraPanel>();extra.Open();yield return null;Check(extra.Panel.activeSelf && extra.Entry.interactable,"Extra cannot open");
            Check(extra.Panel.GetComponentsInChildren<Button>(true).Count(b=>b.name!="タイトルへ戻る" && b.name!="CG一覧" && b.name!="エンディング" && b.interactable)==2,"Locked/unlocked list wrong");extra.OpenReplay(CafeRegularConversation.Id+"/accepted");Check(!extra.Replay.Showing && extra.Panel.activeSelf,"Locked direct entry opened");
            string state=JsonUtility.ToJson(backend.Simulation.State),meta=JsonUtility.ToJson(backend.Conversation);var profile=backend.Replays.Load();string snapshot=JsonUtility.ToJson(profile);
            string path=Path.Combine(output,"cases/slot-3.json"),save=File.ReadAllText(path);extra.OpenReplay(CafeIntakeConversation.Id+"/accepted");yield return null;var ui=extra.Replay;
            Check(ui.Showing && !extra.Panel.activeSelf,"Replay not shown");Check(ui.Panel.GetComponentsInChildren<Graphic>(true).All(g=>g.GetComponent<CanvasRenderer>()!=null),"Renderer absent");Check(!ui.Panel.GetComponentsInChildren<Button>(true).Any(b=>(b.name=="保存" || b.name=="読込" || b.name=="迎える" || b.name=="見送る") && b.gameObject.activeInHierarchy),"Replay exposed game actions");
            typeof(AcquisitionConversationPanel).GetField("revealed",Hidden).SetValue(ui,0f);ui.Next();ui.Next();var progress=(CafeConversationProgress)typeof(AcquisitionConversationPanel).GetField("progress",Hidden).GetValue(ui);Check(progress.node==0,"Same-frame skipped text");yield return null;ui.Next();Check(progress.node==1,"Next failed");ui.Answer(true);ui.Answer(false);yield return null;
            ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="バックログ").onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();Check(ui.HistoryScroll.content.rect.height>=ui.HistoryScroll.content.GetComponent<Text>().preferredHeight-1,"History clipped");ui.HistoryScroll.verticalNormalizedPosition=0;yield return null;ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="バックログ").onClick.Invoke();yield return null;
            progress.node=profile.entries.Single(e=>e.branch_id=="accepted").lines.Length;ui.Refresh();yield return null;ui.Next();yield return null;Check(!ui.Showing && extra.Panel.activeSelf,"Replay did not finish to list");
            extra.OpenReplay(CafeIntakeConversation.Id+"/declined");yield return null;Check(ui.Showing,"Second branch unavailable");ui.Close();yield return null;Check(extra.Panel.activeSelf && state==JsonUtility.ToJson(backend.Simulation.State) && meta==JsonUtility.ToJson(backend.Conversation) && save==File.ReadAllText(path) && snapshot==JsonUtility.ToJson(backend.Replays.Load()),"Replay mutated state, meta, save or unlocks");
            extra.Hide();title.ShowConditions();Check(title.conditionsPanel.activeSelf && !extra.Panel.activeSelf,"Title conditions navigation regressed");title.ShowHome();extra.Open();yield return null;completed=true;File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" Extra checks; zero Unity errors. Profile union, migration, locked branches, read-only ADV, backlog and title navigation.");
        }
    }
}
