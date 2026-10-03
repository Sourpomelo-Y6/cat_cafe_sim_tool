using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatCafePreview.Editor
{
    public static class ContentPreviewSetup
    {
        private const string ScenePath = "Assets/Scenes/CatCafeContentPreview.unity";
        [MenuItem("Cat Cafe/Import Content Pack…")]
        public static void ImportMenu()
        {
            var folder = EditorUtility.OpenFolderPanel("制作ツールで書き出した素材パックを選ぶ", "", "");
            if (folder == "") return;
            try { Import(folder); EditorUtility.DisplayDialog("素材を取り込みました", "Cat Cafe → Open Content Preview を開き、Playで確認してください。", "OK"); }
            catch (Exception ex) { Debug.LogException(ex); EditorUtility.DisplayDialog("取り込みできません", ex.Message, "OK"); }
        }

        public static void Import(string folder)
        {
            folder = Path.GetFullPath(folder);
            var json = File.ReadAllText(Path.Combine(folder, "bundle.json"));
            var bundle = JsonUtility.FromJson<ContentBundle>(json);
            ContentValidation.Validate(bundle);
            var imagePaths = new HashSet<string>();
            foreach (var cat in bundle.cats) foreach (var scene in cat.scenes)
                if (scene.image != "") imagePaths.Add(scene.image);
            foreach (var relative in imagePaths)
            {
                var path = Path.Combine(folder, relative);
                if (!File.Exists(path)) throw new ArgumentException("Missing image: " + relative);
                using (var hash = SHA256.Create())
                {
                    var digest = BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
                    if (digest != Path.GetFileNameWithoutExtension(relative)) throw new ArgumentException("Image hash mismatch: " + relative);
                }
            }
            string root = Path.Combine(Application.dataPath, "Resources/CatCafeContent");
            string target = Path.Combine(root, "Imports", bundle.pack_id);
            if (Directory.Exists(target))
            {
                if (File.ReadAllText(Path.Combine(target, "bundle.json")) != json)
                    throw new ArgumentException("同じパックIDで異なる内容が存在します。制作ツールから書き出し直してください。");
                foreach (var relative in imagePaths)
                    if (!File.Exists(Path.Combine(target, relative)) || !SameBytes(Path.Combine(folder, relative), Path.Combine(target, relative)))
                        throw new ArgumentException("取り込み済み画像が欠落・変更されています。新しいパックとして書き出してください。");
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(target, "images"));
                foreach (var relative in imagePaths) File.Copy(Path.Combine(folder, relative), Path.Combine(target, relative));
                File.WriteAllText(Path.Combine(target, "bundle.json"), json, new System.Text.UTF8Encoding(false));
                var report = Path.Combine(folder, "validation.json");
                if (File.Exists(report)) File.Copy(report, Path.Combine(target, "validation.json"));
            }
            // 全素材を配置した後に参照先だけを切り替える。以前のパックは保持する。
            var catalog = new ContentCatalog { bundleResource = "CatCafeContent/Imports/" + bundle.pack_id + "/bundle" };
            var catalogPath = Path.Combine(root, "catalog.json");
            var temporary = catalogPath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(catalog, true), new System.Text.UTF8Encoding(false));
            if (File.Exists(catalogPath)) File.Replace(temporary, catalogPath, null);
            else File.Move(temporary, catalogPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Imported cat cafe pack: " + bundle.pack_id);
        }

        private static bool SameBytes(string a, string b)
        {
            using (var hash = SHA256.Create())
                return Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(a))) == Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(b)));
        }

        [MenuItem("Cat Cafe/Open Content Preview")]
        public static void OpenPreview()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureScene();
            EditorSceneManager.OpenScene(ScenePath);
        }

        public static void EnsureScene()
        {
            if (File.Exists(ScenePath)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Cat Cafe Content Preview").AddComponent<ContentPreview>();
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(value => value.path == ScenePath)) scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // バッチモードで素材の取り込み・全猫全場面の表示・スクリーンショットを検証する。
        public static void VerifyAndCapture()
        {
            string pack = Environment.GetEnvironmentVariable("CAT_CAFE_CONTENT_PACK");
            if (string.IsNullOrEmpty(pack)) throw new ArgumentException("CAT_CAFE_CONTENT_PACK is required.");
            Import(pack);
            EnsureScene();
            EditorSceneManager.OpenScene(ScenePath);
            var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
            preview.Initialize();
            var source = JsonUtility.FromJson<ContentBundle>(File.ReadAllText(Path.Combine(pack, "bundle.json")));
            var invalid = JsonUtility.FromJson<ContentBundle>(JsonUtility.ToJson(source));
            invalid.cats[0].scenes[0].image = "../outside.png";
            bool rejected = false;
            try { ContentValidation.Validate(invalid); } catch (ArgumentException) { rejected = true; }
            if (!rejected) throw new Exception("Invalid image path was accepted.");
            for (int index = 0; index < source.cats.Length; index++)
            {
                preview.SelectCat(index);
                if (preview.CurrentCatId != source.cats[index].cat_id) throw new Exception("Cat selection mismatch.");
                foreach (var scene in source.cats[index].scenes)
                {
                    preview.ShowScene(scene.key);
                    if (preview.CurrentSceneKey != scene.key || !preview.DisplayText.Contains(scene.text)) throw new Exception("Scene text mismatch.");
                    if (preview.HasImage != (scene.image != "")) throw new Exception("Image load mismatch.");
                }
            }
            preview.SelectCat(0); preview.ShowScene("normal");
            var cameraObject = new GameObject("Capture Camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.white;
            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var target = new RenderTexture(1280, 800, 24);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0); image.Apply();
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/ContentPreview.png", image.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("CAT_CAFE_VERIFY_OK: " + source.cats.Length + " cats / " + source.cats.Length * 4 + " scenes");
        }
    }
}
