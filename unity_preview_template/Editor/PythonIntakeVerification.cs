using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatCafePreview.Editor
{
    [InitializeOnLoad]
    public static class PythonIntakeVerification
    {
        const string Key="CatCafeIntakeTest";
        static PythonIntakeVerification() { EditorApplication.update+=Update; }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>(); var serialized=new SerializedObject(client);
            serialized.FindProperty("port").intValue=8191; serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key,1); SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+120);
            EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0); if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Intake UI test timed out, stage "+stage);
                if(!EditorApplication.isPlaying)return;
                var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); var client=preview.GetComponent<PythonStateClient>();
                var panel=preview.GetComponent<PythonIntakePanel>(); var saves=preview.GetComponent<PythonSavePanel>();
                if(!client.CanManageSaves)return;
                var state=client.CurrentState; var intake=state.intake_request;
                if(stage==1)
                {
                    if(intake==null || intake.status!="waiting" || !panel.panel.activeSelf || !panel.accept.interactable || !panel.decline.interactable || client.CanProgressBusiness)
                        throw new Exception("Waiting intake UI mismatch");
                    SessionState.SetFloat(Key+"funds",state.funds); SessionState.SetInt(Key+"cats",state.cats.Length);
                    preview.GetSceneButton(3).onClick.Invoke(); SessionState.SetInt(Key,2);
                }
                else if(stage==2 && saves.Count==1)
                {
                    panel.accept.onClick.Invoke();
                    if(panel.accept.interactable || panel.decline.interactable)throw new Exception("Intake busy guard failed");
                    SessionState.SetInt(Key,3);
                }
                else if(stage==3 && intake.status=="accepted")
                {
                    var cat=Array.Find(state.cats,item=>item.cat_id==intake.cat_id);
                    if(panel.panel.activeSelf || state.cats.Length!=SessionState.GetInt(Key+"cats",0)+1 || cat==null || cat.working || cat.stamina!=cat.max_stamina
                        || state.funds!=SessionState.GetFloat(Key+"funds",0)-intake.cost || !client.CanProgressBusiness
                        || preview.GetComponent<PythonCatRoster>().Count!=state.cats.Length)throw new Exception("Accepted cat roster mismatch");
                    SessionState.SetString(Key+"accepted",JsonUtility.ToJson(state)); preview.GetSceneButton(3).onClick.Invoke(); SessionState.SetInt(Key,4);
                }
                else if(stage==4 && saves.Count==2)
                { saves.saves.SetValueWithoutNotify(1); saves.resume.onClick.Invoke(); SessionState.SetInt(Key,5); }
                else if(stage==5 && intake.status=="waiting")
                { panel.decline.onClick.Invoke(); SessionState.SetInt(Key,6); }
                else if(stage==6 && intake.status=="declined")
                {
                    if(panel.panel.activeSelf || state.cats.Length!=SessionState.GetInt(Key+"cats",0) || state.funds!=SessionState.GetFloat(Key+"funds",0) || !client.CanProgressBusiness)
                        throw new Exception("Decline state mismatch");
                    SessionState.SetString(Key+"declined",JsonUtility.ToJson(state)); preview.GetSceneButton(3).onClick.Invoke(); SessionState.SetInt(Key,7);
                }
                else if(stage==7 && saves.Count==3)
                { preview.GetSceneButton(2).onClick.Invoke(); SessionState.SetInt(Key,8); }
                else if(stage==8 && state.tick>0)
                { saves.saves.SetValueWithoutNotify(0); saves.resume.onClick.Invoke(); SessionState.SetInt(Key,9); }
                else if(stage==9 && intake.status=="declined" && state.tick==0)
                {
                    CheckRestored("declined",state); saves.saves.SetValueWithoutNotify(1); saves.resume.onClick.Invoke(); SessionState.SetInt(Key,10);
                }
                else if(stage==10 && intake.status=="accepted")
                {
                    CheckRestored("accepted",state);
                    Debug.Log("CAT_CAFE_INTAKE_PLAY_OK: waiting panel / real accept and decline / busy guard / cat registration / funds / answered saves and resume / business unblocked");
                    SessionState.SetInt(Key,0); EditorApplication.ExitPlaymode(); EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
                }
            }
            catch(Exception ex) { SessionState.SetInt(Key,0); Debug.LogException(ex); EditorApplication.Exit(1); }
        }
        static void CheckRestored(string name,PythonCafeState state)
        {
            var expected=JsonUtility.FromJson<PythonCafeState>(SessionState.GetString(Key+name,"")); expected.revision=state.revision;
            if(JsonUtility.ToJson(expected)!=JsonUtility.ToJson(state))throw new Exception("Intake save resume mismatch: "+name);
        }
    }
}
