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
    public sealed class StandaloneOptionsSmoke : MonoBehaviour
    {
        string output;int checks;bool completed,failed;readonly List<string> errors=new List<string>();
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        public static int StorageChecks(string directory)
        {
            Directory.CreateDirectory(directory);int n=0;Action<bool,string> check=(ok,label)=>{n++;if(!ok)throw new Exception(label);};
            string path=Path.Combine(directory,Guid.NewGuid().ToString("N")+".json");var store=new CafeOptionsStore(path);var d=store.Load();check(d.charactersPerSecond==40 && !d.instant && !d.fullscreen,"Missing options defaults");
            foreach(int speed in new[]{10,40,80}){d.charactersPerSecond=speed;d.instant=speed==80;d.fullscreen=speed==10;check(store.Save(d),"Save options");var read=new CafeOptionsStore(path).Load();check(JsonUtility.ToJson(d)==JsonUtility.ToJson(read),"Options roundtrip");}
            string good=File.ReadAllText(path);Directory.CreateDirectory(path+".tmp");d.charactersPerSecond=55;check(!store.Save(d) && File.ReadAllText(path)==good,"Write failure lost original");Directory.Delete(path+".tmp");
            File.WriteAllText(path,"broken");var recovered=new CafeOptionsStore(path);var backup=recovered.Load();check(recovered.Recovered && recovered.Notice!=null && backup.charactersPerSecond==40,"Backup fallback");check(recovered.Save(backup) && File.ReadAllText(path+".invalid")=="broken","Recovery did not preserve bad primary");
            File.WriteAllText(path,"bad primary");File.WriteAllText(path+".bak","bad backup");var invalid=new CafeOptionsStore(path);check(invalid.Load().charactersPerSecond==40 && invalid.Notice!=null && File.ReadAllText(path)=="bad primary","Both corrupt defaults mutated source");check(invalid.Save(new CafeOptionsData()) && File.ReadAllText(path+".invalid")=="bad primary","Explicit recovery failed");
            foreach(var data in new[]{new CafeOptionsData{version=2},new CafeOptionsData{charactersPerSecond=9},new CafeOptionsData{charactersPerSecond=81}}){bool rejected=false;try{data.Validate();}catch(ArgumentException){rejected=true;}check(rejected,"Invalid values accepted");}
            File.WriteAllText(path,"{}");File.Delete(path+".bak");var empty=new CafeOptionsStore(path);empty.Load();check(empty.Notice!=null,"Missing version accepted");return n;
        }
        void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        void Log(string m,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){errors.Add(m+"\n"+stack);if(completed)Fail(new Exception(m));}}
        void Fail(Exception e){if(failed)return;failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-options-check");if(i<0)return;var qa=new GameObject("Options verification").AddComponent<StandaloneOptionsSmoke>();DontDestroyOnLoad(qa);qa.output=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(qa.output);Application.runInBackground=true;Application.logMessageReceived+=qa.Log;
            try{qa.checks+=StorageChecks(Path.Combine(qa.output,"storage"));if(args.Contains("--options-restart"))qa.Check(CafeOptions.Current.charactersPerSecond==55 && CafeOptions.Current.instant && !CafeOptions.Current.fullscreen,"Restart settings lost");qa.StartCoroutine(qa.Guard());}catch(Exception e){qa.Fail(e);}
        }
        IEnumerator Guard(){var run=Run();while(true){bool more;try{if(errors.Count>0)throw new Exception(string.Join("\n",errors));more=run.MoveNext();}catch(Exception e){Fail(e);yield break;}if(!more)yield break;yield return run.Current;}}
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(2);var title=FindObjectOfType<PythonTitleScreen>();var options=title.GetComponent<GameOptionsPanel>();Check(options!=null && options.Entry.interactable,"Title options entry missing");var backend=StandaloneExtraConversationSmoke.Create(Path.Combine(output,"game-"+Guid.NewGuid().ToString("N")));CafeBackends.UseLocal(backend);string state=JsonUtility.ToJson(backend.Simulation.State);
            var original=CafeOptions.Current.Copy();options.Open();Check(GameOptionsPanel.IsOpen,"Open failed");options.Speed.value=10;options.Instant.isOn=!original.instant;options.Cancel();Check(JsonUtility.ToJson(original)==JsonUtility.ToJson(CafeOptions.Current) && !GameOptionsPanel.IsOpen,"Cancel applied draft");
            options.Open();options.Defaults();Check(options.Speed.value==40 && !options.Instant.isOn && !options.Fullscreen.isOn,"Defaults UI wrong");options.Speed.value=80;yield return new WaitForSecondsRealtime(.25f);Check(options.Preview.text.Length>0,"Speed preview empty");options.Instant.isOn=true;yield return null;Check(options.Preview.text.Contains("見ました。") && !options.Speed.interactable,"Instant preview failed");options.Instant.isOn=false;options.Speed.value=10;options.Save();yield return null;Check(CafeOptions.Current.charactersPerSecond==10 && !CafeOptions.Current.instant,"Save did not apply");
            Directory.CreateDirectory(CafeOptions.Store.Path+".tmp");options.Open();options.Speed.value=20;options.Save();Check(GameOptionsPanel.IsOpen && CafeOptions.Current.charactersPerSecond==10 && options.Status.text.Contains("保存できません"),"UI lost draft on save failure");options.Cancel();Directory.Delete(CafeOptions.Store.Path+".tmp");
            var extra=title.GetComponent<TitleExtraPanel>();var host=new GameObject("Options text fixture",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));host.transform.SetParent(title.transform,false);var rect=host.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var replay=title.gameObject.AddComponent<ReplayConversationPanel>();replay.Play(new CafeReplayEntry{conversation_id="options-qa",branch_id="accepted",title="文字表示の検証",cat_name="ミケ",personality="穏やか",lines=new[]{new CafeConversationLine("案内",new string('あ',300)+"終わり"),new CafeConversationLine("案内","次の文章です。")}},host,options.Entry.GetComponentInChildren<Text>().font,()=>host.SetActive(false));
            yield return new WaitForSecondsRealtime(.25f);float count=(float)typeof(AcquisitionConversationPanel).GetField("revealed",Hidden).GetValue(replay);Check(count>0 && count<15,"Slow text setting ignored");
            options.Open();float frozen=(float)typeof(AcquisitionConversationPanel).GetField("revealed",Hidden).GetValue(replay);yield return new WaitForSecondsRealtime(.3f);replay.Next();var p=(CafeConversationProgress)typeof(AcquisitionConversationPanel).GetField("progress",Hidden).GetValue(replay);Check(frozen==(float)typeof(AcquisitionConversationPanel).GetField("revealed",Hidden).GetValue(replay) && p.node==0,"Options advanced underlying dialogue");options.Instant.isOn=true;options.Save();yield return null;Check((float)typeof(AcquisitionConversationPanel).GetField("revealed",Hidden).GetValue(replay)>=303,"Instant did not reveal current line");yield return null;replay.Next();Check(p.node==1,"Instant next requires extra click");yield return null;replay.Close();Destroy(host);
            Check(state==JsonUtility.ToJson(backend.Simulation.State),"Title options changed game");
            SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);var preview=FindObjectOfType<ContentPreview>();var client=preview.GetComponent<PythonStateClient>();options=preview.GetComponent<GameOptionsPanel>();preview.GetComponent<PythonMenuPanel>().Open();Check(options.Entry.gameObject.activeInHierarchy,"Business entry inaccessible");StandaloneDispatchConversationSmoke.Good(backend,"start_business");yield return new WaitForSecondsRealtime(2.2f);Check(client.CanStartAutomatic,"Automatic fixture unavailable");client.ToggleAutomatic();Check(client.Automatic,"Automatic fixture did not start");state=JsonUtility.ToJson(backend.Simulation.State);options.Open();Check(GameOptionsPanel.IsOpen && !client.CanSendCommands && !client.CanProgressBusiness,"Business not blocked");client.ProgressBusiness();client.ToggleAutomatic();yield return new WaitForSecondsRealtime(.3f);Check(!client.Automatic && state==JsonUtility.ToJson(backend.Simulation.State),"Options progressed business");options.Cancel();Check(client.CanSendCommands && !client.Automatic,"Cancel did not release controls");
            options.Open();options.Fullscreen.isOn=true;options.Save();yield return new WaitForSecondsRealtime(1);Check(Screen.fullScreenMode==FullScreenMode.FullScreenWindow,"Fullscreen not applied");options.Open();options.Instant.isOn=true;options.Speed.SetValueWithoutNotify(55);options.Fullscreen.isOn=false;options.Save();yield return new WaitForSecondsRealtime(1);Check(Screen.fullScreenMode==FullScreenMode.Windowed && CafeOptions.Current.charactersPerSecond==55,"Window restore failed");
            SceneManager.LoadScene("CatCafeTitle");yield return new WaitForSecondsRealtime(2);options=FindObjectOfType<PythonTitleScreen>().GetComponent<GameOptionsPanel>();options.Open();Check(options.Speed.value==55 && options.Instant.isOn && !options.Fullscreen.isOn,"Scene transition lost options");completed=true;File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" options checks; zero Unity errors. Persistence, recovery, preview, dialogue, modal blocking, screen mode and scene transition.");
        }
    }
}
