using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor
{
    public static class FacilityTroublePortraitVerification
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static int checks;
        static string output;
        static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        static void Bind(ContentPreview preview,LocalCafeBackend b){CafeBackends.UseLocal(b);typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(preview.GetComponent<PythonStateClient>(),new object[]{b.View()});}
        static void Capture(ContentPreview preview,string name,int width=1280,int height=800){typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{preview,Path.Combine(output,name+".png"),width,height});}
        static void Portrait(AcquisitionConversationPanel view,bool visible)
        {
            var image=(RawImage)typeof(AcquisitionConversationPanel).GetField("portrait",Hidden).GetValue(view);
            Check(view.Showing,"conversation visible");Check(image.transform.parent.parent.gameObject.activeSelf==visible,"portrait frame visibility");
            Check(image.enabled==visible && (image.texture!=null)==visible,"portrait texture and graphic visibility");
        }
        static LocalCafeBackend Backend(string kind)
        {
            var b=new LocalCafeBackend(Path.Combine(output,kind));int seed=Enumerable.Range(0,10000).First(i=>new CafeSimulation(b.Simulation.Rules,i).CurrentStoreEvent.type==kind);
            b.Simulation.Restore(new CafeSimulation(b.Simulation.Rules,seed).State);b.PrepareEquipmentConversation();return b;
        }
        static void Command(LocalCafeBackend b,string kind,string choice=null)
        {
            var s=b.Simulation.State;CafeResponse reply=null;
            var run=b.Request("/commands",JsonUtility.ToJson(new PythonShiftCommand{kind=kind,choice=choice,event_id=CafeEquipmentDialogue.EventId(s.day,b.Simulation.CurrentStoreEvent.type),instance_id=s.instance_id,expected_revision=s.revision,request_id=Guid.NewGuid().ToString("N"),save_id="1",save_slot=1,working_cats=new string[0]}),r=>reply=r);while(run.MoveNext()){}
            Check(reply!=null && reply.result==UnityEngine.Networking.UnityWebRequest.Result.Success,kind+": "+reply?.error);
        }
        public static void Run()
        {
            output=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/FacilityTroublePortrait");Directory.CreateDirectory(output);checks=0;
            int regression=StandaloneEquipmentConversationSmoke.Run(Path.Combine(output,"regression"));
            bool oldInstant=CafeOptions.Current.instant;CafeOptions.Current.instant=true;
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();typeof(PythonStateClient).GetField("preview",Hidden).SetValue(preview.GetComponent<PythonStateClient>(),preview);
            var b=Backend("trouble");Bind(preview,b);var view=preview.GetComponent<EquipmentConversationPanel>()??preview.gameObject.AddComponent<EquipmentConversationPanel>();var owner=preview.GetComponent<PythonStoreEventPanel>();view.Apply(b.View(),owner);view.Open();Portrait(view,false);
            Capture(preview,"trouble-introduction-1280");Capture(preview,"trouble-introduction-1024",1024,768);
            b.EquipmentConversation.progress.node=CafeEquipmentDialogue.Lines(b.EquipmentConversation,"introduction").Length;view.Apply(b.View(),owner);view.Open();Portrait(view,false);Check(view.PlaybackChoosing,"trouble choices visible");Capture(preview,"trouble-choices");
            Command(b,"resolve_store_event","patch");Bind(preview,b);view.Apply(b.View(),owner);view.Open();Portrait(view,false);Capture(preview,"trouble-result");Command(b,"save_game");Command(b,"load_game");Bind(preview,b);view.Apply(b.View(),owner);view.Open();Portrait(view,false);Check(b.EquipmentConversation.progress.branch_id=="patch","result resume kept branch");view.Close();
            var support=Backend("support");Bind(preview,support);view.Apply(support.View(),owner);view.Open();Portrait(view,true);Capture(preview,"support-unchanged");view.Close();
            var surface=new GameObject("R1 replay verification",typeof(RectTransform),typeof(Image));surface.transform.SetParent(owner.panel.transform.parent,false);surface.SetActive(false);
            var replay=preview.gameObject.AddComponent<ReplayConversationPanel>();
            Action<string,string,bool> play=(id,cat,visible)=>{replay.Play(new CafeReplayEntry{conversation_id=id,branch_id="patch",title="表示確認",cat_id=cat,cat_name="確認用",personality="検証",lines=new[]{new CafeConversationLine("あなた","施設トラブルの表示を確認します。")}},surface,owner.title.font,()=>surface.SetActive(false));Portrait(replay,visible);};
            play(CafeEquipmentDialogue.Id,"",false);Capture(preview,"trouble-extra");replay.Close();
            play(CafeEquipmentDialogue.SupportId,"",true);replay.Close();play("intake.v1","cat-mike",true);Capture(preview,"cat-unchanged");replay.Close();
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" R1 UI checks; "+regression+" existing equipment checks. Trouble introduction/choices/result/save-resume/Extra hide portrait and frame; support and cat portrait remain. 1280x800 and 1024x768 rendered. No gameplay rule or save schema change. No build.");CafeOptions.Current.instant=oldInstant;Debug.Log("CAT_CAFE_R1_PORTRAIT_OK: "+checks+" / "+regression);
        }
    }
}
