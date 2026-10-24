using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace CatCafePreview
{
 public class StandalonePlayerStaminaSmoke:MonoBehaviour
 {
  string output;LocalCafeBackend b;int checks;
  void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
  void Good(string kind,string cat=null,string choice=null,string[] working=null,int slot=1){var r=StandaloneGrowthBatchSmoke.Send(b,kind,cat,choice,slot:slot,working:working);Check(r.result==UnityEngine.Networking.UnityWebRequest.Result.Success,kind+":"+r.error);}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
  {
   var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--catcafe-player-stamina");if(i<0)return;
   var q=new GameObject("Player stamina checks").AddComponent<StandalonePlayerStaminaSmoke>();DontDestroyOnLoad(q);q.output=Path.GetFullPath(a[i+1]);Directory.CreateDirectory(q.output);Application.runInBackground=true;q.StartCoroutine(q.Guard());
  }
  IEnumerator Guard(){var r=Run();while(true){bool more;try{more=r.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}if(!more)yield break;yield return r.Current;}}
  void Apply(PythonStateClient c){typeof(PythonStateClient).GetMethod("Accept",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(c,new object[]{b.View()});Canvas.ForceUpdateCanvases();}
  IEnumerator Shot(string name){Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.3f);}
  IEnumerator Run()
  {
   var rules=JsonUtility.FromJson<Standalone.CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
   b=new LocalCafeBackend(Path.Combine(output,"save"),rules);Good("new_game",choice:"free");
   bool restart=Environment.GetCommandLineArgs().Contains("--player-stamina-resume");
   if(restart)
   {
    Good("load_game",slot:2);CafeBackends.UseLocal(b);SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
    var resumed=FindObjectOfType<PythonStateClient>();Apply(resumed);var resumedPanel=resumed.GetComponent<PythonPlayerPanel>();resumedPanel.Open();
    Check(resumedPanel.summary.text.Contains("現在体力 0"),"Separate process stale stamina");Check(resumedPanel.summary.text.Contains("休養予定"),"Separate process stale plan");
    File.WriteAllText(Path.Combine(output,"resume-result.txt"),"PASS "+checks);Application.Quit(0);yield break;
   }
   var cat=b.Simulation.State.cats[0];string id=cat.id;var other=b.Simulation.State.cats[1];
   foreach(bool work in new[]{false,true})foreach(double stamina in new[]{100d,rules.exchange.low_stamina,rules.exchange.low_stamina+.01,0d})
   {
    cat.working=work;cat.stamina=stamina;string before=JsonUtility.ToJson(b.Simulation.State);string guide=b.Simulation.PlayerStaminaGuidance(id);b.View();
    Check(before==JsonUtility.ToJson(b.Simulation.State),"Reading changed state");Check(guide.StartsWith(work?"出勤予定":"休養予定"),"Plan missing");Check(guide.Contains("低体力")== (work && stamina>0 && stamina<=rules.exchange.low_stamina),"Threshold");Check(guide.Contains("体力切れ")== (work && stamina==0),"Exhaustion");
   }
   cat.stamina=100;Good("set_shifts",working:new[]{id,other.id});Good("player_begin",cat:id);Good("player_step",choice:"direct");Good("save_game");Good("load_game");Check(b.View().player_interaction.active,"Active restore");
   CafeBackends.UseLocal(b);SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
   var client=FindObjectOfType<PythonStateClient>();var panel=client.GetComponent<PythonPlayerPanel>();var prep=client.GetComponent<PythonPreparationPanel>();
   Apply(client);panel.Open();Check(panel.summary.text.Contains("出勤予定"),"Active plan");Check(panel.notice.text.Contains("終了後"),"Active lock explanation");Check(!panel.preparation.gameObject.activeSelf,"Active navigation offered");
   yield return Shot("active");
   Good("player_finish");Apply(client);panel.Open();Check(panel.resultGuide.text.Contains("今の体力は回復しません"),"Recovery distinction");Good("save_game");Good("load_game");Apply(client);panel.Open();Check(!b.View().player_interaction.active,"Manual restore");
   var fixture=JsonUtility.FromJson<PythonCafeState>(JsonUtility.ToJson(b.View()));var fixtureCat=fixture.cats.Single(c=>c.cat_id==id);fixtureCat.max_stamina=300;fixtureCat.stamina=250;panel.Apply(fixture);panel.Open();Check(panel.summary.text.Contains("現在体力 250 / 300"),"Grown maximum UI");
   fixture.can_set_shifts=false;panel.Apply(fixture);panel.Open();Check(panel.resultGuide.text.Contains("翌日の営業準備"),"Closed change timing");
   fixture.can_set_shifts=true;fixture.required_action="reservation";panel.Apply(fixture);panel.Open();Check(panel.resultGuide.text.Contains("先にイベント"),"Event change reason");
   Apply(client);panel.Open();
   var details=client.GetComponent<PythonCatDetailsPanel>();panel.ReturnToDetails();details.Open(other.id);Check(!details.previousInteraction.interactable,"Other cat offered old guide");details.Close();panel.Open();
   foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1024,768)})
   {
    Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.5f);Apply(client);panel.Open();yield return Shot("result-"+size.x);
    Check(panel.preparation.interactable,"Preparation disabled");panel.preparation.onClick.Invoke();yield return null;
    Check(!panel.panel.activeSelf && prep.panel.activeSelf && prep.SelectedCatId==id,"Direct navigation");Check(prep.selectedDetails.text.Contains(b.View().cats.Single(c=>c.cat_id==id).shift_forecast),"Forecast stale");
    if(b.Simulation.State.cats.Single(c=>c.id==id).working){prep.Rest();yield return new WaitForSecondsRealtime(.5f);}
    Check(!b.Simulation.State.cats.Single(c=>c.id==id).working && b.Simulation.State.cats.Single(c=>c.id==other.id).working,"Other schedule changed");
    yield return Shot("preparation-"+size.x);prep.Close();panel.Open();Check(panel.summary.text.Contains("休養予定"),"Plan stale after rest");
   }
   Good("set_shifts",working:new string[0]);Apply(client);panel.Open();panel.preparation.onClick.Invoke();Check(!prep.start.interactable,"Zero cats opening enabled");prep.Close();
   // Exhaustion auto-end and a new session must replace the old guide.
   cat=b.Simulation.State.cats.Single(c=>c.id==id);cat.stamina=1;cat.cannot_continue=false;Good("player_begin",cat:id);Good("player_step",choice:"intense");Apply(client);panel.Open();Check(!b.View().player_interaction.active && panel.summary.text.Contains("現在体力 0"),"Auto exhaustion");
   Good("save_game",slot:2);Good("load_game",slot:2);Apply(client);Check(panel.summary.text.Contains("現在体力 0"),"Exhaustion restore");
   // Unrelated game has no session and must hide the guide.
   Good("new_game",choice:"free");Apply(client);Check(!panel.panel.activeSelf && b.View().player_interaction==null,"Old game guide retained");
   File.WriteAllText(Path.Combine(output,"result.txt"),"PASS "+checks);Application.Quit(0);
  }
 }
}
