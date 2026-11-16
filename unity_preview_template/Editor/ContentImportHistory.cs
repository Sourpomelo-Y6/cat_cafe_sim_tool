using System;
using System.IO;
using System.Linq;
using CatCafePreview.Standalone;
using UnityEditor;
using UnityEngine;

namespace CatCafePreview.Editor
{
    internal static class ContentImportHistory
    {
        private static readonly string[] Pointers = {
            "CatCafeContent/catalog.json", "CatCafeContent/human_portraits.json",
            "CatCafeEvents/catalog.json", "CatCafeStateImages/catalog.json", "CatCafeOutfits/catalog.json"
        };
        private static readonly string[] Labels = { "通常素材・交流文", "人物画像", "任意イベント", "状態画像", "見た目画像" };

        [Serializable]
        internal sealed class Snapshot
        {
            public int version;
            public string[] pointers;
            public bool sharedBackground;
        }

        private static string ResourcesPath => Path.Combine(Application.dataPath, "Resources");
        internal static string HistoryPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "UserSettings", "CatCafePreviousContent.json");

        internal static void PublishImport(ContentImportActivation activation)
        {
            if (!activation.HasDifference()) return;
            WriteWithHistory(activation, Capture(ResourcesPath), HistoryPath);
        }

        private static void WriteWithHistory(ContentImportActivation activation, Snapshot previous, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            activation.Add(path, JsonUtility.ToJson(previous, true));
            activation.Publish();
        }

        internal static Snapshot Capture(string resources)
        {
            var values = Pointers.Select(pointer =>
            {
                var path = Path.Combine(resources, pointer);
                return File.Exists(path) ? Convert.ToBase64String(File.ReadAllBytes(path)) : "";
            }).ToArray();
            bool shared = false;
            // Record optional shared artwork, whose absence normally means the default image.
            try
            {
                var events = Parse<OptionalEventPointer>(values[2]);
                if (events != null)
                {
                    var file = ContentImportComparison.ResourceFile(resources, events.eventResource, "CatCafeEvents/Imports", "optional_events");
                    shared = File.Exists(Path.Combine(Path.GetDirectoryName(file), CafeOptionalEventResources.SharedBackgroundFile));
                }
            }
            catch (ArgumentException) { /* Keep the pointer bytes even when the current pointer needs repair. */ }
            return new Snapshot { version = 1, pointers = values, sharedBackground = shared };
        }

        private static Snapshot ReadHistory(string path)
        {
            if (!File.Exists(path)) throw new InvalidOperationException("直前の素材構成はまだ記録されていません。");
            var snapshot = JsonUtility.FromJson<Snapshot>(File.ReadAllText(path));
            if (snapshot == null || snapshot.version != 1 || snapshot.pointers == null || snapshot.pointers.Length != Pointers.Length)
                throw new ArgumentException("直前の素材構成の記録が不正です。");
            return snapshot;
        }

        internal static Snapshot ReadPreviousIfPresent(string path) => File.Exists(path) ? ReadHistory(path) : null;

        internal static string[] PointerTexts(Snapshot snapshot) => snapshot.pointers.Select(value =>
        {
            var bytes = Bytes(value);
            return bytes == null ? null : System.Text.Encoding.UTF8.GetString(bytes).TrimStart('\ufeff');
        }).ToArray();

