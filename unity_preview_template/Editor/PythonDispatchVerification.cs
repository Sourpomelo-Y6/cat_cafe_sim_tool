using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonDispatchVerification
    {
        const string Key="CatCafeDispatchPlay";
        static PythonDispatchVerification() { EditorApplication.update+=Update; }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=8191;serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+240);EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Dispatch timeout stage "+stage);
                if(!EditorApplication.isPlaying)return;
                var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var client=preview.GetComponent<PythonStateClient>();if(!client.CanManageSaves)return;
                preview.RefreshPythonControls();var state=client.CurrentState;var panel=preview.GetComponent<PythonDispatchPanel>();var detail=preview.GetComponent<PythonCatDetailsPanel>();var saves=preview.GetComponent<PythonSavePanel>();
                Action<int> move=value=>SessionState.SetInt(Key,value);
                if(stage==1)
                {
                    detail.Open("cat-sora");detail.dispatch.onClick.Invoke();if(!panel.panel.activeSelf || !panel.send.interactable)throw new Exception("Dispatch entry failed");
                    Capture(preview,"Logs/DispatchLiveTerms.png");panel.send.onClick.Invoke();if(panel.send.interactable)throw new Exception("Busy departure enabled");move(2);
                }
                else if(stage==2 && state.patron_dispatch.events.Length==1)
                {
                    var ev=state.patron_dispatch.events[0];if(ev.status!="travelling" || ev.remaining!=2 || panel.receive.interactable || Array.Find(state.cats,r=>r.cat_id=="cat-sora").activity!="dispatched")throw new Exception("Departure state mismatch");
                    SessionState.SetString(Key+"travel",JsonUtility.ToJson(state.patron_dispatch));preview.GetSceneButton(3).onClick.Invoke();move(3);
                }
                else if(stage==3 && saves.Count==1) { saves.saves.value=0;saves.resume.onClick.Invoke();move(4); }
                else if(stage==4)
                {
                    if(JsonUtility.ToJson(state.patron_dispatch)!=SessionState.GetString(Key+"travel",""))throw new Exception("Travel save mismatch");panel.Close();move(5);
                }
                else if(stage==5)
                {
                    if(state.patron_dispatch.events[0].status=="waiting")
                    {
                        if(!panel.panel.activeSelf || !panel.receive.interactable || panel.close.interactable)throw new Exception("Automatic return display failed");
                        Capture(preview,"Logs/DispatchLiveReturn.png");SessionState.SetFloat(Key+"funds",state.funds);panel.receive.onClick.Invoke();if(panel.receive.interactable)throw new Exception("Busy reward enabled");move(6);
                    }
                    else if(state.growth_choices!=null && state.growth_choices.Length>0)client.RespondGrowth(0);
                    else if(client.CanProgressBusiness)client.ProgressBusiness();
                }
                else if(stage==6 && state.patron_dispatch.events[0].status=="resolved")
                {
                    if(state.funds!=SessionState.GetFloat(Key+"funds",0)+220 || panel.receive.interactable || state.goal_result==null || state.goal_result.kind!="continue_patron")throw new Exception("Reward or satisfaction clear mismatch");
                    Capture(preview,"Logs/DispatchLiveResult.png");preview.GetSceneButton(3).onClick.Invoke();move(7);
                }
                else if(stage==7 && saves.Count==2) { saves.saves.value=1;saves.resume.onClick.Invoke();move(8); }
                else if(stage==8 && state.patron_dispatch.events[0].status=="travelling") { saves.saves.value=0;saves.resume.onClick.Invoke();move(9); }
                else if(stage==9 && state.patron_dispatch.events[0].status=="resolved")
                {
                    if(panel.receive.interactable || state.goal_result==null)throw new Exception("Resolved save mismatch");panel.Close();
                    var goal=preview.GetComponent<PythonGoalPanel>();if(!goal.panel.activeSelf || !goal.resume.interactable)throw new Exception("Patron goal hidden");goal.resume.onClick.Invoke();move(10);
                }
                else if(stage==10 && state.goal_result==null)
                { Debug.Log("CAT_CAFE_DISPATCH_PLAY_OK: departure / busy / travelling save / two closing days / return / reward / satisfaction / clear / result save / continue");SessionState.SetInt(Key,0);EditorApplication.Exit(0); }
            }
            catch(Exception ex) { SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1); }
        }
        static void Capture(ContentPreview preview,string path)
        { Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,path}); }
    }
}
