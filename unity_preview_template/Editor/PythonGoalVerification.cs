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
        public static void RunStages()
        {
            Run();SessionState.SetInt(Key,100);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+240);
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
                preview.RefreshPythonControls();
                if(stage>=100) { VerifyStages(stage,preview,client,panel,saves,state); return; }
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
            if(expected.next_goal!=null && expected.next_goal.stage<2)expected.next_goal=null;
            if(JsonUtility.ToJson(expected)!=JsonUtility.ToJson(state))throw new Exception("Goal restore mismatch: "+name);
        }
        static void VerifyStages(int stage,ContentPreview preview,PythonStateClient client,PythonGoalPanel panel,PythonSavePanel saves,PythonCafeState state)
        {
            Action<int> move=value=>SessionState.SetInt(Key,value);
            if(stage==100)
            {
                if(state.goal_result==null || state.goal_result.stage!=1 || state.next_goal==null || state.next_goal.stage!=2 || !panel.advance.interactable)throw new Exception("Initial stage result missing");
                preview.GetSceneButton(3).onClick.Invoke();move(101);
            }
            else if(stage==101 && saves.Count==1)
            {
                panel.advance.onClick.Invoke();if(panel.advance.interactable || panel.resume.interactable)throw new Exception("Advance busy guard failed");move(102);
            }
            else if(stage==102 && state.goal_result==null)
            {
                if(panel.panel.activeSelf || state.next_goal!=null || !state.objective_progress.summary.Contains("第2段階"))throw new Exception("Second stage did not start");
                SessionState.SetString(Key+"active",JsonUtility.ToJson(state));preview.GetSceneButton(3).onClick.Invoke();move(103);
            }
            else if(stage==103 && saves.Count==2) { saves.saves.SetValueWithoutNotify(1);saves.resume.onClick.Invoke();move(104); }
            else if(stage==104 && state.goal_result!=null)
            {
                if(state.goal_result.stage!=1 || !panel.advance.interactable)throw new Exception("Pending result restore failed");
                panel.advance.onClick.Invoke();move(105);
            }
            else if(stage==105 && state.goal_result==null) { saves.saves.SetValueWithoutNotify(0);saves.resume.onClick.Invoke();move(106); }
            else if(stage==106 && state.goal_result==null)
            { CheckRestored("active",state);preview.GetSceneButton(2).onClick.Invoke();move(107); }
            else if(stage==107 && state.day==2 && state.can_set_shifts) { preview.GetSceneButton(2).onClick.Invoke();move(108); }
            else if(stage==108)
            {
                if(!state.closed) { preview.GetSceneButton(2).onClick.Invoke();return; }
                if(state.goal_result==null || state.goal_result.stage!=2 || state.next_goal==null || state.next_goal.stage!=3)throw new Exception("Second stage did not clear");
                panel.resume.onClick.Invoke();move(109);
            }
            else if(stage==109 && state.goal_result==null)
            {
                if(!panel.openNext.gameObject.activeSelf)throw new Exception("Later challenge entry missing");
                panel.openNext.onClick.Invoke();if(!panel.panel.activeSelf || !panel.details.text.Contains("115"))throw new Exception("Third stage preview missing");
                Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/ThirdGoalPreview.png"});
                panel.advance.onClick.Invoke();move(110);
            }
            else if(stage==110 && state.goal_result==null && state.next_goal==null) { preview.GetSceneButton(2).onClick.Invoke();move(111); }
            else if(stage==111 && state.day==3 && state.can_set_shifts) { preview.GetSceneButton(2).onClick.Invoke();move(112); }
            else if(stage==112)
            {
                if(!state.closed) { preview.GetSceneButton(2).onClick.Invoke();return; }
                if(state.goal_result==null || state.goal_result.stage!=3 || state.next_goal!=null || panel.advance.gameObject.activeSelf || !panel.details.text.Contains("クリア時の成果"))throw new Exception("Final stage result mismatch");
                Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/FinalGoalResult.png"});
                preview.GetSceneButton(3).onClick.Invoke();move(113);
            }
            else if(stage==113 && saves.Count==3) { panel.resume.onClick.Invoke();move(114); }
            else if(stage==114 && state.goal_result==null) { saves.saves.SetValueWithoutNotify(0);saves.resume.onClick.Invoke();move(115); }
            else if(stage==115 && state.goal_result!=null)
            {
                if(state.goal_result.stage!=3 || state.next_goal!=null || !panel.details.text.Contains("クリア時の成果"))throw new Exception("Final clear restore failed");
                Debug.Log("CAT_CAFE_GOAL_STAGES_PLAY_OK: stages 2 and 3 / busy / pending and active saves / later entry / final clear restore");
                SessionState.SetInt(Key,0);EditorApplication.Exit(0);
            }
        }
    }
}
