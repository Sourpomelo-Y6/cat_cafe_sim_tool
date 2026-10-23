using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
namespace CatCafePreview
{
 public class StandalonePatronGuidanceSmoke:MonoBehaviour
 {
  string output;LocalCafeBackend backend;int checks;
  void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
  void Good(string kind,string cat=null,string choice=null){var r=StandaloneGrowthBatchSmoke.Send(backend,kind,cat,choice);Check(r.result==UnityEngine.Networking.UnityWebRequest.Result.Success,kind+":"+r.error);}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
  {
   var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--catcafe-patron-guidance");if(i<0)return;
   var q=new GameObject("Patron guidance check").AddComponent<StandalonePatronGuidanceSmoke>();DontDestroyOnLoad(q);q.output=Path.GetFullPath(a[i+1]);Directory.CreateDirectory(q.output);Application.runInBackground=true;q.StartCoroutine(q.Guard());
  }
  IEnumerator Guard(){var r=Run();while(true){bool more;try{more=r.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}if(!more)yield break;yield return r.Current;}}
  void Apply(PythonStateClient c){typeof(PythonStateClient).GetMethod("Accept",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(c,new object[]{JsonUtility.FromJson<PythonCafeState>(JsonUtility.ToJson(backend.View()))});Canvas.ForceUpdateCanvases();}
  IEnumerator Run()
  {
   backend=new LocalCafeBackend(Path.Combine(output,"save"),StandaloneGrowthBatchSmoke.Rules());Good("new_game",choice:"patron");Good("day_off");Good("resolve_growth_auto");
   CafeBackends.UseLocal(backend);SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
   var client=FindObjectOfType<PythonStateClient>();var panel=client.GetComponent<PythonGrowthPanel>();
   var white=backend.Simulation.State.cats.First(c=>backend.Simulation.Definition(c.id).features.Contains("white"));
   var rows=backend.View().growth_choices;client.GrowthRowIndex=Array.FindIndex(rows,r=>r.cat_id==white.id);Check(client.GrowthRowIndex>=0,"White cat growth missing");
   foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1024,768)})
   {
    Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.5f);Apply(client);yield return null;
    Check(panel.panel.activeSelf && panel.details.text.Contains("白猫 かつ 接客専門"),"Guidance missing");
    var scroll=panel.details.GetComponentInParent<ScrollRect>();Check(scroll!=null,"Guidance needs scroll");scroll.verticalNormalizedPosition=1;Canvas.ForceUpdateCanvases();
    ScreenCapture.CaptureScreenshot(Path.Combine(output,"growth-"+size.x+".png"));yield return new WaitForEndOfFrame();yield return new WaitForSecondsRealtime(.25f);
    Check(scroll.content.rect.height>scroll.viewport.rect.height,"Long guidance clipped");scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(output,"reason-"+size.x+".png"));yield return new WaitForEndOfFrame();yield return new WaitForSecondsRealtime(.25f);
    Check(panel.choices.Any(b=>b.gameObject.activeSelf && b.interactable && b.GetComponentInChildren<Text>().text.Contains("自分で選ぶ")),"Manual choice unavailable");
   }
   Good("resolve_growth_stage",white.id,"service");Apply(client);Check(backend.View().growth_choices.Single(r=>r.cat_id==white.id).decision_reason.Contains("選択中の専門は希望に一致"),"Manual guide stale");
   Good("save_game");Good("load_game");Apply(client);Check(backend.View().growth_choices.Single(r=>r.cat_id==white.id).selected=="service","Staging restore");
   File.WriteAllText(Path.Combine(output,"before-confirm.json"),JsonUtility.ToJson(backend.Simulation.State));Good("resolve_growth_batch");Check(backend.Simulation.GrowthCat(white.id).specialization=="service","Manual choice lost");
   Good("next_day");Apply(client);var dispatch=client.GetComponent<PythonDispatchPanel>();
   foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1024,768)})
   {
    Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.5f);Apply(client);dispatch.OpenOption(white.id,"patron_strict_visit");yield return null;Canvas.ForceUpdateCanvases();
    Check(dispatch.details.text.Contains("白猫 かつ 接客専門") && dispatch.details.text.Contains("出発可否"),"Dispatch conditions missing");
    ScreenCapture.CaptureScreenshot(Path.Combine(output,"dispatch-"+size.x+".png"));yield return new WaitForEndOfFrame();yield return new WaitForSecondsRealtime(.25f);dispatch.Close();
    var prep=client.GetComponent<PythonPreparationPanel>();prep.Open();prep.panel.GetComponentsInChildren<Button>(true).Single(b=>b.GetComponentInChildren<Text>()?.text=="目標・営業の案内").onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();Check(prep.panel.activeSelf && prep.details.text.Contains("白猫 かつ 接客専門"),"Goal guide missing");
    prep.goalScroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(output,"goal-"+size.x+".png"));yield return new WaitForEndOfFrame();yield return new WaitForSecondsRealtime(.25f);prep.Close();
   }
   File.WriteAllText(Path.Combine(output,"result.txt"),"PASS "+checks);Application.Quit(0);
  }
 }
}
