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
    public static class CatExecutionImageVerification
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
            int checkedImages=0;
            foreach(string catId in new[]{"cat-mike","cat-tama","cat-sora","cat-kohaku","cat-mugi","request-cat-1","regular-rescue-1","visiting-cat-1"})
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
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checkedImages+" actual execution/negative-reaction cases across eight cats and eight modes; separate preparation/execution Resources and UI textures; disk save/reload. Resource/UI verification completed.");
            Debug.Log("CAT_CAFE_CAT_EXECUTION_IMAGES_OK");
        }
        private static CafeRules Rules(string catId)
        {
            var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
            rules.store_events.probability=0;
            if(!rules.cats.Any(c=>c.id==catId)){var definition=new[]{rules.intake.cat,rules.regular_introduction.cat,rules.visiting_cat.cat}.Single(c=>c.id==catId);rules.cats=rules.cats.Concat(new[]{JsonUtility.FromJson<CatDefinition>(JsonUtility.ToJson(definition))}).ToArray();definition.id="fixture-"+catId;}
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
