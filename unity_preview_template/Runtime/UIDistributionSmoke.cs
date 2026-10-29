using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
namespace CatCafePreview {
 // Opt-in QA only. Dedicated saves; long text exists only in a view fixture.
 public class UIDistributionSmoke : MonoBehaviour {
  public static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
  public const string EndMarker="[UI QA END]";
  string output; LocalCafeBackend backend; PythonCatDetailsPanel detail; float last=-999; int frame;
  static void Resolution(int w,int h) {
   Screen.SetResolution(w,h,false);
#if UNITY_EDITOR
   var asm=typeof(UnityEditor.Editor).Assembly;var sizesType=asm.GetType("UnityEditor.GameViewSizes");
   var singleton=typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
   var sizes=singleton.GetProperty("instance").GetValue(null,null);
   var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});
   var sizeType=asm.GetType("UnityEditor.GameViewSize");var kind=asm.GetType("UnityEditor.GameViewSizeType");
   var size=Activator.CreateInstance(sizeType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{Enum.ToObject(kind,1),w,h,"UI QA "+w},null);
   group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
   int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
   var gv=UnityEditor.EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));gv.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(gv,count-1,null);gv.Focus();
#endif
  }
  public static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  public static void Accept(ContentPreview p,PythonCafeState view) {
   var c=p.GetComponent<PythonStateClient>();typeof(PythonStateClient).GetField("preview",Hidden).SetValue(c,p);
   typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(c,new object[]{view});
   p.GetComponent<PythonPreparationPanel>()?.Close();
  }
  public static void Preparation(ContentPreview p) {
   p.GetComponent<PythonCatDetailsPanel>().Close();var h=p.GetComponent<PythonBusinessHeader>();var s=p.GetComponent<PythonServiceScreen>();
   Check(h.summary.text.Contains("営業準備") && h.preparation.gameObject.activeSelf && !h.auto.toggle.gameObject.activeSelf,"Preparation header");
   Check(s.rosterPanel.activeSelf && !s.detailText.text.Contains("三毛の毛並み"),"Roster and sidebar");
  }
  public static void Bounds(PythonCatDetailsPanel d) {
   Canvas.ForceUpdateCanvases();var r=d.scroll.GetComponent<RectTransform>();
   Check(r.sizeDelta==new Vector2(488,236) && r.anchoredPosition.x+r.rect.width<=860 && -r.anchoredPosition.y+r.rect.height<374,"Detail area overlaps lower controls");
   Check(d.details.rectTransform.rect.width<=d.scroll.viewport.rect.width+.1f,"Text exceeds viewport");
   var card=(RectTransform)r.parent;
   var corners=new Vector3[4];r.GetWorldCorners(corners);float bottom=corners.Min(v=>card.InverseTransformPoint(v).y);
   foreach(var b in new[]{d.interact,d.dispatch,d.adoption}){b.GetComponent<RectTransform>().GetWorldCorners(corners);Check(corners.Max(v=>card.InverseTransformPoint(v).y)<bottom,"Action overlaps body: "+b.name);}
  }
  public static PythonCafeState LongView(LocalCafeBackend b) {
   var v=b.View();var cat=v.cats.First(c=>c.cat_id=="cat-mike");
   cat.profile_details=string.Join("\n\n",Enumerable.Repeat(cat.profile_details,8))+"\n"+EndMarker;return v;
  }
  public static void Endpoint(PythonCatDetailsPanel d) {
   Bounds(d);d.scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();
   Check(d.details.text.EndsWith(EndMarker) && d.scroll.content.rect.height>d.scroll.viewport.rect.height,"Long profile fixture missing");
   var corners=new Vector3[4];d.scroll.content.GetWorldCorners(corners);float end=d.scroll.viewport.InverseTransformPoint(corners[0]).y;
   Check(Mathf.Abs(end-d.scroll.viewport.rect.yMin)<2,"Scroll bottom does not reach viewport bottom");
  }
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install() {
   var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--catcafe-ui-distribution");
   string path=i>=0?a[i+1]:Environment.GetEnvironmentVariable("CATCAFE_UI_PLAY_OUTPUT");if(string.IsNullOrEmpty(path))return;
   var q=new GameObject("UI distribution QA").AddComponent<UIDistributionSmoke>();DontDestroyOnLoad(q);q.output=Path.GetFullPath(path);Directory.CreateDirectory(q.output);Application.runInBackground=true;q.StartCoroutine(q.Guard());
  }
  IEnumerator Guard(){var e=Run();while(true){bool more;try{more=e.MoveNext();}catch(Exception ex){File.WriteAllText(Path.Combine(output,"failure.txt"),ex.ToString());Debug.LogException(ex);yield break;}if(!more)yield break;yield return e.Current;}}
  IEnumerator Run() {
   var args=Environment.GetCommandLineArgs();backend=new LocalCafeBackend(Path.Combine(output,"save"));
   bool resume=args.Contains("--ui-resume");
   if(resume){StandaloneDispatchConversationSmoke.Good(backend,"load_game",slot:1);Check(File.ReadAllText(Path.Combine(output,"receipt.json"))==JsonUtility.ToJson(backend.View().player_interaction),"Restart changed representative text/image receipt");}
   else StandaloneDispatchConversationSmoke.Good(backend,"new_game","free");
   CafeBackends.UseLocal(backend);SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
   var p=FindObjectOfType<ContentPreview>();p.GetComponent<PythonStateClient>().StopAllCoroutines();Accept(p,backend.View());detail=p.GetComponent<PythonCatDetailsPanel>();
   if(resume){p.GetComponent<PythonPlayerPanel>().Open();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"resume.png"));File.WriteAllText(Path.Combine(output,"resume.txt"),"PASS: separate process restored exact player text/image receipt and loaded image; dedicated slot1.");Check(p.GetComponent<PythonPlayerPanel>().picture.texture!=null,"Resume image missing");yield break;}
   string stateBefore=JsonUtility.ToJson(backend.Simulation.State);
   foreach(int width in new[]{1280,1024}) {
    Resolution(width,width==1280?800:768);yield return new WaitForSecondsRealtime(1);Check(Screen.width==width && Screen.height==(width==1280?800:768),"Actual render resolution");Accept(p,backend.View());Preparation(p);
    yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"preparation-"+width+".png"));yield return null;
    detail.OpenFromList("cat-mike",false);Check(detail.panel.activeSelf,"Open from roster");
    for(int tab=0;tab<5;tab++){detail.recordTabs[tab].onClick.Invoke();Bounds(detail);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"tab-"+tab+"-"+width+".png"));yield return null;}
    Accept(p,LongView(backend));detail.Profile();Endpoint(detail);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"long-end-"+width+".png"));yield return null;
    detail.panel.transform.Find("Details Card/Close").GetComponent<Button>().onClick.Invoke();Check(!detail.panel.activeSelf && p.GetComponent<PythonServiceScreen>().rosterPanel.activeSelf,"Return to roster");
   }
   Check(stateBefore==JsonUtility.ToJson(backend.Simulation.State),"View fixture changed game state");
   // Normal representative action/save, after discarding the long view.
   StandaloneDispatchConversationSmoke.Good(backend,"player_begin",cat:"cat-mike");StandaloneDispatchConversationSmoke.Good(backend,"player_step","direct");StandaloneDispatchConversationSmoke.Good(backend,"save_game",slot:1);
   File.WriteAllText(Path.Combine(output,"receipt.json"),JsonUtility.ToJson(backend.View().player_interaction));
   Check(!File.ReadAllText(Path.Combine(output,"save/slot-1.json")).Contains(EndMarker),"Fixture leaked into save");
   Accept(p,backend.View());p.GetComponent<PythonPlayerPanel>().Open();Check(p.GetComponent<PythonPlayerPanel>().picture.texture!=null,"Representative image missing");
   yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"representative.png"));yield return null;p.GetComponent<PythonPlayerPanel>().Close();
   File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: preparation, five tabs, long profile endpoint and bounds, return to roster at 1280x800/1024x768. UI listeners and view fixture. State unchanged; representative action and dedicated save. Physical input recorded separately.");
   int w=args.Contains("--ui-1280")?1280:1024;Resolution(w,w==1280?800:768);yield return new WaitForSecondsRealtime(1);
   Accept(p,LongView(backend));detail.OpenFromList("cat-mike",false);detail.Profile();File.WriteAllText(Path.Combine(output,"input-ready.txt"),"Ready: long profile fixture. Wheel/drag and close via OS input.");
  }
  void Update(){if(detail==null || !File.Exists(Path.Combine(output,"input-ready.txt")))return;
   if(Input.GetKeyDown(KeyCode.F8)){int w=Screen.width==1280?1024:1280;Resolution(w,w==1280?800:768);detail.scroll.verticalNormalizedPosition=1;}
   if(frame++%15!=0)return;float value=detail.scroll.verticalNormalizedPosition;if(Mathf.Abs(last-value)>.0001f){last=value;File.AppendAllText(Path.Combine(output,"input-trace.txt"),Time.frameCount+" width="+Screen.width+" pos="+value+" y="+detail.scroll.content.anchoredPosition.y+" height="+detail.scroll.content.rect.height+" viewport="+detail.scroll.viewport.rect.height+"\n");} }
 }
}
