using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class StandaloneRegularConversationSmoke : MonoBehaviour
    {
        static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        string output;int checks;bool failed,completed;LocalCafeBackend backend;readonly List<string> errors=new List<string>();
        void Check(bool valid,string label){checks++;if(!valid)throw new Exception(label);}
        void Log(string message,string stack,LogType type){if(type!=LogType.Error && type!=LogType.Exception && type!=LogType.Assert)return;errors.Add(message+"\n"+stack);if(completed && !failed)Fail(new Exception(message));}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Fail(Exception e){failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);}
        public static CafeResponse Command(LocalCafeBackend b,string kind,string choice=null,int slot=1)
        {
            CafeResponse reply=null;var s=b.Simulation.State;
            var cmd=new PythonShiftCommand{kind=kind,choice=choice,save_slot=slot,save_id=slot.ToString(),cat_id=kind=="resolve_regular_introduction"?b.Simulation.Rules.regular_introduction.cat.id:"",working_cats=new string[0],request_id=Guid.NewGuid().ToString("N"),instance_id=s.instance_id,expected_revision=s.revision};
            var request=b.Request("/commands",JsonUtility.ToJson(cmd),r=>reply=r);while(request.MoveNext()){}return reply;
        }
        void Good(LocalCafeBackend b,string kind,string choice=null,int slot=1){var reply=Command(b,kind,choice,slot);Check(reply.result==UnityWebRequest.Result.Success,kind+": "+reply.text);}
        void Rejected(LocalCafeBackend b,string kind){string before=JsonUtility.ToJson(b.Simulation.State);Check(Command(b,kind).result==UnityWebRequest.Result.ProtocolError && before==JsonUtility.ToJson(b.Simulation.State),"Rejected "+kind+" changed state");}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-regular-conversation-check");if(i<0 || FindObjectOfType<StandaloneRegularConversationSmoke>()!=null)return;
            var go=new GameObject("Regular conversation verification");DontDestroyOnLoad(go);var runner=go.AddComponent<StandaloneRegularConversationSmoke>();runner.output=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(runner.output);Application.runInBackground=true;Application.logMessageReceived+=runner.Log;
            try{runner.VerifyDisk(Array.IndexOf(args,"--regular-conversation-restart")>=0);runner.StartCoroutine(runner.Guard());}catch(Exception e){runner.Fail(e);}
        }
        void VerifyDisk(bool restart)
        {
            string fixtures=Path.Combine(output,"rules");if(!restart)Check(StandaloneRegularIntroductionSmoke.Run(fixtures)>0,"Existing regular game rules");
            foreach(string choice in new[]{"accept","decline"})
            {
                string path=Path.Combine(fixtures,choice);var b=new LocalCafeBackend(path);
                if(restart)
                {
                    foreach(int slot in new[]{5,6,7,8}){Good(b,"load_game",slot:slot);Check(b.RegularConversation!=null,"Restart lost conversation");CafeRegularConversation.Validate(b.RegularConversation,b.Simulation.State.regular_introduction.status);}
                    Check(b.RegularConversation.completed && b.RegularConversation.viewed_branches.Length==1,"Restart completion lost");
                }
                Good(b,"load_game",slot:2);Check(b.RegularConversation==null,"Old pending save fabricated viewing");b.BeginRegularConversation();
                foreach(int node in new[]{0,2,5}){b.RegularConversation.node=node;Good(b,"save_game",slot:node==5?6:5);var fresh=new LocalCafeBackend(path);Good(fresh,"load_game",slot:node==5?6:5);Check(fresh.RegularConversation.node==node,"Introduction position lost");}
                double funds=b.Simulation.State.funds;int cats=b.Simulation.State.cats.Count;Good(b,"resolve_regular_introduction",choice);Check(b.RegularConversation.branch_id==(choice=="accept"?"accepted":"declined"),"Result sync failed");
                for(int node=0;node<=4;node++){b.RegularConversation.node=node;Good(b,"save_game",slot:7);Good(b,"load_game",slot:7);string before=JsonUtility.ToJson(b.Simulation.State);for(int n=0;n<5;n++)Good(b,"resolve_regular_introduction",choice);Check(before==JsonUtility.ToJson(b.Simulation.State),"Duplicate result changes gameplay");Rejected(b,"day_off");}
                Check(b.Simulation.State.cats.Count==cats+(choice=="accept"?1:0) && b.Simulation.State.funds==funds-(choice=="accept"?b.Simulation.Rules.regular_introduction.cost:0),"Payment or admission duplicated");
                b.RegularConversation.node=2;Good(b,"save_game",slot:7);b.RegularConversation.node=4;CafeRegularConversation.Finish(b.RegularConversation);CafeRegularConversation.Finish(b.RegularConversation);Good(b,"save_game",slot:8);Check(b.RegularConversation.viewed_branches.Length==1,"Completion duplicated");
                string save=File.ReadAllText(Path.Combine(path,"slot-8.json"));var bad=JsonUtility.FromJson<CafeSaveEnvelope>(save);bad.regular_conversation.node=999;File.WriteAllText(Path.Combine(path,"slot-10.json"),JsonUtility.ToJson(bad));RejectedLoad(b,10);
                Good(b,"load_game",slot:3);Check(b.RegularConversation==null,"Old answered save fabricated viewing");b.BeginRegularConversation();Check(b.RegularConversation==null,"Old answered conversation auto-created");
            }
            backend=new LocalCafeBackend(Path.Combine(output,"initial"));CafeBackends.UseLocal(backend);
        }
        void RejectedLoad(LocalCafeBackend b,int slot){string state=JsonUtility.ToJson(b.Simulation.State),p=JsonUtility.ToJson(b.RegularConversation);Check(Command(b,"load_game",slot:slot).result==UnityWebRequest.Result.ProtocolError && state==JsonUtility.ToJson(b.Simulation.State) && p==JsonUtility.ToJson(b.RegularConversation),"Invalid load changed live state");}
        void Apply(RegularConversationPanel ui){typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(ui.GetComponent<PythonStateClient>(),new object[]{backend.View()});}
        IEnumerator Guard(){var run=Run();while(true){bool more;try{if(errors.Count>0)throw new Exception(string.Join("\n",errors));more=run.MoveNext();}catch(Exception e){Fail(e);yield break;}if(!more)yield break;yield return run.Current;}}
        IEnumerator Run()
        {
            SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
            var ui=FindObjectOfType<RegularConversationPanel>();var owner=ui.GetComponent<PythonRegularIntroductionPanel>();var client=ui.GetComponent<PythonStateClient>();
            Check(!ui.Showing && owner.panel.GetComponentsInChildren<Graphic>(true).All(g=>g.GetComponent<CanvasRenderer>()!=null),"Inactive day-one renderers");
            owner.Open();Check(owner.details.gameObject.activeInHierarchy && owner.notice.text.Contains("常連"),"Untriggered introduction information hidden");owner.Close();
            foreach(bool answer in new[]{true,false})
            {
                backend=new LocalCafeBackend(Path.Combine(output,"rules",answer?"accept":"decline"));CafeBackends.UseLocal(backend);Good(backend,"load_game",slot:2);Apply(ui);yield return null;
                Check(ui.Showing && !client.Automatic,"Late introduction activation");Canvas.ForceUpdateCanvases();var bounds=owner.panel.GetComponent<RectTransform>();var canvas=owner.panel.GetComponentInParent<Canvas>().GetComponent<RectTransform>();Check(Vector2.Distance(bounds.rect.size,canvas.rect.size)<1,"Full-screen layout");
                typeof(AcquisitionConversationPanel).GetField("revealed",Hidden).SetValue(ui,0f);var surface=owner.panel.GetComponentInChildren<ConversationAdvanceSurface>(true);ExecuteEvents.Execute(surface.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);ui.Next();Check(backend.RegularConversation.node==0,"Same frame advance skipped");yield return null;ui.Next();Check(backend.RegularConversation.node==1,"Next advance failed");Check(owner.panel.GetComponentsInChildren<Text>(true).Any(t=>t.text==backend.View().regular_introduction.customer_name),"Introducer speaker displayed");yield return null;
                ui.Close();Check(!ui.Showing && backend.Simulation.RegularIntroductionWaiting,"Close answered event");ui.GetComponent<PythonEventListPanel>().ShowMenu(7);Check(ui.Showing && backend.RegularConversation.node==1,"Event-list reopen lost position");
                backend.RegularConversation.node=5;Apply(ui);yield return null;var buttons=owner.panel.GetComponentsInChildren<Button>(true);var accept=buttons.First(b=>b.name=="迎える");var decline=buttons.First(b=>b.name=="見送る");Check(accept.interactable && decline.interactable,"Choice eligibility");
                double funds=backend.Simulation.State.funds;backend.Simulation.State.funds=backend.Simulation.Rules.regular_introduction.cost;Apply(ui);Check(!accept.interactable && decline.interactable,"Exact funds gating");backend.Simulation.State.funds=funds;
                int capacity=backend.Simulation.Rules.housing.initial_capacity;backend.Simulation.Rules.housing.initial_capacity=5;Apply(ui);Check(!accept.interactable && decline.interactable,"Full housing gating");backend.Simulation.Rules.housing.initial_capacity=capacity;Apply(ui);yield return null;
                buttons.First(b=>b.name=="バックログ").onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();var h=ui.HistoryScroll;Check(h.content.rect.height>=h.content.GetComponent<Text>().preferredHeight-1,"Long history clipped");h.verticalNormalizedPosition=0;yield return null;Check(h.verticalNormalizedPosition<.01f || h.content.rect.height<=h.viewport.rect.height,"History bottom unreachable");buttons.First(b=>b.name=="バックログ").onClick.Invoke();yield return null;
                ui.Answer(answer);ui.Answer(answer);yield return new WaitForSecondsRealtime(2);Check(backend.RegularConversation.branch_id==(answer?"accepted":"declined") && backend.Simulation.State.funds==funds-(answer?backend.Simulation.Rules.regular_introduction.cost:0),"UI answer duplicate or lost");Check(ui.Showing && !client.CanDayOff && !client.CanProgressBusiness && !client.CanStartAutomatic,"Result stop failed");ui.Close();Rejected(backend,"day_off");ui.GetComponent<PythonEventListPanel>().ShowMenu(7);Check(ui.Showing,"Result reopen failed");backend.RegularConversation.node=4;Apply(ui);yield return null;ui.Next();Check(!ui.Showing && backend.RegularConversation.completed && !client.Automatic && !backend.ConversationBlocksProgress,"Finish did not release conversation block");yield return null;owner.Open();Check(owner.details.gameObject.activeInHierarchy && !ui.Showing && owner.notice.text.Contains(answer?"迎え":"見送"),"Completed result record hidden");owner.Close();Good(backend,"start_business");
            }
            // Independent native input uses an isolated save directory, never normal player saves.
            string manual=Path.Combine(output,"manual");Directory.CreateDirectory(manual);bool load=Array.IndexOf(Environment.GetCommandLineArgs(),"--regular-manual-load")>=0;
            if(!load)File.Copy(Path.Combine(output,"rules/accept/slot-2.json"),Path.Combine(manual,"slot-2.json"),true);
            backend=new LocalCafeBackend(manual);Good(backend,"load_game",slot:load?8:2);backend.BeginRegularConversation();if(!load)backend.RegularConversation.node=5;CafeBackends.UseLocal(backend);Apply(ui);yield return new WaitForSecondsRealtime(1);
            Check(errors.Count==0,"Unity errors");completed=true;File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" regular conversation checks; zero Unity errors. Both branches, every result node, restart, masks, funds, housing, text and backlog.");
        }
    }
}
