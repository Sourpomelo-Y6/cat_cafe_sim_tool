using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    // Explicit opt-in QA only. Uses isolated saves, never the player's slots.
    public sealed class StandaloneConversationSmoke : MonoBehaviour
    {
        string output;LocalCafeBackend backend;int checks;
        void Check(bool value,string label){if(!value)throw new Exception(label);checks++;}
        static void Send(LocalCafeBackend b,string kind,string choice=null,int slot=1)
        {
            var s=b.Simulation.State;CafeResponse reply=null;
            var request=b.Request("/commands",JsonUtility.ToJson(new PythonShiftCommand{kind=kind,choice=choice,save_slot=slot,save_id=slot.ToString(),request_id=Guid.NewGuid().ToString("N"),instance_id=s.instance_id,expected_revision=s.revision,working_cats=new string[0]}),r=>reply=r);
            while(request.MoveNext()){}if(reply.result!=UnityWebRequest.Result.Success)throw new Exception(kind+": "+reply.error+" / "+reply.text);
        }
        static void Prepare(LocalCafeBackend b)
        {
            while(b.Simulation.State.day<4){if(b.Simulation.StoreEventWaiting)Send(b,"resolve_store_event",b.Simulation.CurrentStoreEvent.type=="trouble"?"patch":"decline");Send(b,"day_off");}
            if(b.Simulation.StoreEventWaiting)Send(b,"resolve_store_event",b.Simulation.CurrentStoreEvent.type=="trouble"?"patch":"decline");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-conversation-check");if(i<0 || FindObjectOfType<StandaloneConversationSmoke>()!=null)return;
            var go=new GameObject("Conversation verification");DontDestroyOnLoad(go);var runner=go.AddComponent<StandaloneConversationSmoke>();runner.output=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(runner.output);Application.runInBackground=true;
            try{runner.VerifySaves(Array.IndexOf(args,"--conversation-restart")>=0);runner.StartCoroutine(runner.Guard());}catch(Exception e){runner.Fail(e);}
        }
        void Fail(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);}
        void VerifySaves(bool restart)
        {
            foreach(string choice in new[]{"accept","decline"})
            {
                var path=Path.Combine(output,choice);var b=new LocalCafeBackend(path);
                if(restart)
                {
                    Send(b,"load_game",slot:2);Check(b.Conversation.node==3 && b.Conversation.branch_id=="introduction","restart introduction");
                    Send(b,"load_game",slot:3);Check(b.Conversation.node==5 && b.Simulation.IntakeWaiting,"restart choice");
                    Send(b,"load_game",slot:4);Check(b.Conversation.node==2 && b.Conversation.branch_id==(choice=="accept"?"accepted":"declined"),"restart result");
                    string restartBefore=JsonUtility.ToJson(b.Simulation.State);Send(b,"resolve_intake",choice);Check(restartBefore==JsonUtility.ToJson(b.Simulation.State),"restart duplicate");continue;
                }
                Prepare(b);b.BeginIntakeConversation();var p=b.Conversation;
                p.node=3;Send(b,"save_game",slot:2);var fresh=new LocalCafeBackend(path);Send(fresh,"load_game",slot:2);Check(fresh.Conversation.node==3,"Unity introduction resume");
                p.node=5;Send(b,"save_game",slot:3);Send(fresh,"load_game",slot:3);Check(fresh.Conversation.node==5,"Unity choice resume");
                double funds=b.Simulation.State.funds;Send(b,"resolve_intake",choice);p=b.Conversation;p.node=2;Send(b,"save_game",slot:4);Send(fresh,"load_game",slot:4);Check(fresh.Conversation.node==2 && fresh.Conversation.branch_id==p.branch_id,"Unity result resume");
                string before=JsonUtility.ToJson(fresh.Simulation.State);for(int j=0;j<8;j++)Send(fresh,"resolve_intake",choice);Check(before==JsonUtility.ToJson(fresh.Simulation.State),"Unity duplicate answer");
                Check(fresh.Simulation.State.funds==funds-(choice=="accept"?b.Simulation.Rules.intake.cost:0),"Unity cost");
                p.node=4;CafeIntakeConversation.Finish(p);CafeIntakeConversation.Finish(p);Send(b,"save_game",slot:5);Send(fresh,"load_game",slot:5);Check(fresh.Conversation.completed && fresh.Conversation.viewed_branches.Length==1,"Unity completion");
                // Optional presentation absent in an existing v1 save.
                string savePath=Path.Combine(path,"slot-4.json");var envelope=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(savePath));envelope.conversation=null;envelope.slot=6;File.WriteAllText(Path.Combine(path,"slot-6.json"),JsonUtility.ToJson(envelope));Send(fresh,"load_game",slot:6);Check(fresh.Conversation==null,"Old answered save fabricated a conversation");
            }
            if(!restart)Check(StandaloneIntakeSmoke.Run(Path.Combine(output,"existing-intake"))>0,"Existing intake rules regression");
            backend=new LocalCafeBackend(Path.Combine(output,"interactive"));Prepare(backend);CafeBackends.UseLocal(backend);
        }
        void ApplyView(IntakeConversationPanel ui){var client=ui.GetComponent<PythonStateClient>();typeof(PythonStateClient).GetMethod("Accept",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(client,new object[]{backend.View()});}
        IEnumerator Guard()
        {
            var run=Run();while(true){bool more;try{more=run.MoveNext();}catch(Exception e){Fail(e);yield break;}if(!more)yield break;yield return run.Current;}
        }
        IEnumerator Run()
        {
            SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(3);
            var ui=FindObjectOfType<IntakeConversationPanel>();Check(ui!=null && ui.Showing,"Actual scene conversation opened");
            var owner=ui.GetComponent<PythonIntakePanel>();var client=ui.GetComponent<PythonStateClient>();
            Check(!client.Automatic,"Automatic stopped");
            Canvas.ForceUpdateCanvases();var bounds=owner.panel.GetComponent<RectTransform>();var canvas=owner.panel.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            Check(Vector2.Distance(bounds.rect.size,canvas.rect.size)<1,"Conversation covers full canvas");
            var dock=owner.panel.transform.Find("Intake Conversation/Dialogue Window").GetComponent<RectTransform>();
            Check(dock.rect.width>canvas.rect.width*.85f && dock.rect.height>=250,"Readable bottom dialogue window");
            typeof(IntakeConversationPanel).GetField("revealed",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(ui,0f);var advance=owner.panel.GetComponentInChildren<ConversationAdvanceSurface>(true);ExecuteEvents.Execute(advance.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);ui.Next();Check(backend.Conversation.node==0,"Text click reveals without same-frame skip");yield return null;
            ui.Next();Check(backend.Conversation.node==1,"Next after reveal");yield return null;
            ui.Close();Check(!owner.panel.activeSelf && backend.Simulation.IntakeWaiting,"Closing answered request");ui.Open();Check(owner.panel.activeSelf && backend.Conversation.node==1,"Reopen lost position");
            // Both choices and conditions use the real state projection.
            backend.Conversation.node=5;ApplyView(ui);yield return null;
            var buttons=owner.panel.GetComponentsInChildren<Button>(true);var accept=buttons.First(b=>b.name=="迎える");var decline=buttons.First(b=>b.name=="見送る");
            Check(accept.interactable && decline.interactable,"Choice eligibility");
            double funds=backend.Simulation.State.funds;backend.Simulation.State.funds=backend.Simulation.Rules.intake.cost;ApplyView(ui);Check(!accept.interactable && decline.interactable,"Funds display eligibility");backend.Simulation.State.funds=funds;
            int capacity=backend.Simulation.Rules.housing.initial_capacity;backend.Simulation.Rules.housing.initial_capacity=5;ApplyView(ui);Check(!accept.interactable && decline.interactable,"Housing display eligibility");backend.Simulation.Rules.housing.initial_capacity=capacity;
            ApplyView(ui);yield return null;
            buttons.First(b=>b.name=="バックログ").onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();
            var historyArea=ui.HistoryScroll;Check(historyArea.content.rect.height>=historyArea.content.GetComponent<Text>().preferredHeight-1,"Backlog retains full text height");historyArea.verticalNormalizedPosition=0;yield return null;Check(historyArea.content.rect.height<=historyArea.viewport.rect.height+1 || historyArea.verticalNormalizedPosition<.01f,"Backlog bottom reachable");buttons.First(b=>b.name=="バックログ").onClick.Invoke();yield return null;
            ui.Answer(true);ui.Answer(true);yield return new WaitForSecondsRealtime(2);
            Check(backend.Simulation.State.intake_request.status=="accepted" && backend.Simulation.State.cats.Count==6 && backend.Simulation.State.funds==funds-backend.Simulation.Rules.intake.cost,"Actual UI duplicate answer");
            Check(ui.Showing && backend.Conversation.branch_id=="accepted","Result conversation visible");Check(!client.CanProgressBusiness && !client.CanDayOff && !client.CanStartAutomatic,"Result progress controls disabled");
            ui.Close();ui.GetComponent<PythonEventListPanel>().ShowMenu(1);Check(ui.Showing,"Result reopen from event list");
            backend.Conversation.node=4;ApplyView(ui);yield return null;ui.Next();
            Check(!ui.Showing && backend.Conversation.completed && backend.Conversation.viewed_branches.Length==1 && !client.Automatic,"Result finished and manual resume");
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" Unity/Player checks. Both branches and restart fixtures retained. Interactive result scene ready.");
            // Leave a choice ready for independent native input and visual inspection.
            backend.ConversationOpen=false;backend=new LocalCafeBackend(Path.Combine(output,"manual"));Prepare(backend);backend.BeginIntakeConversation();backend.Conversation.node=5;CafeBackends.UseLocal(backend);SceneManager.LoadScene("CatCafePythonState");
        }
    }
}
