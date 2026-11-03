using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor
{
    // Explicit QA only. Prepare/Restore bracket the temporary Player build as well.
    public static class CatOutfitVerification
    {
        [Serializable] class Packs {public string all,full,states,empty;}
        [Serializable] class Backup {public string path;public bool exists;public string bytes;}
        [Serializable] class Snapshot {public Backup[] files;public string[] created;}
        static string Output=>Environment.GetEnvironmentVariable("CATCAFE_OUTFIT_QA_ROOT")??Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/OutfitsF5");
        static string ResourcesRoot=>Path.Combine(Application.dataPath,"Resources");
        static Packs Fixtures=>JsonUtility.FromJson<Packs>(File.ReadAllText(Path.Combine(Output,"fixtures/packs.json")));
        static int checks;
        static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
        static string Pointer=>Path.Combine(ResourcesRoot,"CatCafeOutfits/catalog.json");
        static void SnapshotFiles()
        {
            string path=Path.Combine(Output,"references.json");if(File.Exists(path))throw new Exception("Restore the previous F5 snapshot first.");
            var p=Fixtures;string[] folders={p.all,p.full,p.empty};
            var targets=new List<string>();
            foreach(var folder in folders){string id=CatOutfitPack.Parse(File.ReadAllText(Path.Combine(folder,"cat_outfits.json"))).pack_id;targets.Add(Path.Combine(ResourcesRoot,"CatCafeOutfits/Imports",id));}
            string fullId=CatOutfitPack.Parse(File.ReadAllText(Path.Combine(p.full,"cat_outfits.json"))).pack_id;
            targets.Add(Path.Combine(ResourcesRoot,"CatCafeContent/Imports",fullId));targets.Add(Path.Combine(ResourcesRoot,"CatCafeContent/People",fullId));targets.Add(Path.Combine(ResourcesRoot,"CatCafeEvents/Imports",fullId));
            string stateId=CatStateImagePack.Parse(File.ReadAllText(Path.Combine(p.states,"state_images.json"))).pack_id;targets.Add(Path.Combine(ResourcesRoot,"CatCafeStateImages/Imports",stateId));
            if(targets.Any(Directory.Exists))throw new Exception("Fixture IDs already exist; use fresh fixtures.");
            string[] files={Pointer,Path.Combine(ResourcesRoot,"CatCafeStateImages/catalog.json"),Path.Combine(ResourcesRoot,"CatCafeContent/catalog.json"),Path.Combine(ResourcesRoot,"CatCafeContent/human_portraits.json"),Path.Combine(ResourcesRoot,"CatCafeEvents/catalog.json")};
            File.WriteAllText(path,JsonUtility.ToJson(new Snapshot{files=files.Select(f=>new Backup{path=f,exists=File.Exists(f),bytes=File.Exists(f)?Convert.ToBase64String(File.ReadAllBytes(f)):null}).ToArray(),created=targets.ToArray()},true));
        }
        public static void Restore()
        {
            string path=Path.Combine(Output,"references.json");if(!File.Exists(path))throw new Exception("F5 reference snapshot is missing.");
            var snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(path));
            foreach(var file in snapshot.files){if(file.exists)File.WriteAllBytes(file.path,Convert.FromBase64String(file.bytes));else {if(File.Exists(file.path))File.Delete(file.path);if(File.Exists(file.path+".meta"))File.Delete(file.path+".meta");}}
            foreach(string target in snapshot.created){string full=Path.GetFullPath(target);if(!full.StartsWith(Path.GetFullPath(ResourcesRoot)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Unsafe fixture cleanup path.");if(Directory.Exists(full))Directory.Delete(full,true);if(File.Exists(full+".meta"))File.Delete(full+".meta");}
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            File.Move(path,Path.Combine(Output,"references-restored-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+".json"));
            Debug.Log("CAT_CAFE_OUTFITS_RESTORED");
        }
        public static void Prepare()
        {
            SnapshotFiles();try{RunChecks();CatOutfitImport.Import(Fixtures.all);CatStateImageImport.Import(Fixtures.states);Debug.Log("CAT_CAFE_OUTFITS_PREPARED: "+checks);}catch{Restore();throw;}
        }
        static CafeResponse Command(LocalCafeBackend b,string kind,string choice="",int slot=1,string id=null)
        {
            var s=b.Simulation.State;CafeResponse reply=null;var request=b.Request("/commands",JsonUtility.ToJson(new PythonShiftCommand{kind=kind,choice=choice,cat_id="cat-mike",save_slot=slot,save_id=slot.ToString(),request_id=id??Guid.NewGuid().ToString("N"),instance_id=s.instance_id,expected_revision=s.revision}),r=>reply=r);while(request.MoveNext()){}return reply;
        }
        static void Good(CafeResponse response,string label){Check(response!=null && response.result==UnityEngine.Networking.UnityWebRequest.Result.Success,label+": "+response?.text);}
        public static void CompleteAndRestore()
        {
            try
            {
                string profile=Path.Combine(Output,"editor-profile"),path=Path.Combine(profile,"slot-9.json");var b=new LocalCafeBackend(profile);Good(Command(b,"load_game",slot:9),"extra load");string state=JsonUtility.ToJson(b.Simulation.State),selection=b.CatOutfit("cat-mike"),save=File.ReadAllText(path);
                try
                {
                    File.WriteAllText(path,save.Replace("\"cat_outfits\": [","\"cat_outfits\": false, \"unused\": ["));
                    Check(Command(b,"load_game",slot:9).result!=UnityEngine.Networking.UnityWebRequest.Result.Success,"malformed selection load rejects");Check(JsonUtility.ToJson(b.Simulation.State)==state && b.CatOutfit("cat-mike")==selection,"load failure preserves full session");Check(b.ListSaves().saves.Any(r=>r.slot==9 && r.phase=="invalid"),"invalid slot listed safely");
                }
                finally{File.WriteAllText(path,save);}
                bool duplicate=false;try{CafeCatOutfits.ReadSave("{\"cat_outfits\":null,\"cat_outfits\":[]}");}catch(ArgumentException){duplicate=true;}Check(duplicate,"duplicate root selection rejects");
                string pointer=File.ReadAllText(Pointer),sidecar=Path.Combine(Fixtures.full,"cat_outfits.json"),held=sidecar+".held";
                File.Move(sidecar,held);try{ContentPreviewSetup.Import(Fixtures.full);Check(File.ReadAllText(Pointer)==pointer,"old bundle without sidecar preserves selection catalog");}finally{File.Move(held,sidecar);}
                File.WriteAllText(Path.Combine(Output,"extra-result.txt"),"PASS "+checks+" checks; corrupt load and missing sidecar.");
            }
            finally{Restore();}
        }
        static void RunChecks()
        {
            var p=Fixtures;
            ContentPreviewSetup.Import(p.full);Check(File.ReadAllText(Pointer).Contains(CatOutfitPack.Parse(File.ReadAllText(Path.Combine(p.full,"cat_outfits.json"))).pack_id),"full sidecar hook");
            var snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(Path.Combine(Output,"references.json")));foreach(var f in snapshot.files.Where(f=>f.path!=Pointer)){if(f.exists)File.WriteAllBytes(f.path,Convert.FromBase64String(f.bytes));else if(File.Exists(f.path))File.Delete(f.path);}AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CatOutfitImport.Import(p.all);string pointer=File.ReadAllText(Pointer);CatOutfitImport.Import(p.all);Check(File.ReadAllText(Pointer)==pointer,"same import idempotent");
            var cat=new PythonCatState{cat_id="cat-mike",content_cat_id="playtest-mike",activity="cafe",health_status="healthy",outfit_id="ribbon"};
            Check(CatOutfits.Resolve(cat)?.width==180,"imported ribbon");
            CatStateImageImport.Import(p.states);
            foreach(float stress in new[]{0,60})foreach(float fatigue in new[]{0,60})foreach(float affinity in new[]{0,80}){cat.stress=stress;cat.fatigue=fatigue;cat.affinity=affinity;Check((CatOutfits.Resolve(cat)!=null)==(CatStateMark.Resolve(cat)==0),"state priority combination");}
            cat.stress=cat.fatigue=cat.affinity=0;cat.health_status="sick";Check(CatOutfits.Resolve(cat)==null,"sick");cat.health_status="healthy";cat.activity="dispatched";Check(CatOutfits.Resolve(cat)==null,"away");cat.activity="cafe";cat.stress=float.NaN;Check(CatOutfits.Resolve(cat)==null,"invalid value");cat.stress=0;cat.cat_id="cat-sora";cat.outfit_id="";Check(CatOutfits.Resolve(cat)==null,"other individual normal choice");cat.cat_id="cat-mike";cat.outfit_id="ribbon";
            string invalid=Path.Combine(Output,"invalid");Directory.CreateDirectory(invalid);string original=File.ReadAllText(Path.Combine(p.all,"cat_outfits.json"));
            foreach(string broken in new[]{original.Replace("\"version\": 1","\"version\": 3").Replace("\"version\": 2","\"version\": 3"),original.Replace("ribbon","unknown"),original.Replace("playtest-mike","other"),original.Replace("images/","../images/"),original.Replace("\"version\": 1","\"version\": 1,\"version\":1")})
            {File.WriteAllText(Path.Combine(invalid,"cat_outfits.json"),broken);bool rejected=false;try{CatOutfitImport.Import(invalid);}catch(ArgumentException){rejected=true;}Check(rejected && File.ReadAllText(Pointer)==pointer,"reject invalid pack and preserve pointer");}
            string fresh=original.Replace(CatOutfitPack.Parse(original).pack_id,new string('a',32));File.WriteAllText(Path.Combine(invalid,"cat_outfits.json"),fresh);bool missing=false;try{CatOutfitImport.Import(invalid);}catch(ArgumentException){missing=true;}Check(missing,"missing PNG rejected");Directory.CreateDirectory(Path.Combine(invalid,"images"));string image=CatOutfitPack.Parse(fresh).outfits[0].image;File.WriteAllBytes(Path.Combine(invalid,image),new byte[]{1,2,3});bool corrupt=false;try{CatOutfitImport.Import(invalid);}catch(ArgumentException){corrupt=true;}Check(corrupt && File.ReadAllText(Pointer)==pointer,"hash failure preserves pointer");
            File.WriteAllText(Path.Combine(invalid,"cat_outfits.json"),original+" ");bool collision=false;try{CatOutfitImport.Import(invalid);}catch(ArgumentException){collision=true;}Check(collision,"same ID altered content rejected");
            string profile=Path.Combine(Output,"editor-profile");var b=new LocalCafeBackend(profile);CafeBackends.UseLocal(b);string before=JsonUtility.ToJson(b.Simulation.State);
            Good(Command(b,"set_cat_outfit","ribbon",id:"same-request"),"select ribbon");Good(Command(b,"set_cat_outfit","ribbon",id:"same-request"),"retry same request");Check(JsonUtility.ToJson(b.Simulation.State)==before,"no simulation mutation or revision");Check(b.CatOutfit("cat-mike")=="ribbon","selection stored");
            Good(Command(b,"save_game",slot:9),"save ribbon");Good(Command(b,"set_cat_outfit"),"clear selection");Good(Command(b,"save_game",slot:8),"save normal");Good(Command(b,"load_game",slot:9),"load ribbon");Check(b.CatOutfit("cat-mike")=="ribbon","ribbon slot restored");Good(Command(b,"load_game",slot:8),"load normal");Check(b.CatOutfit("cat-mike")=="","no slot leak");
            var freshBackend=new LocalCafeBackend(profile);Good(Command(freshBackend,"load_game",slot:9),"fresh backend load");Check(freshBackend.CatOutfit("cat-mike")=="ribbon","fresh backend restores");
            string save=File.ReadAllText(Path.Combine(profile,"slot-9.json"));Check(!save.Contains("CatCafeOutfits/") && !save.Contains("ribbon.png"),"save contains stable choice only");
            foreach(string bad in new[]{"{}","false","[{\"cat_id\":\"cat-mike\",\"outfit_id\":3}]","[{\"cat_id\":\"cat-mike\",\"outfit_id\":\"\"}]","[{\"cat_id\":\"Cat-sora\",\"outfit_id\":\"ribbon\"}]","[{\"cat_id\":\"cat-mike\",\"outfit_id\":\"ribbon\",\"extra\":1}]","[{\"cat_id\":\"cat-mike\",\"outfit_id\":\"ribbon\"},{\"cat_id\":\"cat-mike\",\"outfit_id\":\"ribbon\"}]"})
            {bool rejected=false;try{CafeCatOutfits.ReadSave("{\"cat_outfits\":"+bad+"}");}catch(ArgumentException){rejected=true;}Check(rejected,"strict selection JSON");}
            Check(CafeCatOutfits.ReadSave("{\"text\":\"cat_outfits\",\"nested\":{\"cat_outfits\":false}}").Length==0,"nested and prose are not root selection");Check(CafeCatOutfits.ReadSave("{\"cat_outfits\":null}").Length==0,"null legacy");
            string unknown=save.Replace("\"ribbon\"","\"future-set\"");File.WriteAllText(Path.Combine(profile,"slot-9.json"),unknown);Good(Command(freshBackend,"load_game",slot:9),"unknown ID compatible");Check(freshBackend.CatOutfit("cat-mike")=="future-set","unknown selection preserved");File.WriteAllText(Path.Combine(profile,"slot-9.json"),save);
            CatOutfitImport.Import(p.empty);Good(Command(freshBackend,"load_game",slot:9),"load without image");Check(freshBackend.CatOutfit("cat-mike")=="ribbon" && CatOutfits.Resolve(cat)==null,"missing image preserves choice");CatOutfitImport.Import(p.all);Check(CatOutfits.Resolve(cat)!=null,"reimport restores image");
            string old=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/StateMarkUsability20261102/after-care-save.json");File.Copy(old,Path.Combine(profile,"slot-1.json"),true);Good(Command(freshBackend,"load_game"),"legacy save loads");Check(freshBackend.CatOutfit("cat-mike")=="","legacy resets appearance");
            Good(Command(b,"new_game","free"),"new game");Check(b.CatOutfit("cat-mike")=="","new game resets");
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var hidden=BindingFlags.NonPublic|BindingFlags.Instance;typeof(PythonStateClient).GetField("preview",hidden).SetValue(client,preview);CafeBackends.UseLocal(b);Good(Command(b,"set_cat_outfit","ribbon"),"editor choose");var state=b.View();typeof(PythonStateClient).GetField("refreshingState",hidden).SetValue(client,false);typeof(PythonStateClient).GetMethod("Accept",hidden).Invoke(client,new object[]{state});var panel=preview.GetComponent<PythonCatDetailsPanel>();panel.Open("cat-mike");var ui=preview.GetComponent<CatOutfitPanel>();Check(ui!=null,"outfit UI exists");Check(client.CanChangeCatOutfit,"editor can change; backend="+b.CanChangeCatOutfit+" client="+client.CanManageSaves);ui.Open();Check(ui.host!=null && ui.host.activeSelf,"selection dialog open");ui.Choose("");ui.Close();Check(b.CatOutfit("cat-mike")=="ribbon","cancel does not mutate");
            foreach(var size in new[]{new Vector2Int(1024,768),new Vector2Int(1280,800)}){ui.Open();typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{preview,Path.Combine(Output,"editor-dialog-"+size.x+".png"),size.x,size.y});ui.Close();}
            panel.Close();Check(!ui.host.activeSelf,"detail close clears dialog");File.WriteAllText(Path.Combine(Output,"editor-result.txt"),"PASS "+checks+" checks; no scene saves.");
        }
    }
}
