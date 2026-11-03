using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class AllStateImageVerification
    {
        [Serializable] class Packs{public string all,v1,full,partial,empty;}
        [Serializable] class Backup{public string path,bytes;public bool exists;}
        [Serializable] class Snapshot{public Backup[] files;public string[] created;}
        [Serializable] class Cases{public Case[] cases;}
        [Serializable] class Case{public string name,json;public bool valid,legacy_valid;}
        static string Output=>Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/AllStateImages20261103");
        static string Root=>Path.Combine(Application.dataPath,"Resources");
        static string Pointer=>Path.Combine(Root,"CatCafeStateImages/catalog.json");
        static Packs Fixtures=>JsonUtility.FromJson<Packs>(File.ReadAllText(Path.Combine(Output,"fixtures/packs.json")));
        static int checks;
        static void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
        static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}}
        static void WriteReference(string path,byte[] bytes)
        {
            string temp=path+".all-state-qa.tmp";
            try{File.WriteAllBytes(temp,bytes);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
        static void SnapshotFiles()
        {
            string path=Path.Combine(Output,"references.json");if(File.Exists(path))throw new Exception("Restore the previous snapshot first.");var p=Fixtures;
            var targets=new[]{p.all,p.v1,p.full,p.partial,p.empty}.Select(f=>Path.Combine(Root,"CatCafeStateImages/Imports",CatStateImagePack.Parse(File.ReadAllText(Path.Combine(f,"state_images.json"))).pack_id)).ToList();
            string id=CatStateImagePack.Parse(File.ReadAllText(Path.Combine(p.full,"state_images.json"))).pack_id;foreach(string area in new[]{"CatCafeContent/Imports","CatCafeContent/People","CatCafeEvents/Imports","CatCafeOutfits/Imports"})targets.Add(Path.Combine(Root,area,id));if(targets.Any(Directory.Exists))throw new Exception("Fixture IDs already exist.");
            string[] files={Pointer,Path.Combine(Root,"CatCafeContent/catalog.json"),Path.Combine(Root,"CatCafeContent/human_portraits.json"),Path.Combine(Root,"CatCafeEvents/catalog.json"),Path.Combine(Root,"CatCafeOutfits/catalog.json")};
            File.WriteAllText(path,JsonUtility.ToJson(new Snapshot{files=files.Select(f=>new Backup{path=f,exists=File.Exists(f),bytes=File.Exists(f)?Convert.ToBase64String(File.ReadAllBytes(f)):null}).ToArray(),created=targets.ToArray()},true));
        }
        public static void Restore()
        {
            string file=Path.Combine(Output,"references.json");var snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(file));
            foreach(var b in snapshot.files){if(b.exists)WriteReference(b.path,Convert.FromBase64String(b.bytes));else{if(File.Exists(b.path))File.Delete(b.path);if(File.Exists(b.path+".meta"))File.Delete(b.path+".meta");}}
            foreach(string folder in snapshot.created){string full=Path.GetFullPath(folder);if(!full.StartsWith(Path.GetFullPath(Root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Unsafe fixture cleanup path.");if(Directory.Exists(full))Directory.Delete(full,true);if(File.Exists(full+".meta"))File.Delete(full+".meta");}
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);File.Move(file,Path.Combine(Output,"references-restored-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+".json"));Debug.Log("CAT_CAFE_ALL_STATE_IMAGES_RESTORED");
        }
        public static void Prepare()
        {checks=0;SnapshotFiles();try{RunChecks();CatStateImageImport.Import(Fixtures.all);Debug.Log("CAT_CAFE_ALL_STATE_IMAGES_READY: "+checks);}catch{Restore();throw;}}
        public static void VerifyAndRestore(){Prepare();CompleteAndRestore();}
        public static void PrepareLegacy(){CatStateImageImport.Import(Fixtures.v1);Debug.Log("CAT_CAFE_LEGACY_STATE_IMAGES_READY");}
        public static void CompleteAndRestore()
        {
            if(!File.Exists(Path.Combine(Output,"references.json")))throw new Exception("A live QA snapshot is required before importing fixtures.");checks=0;
            try
            {
                ContentPreviewSetup.Import(Fixtures.full);
                EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=BindingFlags.NonPublic|BindingFlags.Instance;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);typeof(PythonStateClient).GetField("refreshingState",flags).SetValue(client,false);var b=new LocalCafeBackend(Path.Combine(Output,"missing-base-profile"));CafeBackends.UseLocal(b);var state=b.View();var row=state.cats.Single(c=>c.cat_id=="cat-sora");row.stress=row.fatigue=65;Action accept=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});accept();var panel=preview.GetComponent<PythonCatDetailsPanel>();panel.Open("cat-sora");var portrait=panel.panel.transform.Find("Details Card/State Portrait").GetComponent<RawImage>();
                Check(preview.PythonCatTexture(row)==ContentPreview.CatPlaceholderTexture(),"fixture truly has no base image");Check(portrait.gameObject.activeSelf && portrait.texture==CatStateImages.Resolve(row,1),"F4 works without base image");
                CatStateImageImport.Import(Fixtures.partial);accept();Check(!portrait.gameObject.activeSelf && panel.stateDetails.GetComponentInParent<ScrollRect>().GetComponent<RectTransform>().rect.width==304,"no higher or base image restores wide layout");
                File.WriteAllText(Path.Combine(Output,"extra-result.txt"),"PASS "+checks+" checks; state image without base and missing-priority fallback.");
            }
            finally{Restore();}
        }
        static void RunChecks()
        {
            foreach(var c in JsonUtility.FromJson<Cases>(File.ReadAllText(Path.Combine(Output,"fixtures/contract-cases.json"))).cases)
            {Check(!Reject(()=>CatStateImagePack.Parse(c.json))==c.valid,"v2 contract "+c.name);Check(!Reject(()=>LegacyStateImages.CatStateImagePack.Parse(c.json))==c.legacy_valid,"legacy parser "+c.name);}
            long sum=0;for(int i=0;i<16;i++)sum=CatStateImageImport.AddImageSize(sum,16L*1024*1024);Check(sum==256L*1024*1024,"total boundary");Check(Reject(()=>CatStateImageImport.AddImageSize(sum,1)),"total overflow");Check(Reject(()=>CatStateImageImport.AddImageSize(0,16L*1024*1024+1)),"per image overflow");
            var p=Fixtures;ContentPreviewSetup.Import(p.full);Check(File.ReadAllText(Pointer).Contains(CatStateImagePack.Parse(File.ReadAllText(Path.Combine(p.full,"state_images.json"))).pack_id),"full sidecar hook");
            var snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(Path.Combine(Output,"references.json")));foreach(var b in snapshot.files.Where(b=>b.path!=Pointer && !b.path.EndsWith("CatCafeOutfits/catalog.json"))){if(b.exists)WriteReference(b.path,Convert.FromBase64String(b.bytes));else if(File.Exists(b.path))File.Delete(b.path);}AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CatStateImageImport.Import(p.all);string pointer=File.ReadAllText(Pointer);CatStateImageImport.Import(p.all);Check(File.ReadAllText(Pointer)==pointer,"reimport idempotent");
            var cat=new PythonCatState{cat_id="cat-sora",content_cat_id="playtest-sora",health_status="healthy",activity="cafe",stress=65,fatigue=65,affinity=80};
            foreach(int kind in new[]{1,2,3})Check(CatStateImages.Resolve(cat,kind)!=null,"non-Mike binding "+kind);
            var twin=new PythonCatState{cat_id="rescue-200",content_cat_id="playtest-sora",health_status="healthy",activity="cafe",stress=0,fatigue=65,affinity=80};Check(CatStateImages.Resolve(cat,1)!=CatStateImages.Resolve(twin,2),"shared identity separate states");Check(CatStateImages.Resolve(cat,2)==CatStateImages.Resolve(twin,2),"same image identity shares texture");
            string summary=CatStateImageImport.ReplacementSummary(p.v1);Check(summary.Contains("外れる対応：6") && summary.Contains("ミケ専用"),"v2 to v1 preview");CatStateImageImport.Import(p.v1);Check(CatStateImages.Resolve(cat,1)==null,"v1 does not extend to other cats");var mike=new PythonCatState{cat_id="cat-mike",content_cat_id="playtest-mike"};Check(CatStateImages.Resolve(mike,1)!=null,"v1 Mike preserved");mike.cat_id="rescue-201";Check(CatStateImages.Resolve(mike,1)==null,"v1 individual restriction preserved");
            CatStateImageImport.Import(p.all);Check(CatStateImages.Resolve(mike,1)!=null,"v2 shares Mike image identity");CatStateImageImport.Import(p.partial);Check(CatStateMark.Resolve(cat)==1 && CatStateImages.Resolve(cat,1)==null && CatStateImages.Resolve(cat,2)!=null,"missing high priority never selects lower image");CatStateImageImport.Import(p.empty);Check(CatStateImages.Resolve(cat,2)==null,"empty clears all");CatStateImageImport.Import(p.all);
            string scratch=Path.Combine(Output,"invalid");Directory.CreateDirectory(scratch);string original=File.ReadAllText(Path.Combine(p.all,"state_images.json"));
            foreach(string bad in new[]{original+" ",original.Replace("\"version\": 2","\"version\": 3"),original.Replace("images/","../images/"),original.Replace("playtest-sora","not-in-bundle")})
            {File.WriteAllText(Path.Combine(scratch,"state_images.json"),bad);Check(Reject(()=>CatStateImageImport.Import(scratch)) && File.ReadAllText(Pointer)==pointer,"invalid pack preserves pointer");}
            string fresh=original.Replace(CatStateImagePack.Parse(original).pack_id,new string('b',32));File.WriteAllText(Path.Combine(scratch,"state_images.json"),fresh);Check(Reject(()=>CatStateImageImport.Import(scratch)),"missing PNG rejected");string image=CatStateImagePack.Parse(fresh).bindings[0].image;Directory.CreateDirectory(Path.Combine(scratch,"images"));File.WriteAllBytes(Path.Combine(scratch,image),new byte[]{1,2});Check(Reject(()=>CatStateImageImport.Import(scratch)) && File.ReadAllText(Pointer)==pointer,"hash failure preserves pointer");
            string sidecar=Path.Combine(p.full,"state_images.json"),held=sidecar+".held";File.Move(sidecar,held);try{ContentPreviewSetup.Import(p.full);Check(File.ReadAllText(Pointer)==pointer,"missing sidecar preserves state catalog");}finally{File.Move(held,sidecar);}
            foreach(var b in snapshot.files.Where(b=>b.path!=Pointer && !b.path.EndsWith("CatCafeOutfits/catalog.json"))){if(b.exists)WriteReference(b.path,Convert.FromBase64String(b.bytes));else if(File.Exists(b.path))File.Delete(b.path);}AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=BindingFlags.NonPublic|BindingFlags.Instance;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);typeof(PythonStateClient).GetField("refreshingState",flags).SetValue(client,false);var backend=new LocalCafeBackend(Path.Combine(Output,"editor-profile"));CafeBackends.UseLocal(backend);var state=backend.View();Action accept=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});accept();var panel=preview.GetComponent<PythonCatDetailsPanel>();panel.Open("cat-sora");var row=state.cats.Single(c=>c.cat_id=="cat-sora");var card=panel.panel.transform.Find("Details Card");var portrait=card.Find("State Portrait").GetComponent<RawImage>();string before=JsonUtility.ToJson(backend.Simulation.State);
            foreach(var size in new[]{new Vector2Int(1024,768),new Vector2Int(1280,800)})foreach(int kind in new[]{1,2,3})
            {row.stress=kind==1?65:0;row.fatigue=kind==2?65:0;row.affinity=80;accept();Check(portrait.texture==CatStateImages.Resolve(row,kind),"detail state agrees");Check(card.Find("State Badge").gameObject.activeSelf,"mark remains");Capture(preview,"state-"+kind+"-"+size.x,size);}
            row.stress=row.fatigue=65;accept();CatStateImageImport.Import(p.partial);accept();Check(portrait.texture==preview.PythonCatTexture(row),"detail higher missing uses base");CatStateImageImport.Import(p.all);row.content_cat_id="not-registered";accept();Check(!portrait.gameObject.activeSelf,"missing all images wide layout");row.content_cat_id="playtest-sora";row.health_status="sick";accept();Check(portrait.texture==preview.PythonCatTexture(row) && !card.Find("State Badge").gameObject.activeSelf,"sick base image");row.health_status="healthy";row.activity="dispatched";accept();Check(portrait.texture==preview.PythonCatTexture(row),"away base image");panel.Close();Check(JsonUtility.ToJson(backend.Simulation.State)==before,"presentation does not mutate game");
            File.WriteAllText(Path.Combine(Output,"editor-result.txt"),"PASS "+checks+" checks; six captures; v1/v2 contract, import, replacement and shared identity.");
        }
        static void Capture(ContentPreview preview,string name,Vector2Int size)=>typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{preview,Path.Combine(Output,name+".png"),size.x,size.y});
    }
}
