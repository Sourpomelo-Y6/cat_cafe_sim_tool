using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public sealed class StandalonePendingCasesSmoke:MonoBehaviour {
  string root;LocalCafeBackend backend;int checks;
  static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
  void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
  void Apply(PythonStateClient c){typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(c,new object[]{backend.View()});}
  void Good(string kind,string choice=null,string id=null,int slot=9){var s=backend.Simulation.State;CafeResponse reply=null;var call=backend.Request("/commands",JsonUtility.ToJson(new PythonShiftCommand{request_id=Guid.NewGuid().ToString("N"),instance_id=s.instance_id,expected_revision=s.revision,kind=kind,choice=choice,event_id=id,save_slot=slot,save_id=slot.ToString(),working_cats=new string[0]}),r=>reply=r);while(call.MoveNext()){}Check(reply.result==UnityEngine.Networking.UnityWebRequest.Result.Success,kind+": "+reply.error);}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--catcafe-pending-check");if(i<0)return;var q=new GameObject("Pending cases QA").AddComponent<StandalonePendingCasesSmoke>();DontDestroyOnLoad(q);q.root=Path.GetFullPath(a[i+1]);Directory.CreateDirectory(q.root);Application.runInBackground=true;q.StartCoroutine(q.Guard());}
  IEnumerator Guard(){var run=Run();while(true){bool more;try{more=run.MoveNext();}catch(Exception ex){File.WriteAllText(Path.Combine(root,"failure.txt"),ex.ToString());Debug.LogException(ex);yield break;}if(!more)yield break;yield return run.Current;}}
  IEnumerator Run(){
   yield return new WaitForSecondsRealtime(1);var rules=StandaloneTrustConversationSmoke.FixtureRules();rules.arrival_ticks=new[]{0,0,0,0};backend=new LocalCafeBackend(Path.Combine(root,"saves"),rules);
   bool restart=Environment.GetCommandLineArgs().Contains("--pending-resume");
   if(restart)Good("load_game");else {
    while(!backend.Simulation.CustomerTrustWaiting && backend.Simulation.State.day<8){backend.Simulation.Execute("start_business");while(backend.Simulation.State.phase!="closed")backend.Simulation.Execute("advance_business");backend.Simulation.Execute("next_day");backend.View();}
    var waiting=backend.Simulation.State.trust_events.Where(e=>e.status=="waiting").ToArray();Check(waiting.Length>=2,"fixture needs multiple customers");backend.BeginTrustConversation(waiting[0].id);Good("resolve_customer_trust","recover",waiting[0].id);backend.TrustConversation.Current.node=1;Good("save_game");
   }
   CafeBackends.UseLocal(backend);UnityEngine.SceneManagement.SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
   var c=FindObjectOfType<PythonStateClient>();Apply(c);yield return null;var list=c.GetComponent<PythonEventListPanel>();list.HoldConversations=true;
   foreach(var d in c.GetComponents<AcquisitionConversationPanel>().Where(d=>d.Showing))d.Close();list.Open();yield return null;
   var rows=list.PendingCases();Check(rows.Count>=2 && rows.Any(r=>r.result) && rows.Any(r=>!r.result),"mixed answer/result rows lost");Check(list.PendingCount==rows.Count,"wrong total");Check(rows.Select(r=>r.group+"/"+r.id).Distinct().Count()==rows.Count,"duplicate identities");
   string facts=JsonUtility.ToJson(backend.Simulation.State);for(int i=0;i<10;i++){list.PendingCases();list.RefreshControls();}Check(facts==JsonUtility.ToJson(backend.Simulation.State),"list replays gameplay");
   var result=rows.First(r=>r.result);var button=list.content.GetComponentsInChildren<Button>().First(b=>b.name.StartsWith("Pending Case") && b.GetComponentInChildren<Text>().text.Contains(result.id));button.onClick.Invoke();yield return null;
   Check(backend.TrustConversationSelected==result.id && c.GetComponent<TrustConversationPanel>().Showing,"exact result route failed");Check(backend.TrustConversation.Current.node==1,"checkpoint position lost");
   var dialog=c.GetComponent<TrustConversationPanel>();var close=dialog.GetComponentsInChildren<Button>().First(b=>b.name=="閉じる");close.onClick.Invoke();yield return null;Check(list.panel.activeSelf && list.PendingCount==rows.Count,"close lost pending or return route");
   button=list.content.GetComponentsInChildren<Button>().First(b=>b.name.StartsWith("Pending Case") && b.GetComponentInChildren<Text>().text.Contains(result.id));button.onClick.Invoke();yield return null;
   backend.TrustConversation.Current.node=3;Apply(c);yield return null;dialog.Next();yield return null;dialog.Next();yield return null;Check(list.panel.activeSelf && list.PendingCount==rows.Count-1,"completion did not refresh count");Check(!c.GetComponents<AcquisitionConversationPanel>().Any(d=>d.Showing),"next conversation auto-played");Check(!c.Automatic && !c.AutoPlayExecuting,"list resumed business");
   // Many pending rows use the existing scroll viewport; this is display-only fixture data.
   var many=backend.View();many.growth_choices=Enumerable.Range(0,8).Select(i=>new PythonGrowthChoice{cat_id="fixture-"+i,name="表示確認"+i,title="成長"}).ToArray();list.Apply(many);list.SelectGroup(0);Check(list.content.rect.height>list.scroll.viewport.rect.height,"many rows cannot scroll");list.scroll.verticalNormalizedPosition=0;Check(list.scroll.verticalNormalizedPosition==0,"scroll cannot reach bottom");Apply(c);
   Good("new_game","free");Apply(c);list.Open();Check(list.PendingCount==0 && !list.HoldConversations,"new game retained pending navigation");
   var go=list.panel.GetComponentsInChildren<Button>().First(b=>b.name=="Continue Business");Check(go.interactable && go.GetComponentInChildren<Text>().text=="営業開始","preparation continuation missing");
   Good("start_business");Apply(c);list.Open();Check(go.interactable && go.GetComponentInChildren<Text>().text=="自動進行を再開","open continuation missing");c.ToggleAutomatic();list.Open();Check(!c.Automatic,"viewing list leaves normal automation running");
   while(backend.Simulation.State.phase!="closed")Good("advance_business");Apply(c);list.Open();Check(go.interactable && go.GetComponentInChildren<Text>().text=="翌日へ","closed continuation missing");
   Good("new_game","bond");backend.Simulation.StartAutoplay("bond","basic",1);Good("pause_autoplay");Apply(c);list.Open();Check(go.interactable && go.GetComponentInChildren<Text>().text=="おまかせを再開","paused continuation missing");
   Good("stop_autoplay");Apply(c);list.Open();Check(!backend.Simulation.State.autoplay.running && !go.GetComponentInChildren<Text>().text.Contains("おまかせ"),"cancelled autoplay revived");
   // Restore a stable mixed checkpoint for physical input and a separate process.
   Good("load_game");Apply(c);yield return null;list.HoldConversations=true;foreach(var d in c.GetComponents<AcquisitionConversationPanel>().Where(d=>d.Showing))d.Close();list.Open();
   if(Environment.GetCommandLineArgs().Contains("--pending-last")){
    foreach(var e in backend.Simulation.State.trust_events.Where(e=>e.status=="waiting").ToArray()){backend.BeginTrustConversation(e.id);Good("resolve_customer_trust","recover",e.id);}
    foreach(var r in backend.TrustConversations.Where(r=>r.event_id!=result.id)){r.Current.node=3;CafeTrustDialogue.Finish(r);}backend.BeginTrustConversation(result.id);Apply(c);list.Open();Check(list.PendingCount==1,"last result fixture invalid");
   }
   File.WriteAllText(Path.Combine(root,restart?"result-resume.txt":"result.txt"),"PASS "+checks+"; manual mixed checkpoint ready");ScreenCapture.CaptureScreenshot(Path.Combine(root,restart?"ready-resume.png":"ready.png"));
  }
 }
}
