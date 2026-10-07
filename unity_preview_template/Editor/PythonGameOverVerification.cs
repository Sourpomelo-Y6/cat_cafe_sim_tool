using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonGameOverVerification
    {
        const string Key="CatCafeEndedPlay";
        static PythonGameOverVerification(){EditorApplication.update+=Update;}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafeTitle.unity");UnityEngine.Object.FindObjectOfType<PythonTitleScreen>().port=8191;EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=8191;serialized.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+120);EditorApplication.EnterPlaymode();
        }
        static void Check(PythonStateClient client)
        {
            var panel=client.GetComponent<PythonGameOverPanel>();
            if(client.CurrentState.game_over==null || !panel.panel.activeSelf || !panel.details.text.Contains("資金不足") || client.CanChangeShifts || client.CanProgressBusiness || client.Automatic)throw new Exception("Ended result mismatch");
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Ended flow timeout: "+stage);
                if(!EditorApplication.isPlaying)return;
                var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();
                if(stage==1 && client!=null && client.CanManageSaves)
                {
                    Check(client);SessionState.SetInt(Key+"count",client.GetComponent<PythonSavePanel>().Count);client.GetComponent<PythonGameOverPanel>().save.onClick.Invoke();
                    if(client.GetComponent<PythonGameOverPanel>().save.interactable || client.GetComponent<PythonGameOverPanel>().title.interactable)throw new Exception("Pending result guard failed");SessionState.SetInt(Key,2);
                }
                else if(stage==2 && client!=null && client.CanManageSaves && client.GetComponent<PythonSavePanel>().Count>SessionState.GetInt(Key+"count",0))
                {Check(client);client.GetComponent<PythonGameOverPanel>().title.onClick.Invoke();SessionState.SetInt(Key,3);}
                else if(stage==3 && title!=null && title.continueGame.interactable)
                {title.continueGame.onClick.Invoke();title.saves.value=0;title.resume.onClick.Invoke();SessionState.SetInt(Key,4);}
                else if(stage==4 && client!=null && client.CanManageSaves)
                {
                    Check(client);Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{client.GetComponent<ContentPreview>(),"Logs/GameOverResult.png"});
                    client.GetComponent<PythonGameOverPanel>().title.onClick.Invoke();SessionState.SetInt(Key,5);
                }
                else if(stage==5 && title!=null && title.newGame.interactable)
                {title.newGame.onClick.Invoke();title.objectives.value=3;title.SelectObjective(3);title.startGame.onClick.Invoke();SessionState.SetInt(Key,6);}
                else if(stage==6 && client!=null && client.CanManageSaves)
                {
                    if(client.CurrentState.game_over!=null || client.GetComponent<PythonGameOverPanel>().panel.activeSelf || !client.CanChangeShifts)throw new Exception("New game did not clear result");
                    SessionState.SetInt(Key,0);Debug.Log("CAT_CAFE_GAME_OVER_PLAY_OK: save / title / resume ended / new game");EditorApplication.Exit(0);
                }
            }
            catch(Exception ex){SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
