using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
namespace CatCafePreview
{
    // Explicit QA only; each fixture keeps its existing rules and isolated save store.
    public sealed class StandaloneIntegratedSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-integrated-check");if(i<0)return;
            var go=new GameObject("Integrated regression").AddComponent<StandaloneIntegratedSmoke>();DontDestroyOnLoad(go);
            go.StartCoroutine(go.Run(Path.GetFullPath(args[i+1]),args.Contains("--integrated-resume")));
        }
        IEnumerator Run(string root,bool restart)
        {
            Directory.CreateDirectory(root);int failures=0,total=0;
            foreach(string name in new[]{"Opening","GrowthBatch","Outcome","Ending","BondEnding","PatronEnding","DispatchConversation","RoundTripConversation","PatronConversation","EncounterConversation","DispatchIntroductionConversation","LodgeConversation","AdoptionConversation","TrustConversation","MissingConversation","ReservationConversation","EquipmentConversation","SupportConversation","CocoConversation","ExtraConversation","CGGallery","StoreNotice","LegacyImport","Autoplay"})
            {
                string result;try
                {
                    var type=typeof(StandaloneIntegratedSmoke).Assembly.GetType("CatCafePreview.Standalone"+name+"Smoke",true);
                    var method=type.GetMethods(BindingFlags.Public|BindingFlags.Static).Single(m=>new[]{"BackendChecks","BackendCases","RunChecks","Run"}.Contains(m.Name));
                    // CG restart needs observations produced by its full rendered runner, not BackendChecks.
                    var p=method.GetParameters();object[] values=p.Length==2?new object[]{Path.Combine(root,name),restart && name!="CGGallery"}:new object[]{Path.Combine(root,name)};
                    int n=(int)method.Invoke(null,values);total+=n;result="PASS "+n+(name=="CGGallery"?" backend only; rendered restart uses --catcafe-cg-gallery-check --cg-restart":"");
                }
                catch(Exception ex){failures++;result="FAIL "+(ex.InnerException??ex);}
                File.WriteAllText(Path.Combine(root,name+(restart?"-resume":"")+".txt"),result);
                yield return null;
            }
            try{int n=AutoplayPauseChecks(Path.Combine(root,"AutoplayPause"+(restart?"-resume":"")));total+=n;File.WriteAllText(Path.Combine(root,"AutoplayPause"+(restart?"-resume":"")+".txt"),"PASS "+n);}catch(Exception ex){failures++;File.WriteAllText(Path.Combine(root,"AutoplayPause-failure.txt"),ex.ToString());}
            File.WriteAllText(Path.Combine(root,restart?"summary-resume.txt":"summary.txt"),"checks="+total+" failures="+failures);
            Application.Quit(failures==0?0:1);
        }
        public static int AutoplayPauseChecks(string root)
        {
            Directory.CreateDirectory(root);var rules=StandaloneReservationConversationSmoke.Rules();
            var b=new LocalCafeBackend(root,rules);Action<string> send=k=>
            {
                if(k!="start_autoplay"){StandaloneDispatchConversationSmoke.Good(b,k);return;}
                var state=b.Simulation.State;var reply=StandaloneDispatchConversationSmoke.Raw(b,JsonUtility.ToJson(new PythonShiftCommand{kind=k,request_id=Guid.NewGuid().ToString("N"),instance_id=state.instance_id,expected_revision=state.revision,working_cats=new string[0],autoplay_objective="popularity",autoplay_mode="fast",autoplay_days=10}));
                if(reply.result!=UnityEngine.Networking.UnityWebRequest.Result.Success)throw new Exception(k+": "+reply.error);
            };
            StandaloneDispatchConversationSmoke.Good(b,"new_game","popularity");
            send("day_off");send("continue_goal");send("advance_goal");send("start_autoplay");
            int safety=0;while(!b.Simulation.ReservationWaiting && safety++<1000)
            {
                if(!b.Simulation.State.autoplay.running){if(b.Simulation.GoalWaiting)send("continue_goal");if(b.Simulation.State.goal.Last().status=="cleared" && b.Simulation.State.goal.Count<rules.goal_stages.Length)send("advance_goal");send("start_autoplay");}
                send("step_autoplay");
            }
            int n=0;Action<bool,string> check=(ok,label)=>{n++;if(!ok)throw new Exception(label);};
            check(b.Simulation.ReservationWaiting && b.Simulation.State.autoplay.running,"Reservation during live autoplay not reproduced");
            // Match the UI opening its pending conversation before the next automatic step.
            b.PrepareReservationConversation();b.ReservationConversationOpen=true;
            var s=b.Simulation.State;double funds=s.funds;int day=s.day,tick=s.tick,operations=s.autoplay.operations;
            string json=JsonUtility.ToJson(new PythonShiftCommand{kind="step_autoplay",request_id=Guid.NewGuid().ToString("N"),instance_id=s.instance_id,expected_revision=s.revision,working_cats=new string[0]});
            var response=StandaloneDispatchConversationSmoke.Raw(b,json);
            s=b.Simulation.State;
            check(response.result==UnityEngine.Networking.UnityWebRequest.Result.Success && b.Simulation.State.autoplay.paused,"Conversation did not pause autoplay: "+response.error);
            check(s.day==day && s.tick==tick && s.funds==funds && s.autoplay.operations==operations,"Pause advanced gameplay");
            string paused=JsonUtility.ToJson(s);StandaloneDispatchConversationSmoke.Raw(b,json);check(paused==JsonUtility.ToJson(s),"Repeated pause request mutated gameplay");
            send("save_game");b=new LocalCafeBackend(root,rules);send("load_game");check(b.Simulation.State.autoplay.paused && b.Simulation.ReservationWaiting,"Paused reservation restart lost");
            s=b.Simulation.State;response=StandaloneDispatchConversationSmoke.Raw(b,JsonUtility.ToJson(new PythonShiftCommand{kind="resolve_reservation",choice="decline",event_id=Standalone.CafeReservationDialogue.EventId(s.reservation),request_id=Guid.NewGuid().ToString("N"),instance_id=s.instance_id,expected_revision=s.revision,working_cats=new string[0]}));
            check(response.result==UnityEngine.Networking.UnityWebRequest.Result.Success,"Paused reservation answer failed: "+response.error);StandaloneReservationConversationSmoke.Finish(b);
            send("resume_autoplay");check(!b.Simulation.State.autoplay.paused,"Resolved conversation could not resume");send("stop_autoplay");check(!b.Simulation.State.autoplay.running,"Stop after conversation failed");return n;
        }
    }
}
