using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor {
 public static class InitialGuestNarrativeVerification {
  static string Output=>Environment.GetEnvironmentVariable("CATCAFE_GUEST_OUTPUT");
  public static void VerifyAndBuild(){Run();StandaloneScreenSetup.Build();}
  public static void Run(){
   Directory.CreateDirectory(Output);
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_IMPORT_PACK"));
   var catalog=CafeNarrativeResources.Load();catalog.Validate();
   var modes=new[]{"ball","plush","tunnel","brush","voice","presence"};int checks=0;
   foreach(string person in new[]{"guest-1","guest-2"})foreach(string cat in new[]{"cat-mike","cat-tama"})foreach(string mode in modes){
    foreach(string action in CafeNarrativeCatalog.Actions(mode).Where(a=>a!="any")){
     var found=catalog.Find("speech",person,mode,action,"any",cat);
     if(found==null || found.action!="any" || found.target_cat!="*" || found.variants.Length!=3)throw new Exception("Missing common pool "+person+mode+action);checks++;
    }
    var sim=new CafeSimulation(Rules());var n=new CafeNarratives(sim);n.SetCatalog(catalog);
    var apply=typeof(CafeNarratives).GetMethod("ApplyCatalog",BindingFlags.Instance|BindingFlags.NonPublic);
    var entry=catalog.Find("speech",person,mode,"direct","any",cat);
    for(int i=0;i<4;i++){
     var row=CafeNarratives.Compose(person,mode,"direct","neutral",100,99,false,false,sim.Definition(cat).name);row.cat_id=cat;row.customer_id=person;
     apply.Invoke(n,new object[]{row,person,person,100d,99d,false});
     if(row.speech_id!=entry.variants[i%3].id || row.speech!=catalog.Expand(entry.variants[i%3].text,person,sim.Definition(cat).name))throw new Exception("Rotation/name mismatch");
     n.Rows.Add(row);checks++;
    }
    foreach(string reaction in new[]{"turn_away","confused","listless"}){
     var row=CafeNarratives.Compose(person,mode,"direct",reaction,10,9,false,false,sim.Definition(cat).name);row.cat_id=cat;
     apply.Invoke(n,new object[]{row,person,person,10d,9d,false});if(string.IsNullOrEmpty(row.speech_id)||row.text.Contains("{cat"))throw new Exception("Negative reaction resolution");checks++;
    }
   }
   if(catalog.Find("speech","guest-1","teaser","direct","any","cat-mike").target_cat=="*" || catalog.Find("speech","guest-1","teaser","direct","any","cat-tama").target_cat!="*")throw new Exception("Legacy dedicated priority changed");
   EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;var client=preview.GetComponent<PythonStateClient>();typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
   foreach(string person in new[]{"guest-1","guest-2"}){
    var b=new LocalCafeBackend(Path.Combine(Output,person+"-saves"),Rules(person));var s=b.Simulation;s.Execute("set_shifts",working:new[]{"cat-tama"});s.Execute("start_business");while(!s.State.exchanges.Any(e=>e.customer_id==person) && s.State.phase=="open")s.Execute("advance_business");
    var exchange=s.State.exchanges.Single(e=>e.customer_id==person);
    // Limited fixture: retain the real seated customer; explicitly choose mode and reset special gauges.
    foreach(string mode in modes){exchange.mode=mode;exchange.previous_reaction="neutral";exchange.last_group=null;exchange.streak=0;exchange.tension=0;exchange.engagement=0;exchange.connect_pending=false;exchange.open_up_pending=false;s.Execute("advance_business");}
    var receipts=b.Narratives.Rows.Where(r=>r.customer_id==person && modes.Contains(r.mode)).ToArray();if(receipts.Length<6 || receipts.Any(r=>string.IsNullOrEmpty(r.speech_id)))throw new Exception("Actual service receipts missing");
    string old=JsonUtility.ToJson(new ReceiptList{rows=b.Narratives.Rows.ToArray()});Send(b,"save_game");
    var loaded=new LocalCafeBackend(Path.Combine(Output,person+"-saves"),Rules(person));Send(loaded,"load_game");
    if(JsonUtility.ToJson(new ReceiptList{rows=loaded.Narratives.Rows.ToArray()})!=old)throw new Exception("Disk reload changed receipt IDs/prose");
    string state=JsonUtility.ToJson(loaded.Simulation.State);var savedRows=JsonUtility.ToJson(new ReceiptList{rows=loaded.Narratives.Rows.ToArray()});loaded.Narratives.SetCatalog(null);
    foreach(var row in loaded.Narratives.Rows)loaded.Narratives.Read(row.session);
    if(savedRows!=JsonUtility.ToJson(new ReceiptList{rows=loaded.Narratives.Rows.ToArray()}))throw new Exception("Viewing regenerated old prose");
    loaded.Narratives.SetCatalog(catalog);CafeBackends.UseLocal(loaded);var view=loaded.View();typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{view});
    var seat=view.seats.First(r=>r.cat_id=="cat-tama");var cat=view.cats.Single(r=>r.cat_id=="cat-tama");var panel=preview.GetComponent<PythonSeatNarrativePanel>()??preview.gameObject.AddComponent<PythonSeatNarrativePanel>();panel.Open(seat,Array.IndexOf(view.seats,seat),cat);
    if(!panel.body.text.Contains(receipts.Last().speech) || panel.body.text.Contains("{cat"))throw new Exception("Seat display missing registered speech");
    foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1024,768)}){
     Capture(preview,Path.Combine(Output,person+"-latest-"+size.x+".png"),size.x,size.y);panel.scroll.verticalNormalizedPosition=.5f;Capture(preview,Path.Combine(Output,person+"-history-"+size.x+".png"),size.x,size.y);panel.scroll.verticalNormalizedPosition=1;
    }
    if(state!=JsonUtility.ToJson(loaded.Simulation.State))throw new Exception("Viewing consumed state");panel.panel.SetActive(false);
    File.WriteAllText(Path.Combine(Output,person+"-receipts.json"),old);
   }
   File.WriteAllText(Path.Combine(Output,"result.txt"),"PASS "+checks+" bounded resolution/rotation/negative checks; 2 guests x17 actions x2 cats; legacy priority; actual service with explicit actor/mode fixture; disk save/load exact receipt IDs/text; no regeneration without catalog; two-resolution seat/latest/history captures. Not physical input or exhaustive natural service sampling.");Debug.Log("CAT_CAFE_INITIAL_GUEST_NARRATIVES_OK");
  }
  [Serializable] class ReceiptList { public CafeNarrativeRow[] rows; }
  static CafeRules Rules(string person=null){var r=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);r.store_events.probability=0;r.arrival_ticks=person=="guest-2"?new[]{39,0}:new[]{0,39};foreach(var p in r.patterns)p.days=new[]{0,1,2,3,4,5,6};return r;}
  static void Send(LocalCafeBackend b,string kind){CafeResponse response=null;var c=new PythonShiftCommand{kind=kind,save_id="2",save_slot=2,instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,request_id=Guid.NewGuid().ToString("N")};var routine=b.Request("/commands",JsonUtility.ToJson(c),r=>response=r);while(routine.MoveNext()){}if(response==null||response.result!=UnityEngine.Networking.UnityWebRequest.Result.Success)throw new Exception(response?.text??"No reply");}
  static void Capture(ContentPreview p,string path,int w,int h)=>typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{p,path,w,h});
 }
}
