using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CatCafePreview
{
 public class StandalonePlayerLayoutSmoke:MonoBehaviour
 {
  string output;LocalCafeBackend b;int checks;PythonPlayerPanel p;PythonStateClient client;
  void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
  void Good(string kind,string cat=null,string choice=null,int slot=2){var r=StandaloneGrowthBatchSmoke.Send(b,kind,cat,choice,slot:slot);Check(r.result==UnityEngine.Networking.UnityWebRequest.Result.Success,kind+":"+r.error);}
  void Apply(){typeof(PythonStateClient).GetMethod("Accept",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(client,new object[]{b.View()});p.Open();Canvas.ForceUpdateCanvases();}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
  {
   var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--catcafe-player-layout");if(i<0)return;
   var q=new GameObject("Player layout checks").AddComponent<StandalonePlayerLayoutSmoke>();DontDestroyOnLoad(q);q.output=Path.GetFullPath(a[i+1]);Directory.CreateDirectory(q.output);Application.runInBackground=true;q.StartCoroutine(q.Guard());
  }
  IEnumerator Guard(){var r=Run();while(true){bool more;try{more=r.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}if(!more)yield break;yield return r.Current;}}
  IEnumerator Shot(string name){Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.25f);}
  Vector2 ImageSize(){var corners=new Vector3[4];p.picture.rectTransform.GetWorldCorners(corners);return corners[2]-corners[0];}
  IEnumerator Run()
  {
   var rules=JsonUtility.FromJson<Standalone.CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);b=new LocalCafeBackend(Path.Combine(output,"save"),rules);
   bool resume=Environment.GetCommandLineArgs().Contains("--layout-resume");
   if(resume)Good("load_game",slot:Environment.GetCommandLineArgs().Contains("--layout-active-resume")?3:2);else {Good("new_game",choice:"free");Good("player_begin",cat:"cat-mike");}
   CafeBackends.UseLocal(b);SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
   client=FindObjectOfType<PythonStateClient>();p=client.GetComponent<PythonPlayerPanel>();Apply();
   if(resume){Check(b.View().player_interaction!=null,"Resume lost session");yield return Shot("resumed");File.WriteAllText(Path.Combine(output,"resume.txt"),"PASS "+checks);Application.Quit(0);yield break;}
   foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1024,768)})
   {
    Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.6f);Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.6f);Apply();yield return Shot("preparation-"+size.x);
    var initial=ImageSize();Check(initial.y>=319,"Small image");
    string before=JsonUtility.ToJson(b.Simulation.State);p.historyToggle.onClick.Invoke();Check(before==JsonUtility.ToJson(b.Simulation.State),"History consumed turn");Check(p.history.text==b.View().player_interaction.history || string.IsNullOrEmpty(b.View().player_interaction.history),"History missing");p.historyToggle.onClick.Invoke();
    // Normal UI action dispatch, using the same button listeners as player input.
    p.actions[0].onClick.Invoke();yield return new WaitForSecondsRealtime(.6f);Apply();Check(b.View().player_interaction.remaining_ticks<20,"UI action not dispatched");yield return Shot("action-"+size.x);
    before=JsonUtility.ToJson(b.Simulation.State);p.historyToggle.onClick.Invoke();yield return Shot("history-"+size.x);Check(before==JsonUtility.ToJson(b.Simulation.State),"History changed simulation");Check(Vector2.Distance(initial,ImageSize())<1,"History resized image");p.historyToggle.onClick.Invoke();
    var fixture=JsonUtility.FromJson<PythonCafeState>(JsonUtility.ToJson(b.View()));fixture.player_interaction.history="最新のやり取り（20手目）\n\n"+string.Concat(Enumerable.Repeat("長いセリフと猫の反応、地の文です。\n",80))+"\n\n── 履歴（新しい順） ──\n古い本文\n\n── 数値履歴 ──\n体力";
    var fixtureCat=fixture.cats.Single(c=>c.cat_id==fixture.player_interaction.cat_id);fixtureCat.max_stamina=300;fixtureCat.stamina=250;p.Apply(fixture);Canvas.ForceUpdateCanvases();Check(p.summary.text.Contains("250 / 300"),"Maximum growth missing");Check(!p.history.text.Contains("古い本文"),"Latest mixed with history");Check(Vector2.Distance(initial,ImageSize())<1,"Long prose resized image");yield return Shot("long-fixture-"+size.x);p.historyToggle.onClick.Invoke();Check(p.history.text.Contains("古い本文"),"Past prose lost");p.historyToggle.onClick.Invoke();
    fixture.player_interaction.narrative_image="missing";fixture.player_interaction.content_cat_id="missing";p.Apply(fixture);Check(p.picture.texture!=null,"Placeholder missing");yield return Shot("fallback-"+size.x);Apply();
    File.AppendAllText(Path.Combine(output,"dimensions.txt"),size+" image="+initial+" frame="+p.picture.transform.parent.GetComponent<RectTransform>().rect.size+"\n");
   }
   Good("save_game");Good("load_game");Apply();Check(b.View().player_interaction.active,"Active load failed");
   p.ReturnToDetails();Check(!p.panel.activeSelf,"Details return failed");p.Open();Check(p.panel.activeSelf,"Details resume failed");
   Good("save_game",slot:3);Apply();p.finish.onClick.Invoke();yield return new WaitForSecondsRealtime(.6f);Apply();Check(!b.View().player_interaction.active,"Finish UI failed");Check(p.preparation.interactable,"Preparation missing");yield return Shot("finished");Good("save_game");Good("load_game");Apply();Check(!b.View().player_interaction.active,"Result load failed");
   if(Environment.GetCommandLineArgs().Contains("--layout-hold")){Good("load_game",slot:3);Apply();File.WriteAllText(Path.Combine(output,"ready.txt"),"Physical input ready");while(true)yield return null;}
   p.preparation.onClick.Invoke();Check(!p.panel.activeSelf && client.GetComponent<PythonPreparationPanel>().panel.activeSelf,"Preparation return failed");
   File.WriteAllText(Path.Combine(output,"result.txt"),"PASS "+checks);Application.Quit(0);
  }
 }
}
