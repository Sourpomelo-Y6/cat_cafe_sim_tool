using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonAutoPlayVerification
    {
        const string Key="CatCafeBatchPlayTest";
        static PythonAutoPlayVerification(){EditorApplication.update+=Update;}
        public static void Run()
        {
            int port=int.Parse(Environment.GetEnvironmentVariable("CAT_CAFE_AUTOPLAY_TEST_PORT"));if(port<=0 || port==8190)throw new Exception("Use an isolated test server port");EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=port;serialized.ApplyModifiedPropertiesWithoutUndo();SessionState.SetInt(Key+"port",port);SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+150);EditorApplication.EnterPlaymode();
        }
        static void Capture(ContentPreview p,string path){Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{p,path});}
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Auto play HTTP test timed out at "+stage);
                if(!EditorApplication.isPlaying)return;var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();if(client==null || client.CurrentState==null)return;var state=client.CurrentState;var p=client.GetComponent<PythonAutoPlayPanel>();var preview=client.GetComponent<ContentPreview>();var saves=client.GetComponent<PythonSavePanel>();var report=state.autoplay?.report;preview.RefreshPythonControls();
                if(stage==1 && client.CanManageSaves)
                {if(report!=null || p.save.interactable)throw new Exception("JSON null report became a result");p.Open();p.SelectDays(9);p.Choose();p.Confirm();SessionState.SetInt(Key,2);Debug.Log("AUTOPLAY_PLAY_STAGE: started");}
                else if(stage==2 && client.AutoPlayRunning && report.operations>0)
                {p.Stop();SessionState.SetInt(Key,3);}
                else if(stage==3 && client.CanManageSaves && !client.AutoPlayRunning)
                {if(report.reason!="cancelled" || report.operations<1 || state.service_assignment.auto_assign)throw new Exception("Cancel or assignment restore failed");SessionState.SetString(Key+"report",report.run_id);p.Save();SessionState.SetInt(Key,4);}
                else if(stage==4 && client.CanManageSaves && saves.Count==1)
                {saves.saves.SetValueWithoutNotify(0);saves.ResumeSelected();SessionState.SetInt(Key,5);}
                else if(stage==5 && client.CanManageSaves && report?.run_id==SessionState.GetString(Key+"report",""))
                {if(client.BatchAutomatic || !p.details.text.Contains("中断"))throw new Exception("Save/load result lost or resumed");p.Open();p.SelectMode(1);p.SelectDays(0);p.Choose();p.Confirm();SessionState.SetInt(Key,6);}
                else if(stage==6 && client.CanManageSaves && !client.AutoPlayRunning && report?.run_id!=SessionState.GetString(Key+"report","") && report?.reason=="day_limit")
                {if(report.days!=1 || client.BatchAutomatic || !p.save.interactable)throw new Exception("Day limit did not stop: days="+report.days+" automatic="+client.BatchAutomatic+" save="+p.save.interactable);Capture(preview,"Logs/AutoPlayLiveResult.png");p.SelectMode(0);p.SelectDays(9);p.Choose();p.Confirm();SessionState.SetInt(Key,7);}
                else if(stage==7 && client.AutoPlayRunning && report.operations>0)
                {var serialized=new SerializedObject(client);serialized.FindProperty("port").intValue=0;serialized.ApplyModifiedPropertiesWithoutUndo();SessionState.SetInt(Key,8);}
                else if(stage==8 && !client.CanSendCommands && !client.BatchAutomatic)
                {var serialized=new SerializedObject(client);serialized.FindProperty("port").intValue=SessionState.GetInt(Key+"port",0);serialized.ApplyModifiedPropertiesWithoutUndo();SessionState.SetInt(Key,9);}
                else if(stage==9 && client.CanSendCommands && client.AutoPlayRunning)
                {if(client.BatchAutomatic || !p.proceed.interactable)throw new Exception("Reconnection resumed automatically");SessionState.SetInt(Key+"held",report.operations);SessionState.SetFloat(Key+"heldAt",Time.realtimeSinceStartup);SessionState.SetInt(Key,10);}
                else if(stage==10 && client.CanSendCommands && Time.realtimeSinceStartup-SessionState.GetFloat(Key+"heldAt",0)>2.5f)
                {if(report.operations!=SessionState.GetInt(Key+"held",0))throw new Exception("Disconnected run kept progressing");p.Choose();SessionState.SetInt(Key,11);}
                else if(stage==11 && client.AutoPlayRunning && report.operations>SessionState.GetInt(Key+"held",0))
                {p.Close();SessionState.SetInt(Key,12);}
                else if(stage==12 && client.CanManageSaves && !client.AutoPlayRunning)
                {if(p.panel.activeSelf || report.reason!="cancelled")throw new Exception("Return to normal did not stop");Debug.Log("CAT_CAFE_AUTOPLAY_PLAY_OK: HTTP start / deferred cancel / manual mode restore / save and load report / one-day limit / network pause / explicit resume / close stops run");SessionState.SetInt(Key,0);EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(0);}
            }
            catch(Exception error){SessionState.SetInt(Key,0);Debug.LogException(error);EditorApplication.Exit(1);}
        }
    }
}
