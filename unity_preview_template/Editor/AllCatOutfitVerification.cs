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
    public static class AllCatOutfitVerification
    {
        [Serializable] class Packs{public string all,v1,full,states,empty;}
        [Serializable] class Backup{public string path,bytes;public bool exists;}
        [Serializable] class Snapshot{public Backup[] files;public string[] created;}
        [Serializable] class Cases{public Case[] cases;}
        [Serializable] class Case{public string name,json;public bool valid;}
        static string Output=>Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/AllCatOutfits20261103");
        static string Root=>Path.Combine(Application.dataPath,"Resources");
        static string Pointer=>Path.Combine(Root,"CatCafeOutfits/catalog.json");
        static Packs Fixtures=>JsonUtility.FromJson<Packs>(File.ReadAllText(Path.Combine(Output,"fixtures/packs.json")));
        static int checks;
        static void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
        static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}}
        static void WriteReference(string path,byte[] bytes)
        {
            string temp=path+".all-outfits-qa.tmp";
            try{File.WriteAllBytes(temp,bytes);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
        static void SnapshotFiles()
        {
            string path=Path.Combine(Output,"references.json");if(File.Exists(path))throw new Exception("Restore the previous snapshot first.");var p=Fixtures;
            var targets=new[]{p.all,p.v1,p.full,p.empty}.Select(f=>Path.Combine(Root,"CatCafeOutfits/Imports",CatOutfitPack.Parse(File.ReadAllText(Path.Combine(f,"cat_outfits.json"))).pack_id)).ToList();
            targets.Add(Path.Combine(Root,"CatCafeStateImages/Imports",CatStateImagePack.Parse(File.ReadAllText(Path.Combine(p.states,"state_images.json"))).pack_id));
            string id=CatOutfitPack.Parse(File.ReadAllText(Path.Combine(p.full,"cat_outfits.json"))).pack_id;foreach(string area in new[]{"CatCafeContent/Imports","CatCafeContent/People","CatCafeEvents/Imports"})targets.Add(Path.Combine(Root,area,id));if(targets.Any(Directory.Exists))throw new Exception("Fixture IDs already exist.");
            string[] files={Pointer,Path.Combine(Root,"CatCafeContent/catalog.json"),Path.Combine(Root,"CatCafeContent/human_portraits.json"),Path.Combine(Root,"CatCafeEvents/catalog.json"),Path.Combine(Root,"CatCafeStateImages/catalog.json")};
            File.WriteAllText(path,JsonUtility.ToJson(new Snapshot{files=files.Select(f=>new Backup{path=f,exists=File.Exists(f),bytes=File.Exists(f)?Convert.ToBase64String(File.ReadAllBytes(f)):null}).ToArray(),created=targets.ToArray()},true));
        }
        public static void Restore()
        {
            string file=Path.Combine(Output,"references.json");var snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(file));
            foreach(var b in snapshot.files){if(b.exists)WriteReference(b.path,Convert.FromBase64String(b.bytes));else{if(File.Exists(b.path))File.Delete(b.path);if(File.Exists(b.path+".meta"))File.Delete(b.path+".meta");}}
            foreach(string folder in snapshot.created){string full=Path.GetFullPath(folder);if(!full.StartsWith(Path.GetFullPath(Root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Unsafe fixture cleanup path.");if(Directory.Exists(full))Directory.Delete(full,true);if(File.Exists(full+".meta"))File.Delete(full+".meta");}
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);File.Move(file,Path.Combine(Output,"references-restored-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+".json"));Debug.Log("CAT_CAFE_ALL_OUTFITS_RESTORED");
        }
        public static void RecheckAndBuild(){RequireSnapshot();RunChecks();StandaloneScreenSetup.Build();}
        static void RequireSnapshot(){if(!File.Exists(Path.Combine(Output,"references.json")))throw new Exception("A live QA snapshot is required.");}
        public static void PrepareAndBuild(){Prepare();StandaloneScreenSetup.Build();}
        public static void PrepareLegacyFreshAndBuild(){Prepare();CatOutfitImport.Import(Fixtures.v1);StandaloneScreenSetup.Build();}
        public static void PrepareLegacyAndBuild(){RequireSnapshot();CatOutfitImport.Import(Fixtures.v1);StandaloneScreenSetup.Build();}
        public static void VerifyAndRestore(){Prepare();Restore();}
        public static void Prepare()
        {SnapshotFiles();try{RunChecks();CatOutfitImport.Import(Fixtures.all);CatStateImageImport.Import(Fixtures.states);Debug.Log("CAT_CAFE_ALL_OUTFITS_READY: "+checks);}catch{Restore();throw;}}
        public static void CompleteAndRestore()
        {
            checks=0;if(!File.Exists(Path.Combine(Output,"references.json")))throw new Exception("QA snapshot is required.");
            try
            {
                var p=Fixtures;string pointer=File.ReadAllText(Pointer);ContentPreviewSetup.Import(p.full);Check(File.ReadAllText(Pointer).Contains(CatOutfitPack.Parse(File.ReadAllText(Path.Combine(p.full,"cat_outfits.json"))).pack_id),"full pack imports outfit sidecar");
                CatOutfitImport.Import(p.all);string sidecar=Path.Combine(p.full,"cat_outfits.json"),held=sidecar+".held";File.Move(sidecar,held);
                try{ContentPreviewSetup.Import(p.full);Check(File.ReadAllText(Pointer)==pointer || File.ReadAllText(Pointer).Contains(CatOutfitPack.Parse(File.ReadAllText(Path.Combine(p.all,"cat_outfits.json"))).pack_id),"old full pack without sidecar preserves outfits");}finally{File.Move(held,sidecar);}
                var selections=Enumerable.Range(0,512).Select(i=>new CafeCatOutfitSelection{cat_id="c"+i.ToString().PadLeft(63,'0'),outfit_id=new string('x',32)}).ToArray();var only=new CafeSaveEnvelope{cat_outfits=selections};string json=JsonUtility.ToJson(only,true);Check(CafeCatOutfits.ReadSave(json).Length==512,"pretty 512 maximum IDs save readback");var state=new CafeSnapshot{cats=selections.Select(r=>new CafeCat{id=r.cat_id,activity="adopted"}).ToList()};CafeCatOutfits.Validate(selections,state);Check(true,"historical adopted selection membership");state.cats.RemoveAt(0);Check(Reject(()=>CafeCatOutfits.Validate(selections,state)),"missing historical individual rejects");
                string profile32=Path.Combine(Output,"shared-save-profile");Directory.CreateDirectory(profile32);string slot=Path.Combine(profile32,"slot-1.json");File.Copy(Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/AllStateImages20261103/v2-1280/saves/slot-1.json"),slot,true);var shared=new LocalCafeBackend(profile32);Good(Command(shared,"load_game",slot:1),"32-cat legacy save for shared family");var family=shared.View().cats.GroupBy(c=>c.content_cat_id).First(g=>g.Count()>1).Take(2).ToArray();var envelope=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(slot));envelope.cat_outfits=new[]{new CafeCatOutfitSelection{cat_id=family[0].cat_id,outfit_id="ribbon"},new CafeCatOutfitSelection{cat_id=family[1].cat_id,outfit_id="future-set"}};File.WriteAllText(slot,JsonUtility.ToJson(envelope,true));Good(Command(shared,"load_game",slot:1),"same family independent saved selections");Check(shared.CatOutfit(family[0].cat_id)=="ribbon" && shared.CatOutfit(family[1].cat_id)=="future-set","shared family restores per individual");Good(Command(shared,"set_cat_outfit",family[0].cat_id),"clear shared-family individual without image");Check(shared.CatOutfit(family[1].cat_id)=="future-set","shared family other choice remains");Good(Command(shared,"save_game",slot:1),"save shared-family sparse selections");var restarted=new LocalCafeBackend(profile32);Good(Command(restarted,"load_game",slot:1),"fresh backend shared family resume");Check(restarted.CatOutfit(family[0].cat_id)=="" && restarted.CatOutfit(family[1].cat_id)=="future-set","shared family persistence after one clear");
                // Capture the actual title layout after generated text, not just the rect size.
                EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=BindingFlags.NonPublic|BindingFlags.Instance;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);typeof(PythonStateClient).GetField("refreshingState",flags).SetValue(client,false);var backend=new LocalCafeBackend(Path.Combine(Output,"layout-profile"));CafeBackends.UseLocal(backend);var view=backend.View();var cat=view.cats.Single(c=>c.cat_id=="cat-sora");cat.name="派遣先から紹介された長い名前の猫";typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{view});var panel=preview.GetComponent<PythonCatDetailsPanel>();panel.Open(cat.cat_id);Canvas.ForceUpdateCanvases();Check(panel.title.resizeTextForBestFit && panel.title.horizontalOverflow==HorizontalWrapMode.Wrap,"long title fits two-line frame");panel.Close();
                File.WriteAllText(Path.Combine(Output,"extra-result.txt"),"PASS "+checks+" checks");
            }
            finally{Restore();}
        }
        static CafeResponse Command(LocalCafeBackend b,string kind,string cat="cat-mike",string choice="",int slot=9)
        {var state=b.Simulation.State;CafeResponse reply=null;var e=b.Request("/commands",JsonUtility.ToJson(new PythonShiftCommand{kind=kind,cat_id=cat,choice=choice,save_slot=slot,save_id=slot.ToString(),request_id=Guid.NewGuid().ToString("N"),instance_id=state.instance_id,expected_revision=state.revision}),r=>reply=r);while(e.MoveNext()){}return reply;}
        static void Good(CafeResponse response,string label){Check(response.result==UnityEngine.Networking.UnityWebRequest.Result.Success,label+": "+response.text);}
        static void RunChecks()
        {
            checks=0;foreach(var test in JsonUtility.FromJson<Cases>(File.ReadAllText(Path.Combine(Output,"fixtures/contract-cases.json"))).cases)Check(!Reject(()=>CatOutfitPack.Parse(test.json))==test.valid,"Python/C# contract "+test.name);var p=Fixtures;CatOutfitImport.Import(p.all);CatStateImageImport.Import(p.states);
            string pointer=File.ReadAllText(Pointer);CatOutfitImport.Import(p.all);Check(File.ReadAllText(Pointer)==pointer,"idempotent import");
            var a=new PythonCatState{cat_id="cat-mike",content_cat_id="playtest-mike",health_status="healthy",activity="cafe",outfit_id="ribbon"};
            var b=new PythonCatState{cat_id="rescue-100",content_cat_id="playtest-mike",health_status="healthy",activity="cafe",outfit_id="ribbon"};
            var c=new PythonCatState{cat_id="cat-sora",content_cat_id="playtest-sora",health_status="healthy",activity="cafe",outfit_id="ribbon"};
            Check(CatOutfits.Resolve(a)!=null && CatOutfits.Resolve(b)==CatOutfits.Resolve(a) && CatOutfits.Resolve(c)!=null,"v2 shared artwork across individuals");
            Check(CatOutfits.LabelFor(c.cat_id,c.content_cat_id)=="ソラのリボン","per-family label");
            b.outfit_id="";Check(CatOutfits.Resolve(b)==null && CatOutfits.Resolve(a)!=null,"independent individual choice");b.outfit_id="ribbon";
            foreach(var cat in new[]{a,b,c})
            {foreach(float stress in new[]{0,60})foreach(float fatigue in new[]{0,60})foreach(float affinity in new[]{0,80}){cat.stress=stress;cat.fatigue=fatigue;cat.affinity=affinity;Check((CatOutfits.Resolve(cat)!=null)==(CatStateMark.Resolve(cat)==0),"F4 priority combinations");}cat.stress=cat.fatigue=cat.affinity=0;cat.health_status="sick";Check(CatOutfits.Resolve(cat)==null,"sick fallback");cat.health_status="healthy";cat.activity="dispatched";Check(CatOutfits.Resolve(cat)==null,"away fallback");cat.activity="cafe";cat.stress=float.NaN;Check(CatOutfits.Resolve(cat)==null,"nonfinite fallback");cat.stress=0;}
            CatOutfitImport.Import(p.v1);Check(CatOutfits.Resolve(a)!=null && CatOutfits.Resolve(b)==null && CatOutfits.Resolve(c)==null,"v1 remains Mike individual only");CatOutfitImport.Import(p.all);
            pointer=File.ReadAllText(Pointer);string invalid=Path.Combine(Output,"invalid-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(invalid);string original=File.ReadAllText(Path.Combine(p.all,"cat_outfits.json"));
            foreach(string broken in new[]{original.Replace("ribbon","unknown"),original.Replace("images/","../images/"),original+" "})
            {File.WriteAllText(Path.Combine(invalid,"cat_outfits.json"),broken);Check(Reject(()=>CatOutfitImport.Import(invalid)) && File.ReadAllText(Pointer)==pointer,"bad pack preserves pointer");}
            string fresh=original.Replace(CatOutfitPack.Parse(original).pack_id,new string('a',32));File.WriteAllText(Path.Combine(invalid,"cat_outfits.json"),fresh);Check(Reject(()=>CatOutfitImport.Import(invalid)),"missing PNG rejected");Directory.CreateDirectory(Path.Combine(invalid,"images"));foreach(var row in CatOutfitPack.Parse(fresh).outfits)File.WriteAllBytes(Path.Combine(invalid,row.image),new byte[]{1,2,3});Check(Reject(()=>CatOutfitImport.Import(invalid)) && File.ReadAllText(Pointer)==pointer,"hash failure keeps pointer");
            long total=0;for(int i=0;i<16;i++)total=CatOutfitImport.AddImageSize(total,16L*1024*1024);Check(Reject(()=>CatOutfitImport.AddImageSize(total,1)),"unique PNG total bound");
            var rows=Enumerable.Range(0,512).Select(i=>new CafeCatOutfitSelection{cat_id="c"+i.ToString().PadLeft(63,'0'),outfit_id=new string('x',32)}).ToArray();
            string max="{\"cat_outfits\":"+"["+string.Join(",",rows.Select(r=>JsonUtility.ToJson(r)))+"]}";Check(CafeCatOutfits.ReadSave(max).Length==512,"512 maximal IDs roundtrip within JSON bound");Check(Reject(()=>CafeCatOutfits.Copy(rows.Concat(new[]{new CafeCatOutfitSelection{cat_id="extra",outfit_id="ribbon"}}).ToArray())),"513 rejected");
            Check(Reject(()=>CafeCatOutfits.Copy(new[]{rows[0],rows[0]})),"duplicate individual rejected");Check(CafeCatOutfits.ReadSave("{\"cat_outfits\":null}").Length==0 && CafeCatOutfits.ReadSave("{}").Length==0,"old saves without selection");
            foreach(string bad in new[]{"false","{}","[{}]","[{\"cat_id\":3,\"outfit_id\":\"ribbon\"}]","[{\"cat_id\":\"cat-mike\",\"outfit_id\":\"\"}]","[{\"cat_id\":\"cat-mike\",\"outfit_id\":\"ribbon\",\"extra\":1}]"})Check(Reject(()=>CafeCatOutfits.ReadSave("{\"cat_outfits\":"+bad+"}")),"strict selection shape");
            Check(Reject(()=>CafeCatOutfits.ReadSave("{\"cat_outfits\":null,\"cat_outfits\":[]}")),"duplicate root rejects");
            Check(Reject(()=>CafeCatOutfits.ReadSave("{\"cat_outfits\":"+"[\""+new string('x',100001)+"\"]}")),"oversize selection rejects");
            var backend=new LocalCafeBackend(Path.Combine(Output,"editor-profile"));CafeBackends.UseLocal(backend);string game=JsonUtility.ToJson(backend.Simulation.State);
            Good(Command(backend,"set_cat_outfit",choice:"ribbon"),"Mike select");Good(Command(backend,"set_cat_outfit","cat-sora","ribbon"),"Sora select");Check(backend.CatOutfit("cat-mike")=="ribbon" && backend.CatOutfit("cat-sora")=="ribbon","two selections retained");Check(JsonUtility.ToJson(backend.Simulation.State)==game,"no game/revision/RNG change");
            Good(Command(backend,"save_game"),"save multiple");Good(Command(backend,"set_cat_outfit","cat-mike"),"clear one");Check(backend.CatOutfit("cat-sora")=="ribbon","clear preserves other");Good(Command(backend,"save_game",slot:8),"save other slot");Good(Command(backend,"load_game"),"load both");Check(backend.CatOutfit("cat-mike")=="ribbon" && backend.CatOutfit("cat-sora")=="ribbon","both restored");Good(Command(backend,"load_game",slot:8),"load sparse");Check(backend.CatOutfit("cat-mike")=="" && backend.CatOutfit("cat-sora")=="ribbon","slots replace selections");
            string savePath=Path.Combine(Output,"editor-profile/slot-9.json"),save=File.ReadAllText(savePath);string before=JsonUtility.ToJson(backend.Simulation.State);
            File.WriteAllText(savePath,save.Replace(@"""cat_id"": ""cat-sora""",@"""cat_id"": ""nonexistent"""));
            Check(Command(backend,"load_game").result!=UnityEngine.Networking.UnityWebRequest.Result.Success,"nonexistent cat rejects");Check(JsonUtility.ToJson(backend.Simulation.State)==before && backend.CatOutfit("cat-mike")=="" && backend.CatOutfit("cat-sora")=="ribbon","failed load preserves session");File.WriteAllText(savePath,save);
            File.WriteAllText(savePath,save.Replace("\"ribbon\"","\"future-set\""));Good(Command(backend,"load_game"),"unknown selection loads");Check(backend.CatOutfit("cat-sora")=="future-set","unknown selection preserved");File.WriteAllText(savePath,save);
            CatOutfitImport.Import(p.empty);Good(Command(backend,"load_game"),"load missing artwork");Check(backend.CatOutfit("cat-sora")=="ribbon" && CatOutfits.Resolve(c)==null,"empty artwork keeps selection");CatOutfitImport.Import(p.all);Check(CatOutfits.Resolve(c)!=null,"reimport restores selected picture");
            backend.Simulation.State.cats.Single(x=>x.id=="cat-sora").activity="adopted";Check(Command(backend,"set_cat_outfit","cat-sora","ribbon").result!=UnityEngine.Networking.UnityWebRequest.Result.Success,"adopted cannot newly apply");Good(Command(backend,"set_cat_outfit","cat-sora"),"adopted can clear");
            File.WriteAllText(Path.Combine(Output,"editor-result.txt"),"PASS "+checks+" checks");
        }
    }
}
