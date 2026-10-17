using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CatCafePreview.Editor
{
    public static class ConversationRendererVerification
    {
        [Serializable] class Report { public bool success;public int checks;public string failure;public string[] errors; }
        static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static int checks;
        static void Check(bool valid,string label){checks++;if(!valid)throw new Exception(label);}
        static void Send(LocalCafeBackend backend,string kind,string choice=null)
        {
            var state=backend.Simulation.State;CafeResponse reply=null;
            var command=new PythonShiftCommand{kind=kind,choice=choice,save_slot=1,save_id="1",request_id=Guid.NewGuid().ToString("N"),instance_id=state.instance_id,expected_revision=state.revision,working_cats=new string[0]};
            var routine=backend.Request("/commands",JsonUtility.ToJson(command),r=>reply=r);while(routine.MoveNext()){}
            Check(reply!=null && reply.result==UnityWebRequest.Result.Success,kind+": "+reply?.text);
        }
        static void Renderers(GameObject root)
        {
            foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))
                Check(graphic.GetComponent<CanvasRenderer>()!=null,"Missing CanvasRenderer: "+graphic.name);
        }
        static void Apply(ContentPreview preview,LocalCafeBackend backend)
        {
            typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(preview.GetComponent<PythonStateClient>(),new object[]{backend.View()});
            Canvas.ForceUpdateCanvases();
        }
        static void Day(LocalCafeBackend backend)
        {
            if(backend.Simulation.StoreEventWaiting)Send(backend,"resolve_store_event",backend.Simulation.CurrentStoreEvent.type=="trouble"?"patch":"decline");
            Send(backend,"day_off");
        }
        public static void Run()
        {
            string workspace=Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT");if(string.IsNullOrEmpty(workspace))throw new Exception("CATCAFE_WORKSPACE_ROOT is required");
            string folder=Path.Combine(workspace,"Logs/StandaloneConversation/RendererFix/Editor");Directory.CreateDirectory(folder);
            checks=0;var errors=new List<string>();string failure=null;
            Application.LogCallback log=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(message+"\n"+stack);};
            Application.logMessageReceived+=log;
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
                var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();
                typeof(PythonStateClient).GetField("preview",Hidden).SetValue(preview.GetComponent<PythonStateClient>(),preview);
                var backend=new LocalCafeBackend(Path.Combine(folder,"saves"));CafeBackends.UseLocal(backend);
                Apply(preview,backend);var ui=preview.GetComponent<IntakeConversationPanel>();var owner=preview.GetComponent<PythonIntakePanel>();
                Check(!owner.panel.activeSelf,"Conversation must be constructed while hidden on day one");
                while(backend.Simulation.State.day<4){if(backend.Simulation.State.day==3)Send(backend,"save_game");Day(backend);Apply(preview,backend);}
                Check(ui.Showing && backend.Simulation.IntakeWaiting,"Day-four conversation did not open");Renderers(owner.panel);
                for(int i=0;i<3;i++){ui.Close();ui.Open();Renderers(owner.panel);}
                var events=UnityEngine.Object.FindObjectOfType<EventSystem>();var results=new List<RaycastResult>();
                foreach(var raycaster in preview.GetComponentsInChildren<GraphicRaycaster>())raycaster.Raycast(new PointerEventData(events){position=new Vector2(Screen.width*.5f,Screen.height*.5f)},results);
                Check(errors.Count==0,"Editor emitted UI errors during enable, disable or raycast");
                backend.Conversation.node=5;Apply(preview,backend);Send(backend,"resolve_intake","accept");Apply(preview,backend);Send(backend,"save_game");Send(backend,"load_game");Apply(preview,backend);Renderers(owner.panel);
                // A new backend and a saved third day exercise the late activation path again.
                var waiting=new LocalCafeBackend(Path.Combine(folder,"third-day"));CafeBackends.UseLocal(waiting);
                while(waiting.Simulation.State.day<3)Day(waiting);Send(waiting,"save_game");Day(waiting);Apply(preview,waiting);Send(waiting,"load_game");Apply(preview,waiting);Day(waiting);Apply(preview,waiting);Renderers(owner.panel);
                var inactive=new GameObject("Inactive custom graphic test");inactive.SetActive(false);
                inactive.AddComponent<ConversationShape>();Check(inactive.GetComponent<CanvasRenderer>()!=null,"Shape requirement missing on inactive object");UnityEngine.Object.DestroyImmediate(inactive);
                inactive=new GameObject("Inactive room test");inactive.SetActive(false);inactive.AddComponent<ConversationRoomGraphic>();Check(inactive.GetComponent<CanvasRenderer>()!=null,"Room requirement missing on inactive object");UnityEngine.Object.DestroyImmediate(inactive);
            }
            catch(Exception ex){failure=ex.ToString();}
            finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Application.logMessageReceived-=log;}
            if(errors.Count>0 && failure==null)failure="UI errors were logged";
            File.WriteAllText(Path.Combine(folder,"result.json"),JsonUtility.ToJson(new Report{success=failure==null,checks=checks,failure=failure,errors=errors.ToArray()},true));
            if(failure!=null)throw new Exception(failure);
            Debug.Log("CAT_CAFE_CONVERSATION_RENDERERS_OK: "+checks+" checks; hidden construction / day 4 / raycast / reopen / saves; zero errors");
        }
        public static void VerifyAndBuild(){Run();StandaloneScreenSetup.Build();}
    }
}
