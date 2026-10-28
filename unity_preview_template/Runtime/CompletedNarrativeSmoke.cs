using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 // Opt-in bounded integration audit. All saves are rooted at the explicit QA directory.
 public class CompletedNarrativeSmoke:MonoBehaviour {
  [Serializable] public class ReceiptList {public CafeNarrativeRow[] rows;}
  public class Case {public string id,cat,mode,scene="normal",person="player";public bool low,capture;public int missing;}
  static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  public static CafeRules Rules(string scene="normal",string person="player") {
   var r=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
   if(scene!="normal") {
    r.exchange.direct_gain=scene=="connect"?1:100;r.exchange.confused_threshold=0;r.exchange.enthusiastic_threshold=0;
    r.exchange.tension_enthusiastic=scene=="open_up"?0:80;r.exchange.tension_neutral=r.exchange.tension_favorable=r.exchange.tension_enthusiastic;
    var c=r.cats.Single(x=>x.id=="cat-mike");c.type_preferences=c.type_preferences.Select(x=>1d).ToArray();c.intensity_preferences=c.intensity_preferences.Select(x=>1d).ToArray();c.boredom_decay=0;
    foreach(var t in r.types)t.gain=1; // Equalize fixture modes; production gains remain untouched.
   }
   r.store_events.probability=0;
   if(person!="player") {r.arrival_ticks=new[]{39,39,0};foreach(var p in r.patterns)p.days=new[]{0,1,2,3,4,5,6};}
   return r;
  }
  public static List<Case> Cases() {
   var list=new List<Case>();var modes=new[]{"teaser","ball","plush","tunnel","pet","brush","voice","presence"};var scenes=new[]{"switch","connect","open_up","simultaneous"};
   for(int m=0;m<modes.Length;m++)for(int s=0;s<scenes.Length;s++)list.Add(new Case{id=modes[m]+"-"+scenes[s],cat="cat-mike",mode=modes[m],scene=scenes[s],capture=s==m%4});
   int i=0;foreach(string cat in new[]{"cat-mike","cat-sora","cat-tama","cat-kohaku","cat-mugi"}) {
    list.Add(new Case{id=cat+"-normal",cat=cat,mode=modes[i++],capture=true});
    list.Add(new Case{id=cat+"-low",cat=cat,mode="pause",low=true,capture=cat=="cat-mike"});
   }
   foreach(string mode in modes.Concat(new[]{"pause"}))list.Add(new Case{id="guest-3-"+mode,cat="cat-tama",mode=mode,person="guest-3",capture=mode=="ball"||mode=="presence"||mode=="pause"});
   foreach(int missing in new[]{1,2,3})list.Add(new Case{id="partial-"+missing,cat="cat-mike",mode="presence",scene="open_up",capture=true,missing=missing});
   return list;
  }
  public static int Audit(string output) {
   Directory.CreateDirectory(output);var catalog=CafeNarrativeResources.Load();catalog.Validate();
   Check(catalog.entries.Length==435 && catalog.entries.Sum(e=>e.variants.Length)==1123,"Actual Resources counts");int count=0;var shadowed=new List<string>();
   foreach(var entry in catalog.entries) {
    string cat=entry.kind=="cat_response"?catalog.cat_bindings.Single(c=>c.subject_id==entry.subject_id).cat_id:entry.target_cat=="*"?"cat-tama":catalog.cat_bindings.Single(c=>c.subject_id==entry.target_cat).cat_id;
    string person=entry.kind=="speech"?catalog.people.Single(p=>p.id==entry.subject_id).game_id:"player";
    string action=CafeNarrativeCatalog.EntryActions(entry.mode,entry.NarrativeScene).Where(a=>a!="any").FirstOrDefault(a=>catalog.Find(entry.kind,person,entry.mode,a,entry.reaction,cat,entry.NarrativeScene)?.Key()==entry.Key());
    if(action==null){shadowed.Add(entry.Key());continue;}
    var sim=new CafeSimulation(Rules());var n=new CafeNarratives(sim);n.SetCatalog(catalog);
    foreach(var v in entry.variants.Concat(entry.variants.Take(1))) {
     var row=Apply(n,cat,person,entry.mode,action,entry.reaction=="any"?"neutral":entry.reaction,entry.NarrativeScene);
     Check((entry.kind=="speech"?row.speech_id:row.cat_text_id)==v.id,"Candidate rotation "+entry.Key());
     string text=entry.kind=="speech"?row.speech:row.cat_response;
     Check(text.StartsWith(catalog.Expand(v.text,person,sim.Definition(cat).name)),"Exact expanded prose "+entry.Key());
     Check(!row.text.Contains("{cat"),"Unexpanded name");n.Rows.Add(row);count++;
    }
   }
   // Independently enumerate specificity, including legacy dedicated pools.
   foreach(string cat in new[]{"cat-mike","cat-sora","cat-tama","cat-kohaku","cat-mugi"})foreach(string person in new[]{"player","guest-1","guest-3","guest-4"})foreach(string mode in CafeNarrativeCatalog.Modes)foreach(string action in CafeNarrativeCatalog.Actions(mode).Where(a=>a!="any"))foreach(string reaction in new[]{"neutral","favorable","enthusiastic","turn_away","confused","listless"}) {
    string subject=catalog.people.FirstOrDefault(p=>p.game_id==person)?.id;string target=catalog.cat_bindings.Single(c=>c.cat_id==cat).subject_id;
    var expected=catalog.entries.Where(e=>e.NarrativeScene=="normal"&&e.kind=="speech"&&e.subject_id==subject&&e.mode==mode&&e.reaction=="any"&&(e.action==action||e.action=="any")&&(e.target_cat==target||e.target_cat=="*")).OrderBy(e=>e.target_cat==target?0:1).ThenBy(e=>e.action==action?0:1).FirstOrDefault();
    Check(catalog.Find("speech",person,mode,action,"any",cat)==expected,"Specific/common priority");
    var expectedCat=catalog.entries.Where(e=>e.NarrativeScene=="normal"&&e.kind=="cat_response"&&e.subject_id==target&&e.mode==mode&&e.reaction==reaction&&(e.action==action||e.action=="any")).OrderBy(e=>e.action==action?0:1).FirstOrDefault();
    Check(catalog.Find("cat_response",cat,mode,action,reaction,cat)==expectedCat,"Cat operation/common priority");count++;
   }
   // Remove only in memory: preserve one dedicated side and verify fallback independently.
   foreach(string mode in new[]{"teaser","ball","plush","tunnel","pet","brush","voice","presence"})foreach(string scene in new[]{"switch","connect","open_up","simultaneous"})foreach(int missing in new[]{0,1,2,3})foreach(string action in scene=="open_up"?new[]{"direct","adapt","pause","switch"}:new[]{scene=="switch"?"switch":"connect"}) {
    var c=JsonUtility.FromJson<CafeNarrativeCatalog>(JsonUtility.ToJson(catalog));c.entries=c.entries.Where(e=>!(e.mode==mode&&e.NarrativeScene==scene&&((e.kind=="speech"&&(missing&1)!=0)||(e.kind=="cat_response"&&(missing&2)!=0)))).ToArray();c.revision=c.ComputeRevision();
    var n=new CafeNarratives(new CafeSimulation(Rules()));n.SetCatalog(c);
    var row=Apply(n,"cat-mike","player",mode,action,"neutral",scene);
    Check(!string.IsNullOrEmpty(row.speech)&&!string.IsNullOrEmpty(row.cat_response),"Fallback empty");
    if((missing&1)==0)Check(c.entries.Any(e=>e.NarrativeScene==scene&&e.variants.Any(v=>v.id==row.speech_id)),"Dedicated human lost");
    if((missing&2)==0){Check(c.entries.Any(e=>e.NarrativeScene==scene&&e.variants.Any(v=>v.id==row.cat_text_id)),"Dedicated cat lost");Check(!row.cat_response.Contains("は心を開いた様子を見せた。"),"Duplicate special appendix");}
    else if(scene=="open_up"||scene=="simultaneous")Check(row.cat_response.Split(new[]{"は心を開いた様子を見せた。"},StringSplitOptions.None).Length==2,"Fallback appendix count");count++;
   }
   File.WriteAllText(Path.Combine(output,"selection.txt"),"PASS "+count+" actual-catalog candidate/priority/fallback checks; shadowed pools="+shadowed.Count+"\n"+string.Join("\n",shadowed));return count;
  }
  static CafeNarrativeRow Apply(CafeNarratives n,string cat,string person,string mode,string action,string reaction,string scene) {
   bool opened=scene=="open_up"||scene=="simultaneous";var row=CafeNarratives.Compose(person,mode,action,reaction,100,99,opened,action=="connect",n.Catalog.CatName(cat));row.cat_id=cat;row.customer_id=person;row.narrative_scene=scene;
   typeof(CafeNarratives).GetMethod("ApplyCatalog",Private).Invoke(n,new object[]{row,person,person,100d,99d,opened});return row;
  }
  public static LocalCafeBackend BuildCase(Case c,string output,bool resume) {
   var b=new LocalCafeBackend(Path.Combine(output,c.id+"-saves"),Rules(c.scene,c.person));
   string receipt=Path.Combine(output,c.id+"-receipts.json");
   if(resume)Send(b,"load_game");else {
    var s=b.Simulation;
    if(c.missing!=0) {var catalog=CafeNarrativeResources.Load();catalog.entries=catalog.entries.Where(e=>!(e.mode==c.mode&&e.NarrativeScene==c.scene&&((e.kind=="speech"&&(c.missing&1)!=0)||(e.kind=="cat_response"&&(c.missing&2)!=0)))).ToArray();catalog.revision=catalog.ComputeRevision();b.Narratives.SetCatalog(catalog);}
    if(c.person=="player") {
     if(c.low)s.State.cats.Single(x=>x.id==c.cat).stamina=10;
     s.Execute("player_begin",catId:c.cat);
     string mode=c.mode=="pause"?"teaser":c.mode;
     if(c.scene=="switch") {if(mode=="teaser")s.Execute("player_step",choice:"switch",source:"ball");s.Execute("player_step",choice:"switch",source:mode);}
     else {
      if(mode!="teaser")s.Execute("player_step",choice:"switch",source:mode);
      s.Execute("player_step",choice:c.mode=="pause"?"pause":"direct");
      if(c.scene!="normal")s.Execute("player_step",choice:c.scene=="open_up"?"direct":"connect");
     }
    } else {
     s.Execute("set_shifts",working:new[]{c.cat});s.Execute("start_business");while(!s.State.exchanges.Any(e=>e.customer_id==c.person)&&s.State.phase=="open")s.Execute("advance_business");
     var e=s.State.exchanges.Single(x=>x.customer_id==c.person);e.mode=c.mode=="pause"?"teaser":c.mode;e.stamina=c.mode=="pause"?10:100;e.previous_reaction="neutral";e.last_group=null;e.streak=0;e.tension=0;e.engagement=0;e.connect_pending=e.open_up_pending=false;
     if(c.mode=="pause") { // Service auto-choice need not rest; use a real player rest for guest pool selection in Audit.
      e.mode="presence";
     }
     s.Execute("advance_business");
    }
    Check(b.Narratives.Rows.Count>0,"No observed action "+c.id);
    Send(b,"save_game");File.WriteAllText(receipt,JsonUtility.ToJson(new ReceiptList{rows=b.Narratives.Rows.ToArray()}));
   }
   Check(JsonUtility.ToJson(new ReceiptList{rows=b.Narratives.Rows.ToArray()})==File.ReadAllText(receipt),"Disk restart receipt drift "+c.id);
   var last=b.Narratives.Rows.Last();Check((last.narrative_scene??"normal")==c.scene,"Actual scene "+c.id+" got "+last.narrative_scene);
   if(c.person=="guest-3")Check(!string.IsNullOrEmpty(last.speech_id),"Guest prose missing");
   if(c.scene!="normal"&&c.missing==0)Check(!string.IsNullOrEmpty(last.speech_id)&&!string.IsNullOrEmpty(last.cat_text_id),"Dedicated special missing "+c.id);
   foreach(var row in b.Narratives.Rows)if(!string.IsNullOrEmpty(row.image_resource))Check(CafeNarrativeResources.Texture(row.image_resource)!=null,"Saved image missing "+row.image_resource);
   string state=JsonUtility.ToJson(b.Simulation.State),rows=JsonUtility.ToJson(new ReceiptList{rows=b.Narratives.Rows.ToArray()});b.Narratives.SetCatalog(null);foreach(var row in b.Narratives.Rows)b.Narratives.Read(row.session);
   Check(state==JsonUtility.ToJson(b.Simulation.State)&&rows==JsonUtility.ToJson(new ReceiptList{rows=b.Narratives.Rows.ToArray()}),"Viewing regenerated history");b.Narratives.SetCatalog(CafeNarrativeResources.Load());return b;
  }
  public static void Show(ContentPreview preview,LocalCafeBackend b,Case c) {
   var client=preview.GetComponent<PythonStateClient>();typeof(PythonStateClient).GetField("preview",Private).SetValue(client,preview);CafeBackends.UseLocal(b);var view=b.View();typeof(PythonStateClient).GetMethod("Accept",Private).Invoke(client,new object[]{view});
   if(c.person=="player")preview.GetComponent<PythonPlayerPanel>().Open();
   else {var seat=view.seats.First(s=>s.cat_id==c.cat);var panel=preview.GetComponent<PythonSeatNarrativePanel>()??preview.gameObject.AddComponent<PythonSeatNarrativePanel>();panel.Open(seat,Array.IndexOf(view.seats,seat),view.cats.Single(x=>x.cat_id==c.cat));}
  }
  public static void Close(ContentPreview p) {p.GetComponent<PythonPlayerPanel>().Close();var panel=p.GetComponent<PythonSeatNarrativePanel>();if(panel!=null&&panel.panel!=null)panel.panel.SetActive(false);}
  public static void DescribeScroll(ContentPreview preview,string output) {
   var p=preview.GetComponent<PythonPlayerPanel>();Canvas.ForceUpdateCanvases();
   File.AppendAllText(Path.Combine(output,"scroll-layout.txt"),"preferred="+p.history.preferredHeight+" content="+p.history.rectTransform.rect.height+" viewport="+p.historyScroll.viewport.rect.height+" position="+p.historyScroll.verticalNormalizedPosition+" anchored="+p.history.rectTransform.anchoredPosition+"\n");
  }
  static void Send(LocalCafeBackend b,string kind) {
   CafeResponse response=null;var cmd=new PythonShiftCommand{kind=kind,save_id="2",save_slot=2,instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,request_id=Guid.NewGuid().ToString("N")};var routine=b.Request("/commands",JsonUtility.ToJson(cmd),r=>response=r);while(routine.MoveNext()){}Check(response!=null&&response.result==UnityEngine.Networking.UnityWebRequest.Result.Success,response?.text??"No save reply");
  }
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install() {
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-completed-narratives");if(i<0)return;var go=new GameObject("Completed narrative QA");DontDestroyOnLoad(go);go.AddComponent<CompletedNarrativeSmoke>().StartCoroutine(go.GetComponent<CompletedNarrativeSmoke>().Run(Path.GetFullPath(args[i+1]),args.Contains("--resume"),args.Contains("--stay")));
  }
  // Programmatic UI-event fixture; never counted as an OS mouse drag.
  static void VerifyScrollRouting(ContentPreview preview,string output) {
   var p=preview.GetComponent<PythonPlayerPanel>();var b=(LocalCafeBackend)CafeBackends.Local;
   var lines=new List<string>();
   foreach(bool history in new[]{false,true}) {
    if(history)p.ToggleHistory();Canvas.ForceUpdateCanvases();p.historyScroll.verticalNormalizedPosition=1;
    var corners=new Vector3[4];p.historyScroll.viewport.GetWorldCorners(corners);
    Vector2 start=RectTransformUtility.WorldToScreenPoint(null,Vector3.Lerp(corners[0],corners[2],.75f));
    var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left,position=start,pressPosition=start};
    var hits=new List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(data,hits);
    var hit=hits.FirstOrDefault();var handler=UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IDragHandler>(hit.gameObject);
    Check(handler==p.historyScroll.gameObject,"Body drag routing: "+hit.gameObject+" -> "+handler);data.pointerPressRaycast=hit;
    int revision=b.Simulation.State.revision,rows=b.Narratives.Rows.Count;
    p.historyScroll.OnInitializePotentialDrag(data);p.historyScroll.OnBeginDrag(data);
    data.position=start+Vector2.up*3000;p.historyScroll.OnDrag(data);p.historyScroll.OnEndDrag(data);Canvas.ForceUpdateCanvases();
    float bottom=p.historyScroll.verticalNormalizedPosition;Check(bottom<.001f,"Fixture drag did not reach bottom "+bottom);
    data.position=start;p.historyScroll.OnBeginDrag(data);data.position=start+Vector2.down*3000;p.historyScroll.OnDrag(data);p.historyScroll.OnEndDrag(data);Canvas.ForceUpdateCanvases();
    Check(p.historyScroll.verticalNormalizedPosition>.999f&&b.Simulation.State.revision==revision&&b.Narratives.Rows.Count==rows,"Fixture return/state failed");
    lines.Add(Screen.width+" history="+history+" hit="+hit.gameObject.name+" handler="+handler.name+" position=1 -> "+bottom+" -> 1; state/rows unchanged");
    if(history)p.ToggleHistory();
   }
   File.WriteAllLines(Path.Combine(output,"scroll-routing.txt"),lines);
  }
  ContentPreview scrollPreview;string scrollOutput;string lastScrollSample;
  void LateUpdate() {
   if(scrollPreview==null)return;
   var p=scrollPreview.GetComponent<PythonPlayerPanel>();if(!p.panel.activeSelf)return;
   var b=(LocalCafeBackend)CafeBackends.Local;
   string sample="text="+p.history.text.Length+" position="+p.historyScroll.verticalNormalizedPosition.ToString("F4")+" anchored="+p.history.rectTransform.anchoredPosition+" content="+p.history.rectTransform.rect.height+" viewport="+p.historyScroll.viewport.rect.height+" revision="+b.Simulation.State.revision+" rows="+b.Narratives.Rows.Count+" held="+Input.GetMouseButton(0)+" pointer="+(Input.GetMouseButton(0)?Input.mousePosition.ToString():"released");
   if(sample!=lastScrollSample){File.AppendAllText(Path.Combine(scrollOutput,"input-scroll.txt"),Time.frameCount+" "+sample+" wheel="+Input.mouseScrollDelta+" mouseHeld="+Input.GetMouseButton(0)+"\n");lastScrollSample=sample;}
  }
  IEnumerator Run(string output,bool resume,bool stay) {
   yield return null;if(UnityEngine.Object.FindObjectOfType<ContentPreview>()==null){UnityEngine.SceneManagement.SceneManager.LoadScene("CatCafePythonState");yield return null;}
   var p=UnityEngine.Object.FindObjectOfType<ContentPreview>();p.Initialize();
   if(Environment.GetCommandLineArgs().Contains("--scroll-only")) {
    var c=Cases().First(x=>x.id=="presence-simultaneous");
    Show(p,BuildCase(c,output,true),c);
    int width=Environment.GetCommandLineArgs().Contains("--scroll-1280")?1280:1024;
    Screen.SetResolution(width,width==1280?800:768,false);yield return null;
    try{VerifyScrollRouting(p,output);}catch(Exception e){File.WriteAllText(Path.Combine(output,"scroll-failure.txt"),e.ToString());Application.Quit(1);yield break;}
    scrollPreview=p;scrollOutput=output;yield break;
   }
   try{Audit(output);}catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Application.Quit(1);yield break;}
   foreach(var c in Cases()) {
    LocalCafeBackend b=null;try{b=BuildCase(c,output,resume);if(c.capture)Show(p,b,c);}catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),c.id+"\n"+e);Application.Quit(1);yield break;}
    if(c.capture)foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1024,768)}) {Screen.SetResolution(size.x,size.y,false);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,(resume?"resume-":"")+c.id+"-"+size.x+".png"));yield return null;}
    Close(p);
   }
   File.WriteAllText(Path.Combine(output,resume?"resume-result.txt":"result.txt"),"PASS: actual Resources 435 pools / 1123 texts; "+Cases().Count+" bounded observed-action cases (3 use memory-only partial catalogs); isolated disk saves; exact receipt IDs/text/revision/image references; no regeneration without catalog; representative rendered UI. Fixture rules/state, not natural-play reachability.");
   if(stay){var c=Cases().First(x=>x.id=="presence-simultaneous");Show(p,BuildCase(c,output,true),c);yield return null;DescribeScroll(p,output);}else Application.Quit(0);
  }
 }
}
