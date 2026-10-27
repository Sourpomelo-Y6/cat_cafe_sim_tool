using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
 public class SpecialNarrativeSmoke:MonoBehaviour
 {
  string output;bool resume,stay;
  [Serializable] class ReceiptList{public CafeNarrativeRow[] rows;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
  {
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-special-qa");if(i<0)return;
   var test=new GameObject("Special narrative QA").AddComponent<SpecialNarrativeSmoke>();DontDestroyOnLoad(test.gameObject);
   test.output=Path.GetFullPath(args[i+1]);test.resume=args.Contains("--special-resume");test.stay=args.Contains("--special-stay");test.StartCoroutine(test.Run());
  }
  static CafeRules Rules(string scene)
  {
   var r=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);r.store_events.probability=0;
   r.exchange.direct_gain=scene=="connect"?1:100;r.exchange.confused_threshold=0;r.exchange.enthusiastic_threshold=0;
   r.exchange.tension_enthusiastic=scene=="open_up"?0:80;r.exchange.tension_neutral=r.exchange.tension_favorable=r.exchange.tension_enthusiastic;
   var cat=r.cats.Single(c=>c.id=="cat-mike");cat.type_preferences=cat.type_preferences.Select(x=>1d).ToArray();cat.intensity_preferences=cat.intensity_preferences.Select(x=>1d).ToArray();cat.boredom_decay=0;return r;
  }
  static void Send(LocalCafeBackend b,string kind)
  {
   CafeResponse reply=null;var c=new PythonShiftCommand{kind=kind,save_id="2",save_slot=2,request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision};
   var routine=b.Request("/commands",JsonUtility.ToJson(c),r=>reply=r);while(routine.MoveNext()){}
   if(reply==null || reply.result!=UnityEngine.Networking.UnityWebRequest.Result.Success)throw new Exception(reply?.text??"No response");
  }
  IEnumerator Run()
  {
   Directory.CreateDirectory(output);yield return null;
   if(UnityEngine.Object.FindObjectOfType<ContentPreview>()==null){UnityEngine.SceneManagement.SceneManager.LoadScene("CatCafePythonState");yield return null;}
   var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var flags=BindingFlags.Instance|BindingFlags.NonPublic;var client=preview.GetComponent<PythonStateClient>();typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
   foreach(string scene in new[]{"switch","connect","open_up","simultaneous"})
   {
    LocalCafeBackend b=null;
    try
    {
     b=new LocalCafeBackend(Path.Combine(output,scene+"-saves"),Rules(scene));
     if(resume)Send(b,"load_game");else
     {
      var s=b.Simulation;s.Execute("player_begin",catId:"cat-mike");
      if(scene=="switch"){s.Execute("player_step",choice:"switch",source:"ball");s.Execute("player_step",choice:"switch",source:"teaser");}
      else{s.Execute("player_step",choice:"direct");s.Execute("player_step",choice:scene=="open_up"?"direct":"connect");}
      Send(b,"save_game");File.WriteAllText(Path.Combine(output,scene+"-receipts.json"),JsonUtility.ToJson(new ReceiptList{rows=b.Narratives.Rows.ToArray()}));
     }
     if(JsonUtility.ToJson(new ReceiptList{rows=b.Narratives.Rows.ToArray()})!=File.ReadAllText(Path.Combine(output,scene+"-receipts.json")))throw new Exception("Saved receipt changed "+scene);
     var row=b.Narratives.Rows.Last();if(row.narrative_scene!=scene || string.IsNullOrEmpty(row.speech_id) || string.IsNullOrEmpty(row.cat_text_id))throw new Exception("Dedicated pool not selected "+scene);
     foreach(var r in b.Narratives.Rows)if(!string.IsNullOrEmpty(r.image_resource) && CafeNarrativeResources.Texture(r.image_resource)==null)throw new Exception("Saved image missing "+r.image_resource);
     string old=JsonUtility.ToJson(b.Simulation.State),rows=JsonUtility.ToJson(new ReceiptList{rows=b.Narratives.Rows.ToArray()});b.Narratives.SetCatalog(null);for(int j=0;j<10;j++)b.Narratives.Read(b.Simulation.ActivePlayer.id);
     if(old!=JsonUtility.ToJson(b.Simulation.State) || rows!=JsonUtility.ToJson(new ReceiptList{rows=b.Narratives.Rows.ToArray()}))throw new Exception("View regenerated receipt");b.Narratives.SetCatalog(CafeNarrativeResources.Load());
     CafeBackends.UseLocal(b);typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{b.View()});preview.GetComponent<PythonPlayerPanel>().Open();
    }
    catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Application.Quit(1);yield break;}
    foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1024,768)})
    {
     Screen.SetResolution(size.x,size.y,false);yield return null;yield return new WaitForEndOfFrame();
     File.AppendAllText(Path.Combine(output,resume?"resume-dimensions.txt":"dimensions.txt"),scene+" requested "+size.x+"x"+size.y+" rendered "+Screen.width+"x"+Screen.height+"\n");
     ScreenCapture.CaptureScreenshot(Path.Combine(output,(resume?"resume-":"")+scene+"-"+size.x+".png"));yield return null;
    }
    if(scene!="simultaneous" || !stay)preview.GetComponent<PythonPlayerPanel>().Close();
   }
   File.WriteAllText(Path.Combine(output,resume?"resume-result.txt":"result.txt"),"PASS: shipped Player v4 Resources; four confirmed fixture scenes / independent receipts; exact save text, IDs, revision and images; read-only; both rendered dimensions. Fixture rules only, production rules untouched.");
   if(!stay)Application.Quit(0);
  }
 }
}
