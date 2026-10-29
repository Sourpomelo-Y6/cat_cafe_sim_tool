using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor {
 public static class UIDistributionVerification {
  public static void Scene() {
   EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
   var p=UnityEngine.Object.FindObjectOfType<ContentPreview>();
   Selection.activeGameObject=p.GetComponentInChildren<Canvas>().gameObject;
   var view=SceneView.GetWindow<SceneView>();view.in2DMode=true;view.FrameSelected();view.Focus();
  }
  public static void Play() {
   EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
   UnityEditor.EditorApplication.EnterPlaymode();
  }
  public static void Run() {
   string output=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/UIDistribution/editor");Directory.CreateDirectory(output);
   EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
   var p=UnityEngine.Object.FindObjectOfType<ContentPreview>();p.Initialize();var b=new LocalCafeBackend(Path.Combine(output,"unused-save"));var d=p.GetComponent<PythonCatDetailsPanel>();string before=JsonUtility.ToJson(b.Simulation.State);
   Action<string,int,int> capture=(name,w,h)=>typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{p,Path.Combine(output,name+"-"+w+".png"),w,h});
   foreach(int w in new[]{1280,1024}) {
    int h=w==1280?800:768;UIDistributionSmoke.Accept(p,b.View());UIDistributionSmoke.Preparation(p);capture("preparation",w,h);
    d.OpenFromList("cat-mike",false);
    for(int t=0;t<5;t++){d.recordTabs[t].onClick.Invoke();UIDistributionSmoke.Bounds(d);capture("tab-"+t,w,h);}
    UIDistributionSmoke.Accept(p,UIDistributionSmoke.LongView(b));d.Profile();UIDistributionSmoke.Endpoint(d);capture("long-end",w,h);
    d.panel.transform.Find("Details Card/Close").GetComponent<Button>().onClick.Invoke();UIDistributionSmoke.Check(!d.panel.activeSelf,"Editor return to list");
   }
   UIDistributionSmoke.Check(before==JsonUtility.ToJson(b.Simulation.State),"Editor fixture changed state");
   EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
   File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: saved preparation, 5 real tabs, long profile view fixture endpoint/bounds and list return, both resolutions. Editor Canvas render; not Game view or physical input. Scene not saved, no save API.");
   CatTexturePolicy.Audit();Debug.Log("CAT_CAFE_UI_DISTRIBUTION_EDITOR_OK");
  }
 }
}
