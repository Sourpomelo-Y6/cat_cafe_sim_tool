using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CatCafePreview.Editor
{
    internal static class SharedImageBuildPlan
    {
        private sealed class Image
        {
            internal string Path, Resource, ImportSettings;
            internal long Bytes;
        }

        private static string Project => Path.GetDirectoryName(Application.dataPath);
        private static string Content => Path.Combine(Application.dataPath, "Resources", "CatCafeContent", "Imports");
        private static string Stash => Path.Combine(Project, "UserSettings", "CatCafeSharedImagePlan");
        private static string PlanFile => Path.Combine(Stash, "aliases.json");
        private static string Published => Path.Combine(Application.dataPath, "Resources", SharedPackImages.AliasResource + ".json");
        private static readonly Regex PackId = new Regex(@"\A[a-f0-9]{32}\z");

        private static SharedPackImages.AliasCatalog Create()
        {
            var images = new List<Image>();
            RequireLocalPath(Content);
            using (var sha = System.Security.Cryptography.SHA256.Create())
                foreach (var pack in Directory.GetDirectories(Content).Where(path => PackId.IsMatch(Path.GetFileName(path))))
                {
                    string folder = Path.Combine(pack, "images");
                    if (!Directory.Exists(folder)) continue;
                    RequireLocalPath(folder);
                    foreach (var file in Directory.GetFiles(folder, "*.png"))
                    {
                        string hash = Path.GetFileNameWithoutExtension(file);
                        if (!Standalone.CafeNarrativeCatalog.Hex(hash, 64)) continue;
                        RequireLocalPath(file);
                        RequireLocalPath(file + ".meta");
                        string digest;
                        using (var input = File.OpenRead(file))
                            digest = string.Concat(sha.ComputeHash(input).Select(b => b.ToString("x2")));
                        if (hash != digest) continue;
                        string resource = SharedPackImages.Root + Path.GetFileName(pack) + SharedPackImages.ImageFolder + hash;
                        images.Add(new Image { Path = "Assets/Resources/" + resource + ".png", Resource = resource,
                            Bytes = new FileInfo(file).Length, ImportSettings = string.Join("\n", File.ReadLines(file + ".meta")
                                .Where(line => !line.StartsWith("guid: ", StringComparison.Ordinal))) });
                    }
                }

            var candidates = new HashSet<string>(images.Select(image => image.Path), StringComparer.Ordinal);
            // Anything that can hold a Unity GUID reference stays a root for dependency tracing.
            var roots = AssetDatabase.GetAllAssetPaths().Where(path => path.StartsWith("Assets/", StringComparison.Ordinal)
                && !candidates.Contains(path) && !AssetDatabase.IsValidFolder(path)).ToArray();
            var direct = new HashSet<string>(AssetDatabase.GetDependencies(roots, true), StringComparer.Ordinal);
            var pointer = JsonUtility.FromJson<ContentCatalog>(File.ReadAllText(Path.Combine(Application.dataPath, "Resources", "CatCafeContent", "catalog.json")));
            string active = (pointer?.bundleResource ?? "").EndsWith("/bundle", StringComparison.Ordinal)
                ? pointer.bundleResource.Substring(0, pointer.bundleResource.Length - "bundle".Length) : "";
            var aliases = new List<SharedPackImages.Alias>();
            foreach (var group in images.GroupBy(image => (Path.GetFileName(image.Path), image.ImportSettings)))
            {
                var source = group.OrderByDescending(image => image.Resource.StartsWith(active, StringComparison.Ordinal) && active.Length > 0)
                    .ThenByDescending(image => direct.Contains(image.Path)).ThenBy(image => image.Resource, StringComparer.Ordinal).First();
                foreach (var image in group)
                {
                    if (image == source || direct.Contains(image.Path)) continue;
                    aliases.Add(new SharedPackImages.Alias { from = image.Resource, to = source.Resource, sourceBytes = image.Bytes });
                }
            }
            return new SharedPackImages.AliasCatalog { aliases = aliases.OrderBy(alias => alias.from, StringComparer.Ordinal).ToArray() };
        }

        internal static T Run<T>(Func<T> build)
        {
            var plan = Create();
            string workspace = Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT");
            if (string.IsNullOrEmpty(workspace)) throw new InvalidOperationException("Missing verification log root.");
            string logs = Path.Combine(workspace, "Logs", "StandaloneBuild", "Runs");
            Directory.CreateDirectory(logs);
            string report = Path.Combine(logs, "SharedImagePlan-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".tsv");
            File.WriteAllLines(report, new[]{"previous_resource\tshared_resource\tsource_png_bytes"}
                .Concat(plan.aliases.Select(alias => alias.from + "\t" + alias.to + "\t" + alias.sourceBytes)));
            Debug.Log("CATCAFE_SHARED_IMAGE_PLAN: " + plan.aliases.Length + " images; " + report);
            if (plan.aliases.Length == 0) return build();

            RequireLocalPath(Stash);
            RequireLocalPath(Published);
            if (Directory.Exists(Stash) || File.Exists(Published) || File.Exists(Published + ".meta"))
                throw new IOException("Shared image staging paths are already in use.");
            string json = JsonUtility.ToJson(plan);
            try
            {
                Directory.CreateDirectory(Stash);
                File.WriteAllText(PlanFile, json);
                WithRefresh(() =>
                {
                    foreach (var alias in plan.aliases)
                    {
                        string from = AssetPath(alias.from);
                        string staged = StagedPath(alias.from);
                        Directory.CreateDirectory(Path.GetDirectoryName(staged));
                        File.Move(from + ".meta", staged + ".meta");
                        File.Move(from, staged);
                    }
                    var pending = Path.Combine(Stash, "publish.tmp");
                    File.WriteAllText(pending, json);
                    File.Move(pending, Published);
                });
                return build();
            }
            finally { Recover(); }
        }

        internal static void Recover()
        {
            if (!Directory.Exists(Stash)) return;
            RequireLocalPath(Stash);
            RequireLocalPath(PlanFile);
            RequireLocalPath(Published);
            RequireLocalPath(Published + ".meta");
            if (!File.Exists(PlanFile)) { Directory.Delete(Stash); return; }
            string json = File.ReadAllText(PlanFile);
            var plan = JsonUtility.FromJson<SharedPackImages.AliasCatalog>(json);
            if (plan?.aliases == null || plan.aliases.Any(alias => !SharedPackImages.ValidAlias(alias)))
                throw new IOException("Shared image recovery plan is incomplete.");
            WithRefresh(() =>
            {
                if (File.Exists(Published))
                {
                    if (File.ReadAllText(Published) != json) throw new IOException("Generated alias file has changed.");
                    if (File.Exists(Published + ".meta")) File.Delete(Published + ".meta");
                    File.Delete(Published);
                }
                foreach (var alias in plan.aliases)
                {
                    string staged = StagedPath(alias.from);
                    string original = AssetPath(alias.from);
                    RequireLocalPath(staged);
                    RequireLocalPath(staged + ".meta");
                    RequireLocalPath(original);
                    RequireLocalPath(original + ".meta");
                    if (File.Exists(staged)) File.Move(staged, original);
                    if (File.Exists(staged + ".meta")) File.Move(staged + ".meta", original + ".meta");
                }
                string pending = Path.Combine(Stash, "publish.tmp");
                if (File.Exists(pending)) File.Delete(pending);
                // Only now discard the plan. Empty directories are owned by this build.
                foreach (var parent in plan.aliases.Select(alias => Path.GetDirectoryName(StagedPath(alias.from))).Distinct())
                    if (Directory.Exists(parent)) Directory.Delete(parent);
                foreach (var parent in plan.aliases.Select(alias => Path.GetDirectoryName(Path.GetDirectoryName(StagedPath(alias.from)))).Distinct())
                    if (Directory.Exists(parent)) Directory.Delete(parent);
                File.Delete(PlanFile);
                Directory.Delete(Stash);
            });
            Debug.Log("CATCAFE_SHARED_IMAGE_PLAN_RESTORED");
        }

        private static string AssetPath(string resource) => Path.Combine(Application.dataPath, "Resources", resource + ".png");
        private static string StagedPath(string resource) => Path.Combine(Stash, resource.Substring(SharedPackImages.Root.Length) + ".png");

        private static void RequireLocalPath(string path)
        {
            string resolved = Path.GetFullPath(path);
            if (!resolved.StartsWith(Path.GetFullPath(Project) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Shared image path escaped the Unity project: " + path);
            for (string candidate = resolved; !string.IsNullOrEmpty(candidate); candidate = Path.GetDirectoryName(candidate))
                if ((Directory.Exists(candidate) || File.Exists(candidate))
                    && (File.GetAttributes(candidate) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Shared image path is a link: " + candidate);
        }

        private static void WithRefresh(Action action)
        {
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                action();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            finally { AssetDatabase.AllowAutoRefresh(); }
        }
    }
}
