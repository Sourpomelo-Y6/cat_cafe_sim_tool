using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    // Standard rules, scripted ordinary UI actions. Never edits simulation or dialogue progress.
    // Physical Windows input is checked separately and must not be claimed by this runner.
    public sealed class StandaloneRoutePlaytest : MonoBehaviour
    {
        string root,objective;bool resume,started,continued,saving,dispatched;int steps,continuedDay,freePreparedDay;float lastChange;string signature;
        LocalCafeBackend Backend=>CafeBackends.Local as LocalCafeBackend;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--catcafe-route");if(i<0)return;
            if(!a.Contains("--catcafe-playtest"))throw new ArgumentException("Route requires isolated playtest store");
            var q=new GameObject("Standard UI route").AddComponent<StandaloneRoutePlaytest>();DontDestroyOnLoad(q);
            q.root=Path.GetFullPath(a[i+1]);q.objective=a[i+2];q.resume=a.Contains("--route-resume");Directory.CreateDirectory(q.root);q.StartCoroutine(q.Guard());
        }
        IEnumerator Guard(){var run=Run();while(true){bool more;try{more=run.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(root,"failure.txt"),e.ToString());Application.Quit(1);yield break;}if(!more)yield break;yield return run.Current;}}
        void Log(string s)=>File.AppendAllText(Path.Combine(root,"ui-actions.txt"),DateTime.UtcNow.ToString("o")+" "+s+"\n");
        bool Click(Button b){if(b==null || !b.gameObject.activeInHierarchy || !b.IsInteractable())return false;Log(b.name+" / "+b.GetComponentInChildren<Text>()?.text);b.onClick.Invoke();return true;}
        bool TextButton(GameObject host,params string[] captions){if(host==null)return false;foreach(string text in captions){var b=host.GetComponentsInChildren<Button>().FirstOrDefault(x=>x.IsInteractable() && x.GetComponentInChildren<Text>()?.text==text);if(Click(b))return true;}return false;}
        void Required(PythonStateClient c)
        {
            string action=c.CurrentState.required_action;int route=-1;
            switch(action){case "store_event":route=2;break;case "intake_request":route=1;break;case "regular_introduction":route=7;break;case "reservation":route=8;break;case "customer_trust":route=9;break;case "missing_return":route=4;break;case "adoption":route=5;break;case "dispatch_choice":route=11;break;case "dispatch_trouble":route=12;break;case "dispatch_introduction":route=13;break;case "dispatch_return":route=Backend.Simulation.State.dispatches.Any(x=>x.status=="waiting" && x.destination.StartsWith("patron"))?6:10;break;}
            var list=c.GetComponent<PythonEventListPanel>();
            if(route>=0){Log("required route "+action);list.ShowMenu(route);return;}
            list.Open();Click(list.entries.FirstOrDefault(x=>x.gameObject.activeInHierarchy && x.IsInteractable()));
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(2);lastChange=Time.realtimeSinceStartup;
            while(++steps<100000)
            {
                yield return new WaitForSecondsRealtime(.04f);
                var b=Backend;if(b==null)throw new Exception("Observer replaced concrete local backend");
                var title=FindObjectOfType<PythonTitleScreen>();
                if(title!=null)
                {
                    if(!started && title.CanManageSaves){started=true;if(resume){title.ResumeSaved("9");Log("resume slot9");}else{title.ShowConditions();int index=Array.FindIndex(b.View().new_game_options,x=>x.choice==objective);title.objectives.value=index;title.SelectObjective(index);title.StartNewGame();title.StartNewGame();Log("new "+objective);}}continue;
                }
                var c=FindObjectOfType<PythonStateClient>();if(c==null || c.CurrentState==null)continue;
                var s=b.Simulation.State;string key=s.day+":"+s.tick+":"+s.revision+":"+string.Join("|",c.GetComponents<AcquisitionConversationPanel>().Where(x=>x.Showing).Select(x=>x.GetType().Name+"/"+x.GetComponentsInChildren<Text>().FirstOrDefault()?.text));
                if(key!=signature){signature=key;lastChange=Time.realtimeSinceStartup;}
                if(Time.realtimeSinceStartup-lastChange>120)throw new Exception("No state progress: "+key+" required="+c.CurrentState.required_action+" "+b.ConversationProgressReason);
                if(s.day>150)throw new Exception("Route exceeded 150 days");
                var dialogs=c.GetComponents<AcquisitionConversationPanel>().Where(x=>x.Showing).ToArray();
                var reservation=c.GetComponent<PythonReservationPanel>();
                if(reservation.Confirming && Click(reservation.confirm))continue;
                bool final=b.PopularityEndingPending || b.BondEndingPending || b.PatronEndingPending;
                int endingNode=b.PopularityEndingPending?b.PopularityEnding.progress.node:b.BondEndingPending?b.BondEnding.progress.node:b.PatronEndingPending?b.PatronEnding.progress.node:0;
                if(final && endingNode>=2 && !resume && !saving && c.CanSaveNow)
                {
                    saving=true;c.SaveToSlot(9);Log("save ending checkpoint");yield return new WaitForSecondsRealtime(1);
                    File.WriteAllText(Path.Combine(root,"checkpoint.txt"),objective+" day="+s.day+" funds="+s.funds);Application.Quit(0);yield break;
                }
                if(dialogs.Length>0)
                {
                    var d=dialogs.Last();var buttons=d.GetComponentsInChildren<Button>().Where(x=>x.gameObject.activeInHierarchy && x.IsInteractable()).ToArray();
                    // Confirmations are normal UI controls; decline irreversible adoption in this policy.
                    var confirm=buttons.FirstOrDefault(x=>x.name=="この内容で回答");
                    if(Click(confirm))continue;
                    var accept=buttons.FirstOrDefault(x=>x.name==(d is AdoptionConversationPanel?"見送る":"迎える"));
                    if(Click(accept))continue;
                    if(Click(buttons.FirstOrDefault(x=>x.name=="見送る")))continue;
                    bool choices=d.GetComponentsInChildren<Button>().Any(x=>x.gameObject.activeInHierarchy && (x.name=="迎える" || x.name=="見送る"));
                    if(choices && c.CanSendCommands && !c.HasPendingResults){d.Close();if(c.AutoPlayRunning){c.StopAutoPlay();Log("stop autoplay for manual event response");}else Required(c);continue;}
                    if(TextButton(d.gameObject,"確定する","確認して実行","会話を終える","次へ","全文表示"))continue;
                    d.Next();continue;
                }
                if(!c.CanSendCommands)continue;
                if(resume && objective=="free" && !continued){continued=true;continuedDay=s.day;}
                if(resume && !continued && !final && c.CanContinueGoal){continued=true;continuedDay=s.day;c.ContinueGoal();Log("continue after ending");continue;}
                if(continued && s.phase=="open" && c.CanSaveNow)
                {
                    c.SaveToSlot(10);yield return new WaitForSecondsRealtime(1);
                    File.WriteAllText(Path.Combine(root,"result.txt"),"PASS standard UI route "+objective+" day="+s.day+" phase="+s.phase+" funds="+s.funds+" Extra="+b.Replays.Load().entries.Length);Application.Quit(0);yield break;
                }
                if(c.CanContinueGoal){c.ContinueGoal();continue;}if(c.CanAdvanceGoal){c.AdvanceGoal();continue;}
                var growth=c.GetComponent<PythonGrowthPanel>();if(growth.panel.activeInHierarchy)
                {
                    var rows=c.CurrentState.growth_choices;int row=Array.FindIndex(rows,x=>x.cat_id=="cat-sora" && x.choices.Any(o=>o.choice=="service") && x.selected!="service");
                    if(objective=="patron" && row>=0){c.GrowthRowIndex=row;growth.Apply(c.CurrentState);c.RespondGrowth(Array.FindIndex(rows[row].choices,x=>x.choice=="service"));Log("manual Sora service specialization");continue;}
                    if(!TextButton(growth.panel,"一覧をまとめて確定"))TextButton(growth.panel,"猫に合う内容で自動決定");continue;
                }
                if(!string.IsNullOrEmpty(c.CurrentState.required_action))
                {
                    var missing=c.GetComponent<PythonMissingPanel>();
                    if(c.CurrentState.required_action=="missing_return" && missing.panel.activeInHierarchy && Click(missing.confirm))continue;
                    Required(c);continue;
                }
                bool trainingPatron=objective=="patron" && string.IsNullOrEmpty(s.growth.First(x=>x.cat_id=="cat-sora").specialization);
                if((objective=="free" && !resume || trainingPatron) && s.phase=="preparation")
                {
                    if(objective=="free" && s.day==2 && c.CanDayOff){c.DayOff();Log("free day off");continue;}
                    if(objective=="free" && s.day==3 && !dispatched)
                    {
                        var option=c.CurrentState.general_dispatch?.options?.FirstOrDefault(x=>x.choice=="neighborhood_visit" && x.can_select);
                        if(option!=null){dispatched=true;var owner=c.GetComponent<PythonGeneralDispatchPanel>();owner.OpenOption(option.cat_id,option.choice);owner.Send();Log("free dispatch");continue;}
                    }
                    if(freePreparedDay!=s.day)
                    {
                        freePreparedDay=s.day;
                        if(trainingPatron){var working=s.cats.Where(x=>(x.id=="cat-sora" || x.id=="cat-mike") && x.activity=="cafe" && x.health=="healthy" && x.stamina>0).Select(x=>x.id).ToArray();c.SetShifts(working);Log("train Sora shifts "+string.Join(",",working));}
                        else {var prep=c.GetComponent<PythonPreparationPanel>();prep.ProposeRest();prep.ConfirmProposal();}
                        continue;
                    }
                }
                if(c.AutoPlayRunning){if(c.AutoPlayPaused)c.ResumeAutoPlay();continue;}
                if(trainingPatron){if(c.CanProgressBusiness)c.ProgressBusiness();else if(c.CanDayOff)c.DayOff();continue;}
                string target=objective=="free"?"popularity":objective;
                if(objective=="free" && !resume && s.day>=11 && c.CanSaveNow){c.SaveToSlot(9);yield return new WaitForSecondsRealtime(1);File.WriteAllText(Path.Combine(root,"checkpoint.txt"),"free day="+s.day);Application.Quit(0);yield break;}
                if(c.CanStartAutoPlay(target,"fast",10)){c.GetComponent<PythonEventListPanel>().ReturnToBusiness();c.StartAutoPlay(target,"fast",10);Log("autoplay fast "+target);continue;}
                if(c.CanProgressBusiness){c.ProgressBusiness();continue;}
            }
            throw new Exception("UI action safety limit");
        }
    }
}
