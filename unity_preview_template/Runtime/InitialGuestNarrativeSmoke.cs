using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public class InitialGuestNarrativeSmoke:MonoBehaviour {
  string output;bool stay;
  [Serializable] class ReceiptList{public CafeNarrativeRow[] rows;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--catcafe-initial-guests");if(i<0)return;var q=new GameObject("Initial Guest QA").AddComponent<InitialGuestNarrativeSmoke>();DontDestroyOnLoad(q.gameObject);q.output=Path.GetFullPath(a[i+1]);q.stay=a.Contains("--guest-ui");q.StartCoroutine(q.Run());}
  static CafeRules Rules(string person){var r=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);r.store_events.probability=0;r.arrival_ticks=person=="guest-2"?new[]{39,0}:new[]{0,39};foreach(var p in r.patterns)p.days=new[]{0,1,2,3,4,5,6};return r;}
  IEnumerator Run(){yield return null;if(stay && UnityEngine.Object.FindObjectOfType<ContentPreview>()==null){UnityEngine.SceneManagement.SceneManager.LoadScene("CatCafePythonState");yield return null;}try{
   foreach(string person in new[]{"guest-1","guest-2"}){
    var b=new LocalCafeBackend(Path.Combine(output,person+"-saves"),Rules(person));CafeResponse reply=null;var cmd=new PythonShiftCommand{kind="load_game",save_id="2",save_slot=2,request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision};var routine=b.Request("/commands",JsonUtility.ToJson(cmd),r=>reply=r);while(routine.MoveNext()){}
    if(reply==null||reply.result!=UnityEngine.Networking.UnityWebRequest.Result.Success)throw new Exception(reply?.text??"No response");
    string old=File.ReadAllText(Path.Combine(output,person+"-receipts.json"));if(JsonUtility.ToJson(new ReceiptList{rows=b.Narratives.Rows.ToArray()})!=old)throw new Exception("Player restart changed IDs/text");
    var catalog=CafeNarrativeResources.Load();if(catalog.entries.Length!=251)throw new Exception("Player catalog count");
    foreach(var row in b.Narratives.Rows){if(!string.IsNullOrEmpty(row.image_resource)&&CafeNarrativeResources.Texture(row.image_resource)==null)throw new Exception("Saved image unavailable");}
    string state=JsonUtility.ToJson(b.Simulation.State);b.Narratives.SetCatalog(null);foreach(var row in b.Narratives.Rows)b.Narratives.Read(row.session);if(state!=JsonUtility.ToJson(b.Simulation.State)||JsonUtility.ToJson(new ReceiptList{rows=b.Narratives.Rows.ToArray()})!=old)throw new Exception("Player read regenerated receipt");b.Narratives.SetCatalog(catalog);
    if(stay&&person=="guest-2"){
     var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",f).SetValue(client,preview);CafeBackends.UseLocal(b);var v=b.View();typeof(PythonStateClient).GetMethod("Accept",f).Invoke(client,new object[]{v});var seat=v.seats.First(s=>s.cat_id=="cat-tama");var cat=v.cats.Single(c=>c.cat_id=="cat-tama");var panel=preview.GetComponent<PythonSeatNarrativePanel>()??preview.gameObject.AddComponent<PythonSeatNarrativePanel>();panel.Open(seat,Array.IndexOf(v.seats,seat),cat);
    }
   }
   File.WriteAllText(Path.Combine(output,"player-result.txt"),"PASS: shipped Player Resources 251 pools; both guests disk restart exact text/candidate IDs/image refs; receipt viewing without catalog unchanged.");if(!stay)Application.Quit(0);
  }catch(Exception e){File.WriteAllText(Path.Combine(output,"player-failure.txt"),e.ToString());Application.Quit(1);}}
 }
}
