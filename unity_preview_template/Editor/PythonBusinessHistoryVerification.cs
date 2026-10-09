using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonBusinessHistoryVerification
    {
        const string Key="CatCafeBusinessHistoryPlay";
        static PythonBusinessHistoryVerification(){EditorApplication.update+=Update;}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=int.Parse(Environment.GetEnvironmentVariable("CAT_CAFE_BUSINESS_HISTORY_TEST_PORT"));serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+120);EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Business history timeout "+stage);
                if(!EditorApplication.isPlaying)return;var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();if(client==null || !client.CanManageSaves)return;
                var state=client.CurrentState;var preview=client.GetComponent<ContentPreview>();var menu=client.GetComponent<PythonMenuPanel>();var p=client.GetComponent<PythonFinanceHistoryPanel>();var saves=client.GetComponent<PythonSavePanel>();preview.RefreshPythonControls();
                if(stage==1)
                {
                    if(state.business_history?.rows?.Length!=2)throw new Exception("Expected two closed days");
                    SessionState.SetInt(Key+"revision",state.revision);menu.open.onClick.Invoke();menu.entries[15].onClick.Invoke();
                    if(menu.panel.activeSelf || !p.panel.activeSelf || !p.title.text.Contains("営業成果") || !p.details.text.Contains("休業日"))throw new Exception("Live history route failed");
                    p.previous.onClick.Invoke();if(!p.details.text.Contains("営業日") || !p.details.text.Contains("接客評価") || !p.details.text.Contains("満員"))throw new Exception("Live daily metrics missing");
                    p.next.onClick.Invoke();if(!p.details.text.Contains("前日との差") || state.revision!=SessionState.GetInt(Key+"revision",0))throw new Exception("History browsing mutated game");
                    SessionState.SetString(Key+"details",p.details.text);p.close.onClick.Invoke();saves.save.onClick.Invoke();SessionState.SetInt(Key,2);
                }
                else if(stage==2)
                {
                    if(saves.Count==0)return;saves.saves.SetValueWithoutNotify(0);saves.ResumeSelected();SessionState.SetInt(Key,3);
                }
                else if(stage==3)
                {
                    menu.open.onClick.Invoke();menu.entries[15].onClick.Invoke();if(p.details.text!=SessionState.GetString(Key+"details",""))throw new Exception("History save resume mismatch");
                    Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/BusinessHistoryLive.png"});
                    Debug.Log("CAT_CAFE_BUSINESS_HISTORY_PLAY_OK: menu / daily metrics / previous and next / read only / save and HTTP resume");
                    SessionState.SetInt(Key,0);EditorApplication.Exit(0);
                }
            }
            catch(Exception ex){SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
