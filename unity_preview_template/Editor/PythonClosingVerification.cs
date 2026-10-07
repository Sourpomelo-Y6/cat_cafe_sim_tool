using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonClosingVerification
    {
        const string Key="CatCafeClosingPlay";
        static PythonClosingVerification(){EditorApplication.update+=Update;}
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
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Closing flow timeout: "+stage);
                if(!EditorApplication.isPlaying)return;var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();if(client==null || !client.CanManageSaves)return;
                var panel=client.GetComponent<PythonClosingPanel>();var state=client.CurrentState;
                if(stage==1)
                {
                    if(!state.closed || !panel.panel.activeSelf || !panel.next.interactable || state.finance==null || !panel.details.text.Contains(state.finance.closing_funds.ToString("0.#")))throw new Exception("Live closing result missing");
                    SessionState.SetInt(Key+"day",state.day);SessionState.SetFloat(Key+"funds",state.funds);SessionState.SetInt(Key+"count",client.GetComponent<PythonSavePanel>().Count);
                    panel.close.onClick.Invoke();if(panel.panel.activeSelf)throw new Exception("Closing close failed");panel.open.onClick.Invoke();if(!panel.panel.activeSelf)throw new Exception("Closing reopen failed");panel.save.onClick.Invoke();
                    if(panel.next.interactable || panel.save.interactable)throw new Exception("Pending save guards failed");SessionState.SetInt(Key,2);
                }
                else if(stage==2 && client.GetComponent<PythonSavePanel>().Count>SessionState.GetInt(Key+"count",0))
                {
                    if(!panel.notice.text.Contains("保存"))throw new Exception("Saved notice missing");Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{client.GetComponent<ContentPreview>(),"Logs/ClosingLive.png"});
                    panel.next.onClick.Invoke();if(panel.next.interactable)throw new Exception("Pending next day guard failed");SessionState.SetInt(Key,3);
                }
                else if(stage==3 && state.day>SessionState.GetInt(Key+"day",0))
                {
                    if(state.closed || !state.can_set_shifts || panel.panel.activeSelf || panel.open.gameObject.activeSelf)throw new Exception("Next preparation failed");
                    var saves=client.GetComponent<PythonSavePanel>();saves.saves.value=0;saves.resume.onClick.Invoke();SessionState.SetInt(Key,4);
                }
                else if(stage==4 && state.closed)
                {
                    if(state.day!=SessionState.GetInt(Key+"day",0) || state.funds!=SessionState.GetFloat(Key+"funds",0) || !panel.panel.activeSelf)throw new Exception("Saved closing restore failed");
                    SessionState.SetInt(Key,0);Debug.Log("CAT_CAFE_CLOSING_PLAY_OK: review / close / reopen / save / next preparation / load closed save");EditorApplication.Exit(0);
                }
            }
            catch(Exception ex){SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
