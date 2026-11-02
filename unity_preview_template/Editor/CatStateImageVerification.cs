using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    // Explicit QA only. Prepare/Restore bracket the temporary Player build as well.
    public static class CatStateImageVerification
    {
        [Serializable] class Packs {public string all,full,partial,empty;}
        [Serializable] class Backup {public string path;public bool exists;public string bytes;}
        [Serializable] class Snapshot {public Backup[] files;public string[] created;}
        static string Output=>Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/StateImagesF4");
        static string ResourcesRoot=>Path.Combine(Application.dataPath,"Resources");
        static Packs Fixtures=>JsonUtility.FromJson<Packs>(File.ReadAllText(Path.Combine(Output,"fixtures/packs.json")));
        static int checks;
        static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
        static string Pointer=>Path.Combine(ResourcesRoot,"CatCafeStateImages/catalog.json");
        static void SnapshotFiles()
        {
            string path=Path.Combine(Output,"references.json");if(File.Exists(path))throw new Exception("Restore the previous F4 snapshot first.");
            var p=Fixtures;string[] folders={p.all,p.full,p.partial,p.empty};
            var targets=new List<string>();
            foreach(var folder in folders){string id=CatStateImagePack.Parse(File.ReadAllText(Path.Combine(folder,"state_images.json"))).pack_id;targets.Add(Path.Combine(ResourcesRoot,"CatCafeStateImages/Imports",id));}
            string fullId=CatStateImagePack.Parse(File.ReadAllText(Path.Combine(p.full,"state_images.json"))).pack_id;
            targets.Add(Path.Combine(ResourcesRoot,"CatCafeContent/Imports",fullId));targets.Add(Path.Combine(ResourcesRoot,"CatCafeContent/People",fullId));targets.Add(Path.Combine(ResourcesRoot,"CatCafeEvents/Imports",fullId));
            if(targets.Any(Directory.Exists))throw new Exception("Fixture IDs already exist; use fresh fixtures.");
            string[] files={Pointer,Path.Combine(ResourcesRoot,"CatCafeContent/catalog.json"),Path.Combine(ResourcesRoot,"CatCafeContent/human_portraits.json"),Path.Combine(ResourcesRoot,"CatCafeEvents/catalog.json")};
            File.WriteAllText(path,JsonUtility.ToJson(new Snapshot{files=files.Select(f=>new Backup{path=f,exists=File.Exists(f),bytes=File.Exists(f)?Convert.ToBase64String(File.ReadAllBytes(f)):null}).ToArray(),created=targets.ToArray()},true));
        }
        public static void Restore()
        {
            string path=Path.Combine(Output,"references.json");if(!File.Exists(path))throw new Exception("F4 reference snapshot is missing.");
            var snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(path));
            foreach(var file in snapshot.files){if(file.exists)File.WriteAllBytes(file.path,Convert.FromBase64String(file.bytes));else {if(File.Exists(file.path))File.Delete(file.path);if(File.Exists(file.path+".meta"))File.Delete(file.path+".meta");}}
            foreach(string target in snapshot.created){string full=Path.GetFullPath(target);if(!full.StartsWith(Path.GetFullPath(ResourcesRoot)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Unsafe fixture cleanup path.");if(Directory.Exists(full))Directory.Delete(full,true);if(File.Exists(full+".meta"))File.Delete(full+".meta");}
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            File.Move(path,Path.Combine(Output,"references-restored-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+".json"));
            Debug.Log("CAT_CAFE_STATE_IMAGES_RESTORED");
        }
        public static void Prepare()
        {
            SnapshotFiles();try{RunChecks();CatStateImageImport.Import(Fixtures.all);Debug.Log("CAT_CAFE_STATE_IMAGES_PREPARED: "+checks);}catch{Restore();throw;}
        }
        public static void CompleteAndRestore()
        {
            try
            {
                string profile=Path.Combine(Output,"native-profile");Directory.CreateDirectory(profile);
                File.Copy(Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/StateMarkUsability20261102/after-care-save.json"),Path.Combine(profile,"slot-1.json"),true);
                var backend=new LocalCafeBackend(profile);Action<string,string> command=(kind,choice)=>
                {
                    var s=backend.Simulation.State;var cmd=new PythonShiftCommand{request_id=Guid.NewGuid().ToString("N"),instance_id=s.instance_id,expected_revision=s.revision,kind=kind,choice=choice,cat_id="cat-mike",save_id="1",save_slot=1};
                    CafeResponse response=null;var request=backend.Request("/commands",JsonUtility.ToJson(cmd),r=>response=r);while(request.MoveNext()){}
                    Check(response!=null && response.result==UnityEngine.Networking.UnityWebRequest.Result.Success,"native command "+kind+": "+response?.text);
                };
                command("load_game","");var mike=backend.View().cats.Single(c=>c.cat_id=="cat-mike");Check(CatStateMark.Resolve(mike)==3 && CatStateImages.Resolve(mike,3)!=null,"old save computes affinity image");
                // Isolated fixture only: establish the two care thresholds, preserving earned affinity.
                var cat=backend.Simulation.State.cats.Single(c=>c.id=="cat-mike");cat.stress=65;cat.fatigue=65;
                typeof(LocalCafeBackend).GetMethod("ResetJournal",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(backend,null);
                foreach(int kind in new[]{1,2})
                {
                    mike=backend.View().cats.Single(c=>c.cat_id=="cat-mike");Check(CatStateMark.Resolve(mike)==kind && CatStateImages.Resolve(mike,kind)!=null,"native care before image "+kind);
                    var item=backend.Simulation.Rules.item_shop.First(r=>kind==1?r.item.stress_relief>5:r.item.fatigue_relief>5);
                    command("purchase_item",item.item.id);string source=backend.Simulation.ItemInventory().Last(i=>i.item.id==item.item.id).source;command("use_item",source);
                }
                mike=backend.View().cats.Single(c=>c.cat_id=="cat-mike");Check(CatStateMark.Resolve(mike)==3 && CatStateImages.Resolve(mike,3)!=null,"care returns to affinity image");command("save_game","");
                string saved=File.ReadAllText(Path.Combine(profile,"slot-1.json"));Check(!saved.Contains("state_images") && !saved.Contains("affinity_high"),"save excludes image presentation");
                var reloaded=new LocalCafeBackend(profile);var s2=reloaded.Simulation.State;CafeResponse loaded=null;var req=reloaded.Request("/commands",JsonUtility.ToJson(new PythonShiftCommand{request_id=Guid.NewGuid().ToString("N"),instance_id=s2.instance_id,expected_revision=s2.revision,kind="load_game",save_id="1"}),r=>loaded=r);while(req.MoveNext()){}
                Check(loaded!=null && loaded.result==UnityEngine.Networking.UnityWebRequest.Result.Success,"new backend resumes save");mike=reloaded.View().cats.Single(c=>c.cat_id=="cat-mike");Check(CatStateMark.Resolve(mike)==3 && CatStateImages.Resolve(mike,3)!=null,"resume recomputes image");
                File.WriteAllText(Path.Combine(Output,"native-result.txt"),"PASS "+checks+" checks; legacy save, care commands, save and fresh backend resume.");
            }
            finally{Restore();}
        }
        static void RunChecks()
        {
            var p=Fixtures;CatStateImageImport.Import(p.all);string pointer=File.ReadAllText(Pointer);CatStateImageImport.Import(p.all);Check(File.ReadAllText(Pointer)==pointer,"idempotent import");
            var cat=new PythonCatState{cat_id="cat-mike",content_cat_id="playtest-mike",activity="cafe",health_status="healthy"};
            int[] widths={160,80,128},heights={100,160,128};
            for(int i=1;i<=3;i++){var image=CatStateImages.Resolve(cat,i);Check(image!=null && image.width==widths[i-1] && image.height==heights[i-1],"imported PNG dimensions "+i);}
            string scratch=Path.Combine(Output,"invalid");Directory.CreateDirectory(scratch);string original=File.ReadAllText(Path.Combine(p.all,"state_images.json"));
            foreach(string broken in new[]{original.Replace("\"version\": 1","\"version\": 2"),original.Replace("stress_high","unknown"),original.Replace("playtest-mike","other"),original.Replace("images/","../images/"),original.Replace("\"version\": 1","\"version\": 1, \"version\": 1")})
            {
                Check(broken!=original,"invalid fixture differs");File.WriteAllText(Path.Combine(scratch,"state_images.json"),broken);bool rejected=false;try{CatStateImageImport.Import(scratch);}catch(ArgumentException){rejected=true;}Check(rejected && File.ReadAllText(Pointer)==pointer,"invalid input preserves pointer");
            }
            // Existing ID must reject even a whitespace-only change.
            File.WriteAllText(Path.Combine(scratch,"state_images.json"),original+" ");bool collision=false;try{CatStateImageImport.Import(scratch);}catch(ArgumentException){collision=true;}Check(collision && File.ReadAllText(Pointer)==pointer,"same ID collision rejected");
            CatStateImageImport.Import(p.partial);cat.stress=cat.fatigue=65;Check(CatStateMark.Resolve(cat)==1 && CatStateImages.Resolve(cat,1)==null && CatStateImages.Resolve(cat,2)!=null,"missing higher priority image falls back to base");
            CatStateImageImport.Import(p.empty);Check(CatStateImages.Resolve(cat,1)==null && CatStateImages.Resolve(cat,2)==null,"explicit empty clears images");
            ContentPreviewSetup.Import(p.full);Check(File.ReadAllText(Pointer).Contains(CatStateImagePack.Parse(File.ReadAllText(Path.Combine(p.full,"state_images.json"))).pack_id),"full bundle imports sidecar");
            // Restore normal main/human/event references while retaining isolated state images.
            var snap=JsonUtility.FromJson<Snapshot>(File.ReadAllText(Path.Combine(Output,"references.json")));
            foreach(var f in snap.files.Where(f=>f.path!=Pointer)){if(f.exists)File.WriteAllBytes(f.path,Convert.FromBase64String(f.bytes));else if(File.Exists(f.path))File.Delete(f.path);}
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);CatStateImageImport.Import(p.all);
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var hidden=BindingFlags.NonPublic|BindingFlags.Instance;typeof(PythonStateClient).GetField("preview",hidden).SetValue(client,preview);
            var backend=new LocalCafeBackend(Path.Combine(Output,"editor-profile"));CafeBackends.UseLocal(backend);var state=backend.View();var mike=state.cats.Single(c=>c.cat_id=="cat-mike");var panel=preview.GetComponent<PythonCatDetailsPanel>();Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",hidden).Invoke(client,new object[]{state});apply();panel.Open("cat-mike");var card=panel.panel.transform.Find("Details Card");
            foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1024,768)})foreach(int kind in new[]{1,2,3})
            {
                mike.stress=kind==1?65:0;mike.fatigue=kind==2?65:0;mike.affinity=80;apply();var portrait=card.Find("State Portrait").GetComponent<RawImage>();Check(portrait.texture==CatStateImages.Resolve(mike,kind),"detail image agrees with mark");Check(card.Find("State Badge").gameObject.activeSelf,"mark retained");Check(portrait.rectTransform.rect.width<=96 && portrait.rectTransform.rect.height<=96,"aspect fits");
                typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{preview,Path.Combine(Output,"editor-"+kind+"-"+size.x+".png"),size.x,size.y});
            }
            mike.health_status="sick";apply();Check(card.Find("State Portrait").GetComponent<RawImage>().texture==preview.PythonCatTexture(mike),"sick uses base");mike.health_status="healthy";mike.activity="dispatch";apply();Check(card.Find("State Portrait").GetComponent<RawImage>().texture==preview.PythonCatTexture(mike),"away uses base");mike.activity="cafe";mike.stress=float.NaN;apply();Check(CatStateMark.Resolve(mike)==0,"invalid state suppressed");
            panel.Open(state.cats.First(c=>c.cat_id!="cat-mike").cat_id);Check(!card.Find("State Badge").gameObject.activeSelf,"other cat resets");panel.Close();
            File.WriteAllText(Path.Combine(Output,"editor-result.txt"),"PASS "+checks+" checks; six captures; scenes not saved.");
        }
    }
}
