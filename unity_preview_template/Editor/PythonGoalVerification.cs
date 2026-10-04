using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatCafePreview.Editor
{
    [InitializeOnLoad]
    public static class PythonGoalVerification
    {
        const string Key="CatCafeGoalTest";
        static PythonGoalVerification() { EditorApplication.update+=Update; }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>(); var serialized=new SerializedObject(client);
            serialized.FindProperty("port").intValue=8191; serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key,1); SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+120); EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0); if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Goal test timed out, stage "+stage);
                if(!EditorApplication.isPlaying)return;
                var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); var client=preview.GetComponent<PythonStateClient>();
                if(!client.CanManageSaves)return;
                var state=client.CurrentState; var panel=preview.GetComponent<PythonGoalPanel>(); var saves=preview.GetComponent<PythonSavePanel>();
                if(stage==1)
                {
                    if(state.goal_result==null || !panel.panel.activeSelf || !panel.resume.interactable || client.CanProgressBusiness)throw new Exception("Pending result mismatch");
                    SessionState.SetString(Key+"pending",JsonUtility.ToJson(state)); preview.GetSceneButton(3).onClick.Invoke(); SessionState.SetInt(Key,2);
                }
                else if(stage==2 && saves.Count==1)
                { panel.resume.onClick.Invoke(); if(panel.resume.interactable)throw new Exception("Goal busy guard failed"); SessionState.SetInt(Key,3); }
                else if(stage==3 && state.goal_result==null)
                {
                    if(panel.panel.activeSelf || !client.CanProgressBusiness)throw new Exception("Goal continuation did not unblock next day");
                    SessionState.SetString(Key+"confirmed",JsonUtility.ToJson(state)); preview.GetSceneButton(3).onClick.Invoke(); SessionState.SetInt(Key,4);
                }
                else if(stage==4 && saves.Count==2) { preview.GetSceneButton(2).onClick.Invoke(); SessionState.SetInt(Key,5); }
                else if(stage==5 && state.day==2)
                { saves.saves.SetValueWithoutNotify(1); saves.resume.onClick.Invoke(); SessionState.SetInt(Key,6); }
                else if(stage==6 && state.goal_result!=null && panel.resume.interactable)
                { CheckRestored("pending",state); panel.resume.onClick.Invoke(); SessionState.SetInt(Key,7); }
                else if(stage==7 && state.goal_result==null)
                { saves.saves.SetValueWithoutNotify(0); saves.resume.onClick.Invoke(); SessionState.SetInt(Key,8); }
                else if(stage==8 && state.goal_result==null && state.day==1 && client.CanProgressBusiness)
                {
                    CheckRestored("confirmed",state);
                    Debug.Log("CAT_CAFE_GOAL_PLAY_OK: pending results / continue button / busy guard / next day / pending and confirmed saves restored");
                    SessionState.SetInt(Key,0); EditorApplication.ExitPlaymode(); EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
                }
            }
            catch(Exception ex) { SessionState.SetInt(Key,0); Debug.LogException(ex); EditorApplication.Exit(1); }
        }
        static void CheckRestored(string name,PythonCafeState state)
        {
            var expected=JsonUtility.FromJson<PythonCafeState>(SessionState.GetString(Key+name,"")); expected.revision=state.revision;
            if(expected.goal_result!=null && string.IsNullOrEmpty(expected.goal_result.status))expected.goal_result=null;
            if(JsonUtility.ToJson(expected)!=JsonUtility.ToJson(state))throw new Exception("Goal restore mismatch: "+name);
        }
    }
}
