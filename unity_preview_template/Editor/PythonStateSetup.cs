using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatCafePreview.Editor
{
    public static class PythonStateSetup
    {
        const string Path = "Assets/Scenes/CatCafePythonState.unity";
        [MenuItem("Cat Cafe/プレビュー/営業画面を開く", false, 0)]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (System.IO.File.Exists(Path)) { EditorSceneManager.OpenScene(Path); return; }
            CreateAndVerify();
        }
        public static void CreateAndVerify()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/CatCafeContentPreview.unity");
            var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
            preview.gameObject.AddComponent<PythonStateClient>();
            EditorSceneManager.SaveScene(scene, Path);
            Verify();
        }
        public static void Verify()
        {
            EditorSceneManager.OpenScene(Path);
            var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
            preview.Initialize();
            var state = JsonUtility.FromJson<PythonCafeState>("{\"version\":1,\"day\":1,\"tick\":0,\"funds\":1000,\"cats\":[{\"cat_id\":\"cat-mike\",\"content_cat_id\":\"playtest-mike\",\"name\":\"ミケ\",\"stamina\":73,\"max_stamina\":100,\"working\":true,\"health_status\":\"healthy\",\"activity\":\"cafe\"},{\"cat_id\":\"cat-tama\",\"content_cat_id\":\"playtest-tama\",\"name\":\"タマ\",\"stamina\":100,\"max_stamina\":100,\"working\":false,\"health_status\":\"healthy\",\"activity\":\"cafe\"}]}");
            PythonStateClient.Validate(state); preview.ApplyPythonState(state);
            if (!preview.DisplayText.Contains("73/100") || !preview.HasImage || preview.PerformAction("play")) throw new Exception("Python display mismatch");
            preview.SelectCat(1);
            if (preview.CurrentSceneKey != "sleep" || !preview.DisplayText.Contains("休養")) throw new Exception("Rest mismatch");
            preview.PythonConnectionError("offline");
            if (preview.HasImage) throw new Exception("Stale image after disconnect");
            preview.ApplyPythonState(state);
            if (!preview.HasImage) throw new Exception("Reconnect failed");
            state.cats[1].content_cat_id = "unknown";
            preview.ApplyPythonState(state);
            if (preview.HasImage || !preview.DisplayText.Contains("未登録")) throw new Exception("Missing content mismatch");
            Debug.Log("CAT_CAFE_PYTHON_STATE_OK: state, IDs, stamina, rest, reconnect, missing content, read-only UI");
        }
        public static void VerifyLive()
        {
            Verify();
            var request = UnityEngine.Networking.UnityWebRequest.Get("http://127.0.0.1:8190/state");
            request.timeout = 5;
            var operation = request.SendWebRequest();
            double deadline = EditorApplication.timeSinceStartup + 15;
            EditorApplication.CallbackFunction update = null;
            update = () =>
            {
                if (!operation.isDone && EditorApplication.timeSinceStartup < deadline) return;
                EditorApplication.update -= update;
                try
                {
                    if (!operation.isDone || request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                        throw new Exception("HTTP connection failed: " + request.error);
                    var state = JsonUtility.FromJson<PythonCafeState>(request.downloadHandler.text);
                    PythonStateClient.Validate(state);
                    var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
                    preview.ApplyPythonState(state);
                    for (int i = 0; i < state.cats.Length; i++)
                    {
                        preview.SelectCat(i);
                        if (!preview.DisplayText.Contains(state.cats[i].name) || !preview.HasImage)
                            throw new Exception("Live cat display failed");
                    }
                    Debug.Log("CAT_CAFE_PYTHON_LIVE_OK: UnityWebRequest / Python HTTP / 5 cats / mapped images");
                    request.Dispose(); EditorApplication.Exit(0);
                }
                catch (Exception ex) { Debug.LogException(ex); request.Dispose(); EditorApplication.Exit(1); }
            };
            EditorApplication.update += update;
        }
    }
}
