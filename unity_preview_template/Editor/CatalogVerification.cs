using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor
{
    public static class CatalogVerification
    {
        [Serializable] private class RotationRows { public CafeNarrativeRow[] rows; }

        // Unlike the temporary import probe, this explicitly activates the authored pack for ordinary Play.
        public static void ActivateAndVerifyAuthoredPack()
        {
            string folder=Environment.GetEnvironmentVariable("CATCAFE_IMPORT_PACK");
            string output=Environment.GetEnvironmentVariable("CATCAFE_NARRATIVE_OUTPUT");
            Directory.CreateDirectory(output);
            string pointer=Path.Combine(Application.dataPath,"Resources/CatCafeContent/catalog.json");
            byte[] original=File.ReadAllBytes(pointer);
            try
            {
                ContentPreviewSetup.Import(folder);
                var catalog=CafeNarrativeResources.Load();
                if(catalog==null)throw new Exception("No active narrative catalog after import");
                string saves=Path.Combine(output,"rotation-saves");
                var b=new LocalCafeBackend(saves);
                b.Simulation.Execute("player_begin",catId:"cat-mike");
                for(int i=0;i<4;i++)b.Simulation.Execute("player_step",choice:"pause");
                var rows=b.Narratives.Rows.ToArray();
                if(rows.Length!=4 || rows.Any(r=>r.catalog_revision!=catalog.revision)
                    || rows.Take(3).Select(r=>r.cat_text_id).Distinct().Count()!=3
                    || rows.Take(3).Select(r=>r.speech_id).Distinct().Count()!=3
                    || rows[3].cat_text_id!=rows[0].cat_text_id || rows[3].speech_id!=rows[0].speech_id)
                    throw new Exception("Active authored candidates did not rotate 1/2/3/1");
                string session=b.Simulation.ActivePlayer.id, prose=b.Narratives.Read(session);
                string before=JsonUtility.ToJson(b.Simulation.State);
                for(int i=0;i<20;i++)if(b.Narratives.Read(session)!=prose)throw new Exception("Read changed prose");
                if(JsonUtility.ToJson(b.Simulation.State)!=before)throw new Exception("Read changed gameplay");
                SaveCommand(b,"save_game");
                var reopened=new LocalCafeBackend(saves);SaveCommand(reopened,"load_game");
                if(reopened.Narratives.Read(session)!=prose)throw new Exception("Load changed historical prose");
                reopened.Simulation.Execute("player_step",choice:"pause");
                var next=reopened.Narratives.Rows.Last();
                if(next.cat_text_id!=rows[1].cat_text_id || next.speech_id!=rows[1].speech_id)
                    throw new Exception("Candidate rotation did not resume after load");
                File.WriteAllText(Path.Combine(output,"rotation-rows.json"),JsonUtility.ToJson(new RotationRows{rows=reopened.Narratives.Rows.ToArray()},true));
                NarrativeVerification.Run();
                if(CafeNarrativeResources.Load()?.pack_id!=catalog.pack_id)throw new Exception("Active pack changed during screen verification");
                File.WriteAllText(Path.Combine(output,"active-catalog-result.txt"),"PASS: authored catalog active for ordinary Editor Play; speech and cat 1/2/3/1, disk save/load then candidate 2, 20 stable read-only views, current screens verified.\nPack: "+catalog.pack_id+"\nRevision: "+catalog.revision+"\n");
                Debug.Log("CAT_CAFE_ACTIVE_CATALOG_OK");
            }
            catch
            {
                File.WriteAllBytes(pointer,original);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                throw;
            }
        }

        private static void SaveCommand(LocalCafeBackend b,string kind)
        {
            CafeResponse response=null;
            var command=new PythonShiftCommand{kind=kind,save_id="2",save_slot=2,instance_id=b.Simulation.State.instance_id,
                expected_revision=b.Simulation.State.revision,request_id=Guid.NewGuid().ToString("N")};
            var request=b.Request("/commands",JsonUtility.ToJson(command),r=>response=r);
            while(request.MoveNext()){}
            if(response==null || response.result!=UnityEngine.Networking.UnityWebRequest.Result.Success)
                throw new Exception(response?.text??"Save/load returned no response");
        }

        // Verify a real authored export without changing the ordinary active pack or making a build.
        public static void VerifyAuthoredPack()
        {
            string folder=Environment.GetEnvironmentVariable("CATCAFE_IMPORT_PACK");
            string output=Environment.GetEnvironmentVariable("CATCAFE_NARRATIVE_OUTPUT");
            Directory.CreateDirectory(output);
            var expected=JsonUtility.FromJson<CafeNarrativeCatalog>(File.ReadAllText(Path.Combine(folder,"interaction_texts.json")));
            expected.Validate();
            string pointer=Path.Combine(Application.dataPath,"Resources/CatCafeContent/catalog.json");
            byte[] original=File.ReadAllBytes(pointer);
            string imports=Path.GetFullPath(Path.Combine(Application.dataPath,"Resources/CatCafeContent/Imports"));
            string target=Path.GetFullPath(Path.Combine(imports,expected.pack_id));
            if(!CafeNarrativeCatalog.Hex(expected.pack_id,32) || !target.StartsWith(imports+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)
                || Directory.Exists(target))throw new Exception("Verification requires a new isolated import target");
            try
            {
                ContentPreviewSetup.Import(folder);
                ContentPreviewSetup.Import(folder);
                var loaded=CafeNarrativeResources.Load();
                if(loaded==null || loaded.pack_id!=expected.pack_id || loaded.revision!=expected.revision
                    || loaded.entries.Length!=expected.entries.Length)throw new Exception("Authored catalog was not loaded intact");
                var b=new LocalCafeBackend(Path.Combine(output,"authored-saves"));
                b.Simulation.Execute("player_begin",catId:"cat-mike");
                b.Simulation.Execute("player_step",choice:"direct");
                var row=b.Narratives.Rows.Last();
                var cat=loaded.Find("cat_response","cat-mike","teaser","direct",row.reaction);
                if(row.catalog_revision!=loaded.revision || cat==null || !cat.variants.Any(v=>v.id==row.cat_text_id && row.text.Contains(v.text)))
                    throw new Exception("Actual action/reaction did not select authored cat text");
                if(string.IsNullOrEmpty(row.image_resource) || CafeNarrativeResources.Texture(row.image_resource)==null)
                    throw new Exception("Authored receipt image not loaded");
                b.Simulation.Execute("player_step",choice:"switch",source:"pet");
                b.Simulation.Execute("player_step",choice:"direct");
                row=b.Narratives.Rows.Last();
                if(loaded.Find("speech","player","pet","direct","any")==null
                    && (!string.IsNullOrEmpty(row.speech_id) || string.IsNullOrEmpty(row.speech)))throw new Exception("Missing speech fallback failed");
                NarrativeVerification.Run();
                File.WriteAllText(Path.Combine(output,"authored-import-result.txt"),
                    "PASS: actual authored export imported twice, revision/entries preserved, Resources catalog and receipt image loaded, actual reaction selected authored text, missing speech fallback, save/load and both existing screen captures.\n"
                    +"Pack: "+loaded.pack_id+"\nEntries: "+loaded.entries.Length+"\nCandidates: "+loaded.entries.Sum(e=>e.variants.Length)+"\nRevision: "+loaded.revision+"\n");
                Debug.Log("CAT_CAFE_AUTHORED_IMPORT_OK");
            }
            finally
            {
                File.WriteAllBytes(pointer,original);
                // Remove only this verification's newly created temporary import; retain the exported pack.
                if(Directory.Exists(target))Directory.Delete(target,true);
                if(File.Exists(target+".meta"))File.Delete(target+".meta");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        public static void VerifyAndBuild()
        {
            string folder=Environment.GetEnvironmentVariable("CATCAFE_CATALOG_FIXTURE");
            string output=Environment.GetEnvironmentVariable("CATCAFE_NARRATIVE_OUTPUT");
            string source=Path.Combine(folder,"interaction_texts.json"), json=File.ReadAllText(source);
            var catalog=JsonUtility.FromJson<CafeNarrativeCatalog>(json);
            string pointer=Path.Combine(Application.dataPath,"Resources/CatCafeContent/catalog.json");
            byte[] original=File.ReadAllBytes(pointer);
            string imports=Path.GetFullPath(Path.Combine(Application.dataPath,"Resources/CatCafeContent/Imports"));
            string target=Path.GetFullPath(Path.Combine(imports,catalog.pack_id));
            if(!CafeNarrativeCatalog.Hex(catalog.pack_id,32) || !target.StartsWith(imports+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || Directory.Exists(target))throw new Exception("QA pack must be new and isolated");
            try
            {
                Action<CafeNarrativeCatalog> reject=bad=>{
                    File.WriteAllText(source,JsonUtility.ToJson(bad));bool rejected=false;
                    try{ContentPreviewSetup.Import(folder);}catch(ArgumentException){rejected=true;}
                    if(!rejected || !original.SequenceEqual(File.ReadAllBytes(pointer)) || Directory.Exists(target))throw new Exception("Invalid import modified active pack");
                    File.WriteAllText(source,json);
                };
                var bad=JsonUtility.FromJson<CafeNarrativeCatalog>(json);bad.revision=new string('0',64);reject(bad);
                bad=JsonUtility.FromJson<CafeNarrativeCatalog>(json);bad.pack_id=new string('a',32);reject(bad);
                bad=JsonUtility.FromJson<CafeNarrativeCatalog>(json);bad.cat_bindings[0].content_cat_id="missing-cat";bad.revision=bad.ComputeRevision();reject(bad);
                ContentPreviewSetup.Import(folder);ContentPreviewSetup.Import(folder);
                var loaded=CafeNarrativeResources.Load();if(loaded==null || loaded.revision!=catalog.revision)throw new Exception("Resource catalog differs");
                var b=new LocalCafeBackend(Path.Combine(output,"resource-check"));
                b.Simulation.Execute("player_begin",catId:"cat-mike");b.Simulation.Execute("player_step",choice:"direct");
                var row=b.Narratives.Rows.Single();if(string.IsNullOrEmpty(row.speech_id) || CafeNarrativeResources.Texture(row.image_resource)==null)throw new Exception("Imported text/image not selected");
                string installed=File.ReadAllText(Path.Combine(target,"interaction_texts.json"));
                bad=JsonUtility.FromJson<CafeNarrativeCatalog>(json);bad.entries[0].variants[0].text+="更新";bad.revision=bad.ComputeRevision();
                File.WriteAllText(source,JsonUtility.ToJson(bad));bool changedRejected=false;
                try{ContentPreviewSetup.Import(folder);}catch(ArgumentException){changedRejected=true;}
                if(!changedRejected || installed!=File.ReadAllText(Path.Combine(target,"interaction_texts.json")))throw new Exception("Immutable pack overwritten");
                File.WriteAllText(source,json);
                NarrativeVerification.Run();
                var panel=UnityEngine.Object.FindObjectOfType<ContentPreview>().GetComponent<PythonPlayerPanel>();
                string history=panel.history.text;
                panel.history.text=history.Split(new[]{"── 履歴"},StringSplitOptions.None)[0].TrimEnd();
                Canvas.ForceUpdateCanvases();
                if(panel.history.preferredHeight>panel.historyScroll.viewport.rect.height)throw new Exception("Latest short exchange does not fit viewport");
                panel.history.text=history;
                File.WriteAllText(Path.Combine(output,"catalog-result.txt"),"Unity JsonUtility hash, rejected imports, atomic pointer, idempotent import, immutable pack, Resource text/image, save/load and actual UI checks passed.\n");
                // Build the ordinary active pack. QA resources remain available only to the opt-in resume check.
                File.WriteAllBytes(pointer,original);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                StandaloneScreenSetup.Build();
            }
            finally
            {
                File.WriteAllText(source,json);File.WriteAllBytes(pointer,original);
                // Only this invocation's newly imported QA pack is removed; existing packs are preserved.
                if(Directory.Exists(target))Directory.Delete(target,true);
                if(File.Exists(target+".meta"))File.Delete(target+".meta");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }
    }
}
