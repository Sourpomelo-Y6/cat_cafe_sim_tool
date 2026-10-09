using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonPersistVerification
    {
        const string Key="CatCafePersistTest";static PythonPersistVerification(){EditorApplication.update+=Update;}
        public static void Run(){int port=int.Parse(Environment.GetEnvironmentVariable("CAT_CAFE_PERSIST_TEST_PORT"));if(port<=0 || port==8190)throw new Exception("Use an isolated server");EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=port;serialized.ApplyModifiedPropertiesWithoutUndo();SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+120);EditorApplication.EnterPlaymode();}
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Persist Play verification timed out at "+stage);if(!EditorApplication.isPlaying)return;var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();if(client?.CurrentState==null)return;var p=client.GetComponent<PythonPersistPanel>();var state=client.CurrentState;
                if(stage==1 && client.CanRetryPersist){if(!p.panel.activeSelf || client.CanProgressBusiness || !p.details.text.Contains("親しみ"))throw new Exception("Pending screen missing");SessionState.SetFloat(Key+"funds",state.funds);SessionState.SetInt(Key+"tick",state.tick);SessionState.SetInt(Key+"day",state.day);p.Close();if(!p.activity.activeSelf)throw new Exception("Recovery entry missing");p.activityDetails.onClick.Invoke();p.retry.onClick.Invoke();SessionState.SetInt(Key,2);}
                else if(stage==2 && client.CanRetryPersist && state.pending_results.reason=="検証用：保存先を使用できません。") {if(!p.details.text.Contains("保存先を使用できません") || client.CanManageSaves)throw new Exception("Retry failure missing");Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{client.GetComponent<ContentPreview>(),"Logs/PendingResultsLive.png"});p.retry.onClick.Invoke();SessionState.SetInt(Key,3);}
                else if(stage==3 && !client.HasPendingResults && client.CanManageSaves){if(p.activity.activeSelf || p.retry.interactable || state.funds!=SessionState.GetFloat(Key+"funds",0) || state.tick!=SessionState.GetInt(Key+"tick",0) || state.day!=SessionState.GetInt(Key+"day",0))throw new Exception("Retry replayed business rewards or remained blocked");Debug.Log("CAT_CAFE_PERSIST_PLAY_OK: restored pending / retry failure reason / UI retry / recovered / no business replay");SessionState.SetInt(Key,0);EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(0);}
            }
            catch(Exception error){SessionState.SetInt(Key,0);Debug.LogException(error);EditorApplication.Exit(1);}
        }
    }
}
