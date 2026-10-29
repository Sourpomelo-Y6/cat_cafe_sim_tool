using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor {
 public static class CatDetailsOverlapVerification {
  public static void VerifySaved() {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
   var p=UnityEngine.Object.FindObjectOfType<ContentPreview>();var d=p.GetComponent<PythonCatDetailsPanel>();
   if(d.scroll.GetComponent<RectTransform>().sizeDelta!=new Vector2(488,236) || d.interact.GetComponent<RectTransform>().anchoredPosition!=new Vector2(24,-408))throw new Exception("Saved detail bounds invalid");
   Debug.Log("CAT_CAFE_SAVED_DETAIL_BOUNDS_OK");
  }
  public static void VerifyAndBuild() {
   string output=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/CatDetailsFix");Directory.CreateDirectory(output);
   PythonCatDetailsLayout.BuildAndVerify();
   var fixedScene=File.ReadAllBytes("Assets/Scenes/CatCafePythonState.unity");
   // Reproduce the exact regeneration order which used to overwrite the split card.
   PythonPlayerSetup.Build();
   var p=UnityEngine.Object.FindObjectOfType<ContentPreview>();var d=p.GetComponent<PythonCatDetailsPanel>();
   var area=d.scroll.GetComponent<RectTransform>();
   if(area.sizeDelta!=new Vector2(488,236) || d.interact.GetComponent<RectTransform>().anchoredPosition!=new Vector2(24,-408))throw new Exception("Player regeneration overwrote split detail layout");
   p.Initialize();var client=p.GetComponent<PythonStateClient>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,p);
   var backend=new LocalCafeBackend();var state=backend.View();state.instance_id="detail-layout-qa";
   typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});d.Open("cat-mike");
   foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1024,768)}) {
    for(int tab=0;tab<5;tab++) {
     d.recordTabs[tab].onClick.Invoke();Canvas.ForceUpdateCanvases();
     if(d.scroll.viewport.rect.width>area.rect.width || area.anchoredPosition.x+area.rect.width>860 || -area.anchoredPosition.y+area.rect.height>=374)throw new Exception("Detail bounds overlap controls");
     if(d.details.rectTransform.rect.width>d.scroll.viewport.rect.width+.1f)throw new Exception("Text wider than viewport");
     typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{p,Path.Combine(output,"editor-"+tab+"-"+size.x+".png"),size.x,size.y});
    }
   }
   d.Close();
   // Restore the final player image layout after checking the legacy generator.
   File.WriteAllBytes("Assets/Scenes/CatCafePythonState.unity",fixedScene);
   EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
   File.WriteAllText(Path.Combine(output,"editor-result.txt"),"PASS: existing detail fixture, player regeneration order, five real profile/record tabs at 1280/1024, section bounds and text width. No natural-play or physical input claim.");
   StandaloneScreenSetup.Build();
  }
 }
}
