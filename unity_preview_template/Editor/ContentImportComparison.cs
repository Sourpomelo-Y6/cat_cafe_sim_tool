using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CatCafePreview.Standalone;
using UnityEngine;

namespace CatCafePreview.Editor
{
    internal sealed class ContentImportComparison
    {
        private const int DisplayLimit = 18;
        private readonly List<string> details = new List<string>();
        private readonly Dictionary<string, string> hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        internal int Count => details.Count;
        internal string FullSummary => string.Join("\n", details);
        internal string Summary => string.Join("\n", details.Take(DisplayLimit)) +
            (Count > DisplayLimit ? "\nほか " + (Count - DisplayLimit) + " 件" : "");

        internal static ContentImportComparison Compare(string folder, string resources)
        {
            var result = new ContentImportComparison();
            var nextBundle = Path.Combine(folder, "bundle.json");
            var incoming = JsonUtility.FromJson<ContentBundle>(File.ReadAllText(nextBundle));
            ContentValidation.Validate(incoming);

            var contentRoot = Path.Combine(resources, "CatCafeContent");
            var pointer = ReadIfPresent<ContentCatalog>(Path.Combine(contentRoot, "catalog.json"));
            if (pointer == null)
            {
                result.Add("通常素材", "初回取り込み");
                return result;
            }

            var activeBundle = ResourceFile(resources, pointer.bundleResource, "CatCafeContent/Imports", "bundle");
            var current = JsonUtility.FromJson<ContentBundle>(File.ReadAllText(activeBundle));
            ContentValidation.Validate(current);
            var activeFolder = Path.GetDirectoryName(activeBundle);
            result.CompareJson(nextBundle, activeBundle, "猫素材・表示名・指名派遣");
            var activeInteraction = string.IsNullOrEmpty(pointer.interactionResource) ? null :
                ResourceFile(resources, pointer.interactionResource, "CatCafeContent/Imports", "interaction_texts");
            var nextInteraction = ExistingFile(folder, "interaction_texts.json");
            result.CompareJson(nextInteraction, activeInteraction, "交流文");

            var activePeople = Path.Combine(contentRoot, "human_portraits.json");
            var nextPeople = ExistingFile(folder, "human_portraits.json");
            if (nextPeople != null)
            {
                if (File.Exists(activePeople)) result.CompareJson(nextPeople, activePeople, "人物画像");
                else result.Add("人物画像", "新規登録");
            }

            var eventPointer = ReadIfPresent<OptionalEventPointer>(Path.Combine(resources, "CatCafeEvents/catalog.json"));
            var activeEvents = eventPointer != null ? ResourceFile(resources, eventPointer.eventResource, "CatCafeEvents/Imports", "optional_events")
                : string.IsNullOrEmpty(current.optional_events) ? null : Path.Combine(activeFolder, "optional_events.json");
            var nextEvents = ExistingFile(folder, "optional_events.json");
            if (nextEvents != null)
            {
                result.CompareJson(nextEvents, activeEvents, "任意イベント");
                var newShared = ExistingFile(folder, CafeOptionalEventResources.SharedBackgroundFile);
                var oldShared = eventPointer == null ? null :
                    ExistingFile(Path.GetDirectoryName(activeEvents), CafeOptionalEventResources.SharedBackgroundFile);
                if (result.Hash(newShared) != result.Hash(oldShared)) result.Add("任意イベント", "共通背景画像");
            }
            else if (eventPointer == null && activeEvents != null)
                result.Add("任意イベント", "内蔵イベント定義の削除");

            var nextState = ExistingFile(folder, "state_images.json");
            var nextOutfit = ExistingFile(folder, "cat_outfits.json");
            string activeState = null, activeOutfit = null;
            if (nextState != null)
            {
                var p = ReadIfPresent<CatStateImagePointer>(Path.Combine(resources, "CatCafeStateImages/catalog.json"));
                if (p != null) activeState = ResourceFile(resources, p.resource, "CatCafeStateImages/Imports", "state_images");
                result.CompareJson(nextState, activeState, "状態画像");
            }
            if (nextOutfit != null)
            {
                var p = ReadIfPresent<CatOutfitPointer>(Path.Combine(resources, "CatCafeOutfits/catalog.json"));
                if (p != null) activeOutfit = ResourceFile(resources, p.resource, "CatCafeOutfits/Imports", "cat_outfits");
                result.CompareJson(nextOutfit, activeOutfit, "見た目画像");
            }

            if (result.Count != 0) return result;

            // Equal definition files name the same content-addressed images. Prove both copies
            // still match their references before treating the import as a no-op.
            var images = incoming.cats.SelectMany(c => c.scenes).Select(s => s.image).Where(s => !string.IsNullOrEmpty(s));
            result.CheckImages(folder, activeFolder, images);
            if (nextInteraction != null)
            {
                var text = JsonUtility.FromJson<CafeNarrativeCatalog>(File.ReadAllText(nextInteraction));
                text.Validate();
                if (text.pack_id != incoming.pack_id)
                    throw new ArgumentException("交流文と素材パックのIDが一致しません。");
                var active = JsonUtility.FromJson<CafeNarrativeCatalog>(File.ReadAllText(activeInteraction));
                active.Validate();
                if (active.pack_id != current.pack_id) throw new ArgumentException("現行の交流文と素材パックのIDが一致しません。");
                result.CheckImages(folder, activeFolder, text.image_bindings.Select(row => row.image).Where(path => !string.IsNullOrEmpty(path)));
            }
            if (nextPeople != null)
            {
                var people = HumanPortraitImport.Validate(folder);
                var active = ReadIfPresent<HumanPortraitPack>(activePeople);
                active.Validate();
                result.CheckImages(folder, Path.Combine(contentRoot, "People", active.pack_id), people.people.Select(row => row.image));
            }
            if (nextState != null)
            {
                var pack = CatStateImageImport.Validate(folder, incoming, true);
                result.CheckImages(folder, Path.GetDirectoryName(activeState), pack.bindings.Select(row => row.image));
            }
            if (nextOutfit != null)
            {
                var pack = CatOutfitImport.Validate(folder, incoming, true);
                result.CheckImages(folder, Path.GetDirectoryName(activeOutfit), pack.outfits.Select(row => row.image));
            }
            if (nextEvents != null)
            {
                var pack = OptionalEventImport.Validate(folder, incoming);
                if (pack.pack_id != incoming.pack_id) throw new ArgumentException("任意イベントと素材パックのIDが一致しません。");
                foreach (var art in pack.events.Select(row => row.artwork?[0]).Where(CafeOptionalEvents.IsCustomBackground).Distinct())
                    result.CheckImage(Path.Combine(resources, CafeOptionalEventResources.BackgroundDirectory, art + ".png"), art.Substring(3), false);
            }
            return result;
        }

