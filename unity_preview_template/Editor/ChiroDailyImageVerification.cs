using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CatCafePreview.Standalone;

namespace CatCafePreview.Editor
{
    public static class ChiroDailyImageVerification
    {
        public static void VerifyAndBuild(){Run();StandaloneScreenSetup.Build();}
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("CATCAFE_IMAGE_OUTPUT");
            Directory.CreateDirectory(output);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var catalog=CafeNarrativeResources.Load();catalog.Validate();
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var client=preview.GetComponent<PythonStateClient>();
            typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var panel=preview.GetComponent<PythonPlayerPanel>();
            // Real shop admission path, retaining the standard candidate ID and rules.
            var shopRules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
            shopRules.starting_funds=10000;shopRules.store_events.probability=0;
            var joined=new LocalCafeBackend(Path.Combine(output,"shop-saves"),shopRules);
            joined.Simulation.Execute("purchase_housing");joined.Simulation.Execute("purchase_cat",catId:"shop-1");
            if(!joined.Simulation.State.cats.Any(c=>c.id=="shop-1"))throw new Exception("Shop admission failed");
            var normal=preview.PlayerTexture("shop-1","normal","normal");
            var sleep=preview.PlayerTexture("shop-1","sleep","sleep");
            if(normal==null || sleep==null || normal==sleep || normal==ContentPreview.CatPlaceholderTexture())throw new Exception("Normal/sleep resources missing");
            foreach(string mode in new[]{"teaser","ball","plush","tunnel","pet","brush","voice","presence"})
            foreach(string special in new[]{"open_up","simultaneous"})
                if(preview.PlayerTexture("shop-1",mode,mode+"__"+special)==normal)throw new Exception("Special resource missing");
            joined.Simulation.Execute("player_begin",catId:"shop-1");
            joined.Simulation.Execute("player_step",choice:"direct");
            var admittedReceipt=joined.Narratives.Rows.Last();
            if(CafeNarrativeResources.Texture(admittedReceipt.image_resource)==null)throw new Exception("Joined cat receipt image missing");
            Request(joined,"save_game");var loaded=new LocalCafeBackend(Path.Combine(output,"shop-saves"),shopRules);Request(loaded,"load_game");
            if(!loaded.Simulation.State.cats.Any(c=>c.id=="shop-1") || loaded.Narratives.Rows.Last().image_resource!=admittedReceipt.image_resource)throw new Exception("Joined cat reload changed image");
            int checkedImages=0;
            foreach(string catId in new[]{"shop-1"})
            foreach(string mode in new[]{"teaser","ball","plush","tunnel","pet","brush","voice","presence"})
            {
                var preparation=preview.PlayerTexture(catalog.ContentId(catId,catId),mode,mode+"__switch");
                var execution=preview.PlayerTexture(catalog.ContentId(catId,catId),mode,mode+"__direct");
                if(preparation==null || execution==null || preparation==execution)throw new Exception("Missing distinct pair: "+mode);
                foreach(string action in new[]{"direct","adapt"})
                {
                    var rules=Rules(catId);
                    int modeIndex=Array.FindIndex(rules.types,t=>t.id==mode);
                    rules.cats.Single(c=>c.id==catId).type_preferences[modeIndex]=12/(rules.exchange.direct_gain*rules.types[modeIndex].gain);
                    var backend=new LocalCafeBackend(Path.Combine(output,"saves"),rules);
                    var sim=backend.Simulation;
                    sim.Execute("player_begin",catId:catId);
                    if(mode!="teaser")sim.Execute("player_step",choice:"switch",source:mode);
                    CafeBackends.UseLocal(backend);
                    typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{backend.View()});
                    panel.Open();
                    if(panel.picture.texture!=preparation)throw new Exception("Preparation UI mismatch: "+mode);
                    sim.Execute("player_step",choice:action);
                    var receipt=backend.Narratives.Rows.Last();
                    if(receipt.image_scene_key!=mode+"__"+action || CafeNarrativeResources.Texture(receipt.image_resource)!=execution)
                        throw new Exception("Execution receipt mismatch: "+mode+"/"+action+"/"+receipt.reaction);
                    typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{backend.View()});
                    if(panel.picture.texture!=execution)throw new Exception("Execution UI mismatch: "+mode);
                    if(action=="direct" && (mode=="ball" || mode=="pet"))
                        typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{preview,Path.Combine(output,catId+"-"+mode+".png"),1280,800});
                    // Save actual receipts, then load without recomputing the image.
                    Request(backend,"save_game");
                    var reopened=new LocalCafeBackend(Path.Combine(output,"saves"),rules);
                    Request(reopened,"load_game");
                    if(reopened.Narratives.Rows.Last().image_resource!=receipt.image_resource)
                        throw new Exception("Image changed on disk reload: "+mode);
                    panel.Close();checkedImages++;
                }
                foreach(string reaction in new[]{"turn_away","confused","listless"})
                {
                    var rules=Rules(catId);var definition=rules.cats.Single(c=>c.id==catId);
                    int index=Array.FindIndex(rules.types,t=>t.id==mode);
                    definition.type_preferences[index]=1;
                    if(reaction=="turn_away")definition.type_preferences[index]=0;
                    if(reaction=="confused")definition.type_preferences[index]=2/(rules.exchange.direct_gain*rules.types[index].gain);
                    var backend=new LocalCafeBackend(Path.Combine(output,"negative-saves"),rules);
                    if(reaction=="listless")backend.Simulation.State.cats.Single(c=>c.id==catId).stamina=10;
                    backend.Simulation.Execute("player_begin",catId:catId);
                    if(mode!="teaser")backend.Simulation.Execute("player_step",choice:"switch",source:mode);
                    backend.Simulation.Execute("player_step",choice:"direct");
                    var row=backend.Narratives.Rows.Last();
                    if(row.reaction!=reaction || row.image_scene_key!=mode || CafeNarrativeResources.Texture(row.image_resource)!=preparation)
                        throw new Exception("Negative reaction image mismatch: "+mode+"/"+reaction+"/"+row.reaction);
                    if(backend.View().player_interaction.image_key!=mode+"__switch")throw new Exception("Fallback image key mismatch");
                    checkedImages++;
                }
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checkedImages+" actual execution/negative-reaction cases across Chiro and eight modes; separate preparation/execution Resources and UI textures; disk save/reload. Resource/UI verification completed.");
            Debug.Log("CAT_CAFE_CHIRO_IMAGES_OK");
        }
        private static CafeRules Rules(string catId)
        {
            var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
            rules.store_events.probability=0;
            if(!rules.cats.Any(c=>c.id==catId)){var definition=rules.pet_shop.Select(r=>r.cat).Concat(new[]{rules.intake.cat,rules.regular_introduction.cat,rules.visiting_cat.cat}).Single(c=>c.id==catId);rules.cats=rules.cats.Concat(new[]{JsonUtility.FromJson<CatDefinition>(JsonUtility.ToJson(definition))}).ToArray();definition.id="fixture-"+catId;}
            return rules;
        }
        private static void Request(LocalCafeBackend backend,string kind)
        {
            CafeResponse result=null;
            var command=new PythonShiftCommand{kind=kind,save_id="2",save_slot=2,instance_id=backend.Simulation.State.instance_id,expected_revision=backend.Simulation.State.revision,request_id=Guid.NewGuid().ToString("N")};
            var routine=backend.Request("/commands",JsonUtility.ToJson(command),response=>result=response);
            while(routine.MoveNext()){}
            if(result==null || result.result!=UnityEngine.Networking.UnityWebRequest.Result.Success)throw new Exception(result?.text??"No response");
        }
    }
}
