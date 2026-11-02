using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class AllCatStateMarkVerification
    {
        static int checks;
        static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
        public static void Run()
        {
            string root=Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),output=Path.Combine(root,"Logs/AllCatMarks20261103");Directory.CreateDirectory(output);
            foreach(string id in new[]{"cat-mike","cat-sora","request-cat-1","regular-rescue-1","visiting-cat-1","shop-1","rescue-31","dispatch-rescue-7","future-cat"})
            {
                var cat=new PythonCatState{cat_id=id,health_status="healthy",activity="cafe"};Check(CatStateMark.Resolve(cat)==0,"normal "+id);
                foreach(float n in new[]{59.99f,60,60.01f}){cat.stress=n;Check(CatStateMark.Resolve(cat)==(n<60?0:1),"stress boundary "+id);}cat.stress=0;
                foreach(float n in new[]{59.99f,60,60.01f}){cat.fatigue=n;Check(CatStateMark.Resolve(cat)==(n<60?0:2),"fatigue boundary "+id);}cat.fatigue=0;
                foreach(float n in new[]{79.99f,80,80.01f}){cat.affinity=n;Check(CatStateMark.Resolve(cat)==(n<80?0:3),"affinity boundary "+id);}
                cat.stress=cat.fatigue=80;Check(CatStateMark.Resolve(cat)==1,"stress priority");cat.stress=0;Check(CatStateMark.Resolve(cat)==2,"fatigue priority");cat.working=false;Check(CatStateMark.Resolve(cat)==2,"resting in cafe");
                foreach(string activity in new[]{"dispatched","missing","adopted","unknown"}){cat.activity=activity;Check(CatStateMark.Resolve(cat)==0,"away "+activity);}cat.activity="cafe";cat.health_status="sick";Check(CatStateMark.Resolve(cat)==0,"sick");cat.health_status="healthy";cat.stress=float.NaN;Check(CatStateMark.Resolve(cat)==0,"NaN");cat.stress=0;cat.affinity=float.PositiveInfinity;Check(CatStateMark.Resolve(cat)==0,"infinite");
            }
            Check(CatStateMark.Resolve(null)==0,"null");Check(CatStateMark.Resolve(new PythonCatState{cat_id="",health_status="healthy",activity="cafe",stress=80})==0,"empty identity");
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=BindingFlags.NonPublic|BindingFlags.Instance;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);typeof(PythonStateClient).GetField("refreshingState",flags).SetValue(client,false);
            var b=new LocalCafeBackend(Path.Combine(output,"editor-profile"));CafeBackends.UseLocal(b);var state=b.View();string before=JsonUtility.ToJson(b.Simulation.State);Action accept=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});accept();var panel=preview.GetComponent<PythonCatDetailsPanel>();panel.Open("cat-mike");var card=panel.panel.transform.Find("Details Card");var badge=card.Find("State Badge");var portrait=card.Find("State Portrait").GetComponent<RawImage>();var health=panel.stateDetails.GetComponentInParent<ScrollRect>();
            foreach(var row in state.cats){row.stress=65;accept();panel.Open(row.cat_id);Check(badge.gameObject.activeSelf,"initial cat mark "+row.cat_id);Check(portrait.texture==preview.PythonCatTexture(row),"own texture "+row.cat_id);Check(health.GetComponent<RectTransform>().rect.width==200,"split width");}
            var sora=state.cats.Single(c=>c.cat_id=="cat-sora");sora.stress=0;sora.fatigue=65;accept();panel.Open(sora.cat_id);Check(card.Find("State Caption").GetComponent<Text>().text=="疲労高め","other cat state caption");Check(!preview.GetComponent<CatOutfitPanel>().entry.gameObject.activeSelf,"outfit remains Mike only");Check(panel.title.rectTransform.rect.width==310,"other title width");
            var twin=JsonUtility.FromJson<PythonCatState>(JsonUtility.ToJson(sora));twin.cat_id="rescue-999";twin.stress=65;twin.fatigue=0;state.cats=state.cats.Concat(new[]{twin}).ToArray();accept();panel.Open(twin.cat_id);Check(card.Find("State Caption").GetComponent<Text>().text=="ストレス高め","shared texture separate individual");panel.Open(sora.cat_id);Check(card.Find("State Caption").GetComponent<Text>().text=="疲労高め","switch restores own values");
            string content=sora.content_cat_id;sora.content_cat_id="not-registered";accept();Check(!portrait.gameObject.activeSelf && !badge.gameObject.activeSelf,"missing image hides compact block");Check(health.GetComponent<RectTransform>().rect.width==304 && health.viewport.rect.width==272,"wide numerical block");sora.content_cat_id=content;accept();Check(portrait.gameObject.activeSelf && badge.gameObject.activeSelf,"restored image recomputes");
            foreach(var size in new[]{new Vector2Int(1024,768),new Vector2Int(1280,800)})
            {
                panel.Open(sora.cat_id);Capture(preview,output,"sora-"+size.x,size);sora.content_cat_id="not-registered";accept();Capture(preview,output,"missing-"+size.x,size);sora.content_cat_id=content;accept();panel.Open("cat-mike");var entry=preview.GetComponent<CatOutfitPanel>().entry.GetComponent<RectTransform>();Check(entry.anchoredPosition==new Vector2(184,-20) && entry.rect.width==112 && panel.title.rectTransform.rect.width==144,"Mike header layout");Capture(preview,output,"mike-"+size.x,size);
            }
            panel.Open(sora.cat_id);sora.health_status="sick";accept();Check(!badge.gameObject.activeSelf,"sick hides mark");sora.health_status="healthy";accept();Check(badge.gameObject.activeSelf,"healthy restores mark");panel.Disconnect();Check(!badge.gameObject.activeSelf,"disconnect clears");accept();Check(badge.gameObject.activeSelf,"reconnect recomputes");panel.Close();Check(!panel.panel.activeSelf,"close");Check(JsonUtility.ToJson(b.Simulation.State)==before,"display never mutates simulation");
            string profile=Path.Combine(output,"full-profile");Directory.CreateDirectory(profile);File.Copy(Path.Combine(root,"Logs/Housing32/Unity/profile/standalone-v1/slot-1.json"),Path.Combine(profile,"slot-1.json"),true);var full=new LocalCafeBackend(profile);var current=full.Simulation.State;CafeResponse reply=null;var load=full.Request("/commands",JsonUtility.ToJson(new PythonShiftCommand{kind="load_game",save_id="1",request_id=Guid.NewGuid().ToString("N"),instance_id=current.instance_id,expected_revision=current.revision}),r=>reply=r);while(load.MoveNext()){}Check(reply.result==UnityEngine.Networking.UnityWebRequest.Result.Success,"old full save loads");state=full.View();CafeBackends.UseLocal(full);Check(state.cats.Length==32,"full save has 32 cats");accept();
            foreach(var row in state.cats.Skip(29)){panel.Open(row.cat_id);Check(panel.title.text==row.name+"の詳細","late roster individual");row.health_status="healthy";row.activity="cafe";row.stress=65;accept();Check(CatStateMark.Resolve(row)==1,"late roster mark");}
            File.WriteAllText(Path.Combine(output,"editor-result.txt"),"PASS "+checks+" checks; six captures; 32-cat legacy save; scenes not saved.");Debug.Log("CAT_CAFE_ALL_CAT_MARKS_OK: "+checks);
        }
        static void Capture(ContentPreview p,string output,string name,Vector2Int size)=>typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{p,Path.Combine(output,name+".png"),size.x,size.y});
    }
}
