using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    // Explicit QA mode, isolated slots and synthetic full-roster boundary fixtures.
    public sealed class StandaloneDayOffProgressSmoke : MonoBehaviour
    {
        static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        string output;int checks;LocalCafeBackend backend;bool failed;
        readonly List<string> results=new List<string>();
        void Check(bool valid,string label){if(!valid)throw new Exception(label);checks++;results.Add(label);}
        void Fail(Exception e){if(failed)return;failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Application.Quit(1);}
        void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)Fail(new Exception(message+"\n"+stack));}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        CafeResponse Send(LocalCafeBackend b,string kind,string choice=null,int slot=1)
        {
            var s=b.Simulation.State;CafeResponse reply=null;var run=b.Request("/commands",JsonUtility.ToJson(new PythonShiftCommand{kind=kind,choice=choice,cat_id=kind=="player_begin"?s.cats[0].id:null,save_slot=slot,save_id=slot.ToString(),instance_id=s.instance_id,expected_revision=s.revision,request_id=Guid.NewGuid().ToString("N"),working_cats=new string[0]}),r=>reply=r);while(run.MoveNext()){}return reply;
        }
        void Good(LocalCafeBackend b,string kind,string choice=null,int slot=1){var reply=Send(b,kind,choice,slot);if(reply.result!=UnityWebRequest.Result.Success)throw new Exception(kind+": "+reply.text);Check(true,kind);}
        LocalCafeBackend Fixture(string name,int count=6,bool expanded=false,bool poor=false)
        {
            var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
            rules.intake.day=1;rules.starting_funds=poor?rules.intake.cost:10000;
            var cats=rules.cats.ToList();while(cats.Count<count){var cat=JsonUtility.FromJson<CatDefinition>(JsonUtility.ToJson(cats[0]));cat.id="qa-full-"+cats.Count;cat.name="検証猫"+cats.Count;cats.Add(cat);}rules.cats=cats.ToArray();
            var b=new LocalCafeBackend(Path.Combine(output,name),rules);
            if(expanded){var s=b.Simulation.State;s.housing.purchased_day=s.housing.upgraded_day=1;s.housing_expenses=rules.housing.cost+rules.housing_upgrade.cost;s.expenses+=s.housing_expenses;s.funds-=s.housing_expenses;}
            if(b.Simulation.StoreEventWaiting)Good(b,"resolve_store_event",b.Simulation.CurrentStoreEvent.type=="trouble"?"patch":"decline");
            return b;
        }
        void Verify(bool restart)
        {
            foreach(string name in new[]{"full","maximum","poor","accept"})
            {
                bool accept=name=="accept";var b=Fixture(name,name=="maximum"?12:accept?5:6,name=="maximum",name=="poor");
                if(restart){Good(b,"load_game");Check(b.IntakeResultPending,"Process restart keeps unfinished result "+name);}
                else
                {
                    string before=JsonUtility.ToJson(b.Simulation.State);var view=b.View();Check(view.day_off!=null && !view.day_off.can_select && view.day_off.reason.Contains("受け入れ依頼"),"Waiting reason "+name);Check(before==JsonUtility.ToJson(b.Simulation.State),"Preview is read only "+name);
                    Check(view.intake_request.can_decline && view.intake_request.can_accept==accept,"Full/funds choice eligibility "+name);
                    b.BeginIntakeConversation();Good(b,"resolve_intake",accept?"accept":"decline");Check(b.IntakeResultPending && b.ConversationProgressReason.Contains(accept?"受け入れ済み":"見送り済み"),"Answered and unfinished are distinct "+name);
                    before=JsonUtility.ToJson(b.Simulation.State);for(int i=0;i<8;i++)Good(b,"resolve_intake",accept?"accept":"decline");Check(before==JsonUtility.ToJson(b.Simulation.State),"Repeated answer no charge/join "+name);
                    Check(Send(b,"day_off").result!=UnityWebRequest.Result.Success,"Closed result blocks command "+name);Good(b,"save_game");
                    var fresh=new LocalCafeBackend(Path.Combine(output,name),b.Simulation.Rules);Good(fresh,"load_game");b=fresh;Check(b.IntakeResultPending,"Disk restore keeps result "+name);
                }
                b.Conversation.node=4;CafeIntakeConversation.Finish(b.Conversation);CafeIntakeConversation.Finish(b.Conversation);Check(!b.ConversationBlocksProgress && b.Conversation.viewed_branches.Length==1,"Finish releases only conversation "+name);
                string snap=JsonUtility.ToJson(b.Simulation.State);var off=b.View().day_off;Check(off.can_select && snap==JsonUtility.ToJson(b.Simulation.State),"Resolved full roster can take day off "+name);
                Good(b,"day_off");Check(b.Simulation.State.day==2,"No adoption/expansion required to advance "+name);
            }
            var remaining=Fixture("remaining");remaining.BeginIntakeConversation();Good(remaining,"resolve_intake","decline");
            remaining.Simulation.State.reservation.status="accepted";remaining.Simulation.State.reservation.visit_day=1;
            Check(!remaining.View().day_off.can_select && remaining.View().day_off.reason.Contains("特別予約"),"Reservation restriction coexists with unfinished result");
            remaining.Conversation.node=4;CafeIntakeConversation.Finish(remaining.Conversation);
            Check(!remaining.ConversationBlocksProgress && !remaining.View().day_off.can_select,"Completing conversation does not remove reservation restriction");
            remaining.Simulation.State.reservation.status="untriggered";remaining.Simulation.State.reservation.visit_day=0;
            Good(remaining,"player_begin");Check(!remaining.View().day_off.can_select && remaining.View().day_off.reason.Contains("交流へ戻る"),"Player interaction has concrete resolution");Good(remaining,"player_finish");Check(remaining.View().day_off.can_select,"Finishing player interaction releases its restriction");
            Good(remaining,"start_business");Check(remaining.Simulation.State.phase=="open","Full declined roster can open business");
            while(remaining.Simulation.State.phase=="open")Good(remaining,"advance_business");Good(remaining,"next_day");Check(remaining.Simulation.State.day==2,"Full declined roster can finish business and advance");
            backend=Fixture("ui");CafeBackends.UseLocal(backend);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-dayoff-progress-check");if(i<0 || FindObjectOfType<StandaloneDayOffProgressSmoke>()!=null)return;
            var go=new GameObject("Day off progress verification");DontDestroyOnLoad(go);var runner=go.AddComponent<StandaloneDayOffProgressSmoke>();runner.output=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(runner.output);Application.runInBackground=true;Application.logMessageReceived+=runner.Log;
            try{runner.Verify(Array.IndexOf(args,"--dayoff-restart")>=0);runner.StartCoroutine(runner.Guard());}catch(Exception e){runner.Fail(e);}
        }
        void Apply(PythonStateClient client){typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(client,new object[]{backend.View()});}
        IEnumerator Guard(){var run=Run();while(!failed){bool more;try{more=run.MoveNext();}catch(Exception e){Fail(e);yield break;}if(!more)yield break;yield return run.Current;}}
        IEnumerator Run()
        {
            SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
            var client=FindObjectOfType<PythonStateClient>();var ui=client.GetComponent<IntakeConversationPanel>();var prep=client.GetComponent<PythonPreparationPanel>();var off=client.GetComponent<PythonDayOffPanel>();var events=client.GetComponent<PythonEventListPanel>();
            Apply(client);ui.Close();prep.Open();Check(prep.dayOff.interactable && prep.notice.text.Contains("受け入れ依頼"),"Preparation entry explains pending request");prep.OpenDayOff();Check(off.panel.activeSelf && !off.proceed.interactable && off.details.text.Contains("見送る"),"Disabled day-off shows resolution");off.ResolveBlock();Check(events.panel.activeSelf,"Day-off resolution opens event list");events.ShowMenu(1);backend.Conversation.node=5;Apply(client);yield return null;
            var owner=client.GetComponent<PythonIntakePanel>();Check(owner.panel.GetComponentsInChildren<Text>(true).Any(t=>t.text.Contains("在籍 6 / 上限 6") && t.text.Contains("満員")),"Full count/capacity and reason visible");
            ui.Answer(false);ui.Answer(false);yield return new WaitForSecondsRealtime(1);Check(backend.Simulation.State.intake_request.status=="declined" && backend.Simulation.State.cats.Count==6,"UI double decline only once");ui.Close();prep.Open();Check(prep.notice.text.Contains("見送り済み") && prep.notice.text.Contains("結果会話"),"Preparation explains declined result");prep.OpenDayOff();Check(!off.proceed.interactable && off.details.text.Contains("会話を終える"),"Day-off explains unfinished result");
            off.ResolveBlock();Check(events.PendingFor(1)==1 && events.GroupFor(1)==0,"Unfinished result in pending group");events.ShowMenu(1);Check(ui.Showing,"Result reliably reopened");backend.Conversation.node=4;Apply(client);yield return null;ui.Next();yield return null;Check(!backend.ConversationBlocksProgress && client.CanDayOff,"UI completion releases progress");
            prep.Open();prep.OpenDayOff();Check(off.proceed.interactable,"Day-off available after decline");off.Choose();off.Cancel();Check(backend.Simulation.State.day==1,"Cancel is read only");off.Choose();off.Confirm();off.Confirm();yield return new WaitForSecondsRealtime(1);Check(backend.Simulation.State.day==2,"Double confirm advances exactly one day");
            // Leave a full roster for independent native interaction and screenshots.
            backend=Fixture("manual");CafeBackends.UseLocal(backend);Apply(client);backend.Conversation.node=5;Apply(client);yield return new WaitForSecondsRealtime(1);
            File.WriteAllText(Path.Combine(output,Array.IndexOf(Environment.GetCommandLineArgs(),"--dayoff-restart")>=0?"result-restart.txt":"result-first.txt"),"PASS "+checks+" checks\n"+string.Join("\n",results));
        }
    }
}
