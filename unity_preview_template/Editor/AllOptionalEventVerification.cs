using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor
{
    public static class AllOptionalEventVerification
    {
        [Serializable] class Packs{public string all,v1,full,old_full,changed,empty;}
        [Serializable] class Backup{public string path,bytes;public bool exists;}
        [Serializable] class Snapshot{public Backup[] files;public string[] created;}
        [Serializable] class Cases{public Case[] cases;}
        [Serializable] class Case{public string name,json;public bool valid;}
        static string Output=>Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/AllOptionalEvents20261103");
        static string Root=>Path.Combine(Application.dataPath,"Resources");
        static string Pointer=>Path.Combine(Root,"CatCafeEvents/catalog.json");
        static Packs Fixtures=>JsonUtility.FromJson<Packs>(File.ReadAllText(Path.Combine(Output,"fixtures/packs.json")));
        static int checks;
        static void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
        static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}}
        static void WriteReference(string path,byte[] bytes)
        {
            string temp=path+".all-optional-qa.tmp";
            try{File.WriteAllBytes(temp,bytes);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
        static void SnapshotFiles()
        {
            string path=Path.Combine(Output,"references.json");if(File.Exists(path))throw new Exception("Restore the previous snapshot first.");var p=Fixtures;
            var targets=new[]{p.all,p.v1,p.full,p.changed,p.empty}.Select(f=>Path.Combine(Root,"CatCafeEvents/Imports",CafeOptionalEventJson.Parse(File.ReadAllText(Path.Combine(f,"optional_events.json"))).pack_id)).ToList();
            foreach(string full in new[]{p.full,p.old_full}){string id=JsonUtility.FromJson<ContentBundle>(File.ReadAllText(Path.Combine(full,"bundle.json"))).pack_id;foreach(string area in new[]{"CatCafeContent/Imports","CatCafeContent/People"})targets.Add(Path.Combine(Root,area,id));}if(targets.Any(Directory.Exists))throw new Exception("Fixture IDs already exist.");
            string[] files={Pointer,Path.Combine(Root,"CatCafeContent/catalog.json"),Path.Combine(Root,"CatCafeContent/human_portraits.json"),Path.Combine(Root,"CatCafeOutfits/catalog.json"),Path.Combine(Root,"CatCafeStateImages/catalog.json")};
            File.WriteAllText(path,JsonUtility.ToJson(new Snapshot{files=files.Select(f=>new Backup{path=f,exists=File.Exists(f),bytes=File.Exists(f)?Convert.ToBase64String(File.ReadAllBytes(f)):null}).ToArray(),created=targets.ToArray()},true));
        }
        public static void Restore()
        {
            string file=Path.Combine(Output,"references.json");var snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(file));
            foreach(var b in snapshot.files){if(b.exists)WriteReference(b.path,Convert.FromBase64String(b.bytes));else{if(File.Exists(b.path))File.Delete(b.path);if(File.Exists(b.path+".meta"))File.Delete(b.path+".meta");}}
            foreach(string folder in snapshot.created){string full=Path.GetFullPath(folder);if(!full.StartsWith(Path.GetFullPath(Root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Unsafe fixture cleanup path.");if(Directory.Exists(full))Directory.Delete(full,true);if(File.Exists(full+".meta"))File.Delete(full+".meta");}
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);File.Move(file,Path.Combine(Output,"references-restored-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+".json"));Debug.Log("CAT_CAFE_ALL_OPTIONAL_EVENTS_RESTORED");
        }
        static void RequireSnapshot(){if(!File.Exists(Path.Combine(Output,"references.json")))throw new Exception("A live QA snapshot is required.");}
        public static void Prepare(){SnapshotFiles();try{RunChecks();OptionalEventImport.Import(Fixtures.all);Debug.Log("CAT_CAFE_ALL_OPTIONAL_EVENTS_READY: "+checks);}catch{Restore();throw;}}
        public static void PrepareLegacyAfterChecks(){RequireSnapshot();try{RunChecks();OptionalEventImport.Import(Fixtures.v1);Debug.Log("CAT_CAFE_OPTIONAL_V1_READY");}catch{Restore();throw;}}
        public static void PrepareLegacy(){RequireSnapshot();OptionalEventImport.Import(Fixtures.v1);Debug.Log("CAT_CAFE_OPTIONAL_V1_READY");}
        public static void Recheck(){RequireSnapshot();try{RunChecks();OptionalEventImport.Import(Fixtures.all);Debug.Log("CAT_CAFE_ALL_OPTIONAL_EVENTS_READY: "+checks);}catch{Restore();throw;}}
        public static void VerifyLegacyReset()
        {
            checks=0;string path=Path.Combine(Output,"legacy-reset-fixture/import.json"),profile=Path.Combine(Output,"legacy-reset-profile-"+Guid.NewGuid().ToString("N"));var b=new LocalCafeBackend(profile);b.View();byte[] before=File.ReadAllBytes(path);
            var sample=new CafeSnapshot{day=2};sample.cats.Add(new CafeCat{id="cat-mike"});sample.player_cats.Add(new CafePlayerCat{cat_id="cat-mike",affinity=20});var receipt=CafeOptionalEvents.Start(sample,"開始時の猫","playtest-mike","");typeof(LocalCafeBackend).GetMethod("RestoreOptionalEvents",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(b,new object[]{new[]{receipt}});Check(b.OptionalEvents.Length==1,"pre-import story exists");
            var state=b.Simulation.State;CafeResponse reply=null;var request=b.Request("/commands",JsonUtility.ToJson(new PythonShiftCommand{kind="import_legacy_save",save_id=path,save_slot=3,request_id=Guid.NewGuid().ToString("N"),instance_id=state.instance_id,expected_revision=state.revision}),r=>reply=r);while(request.MoveNext()){}Good(reply,"import actual current-rule Python conversion");Check(b.OptionalEvents.Length==0,"Python import resets prior story session");Check(CafeOptionalEventSaveJson.ReadSave(File.ReadAllText(Path.Combine(profile,"slot-3.json"))).Length==0,"import save contains no prior story");Good(Command(b,"load_game",3),"reload imported save");Check(b.OptionalEvents.Length==0,"import reload retains empty story state");Check(before.SequenceEqual(File.ReadAllBytes(path)),"import fixture remains byte-identical");File.WriteAllText(Path.Combine(Output,"legacy-reset-result.txt"),"PASS "+checks+" checks");Debug.Log("CAT_CAFE_OPTIONAL_LEGACY_RESET_OK: "+checks);
        }
        static CafeResponse Command(LocalCafeBackend backend,string kind,int slot=1,string choice="")
        {var state=backend.Simulation.State;CafeResponse reply=null;var request=backend.Request("/commands",JsonUtility.ToJson(new PythonShiftCommand{kind=kind,choice=choice,save_slot=slot,save_id=slot.ToString(),request_id=Guid.NewGuid().ToString("N"),instance_id=state.instance_id,expected_revision=state.revision}),r=>reply=r);while(request.MoveNext()){}return reply;}
        static void Good(CafeResponse response,string label){Check(response.result==UnityEngine.Networking.UnityWebRequest.Result.Success,label+": "+response.text);}
        static bool Start(LocalCafeBackend b,OptionalEventCandidate row)=>b.BeginOptionalEvent(row.cat_id,row.event_id,row.revision,b.Simulation.State.instance_id,b.Simulation.State.revision,"");
        static void Complete(CafeOptionalEvent row,bool accept=true){row.progress.node=row.introduction.Length;CafeOptionalEvents.Choose(row,accept);row.progress.node=CafeOptionalEvents.Lines(row,row.progress.branch_id).Length;CafeOptionalEvents.Finish(row);}
        public static CafeOptionalEventCatalog SharedCatalog(PythonCatState[] family,PythonCatState other)
        {
            var seed=CafeOptionalEventJson.Parse(File.ReadAllText(Path.Combine(Fixtures.all,"optional_events.json"))).events.First(e=>e.target_kind=="content");
            var rows=new[]{family[0],other}.Select(c=>{var d=seed.Copy();d.event_id="optional."+c.content_cat_id+".story";d.content_cat_id=c.content_cat_id;d.title="検証用の個別イベント";d.min_day=1;d.min_affinity=0;d.revision=d.Digest();return d;}).ToArray();
            return new CafeOptionalEventCatalog{version=2,pack_id=new string('0',32),events=rows};
        }
        static void RunChecks()
        {
            checks=0;var p=Fixtures;
            foreach(var test in JsonUtility.FromJson<Cases>(File.ReadAllText(Path.Combine(Output,"fixtures/cases.json"))).cases)Check(!Reject(()=>CafeOptionalEventJson.Parse(test.json))==test.valid,"Python/C# contract "+test.name);
            OptionalEventImport.Import(p.all);string pointer=File.ReadAllText(Pointer);OptionalEventImport.Import(p.all);Check(File.ReadAllText(Pointer)==pointer,"idempotent import");
            string invalid=Path.Combine(Output,"invalid-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(invalid);string original=File.ReadAllText(Path.Combine(p.all,"optional_events.json"));
            foreach(string bad in new[]{original+" ",original.Replace("playtest-sora","unknown-target"),original.Replace("\"events\":","\"reward\":10,\"events\":")}){File.WriteAllText(Path.Combine(invalid,"optional_events.json"),bad);Check(Reject(()=>OptionalEventImport.Import(invalid)) && File.ReadAllText(Pointer)==pointer,"invalid import keeps pointer");}
            ContentPreviewSetup.Import(p.full);Check(File.ReadAllText(Pointer).Contains(CafeOptionalEventJson.Parse(File.ReadAllText(Path.Combine(p.full,"optional_events.json"))).pack_id),"full bundle event hook");OptionalEventImport.Import(p.all);ContentPreviewSetup.Import(p.old_full);Check(File.ReadAllText(Pointer)==pointer,"old full bundle without events keeps independent catalog");
            var snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(Path.Combine(Output,"references.json")));foreach(var file in snapshot.files.Where(f=>f.path!=Pointer)){if(file.exists)WriteReference(file.path,Convert.FromBase64String(file.bytes));else if(File.Exists(file.path))File.Delete(file.path);}AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var legacy=CafeOptionalEventJson.Parse(File.ReadAllText(Path.Combine(p.v1,"optional_events.json"))).events[0];Check(legacy.Matches("cat-mike","playtest-mike") && !legacy.Matches("rescue-100","playtest-mike"),"v1 remains individual only");
            string profile=Path.Combine(Output,"editor-profile");Directory.CreateDirectory(profile);File.Copy(Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/AllStateImages20261103/v2-1280/saves/slot-1.json"),Path.Combine(profile,"slot-1.json"),true);
            var backend=new LocalCafeBackend(profile);CafeBackends.UseLocal(backend);Good(Command(backend,"load_game"),"32 cat legacy load");var view=backend.View();var family=view.cats.GroupBy(c=>c.content_cat_id).First(g=>g.Count()>1).Take(2).ToArray();var other=view.cats.First(c=>c.content_cat_id!=family[0].content_cat_id);var definitions=SharedCatalog(family,other);backend.SetOptionalCatalog(definitions);string game=JsonUtility.ToJson(backend.Simulation.State);
            var a=backend.OptionalForCat(family[0].cat_id);var b=backend.OptionalForCat(family[1].cat_id);Check(a!=null && b!=null,"same family two candidates");
            Check(!backend.BeginOptionalEvent(a.cat_id,a.event_id,"stale",backend.Simulation.State.instance_id,backend.Simulation.State.revision,""),"stale definition refuses start");Check(Start(backend,a),"start A");Check(Start(backend,a) && backend.OptionalEvents.Length==1,"start retry does not duplicate");Check(!Start(backend,b),"unfinished A blocks new B");
            Complete(backend.OptionalEvent);backend.RememberViewedConversations();string archived=backend.Replays.Load().entries.Single(e=>e.conversation_id==a.event_id+".v"+a.revision).cat_name;
            Check(Start(backend,b),"B starts after A completes");Complete(backend.OptionalEvent);backend.RememberViewedConversations();Check(backend.Replays.Load().entries.Single(e=>e.conversation_id==a.event_id+".v"+a.revision).cat_name==archived,"same branch Extra keeps first individual");Check(backend.OptionalEvents.Length==2 && backend.OptionalEvents.All(r=>r.progress.completed),"two distinct completion receipts");
            var candidate=backend.OptionalForCat(other.cat_id);Check(Start(backend,candidate),"different family C starts");var receipt=backend.OptionalEvent;receipt.progress.node=1;string frozen=JsonUtility.ToJson(receipt);Check(JsonUtility.ToJson(backend.Simulation.State)==game,"all story operations leave gameplay exactly unchanged");Good(Command(backend,"save_game",9),"save multiple receipts");
            Check(backend.Simulation.State.revision==view.revision+1,"ordinary save advances revision once");
            var revised=definitions.Copy();foreach(var d in revised.events){d.introduction[0].text="改稿した未開始用の本文。";d.revision=d.Digest();}backend.SetOptionalCatalog(revised);Good(Command(backend,"load_game",9),"load old started text after revision");Check(JsonUtility.ToJson(backend.OptionalEvent)==frozen,"receipt keeps original text and position");
            backend.SetOptionalCatalog(new CafeOptionalEventCatalog{version=2,pack_id=new string('0',32),events=new CafeOptionalDefinition[0]});Check(Start(backend,backend.OptionalCandidates.Single()),"resume after definition removal");
            string savePath=Path.Combine(profile,"slot-9.json"),save=File.ReadAllText(savePath),before=JsonUtility.ToJson(backend.Simulation.State),beforeRows=JsonUtility.ToJson(new CafeSaveEnvelope{optional_events=backend.OptionalEvents});
            File.WriteAllText(savePath,save.Replace("\"optional_events\": [","\"optional_events\": false, \"unused\": ["));
            Check(Command(backend,"load_game",9).result!=UnityEngine.Networking.UnityWebRequest.Result.Success,"invalid receipt array load refuses");Check(JsonUtility.ToJson(backend.Simulation.State)==before && JsonUtility.ToJson(new CafeSaveEnvelope{optional_events=backend.OptionalEvents})==beforeRows,"failed load preserves session");File.WriteAllText(savePath,save);
            Good(Command(backend,"load_game",1),"old save clears optional records");Check(backend.OptionalEvents.Length==0,"no slot leak");backend.SetOptionalCatalog(definitions);
            Debug.Log("ALL_OPTIONAL_EDITOR_UI_START");EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=BindingFlags.NonPublic|BindingFlags.Instance;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);typeof(PythonStateClient).GetField("refreshingState",flags).SetValue(client,false);CafeBackends.UseLocal(backend);typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{backend.View()});var detail=preview.GetComponent<PythonCatDetailsPanel>();detail.Open(family[0].cat_id);var entry=detail.panel.transform.Find("Details Card/Optional Cat Event Entry").GetComponent<Button>();Check(entry.gameObject.activeSelf && entry.interactable,"dynamic individual detail event entry");var panel=preview.GetComponent<OptionalEventConversationPanel>()??preview.gameObject.AddComponent<OptionalEventConversationPanel>();Check(panel.OpenEvent(family[0].cat_id),"open explicit dynamic target");Check(backend.OptionalEvent.cat_id==family[0].cat_id,"correct selected individual");panel.Close();detail.Open(family[1].cat_id);Check(!entry.interactable,"different detail cannot resume A through B");detail.Close();
            var source=JsonUtility.FromJson<CafeOptionalEvent>(JsonUtility.ToJson(backend.OptionalEvent));source.progress.branch_id="accepted";source.progress.node=source.accepted.Length;source.progress.page_offset=0;source.progress.completed=true;source.progress.viewed_branches=new[]{"accepted"};
            var capRows=Enumerable.Range(0,512).Select(i=>{var row=JsonUtility.FromJson<CafeOptionalEvent>(JsonUtility.ToJson(source));row.cat_id="history-"+i;return row;}).ToArray();capRows[0].progress.completed=false;capRows[0].progress.node=1;capRows[0].progress.viewed_branches=new string[0];
            typeof(LocalCafeBackend).GetMethod("RestoreOptionalEvents",flags).Invoke(backend,new object[]{capRows});var pending=backend.OptionalCandidates.Single(c=>c.resume);Check(Start(backend,pending),"512 records still allow historical resume");var fresh=backend.OptionalCandidates.First(c=>!c.resume);Check(backend.OptionalReason(fresh)!="" && !Start(backend,fresh),"512 records with unfinished record block additional story");Good(Command(backend,"save_game",8),"512 records normal save");Good(Command(backend,"load_game",8),"512 records reload");Check(backend.OptionalEvents.Length==512 && backend.OptionalEvents.Count(r=>!r.progress.completed)==1,"512 receipts restored without pruning");Complete(backend.OptionalEvent);Check(backend.OptionalEvents.All(r=>r.progress.completed),"512 records still allow completion");Check(backend.OptionalReason(fresh).Contains("512") && !Start(backend,fresh),"completed 512 records block only additional story");
            Good(Command(backend,"new_game",1,"free"),"new game clears event progress");Check(backend.OptionalEvents.Length==0,"new game no progress leak");
            var archival=new LocalCafeBackend(profile);Good(Command(archival,"load_game",9),"load original receipts for Extra");archival.SetOptionalCatalog(definitions);CafeBackends.UseLocal(archival);EditorSceneManager.OpenScene("Assets/Scenes/CatCafeTitle.unity");var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();title.Apply(archival.View());var extra=title.GetComponent<TitleExtraPanel>()??title.gameObject.AddComponent<TitleExtraPanel>();extra.Initialize(title);extra.Open();extra.RefreshList();var extraList=(ScrollRect)typeof(TitleExtraPanel).GetField("list",flags).GetValue(extra);
            foreach(var definition in definitions.events){var button=extraList.content.Find((definition.event_id+".v"+definition.revision+"/accepted").Replace('/','_')).GetComponent<Button>();Check(button.GetComponentInChildren<Text>().text.Contains("現行版"),"every family has correct current-version Extra label");}
            string key=a.event_id+".v"+a.revision+"/accepted";var unlocked=extraList.content.Find(key.Replace('/','_')).GetComponent<Button>();Check(unlocked.interactable && unlocked.GetComponentInChildren<Text>().text.Contains(archived),"Extra shows frozen individual name");archival.SetOptionalCatalog(new CafeOptionalEventCatalog{version=2,pack_id=new string('0',32),events=new CafeOptionalDefinition[0]});extra.RefreshList();Check(unlocked.interactable && unlocked.GetComponentInChildren<Text>().text.Contains("以前の版"),"removed definition retains historical Extra");extra.OpenReplay(key);Check(extra.Replay.Showing,"historical shared replay opens after removal");extra.Replay.Close();
            var envelope=(CafeSaveEnvelope)typeof(LocalCafeBackend).GetMethod("DecodeSave",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{save});var completed=envelope.optional_events.Where(r=>r.progress.completed).ToArray();completed[0].started_day=1;completed[1].started_day=2;envelope.optional_events=completed.Reverse().ToArray();var extracted=CafeReplayCatalog.FromSave(envelope,archival.Simulation.Rules,id=>id,id=>id,id=>id);Check(extracted.Single(e=>e.Key==key).cat_name==completed[0].cat_name,"earliest started record wins extraction");completed[1].started_day=1;extracted=CafeReplayCatalog.FromSave(envelope,archival.Simulation.Rules,id=>id,id=>id,id=>id);Check(extracted.Single(e=>e.Key==key).cat_name==completed[1].cat_name,"equal-day extraction keeps first saved row");
            File.WriteAllText(Path.Combine(Output,"editor-result.txt"),"PASS "+checks+" checks");
        }
    }
}
