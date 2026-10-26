using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview.Editor
{
    public static class SeatListScrollVerification
    {
        public static void Build()
        {
            const string scenePath = "Assets/Scenes/CatCafePythonState.unity";
            const string prefabPath = "Assets/CatCafePreview/Prefabs/PythonSeatCard.prefab";
            var sceneBytes = File.ReadAllBytes(scenePath);
            var prefabBytes = File.ReadAllBytes(prefabPath);
            try
            {
                // Exercise regeneration, then retain unrelated authored screen/prefab settings.
                PythonServiceSetup.BuildAndVerify();
                PythonServiceLayout.Build();
                PythonServiceLayout.Build();
                Verify();
            }
            finally
            {
                File.WriteAllBytes(scenePath, sceneBytes);
                File.WriteAllBytes(prefabPath, prefabBytes);
                AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceUpdate);
                EditorSceneManager.OpenScene(scenePath);
            }
            var current = UnityEngine.Object.FindObjectOfType<PythonServiceScreen>();
            SeatListLayout.Configure(current);
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                prefab.GetComponent<RectTransform>().sizeDelta = SeatListLayout.CardSize;
                SeatListLayout.StyleCard(prefab.GetComponent<PythonCatCard>());
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("CAT_CAFE_SEAT_SCROLL_REGEN_OK: base, layout, repeated layout, 350px cards, viewport, scrollbar");
            StandaloneScreenSetup.Build();
        }

        static void Verify()
        {
            var service = UnityEngine.Object.FindObjectOfType<PythonServiceScreen>();
            var scroll = service.seatPanel.GetComponent<ScrollRect>();
            var grid = service.seatContent.GetComponent<GridLayoutGroup>();
            if (!scroll.vertical || scroll.horizontal || scroll.verticalScrollbar == null || scroll.scrollSensitivity != 40 ||
                grid.cellSize != SeatListLayout.CardSize || grid.constraintCount != 4 || scroll.viewport.rect.height != 488 ||
                service.cardPrefab.details.GetComponent<RectTransform>().anchoredPosition.y != -314)
                throw new Exception("Seat regeneration lost shared layout");
        }
    }
}
