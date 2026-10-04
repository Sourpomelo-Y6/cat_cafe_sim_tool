using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatCafePreview.Editor
{
    [InitializeOnLoad]
    public static class PythonShiftVerification
    {
        const string Key = "CatCafeShiftVerify";
        static PythonShiftVerification() { EditorApplication.update += Update; }
        public static void PrepareScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
            preview.ConfigurePythonButtons();
            var client = preview.GetComponent<PythonStateClient>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(preview.GetSceneButton(0).onClick, client.WorkSelected);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(preview.GetSceneButton(1).onClick, client.RestSelected);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(preview.GetSceneButton(2).onClick, client.ProgressBusiness);
            for (int i = 0; i < 4; i++)
            {
                var button = preview.GetSceneButton(i);
                PrefabUtility.RecordPrefabInstancePropertyModifications(button);
                PrefabUtility.RecordPrefabInstancePropertyModifications(button.GetComponentInChildren<UnityEngine.UI.Text>());
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        public static void Run()
        {
            PrepareScene();
            SessionState.SetInt(Key, 1);
            SessionState.SetFloat(Key + "deadline", (float)EditorApplication.timeSinceStartup + 60);
            EditorApplication.EnterPlaymode();
        }
        public static void RunBusiness()
        {
            PrepareScene();
            var client = UnityEngine.Object.FindObjectOfType<PythonStateClient>();
            var serialized = new SerializedObject(client);
            serialized.FindProperty("port").intValue = 8191;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // Test-only port; do not save it in the user scene.
            SessionState.SetBool(Key + "business", true);
            SessionState.SetInt(Key, 1);
            SessionState.SetFloat(Key + "deadline", (float)EditorApplication.timeSinceStartup + 120);
            EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage = SessionState.GetInt(Key, 0);
            if (stage == 0) return;
            try
            {
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + "deadline", 0)) throw new Exception("Shift Play test timed out");
                if (!EditorApplication.isPlaying) return;
                var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
                var client = preview.GetComponent<PythonStateClient>();
                var roster = preview.GetComponent<PythonCatRoster>();
                if (stage <= 3 && !client.CanChangeShifts) return;
                if (stage == 1)
                {
                    preview.SelectCat(0);
                    if (!preview.GetSceneButton(1).interactable) throw new Exception("Rest button not ready");
                    if(roster != null) roster.GetCard(0).rest.onClick.Invoke(); else preview.GetSceneButton(1).onClick.Invoke();
                    if (preview.GetSceneButton(1).interactable) throw new Exception("Busy button enabled");
                    SessionState.SetInt(Key, 2);
                }
                else if (stage == 2)
                {
                    if (!preview.GetSceneButton(0).interactable) return;
                    if (preview.CurrentSceneKey != "sleep" || !preview.DisplayText.Contains("予定：休養")) throw new Exception("Rest state not reflected");
                    if(roster != null) roster.GetCard(0).work.onClick.Invoke(); else preview.GetSceneButton(0).onClick.Invoke();
                    SessionState.SetInt(Key, 3);
                }
                else if (stage == 3)
                {
                    if (!preview.GetSceneButton(1).interactable) return;
                    if (preview.CurrentSceneKey != "normal" || !preview.DisplayText.Contains("予定：出勤")) throw new Exception("Work state not reflected");
                    if (!preview.GetSceneButton(2).interactable || !preview.GetSceneButton(3).interactable) throw new Exception("Business controls mismatch");
                    if (SessionState.GetBool(Key + "business", false))
                    {
                        preview.GetSceneButton(2).onClick.Invoke();
                        if (preview.GetSceneButton(2).interactable) throw new Exception("Business busy guard failed");
                        SessionState.SetInt(Key, 4);
                        return;
                    }
                    Debug.Log("CAT_CAFE_SHIFT_PLAY_OK: real UI buttons / HTTP commands / rest and work / state images / busy guard");
                    SessionState.SetInt(Key, 0);
                    EditorApplication.ExitPlaymode();
                    EditorApplication.delayCall += () => EditorApplication.Exit(0);
                }
                else if (stage == 4)
                {
                    if (client.CurrentState == null) return;
                    var state = client.CurrentState;
                    var service = preview.GetComponent<PythonServiceScreen>();
                    if(service != null && state.tick>0 && (service.ShowingSeats == state.closed || service.OccupiedCount != Array.FindAll(state.seats,seat=>!string.IsNullOrEmpty(seat.customer_id)).Length && !state.closed))
                        throw new Exception("Live service screen mismatch");
                    if(roster != null && (roster.Count != state.cats.Length || roster.CustomerCount != Array.FindAll(state.seats,seat=>!string.IsNullOrEmpty(seat.customer_id)).Length))
                        throw new Exception("Roster live customer matching failed");
                    if (state.seats != null && Array.Exists(state.seats, seat => !string.IsNullOrEmpty(seat.customer_id)))
                        SessionState.SetBool(Key + "occupied", true);
                    if (state.closed)
                    {
                        if (!preview.DisplayText.Contains("閉店結果") || !preview.DisplayText.Contains("支出")
                            || state.completed_interactions <= 0 || !SessionState.GetBool(Key + "occupied", false)
                            || preview.GetSceneButton(2).interactable || preview.GetSceneButton(0).interactable)
                            throw new Exception("Closing result mismatch");
                        Debug.Log("CAT_CAFE_BUSINESS_PLAY_OK: shift UI / start / ticks / automatic service / occupied seats / closing finances / cat health");
                        SessionState.SetInt(Key, 0); SessionState.SetBool(Key + "business", false);
                        EditorApplication.ExitPlaymode();
                        EditorApplication.delayCall += () => EditorApplication.Exit(0);
                    }
                    else if (client.CanProgressBusiness)
                    {
                        if (state.tick > 0 && preview.GetSceneButton(1).interactable) throw new Exception("Shift button enabled during business");
                        preview.GetSceneButton(2).onClick.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                SessionState.SetInt(Key, 0); Debug.LogException(ex); EditorApplication.Exit(1);
            }
        }
    }
}
