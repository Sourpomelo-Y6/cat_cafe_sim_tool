using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatCafePreview.Editor
{
    [InitializeOnLoad]
    public static class PythonPlayerVerification
    {
        const string Key="CatCafePlayerPlay";
        static PythonPlayerVerification() { EditorApplication.update+=Update; }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());
            serialized.FindProperty("port").intValue=8191;serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+150);EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Player test timeout stage "+stage);
                if(!EditorApplication.isPlaying)return;
                var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var client=preview.GetComponent<PythonStateClient>();
                if(!client.CanManageSaves)return;
                preview.RefreshPythonControls();
                var state=client.CurrentState;var player=preview.GetComponent<PythonPlayerPanel>();var detail=preview.GetComponent<PythonCatDetailsPanel>();var saves=preview.GetComponent<PythonSavePanel>();
                Action<int> move=value=>SessionState.SetInt(Key,value);
                if(stage==1)
                {
                    detail.Open("cat-mugi");if(!detail.interact.interactable)throw new Exception("Player entry disabled");
                    detail.interact.onClick.Invoke();if(detail.interact.interactable)throw new Exception("Player start busy guard failed");move(2);
                }
                else if(stage==2 && state.player_interaction!=null && state.player_interaction.active)
                {
                    if(!player.panel.activeSelf || detail.panel.activeSelf || state.player_interaction.remaining_sets!=2 || client.CanProgressBusiness || client.CanChangeShifts)throw new Exception("Player start state mismatch");
                    player.targets.value=Array.FindIndex(state.player_interaction.targets,row=>row.choice=="voice");player.actions[5].onClick.Invoke();move(3);
                }
                else if(stage==3 && state.player_interaction.mode=="声をかける")
                { SessionState.SetString(Key+"midway",JsonUtility.ToJson(state.player_interaction));preview.GetSceneButton(3).onClick.Invoke();move(4); }
                else if(stage==4 && saves.Count==1) { player.actions[0].onClick.Invoke();move(5); }
                else if(stage==5 && state.player_interaction.remaining_ticks==8) { saves.saves.value=0;saves.resume.onClick.Invoke();move(6); }
                else if(stage==6 && state.player_interaction.remaining_ticks==9)
                {
                    if(JsonUtility.ToJson(state.player_interaction)!=SessionState.GetString(Key+"midway","") || !player.panel.activeSelf)throw new Exception("Midway save restore mismatch");
                    player.Close();detail.Open("cat-mugi");if(detail.interact.GetComponentInChildren<UnityEngine.UI.Text>().text!="交流を再開")throw new Exception("Resume entry missing");
                    detail.interact.onClick.Invoke();if(!player.panel.activeSelf)throw new Exception("Resume button failed");
                    player.targets.value=Array.FindIndex(state.player_interaction.targets,row=>row.choice=="teaser");player.actions[5].onClick.Invoke();move(7);
                }
                else if(stage==7 && state.player_interaction.mode=="ねこじゃらし") { player.actions[0].onClick.Invoke();move(8); }
                else if(stage==8 && state.player_interaction.remaining_ticks==7)
                {
                    Canvas.ForceUpdateCanvases();Capture(preview,"Logs/PlayerLive.png");player.finish.onClick.Invoke();if(player.finish.interactable)throw new Exception("Player finish busy guard failed");move(9);
                }
                else if(stage==9 && !state.player_interaction.active)
                {
                    if(state.tick!=0 || state.funds!=1000 || state.player_interaction.affinity_after<=0 || player.finish.interactable || !player.summary.text.Contains("確定") || state.goal_result==null || state.goal_result.kind!="continue_bond_goal")throw new Exception("Result or bond goal mismatch");
                    SessionState.SetString(Key+"result",JsonUtility.ToJson(state.player_interaction));Canvas.ForceUpdateCanvases();Capture(preview,"Logs/PlayerResult.png");
                    preview.GetSceneButton(3).onClick.Invoke();move(10);
                }
                else if(stage==10 && saves.Count==2) { saves.saves.value=1;saves.resume.onClick.Invoke();move(11); }
                else if(stage==11 && state.player_interaction.active) { saves.saves.value=0;saves.resume.onClick.Invoke();move(12); }
                else if(stage==12 && !state.player_interaction.active)
                {
                    if(JsonUtility.ToJson(state.player_interaction)!=SessionState.GetString(Key+"result","") || !player.panel.activeSelf)throw new Exception("Result save restore mismatch");
                    player.Close();var goal=preview.GetComponent<PythonGoalPanel>();if(!goal.panel.activeSelf || !goal.resume.interactable)throw new Exception("Bond goal hidden after result close");
                    goal.resume.onClick.Invoke();move(13);
                }
                else if(stage==13 && state.goal_result==null)
                {
                    if(!client.CanProgressBusiness)throw new Exception("Business remained blocked after player result");
                    Debug.Log("CAT_CAFE_PLAYER_PLAY_OK: start / switch / midway save / resume / busy / result / affinity / bond clear / result save / continue");SessionState.SetInt(Key,0);EditorApplication.Exit(0);
                }
            }
            catch(Exception ex) { SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1); }
        }
        static void Capture(ContentPreview preview,string path)
        { typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,path}); }
    }
}
