using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor
{
    public static class DispatchConversationVerification
    {
        [Serializable] class Report{public bool success;public int checks;public string failure;public string[] errors;}
        public static void VerifyAndBuild()
        {
            ConversationRendererVerification.Run();Run();StandaloneScreenSetup.Build();
        }
        public static void Run()
        {
            string workspace=Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT");string folder=Path.Combine(workspace,"Logs/StandaloneDispatchConversation/Editor");Directory.CreateDirectory(folder);int checks=0;string failure=null;var errors=new List<string>();
            Application.LogCallback log=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(message+"\n"+stack);};Application.logMessageReceived+=log;
            Action<bool,string> check=(ok,label)=>{checks++;if(!ok)throw new Exception(label);};
            try
            {
                checks+=StandaloneDispatchConversationSmoke.BackendChecks(Path.Combine(folder,"cases"),false);
                check(StandaloneDispatchSmoke.Run(Path.Combine(folder,"existing-rules"))>0,"Existing dispatch rules regression");
                EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var hidden=BindingFlags.NonPublic|BindingFlags.Instance;typeof(PythonStateClient).GetField("preview",hidden).SetValue(client,preview);
                var b=new LocalCafeBackend(Path.Combine(folder,"ui"));CafeBackends.UseLocal(b);Action apply=()=>{typeof(PythonStateClient).GetMethod("Accept",hidden).Invoke(client,new object[]{b.View()});Canvas.ForceUpdateCanvases();};apply();var owner=preview.GetComponent<PythonGeneralDispatchPanel>();var ui=preview.GetComponent<DispatchConversationPanel>();owner.OpenOption("cat-tama",CafeDispatchDialogue.Destination);owner.Send();check(ui.Showing,"Departure not shown");
                Action renderers=()=>{foreach(var graphic in ui.Panel.GetComponentsInChildren<Graphic>(true))check(graphic.GetComponent<CanvasRenderer>()!=null,"Missing renderer: "+graphic.name);};renderers();
                b.DispatchConversation.Current.node=4;apply();check(ui.Panel.GetComponentsInChildren<Button>(true).Single(v=>v.name=="迎える").interactable,"Departure not eligible");for(int i=0;i<3;i++){ui.Close();owner.Send();renderers();}
                StandaloneDispatchConversationSmoke.Good(b,"dispatch_general",CafeDispatchDialogue.Destination);apply();check(b.DispatchResultPending,"Departure result missing");b.DispatchConversation.Current.node=3;CafeDispatchDialogue.Finish(b.DispatchConversation);ui.Close();apply();
                owner.OpenReturn(b.DispatchConversation.event_id);check(ui.Showing && b.DispatchConversation.stage=="travelling","Travel not shown");ui.Close();StandaloneDispatchConversationSmoke.Good(b,"dispatch_general",CafeDispatchDialogue.Destination,"cat-kohaku");StandaloneDispatchConversationSmoke.Good(b,"day_off");apply();check(ui.Showing && b.Simulation.DispatchWaiting,"Return not shown");renderers();
                var raycaster=ui.Panel.GetComponentInParent<GraphicRaycaster>();raycaster.Raycast(new PointerEventData(UnityEngine.Object.FindObjectOfType<EventSystem>()){position=new Vector2(Screen.width*.5f,Screen.height*.5f)},new List<RaycastResult>());
                string first=b.DispatchConversation.event_id;StandaloneDispatchConversationSmoke.Good(b,"receive_general",first,b.DispatchConversation.cat_id);apply();b.DispatchConversation.Current.node=3;CafeDispatchDialogue.Finish(b.DispatchConversation);ui.Close();apply();check(b.Simulation.DispatchWaiting && !client.CanDayOff,"First result incorrectly released another return");check(b.DispatchConversation.event_id!=first,"Second return not selected");StandaloneDispatchConversationSmoke.Good(b,"receive_general",b.DispatchConversation.event_id,b.DispatchConversation.cat_id);apply();b.DispatchConversation.Current.node=3;CafeDispatchDialogue.Finish(b.DispatchConversation);ui.Close();apply();check(!b.DispatchResultPending && !b.Simulation.DispatchWaiting,"All returns failed to release");
                var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.intake.day=2;
                b=new LocalCafeBackend(Path.Combine(folder,"overlap"),rules);CafeBackends.UseLocal(b);StandaloneDispatchConversationSmoke.Good(b,"dispatch_general",CafeDispatchDialogue.Destination);StandaloneDispatchConversationSmoke.Good(b,"day_off");apply();
                check(preview.GetComponents<AcquisitionConversationPanel>().Count(p=>p.Showing)==1 && !owner.panel.activeSelf,"Overlapping intake and return panels");
                ui.GetComponent<PythonEventListPanel>().ShowMenu(10);check(ui.Showing && !owner.panel.activeSelf,"Event list raised legacy return over ADV");
                StandaloneDispatchConversationSmoke.Good(b,"receive_general",b.DispatchConversation.event_id);apply();b.DispatchConversation.Current.node=3;CafeDispatchDialogue.Finish(b.DispatchConversation);ui.Close();apply();check(b.Simulation.IntakeWaiting && !client.CanDayOff,"Return completion released pending intake");
                check(errors.Count==0,"Unity errors");
            }
            catch(Exception ex){failure=ex.ToString();}
            finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Application.logMessageReceived-=log;}
            if(errors.Count>0 && failure==null)failure="Unity UI errors";File.WriteAllText(Path.Combine(folder,"result.json"),JsonUtility.ToJson(new Report{success=failure==null,checks=checks,failure=failure,errors=errors.ToArray()},true));if(failure!=null)throw new Exception(failure);Debug.Log("CAT_CAFE_DISPATCH_CONVERSATION_OK: "+checks);
        }
    }
}
