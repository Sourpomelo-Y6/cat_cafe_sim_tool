using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonEventLayoutVerification
    {
        const string Key="CatCafeEventLayoutPlay";
        static PythonEventLayoutVerification(){EditorApplication.update+=Update;}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=8191;serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+120);EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Event layout timeout stage "+stage);
                if(!EditorApplication.isPlaying)return;var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();if(client==null || !client.CanManageSaves)return;
                var preview=client.GetComponent<ContentPreview>();preview.RefreshPythonControls();var state=client.CurrentState;
                var intake=client.GetComponent<PythonIntakePanel>();var growth=client.GetComponent<PythonGrowthPanel>();var store=client.GetComponent<PythonStoreEventPanel>();
                if(stage==1)
                {
                    if(state.intake_request?.status!="waiting" || state.store_event==null || state.growth_choices==null || state.growth_choices.Length==0 || client.CanProgressBusiness || client.Automatic)throw new Exception("Initial overlapping events missing");
                    if(intake.decline.interactable || !intake.choiceDetails[1].text.Contains(state.intake_request.response_reason))throw new Exception("Blocked intake reason missing");
                    SessionState.SetInt(Key+"intake",0);SessionState.SetInt(Key+"growth",0);SessionState.SetInt(Key+"store",0);SessionState.SetInt(Key,2);
                }
                else if(stage==2)
                {
                    if(state.intake_request?.status=="waiting" && intake.decline.interactable)
                    {
                        Front(intake.panel);Capture(preview,"Logs/EventIntakeLive.png");intake.decline.onClick.Invoke();if(intake.decline.interactable || !intake.notice.text.Contains("送信"))throw new Exception("Intake busy guard failed");SessionState.SetInt(Key+"intake",SessionState.GetInt(Key+"intake",0)+1);
                    }
                    else if(state.growth_choices!=null && state.growth_choices.Length>0 && Array.Exists(state.growth_choices[0].choices,row=>row.can_select))
                    {
                        Front(growth.panel);int index=Array.FindIndex(state.growth_choices[0].choices,row=>row.can_select);Capture(preview,"Logs/EventGrowthLive.png");growth.choices[index].onClick.Invoke();if(growth.choices[index].interactable || !growth.notice.text.Contains("送信"))throw new Exception("Growth busy guard failed");SessionState.SetInt(Key+"growth",SessionState.GetInt(Key+"growth",0)+1);
                    }
                    else if(state.store_event!=null && Array.Exists(state.store_event.choices,row=>row.can_select && row.choice!="close"))
                    {
                        Front(store.panel);int index=Array.FindIndex(state.store_event.choices,row=>row.can_select && row.choice!="close");if(string.IsNullOrEmpty(store.choiceDetails[index].text))throw new Exception("Live choice description missing");Capture(preview,"Logs/EventStoreLive.png");store.choices[index].onClick.Invoke();if(store.choices[index].interactable || !store.notice.text.Contains("送信"))throw new Exception("Store busy guard failed");SessionState.SetInt(Key+"store",SessionState.GetInt(Key+"store",0)+1);
                    }
                    else if(state.intake_request?.status!="waiting" && state.store_event==null && (state.growth_choices==null || state.growth_choices.Length==0))SessionState.SetInt(Key,4);
                    else throw new Exception("Overlapping events have no available response");
                }
                else if(stage==4 && state.store_event==null)
                {
                    if(SessionState.GetInt(Key+"intake",0)<1 || SessionState.GetInt(Key+"growth",0)<1 || SessionState.GetInt(Key+"store",0)<1)throw new Exception("Some event responses not exercised");
                    if(intake.panel.activeSelf || store.panel.activeSelf || growth.panel.activeSelf || !client.CanProgressBusiness || client.Automatic || string.IsNullOrEmpty(client.GetComponent<PythonEventPresentation>().Message))throw new Exception("Answered events did not unblock preparation");
                    preview.GetSceneButton(2).onClick.Invoke();SessionState.SetInt(Key,5);
                }
                else if(stage==5 && !state.can_set_shifts && state.tick>0)
                {
                    if(!client.GetComponent<PythonServiceScreen>().ShowingSeats || !client.CanStartAutomatic || client.Automatic)throw new Exception("Manual service return failed");
                    client.GetComponent<PythonAutoPanel>().toggle.onClick.Invoke();if(!client.Automatic)throw new Exception("Manual automatic restart failed");client.GetComponent<PythonAutoPanel>().toggle.onClick.Invoke();
                    SessionState.SetInt(Key,0);Debug.Log("CAT_CAFE_EVENT_LAYOUT_PLAY_OK: overlapping events / priority / intake / growth / store / busy / feedback / manual opening / service return / manual restart");EditorApplication.Exit(0);
                }
            }
            catch(Exception ex){SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
        static void Front(GameObject panel)
        { if(!panel.activeSelf || panel.transform.GetSiblingIndex()!=panel.transform.parent.childCount-1)throw new Exception("Available event not in front: "+panel.name); }
        static void Capture(ContentPreview preview,string path)
        { Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,path}); }
    }
}
