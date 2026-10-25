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
 public sealed class StandaloneHumanIconsSmoke:MonoBehaviour {
  string output;int checks;static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
  void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--catcafe-human-icons");if(i<0)return;var q=new GameObject("Human icons QA").AddComponent<StandaloneHumanIconsSmoke>();DontDestroyOnLoad(q);q.output=Path.GetFullPath(a[i+1]);Directory.CreateDirectory(q.output);Application.runInBackground=true;q.StartCoroutine(q.Guard());}
  IEnumerator Guard(){var work=Run();while(true){bool more;try{more=work.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}if(!more)break;yield return work.Current;}File.WriteAllText(Path.Combine(output,Environment.GetCommandLineArgs().Contains("--catcafe-human-icons-resume")?"manual-result.txt":"result.txt"),"PASS: "+checks+" checks. Actual Player UI with isolated view/event/replay fixtures; no natural event or physical input claim.");Application.Quit(0);}
  void Apply(PythonStateClient client,PythonCafeState view){typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(client,new object[]{view});Canvas.ForceUpdateCanvases();}
  void Send(LocalCafeBackend b,string kind){var state=b.Simulation.State;CafeResponse response=null;var cmd=new PythonShiftCommand{kind=kind,request_id=Guid.NewGuid().ToString("N"),instance_id=state.instance_id,expected_revision=state.revision,save_id="1",save_slot=1,working_cats=new string[0]};var work=b.Request("/commands",JsonUtility.ToJson(cmd),r=>response=r);while(work.MoveNext()){}Check(response!=null && response.result==UnityEngine.Networking.UnityWebRequest.Result.Success,kind+": "+response?.error);}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
  // Resolve optional preparation requests only in the isolated Yoshida fixture.
  static void PrepareYoshidaFixture(CafeSimulation simulation){
   if(simulation.ReservationWaiting)simulation.Execute("resolve_reservation",choice:"decline");
   if(simulation.StoreEventWaiting)simulation.Execute("resolve_store_event",choice:simulation.CurrentStoreEvent.type=="trouble"?"patch":"decline");
   if(simulation.IntakeWaiting)simulation.Execute("resolve_intake",choice:"decline");
   if(simulation.RegularIntroductionWaiting)simulation.Execute("resolve_regular_introduction",choice:"decline");
  }
  IEnumerator Run(){
   bool fujiwara=Environment.GetCommandLineArgs().Contains("--catcafe-fujiwara");bool yamada=Environment.GetCommandLineArgs().Contains("--catcafe-yamada");bool yoshida=Environment.GetCommandLineArgs().Contains("--catcafe-yoshida");bool kato=Environment.GetCommandLineArgs().Contains("--catcafe-kato");bool kobayashi=Environment.GetCommandLineArgs().Contains("--catcafe-kobayashi");bool nakamura=Environment.GetCommandLineArgs().Contains("--catcafe-nakamura");bool yamamoto=Environment.GetCommandLineArgs().Contains("--catcafe-yamamoto");bool watanabe=Environment.GetCommandLineArgs().Contains("--catcafe-watanabe");bool ito=Environment.GetCommandLineArgs().Contains("--catcafe-ito");bool tanaka=Environment.GetCommandLineArgs().Contains("--catcafe-tanaka");bool takahashi=Environment.GetCommandLineArgs().Contains("--catcafe-takahashi");string target=fujiwara?"advanced-white":yamada?"guest-12":yoshida?"guest-11":kato?"guest-10":kobayashi?"guest-9":nakamura?"guest-8":yamamoto?"guest-7":watanabe?"guest-6":ito?"guest-5":tanaka?"guest-4":takahashi?"guest-3":"guest-2";var ids=fujiwara?new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9","guest-10","guest-11","guest-12","advanced-white"}:yamada?new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9","guest-10","guest-11","guest-12"}:yoshida?new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9","guest-10","guest-11"}:kato?new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9","guest-10"}:kobayashi?new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9"}:nakamura?new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8"}:yamamoto?new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7"}:watanabe?new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6"}:ito?new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5"}:tanaka?new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4"}:takahashi?new[]{"guest-1","guest-2","reservation-longhair","guest-3"}:new[]{"guest-1","guest-2","reservation-longhair"};var textures=ids.Select(HumanPortraitResources.Resolve).ToArray();Check(textures.All(t=>t!=null && t!=HumanPortraitResources.Placeholder()),"Missing customer icon");Check(textures.Distinct().Count()==ids.Length,"Customer identities share image");Check(HumanPortraitResources.Resolve("unknown")==HumanPortraitResources.Placeholder(),"Unknown customer");
   if(Environment.GetCommandLineArgs().Contains("--catcafe-human-icons-resume")){
    var loaded=new LocalCafeBackend(Path.Combine(output,"save"));Send(loaded,"load_game");CafeBackends.UseLocal(loaded);
    Check(loaded.View().seats.Any(s=>s.customer_id==target),"Separate process resumed target customer");
    File.WriteAllText(Path.Combine(output,"resumed.json"),JsonUtility.ToJson(loaded.View(),true));
    SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
    var manualClient=FindObjectOfType<PythonStateClient>();Apply(manualClient,loaded.View());manualClient.GetComponent<PythonServiceScreen>().ShowSeats();
    Screen.SetResolution(1024,768,false);yield return new WaitForSecondsRealtime(.5f);
    File.WriteAllText(Path.Combine(output,"manual-ready.txt"),"Separate process load_game: target seated; waiting for input review.");
    while(!File.Exists(Path.Combine(output,"manual-done.txt")))yield return null;
    yield return Shot("manual-after-input-1024");yield break;
   }
   var backend=new LocalCafeBackend(Path.Combine(output,"save"));CafeBackends.UseLocal(backend);
   if(fujiwara){
    var sim=backend.Simulation;
    sim.Execute("start_popularity_challenge");
    Check(!sim.CustomerSchedule(1).Any(a=>a.customer_id==target),"Fujiwara locked before first clear");
    // Only popularity is seeded in this isolated unlock fixture. Production goal rules remain intact.
    sim.State.popularity=sim.Rules.popularity.cap;sim.Execute("day_off");
    Check(sim.ClearedGoalDay(1)==1,"First stage settled by normal day_off");
    Check(!sim.CustomerSchedule(1).Any(a=>a.customer_id==target),"Fujiwara absent on clear day");
    while(sim.GrowthWaiting){sim.Execute("resolve_growth_auto");sim.Execute("resolve_growth_batch");}
    sim.Execute("continue_goal");if(sim.State.phase=="closed")sim.Execute("next_day");PrepareYoshidaFixture(sim);
    Check(sim.CustomerSchedule(sim.State.day).Single(a=>a.customer_id==target).tick==0,"Fujiwara arrives next day at opening tick zero");
    Check(sim.Rules.advanced_customer.feature=="white" && sim.Rules.advanced_customer.connect_count==1 && sim.Rules.advanced_customer.bonus==100,"Production advanced conditions unchanged");
    var eval=typeof(CafeSimulation).GetMethod("SpecialistResult",BindingFlags.Instance|BindingFlags.NonPublic);
    var cats=sim.ImportDefinitions(sim.State);var white=cats.First(c=>c.features.Contains("white"));var other=cats.First(c=>!c.features.Contains("white"));
    foreach(var test in new[]{new{cat=white.id,count=0,result="failure"},new{cat=white.id,count=1,result="success"},new{cat=other.id,count=1,result="failure"}})
     Check((string)eval.Invoke(sim,new object[]{sim.State,target,test.cat,test.count,0,false,0,0})==test.result,"Production condition boundary "+test.cat+" "+test.count);
   }
   if(yoshida||yamada){
    backend.Simulation.Execute("start_popularity_challenge");
    Check(!backend.Simulation.CustomerSchedule(1).Any(a=>a.customer_id==target),"Second-stage target locked before popularity clears");
    for(int stage=1;stage<=2;stage++){
     // Seed only isolated fixture popularity; settle and advance through production commands.
     backend.Simulation.State.popularity=backend.Simulation.Rules.popularity.cap;
     backend.Simulation.Execute("day_off");
     Check(backend.Simulation.ClearedGoalDay(stage)==stage,"Stage "+stage+" settled by normal day_off");
     Check(!backend.Simulation.CustomerSchedule(stage).Any(a=>a.customer_id==target),"Second-stage target absent on stage "+stage+" clear day");
     while(backend.Simulation.GrowthWaiting){backend.Simulation.Execute("resolve_growth_auto");backend.Simulation.Execute("resolve_growth_batch");}
     if(backend.Simulation.ReservationWaiting)backend.Simulation.Execute("resolve_reservation",choice:"decline");
     backend.Simulation.Execute("continue_goal");
     if(stage==1)backend.Simulation.Execute("advance_goal");
     if(backend.Simulation.State.phase=="closed")backend.Simulation.Execute("next_day");
     PrepareYoshidaFixture(backend.Simulation);
     Check(backend.Simulation.CustomerSchedule(backend.Simulation.State.day).Any(a=>a.customer_id==target)==(stage==2),"Second-stage target requires second stage and following day");
    }
    Check(backend.Simulation.CustomerSchedule(backend.Simulation.State.day).Single(a=>a.customer_id==target).tick==(yamada?30:10),"Second-stage production arrival tick");
    backend.Simulation.Execute("expand_seats");
    backend.Simulation.Execute("start_business");while(backend.Simulation.State.phase=="open")backend.Simulation.Execute("advance_business");
    while(backend.Simulation.GrowthWaiting){backend.Simulation.Execute("resolve_growth_auto");backend.Simulation.Execute("resolve_growth_batch");}
    backend.Simulation.Execute("next_day");
    PrepareYoshidaFixture(backend.Simulation);
    backend.Simulation.Execute("expand_seats");backend.Simulation.Execute("expand_seats");
   }
   if(yamamoto||nakamura||kobayashi||kato){
    backend.Simulation.Execute("start_popularity_challenge");
    Check(!backend.Simulation.CustomerSchedule(1).Any(a=>a.customer_id==target),target+" locked before first popularity clear");
    // Isolated unlocked fixture: seed popularity only, retain the production rules and settle through day_off.
    backend.Simulation.State.popularity=backend.Simulation.Rules.popularity.cap;
    backend.Simulation.Execute("day_off");
    Check(backend.Simulation.ClearedGoalDay(1)==1,"Fixture first stage settled on day one");
    Check(!backend.Simulation.CustomerSchedule(1).Any(a=>a.customer_id==target),target+" absent on clear day");
    while(backend.Simulation.GrowthWaiting){backend.Simulation.Execute("resolve_growth_auto");backend.Simulation.Execute("resolve_growth_batch");}
    backend.Simulation.Execute("continue_goal");
    if(backend.Simulation.State.phase=="closed")backend.Simulation.Execute("next_day");
    if(backend.Simulation.StoreEventWaiting)backend.Simulation.Execute("resolve_store_event",choice:backend.Simulation.CurrentStoreEvent.type=="trouble"?"patch":"decline");
    Check(backend.Simulation.CustomerSchedule(backend.Simulation.State.day).Any(a=>a.customer_id==target),target+" scheduled only after fixture clear day");
    backend.Simulation.Execute("expand_seats");if(nakamura||kobayashi||kato||yoshida||yamada){
     // Earn the next expansion through one ordinary business day in this unlocked fixture.
     backend.Simulation.Execute("start_business");while(backend.Simulation.State.phase=="open")backend.Simulation.Execute("advance_business");
     while(backend.Simulation.GrowthWaiting){backend.Simulation.Execute("resolve_growth_auto");backend.Simulation.Execute("resolve_growth_batch");}
     backend.Simulation.Execute("next_day");
     if(backend.Simulation.StoreEventWaiting)backend.Simulation.Execute("resolve_store_event",choice:backend.Simulation.CurrentStoreEvent.type=="trouble"?"patch":"decline");
     backend.Simulation.Execute("expand_seats");backend.Simulation.Execute("expand_seats");
    }
   }if(tanaka){backend.Simulation.Execute("day_off");while(backend.Simulation.GrowthWaiting){backend.Simulation.Execute("resolve_growth_auto");backend.Simulation.Execute("resolve_growth_batch");}if(backend.Simulation.State.phase=="closed")backend.Simulation.Execute("next_day");if(backend.Simulation.StoreEventWaiting)backend.Simulation.Execute("resolve_store_event",choice:backend.Simulation.CurrentStoreEvent.type=="trouble"?"patch":"decline");Check(backend.Simulation.State.day==2,"Second-day schedule prepared through normal commands");}if(ito||watanabe){for(int day=0;day<7 && !backend.Simulation.CustomerSchedule(backend.Simulation.State.day).Any(a=>a.customer_id==target);day++){backend.Simulation.Execute("day_off");while(backend.Simulation.GrowthWaiting){backend.Simulation.Execute("resolve_growth_auto");backend.Simulation.Execute("resolve_growth_batch");}if(backend.Simulation.State.phase=="closed")backend.Simulation.Execute("next_day");if(backend.Simulation.StoreEventWaiting)backend.Simulation.Execute("resolve_store_event",choice:backend.Simulation.CurrentStoreEvent.type=="trouble"?"patch":"decline");}Check(backend.Simulation.CustomerSchedule(backend.Simulation.State.day).Any(a=>a.customer_id==target),"Target scheduled after normal day progression");}backend.Simulation.Execute("start_business");if(nakamura||kobayashi||kato||yoshida||yamada){
    while(backend.Simulation.State.tick<(yamada?30:yoshida?10:kato?28:kobayashi?20:12))backend.Simulation.Execute("advance_business");
    backend.Simulation.Execute("set_auto_assignment",choice:"manual");backend.Simulation.Execute("advance_business");
    Check(backend.Simulation.State.queue.Contains(target),"Target arrived at production arrival tick");
    backend.Simulation.Execute("assign_service",catId:backend.Simulation.AssignmentCats.First().id,seatId:"seat-"+(backend.Simulation.AssignmentFreeSeats.First()+1),customerId:target);
    backend.Simulation.Execute("set_auto_assignment",choice:"automatic");
   }for(int i=0;i<40 && backend.Simulation.State.phase=="open" && !backend.View().seats.Any(s=>(takahashi||tanaka||ito||watanabe||yamamoto||nakamura||kobayashi||kato||yoshida||yamada||fujiwara)?s.customer_id==target:!string.IsNullOrEmpty(s.customer_id));i++)backend.Simulation.Execute("advance_business");File.WriteAllText(Path.Combine(output,"before-save-state.json"),JsonUtility.ToJson(backend.Simulation.State,true));Check(!(takahashi||tanaka||ito||watanabe||yamamoto||nakamura||kobayashi||kato||yoshida||yamada||fujiwara) || backend.View().seats.Any(s=>s.customer_id==target),"Selected customer seated before save");File.WriteAllText(Path.Combine(output,"seated.json"),JsonUtility.ToJson(backend.View(),true));Send(backend,"save_game");var resumed=new LocalCafeBackend(Path.Combine(output,"save"));Send(resumed,"load_game");Check(resumed.View().seats.Select(s=>s.customer_id).SequenceEqual(backend.View().seats.Select(s=>s.customer_id)),"Save/resume customer identities");Check(!(takahashi||tanaka||ito||watanabe||yamamoto||nakamura||kobayashi||kato||yoshida||yamada||fujiwara) || resumed.View().seats.Any(s=>s.customer_id==target),"Selected customer seated after resume");
   string beforeSeats=JsonUtility.ToJson(backend.Simulation.State);Check(HumanPortraitResources.Resolve("guest-24")==HumanPortraitResources.Placeholder(),"Unregistered customer");if(takahashi||tanaka||ito||watanabe||yamamoto||nakamura||kobayashi||kato||yoshida||yamada||fujiwara){var field=typeof(HumanPortraitResources).GetField("pack",BindingFlags.Static|BindingFlags.NonPublic);var pack=(HumanPortraitPack)field.GetValue(null);var row=pack.people.First(r=>r.game_id==target);string original=row.image;try{row.image="people/"+new string('0',64)+".png";Check(HumanPortraitResources.Resolve(target)==HumanPortraitResources.Placeholder(),"Missing image fallback");}finally{row.image=original;}Check(HumanPortraitResources.Resolve(target)==textures[Array.IndexOf(ids,target)],"Image restored after missing fixture");}SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);var client=FindObjectOfType<PythonStateClient>();var screen=client.GetComponent<PythonServiceScreen>();Screen.SetResolution(1024,768,false);yield return new WaitForSecondsRealtime(.6f);
   foreach(int width in new[]{1280,1024}){
    Screen.SetResolution(width,width==1280?800:768,false);yield return new WaitForSecondsRealtime(.4f);
    foreach(var id in ids){var view=backend.View();var seat=view.seats.First(s=>!string.IsNullOrEmpty(s.customer_id));seat.customer_id=id;seat.customer_name=id=="guest-1"?"佐藤さん":id=="guest-2"?"鈴木さん":id=="guest-3"?"高橋さん":id=="guest-4"?"田中さん":id=="guest-5"?"伊藤さん":id=="guest-6"?"渡辺さん":id=="guest-7"?"山本さん":id=="guest-8"?"中村さん":id=="guest-9"?"小林さん":id=="guest-10"?"加藤さん":id=="guest-11"?"吉田さん":id=="guest-12"?"山田さん":id=="advanced-white"?"藤原さん":"森さん";Apply(client,view);screen.ShowSeats();yield return null;var card=screen.seatContent.GetComponentsInChildren<PythonCatCard>().First(c=>c.customerIcon.activeSelf);Check(card.customerIcon.GetComponentInChildren<RawImage>().texture==HumanPortraitResources.Resolve(id),"Actual seat "+id);if(!fujiwara||id==target)yield return Shot("seat-"+id+"-"+width);seat.customer_id="unknown";screen.Apply(view);Check(card.customerIcon.GetComponentInChildren<RawImage>().texture==HumanPortraitResources.Placeholder(),"Seat replacement fallback");seat.customer_id=null;screen.Apply(view);Check(!card.customerIcon.activeSelf,"Empty seat");}
   }
   if(yoshida||yamada||fujiwara){foreach(int width in new[]{1280,1024}){Screen.SetResolution(width,width==1280?800:768,false);yield return new WaitForSecondsRealtime(.4f);Apply(client,resumed.View());screen.ShowSeats();yield return Shot("actual-seated-"+(fujiwara?"fujiwara":yamada?"yamada":"yoshida")+"-"+width);}}
   Check(beforeSeats==JsonUtility.ToJson(backend.Simulation.State),"Seat rendering changed gameplay or saved text");var trust=new LocalCafeBackend(Path.Combine(output,"trust"));
   if(fujiwara){
    // Suspension history is a fixture; eligibility and presentation run the production code.
    var d=new CafeDiscontent{customer_id=target};
    for(int i=0;i<trust.Simulation.Rules.trust.warning_suspensions;i++)d.suspensions.Add(new CafeSuspension{day=i+1,until=i+2});
    trust.Simulation.State.discontent.Add(d);trust.Simulation.PresentCustomerTrust();
    Check(trust.Simulation.State.trust_events.Single().customer_id==target,"Fujiwara eligible for existing trust warning");
   }else trust.Simulation.State.trust_events.Add(new CafeTrustEvent{id="icon-fixture",customer_id=target,status="waiting",day=1,suspensions=1});CafeBackends.UseLocal(trust);Apply(client,trust.View());var owner=client.GetComponent<PythonCustomerTrustPanel>();var ui=owner.Dialogue;ui.Apply(trust.View(),owner);Check(ui.OpenPending(),"Trust open");ui.Next();Check((Texture2D)typeof(TrustConversationPanel).GetMethod("PortraitTexture",Hidden).Invoke(ui,new object[]{""})==HumanPortraitResources.Resolve(target),"Trust customer binding");string before=JsonUtility.ToJson(trust.Simulation.State);
   foreach(int width in new[]{1280,1024}){Screen.SetResolution(width,width==1280?800:768,false);yield return new WaitForSecondsRealtime(.4f);yield return Shot("trust-"+target+"-"+width);}ui.Close();
   var canvas=client.GetComponentInChildren<Canvas>();var host=new GameObject("Icon replay fixture",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));host.transform.SetParent(canvas.transform,false);var replay=client.gameObject.AddComponent<ReplayConversationPanel>();
   foreach(int width in new[]{1280,1024}){Screen.SetResolution(width,width==1280?800:768,false);yield return new WaitForSecondsRealtime(.4f);replay.Play(new CafeReplayEntry{conversation_id=CafeReservationDialogue.Id+".response",branch_id="accepted",title="特別予約",customer_id="reservation-longhair",cat_name="森さん",personality="特別予約のお客さん",lines=new[]{new CafeConversationLine("森さん","予約の条件を確認させてください。")}},host,owner.title.font,()=>{});yield return null;replay.Next();Check((Texture2D)typeof(ReplayConversationPanel).GetMethod("PortraitTexture",Hidden).Invoke(replay,new object[]{""})==textures[2],"Reservation Extra identity");yield return Shot("reservation-extra-mori-"+width);replay.Close();}
   Check(before==JsonUtility.ToJson(trust.Simulation.State),"Rendering/replay changed gameplay");
   replay.Play(new CafeReplayEntry{conversation_id=CafeTrustDialogue.Id+".response",branch_id="recover",title="旧回想",cat_name="鈴木さん",lines=new[]{new CafeConversationLine("案内","旧記録")}},host,owner.title.font,()=>{});Check((Texture2D)typeof(ReplayConversationPanel).GetMethod("PortraitTexture",Hidden).Invoke(replay,new object[]{""})==HumanPortraitResources.Placeholder(),"Old replay guessed identity");
   var reservation=client.GetComponent<ReservationConversationPanel>()??client.gameObject.AddComponent<ReservationConversationPanel>();Check((Texture2D)typeof(ReservationConversationPanel).GetMethod("PortraitTexture",Hidden).Invoke(reservation,new object[]{""})==textures[2],"Reservation portrait lookup");
  }
 }
}
