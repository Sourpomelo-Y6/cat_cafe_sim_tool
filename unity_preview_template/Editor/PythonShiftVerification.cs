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
                if (!client.CanChangeShifts) return;
                if (stage == 1)
                {
                    preview.SelectCat(0);
                    if (!preview.GetSceneButton(1).interactable) throw new Exception("Rest button not ready");
                    preview.GetSceneButton(1).onClick.Invoke();
                    if (preview.GetSceneButton(1).interactable) throw new Exception("Busy button enabled");
                    SessionState.SetInt(Key, 2);
                }
                else if (stage == 2)
                {
                    if (!preview.GetSceneButton(0).interactable) return;
                    if (preview.CurrentSceneKey != "sleep" || !preview.DisplayText.Contains("予定：休養")) throw new Exception("Rest state not reflected");
                    preview.GetSceneButton(0).onClick.Invoke();
                    SessionState.SetInt(Key, 3);
                }
                else if (stage == 3)
                {
                    if (!preview.GetSceneButton(1).interactable) return;
                    if (preview.CurrentSceneKey != "normal" || !preview.DisplayText.Contains("予定：出勤")) throw new Exception("Work state not reflected");
                    if (preview.GetSceneButton(2).interactable || preview.GetSceneButton(3).interactable) throw new Exception("Future operation enabled");
                    Debug.Log("CAT_CAFE_SHIFT_PLAY_OK: real UI buttons / HTTP commands / rest and work / state images / busy guard");
                    SessionState.SetInt(Key, 0);
                    EditorApplication.ExitPlaymode();
                    EditorApplication.delayCall += () => EditorApplication.Exit(0);
                }
            }
            catch (Exception ex)
            {
                SessionState.SetInt(Key, 0); Debug.LogException(ex); EditorApplication.Exit(1);
            }
        }
    }
}
