using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonClearResultsVerification
    {
        const string Key="CatCafeClearResultsPlay";
        static PythonClearResultsVerification(){EditorApplication.update+=Update;UnityEngine.SceneManagement.SceneManager.sceneLoaded+=Loaded;}
        static void Loaded(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if(SessionState.GetInt(Key,0)==0)return;
            int port=int.Parse(Environment.GetEnvironmentVariable("CAT_CAFE_CLEAR_TEST_PORT"));
            var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();if(title!=null)title.port=port;
            var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();if(client!=null){var serialized=new SerializedObject(client);serialized.FindProperty("port").intValue=port;serialized.ApplyModifiedPropertiesWithoutUndo();}
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());
            serialized.FindProperty("port").intValue=int.Parse(Environment.GetEnvironmentVariable("CAT_CAFE_CLEAR_TEST_PORT"));serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+120);EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Clear records play timed out: "+stage);
                if(!EditorApplication.isPlaying)return;
                var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();
                if(stage==4)
                {
                    if(title==null)return;title.continueGame.onClick.Invoke();if(!title.resume.interactable)return;
                    title.saves.SetValueWithoutNotify(0);title.resume.onClick.Invoke();SessionState.SetInt(Key,5);return;
                }
                var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();if(client==null || client.CurrentState==null || !client.CanManageSaves)return;
                var state=client.CurrentState;var p=client.GetComponent<PythonClearResultsPanel>();var menu=client.GetComponent<PythonMenuPanel>();var saves=client.GetComponent<PythonSavePanel>();client.GetComponent<ContentPreview>().RefreshPythonControls();
                if(stage==1)
                {
                    if(state.clear_results?.rows==null || state.clear_results.rows.Length!=1 || !client.CanContinueGoal)throw new Exception("Cleared fixture missing");
                    menu.open.onClick.Invoke();menu.entries[14].onClick.Invoke();
                    if(!p.panel.activeSelf || !p.details.text.Contains("猫との好感度") || menu.panel.activeSelf)throw new Exception("Live menu route failed");
                    SessionState.SetString(Key+"details",p.details.text);p.close.onClick.Invoke();
                    client.GetComponent<PythonGoalPanel>().resume.onClick.Invoke();SessionState.SetInt(Key,2);
                }
                else if(stage==2)
                {
                    if(state.goal_result!=null)return;menu.open.onClick.Invoke();menu.entries[14].onClick.Invoke();
                    if(p.details.text!=SessionState.GetString(Key+"details","") || !p.notice.text.Contains("確認済み"))throw new Exception("Continuation changed clear record");
                    p.close.onClick.Invoke();saves.save.onClick.Invoke();SessionState.SetInt(Key,3);
                }
                else if(stage==3)
                {
                    if(saves.Count==0)return;
                    client.GetComponent<PythonReturnToTitle>().Return();SessionState.SetInt(Key,4);
                }
                else if(stage==5)
                {
                    if(title!=null)return;
                    menu.open.onClick.Invoke();menu.entries[14].onClick.Invoke();
                    if(p.details.text!=SessionState.GetString(Key+"details","") || state.goal_result!=null)throw new Exception("Title save resume lost clear record");
                    Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{client.GetComponent<ContentPreview>(),"Logs/ClearResultsLive.png"});
                    Debug.Log("CAT_CAFE_CLEAR_RESULTS_PLAY_OK: menu buttons / captured bond / continue / save / title resume / frozen record");
                    SessionState.SetInt(Key,0);EditorApplication.Exit(0);
                }
            }
            catch(Exception ex){SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
