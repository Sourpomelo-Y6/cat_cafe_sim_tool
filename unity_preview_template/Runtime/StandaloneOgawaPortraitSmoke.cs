using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public sealed class StandaloneOgawaPortraitSmoke:MonoBehaviour {
  const string Target="event-guest-41";string output;int checks;
  static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--catcafe-ogawa-portrait");if(i<0)return;var q=new GameObject("Ogawa portrait QA").AddComponent<StandaloneOgawaPortraitSmoke>();DontDestroyOnLoad(q);q.output=Path.GetFullPath(a[i+1]);Directory.CreateDirectory(q.output);Application.runInBackground=true;q.StartCoroutine(q.Guard());}
  void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
  IEnumerator Guard(){var work=Run();while(true){bool more;try{more=work.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Application.Quit(1);yield break;}if(!more)break;yield return work.Current;}File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" checks. Isolated view/trust fixtures; no natural occurrence or physical input claim.");Application.Quit(0);}
  void Apply(PythonStateClient c,PythonCafeState v){typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(c,new object[]{v});Canvas.ForceUpdateCanvases();}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
  IEnumerator Run(){
   var b=new LocalCafeBackend(Path.Combine(output,"isolated-save"));CafeBackends.UseLocal(b);
   Check(b.Details.CustomerName(Target)=="小川さん","Actual stable ID name");
   var pack=JsonUtility.FromJson<HumanPortraitPack>(Resources.Load<TextAsset>("CatCafeContent/human_portraits").text);
   Check(pack.people.Length==25,"25 bindings");
   Check(pack.people.Select(r=>HumanPortraitResources.Resolve(r.game_id)).Distinct().Count()==25,"25 distinct portraits");
   var texture=HumanPortraitResources.Resolve(Target);Check(texture!=HumanPortraitResources.Placeholder(),"Ogawa loaded");
   foreach(var id in new[]{"guest-29","guest-49","event-guest-42","unknown"})Check(HumanPortraitResources.Resolve(id)==HumanPortraitResources.Placeholder(),"No alias "+id);
   b.Simulation.Execute("set_shifts",working:b.Simulation.State.cats.Select(c=>c.id).ToArray());b.Simulation.Execute("start_business");
   string before=JsonUtility.ToJson(b.Simulation.State);
   SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
   var client=FindObjectOfType<PythonStateClient>();var screen=client.GetComponent<PythonServiceScreen>();Screen.SetResolution(1024,768,false);yield return new WaitForSecondsRealtime(.6f);
   foreach(int width in new[]{1280,1024}){
    Screen.SetResolution(width,width==1280?800:768,false);yield return new WaitForSecondsRealtime(.5f);
    var view=b.View();var seat=view.seats[0];seat.customer_id=Target;seat.customer_name=b.Details.CustomerName(Target);seat.cat_id=view.cats[0].cat_id;seat.narrative="小川さんの画像確認用の隔離表示。過去本文は変更しない。";view.customers=new[]{new PythonCustomerState{customer_id=Target,name=seat.customer_name,status="seated",seat_id=seat.seat_id}};Apply(client,view);screen.ShowSeats();yield return null;
    var card=screen.seatContent.GetComponentsInChildren<PythonCatCard>().First(c=>c.customerIcon.activeSelf);
    Check(card.customerIcon.GetComponentInChildren<RawImage>().texture==texture,"Seat texture "+width);yield return Shot("seat-"+width);
    Check(screen.detailText.text.Contains("小川さん"),"Customer list name "+width);card.details.onClick.Invoke();yield return null;var detail=client.GetComponent<PythonSeatNarrativePanel>();Check(detail.panel.activeSelf && detail.title.text.Contains("小川さん"),"Seat detail name "+width);Check(detail.picture.texture!=texture,"Detail keeps cat picture "+width);yield return Shot("detail-"+width);detail.panel.SetActive(false);
    seat.customer_id="guest-29";screen.Apply(view);Check(card.customerIcon.GetComponentInChildren<RawImage>().texture==HumanPortraitResources.Placeholder(),"Same name replacement "+width);
    seat.customer_id=null;screen.Apply(view);Check(!card.customerIcon.activeSelf,"Empty seat "+width);
    seat.customer_id="guest-1";screen.Apply(view);Check(card.customerIcon.GetComponentInChildren<RawImage>().texture==HumanPortraitResources.Resolve("guest-1"),"Existing Sato "+width);
   }
   Check(before==JsonUtility.ToJson(b.Simulation.State),"Rendering preserves gameplay");
   var trust=new LocalCafeBackend(Path.Combine(output,"isolated-trust"));trust.Simulation.State.trust_events.Add(new CafeTrustEvent{id="ogawa-fixture",customer_id=Target,status="waiting",day=1,suspensions=1});CafeBackends.UseLocal(trust);Apply(client,trust.View());
   var owner=client.GetComponent<PythonCustomerTrustPanel>();var ui=owner.Dialogue;ui.Apply(trust.View(),owner);Check(ui.OpenPending(),"Trust open");ui.Next();
   Check((Texture2D)typeof(TrustConversationPanel).GetMethod("PortraitTexture",Hidden).Invoke(ui,new object[]{""})==texture,"Trust portrait ID");
   string trustBefore=JsonUtility.ToJson(trust.Simulation.State);
   foreach(int width in new[]{1280,1024}){Screen.SetResolution(width,width==1280?800:768,false);yield return new WaitForSecondsRealtime(.4f);yield return Shot("trust-"+width);}ui.Close();
   Check(trustBefore==JsonUtility.ToJson(trust.Simulation.State),"Trust display preserves state");
  }
 }
}
