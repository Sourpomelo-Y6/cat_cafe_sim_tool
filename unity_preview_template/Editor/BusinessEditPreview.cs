using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor {
 // Explicit editor-only preview: no ExecuteAlways, backend commands, or real save IO.
 public static class BusinessEditPreview {
  const string ScenePath="Assets/Scenes/CatCafePythonState.unity";
  [MenuItem("Cat Cafe/編集プレビュー/営業準備を表示")]
  public static void Preparation() { Apply(false,true); }
  [MenuItem("Cat Cafe/編集プレビュー/猫詳細・個性と外見を表示")]
  public static void Profile() { Apply(true,true); }
  [MenuItem("Cat Cafe/編集プレビュー/営業準備を表示",true)]
  [MenuItem("Cat Cafe/編集プレビュー/猫詳細・個性と外見を表示",true)]
  static bool Available() { return !EditorApplication.isPlayingOrWillChangePlaymode && EditorSceneManager.GetActiveScene().path==ScenePath; }
  static ContentPreview Apply(bool profile,bool undo) {
   if(!Available())throw new InvalidOperationException("CatCafePythonStateを編集モードで開いてください。");
   var p=UnityEngine.Object.FindObjectOfType<ContentPreview>();if(p==null)throw new InvalidOperationException("ContentPreviewがありません。");
   if(undo)Undo.RegisterFullObjectHierarchyUndo(p.gameObject,"現在のゲームUIを編集プレビュー");
   p.Initialize();
   // An uncreated unique directory prevents reading the user's save/read/gallery data.
   var backend=new LocalCafeBackend(Path.Combine(Path.GetTempPath(),"CatCafeEditPreview-"+Guid.NewGuid().ToString("N")));
   var client=p.GetComponent<PythonStateClient>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   typeof(PythonStateClient).GetField("preview",flags).SetValue(client,p);
   typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{backend.View()});
   p.GetComponent<PythonPreparationPanel>()?.Close();
   var details=p.GetComponent<PythonCatDetailsPanel>();details.ResetSelection();
   PlayerInteractionLayout.Apply(p.GetComponent<PythonPlayerPanel>());
   if(profile){details.Open("cat-mike");details.Profile();}
   Canvas.ForceUpdateCanvases();
   // Script changes to inherited prefab properties need explicit overrides to survive reload.
   foreach(var component in p.GetComponentsInChildren<Component>(true))
    if(component!=null && PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
   EditorSceneManager.MarkSceneDirty(p.gameObject.scene);SceneView.RepaintAll();
   return p;
  }
  public static void VerifyAndSave() {
   EditorSceneManager.OpenScene(ScenePath);var p=Apply(false,false);
   var header=p.GetComponent<PythonBusinessHeader>();var service=p.GetComponent<PythonServiceScreen>();
   if(header.auto.toggle.gameObject.activeSelf || !header.preparation.gameObject.activeSelf || !header.summary.text.Contains("営業準備"))throw new Exception("Preparation header differs from runtime");
   if(service.detailText.text.Contains("三毛の毛並み") || !service.rosterPanel.activeSelf)throw new Exception("Old content preview retained");
   string output=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/EditorLayoutPreview");Directory.CreateDirectory(output);
   foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1024,768)})typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{p,Path.Combine(output,"preparation-"+size.x+".png"),size.x,size.y});
   EditorSceneManager.SaveScene(p.gameObject.scene);
   Apply(true,false);var d=p.GetComponent<PythonCatDetailsPanel>();if(!d.details.text.Contains("プロフィール") || d.scroll.GetComponent<RectTransform>().sizeDelta!=new Vector2(488,236))throw new Exception("Detail preview differs from current layout");
   typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{p,Path.Combine(output,"profile-1280.png"),1280,800});
   // Save only the default preparation layout. The profile remains an explicit menu preview.
   EditorSceneManager.OpenScene(ScenePath);VerifySaved();File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: editor preparation uses runtime View/Accept, header visibility, sidebar, detail bounds, 1280/1024 captures; prefab overrides persist after scene reload. No game commands or real save IO; no build.");Debug.Log("CAT_CAFE_EDIT_PREVIEW_OK");
  }
  public static void VerifySaved() {
   EditorSceneManager.OpenScene(ScenePath);
   var p=UnityEngine.Object.FindObjectOfType<ContentPreview>();
   var header=p.GetComponent<PythonBusinessHeader>();var service=p.GetComponent<PythonServiceScreen>();
   if(header.auto.toggle.gameObject.activeSelf || !header.preparation.gameObject.activeSelf || !header.summary.text.Contains("営業準備") || service.detailText.text.Contains("三毛の毛並み") || !service.rosterPanel.activeSelf)
    throw new Exception("Saved editor preparation did not survive scene reload");
   Debug.Log("CAT_CAFE_SAVED_EDIT_PREVIEW_OK");
  }
 }
}
