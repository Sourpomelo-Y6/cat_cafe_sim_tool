using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonPreparationVerification
    {
        const string Key="CatCafePreparationPlay";
        static PythonPreparationVerification(){EditorApplication.update+=Update;}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=8191;serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+90);EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Preparation flow timeout: "+stage);
                if(!EditorApplication.isPlaying)return;var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();if(client==null || !client.CanManageSaves)return;
                var panel=client.GetComponent<PythonPreparationPanel>();var preview=client.GetComponent<ContentPreview>();preview.RefreshPythonControls();
                if(stage==1)
                {
                    if(!panel.panel.activeSelf || !panel.notice.text.Contains("出勤する猫がいません") || client.CanProgressBusiness)throw new Exception("Initial preparation advice failed");
                    panel.close.onClick.Invoke();if(panel.panel.activeSelf)throw new Exception("Close failed");panel.open.onClick.Invoke();if(!panel.panel.activeSelf)throw new Exception("Reopen failed");
                    Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/PreparationLive.png"});
                    panel.cats.value=1;if(!panel.work.interactable)throw new Exception("Preparation cat cannot work");panel.work.onClick.Invoke();
                    if(panel.start.interactable || panel.work.interactable || panel.cats.interactable)throw new Exception("Preparation pending guards failed");SessionState.SetInt(Key,2);
                }
                else if(stage==2 && client.CurrentState.cats[1].working)
                {
                    if(!panel.panel.activeSelf || !panel.start.interactable || panel.cats.value!=1 || !panel.summary.text.Contains("出勤 1匹") || !panel.selectedDetails.text.Contains(client.CurrentState.cats[1].name))throw new Exception("Preparation shift selection mismatch: visible="+panel.panel.activeSelf+" start="+panel.start.interactable+" selection="+panel.cats.value+" summary="+panel.summary.text+" selected="+panel.selectedDetails.text);
                    if(!panel.notice.text.Contains("営業開始") || !panel.rest.interactable)throw new Exception("Ready guidance missing");panel.rest.onClick.Invoke();SessionState.SetInt(Key,5);
                }
                else if(stage==5 && !client.CurrentState.cats[1].working)
                {
                    if(panel.start.interactable || !panel.summary.text.Contains("出勤 0匹"))throw new Exception("Rest did not update start condition");panel.work.onClick.Invoke();SessionState.SetInt(Key,6);
                }
                else if(stage==6 && client.CurrentState.cats[1].working)
                {
                    Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/PreparationReady.png"});
                    panel.start.onClick.Invoke();if(panel.start.interactable || panel.work.interactable)throw new Exception("Start pending guard failed");SessionState.SetInt(Key,3);
                }
                else if(stage==3 && !client.CurrentState.can_set_shifts)
                {
                    if(panel.panel.activeSelf || panel.open.gameObject.activeSelf || !client.CanStartAutomatic)throw new Exception("Guide remained during service");client.GetComponent<PythonAutoPanel>().toggle.onClick.Invoke();SessionState.SetInt(Key,4);
                }
                else if(stage==4 && client.CurrentState.tick>0)
                {
                    client.GetComponent<PythonAutoPanel>().toggle.onClick.Invoke();if(client.Automatic)throw new Exception("Automatic stop failed");SessionState.SetInt(Key,0);Debug.Log("CAT_CAFE_PREPARATION_PLAY_OK: guide / close / reopen / select / work / rest / busy / start / automatic tick / stop");EditorApplication.Exit(0);
                }
            }
            catch(Exception ex){SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
