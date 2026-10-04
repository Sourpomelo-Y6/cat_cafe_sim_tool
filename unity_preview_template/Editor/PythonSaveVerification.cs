using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatCafePreview.Editor
{
    [InitializeOnLoad]
    public static class PythonSaveVerification
    {
        const string Key="CatCafeSaveTest";
        static PythonSaveVerification() { EditorApplication.update+=Update; }
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
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Save UI test timed out");
                if(!EditorApplication.isPlaying)return;
                var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); var client=preview.GetComponent<PythonStateClient>(); var panel=preview.GetComponent<PythonSavePanel>();
                if(!client.CanManageSaves)return;
                if(stage==1) { preview.GetSceneButton(2).onClick.Invoke(); SessionState.SetInt(Key,2); }
                else if(stage==2 && client.CurrentState.tick==1) { preview.GetSceneButton(2).onClick.Invoke(); SessionState.SetInt(Key,3); }
                else if(stage==3 && client.CurrentState.tick==2)
                {
                    SessionState.SetString(Key+"snapshot",JsonUtility.ToJson(client.CurrentState));
                    preview.GetSceneButton(3).onClick.Invoke(); if(preview.GetSceneButton(3).interactable)throw new Exception("Save busy guard failed"); SessionState.SetInt(Key,4);
                }
                else if(stage==4 && panel.Count>0) { preview.GetSceneButton(2).onClick.Invoke(); SessionState.SetInt(Key,5); }
                else if(stage==5 && client.CurrentState.tick==3) { panel.saves.SetValueWithoutNotify(0); panel.resume.onClick.Invoke(); SessionState.SetInt(Key,6); }
                else if(stage==6 && client.CurrentState.tick==2)
                {
                    var expected=JsonUtility.FromJson<PythonCafeState>(SessionState.GetString(Key+"snapshot","")); expected.revision=client.CurrentState.revision;
                    if(JsonUtility.ToJson(expected)!=JsonUtility.ToJson(client.CurrentState))throw new Exception("Active save resume mismatch");
                    SessionState.SetInt(Key,7);
                }
                else if(stage==7)
                {
                    if(!client.CurrentState.closed)preview.GetSceneButton(2).onClick.Invoke();
                    else { preview.GetSceneButton(3).onClick.Invoke(); SessionState.SetInt(Key,8); }
                }
                else if(stage==8 && panel.Count>=2)
                {
                    Debug.Log("CAT_CAFE_SAVE_PLAY_OK: real save button / saved game list / resume active seats and stamina / closed save / busy guard");
                    SessionState.SetInt(Key,0); EditorApplication.ExitPlaymode(); EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
                }
            }
            catch(Exception ex) { SessionState.SetInt(Key,0); Debug.LogException(ex); EditorApplication.Exit(1); }
        }
    }
}
