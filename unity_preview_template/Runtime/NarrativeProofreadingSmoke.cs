using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public class NarrativeProofreadingSmoke:MonoBehaviour {
  public static System.Collections.Generic.Dictionary<string,LocalCafeBackend> Cases=new System.Collections.Generic.Dictionary<string,LocalCafeBackend>();
  public const string Sora="cat-sora";
  [Serializable] public class ReceiptList { public CafeNarrativeRow[] rows; }
  public static CafeRules Rules(){var r=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);r.store_events.probability=0;return r;}
  public static void Send(LocalCafeBackend b,string kind){CafeResponse reply=null;var cmd=new PythonShiftCommand{kind=kind,save_id="2",save_slot=2,request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision};var routine=b.Request("/commands",JsonUtility.ToJson(cmd),r=>reply=r);while(routine.MoveNext()){}if(reply==null||reply.result!=UnityEngine.Networking.UnityWebRequest.Result.Success)throw new Exception(reply?.text??"No response");}
  static string Rows(LocalCafeBackend b)=>JsonUtility.ToJson(new ReceiptList{rows=b.Narratives.Rows.ToArray()});
  static CafeNarrativeCatalog CommonFixture(CafeNarrativeCatalog source){var c=JsonUtility.FromJson<CafeNarrativeCatalog>(JsonUtility.ToJson(source));c.entries=c.entries.Where(e=>!(e.kind=="cat_response"&&e.subject_id=="cat-mike"&&e.mode=="teaser"&&e.reaction=="neutral"&&e.action!="any")).ToArray();c.revision=c.ComputeRevision();c.Validate();return c;}
  public static LocalCafeBackend Check(string output){
   Directory.CreateDirectory(output);var current=CafeNarrativeResources.Load();current.Validate();
   var old=JsonUtility.FromJson<CafeNarrativeCatalog>(Resources.Load<TextAsset>("CatCafeContent/Imports/4e511312c48b4cee96882c669d5ea5cf/interaction_texts").text);old.Validate();
   if(current.revision==old.revision||current.entries.Length!=251)throw new Exception("Catalog revision/count");
   int checks=0;var apply=typeof(CafeNarratives).GetMethod("ApplyCatalog",BindingFlags.Instance|BindingFlags.NonPublic);
   // Boundary fixture: explicit reaction, no invented natural occurrence.
   foreach(string cat in new[]{Sora,"cat-mike"})foreach(string reaction in (cat==Sora?new[]{"neutral","listless"}:new[]{"neutral","turn_away","confused","listless","favorable","enthusiastic"})){
    string mode=cat==Sora?"pause":"teaser",action=cat==Sora?"pause":"direct";
    var previousCatalog=cat=="cat-mike"&&reaction=="neutral"?CommonFixture(old):old;var selectedCatalog=cat=="cat-mike"&&reaction=="neutral"?CommonFixture(current):current;
    var sim=new CafeSimulation(Rules());var a=new CafeNarratives(sim);a.SetCatalog(previousCatalog);var n=new CafeNarratives(sim);n.SetCatalog(selectedCatalog);
    for(int i=0;i<3;i++){
     Func<CafeNarratives,CafeNarrativeRow> generate=ns=>{var row=CafeNarratives.Compose("プレイヤー",mode,action,reaction,10,10,false,false,sim.Definition(cat).name);row.cat_id=cat;apply.Invoke(ns,new object[]{row,"player","プレイヤー",10d,10d,false});ns.Rows.Add(row);return row;};
     var before=generate(a);var after=generate(n);var entry=selectedCatalog.Find("cat_response",cat,mode,action,reaction,cat);
     if(entry==null||after.cat_text_id!=entry.variants[i].id||after.cat_response!=current.Expand(entry.variants[i].text,"player",sim.Definition(cat).name))throw new Exception("Resolution/rotation mismatch");
     if(before.cat_text_id!=after.cat_text_id||before.speech!=after.speech||before.human_action!=after.human_action||before.narration!=after.narration||before.image_scene_key!=after.image_scene_key||Path.GetFileName(before.image_resource)!=Path.GetFileName(after.image_resource))throw new Exception("Unrelated presentation changed");checks++;
    }
   }
   LocalCafeBackend display=null;
   foreach(string cat in new[]{Sora,"cat-mike"})foreach(bool alternate in new[]{false,true}){
    bool tired=cat==Sora&&alternate,common=cat=="cat-mike"&&alternate;
    var previousCatalog=common?CommonFixture(old):old;var selectedCatalog=common?CommonFixture(current):current;
    string name=cat+(tired?"-tired":common?"-common-fixture":"");string path=Path.Combine(output,name);var rules=Rules();
    // In-memory fixture only: keep low-stamina pauses below the boundary; neutral teaser score.
    if(tired)rules.exchange.pause_recovery=1;
    if(cat=="cat-mike"){int index=Array.FindIndex(rules.types,t=>t.id=="teaser");rules.cats.Single(c=>c.id==cat).type_preferences[index]=(rules.exchange.confused_threshold+rules.exchange.favorable_threshold)/2/(rules.exchange.direct_gain*rules.types[index].gain);}
    var b=new LocalCafeBackend(path,rules);if(tired)b.Simulation.State.cats.Single(c=>c.id==cat).stamina=10;
    b.Narratives.SetCatalog(previousCatalog);b.Simulation.Execute("player_begin",catId:cat);string action=cat==Sora?"pause":"direct";
    b.Simulation.Execute("player_step",choice:action);var original=JsonUtility.ToJson(b.Narratives.Rows.First());Send(b,"save_game");
    var reopened=new LocalCafeBackend(path,rules);Send(reopened,"load_game");if(JsonUtility.ToJson(reopened.Narratives.Rows.First())!=original)throw new Exception("Old disk receipt regenerated");
    reopened.Narratives.SetCatalog(selectedCatalog);
    for(int i=0;i<3;i++)reopened.Simulation.Execute("player_step",choice:action);
    var newer=reopened.Narratives.Rows.Skip(1).ToArray();string reaction=tired?"listless":"neutral";
    if(newer.Length!=3||newer.Any(r=>r.reaction!=reaction||r.catalog_revision!=selectedCatalog.revision))throw new Exception("Actual action reaction fixture mismatch "+name);
    foreach(var row in newer){var entry=selectedCatalog.Find("cat_response",cat,cat==Sora?"pause":"teaser",action,reaction,cat);if(!entry.variants.Any(v=>v.id==row.cat_text_id&&selectedCatalog.Expand(v.text,"player",reopened.Simulation.Definition(cat).name)==row.cat_response))throw new Exception("New action selected old text");if(common&&entry.action!="any")throw new Exception("Common fallback not exercised");}
    if(JsonUtility.ToJson(reopened.Narratives.Rows.First())!=original)throw new Exception("New action rewrote history");Send(reopened,"save_game");string expected=Rows(reopened);
    var loaded=new LocalCafeBackend(path,rules);Send(loaded,"load_game");if(Rows(loaded)!=expected)throw new Exception("New disk receipts changed");
    string state=JsonUtility.ToJson(loaded.Simulation.State);loaded.Narratives.SetCatalog(null);foreach(var row in loaded.Narratives.Rows)loaded.Narratives.Read(row.session);
    if(Rows(loaded)!=expected||JsonUtility.ToJson(loaded.Simulation.State)!=state)throw new Exception("Viewing regenerated/mutated");loaded.Narratives.SetCatalog(current);
    File.WriteAllText(Path.Combine(output,name+"-receipts.json"),expected);Cases[name]=loaded;if(tired)display=loaded;checks+=newer.Length;
   }
   File.WriteAllText(Path.Combine(output,"result.txt"),"PASS "+checks+" checks: explicit-reaction boundary rotation; unchanged speech/action/narration/image hashes; actual actions under limited in-memory fixtures; old/new disk receipts; catalog-free read stability. old="+old.revision+" new="+current.revision);return display;
  }
  static bool installed;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(installed)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-proofreading");if(i<0)return;installed=true;var go=new GameObject("Proofreading QA").AddComponent<NarrativeProofreadingSmoke>();DontDestroyOnLoad(go.gameObject);go.StartCoroutine(go.Run(args[i+1]));}
  public static void Show(LocalCafeBackend b){var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var flags=BindingFlags.Instance|BindingFlags.NonPublic;var client=preview.GetComponent<PythonStateClient>();typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);CafeBackends.UseLocal(b);typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{b.View()});preview.GetComponent<PythonPlayerPanel>().Open();}
  IEnumerator Run(string output){yield return null;bool stay=Environment.GetCommandLineArgs().Contains("--proofreading-ui");if(stay&&UnityEngine.Object.FindObjectOfType<ContentPreview>()==null){UnityEngine.SceneManagement.SceneManager.LoadScene("CatCafePythonState");yield return null;}try{var b=Check(output);if(stay)Show(b);else Application.Quit(0);}catch(Exception e){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Application.Quit(1);}}
 }
}
