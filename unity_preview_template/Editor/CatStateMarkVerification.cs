using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class CatStateMarkVerification
    {
        static int checks;
        static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
        public static void Run()
        {
            string output=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/CatStateMarks");Directory.CreateDirectory(output);
            var cat=new PythonCatState{cat_id="cat-mike",activity="cafe",health_status="healthy"};
            Check(CatStateMark.Resolve(cat)==0,"normal");
            foreach(float n in new[]{59.99f,60,60.01f}){cat.stress=n;Check(CatStateMark.Resolve(cat)==(n<60?0:1),"stress boundary");}
            cat.stress=0;foreach(float n in new[]{59.99f,60,60.01f}){cat.fatigue=n;Check(CatStateMark.Resolve(cat)==(n<60?0:2),"fatigue boundary");}
            cat.fatigue=0;foreach(float n in new[]{79.99f,80,80.01f}){cat.affinity=n;Check(CatStateMark.Resolve(cat)==(n<80?0:3),"affinity boundary");}
            cat.stress=cat.fatigue=80;Check(CatStateMark.Resolve(cat)==1,"priority stress");cat.stress=0;Check(CatStateMark.Resolve(cat)==2,"priority fatigue");
            cat.health_status="sick";Check(CatStateMark.Resolve(cat)==0,"sick");cat.health_status="healthy";cat.activity="dispatch";Check(CatStateMark.Resolve(cat)==0,"away");cat.activity="cafe";cat.cat_id="cat-sora";Check(CatStateMark.Resolve(cat)==2,"other cat same priority");cat.cat_id="cat-mike";cat.stress=float.NaN;Check(CatStateMark.Resolve(cat)==0,"invalid");cat.stress=float.PositiveInfinity;Check(CatStateMark.Resolve(cat)==0,"infinite");Check(CatStateMark.Resolve(null)==0,"missing");
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var hidden=BindingFlags.NonPublic|BindingFlags.Instance;typeof(PythonStateClient).GetField("preview",hidden).SetValue(client,preview);
            var backend=new LocalCafeBackend(Path.Combine(output,"profile"));CafeBackends.UseLocal(backend);var state=backend.View();var mike=state.cats.Single(c=>c.cat_id=="cat-mike");mike.affinity=80;mike.stress=60;mike.fatigue=60;
            var panel=preview.GetComponent<PythonCatDetailsPanel>();Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",hidden).Invoke(client,new object[]{state});apply();panel.Open("cat-mike");
            var card=panel.panel.transform.Find("Details Card");var badge=card.Find("State Badge");Check(badge.gameObject.activeSelf,"badge visible");Check(card.Find("State Caption").GetComponent<Text>().text=="ストレス高め","stress caption");
            string before=JsonUtility.ToJson(state);
            foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1024,768)})foreach(int kind in new[]{1,2,3})
            {
                mike.stress=kind==1?60:0;mike.fatigue=kind==2?60:0;apply();
                typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{preview,Path.Combine(output,"mark-"+kind+"-"+size.x+".png"),size.x,size.y});
                Check(badge.gameObject.activeSelf,"update badge");
                var symbol=badge.GetComponentInChildren<CatStateMarkGraphic>();using(var mesh=new VertexHelper()){typeof(CatStateMarkGraphic).GetMethod("OnPopulateMesh",hidden,null,new[]{typeof(VertexHelper)},null).Invoke(symbol,new object[]{mesh});Check(symbol.canvasRenderer!=null && mesh.currentVertCount>0,"symbol mesh");}
                var healthScroll=panel.stateDetails.GetComponentInParent<ScrollRect>();healthScroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();Check(healthScroll.content.rect.height>healthScroll.viewport.rect.height,"health scroll available");
                Check(panel.stateDetails.text.Contains("本日の交流"),"health end retained");
            }
            before=JsonUtility.ToJson(state);foreach(var tab in panel.recordTabs){tab.onClick.Invoke();Check(panel.panel.activeSelf,"record tab remains open");}panel.GrowthRecords();Check(JsonUtility.ToJson(state)==before,"presentation mutated state");
            string contentId=mike.content_cat_id;mike.content_cat_id="missing-image";apply();Check(!badge.gameObject.activeSelf,"missing image hides mark");mike.content_cat_id=contentId;apply();
            panel.Open(state.cats.First(c=>c.cat_id!="cat-mike").cat_id);Check(!badge.gameObject.activeSelf,"other clears");panel.Open("cat-mike");panel.Disconnect();Check(!badge.gameObject.activeSelf,"disconnect clears");apply();Check(badge.gameObject.activeSelf,"reconnect");
            state.instance_id="reload-test";apply();Check(!panel.panel.activeSelf,"load closes");panel.Open("cat-mike");Check(badge.gameObject.activeSelf,"reload computes");panel.Close();Check(!panel.panel.activeSelf,"close");
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS "+checks+" checks; six captures; isolated state; no scene saves");Debug.Log("CAT_CAFE_STATE_MARK_OK: "+checks);
        }
    }
}
