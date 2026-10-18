using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class StandaloneEncounterConversationSmoke : MonoBehaviour
    {
        static readonly BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
        string output;int checks;LocalCafeBackend backend;bool completed,failed;readonly List<string> errors=new List<string>();
        public static CafeRules Rules(){var r=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);r.store_events.probability=0;r.dispatch_unlocks=r.dispatch_unlocks.Where(u=>u.id!=CafeEncounterDialogue.Destination).ToArray();return r;}
        public static LocalCafeBackend Create(string path)=>new LocalCafeBackend(path,Rules());
        public static void Good(LocalCafeBackend b,string kind,string choice=null,string id=null,int slot=1)
        {var reply=Send(b,kind,choice,id,slot);if(reply.result!=UnityWebRequest.Result.Success)throw new Exception(kind+": "+reply.error);}
        public static CafeResponse Send(LocalCafeBackend b,string kind,string choice=null,string id=null,int slot=1)=>StandaloneDispatchConversationSmoke.Raw(b,Payload(b,kind,choice,id,slot));
        static string Payload(LocalCafeBackend b,string kind,string choice,string id,int slot)=>JsonUtility.ToJson(new PythonShiftCommand{kind=kind,choice=choice,event_id=id,cat_id="cat-mike",save_id=slot.ToString(),save_slot=slot,instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,request_id=Guid.NewGuid().ToString("N")});
        public static string Setup(LocalCafeBackend b)
        {Good(b,"new_game","free");Good(b,"dispatch_general",CafeEncounterDialogue.Destination);Good(b,"day_off");string id=b.Simulation.State.dispatches.Single().id;if(!b.BeginEncounterConversation(id))throw new Exception("Encounter not started");return id;}
        public static int BackendChecks(string folder,bool restart)
        {
            int n=0;Action<bool,string> check=(ok,label)=>{n++;if(!ok)throw new Exception(label);};
            foreach(string branch in new[]{"accept","decline"})
            {
                var b=Create(Path.Combine(folder,branch));
                if(restart)foreach(int slot in new[]{1,2,3,4}){Good(b,"load_game",slot:slot);check(b.EncounterConversation!=null,"Restart meta lost");}
                string id=Setup(b);var p=b.EncounterConversation.progress;p.node=1;Good(b,"save_game",slot:1);Good(b,"load_game",slot:1);p=b.EncounterConversation.progress;check(p.node==1,"Intro position lost");p.node=4;Good(b,"save_game",slot:2);Good(b,"load_game",slot:2);check(b.EncounterConversation.progress.node==4,"Choice lost");
                b.EncounterConversationOpen=true;check(Send(b,"day_off").result==UnityWebRequest.Result.ProtocolError,"Open dialogue advanced");b.EncounterConversationOpen=false;check(Send(b,"day_off").result==UnityWebRequest.Result.ProtocolError,"Unanswered encounter advanced");
                double funds=b.Simulation.State.funds;string payload=Payload(b,"resolve_dispatch_choice",branch,id,1);var reply=StandaloneDispatchConversationSmoke.Raw(b,payload);check(reply.result==UnityWebRequest.Result.Success,"Answer failed");string after=JsonUtility.ToJson(b.Simulation.State);check(StandaloneDispatchConversationSmoke.Raw(b,payload).text==reply.text && after==JsonUtility.ToJson(b.Simulation.State),"Request replay applied effects");check(Send(b,"resolve_dispatch_choice",branch,id).result==UnityWebRequest.Result.ProtocolError && after==JsonUtility.ToJson(b.Simulation.State),"New duplicate request applied");
                p=b.EncounterConversation.progress;check(p.branch_id==branch && p.node==0 && b.EncounterResultPending && b.Simulation.State.funds==funds,"Result or early payout");p.node=1;Good(b,"save_game",slot:3);var fresh=Create(Path.Combine(folder,branch));Good(fresh,"load_game",slot:3);check(fresh.EncounterConversation.progress.node==1 && fresh.EncounterResultPending,"Fresh result resume");check(Send(b,"day_off").result==UnityWebRequest.Result.ProtocolError,"Hidden result advanced");
                p.node=3;CafeEncounterDialogue.Finish(p);CafeEncounterDialogue.Finish(p);Good(b,"save_game",slot:4);check(!b.EncounterResultPending && p.viewed_branches.Length==1,"Completion duplicated");
                var save=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(folder,branch,"slot-4.json")));save.slot=5;save.encounter_conversations=null;save.encounter_conversation_selected=null;File.WriteAllText(Path.Combine(folder,branch,"slot-5.json"),JsonUtility.ToJson(save));Good(b,"load_game",slot:5);check(!b.BeginEncounterConversation(id) && b.EncounterConversations.Length==0,"Legacy answered fabricated viewing");
                Good(b,"load_game",slot:2);check(b.EncounterConversation.progress.branch_id=="introduction","Waiting restore");save=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(folder,branch,"slot-2.json")));save.slot=6;save.encounter_conversations=null;save.encounter_conversation_selected=null;File.WriteAllText(Path.Combine(folder,branch,"slot-6.json"),JsonUtility.ToJson(save));Good(b,"load_game",slot:6);check(b.BeginEncounterConversation(id) && b.EncounterConversation.progress.node==0,"Legacy waiting not startable");
                save=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(folder,branch,"slot-3.json")));save.slot=7;save.encounter_conversations[0].progress.branch_id=branch=="accept"?"decline":"accept";File.WriteAllText(Path.Combine(folder,branch,"slot-7.json"),JsonUtility.ToJson(save));after=JsonUtility.ToJson(b.Simulation.State);check(Send(b,"load_game",slot:7).result==UnityWebRequest.Result.ProtocolError && after==JsonUtility.ToJson(b.Simulation.State),"Invalid branch replaced state");
                Good(b,"load_game",slot:4);double reward=b.Simulation.DispatchReceivedReward(b.Simulation.State.dispatches.Single());check(reward==b.Simulation.State.dispatches.Single().reward+(branch=="accept"?100:0),"Reward rule changed");Good(b,"day_off");double beforeReceipt=b.Simulation.State.funds;Good(b,"receive_general",id);check(b.Simulation.State.funds==beforeReceipt+reward,"Receipt accounting differs");
            }
            return n;
        }
        void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){errors.Add(message+"\n"+stack);if(completed)Fail(new Exception(message));}}
        void Fail(Exception e){if(failed)return;failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-encounter-conversation-check");if(i<0)return;
            var qa=new GameObject("Encounter conversation verification").AddComponent<StandaloneEncounterConversationSmoke>();DontDestroyOnLoad(qa);qa.output=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(qa.output);Application.runInBackground=true;Application.logMessageReceived+=qa.Log;
            try{qa.checks+=BackendChecks(Path.Combine(qa.output,"cases"),args.Contains("--encounter-restart"));qa.StartCoroutine(qa.Guard());}catch(Exception e){qa.Fail(e);}
        }
        void Apply(PythonStateClient c){typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(c,new object[]{backend.View()});Canvas.ForceUpdateCanvases();}
        IEnumerator Guard(){var run=Run();while(true){bool more;try{if(errors.Count>0)throw new Exception(string.Join("\n",errors));more=run.MoveNext();}catch(Exception e){Fail(e);yield break;}if(!more)yield break;yield return run.Current;}}
        IEnumerator Run()
        {
            backend=Create(Path.Combine(output,"ui"));CafeBackends.UseLocal(backend);SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
            var client=FindObjectOfType<PythonStateClient>();var owner=client.GetComponent<PythonDispatchChoicePanel>();var ui=owner.Dialogue;Check(!ui.Showing,"Initially visible");string id=Setup(backend);Apply(client);yield return null;
            Check(ui.Showing && !owner.panel.activeSelf,"Waiting ADV absent or old panel overlaps");Check(ui.Panel.GetComponentsInChildren<Graphic>(true).All(g=>g.GetComponent<CanvasRenderer>()!=null),"CanvasRenderer absent");Check(Vector2.Distance(ui.Panel.GetComponent<RectTransform>().rect.size,ui.Panel.GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect.size)<1,"Not full screen");
            typeof(AcquisitionConversationPanel).GetField("revealed",Hidden).SetValue(ui,0f);ui.Next();ui.Next();Check(backend.EncounterConversation.progress.node==0,"Same frame skipped text");yield return null;ui.Next();Check(backend.EncounterConversation.progress.node==1,"Next not advancing");
            ui.Close();Check(backend.Simulation.DispatchChoiceWaiting && !backend.EncounterConversationOpen && !client.CanDayOff,"Closing answered or released waiting");client.GetComponent<PythonEventListPanel>().ShowMenu(11);Check(ui.Showing && !owner.panel.activeSelf,"Event list raised old panel");
            backend.EncounterConversation.progress.node=4;Apply(client);yield return null;Check(ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="迎える").interactable && ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="見送る").interactable,"Choice unavailable");
            ui.Answer(true);ui.Answer(false);yield return new WaitForSecondsRealtime(1);Check(backend.EncounterConversation.progress.branch_id=="accept" && !client.CanDayOff,"UI repeated answer changed result");ui.Close();client.GetComponent<PythonEventListPanel>().ShowMenu(11);Check(ui.Showing && client.GetComponent<PythonEventListPanel>().PendingFor(11)>0,"Result stranded");
            ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="バックログ").onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();Check(ui.HistoryScroll.content.rect.height>=ui.HistoryScroll.content.GetComponent<Text>().preferredHeight-1,"History clipped");ui.HistoryScroll.verticalNormalizedPosition=0;yield return null;ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="バックログ").onClick.Invoke();yield return null;
            backend.EncounterConversation.progress.node=3;Apply(client);yield return null;ui.Next();yield return null;Check(!ui.Showing && !backend.EncounterResultPending && !client.Automatic,"Finish auto resumed or pending");
            string manual=Path.Combine(output,"manual");backend=Create(manual);if(Environment.GetCommandLineArgs().Contains("--encounter-manual-load"))Good(backend,"load_game",slot:8);else {Setup(backend);backend.EncounterConversation.progress.node=4;Good(backend,"save_game",slot:2);}CafeBackends.UseLocal(backend);Apply(client);yield return null;
            completed=true;File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" encounter checks; zero Unity errors.");
        }
    }
}
