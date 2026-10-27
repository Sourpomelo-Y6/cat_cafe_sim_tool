using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor {
 public static class CompletedNarrativeVerification {
  public static void VerifyAndBuild() {
   string output=Environment.GetEnvironmentVariable("CATCAFE_COMPLETED_OUTPUT");Directory.CreateDirectory(output);
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_IMPORT_PACK"));
   CompletedNarrativeSmoke.Audit(output);
   EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var p=UnityEngine.Object.FindObjectOfType<ContentPreview>();p.Initialize();
   foreach(var c in CompletedNarrativeSmoke.Cases()) {
    var b=CompletedNarrativeSmoke.BuildCase(c,output,false);CompletedNarrativeSmoke.BuildCase(c,output,true);
    if(c.capture) {
     CompletedNarrativeSmoke.Show(p,b,c);
     foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1024,768)})typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{p,Path.Combine(output,c.id+"-"+size.x+".png"),size.x,size.y});
     if(c.id=="presence-simultaneous") {
      CompletedNarrativeSmoke.DescribeScroll(p,output);var panel=p.GetComponent<PythonPlayerPanel>();panel.ToggleHistory();Canvas.ForceUpdateCanvases();CompletedNarrativeSmoke.DescribeScroll(p,output);
      panel.historyScroll.verticalNormalizedPosition=0;CompletedNarrativeSmoke.DescribeScroll(p,output);
      typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{p,Path.Combine(output,"history-bottom-1024.png"),1024,768});panel.ToggleHistory();
     }
    }
    CompletedNarrativeSmoke.Close(p);
   }
   File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: actual Resources, full selection audit, 54 bounded action cases (3 partial memory catalogs), disk reload exact receipts/images, representative Canvas at 1280/1024. Not natural-play coverage.");
   Debug.Log("CAT_CAFE_COMPLETED_NARRATIVES_OK");BuildLatest();
  }
  // Historic packs carry concurrent images with pending importer settings. Apply the
  // existing policy only for this build, then restore every original meta byte.
  public static void BuildLatest() {
   string output=Environment.GetEnvironmentVariable("CATCAFE_COMPLETED_OUTPUT");Directory.CreateDirectory(output);
   var bad=CatTexturePolicy.Targets().Where(p=>!CatTexturePolicy.Matches((TextureImporter)AssetImporter.GetAtPath(p))).ToArray();
   var original=new Dictionary<string,byte[]>();string backup=Path.Combine(output,"policy-meta");Directory.CreateDirectory(backup);
   File.WriteAllLines(Path.Combine(backup,"paths.txt"),bad);
   for(int i=0;i<bad.Length;i++){original[bad[i]+".meta"]=File.ReadAllBytes(bad[i]+".meta");File.WriteAllBytes(Path.Combine(backup,i+".meta"),original[bad[i]+".meta"]);}
   try {
    foreach(string path in bad){var importer=(TextureImporter)AssetImporter.GetAtPath(path);CatTexturePolicy.Configure(importer);importer.SaveAndReimport();}
    CatTexturePolicy.Validate();StandaloneScreenSetup.Build();
   } finally {
    foreach(var row in original)File.WriteAllBytes(row.Key,row.Value);
    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    foreach(var row in original)if(!File.ReadAllBytes(row.Key).SequenceEqual(row.Value))throw new Exception("Original importer restore mismatch "+row.Key);
    File.WriteAllText(Path.Combine(output,"policy-restored.txt"),"Restored "+original.Count+" original meta files byte-for-byte after bounded build policy application.");
   }
  }
 }
}
