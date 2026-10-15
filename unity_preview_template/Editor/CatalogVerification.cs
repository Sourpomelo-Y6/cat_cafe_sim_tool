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
