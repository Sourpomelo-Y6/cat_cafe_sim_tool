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
        public static void VerifyInteraction()
        {
            var state = new CatInteractionState();
            state.Act("play");
            if (state.Stamina != 95 || state.Engagement != 12 || state.Turns != 9) throw new Exception("Play mismatch");
            state.Act("play");
            if (Math.Abs(state.Engagement - 22.2f) > .001) throw new Exception("Boredom mismatch");
            state.Act("pet");
            if (state.Stamina != 88 || Math.Abs(state.Engagement - 30.2f) > .001) throw new Exception("Pet mismatch");
            state.Act("sleep");
            if (state.Stamina != 90) throw new Exception("Rest mismatch");
            while (state.Turns > 0 && state.EndReason == "") state.Act("sleep");
            if (state.Act("play")) throw new Exception("Ended set accepted action");
            state.NextSet();
            if (state.Turns != 10 || state.Engagement != 0 || state.Stamina != 100) throw new Exception("Next set mismatch");
            for (int i = 0; i < 20; i++)
            {
                state.NextSet();
                for (int j = 0; j < 10; j++) state.Act("play");
            }
            if (state.Stamina != 0) throw new Exception("Exhaustion mismatch");
            state.NextSet(); state.Act("sleep");
            if (state.Stamina != 2) throw new Exception("Rest after exhaustion mismatch");
            VerifyAndCapture();
            var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
            var first = preview.CurrentInteraction;
            preview.PerformAction("play");
            preview.SelectCat(1);
            if (preview.CurrentInteraction.Stamina != 100) throw new Exception("Cat states mixed");
            preview.SelectCat(0);
            if (preview.CurrentInteraction != first || first.Stamina != 95) throw new Exception("Selection changed state");
            Debug.Log("CAT_CAFE_INTERACTION_OK: rules, rest, limits, exhaustion, independent cat states");
        }

        private const string ScenePath = "Assets/Scenes/CatCafeContentPreview.unity";
        [MenuItem("Cat Cafe/素材/素材パックを取り込む…", false, 0)]
        public static void ImportMenu()
        {
            var folder = EditorUtility.OpenFolderPanel("制作ツールで書き出した素材パックを選ぶ", "", "");
            if (folder == "") return;
            try { Import(folder); EditorUtility.DisplayDialog("素材を取り込みました", "Cat Cafe → プレビュー → 素材プレビューを開く を開き、Playで確認してください。", "OK"); }
            catch (Exception ex) { Debug.LogException(ex); EditorUtility.DisplayDialog("取り込みできません", ex.Message, "OK"); }
        }

        public static void Import(string folder)
        {
            folder = Path.GetFullPath(folder);
            var json = File.ReadAllText(Path.Combine(folder, "bundle.json"));
            var bundle = JsonUtility.FromJson<ContentBundle>(json);
            ContentValidation.Validate(bundle);
            string interactionPath=Path.Combine(folder,"interaction_texts.json");
            string interactionJson=File.Exists(interactionPath)?File.ReadAllText(interactionPath):null;
            CatCafePreview.Standalone.CafeNarrativeCatalog interactions=null;
            if(interactionJson!=null)
            {
                interactions=JsonUtility.FromJson<CatCafePreview.Standalone.CafeNarrativeCatalog>(interactionJson);interactions.Validate();
                if(interactions.pack_id!=bundle.pack_id)throw new ArgumentException("交流文と素材パックのIDが一致しません。");
                foreach(var binding in interactions.cat_bindings)
                    if(binding.content_cat_id!="" && !Array.Exists(bundle.cats,c=>c.cat_id==binding.content_cat_id))throw new ArgumentException("交流文の猫画像IDが素材パックにありません。");
                foreach(var image in interactions.image_bindings)
                {
                    if(image.image=="")continue;
                    var cat=Array.Find(bundle.cats,c=>c.cat_id==image.content_cat_id);
                    if(cat==null || !Array.Exists(cat.scenes,r=>r.key==image.resolved_scene_key && r.image==image.image))throw new ArgumentException("交流文の画像対応が素材と一致しません。");
                }
            }
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
                string existingInteraction=Path.Combine(target,"interaction_texts.json");
                if(File.Exists(existingInteraction)!=(interactionJson!=null) || interactionJson!=null && File.ReadAllText(existingInteraction)!=interactionJson)
                    throw new ArgumentException("同じパックIDで交流文が変更されています。書き出し直してください。");
                foreach (var relative in imagePaths)
                    if (!File.Exists(Path.Combine(target, relative)) || !SameBytes(Path.Combine(folder, relative), Path.Combine(target, relative)))
                        throw new ArgumentException("取り込み済み画像が欠落・変更されています。新しいパックとして書き出してください。");
            }
            else
            {
                string pending=target+".pending-"+Guid.NewGuid().ToString("N");
                try
                {
                    Directory.CreateDirectory(Path.Combine(pending, "images"));
                    foreach (var relative in imagePaths) File.Copy(Path.Combine(folder, relative), Path.Combine(pending, relative));
                    File.WriteAllText(Path.Combine(pending, "bundle.json"), json, new System.Text.UTF8Encoding(false));
                    if(interactionJson!=null)File.WriteAllText(Path.Combine(pending,"interaction_texts.json"),interactionJson,new System.Text.UTF8Encoding(false));
                    var report=Path.Combine(folder,"validation.json");if(File.Exists(report))File.Copy(report,Path.Combine(pending,"validation.json"));
                    Directory.Move(pending,target);
                }
                finally
                {
                    string expected=Path.GetFullPath(Path.Combine(root,"Imports"))+Path.DirectorySeparatorChar;
                    if(Directory.Exists(pending) && Path.GetFullPath(pending).StartsWith(expected,StringComparison.OrdinalIgnoreCase)
                        && Path.GetFileName(pending).StartsWith(bundle.pack_id+".pending-",StringComparison.Ordinal))Directory.Delete(pending,true);
                }
            }
            // 全素材を配置した後に参照先だけを切り替える。以前のパックは保持する。
            var catalog = new ContentCatalog { bundleResource = "CatCafeContent/Imports/" + bundle.pack_id + "/bundle", interactionResource=interactionJson==null?null:"CatCafeContent/Imports/"+bundle.pack_id+"/interaction_texts" };
            var catalogPath = Path.Combine(root, "catalog.json");
            var temporary = catalogPath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(catalog, true), new System.Text.UTF8Encoding(false));
            if (File.Exists(catalogPath)) File.Replace(temporary, catalogPath, null);
            else File.Move(temporary, catalogPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CatTexturePolicy.ApplyPack(target);
            Debug.Log("Imported cat cafe pack: " + bundle.pack_id);
        }

        private static bool SameBytes(string a, string b)
        {
            using (var hash = SHA256.Create())
                return Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(a))) == Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(b)));
        }

        // 既存のUIを再生成せず、一覧の項目テンプレートだけを修正する。
        public static void RepairCatDropdown()
        {
            const string path = "Assets/CatCafePreview/Prefabs/ContentPreview.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var dropdown = root.GetComponentInChildren<UnityEngine.UI.Dropdown>(true);
                ContentPreviewLayout.ConfigureCatDropdown(dropdown);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            EditorSceneManager.OpenScene(ScenePath);
            var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
            var sceneDropdown = preview.GetComponentInChildren<UnityEngine.UI.Dropdown>(true);
            ContentPreviewLayout.ConfigureCatDropdown(sceneDropdown);
            foreach (var rect in sceneDropdown.template.GetComponentsInChildren<RectTransform>(true))
                PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sceneDropdown.itemText);
            EditorSceneManager.SaveScene(preview.gameObject.scene, ScenePath);
            VerifyAndCapture();
        }

        [MenuItem("Cat Cafe/プレビュー/素材プレビューを開く", false, 1)]
        public static void OpenPreview()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureScene();
            EditorSceneManager.OpenScene(ScenePath);
        }

        public static void EnsureScene()
        {
            var scene = File.Exists(ScenePath) ? EditorSceneManager.OpenScene(ScenePath)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
            if (preview == null) preview = new GameObject("Cat Cafe Content Preview").AddComponent<ContentPreview>();
            if (preview.HasUIBindings)
            {
                UpgradeLegacyFonts(preview);
                return; // 手動配置・色・フォントなどの編集を保持する。
            }
            if (preview.GetComponentInChildren<Canvas>(true) != null)
                throw new ArgumentException("UIは既にあります。ContentPreviewの参照をInspectorで修正してください。自動再配置は行いません。");
            ContentPreviewLayout.Build(preview);
            preview.Initialize();
            Directory.CreateDirectory("Assets/CatCafePreview/Prefabs");
            const string prefabPath = "Assets/CatCafePreview/Prefabs/ContentPreview.prefab";
            if (File.Exists(prefabPath)) throw new ArgumentException("既存のプレビューPrefabは上書きしません。");
            PrefabUtility.SaveAsPrefabAssetAndConnect(preview.gameObject, prefabPath, InteractionMode.AutomatedAction);
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(value => value.path == ScenePath)) scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void UpgradeLegacyFonts(ContentPreview preview)
        {
            var legacy = AssetDatabase.LoadAssetAtPath<Font>("Assets/CatCafePreview/Fonts/PreviewJapanese.fontsettings");
            if (legacy == null) return;
            var font = ContentPreviewLayout.LoadFont();
            const string path = "Assets/CatCafePreview/Prefabs/ContentPreview.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            try
            {
                foreach (var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    if (text.font == legacy) { text.font = font; changed = true; }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            foreach (var text in preview.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (text.font == legacy)
                {
                    text.font = font;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                    changed = true;
                }
            if (changed) EditorSceneManager.SaveScene(preview.gameObject.scene, ScenePath);
        }

        [MenuItem("Cat Cafe/素材/表示内容を更新", false, 1)]
        public static void RefreshDisplayedContent()
        {
            var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
            if (preview == null || !preview.HasUIBindings)
            {
                EditorUtility.DisplayDialog("プレビューを開いてください", "Cat Cafe → プレビュー → 素材プレビューを開く から開いてください。", "OK");
                return;
            }
            Undo.RecordObjects(preview.GetComponentsInChildren<Component>(true), "Refresh preview content");
            preview.Initialize();
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
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
            if (!preview.HasUIBindings || preview.GetComponentInChildren<Canvas>() == null)
                throw new Exception("Serialized UI did not survive scene reload.");
            if (!PrefabUtility.IsPartOfPrefabInstance(preview)) throw new Exception("Preview is not a prefab instance.");
            CaptureCanvas(preview, "Logs/ContentPreviewEditor.png");
            int objectCount = preview.GetComponentsInChildren<Transform>(true).Length;
            preview.Initialize();
            preview.Initialize();
            if (preview.GetComponentsInChildren<Transform>(true).Length != objectCount)
                throw new Exception("Runtime initialization created duplicate UI.");
            var firstButton = preview.GetSceneButton(0);
            var buttonPosition = firstButton.GetComponent<RectTransform>().anchoredPosition;
            firstButton.GetComponent<RectTransform>().anchoredPosition = buttonPosition + new Vector2(12, 0);
            var caption = firstButton.GetComponentInChildren<UnityEngine.UI.Text>();
            int originalSize = caption.fontSize;
            caption.fontSize = originalSize + 2;
            preview.Initialize();
            if (caption.fontSize != originalSize + 2 || firstButton.GetComponent<RectTransform>().anchoredPosition != buttonPosition + new Vector2(12, 0))
                throw new Exception("Initialization replaced designer changes.");
            caption.fontSize = originalSize;
            firstButton.GetComponent<RectTransform>().anchoredPosition = buttonPosition;
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
                    int buttonIndex = Array.IndexOf(ContentValidation.SceneKeys, scene.key);
                    if (buttonIndex < 0 || buttonIndex >= 4)
                    {
                        preview.ShowScene(scene.key);
                        if(preview.CurrentSceneKey!=scene.key || preview.HasImage!=(scene.image!=""))throw new Exception("Interaction scene preview mismatch.");
                        continue;
                    }
                    var button = preview.GetSceneButton(buttonIndex);
                    if (button.onClick.GetPersistentEventCount() != 1 || button.onClick.GetPersistentMethodName(0) != "ShowScene")
                        throw new Exception("Scene button has no serialized action.");
                    // バッチ検証は編集モードなので、この呼び出しだけ実行を許可する。
                    var callState = button.onClick.GetPersistentListenerState(0);
                    button.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
                    button.onClick.Invoke();
                    button.onClick.SetPersistentListenerState(0, callState);
                    if (preview.CurrentSceneKey != scene.key || !preview.DisplayText.Contains(scene.text)) throw new Exception("Scene text mismatch.");
                    if (preview.HasImage != (scene.image != "")) throw new Exception("Image load mismatch.");
                }
            }
            preview.SelectCat(0); preview.ShowScene("normal");
            CaptureCanvas(preview, "Logs/ContentPreview.png");
            VerifyOpenDropdown(preview);
            Debug.Log("CAT_CAFE_VERIFY_OK: serialized UI / prefab / designer edits preserved / " + source.cats.Length + " cats / " + source.cats.Length * 4 + " scenes");
        }
        private static void VerifyOpenDropdown(ContentPreview preview)
        {
            var dropdown = preview.GetComponentInChildren<UnityEngine.UI.Dropdown>(true);
            Canvas.ForceUpdateCanvases();
            var label = dropdown.itemText;
            var extent = label.rectTransform.rect.size;
            var settings = label.GetGenerationSettings(extent);
            using (var generator = new TextGenerator())
            {
                foreach (var option in dropdown.options)
                {
                    generator.Populate(option.text, settings);
                    if (generator.vertexCount <= 4 || label.preferredHeight > extent.y)
                        throw new Exception("Dropdown option is clipped: " + option.text);
                }
                settings.generationExtents = new Vector2(extent.x, 17);
                generator.Populate(dropdown.options[0].text, settings);
                Debug.Log("OLD_DROPDOWN_HEIGHT_17_VERTICES: " + generator.vertexCount);
            }
            // Dropdownのフェード処理は通常Play時のStartで準備される。
            typeof(UnityEngine.UI.Dropdown).GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(dropdown, null);
            dropdown.alphaFadeSpeed = 0;
            dropdown.Show();
            var popup = dropdown.transform.Find("Dropdown List");
            if (popup == null) throw new Exception("Dropdown did not open.");
            foreach (var group in popup.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1;
            Canvas.ForceUpdateCanvases();
            var toggles = popup.GetComponentsInChildren<UnityEngine.UI.Toggle>();
            if (toggles.Length != dropdown.options.Count) throw new Exception("Dropdown option count mismatch.");
            CaptureCanvas(preview, "Logs/ContentPreviewDropdown.png");
            Debug.Log("CAT_CAFE_DROPDOWN_OK: " + toggles.Length + " visible options / row height " + extent.y);
            UnityEngine.Object.DestroyImmediate(popup.gameObject);
            var blocker = preview.transform.Find("Content Canvas/Blocker");
            if (blocker != null) UnityEngine.Object.DestroyImmediate(blocker.gameObject);
        }
        private static void CaptureCanvas(ContentPreview preview, string filename)
        {
            var cameraObject = new GameObject("Capture Camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.white;
            var canvas = preview.GetComponentInChildren<Canvas>();
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            var previousDistance = canvas.planeDistance;
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
            File.WriteAllBytes(filename, image.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
            canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousDistance;
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
