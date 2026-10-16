using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatCafePreview
{
    public static class StandaloneCafeSetup
    {
        public const string ScenePath = "Assets/Scenes/CatCafeStandalone.unity";
        [MenuItem("Cat Cafe/Standalone/Create first-stage scene")]
        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Standalone cafe").AddComponent<StandaloneCafeScreen>();
            var camera = new GameObject("Camera").AddComponent<Camera>(); camera.backgroundColor = new Color(.12f, .16f, .18f); camera.clearFlags = CameraClearFlags.SolidColor;
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Standalone first-stage scene created: " + ScenePath);
        }
        // Batch mode entry point also verifies actual Unity JSON save round trips.
        public static void VerifyAndBuild()
        {
            StandaloneCafeSmoke.Run("StandaloneEditorSmoke.json");
            var backend = new LocalCafeBackend(); var sim = backend.Simulation;
            sim.Execute("start_business");
            for (int i = 0; i < 10; i++) sim.Execute("advance_business");
            string json = JsonUtility.ToJson(sim.State); var snapshot = JsonUtility.FromJson<Standalone.CafeSnapshot>(json); snapshot.NormalizeJsonStrings(); sim.Restore(snapshot);
            while (sim.State.phase != "closed") sim.Execute("advance_business");
            sim.Execute("next_day"); sim.Validate(sim.State);
            CreateScene();
            Editor.StandaloneBuildStorage.Build(new[] { ScenePath });
            File.WriteAllText("StandaloneVerification.json", "{\"unity_json_roundtrip\":true,\"day\":" + sim.State.day + ",\"funds\":" + sim.State.funds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
            Debug.Log("Standalone verification succeeded");
        }
    }
}
