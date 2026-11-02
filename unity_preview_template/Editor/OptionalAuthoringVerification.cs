using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor
{
    public static class OptionalAuthoringVerification
    {
        static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static int checks;static string output,fixtures;
        static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        static void Good(LocalCafeBackend b,string kind,string choice=null)=>StandaloneDispatchConversationSmoke.Good(b,kind,choice,cat:"cat-mike");
        static void Prepare(LocalCafeBackend b){Good(b,"day_off");if(b.Simulation.State.phase=="closed")Good(b,"next_day");Good(b,"player_begin");Good(b,"player_step","direct");if(b.Simulation.PlayerActive)Good(b,"player_finish");}
        static void Apply(ContentPreview p,LocalCafeBackend b){CafeBackends.UseLocal(b);typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(p.GetComponent<PythonStateClient>(),new object[]{b.View()});}
        static void Capture(ContentPreview p,string name,int width,int height)=>typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{p,Path.Combine(output,name+".png"),width,height});
        static void Finish(LocalCafeBackend b,bool choice)
        {
            var r=b.OptionalEvent;r.progress.node=r.introduction.Length;r.progress.page_offset=0;CafeOptionalEvents.Choose(r,choice);r.progress.node=CafeOptionalEvents.Lines(r,r.progress.branch_id).Length;CafeOptionalEvents.Finish(r);b.RememberViewedConversations();
        }
        public static void Run()
        {
            string workspace=Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT");fixtures=Path.Combine(workspace,"Logs/OptionalEventsF2/fixtures");
            output=Path.Combine(workspace,"Logs/OptionalEventsF2/Editor-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(output);checks=0;
            string root=Path.Combine(Application.dataPath,"Resources/CatCafeEvents"),pointer=Path.Combine(root,"catalog.json");byte[] before=File.Exists(pointer)?File.ReadAllBytes(pointer):null;
            string imagePointer=Path.Combine(Application.dataPath,"Resources/CatCafeContent/catalog.json");byte[] imageBefore=File.ReadAllBytes(imagePointer);
            var owned=new[]{new string('1',32),new string('2',32),new string('3',32)}.Where(id=>!Directory.Exists(Path.Combine(root,"Imports",id))).ToArray();
            string fullFolder=Environment.GetEnvironmentVariable("CATCAFE_OPTIONAL_FULL_PACK");
            string fullId=string.IsNullOrEmpty(fullFolder)?null:JsonUtility.FromJson<ContentBundle>(File.ReadAllText(Path.Combine(fullFolder,"bundle.json"))).pack_id;
            string fullTarget=fullId==null?null:Path.Combine(Application.dataPath,"Resources/CatCafeContent/Imports",fullId);
            bool removeFull=fullTarget!=null && !Directory.Exists(fullTarget);if(fullId!=null && !Directory.Exists(Path.Combine(root,"Imports",fullId)))owned=owned.Concat(new[]{fullId}).ToArray();
            bool instant=CafeOptions.Current.instant;CafeOptions.Current.instant=true;
            try
            {
                ContentPreviewSetup.Import(Path.Combine(fixtures,"first"));var first=CafeOptionalEventResources.Load();Check(first.events.Length==1,"Event-only import active");
                Check(imageBefore.SequenceEqual(File.ReadAllBytes(imagePointer)),"No image pack copy or pointer change");
                var b=StandaloneOptionalEventSmoke.Create(Path.Combine(output,"save"));Prepare(b);
                EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var p=UnityEngine.Object.FindObjectOfType<ContentPreview>();p.Initialize();typeof(PythonStateClient).GetField("preview",Hidden).SetValue(p.GetComponent<PythonStateClient>(),p);Apply(p,b);
                var view=p.GetComponent<OptionalEventConversationPanel>()??p.gameObject.AddComponent<OptionalEventConversationPanel>();Check(view.OpenEvent(),"Authored definition starts through existing UI");
                var old=b.OptionalEvent;Check(old.revision==first.events[0].revision && old.title==first.events[0].title,"Authored revision copied");Capture(p,"original-1280",1280,800);view.Close();
                old.progress.node=1;old.progress.page_offset=4;string frozen=JsonUtility.ToJson(old);Good(b,"save_game");
                ContentPreviewSetup.Import(Path.Combine(fixtures,"second"));var second=CafeOptionalEventResources.Load();Check(second.events[0].revision!=old.revision,"Rewrite becomes active new revision");
                var restarted=StandaloneOptionalEventSmoke.Create(Path.Combine(output,"save"));Good(restarted,"load_game");Apply(p,restarted);
                Check(JsonUtility.ToJson(restarted.OptionalEvent)==frozen,"Old save restores original prose and position after catalogue update");Check(view.OpenEvent(),"Old prose resumes");Capture(p,"old-resume-1024",1024,768);view.Close();Finish(restarted,true);
                string oldKey=restarted.OptionalEvent.progress.conversation_id+"/accepted";Good(restarted,"save_game");
                Good(restarted,"new_game","free");Prepare(restarted);Apply(p,restarted);Check(view.OpenEvent(),"Fresh game starts current definition");
                Check(restarted.OptionalEvent.title==second.events[0].title && restarted.OptionalEvent.introduction[0].text==second.events[0].introduction[0].text,"New start uses edited prose");Capture(p,"rewritten-1280",1280,800);view.Close();restarted.OptionalEvent.progress.node=1;restarted.OptionalEvent.progress.page_offset=4;Good(restarted,"save_game");Finish(restarted,false);
                var archive=restarted.Replays.Load();Check(archive.entries.Any(e=>e.Key==oldKey) && archive.entries.Count(e=>CafeReplayCatalog.IsOptional(e.conversation_id))==2,"Old and rewritten Extra entries retained");
                Check(restarted.OptionalReplayDefinitions.Any(d=>d.Key==oldKey),"Historical Extra remains listed");
                ContentPreviewSetup.Import(Path.Combine(fixtures,"disabled"));Check(CafeOptionalEventResources.Load().events.Length==0,"Explicit disabled definition does not fall back");
                var removed=StandaloneOptionalEventSmoke.Create(Path.Combine(output,"save"));Good(removed,"load_game");Check(removed.OptionalEvent!=null && !removed.OptionalEvent.progress.completed,"Definition removal does not discard saved receipt");Apply(p,removed);Check(view.OpenEvent(),"Removed definition still resumes its frozen story");view.Close();
                Good(removed,"new_game","free");Prepare(removed);Check(!removed.OptionalEventListed,"Disabled catalogue prevents new starts");
                string validPointer=File.ReadAllText(pointer);var invalid=Path.Combine(output,"invalid");Directory.CreateDirectory(invalid);
                var tampered=first.Copy();tampered.events[0].title="改変";tampered.events[0].revision=tampered.events[0].Digest();File.WriteAllText(Path.Combine(invalid,"optional_events.json"),JsonUtility.ToJson(tampered,true));
                bool rejected=false;try{ContentPreviewSetup.Import(invalid);}catch(ArgumentException){rejected=true;}Check(rejected && File.ReadAllText(pointer)==validPointer,"Same pack ID rewrite rejected without changing active pointer");
                var wrong=second.Copy();wrong.pack_id=new string('6',32);wrong.events[0].revision="invalid";File.WriteAllText(Path.Combine(invalid,"optional_events.json"),JsonUtility.ToJson(wrong,true));rejected=false;try{ContentPreviewSetup.Import(invalid);}catch(ArgumentException){rejected=true;}Check(rejected && File.ReadAllText(pointer)==validPointer,"Invalid prose revision rejected before activation");
                Check(imageBefore.SequenceEqual(File.ReadAllBytes(imagePointer)),"All event-only operations preserve image pointer");
                EditorSceneManager.OpenScene("Assets/Scenes/CatCafeTitle.unity");CafeBackends.UseLocal(removed);var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();title.Apply(removed.View());
                var extra=title.GetComponent<TitleExtraPanel>()??title.gameObject.AddComponent<TitleExtraPanel>();extra.Initialize(title);extra.Open();
                var extraList=(UnityEngine.UI.ScrollRect)typeof(TitleExtraPanel).GetField("list",Hidden).GetValue(extra);Canvas.ForceUpdateCanvases();
                var oldButton=extraList.content.Find(oldKey.Replace('/','_')).GetComponent<UnityEngine.UI.Button>();Check(oldButton.interactable && oldButton.GetComponent<RectTransform>().rect.width>100,"Historical Extra revision has a visible usable row");
                var third=second.Copy();third.events[0].title="三つ目の版";third.events[0].revision=third.events[0].Digest();removed.SetOptionalCatalog(third);extra.RefreshList();Canvas.ForceUpdateCanvases();
                var added=extraList.content.Find((third.events[0].event_id+".v"+third.events[0].revision+"/accepted").Replace('/','_')).GetComponent<UnityEngine.UI.Button>();Check(!added.interactable && added.GetComponent<RectTransform>().rect.width>100,"New revision row added dynamically without unlocking unread content");
                Check(oldButton.GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("以前の版"),"Historical revision is identified in Extra");Check(added.GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("現行版"),"Current revision is identified in Extra");
                extra.OpenReplay(oldKey);Check(extra.Replay.Showing,"Historical replay opens after definition removal");extra.Replay.Close();
                if(fullFolder!=null)
                {
                    ContentPreviewSetup.Import(fullFolder);Check(CafeOptionalEventResources.Load().pack_id==fullId,"Full content bundle activates its included event definition");
                    string importedPointer=File.ReadAllText(pointer);ContentPreviewSetup.Import(fullFolder);Check(File.ReadAllText(pointer)==importedPointer,"Full content bundle reimport is immutable and idempotent");
                }
                File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" optional authoring checks. Tool JSON -> immutable import -> original save -> edited catalogue -> original resume/new start -> both Extra revisions -> disabled catalogue. Isolated rules, no production saves.");Debug.Log("CAT_CAFE_OPTIONAL_AUTHORING_OK: "+checks);
            }
            finally
            {
                if(before!=null)File.WriteAllBytes(pointer,before);else if(File.Exists(pointer))File.Delete(pointer);
                foreach(string id in owned){string target=Path.GetFullPath(Path.Combine(root,"Imports",id));string allowed=Path.GetFullPath(Path.Combine(root,"Imports"))+Path.DirectorySeparatorChar;if(!target.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new Exception("Unexpected verification path");if(Directory.Exists(target))Directory.Delete(target,true);if(File.Exists(target+".meta"))File.Delete(target+".meta");}
                File.WriteAllBytes(imagePointer,imageBefore);
                if(removeFull){string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"Resources/CatCafeContent/Imports"))+Path.DirectorySeparatorChar;string target=Path.GetFullPath(fullTarget);if(!target.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new Exception("Unexpected full-import probe path");if(Directory.Exists(target))Directory.Delete(target,true);if(File.Exists(target+".meta"))File.Delete(target+".meta");}
                CafeOptions.Current.instant=instant;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }
        public static void ActivateAdopted()
        {
            string folder=Environment.GetEnvironmentVariable("CATCAFE_OPTIONAL_PACK");if(string.IsNullOrEmpty(folder))throw new ArgumentException("CATCAFE_OPTIONAL_PACKでToolから出力したイベントフォルダを指定してください。");
            ContentPreviewSetup.Import(folder);var catalog=CafeOptionalEventResources.Load();if(catalog.events.Length!=1)throw new Exception("No adopted Mike event");Debug.Log("CAT_CAFE_OPTIONAL_ADOPTED_OK: "+catalog.events[0].revision);
        }
    }
}