        [MenuItem("Cat Cafe/素材/直前の素材構成に戻す…", false, 1)]
        private static void RestoreMenu()
        {
            try
            {
                var previous = ReadHistory(HistoryPath);
                var current = Capture(ResourcesPath);
                string summary = Describe(current, previous);
                if (EditorUtility.DisplayDialog("直前の素材構成に戻す", summary, "戻す", "キャンセル"))
                {
                    Restore(ResourcesPath, HistoryPath);
                    EditorUtility.DisplayDialog("素材構成を切り替えました", "Unity の再生を開始すると復元した素材を使います。", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("素材構成を切り替えられません", ex.Message, "OK");
            }
        }

        private static string Describe(Snapshot current, Snapshot previous)
        {
            var before = PointerNames(current);
            var after = PointerNames(previous);
            return string.Join("\n\n", Labels.Select((label, i) => label + "\n  現在　" + before[i] + "\n  復元　" + after[i]));
        }

        private static string[] PointerNames(Snapshot saved)
        {
            var p = saved.pointers;
            var main = Parse<ContentCatalog>(p[0]);
            return new[] {
                Show(main?.bundleResource) + (string.IsNullOrEmpty(main?.interactionResource) ? "（交流文なし）" : "（交流文あり）"),
                Parse<HumanPortraitPack>(p[1])?.pack_id ?? "（なし）",
                Show(Parse<OptionalEventPointer>(p[2])?.eventResource,
                    main == null ? "（なし）" : "通常素材側の設定"),
                Show(Parse<CatStateImagePointer>(p[3])?.resource), Show(Parse<CatOutfitPointer>(p[4])?.resource)
            };
        }

        private static string Show(string path, string fallback = "（なし）")
        {
            if (string.IsNullOrEmpty(path)) return fallback;
            var parts = path.Split('/');
            return parts.Length >= 2 ? parts[parts.Length - 2] : path;
        }

        internal static void Restore(string resources, string history)
        {
            var previous = ReadHistory(history);
            Validate(resources, previous);
            var activation = new ContentImportActivation();
            for (int i = 0; i < Pointers.Length; i++)
                activation.AddBytes(Path.Combine(resources, Pointers[i]), Bytes(previous.pointers[i]));
            if (activation.HasDifference())
                WriteWithHistory(activation, Capture(resources), history);
        }

        private static byte[] Bytes(string value) => string.IsNullOrEmpty(value) ? null : Convert.FromBase64String(value);

        private static T Parse<T>(string value) where T : class
        {
            var bytes = Bytes(value);
            if (bytes == null) return null;
            return JsonUtility.FromJson<T>(System.Text.Encoding.UTF8.GetString(bytes).TrimStart('\ufeff')) ??
                throw new ArgumentException("素材参照の記録が不正です。");
        }

        private static string ReadRequired(string path)
        {
            Require(path);
            return File.ReadAllText(path);
        }

        private static void Require(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("復元先の素材が見つかりません: " + path, path);
        }

        private static void RequireImages(string folder, System.Collections.Generic.IEnumerable<string> images)
        {
            foreach (var image in images.Where(value => !string.IsNullOrEmpty(value)).Distinct(StringComparer.Ordinal))
                Require(Path.Combine(folder, image));
        }

        private static void Validate(string resources, Snapshot saved)
        {
            var p = saved.pointers;
            var main = Parse<ContentCatalog>(p[0]);
            ContentBundle bundle = null;
            string bundleFile = null;
            if (main != null)
            {
                bundleFile = ContentImportComparison.ResourceFile(resources, main.bundleResource, "CatCafeContent/Imports", "bundle");
                bundle = JsonUtility.FromJson<ContentBundle>(ReadRequired(bundleFile));
                ContentValidation.Validate(bundle);
                if (Path.GetFileName(Path.GetDirectoryName(bundleFile)) != bundle.pack_id)
                    throw new ArgumentException("通常素材のパックIDが一致しません。");
                string folder = Path.GetDirectoryName(bundleFile);
                RequireImages(folder, bundle.cats.SelectMany(cat => cat.scenes).Select(scene => scene.image));
                if (!string.IsNullOrEmpty(main.interactionResource))
                {
                    var file = ContentImportComparison.ResourceFile(resources, main.interactionResource, "CatCafeContent/Imports", "interaction_texts");
                    if (Path.GetDirectoryName(file) != folder) throw new ArgumentException("交流文の参照先が通常素材と一致しません。");
                    var text = JsonUtility.FromJson<CafeNarrativeCatalog>(ReadRequired(file));
                    text.Validate();
                    if (text.pack_id != bundle.pack_id) throw new ArgumentException("交流文のパックIDが一致しません。");
                    RequireImages(folder, text.image_bindings.Select(row => row.image));
                }
            }

            var people = Parse<HumanPortraitPack>(p[1]);
            if (people != null)
            {
                people.Validate();
                RequireImages(Path.Combine(resources, "CatCafeContent", "People", people.pack_id),
                    people.people.Select(row => row.image));
            }
            var events = Parse<OptionalEventPointer>(p[2]);
            string eventFile = events != null
                ? ContentImportComparison.ResourceFile(resources, events.eventResource, "CatCafeEvents/Imports", "optional_events")
                : string.IsNullOrEmpty(bundle?.optional_events) ? null : Path.Combine(Path.GetDirectoryName(bundleFile), "optional_events.json");
            if (eventFile != null)
            {
                if (bundle == null) throw new ArgumentException("任意イベントに対応する通常素材がありません。");
                var definitions = CafeOptionalEventJson.Parse(ReadRequired(eventFile));
                if (Path.GetFileName(Path.GetDirectoryName(eventFile)) != definitions.pack_id)
                    throw new ArgumentException("任意イベントのパックIDが一致しません。");
                CafeOptionalEventResources.ValidateBinding(definitions, bundle);
                foreach (var art in definitions.events.Select(row => row.artwork?[0]).Where(CafeOptionalEvents.IsCustomBackground).Distinct())
                    Require(Path.Combine(resources, CafeOptionalEventResources.BackgroundDirectory, art + ".png"));
                if (saved.sharedBackground) Require(Path.Combine(Path.GetDirectoryName(eventFile), CafeOptionalEventResources.SharedBackgroundFile));
            }
            var state = Parse<CatStateImagePointer>(p[3]);
            if (state != null)
            {
                var file = ContentImportComparison.ResourceFile(resources, state.resource, "CatCafeStateImages/Imports", "state_images");
                var images = CatStateImagePack.Parse(ReadRequired(file));
                if (Path.GetFileName(Path.GetDirectoryName(file)) != images.pack_id)
                    throw new ArgumentException("状態画像のパックIDが一致しません。");
                RequireImages(Path.GetDirectoryName(file), images.bindings.Select(row => row.image));
            }
            var outfit = Parse<CatOutfitPointer>(p[4]);
            if (outfit != null)
            {
                var file = ContentImportComparison.ResourceFile(resources, outfit.resource, "CatCafeOutfits/Imports", "cat_outfits");
                var images = CatOutfitPack.Parse(ReadRequired(file));
                if (Path.GetFileName(Path.GetDirectoryName(file)) != images.pack_id)
                    throw new ArgumentException("見た目画像のパックIDが一致しません。");
                RequireImages(Path.GetDirectoryName(file), images.outfits.Select(row => row.image));
            }
        }
    }
}
