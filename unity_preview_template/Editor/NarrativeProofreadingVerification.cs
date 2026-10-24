using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor {
 public static class NarrativeProofreadingVerification {
  public static void CompileOnly(){Debug.Log("CAT_CAFE_PROOFREADING_EDITOR_COMPILE_OK");}
  public static void VerifyAndBuild(){
   string output=Environment.GetEnvironmentVariable("CATCAFE_PROOF_OUTPUT");
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_IMPORT_PACK"));
   var b=NarrativeProofreadingSmoke.Check(output);
   EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var flags=BindingFlags.Instance|BindingFlags.NonPublic;var client=preview.GetComponent<PythonStateClient>();typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);CafeBackends.UseLocal(b);typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{b.View()});
   foreach(string name in new[]{"cat-sora","cat-sora-tired","cat-mike-common-fixture"}){
    NarrativeProofreadingSmoke.Show(NarrativeProofreadingSmoke.Cases[name]);
    foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1024,768)}){
     var capture=typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic);var player=preview.GetComponent<PythonPlayerPanel>();
     capture.Invoke(null,new object[]{preview,Path.Combine(output,name+"-"+size.x+".png"),size.x,size.y});
     player.ToggleHistory();player.historyScroll.verticalNormalizedPosition=0;
     capture.Invoke(null,new object[]{preview,Path.Combine(output,name+"-history-"+size.x+".png"),size.x,size.y});player.ToggleHistory();
    }
   }
   Debug.Log("CAT_CAFE_PROOFREADING_OK");
   // Temporary use of existing policy for the pre-existing pending Rin pack only.
   string pending=Path.Combine(Application.dataPath,"Resources/CatCafeContent/Imports/4762dd1bd978461bb07c041c59417281");
   var originals=Directory.GetFiles(pending,"*.png.meta",SearchOption.AllDirectories).ToDictionary(p=>p,File.ReadAllBytes);
   try{CatTexturePolicy.ApplyPack(pending);StandaloneScreenSetup.Build();}
   finally{foreach(var pair in originals)if(!File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value))File.WriteAllBytes(pair.Key,pair.Value);}
  }
 }
}