        private static T ReadIfPresent<T>(string path) where T : class =>
            File.Exists(path) ? JsonUtility.FromJson<T>(File.ReadAllText(path)) ??
                throw new ArgumentException("素材参照が不正です: " + path) : null;

        private static string ExistingFile(string root, string relative)
        {
            var file = Path.Combine(root, relative);
            return File.Exists(file) ? file : null;
        }

        private static string ResourceFile(string resources, string resource, string root, string filename)
        {
            var pattern = @"\A" + Regex.Escape(root) + "/[a-f0-9]{32}/" + Regex.Escape(filename) + @"\z";
            if (!Regex.IsMatch(resource ?? "", pattern)) throw new ArgumentException("現行パックの参照先が不正です: " + resource);
            return Path.Combine(resources, resource + ".json");
        }

        private void CheckImages(string folder, string activeFolder, IEnumerable<string> images)
        {
            foreach (var relative in images.Distinct(StringComparer.Ordinal))
            {
                var digest = Path.GetFileNameWithoutExtension(relative);
                CheckImage(Path.Combine(folder, relative), digest, true);
                CheckImage(Path.Combine(activeFolder, relative), digest, false);
            }
        }

        private void CheckImage(string file, string digest, bool source)
        {
            if (File.Exists(file) && Hash(file) == digest) return;
            if (source) throw new ArgumentException("書き出し画像の欠落またはハッシュ不一致: " + file);
            Add("現行画像", "欠落またはハッシュ不一致 / " + Path.GetFileName(file));
        }

        private string Hash(string file)
        {
            if (file == null) return null;
            if (!hashes.TryGetValue(file, out var digest))
            {
                using (var sha = SHA256.Create())
                using (var stream = File.OpenRead(file))
                    hashes[file] = digest = string.Concat(sha.ComputeHash(stream).Select(b => b.ToString("x2")));
            }
            return digest;
        }

        private void CompareJson(string next, string active, string label)
        {
            if (next == null || active == null)
            {
                if (next != active) Add(label, next == null ? "削除" : "新規登録");
                return;
            }
            CompareValue(ReadDefinition(active), ReadDefinition(next), label);
        }

        private static Dictionary<string, object> ReadDefinition(string path)
        {
            const int limit = 16 * 1024 * 1024;
            if (new FileInfo(path).Length > limit) throw new ArgumentException("素材定義が大きすぎます: " + path);
            var data = CafeOptionalEventJson.ReadValue(File.ReadAllText(path), limit, literals: true) as Dictionary<string, object>;
            if (data == null) throw new ArgumentException("素材定義が不正です: " + path);
            data.Remove("pack_id");
            return data;
        }

        private void CompareValue(object before, object after, string path)
        {
            if (before is Dictionary<string, object> oldMap && after is Dictionary<string, object> newMap)
            {
                foreach (var key in oldMap.Keys.Union(newMap.Keys).OrderBy(key => key, StringComparer.Ordinal))
                {
                    bool had = oldMap.TryGetValue(key, out var oldValue), has = newMap.TryGetValue(key, out var newValue);
                    var field = path + " / " + key;
                    if (!had || !has) Add(field, has ? "追加" : "削除");
                    else CompareValue(oldValue, newValue, field);
                }
            }
            else if (before is object[] oldRows && after is object[] newRows)
            {
                for (int i = 0; i < Math.Max(oldRows.Length, newRows.Length); i++)
                {
                    var location = path + " / " + RowName(i < newRows.Length ? newRows[i] : oldRows[i], i);
                    if (i >= oldRows.Length || i >= newRows.Length) Add(location, i < newRows.Length ? "追加" : "削除");
                    else CompareValue(oldRows[i], newRows[i], location);
                }
            }
            else if (!Equals(before, after))
                Add(path, Display(before) + " → " + Display(after));
        }

        private static string RowName(object value, int index)
        {
            if (value is Dictionary<string, object> row)
                foreach (var key in new[]{"pool_key", "event_id", "customer_id", "game_id", "cat_id", "key", "id", "slot"})
                    if (row.TryGetValue(key, out var name)) return Display(name);
            return (index + 1) + "番";
        }

        private static string Display(object value)
        {
            if (value is Array || value is Dictionary<string, object>) return "定義";
            var text = Convert.ToString(value);
            if (string.IsNullOrEmpty(text)) return "（なし）";
            text = text.Replace("\r", "").Replace("\n", " ");
            return text.Length > 100 ? text.Substring(0, 100) + "…" : text;
        }

        private void Add(string path, string description)
        {
            details.Add(path + "：" + description);
        }
    }
}
