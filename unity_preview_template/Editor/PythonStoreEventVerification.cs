using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad]
    public static class PythonStoreEventVerification
    {
        const string Key="CatCafeStoreTest";
        static PythonStoreEventVerification() { EditorApplication.update+=Update; }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>(); var serialized=new SerializedObject(client);
            serialized.FindProperty("port").intValue=8191; serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key,1); SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+100); EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0); if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Store test timeout stage "+stage);
                if(!EditorApplication.isPlaying)return;
                var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); var client=preview.GetComponent<PythonStateClient>();
                if(!client.CanManageSaves)return;preview.RefreshPythonControls();
                var state=client.CurrentState; var panel=preview.GetComponent<PythonStoreEventPanel>(); var saves=preview.GetComponent<PythonSavePanel>();
                if(stage==1)
                {
                    if(state.store_event==null || !panel.panel.activeSelf || !panel.choices[1].interactable || client.CanProgressBusiness || client.CanStartAutomatic)throw new Exception("Pending event guard failed");
                    preview.GetSceneButton(3).onClick.Invoke(); SessionState.SetInt(Key,2);
                }
                else if(stage==2 && saves.Count==1)
                { panel.choices[1].onClick.Invoke(); if(panel.choices[0].interactable)throw new Exception("Busy event guard failed"); SessionState.SetInt(Key,3); }
                else if(stage==3 && state.store_event==null && client.CanProgressBusiness)
                {
                    if(panel.panel.activeSelf || !client.CanProgressBusiness || client.Automatic)throw new Exception("Resolved event did not unblock manual start");
                    saves.saves.SetValueWithoutNotify(0); saves.resume.onClick.Invoke(); SessionState.SetInt(Key,4);
                }
                else if(stage==4 && state.store_event!=null && panel.choices[0].interactable)
                { if(!string.IsNullOrEmpty(preview.GetComponent<PythonEventPresentation>().Message))throw new Exception("Loading event save reported a response");if(!panel.panel.activeSelf || !panel.choices[0].interactable)throw new Exception("Restored event UI failed"); panel.choices[0].onClick.Invoke(); SessionState.SetInt(Key,5); }
                else if(stage==5 && state.store_event==null && client.CanProgressBusiness)
                { preview.GetSceneButton(2).onClick.Invoke(); SessionState.SetInt(Key,6); }
                else if(stage==6 && state.tick>0)
                {
                    if(!client.CanStartAutomatic || client.Automatic)throw new Exception("Manual restart guard failed");
                    preview.GetComponent<PythonAutoPanel>().toggle.onClick.Invoke(); SessionState.SetInt(Key,7);
                }
                else if(stage==7 && state.tick>=2)
                {
                    Debug.Log("CAT_CAFE_STORE_PLAY_OK: event / save / answer button / busy / restore / manual opening / automatic restart");
                    SessionState.SetInt(Key,0); EditorApplication.ExitPlaymode(); EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
                }
            }
            catch(Exception ex) { SessionState.SetInt(Key,0); Debug.LogException(ex); EditorApplication.Exit(1); }
        }
    }
}
