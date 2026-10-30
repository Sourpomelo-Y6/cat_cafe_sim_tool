using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor
{
    public static class InteractionStatisticsVerification
    {
        static ContentPreview verificationPreview;
        [MenuItem("Cat Cafe/開発用/検証/交流累計（ビルドなし）")]
        public static void RunInOpenEditor()
        {
            string output="C:/Users/hi-wa/source/codex_repos/CatCafeSim/Logs/InteractionStatistics";
            var original=UnityEngine.Object.FindObjectOfType<ContentPreview>();if(original==null)throw new Exception("Open the cafe scene before verification");
            var oldScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var oldBackend=CafeBackends.Local;bool oldStandalone=CafeBackends.Standalone;
            var temporary=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                var clone=UnityEngine.Object.Instantiate(original.gameObject);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone,temporary);verificationPreview=clone.GetComponent<ContentPreview>();Environment.SetEnvironmentVariable("CATCAFE_STATISTICS_OUTPUT",output);Run();
            }
            catch(Exception ex){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"unity-error.txt"),ex.ToString());Debug.LogException(ex);}
            finally{verificationPreview=null;EditorSceneManager.CloseScene(temporary,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(oldScene);CafeBackends.UseLocal(oldBackend);CafeBackends.Standalone=oldStandalone;}
        }
        static void Check(bool valid,string message){if(!valid)throw new Exception(message);}
        static CafeResponse Send(LocalCafeBackend b,string kind,string choice=null,int slot=3)
        {
            var s=b.Simulation.State;CafeResponse result=null;var command=new PythonShiftCommand{kind=kind,choice=choice,cat_id="cat-mike",save_id=slot.ToString(),save_slot=slot,instance_id=s.instance_id,expected_revision=s.revision,request_id=Guid.NewGuid().ToString("N")};
            var routine=b.Request("/commands",JsonUtility.ToJson(command),r=>result=r);while(routine.MoveNext()){}Check(result!=null && result.result==UnityEngine.Networking.UnityWebRequest.Result.Success,kind+": "+result?.error);return result;
        }
        static string WithoutStatistics(string json)
        {
            int start=json.IndexOf("\"interaction_statistics\"",StringComparison.Ordinal),value=json.IndexOf('{',start),depth=0,end=-1;bool quoted=false,escaped=false;
            for(int i=value;i<json.Length;i++)
            {
                char c=json[i];if(quoted){if(escaped)escaped=false;else if(c=='\\')escaped=true;else if(c=='"')quoted=false;continue;}if(c=='"'){quoted=true;continue;}if(c=='{')depth++;if(c=='}' && --depth==0){end=i+1;break;}
            }
            if(start<0 || end<0)throw new Exception("Missing statistics test fixture");while(end<json.Length && char.IsWhiteSpace(json[end]))end++;if(end<json.Length && json[end]==',')end++;return json.Remove(start,end-start);
        }
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("CATCAFE_STATISTICS_OUTPUT");Directory.CreateDirectory(output);var backend=new LocalCafeBackend(Path.Combine(output,"unity-saves"));
            Send(backend,"player_begin");Send(backend,"player_step","direct");Send(backend,"player_step","pause");Send(backend,"save_game");
            string saved=File.ReadAllText(Path.Combine(output,"unity-saves/slot-3.json"));Check(saved.Contains("interaction_statistics"),"statistics saved by backend");
            var reopened=new LocalCafeBackend(Path.Combine(output,"unity-saves"));Send(reopened,"load_game");Check(reopened.InteractionStatistics.Data.rows.Sum(r=>r.count)==2,"Unity JsonUtility long and cursor round trip");Send(reopened,"player_step","direct");Check(reopened.InteractionStatistics.Data.rows.Sum(r=>r.count)==3,"resume once");
            // Simulate a genuinely old JSON document without the added field.
            string legacy=WithoutStatistics(saved);File.WriteAllText(Path.Combine(output,"unity-saves/slot-3.json"),legacy);
            Send(reopened,"load_game");Check(!reopened.InteractionStatistics.Data.complete && reopened.InteractionStatistics.Data.rows.Count==0,"old save begins with unknown prior coverage");Send(reopened,"player_step","direct");Check(reopened.InteractionStatistics.Data.rows.Sum(r=>r.count)==1,"old partial save resumes next action");
            Send(reopened,"new_game","free");Check(reopened.InteractionStatistics.Data.complete && reopened.InteractionStatistics.Data.rows.Count==0,"new game resets record");
            Send(reopened,"player_begin");Send(reopened,"player_step","direct");Send(reopened,"player_step","pause");Send(reopened,"player_finish");
            if(verificationPreview==null)EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=verificationPreview??UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var flags=BindingFlags.Instance|BindingFlags.NonPublic;var client=preview.GetComponent<PythonStateClient>();typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);CafeBackends.UseLocal(reopened);typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{reopened.View()});
            var cat=preview.GetComponent<PythonCatDetailsPanel>();cat.Open("cat-mike");var entry=cat.panel.transform.Find("Details Card/Interaction Statistics Entry").GetComponent<Button>();Check(entry.interactable,"record accessible even for unavailable interaction");entry.onClick.Invoke();var view=preview.GetComponent<InteractionStatisticsPanel>();Check(view!=null,"cat entry opens shared panel");
            var body=(Text)typeof(InteractionStatisticsPanel).GetField("body",flags).GetValue(view);Check(body.text.Contains("通常交流行動：1回") && body.text.Contains("補助操作：1回"),"counts rendered");
            var capture=typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(int width in new[]{1280,1024})capture.Invoke(null,new object[]{preview,Path.Combine(output,"cat-record-"+width+".png"),width,800});
            var modal=(GameObject)typeof(InteractionStatisticsPanel).GetField("panel",flags).GetValue(view);
            Check(modal.GetComponent<Canvas>().overrideSorting && modal.GetComponent<Canvas>().sortingOrder==100,"modal has stable sorting layer");
            var catDropdown=(Dropdown)typeof(InteractionStatisticsPanel).GetField("cats",flags).GetValue(view);catDropdown.alphaFadeSpeed=0;typeof(Dropdown).GetMethod("Start",flags).Invoke(catDropdown,null);catDropdown.Show();
            var popup=catDropdown.transform.Find("Dropdown List");Check(popup!=null && popup.gameObject.activeSelf,"cat dropdown opens");
            var group=popup.GetComponent<CanvasGroup>();if(group!=null)group.alpha=1;
            Check(popup.GetComponent<Canvas>().sortingOrder>modal.GetComponent<Canvas>().sortingOrder,"dropdown renders above modal");
            cat.panel.transform.SetAsLastSibling();int order=modal.transform.GetSiblingIndex();
            for(int frame=0;frame<120;frame++)typeof(InteractionStatisticsPanel).GetMethod("Update",flags).Invoke(view,null);
            Check(modal.transform.GetSiblingIndex()==order && modal.activeSelf && popup.gameObject.activeSelf,"refresh never fights sibling order or hides dropdown");
            foreach(int width in new[]{1280,1024})capture.Invoke(null,new object[]{preview,Path.Combine(output,"cat-dropdown-"+width+".png"),width,800});
            var option=popup.GetComponentsInChildren<Toggle>().First(t=>t.GetComponentInChildren<Text>().text=="全猫");option.isOn=true;Check(catDropdown.value==0 && body.text.Contains("対象：全猫"),"dropdown item changes actual filter");
            popup.gameObject.SetActive(false);catDropdown.Hide();
            var scroll=(ScrollRect)typeof(InteractionStatisticsPanel).GetField("scroll",flags).GetValue(view);scroll.verticalNormalizedPosition=0;capture.Invoke(null,new object[]{preview,Path.Combine(output,"cat-record-details.png"),1280,800});
            var panel=(GameObject)typeof(InteractionStatisticsPanel).GetField("panel",flags).GetValue(view);panel.SetActive(false);cat.Close();var directory=preview.GetComponent<PythonCustomerDirectoryPanel>();directory.Open();var personEntry=directory.close.transform.parent.Find("Interaction Statistics Entry").GetComponent<Button>();personEntry.onClick.Invoke();
            var people=(Dropdown)typeof(InteractionStatisticsPanel).GetField("people",flags).GetValue(view);int player=people.options.FindIndex(o=>o.text=="プレイヤー");Check(player>=0,"player selectable before customer record");people.value=player;Check(body.text.Contains("通常交流行動：1回"),"person perspective same fact");capture.Invoke(null,new object[]{preview,Path.Combine(output,"player-record.png"),1280,800});
            string before=JsonUtility.ToJson(reopened.InteractionStatistics.Data);for(int i=0;i<20;i++){people.value=0;people.value=player;}Check(before==JsonUtility.ToJson(reopened.InteractionStatistics.Data),"browsing is read-only");
            File.WriteAllText(Path.Combine(output,"unity-result.txt"),"PASS: Unity compilation, backend commands, JsonUtility/save/load/new game, old partial save, runtime entry buttons, cat/person/player filters and read-only browsing; canvas and opened dropdown rendered at 1280 and 1024; stable modal layer under 120 competing refreshes; dropdown selection applied. Standalone build not run.");Debug.Log("CAT_CAFE_INTERACTION_STATISTICS_OK");
        }
    }
}
