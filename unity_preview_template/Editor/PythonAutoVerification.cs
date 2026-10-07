using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatCafePreview.Editor
{
    [InitializeOnLoad]
    public static class PythonAutoVerification
    {
        const string Key="CatCafeAutoTest";
        static PythonAutoVerification() { EditorApplication.update+=Update; }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>(); var serialized=new SerializedObject(client);
            serialized.FindProperty("port").intValue=8191; serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key,1); SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+150); EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0); if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Auto test timed out, stage "+stage);
                if(!EditorApplication.isPlaying)return;
                var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); var client=preview.GetComponent<PythonStateClient>();
                var state=client.CurrentState; var panel=preview.GetComponent<PythonAutoPanel>(); var saves=preview.GetComponent<PythonSavePanel>();
                if(stage==11)
                {
                    if(!client.Automatic)
                    {
                        Debug.Log("CAT_CAFE_AUTO_PLAY_OK: manual opening / normal delay / stop / fast mode / save pause and load stop / automatic closing / manual next day / network error stop");
                        SessionState.SetInt(Key,0); EditorApplication.ExitPlaymode(); EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
                    }
                    return;
                }
                if(!client.CanManageSaves)return;preview.RefreshPythonControls();
                if(stage==1)
                {
                    if(panel.toggle.interactable)throw new Exception("Auto enabled before opening");
                    preview.GetSceneButton(2).onClick.Invoke(); SessionState.SetInt(Key,2);
                }
                else if(stage==2 && state.tick==1)
                { panel.toggle.onClick.Invoke(); SessionState.SetFloat(Key+"started",Time.realtimeSinceStartup); SessionState.SetInt(Key,3); }
                else if(stage==3 && state.tick>=2)
                {
                    if(Time.realtimeSinceStartup-SessionState.GetFloat(Key+"started",0)<.95f)throw new Exception("Normal speed advanced too early");
                    var service=preview.GetComponent<PythonServiceScreen>();if(!service.serviceSummary.text.Contains("進行中") || !panel.toggle.GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("停止"))throw new Exception("Live automatic status missing");
                    Canvas.ForceUpdateCanvases();if(!service.detailText.text.Contains("来店客一覧"))throw new Exception("Live customer list missing");typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/ServiceLive.png"});
                    panel.toggle.onClick.Invoke(); SessionState.SetInt(Key+"stoppedTick",state.tick); SessionState.SetFloat(Key+"stopped",Time.realtimeSinceStartup); SessionState.SetInt(Key,4);
                }
                else if(stage==4 && Time.realtimeSinceStartup-SessionState.GetFloat(Key+"stopped",0)>2.5f)
                {
                    if(state.tick!=SessionState.GetInt(Key+"stoppedTick",0) || client.Automatic)throw new Exception("Stop did not hold tick");
                    panel.speed.onClick.Invoke(); if(!client.Fast || !preview.GetComponent<PythonServiceScreen>().serviceSummary.text.Contains("0.25秒"))throw new Exception("Fast speed did not select");
                    panel.toggle.onClick.Invoke(); preview.GetSceneButton(3).onClick.Invoke(); SessionState.SetInt(Key,5);
                }
                else if(stage==5 && saves.Count==1 && state.tick>SessionState.GetInt(Key+"stoppedTick",0))
                { saves.saves.SetValueWithoutNotify(0); saves.resume.onClick.Invoke(); SessionState.SetInt(Key,6); }
                else if(stage==6 && state.tick==SessionState.GetInt(Key+"stoppedTick",0))
                {
                    if(client.Automatic)throw new Exception("Loading did not stop automatic progress");
                    panel.toggle.onClick.Invoke(); SessionState.SetInt(Key,7);
                }
                else if(stage==7 && state.closed)
                {
                    if(client.Automatic || state.day!=1 || state.tick!=state.opening_ticks)throw new Exception("Automatic passed closing");
                    SessionState.SetFloat(Key+"closed",Time.realtimeSinceStartup); SessionState.SetInt(Key,8);
                }
                else if(stage==8 && Time.realtimeSinceStartup-SessionState.GetFloat(Key+"closed",0)>1.5f)
                { if(state.day!=1)throw new Exception("Automatic moved to next day"); preview.GetSceneButton(2).onClick.Invoke(); SessionState.SetInt(Key,9); }
                else if(stage==9 && state.day==2 && state.can_set_shifts)
                { if(client.Automatic)throw new Exception("Automatic restarted on next day"); preview.GetSceneButton(2).onClick.Invoke(); SessionState.SetInt(Key,10); }
                else if(stage==10 && state.day==2 && state.tick>0)
                {
                    panel.toggle.onClick.Invoke(); var serialized=new SerializedObject(client); serialized.FindProperty("port").intValue=0; serialized.ApplyModifiedPropertiesWithoutUndo(); SessionState.SetInt(Key,11);
                }
            }
            catch(Exception ex) { SessionState.SetInt(Key,0); Debug.LogException(ex); EditorApplication.Exit(1); }
        }
    }
}
