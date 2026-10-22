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
    public static class LeoDailyImageVerification
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
            joined.Simulation.Execute("purchase_housing");joined.Simulation.Execute("purchase_cat",catId:"shop-2");
            if(!joined.Simulation.State.cats.Any(c=>c.id=="shop-2"))throw new Exception("Shop admission failed");
            var normal=preview.PlayerTexture("shop-2","normal","normal");
            var sleep=preview.PlayerTexture("shop-2","sleep","sleep");
            if(normal==null || sleep==null || normal==sleep || normal==ContentPreview.CatPlaceholderTexture())throw new Exception("Normal/sleep resources missing");
            CafeBackends.UseLocal(joined);
            foreach(string scene in new[]{"normal","sleep"})
            {
                joined.Simulation.Execute("set_shifts",working:scene=="normal"?new[]{"shop-2"}:new string[0]);
                typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{joined.View()});
                var roster=preview.GetComponent<PythonCatRoster>();
                var card=Enumerable.Range(0,roster.Count).Select(i=>roster.GetCard(i)).Single(c=>c.state.cat_id=="shop-2");
                if(!card.picture.enabled || !card.picture.gameObject.activeInHierarchy || card.picture.texture!=preview.PlayerTexture("shop-2",scene,scene))throw new Exception("Normal/sleep roster UI mismatch");
                var preparationPanel=preview.GetComponent<PythonPreparationPanel>();
                preparationPanel.Open();preparationPanel.SelectCatId("shop-2");
                Canvas.ForceUpdateCanvases();preparationPanel.catsScroll.verticalNormalizedPosition=0;
                if(preparationPanel.SelectedCatId!="shop-2")throw new Exception("Preparation selection mismatch");
                typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{preview,Path.Combine(output,"shop-2-"+scene+".png"),1280,800});
            }
            foreach(string mode in new[]{"teaser","ball","plush","tunnel","pet","brush","voice","presence"})
            foreach(string special in new[]{"open_up","simultaneous"})
                if(preview.PlayerTexture("shop-2",mode,mode+"__"+special)==normal)throw new Exception("Special resource missing");
            joined.Simulation.Execute("player_begin",catId:"shop-2");
            joined.Simulation.Execute("player_step",choice:"direct");
            var admittedReceipt=joined.Narratives.Rows.Last();
            if(CafeNarrativeResources.Texture(admittedReceipt.image_resource)==null)throw new Exception("Joined cat receipt image missing");
            Request(joined,"save_game");var loaded=new LocalCafeBackend(Path.Combine(output,"shop-saves"),shopRules);Request(loaded,"load_game");
            if(!loaded.Simulation.State.cats.Any(c=>c.id=="shop-2") || loaded.Narratives.Rows.Last().image_resource!=admittedReceipt.image_resource)throw new Exception("Joined cat reload changed image");
            int checkedImages=0;
            foreach(string catId in new[]{"shop-2"})
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
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checkedImages+" actual execution/negative-reaction cases across Leo and eight modes; separate preparation/execution Resources and UI textures; disk save/reload. Resource/UI verification completed.");
            Debug.Log("CAT_CAFE_LEO_IMAGES_OK");
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
