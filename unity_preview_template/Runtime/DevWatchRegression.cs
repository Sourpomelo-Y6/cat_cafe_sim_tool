#if CATCAFE_DEV_ACQUISITION
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    // Explicitly opted-in Player regression checks; never installed by normal watch runs.
    [DefaultExecutionOrder(-10000)]
    public sealed class DevWatchRegression : MonoBehaviour
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        const string Injected="QA: intentional watch stop";
        const string DemoDigest="742057522db322227169fcb4032db9e2043a667726d144d11cf3f2647e16aa64";
        [Serializable] sealed class Result {public string mode,status="PASS",digest,dialogue,save_sha256;public int checks,day;}
        DevWatchSession watch;LocalCafeBackend backend;string mode,root;bool hooked,raceSeen,finished;int phase,checks,probeFrames,probeCommands;
        float began;string oldCheckpoint,oldSave;Result expected;
        T Get<T>(object o,string name)=>(T)o.GetType().GetField(name,Hidden).GetValue(o);
        void Set(object o,string name,object value)=>o.GetType().GetField(name,Hidden).SetValue(o,value);
        void Call(string name,params object[] args)=>typeof(DevWatchSession).GetMethod(name,Hidden).Invoke(watch,args);
        string SaveFile=>(string)typeof(DevWatchSession).GetProperty("SaveFile",Hidden).GetValue(watch);
        string CheckpointFile=>Path.Combine(root,"watch-checkpoint.json");
        PythonStateClient Client=>FindObjectOfType<PythonStateClient>();
        string Digest=>CafeReplayJournal.Digest(backend.Simulation.State);
        void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--dev-watch-regression");if(i<0)return;
            var watch=FindObjectOfType<DevWatchSession>();if(watch==null)throw new Exception("Regression requires isolated watch mode");
            var host=new GameObject("Watch regression").AddComponent<DevWatchRegression>();DontDestroyOnLoad(host);
            host.watch=watch;host.mode=args[i+1];host.root=host.Get<string>(watch,"root");host.backend=host.Get<LocalCafeBackend>(watch,"backend");host.began=Time.realtimeSinceStartup;
            if(host.mode=="resume-error")host.expected=JsonUtility.FromJson<Result>(File.ReadAllText(Path.Combine(host.root,"watch-regression.json")));
            if(host.mode=="exit-error")host.backend.CommandObserved+=host.AfterCommand;
        }
        void Update()
        {
            if(finished)return;
            try{if(Time.realtimeSinceStartup-began>180)throw new Exception("Regression timed out");Tick();}
            catch(Exception e){finished=true;File.WriteAllText(Path.Combine(root,"watch-regression-failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);}
        }
        void Tick()
        {
            var c=Client;if(c==null || c.CurrentState==null)return;
            if(mode=="resume-error")
            {
                if(!Get<bool>(watch,"loaded") || !c.UIStateReady)return;
                if(phase==0){Check(Get<bool>(watch,"paused"),"Resume must stay paused");Check(Digest==expected.digest,"Saved gameplay changed on reload");var d=c.GetComponents<AcquisitionConversationPanel>().LastOrDefault(p=>p.Showing);Check((d?.PlaybackKey??"")==expected.dialogue,"Saved dialogue changed on reload");probeCommands=Get<int>(watch,"commands");phase=1;}
                if(phase==1){Check(Get<bool>(watch,"paused") && Get<int>(watch,"commands")==probeCommands && Digest==expected.digest,"Paused restore advanced gameplay");if(++probeFrames>=3){Set(watch,"paused",false);Set(watch,"lastChange",Time.realtimeSinceStartup);phase=2;}return;}
                if(phase==2 && Get<bool>(watch,"complete")){Check(Digest==DemoDigest,"Resumed run differs from uninterrupted eight-day demo");Finish();}
                return;
            }
            if(!hooked && c.UIStateReady)
            {
                var d=c.GetComponent<EquipmentConversationPanel>();
                if(d!=null && d.Showing && d.PlaybackChoosing && c.CurrentState.store_event?.kind=="trouble")
                {
                    var repair=d.PlaybackHost.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.gameObject.activeInHierarchy && b.IsInteractable() && b.GetComponentInChildren<Text>()?.text=="修理");
                    if(repair!=null){hooked=true;repair.onClick.AddListener(()=>{try{ProbeBusyFrame(c);}catch(Exception e){finished=true;File.WriteAllText(Path.Combine(root,"watch-regression-failure.txt"),e.ToString());Application.Quit(1);}});}
                }
            }
            if(!raceSeen || !c.UIStateReady)return;
            if(mode=="race"){Check(Get<string>(watch,"error")=="" && !c.GetComponent<EquipmentConversationPanel>().PlaybackChoosing,"Repair response caused false stop or did not reach result");Finish();return;}
            if(phase==0)
            {
                Set(watch,"paused",true);
                if(mode=="save-failure"){Call("RequestSave",false);phase=1;return;}
                if(mode=="exit-error")Set(watch,"exitOnComplete",true);
                Call("Fail",Injected);Check(Get<string>(watch,"error")==Injected,"Stop reason missing");
                if(mode=="exit-error"){phase=9;return;}
                Call("RequestSave",false);phase=5;return;
            }
            if(mode=="save-failure")
            {
                if(phase==1 && Saved())
                {
                    oldCheckpoint=CafeDevAcquisition.Hash(File.ReadAllBytes(CheckpointFile));oldSave=CafeDevAcquisition.Hash(File.ReadAllBytes(SaveFile));
                    Directory.CreateDirectory(SaveFile+".tmp");Set(watch,"exitOnComplete",true);Call("Fail",Injected);Call("RequestSave",true);phase=2;return;
                }
                if(phase==2 && Get<string>(watch,"saveError")!="")
                {
                    Check(!Get<bool>(watch,"saved") && !Get<bool>(watch,"saving") && !Get<bool>(watch,"saveRequested"),"Failed save claimed completion");
                    Check(Get<string>(watch,"error")==Injected,"Save failure replaced original stop reason");
                    Check(CafeDevAcquisition.Hash(File.ReadAllBytes(SaveFile))==oldSave && CafeDevAcquisition.Hash(File.ReadAllBytes(CheckpointFile))==oldCheckpoint,"Failed save replaced last good checkpoint");
                    Directory.Delete(SaveFile+".tmp");Call("RequestSave",false);phase=3;return;
                }
                if(phase==3 && Saved())
                {
                    Check(Get<string>(watch,"saveError")=="","Save retry retained failure");oldCheckpoint=CafeDevAcquisition.Hash(File.ReadAllBytes(CheckpointFile));
                    oldSave=CafeDevAcquisition.Hash(File.ReadAllBytes(SaveFile));bool resumed=Get<bool>(watch,"resume"),loaded=Get<bool>(watch,"loaded");
                    Set(watch,"resume",true);Set(watch,"loaded",false);Call("RequestSave",true);
                    Check(!Get<bool>(watch,"saveRequested") && !Get<bool>(watch,"saving") && Get<string>(watch,"saveError")!="","Unrestored load was allowed to overwrite save");
                    Check(CafeDevAcquisition.Hash(File.ReadAllBytes(CheckpointFile))==oldCheckpoint && CafeDevAcquisition.Hash(File.ReadAllBytes(SaveFile))==oldSave,"Unrestored load changed checkpoint");
                    Set(watch,"resume",resumed);Set(watch,"loaded",loaded);
                    Call("RequestSave",false);Set(watch,"saveStarted",Time.realtimeSinceStartup-31);phase=4;return;
                }
                if(phase==4 && Get<string>(watch,"saveError").Contains("30"))
                {
                    Check(CafeDevAcquisition.Hash(File.ReadAllBytes(CheckpointFile))==oldCheckpoint,"Waiting timeout changed checkpoint");
                    Call("RequestSave",false);phase=5;return;
                }
            }
            if(phase==5 && Saved()){Check(Get<string>(watch,"error")==Injected,"Successful save cleared stop reason");ValidateSave();Finish();}
        }
        bool Saved()=>Get<bool>(watch,"saved") && !Get<bool>(watch,"saving") && !Get<bool>(watch,"saveRequested");
        void ProbeBusyFrame(PythonStateClient c)
        {
            if(raceSeen)return;
            Check(!c.UIStateReady,"Repair did not queue a real command");Call("Choose");Check(Get<string>(watch,"error")=="","Queued command was mistaken for missing answer");
            // Reproduce the gap after pending clears but before /state, /saves and button refresh finish.
            var pending=Get<object>(c,"pending");bool refresh=Get<bool>(c,"refreshingState");
            try{Set(c,"pending",null);Set(c,"refreshingState",true);Check(c.CanSendCommands && !c.UIStateReady,"Stale UI fixture was not busy");Call("Choose");Check(Get<string>(watch,"error")=="","Stale disabled buttons caused false stop");}
            finally{Set(c,"pending",pending);Set(c,"refreshingState",refresh);}
            raceSeen=true;
        }
        void ValidateSave()
        {
            var cp=JsonUtility.FromJson<DevWatchSession.Checkpoint>(File.ReadAllText(CheckpointFile));
            Check(cp.save_sha256==CafeDevAcquisition.Hash(File.ReadAllBytes(SaveFile)),"Checkpoint hash does not match save");Check(cp.state_digest==Digest,"Checkpoint gameplay mismatch");
            Check(File.Exists(Path.Combine(root,"watch-failure.json")),"Stop reason was not retained");
        }
        void AfterCommand(string json,CafeResponse response)
        {
            if(phase!=9 || JsonUtility.FromJson<PythonShiftCommand>(json).kind!="save_game")return;
            try{Check(response.result==UnityWebRequest.Result.Success && Get<bool>(watch,"saved"),"Exit happened before successful save");ValidateSave();Finish(false);}
            catch(Exception e){File.WriteAllText(Path.Combine(root,"watch-regression-failure.txt"),e.ToString());Application.Quit(1);}
        }
        void Finish(bool quit=true)
        {
            finished=true;var d=Client.GetComponents<AcquisitionConversationPanel>().LastOrDefault(p=>p.Showing);
            File.WriteAllText(Path.Combine(root,"watch-regression.json"),JsonUtility.ToJson(new Result{mode=mode,checks=checks,day=backend.Simulation.State.day,digest=Digest,dialogue=d?.PlaybackKey??"",save_sha256=File.Exists(SaveFile)?CafeDevAcquisition.Hash(File.ReadAllBytes(SaveFile)):""},true));if(quit)Application.Quit(0);
        }
    }
}
#endif
