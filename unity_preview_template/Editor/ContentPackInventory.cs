using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CatCafePreview.Editor
{
    internal sealed class ContentPackInventory
    {
        internal sealed class Pack
        {
            internal string Path;
            internal long? Bytes;
            internal bool Current, Previous;
            internal readonly HashSet<string> Saves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            internal bool HasReference => Current || Previous || Saves.Count > 0;
        }

        private static readonly string[] Roots = {
            "CatCafeContent/Imports", "CatCafeContent/People", "CatCafeEvents/Imports",
            "CatCafeStateImages/Imports", "CatCafeOutfits/Imports"
        };
        private static readonly Regex Id = new Regex(@"\A[a-f0-9]{32}\z");
        private static readonly Regex Resource = new Regex(@"\A(?<pack>(?:" +
            string.Join("|", Roots.Select(Regex.Escape)) + @")/[a-f0-9]{32})/[^""\s\\]+\z");
        private static readonly Regex SaveFile = new Regex(@"\A(?:slot-[0-9]+\.json(?:\.bak)?|extra(?:-images)?-v1\.json(?:\.bak)?)\z",
            RegexOptions.IgnoreCase);
        private static readonly Regex SavedResource = new Regex(@"""(?<resource>CatCafe[^""\s\\]*)""");
        private readonly Dictionary<string, Pack> packs = new Dictionary<string, Pack>(StringComparer.Ordinal);
        internal readonly List<string> Warnings = new List<string>();
        internal int ScannedSaves { get; private set; }
        internal bool HasPrevious { get; private set; }
        internal Pack[] Rows => packs.Values.OrderByDescending(pack => pack.Bytes ?? -1)
            .ThenBy(pack => pack.Path, StringComparer.Ordinal).ToArray();

        internal static ContentPackInventory Read(string resources, string historyPath, string saveFolder)
        {
            var result = new ContentPackInventory();
            foreach (var root in Roots)
            {
                string directory = Path.Combine(resources, root);
                if (!Directory.Exists(directory)) continue;
                result.TryRead(directory, () =>
                {
                    var rootFolder = new DirectoryInfo(directory);
                    if ((rootFolder.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        result.Warnings.Add("リンク先を省略: " + directory);
                        return;
                    }
                    foreach (var folder in rootFolder.EnumerateDirectories().Where(d => Id.IsMatch(d.Name)))
                    {
                        var pack = result.Get(root + "/" + folder.Name);
                        result.TryRead(folder.FullName, () =>
                        {
                            long total = 0;
                            if (result.Walk(folder, file => total += file.Length)) pack.Bytes = total;
                        });
                    }
                });
            }
            result.TryRead("現行設定", () => result.ReadConfiguration(ContentImportHistory.Capture(resources), previous: false));
            result.TryRead("直前の復元先", () =>
            {
                var previous = ContentImportHistory.ReadPreviousIfPresent(historyPath);
                result.HasPrevious = previous != null;
                if (previous != null) result.ReadConfiguration(previous, previous: true);
            });
            if (!string.IsNullOrEmpty(saveFolder))
                result.Walk(new DirectoryInfo(saveFolder), file =>
                {
                    if (!SaveFile.IsMatch(file.Name)) return;
                    if (file.Length > 64L * 1024 * 1024)
                    {
                        result.Warnings.Add("64 MiB を超えるセーブを省略: " + file.FullName);
                        return;
                    }
                    result.TryRead(file.FullName, () =>
                    {
                        var json = File.ReadAllText(file.FullName);
                        foreach (Match match in SavedResource.Matches(json))
                            result.MarkResource(match.Groups["resource"].Value, pack => pack.Saves.Add(file.FullName));
                        result.ScannedSaves++;
                    });
                });
            return result;
        }

        private void ReadConfiguration(ContentImportHistory.Snapshot snapshot, bool previous)
        {
            var pointers = ContentImportHistory.PointerTexts(snapshot);
            void Mark(Pack pack)
            {
                if (previous) pack.Previous = true;
                else pack.Current = true;
            }

            // Each pointer is independent. A malformed sidecar must not hide the others.
            var label = previous ? "直前の復元先" : "現行設定";
            TryRead(label + " / 通常素材・交流文", () =>
            {
                var catalog = Parse<ContentCatalog>(pointers[0]);
                MarkResource(catalog?.bundleResource, Mark, strict: true);
                MarkResource(catalog?.interactionResource, Mark, strict: true);
            });
            TryRead(label + " / 人物画像", () =>
            {
                var people = Parse<HumanPortraitPack>(pointers[1]);
                if (people == null) return;
                if (!Id.IsMatch(people.pack_id ?? "")) throw new ArgumentException("人物画像のパックIDが不正です。");
                Mark(Get("CatCafeContent/People/" + people.pack_id));
            });
            TryRead(label + " / 任意イベント", () => MarkResource(Parse<OptionalEventPointer>(pointers[2])?.eventResource, Mark, strict: true));
            TryRead(label + " / 状態画像", () => MarkResource(Parse<CatStateImagePointer>(pointers[3])?.resource, Mark, strict: true));
            TryRead(label + " / 見た目画像", () => MarkResource(Parse<CatOutfitPointer>(pointers[4])?.resource, Mark, strict: true));
        }

        private static T Parse<T>(string json) where T : class =>
            json == null ? null : JsonUtility.FromJson<T>(json) ?? throw new ArgumentException("素材参照を読めません。");

        private void MarkResource(string resource, Action<Pack> mark, bool strict = false)
        {
            if (string.IsNullOrEmpty(resource)) return;
            var match = Resource.Match(resource);
            if (match.Success) mark(Get(match.Groups["pack"].Value));
            else if (strict) throw new ArgumentException("素材の参照先が不正です: " + resource);
        }

        private Pack Get(string path)
        {
            if (!packs.TryGetValue(path, out var pack))
                packs.Add(path, pack = new Pack { Path = path });
            return pack;
        }

        private bool Walk(DirectoryInfo folder, Action<FileInfo> visit)
        {
            bool complete = false;
            TryRead(folder.FullName, () =>
            {
                if ((folder.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    Warnings.Add("リンク先を省略: " + folder.FullName);
                    return;
                }
                complete = true;
                foreach (var file in folder.EnumerateFiles())
                {
                    if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        Warnings.Add("リンク先を省略: " + file.FullName);
                        complete = false;
                        continue;
                    }
                    visit(file);
                }
                foreach (var child in folder.EnumerateDirectories())
                    if (!Walk(child, visit)) complete = false;
            }, () => complete = false);
            return complete;
        }

        private void TryRead(string source, Action read, Action onFailure = null)
        {
            try { read(); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                                           error is ArgumentException || error is FormatException)
            {
                Warnings.Add(source + ": " + error.Message);
                onFailure?.Invoke();
            }
        }
    }

    public sealed class ContentPackInventoryWindow : EditorWindow
    {
        private ContentPackInventory inventory;
        private string saveFolder;
        private Vector2 scroll;
        private readonly HashSet<string> expanded = new HashSet<string>(StringComparer.Ordinal);

        [MenuItem("Cat Cafe/素材/素材パックの容量と参照状況", false, 2)]
        private static void Open()
        {
            var window = GetWindow<ContentPackInventoryWindow>("素材パック一覧");
            window.minSize = new Vector2(610, 380);
            window.Refresh();
        }

        private void Refresh()
        {
            inventory = ContentPackInventory.Read(Path.Combine(Application.dataPath, "Resources"), ContentImportHistory.HistoryPath, saveFolder);
            Repaint();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("旧セーブのフォルダを選ぶ…"))
                {
                    var folder = EditorUtility.OpenFolderPanel("確認する旧セーブのフォルダ", saveFolder ?? Application.persistentDataPath, "");
                    if (folder != "") { saveFolder = folder; Refresh(); }
                }
                if (GUILayout.Button("選択を解除")) { saveFolder = null; Refresh(); }
                if (GUILayout.Button("再集計")) Refresh();
            }
            if (inventory == null) Refresh();
            var rows = inventory.Rows;
            EditorGUILayout.LabelField($"容量を集計できたパック {rows.Count(row => row.Bytes.HasValue)} 件 / {Size(rows.Sum(row => row.Bytes ?? 0))}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(inventory.HasPrevious ? "直前の復元先を確認済み" : "直前の復元先の記録なし");
            EditorGUILayout.LabelField(saveFolder == null ? "旧セーブの確認なし" :
                $"{saveFolder}  /  確認したファイル {inventory.ScannedSaves} 件", EditorStyles.wordWrappedLabel);
            EditorGUILayout.HelpBox("容量は Unity プロジェクト内のファイルの合計です。旧セーブは選択フォルダ内のスロットと回想・画像記録の明示参照を確認します。「参照未確認」には未調査の参照が含まれる場合があります。", MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var row in rows)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var references = new List<string>();
                    if (row.Current) references.Add("現行");
                    if (row.Previous) references.Add("直前の復元先");
                    if (row.Saves.Count > 0) references.Add($"旧セーブ・記録 {row.Saves.Count} 件");
                    EditorGUILayout.LabelField(row.Path, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField((row.Bytes.HasValue ? Size(row.Bytes.Value) : "容量不明／素材が見つからない") +
                        "  /  " + (row.HasReference ? string.Join("、", references) : "参照未確認"), EditorStyles.wordWrappedLabel);
                    if (row.Saves.Count == 0) continue;
                    if (EditorGUILayout.Foldout(expanded.Contains(row.Path), "参照元を表示", true))
                    {
                        expanded.Add(row.Path);
                        foreach (var file in row.Saves.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                            EditorGUILayout.LabelField(file, EditorStyles.wordWrappedLabel);
                    }
                    else expanded.Remove(row.Path);
                }
            }
            foreach (var warning in inventory.Warnings)
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            EditorGUILayout.EndScrollView();
        }

        private static string Size(long bytes)
        {
            if (bytes < 1024) return bytes.ToString(CultureInfo.InvariantCulture) + " B";
            if (bytes < 1024L * 1024) return (bytes / 1024.0).ToString("0.0", CultureInfo.InvariantCulture) + " KiB";
            if (bytes < 1024L * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("0.0", CultureInfo.InvariantCulture) + " MiB";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("0.00", CultureInfo.InvariantCulture) + " GiB";
        }
    }
}
