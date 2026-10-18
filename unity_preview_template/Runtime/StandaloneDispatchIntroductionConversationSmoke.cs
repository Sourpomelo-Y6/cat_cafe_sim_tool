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
    public sealed class StandaloneDispatchIntroductionConversationSmoke : MonoBehaviour
    {
        static readonly BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
        string output;int checks;LocalCafeBackend backend;bool completed,failed;readonly List<string> errors=new List<string>();
        public static LocalCafeBackend Create(string path)=>new LocalCafeBackend(path,StandaloneEncounterConversationSmoke.Rules());
        public static string Payload(LocalCafeBackend b,string kind,string choice=null,string id=null,int slot=1,string cat="cat-mike")=>JsonUtility.ToJson(new PythonShiftCommand{kind=kind,choice=choice,event_id=id,cat_id=cat,save_id=slot.ToString(),save_slot=slot,instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,request_id=Guid.NewGuid().ToString("N")});
        public static CafeResponse Send(LocalCafeBackend b,string kind,string choice=null,string id=null,int slot=1,string cat="cat-mike")=>StandaloneDispatchConversationSmoke.Raw(b,Payload(b,kind,choice,id,slot,cat));
        public static void Good(LocalCafeBackend b,string kind,string choice=null,string id=null,int slot=1,string cat="cat-mike"){var reply=Send(b,kind,choice,id,slot,cat);if(reply.result!=UnityWebRequest.Result.Success)throw new Exception(kind+": "+reply.error);}
        public static string Setup(LocalCafeBackend b)
        {
            Good(b,"new_game","free");Good(b,"dispatch_general",CafeDispatchIntroductionDialogue.Destination);Good(b,"day_off");string id=b.Simulation.State.dispatches.Single().id;Good(b,"resolve_dispatch_choice","decline",id);Good(b,"day_off");Good(b,"receive_general",id);
            if(!b.BeginDispatchIntroductionConversation(id))throw new Exception("No introduction");return id;
        }
        public static int BackendChecks(string folder,bool restart)
        {
            int n=0;Action<bool,string> check=(ok,label)=>{n++;if(!ok)throw new Exception(label);};
            foreach(string answer in new[]{"accept","decline"})
            {
                string path=Path.Combine(folder,answer);var b=Create(path);
                if(restart)foreach(int slot in new[]{1,2,3,4}){Good(b,"load_game",slot:slot);check(b.DispatchIntroductionConversation!=null,"Restart lost meta");}
                string id=Setup(b),candidate=b.Simulation.State.dispatches.Single().introduction.cat_id;var p=b.DispatchIntroductionConversation.progress;
                p.node=2;Good(b,"save_game",slot:1);Good(b,"load_game",slot:1);check(b.DispatchIntroductionConversation.progress.node==2,"Intro lost");p=b.DispatchIntroductionConversation.progress;p.node=5;Good(b,"save_game",slot:2);Good(b,"load_game",slot:2);check(b.DispatchIntroductionConversation.progress.node==5,"Choice lost");check(Send(b,"day_off").result==UnityWebRequest.Result.ProtocolError,"Unanswered advanced");
                double funds=b.Simulation.State.funds,popularity=b.Simulation.State.popularity,cost=200;int count=b.Simulation.State.cats.Count;var cat=b.Simulation.State.dispatches.Single().introduction;
                b.Simulation.State.funds=cost;check(!b.View().dispatch_introductions.Single().can_accept && b.View().dispatch_introductions.Single().can_decline,"Funds gate missing");b.Simulation.State.funds=funds;
                int capacity=b.Simulation.Rules.housing.initial_capacity;b.Simulation.Rules.housing.initial_capacity=count;check(!b.View().dispatch_introductions.Single().can_accept && b.View().dispatch_introductions.Single().can_decline,"Capacity gate missing");b.Simulation.Rules.housing.initial_capacity=capacity;
                string payload=Payload(b,"resolve_dispatch_introduction",answer,id,cat:candidate);var reply=StandaloneDispatchConversationSmoke.Raw(b,payload);check(reply.result==UnityWebRequest.Result.Success,"Answer failed");string state=JsonUtility.ToJson(b.Simulation.State);check(StandaloneDispatchConversationSmoke.Raw(b,payload).text==reply.text && state==JsonUtility.ToJson(b.Simulation.State),"Replay duplicated admission");check(Send(b,"resolve_dispatch_introduction",answer,id,cat:candidate).result==UnityWebRequest.Result.ProtocolError && state==JsonUtility.ToJson(b.Simulation.State),"New duplicate mutated state");
                p=b.DispatchIntroductionConversation.progress;check(p.branch_id==(answer=="accept"?"accepted":"declined") && b.Simulation.State.funds==funds-(answer=="accept"?cost:0) && b.Simulation.State.cats.Count==count+(answer=="accept"?1:0) && b.Simulation.State.popularity==popularity,"Joining terms changed");
                if(answer=="accept"){var joined=b.Simulation.State.cats.Single(c=>c.id==candidate);check(joined.health=="healthy" && joined.stamina==b.Simulation.MaxStamina(candidate) && joined.fatigue==0 && joined.stress==0 && !joined.working && joined.activity=="cafe","Joined status differs");}
                p.node=1;Good(b,"save_game",slot:3);var fresh=Create(path);Good(fresh,"load_game",slot:3);check(fresh.DispatchIntroductionConversation.progress.node==1 && fresh.DispatchIntroductionResultPending,"Fresh result failed");check(Send(b,"day_off").result==UnityWebRequest.Result.ProtocolError,"Hidden result advanced");p.node=4;CafeDispatchIntroductionDialogue.Finish(p);CafeDispatchIntroductionDialogue.Finish(p);Good(b,"save_game",slot:4);check(p.viewed_branches.Length==1 && !b.DispatchIntroductionResultPending,"Completion duplicated");
                foreach(int oldSlot in new[]{2,4})
                {var save=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(path,"slot-"+oldSlot+".json")));save.slot=oldSlot+4;save.dispatch_introduction_conversations=null;save.dispatch_introduction_conversation_selected=null;File.WriteAllText(Path.Combine(path,"slot-"+save.slot+".json"),JsonUtility.ToJson(save));Good(b,"load_game",slot:save.slot);check(b.DispatchIntroductionConversations.Length==0,"Old save fabricated viewing");check(b.BeginDispatchIntroductionConversation(id)==(oldSlot==2),"Old save dialogue policy");}
                Good(b,"load_game",slot:2);var bad=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(path,"slot-3.json")));bad.slot=7;bad.dispatch_introduction_conversations[0].progress.node=99;File.WriteAllText(Path.Combine(path,"slot-7.json"),JsonUtility.ToJson(bad));state=JsonUtility.ToJson(b.Simulation.State);string meta=JsonUtility.ToJson(b.DispatchIntroductionConversation);check(Send(b,"load_game",slot:7).result==UnityWebRequest.Result.ProtocolError && state==JsonUtility.ToJson(b.Simulation.State) && meta==JsonUtility.ToJson(b.DispatchIntroductionConversation),"Bad load replaced live state");
                Good(b,"load_game",slot:4);Good(b,"dispatch_general",CafeDispatchIntroductionDialogue.Destination);Good(b,"day_off");string second=b.Simulation.State.dispatches.Last().id;Good(b,"resolve_dispatch_choice","decline",second);Good(b,"resolve_intake","decline");Good(b,"day_off");Good(b,"receive_general",second);b.BeginDispatchIntroductionConversation(second);check(b.DispatchIntroductionConversations.Length==2 && b.DispatchIntroductionConversations[0].progress.completed && b.Simulation.State.dispatches.Last().introduction.cat_id!=candidate,"Later candidate overwrote history");
            }
            return n;
        }
        void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){errors.Add(message+"\n"+stack);if(completed)Fail(new Exception(message));}}
        void Fail(Exception e){if(failed)return;failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-dispatch-introduction-conversation-check");if(i<0)return;
            var qa=new GameObject("Dispatch introduction conversation verification").AddComponent<StandaloneDispatchIntroductionConversationSmoke>();DontDestroyOnLoad(qa);qa.output=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(qa.output);Application.runInBackground=true;Application.logMessageReceived+=qa.Log;
            try{qa.checks+=BackendChecks(Path.Combine(qa.output,"cases"),args.Contains("--dispatch-introduction-restart"));qa.StartCoroutine(qa.Guard());}catch(Exception e){qa.Fail(e);}
        }
        void Apply(PythonStateClient c){typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(c,new object[]{backend.View()});Canvas.ForceUpdateCanvases();}
        IEnumerator Guard(){var run=Run();while(true){bool more;try{if(errors.Count>0)throw new Exception(string.Join("\n",errors));more=run.MoveNext();}catch(Exception e){Fail(e);yield break;}if(!more)yield break;yield return run.Current;}}
        IEnumerator Run()
        {
            backend=Create(Path.Combine(output,"ui"));CafeBackends.UseLocal(backend);SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
            var client=FindObjectOfType<PythonStateClient>();var owner=client.GetComponent<PythonDispatchIntroductionPanel>();var ui=owner.Dialogue;Check(!ui.Showing,"Initially visible");
            foreach(bool yes in new[]{false,true})
            {
                Setup(backend);Apply(client);yield return null;Check(ui.Showing && !owner.panel.activeSelf,"ADV absent or overlap");Check(ui.Panel.GetComponentsInChildren<Graphic>(true).All(g=>g.GetComponent<CanvasRenderer>()!=null),"CanvasRenderer absent");Check(Vector2.Distance(ui.Panel.GetComponent<RectTransform>().rect.size,ui.Panel.GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect.size)<1,"Not full screen");
                typeof(AcquisitionConversationPanel).GetField("revealed",Hidden).SetValue(ui,0f);ui.Next();ui.Next();Check(backend.DispatchIntroductionConversation.progress.node==0,"Same-frame skip");yield return null;ui.Next();Check(backend.DispatchIntroductionConversation.progress.node==1,"Next failed");ui.Close();Check(backend.Simulation.DispatchIntroductionWaiting && !client.CanDayOff,"Close answered or advanced");client.GetComponent<PythonEventListPanel>().ShowMenu(13);Check(ui.Showing && !owner.panel.activeSelf,"List raises legacy UI");
                backend.DispatchIntroductionConversation.progress.node=5;Apply(client);yield return null;var yesButton=ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="迎える");var noButton=ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="見送る");double funds=backend.Simulation.State.funds;backend.Simulation.State.funds=200;Apply(client);Check(!yesButton.interactable && noButton.interactable,"Insufficient funds UI");backend.Simulation.State.funds=funds;int cap=backend.Simulation.Rules.housing.initial_capacity;backend.Simulation.Rules.housing.initial_capacity=5;Apply(client);Check(!yesButton.interactable && noButton.interactable,"Full capacity UI");backend.Simulation.Rules.housing.initial_capacity=cap;Apply(client);
                ui.HousingButton.onClick.Invoke();yield return null;var h=client.GetComponent<PythonHousingPanel>();Check(h.panel.activeSelf && client.GetComponent<PythonCatAcquisitionNavigation>().HousingFrom(ui.Panel),"Housing navigation missing");Apply(client);Check(h.panel.transform.GetSiblingIndex()>ui.Panel.transform.GetSiblingIndex(),"ADV covered housing on refresh");h.close.onClick.Invoke();yield return null;Check(ui.Showing && !h.panel.activeSelf && backend.DispatchIntroductionConversation.progress.node==5,"Housing return lost position");
                ui.Answer(yes);ui.Answer(!yes);yield return new WaitForSecondsRealtime(1);Check(backend.DispatchIntroductionConversation.progress.branch_id==(yes?"accepted":"declined") && backend.Simulation.State.funds==funds-(yes?200:0) && !client.CanDayOff,"Repeated UI answer or early release");ui.Close();client.GetComponent<PythonEventListPanel>().ShowMenu(13);Check(ui.Showing && client.GetComponent<PythonEventListPanel>().PendingFor(13)>0,"Result stranded");
                ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="バックログ").onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();Check(ui.HistoryScroll.content.rect.height>=ui.HistoryScroll.content.GetComponent<Text>().preferredHeight-1,"Long history clipped");ui.HistoryScroll.verticalNormalizedPosition=0;yield return null;ui.Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="バックログ").onClick.Invoke();yield return null;
                backend.DispatchIntroductionConversation.progress.node=4;Apply(client);yield return null;ui.Next();yield return null;Check(!ui.Showing && !backend.DispatchIntroductionResultPending && !client.Automatic,"Finish auto resumed or blocked");owner.Open();Check(owner.panel.activeSelf && !ui.Showing,"Completed record cannot open");owner.Close();
            }
            string manual=Path.Combine(output,"manual");backend=Create(manual);if(Environment.GetCommandLineArgs().Contains("--dispatch-introduction-manual-load"))Good(backend,"load_game",slot:8);else {Setup(backend);backend.DispatchIntroductionConversation.progress.node=5;Good(backend,"save_game",slot:2);}CafeBackends.UseLocal(backend);Apply(client);yield return null;
            completed=true;File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" dispatch introduction conversation checks; zero Unity errors.");
        }
    }
}
