using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace CatCafePreview.Editor
{
    internal static class SharedPackBuildImages
    {
        private static string Project => Path.GetDirectoryName(Application.dataPath);
        private static string Source => Path.GetFullPath(Path.Combine(Application.dataPath, "Resources", SharedPackImages.Root, SharedPackImages.PreviousPack, "images"));
        private static string Stash => Path.GetFullPath(Path.Combine(Project, "UserSettings", "CatCafeSharedPackBuild"));

        internal static void Recover()
        {
            if (!Directory.Exists(Stash)) return;
            VerifyPaths();
            MoveWithoutAutoRefresh(() =>
            {
                string saved = Path.Combine(Stash, "images");
                string savedMeta = Path.Combine(Stash, "images.meta");
                if (Directory.Exists(saved)) Directory.Move(saved, Source);
                if (File.Exists(savedMeta)) File.Move(savedMeta, Source + ".meta");
                Directory.Delete(Stash);
            });
            Debug.Log("CATCAFE_SHARED_PACK_SOURCE_RESTORED");
        }

        internal static T Run<T>(string[] scenes, Func<T> build)
        {
            VerifyPaths();
            ValidatePair(scenes);
            try
            {
                MoveWithoutAutoRefresh(() =>
                {
                    Directory.CreateDirectory(Stash);
                    File.Move(Source + ".meta", Path.Combine(Stash, "images.meta"));
                    Directory.Move(Source, Path.Combine(Stash, "images"));
                });
                Debug.Log("CATCAFE_SHARED_PACK_SOURCE_STAGED: 860 PNGs");
                return build();
            }
            finally { Recover(); }
        }

        private static void MoveWithoutAutoRefresh(Action move)
        {
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                move();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            finally { AssetDatabase.AllowAutoRefresh(); }
        }

        private static void VerifyPaths()
        {
            if (!Source.StartsWith(Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || !Stash.StartsWith(Path.GetFullPath(Project) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Shared-pack paths escaped the Unity project.");
            foreach (var path in new[]{Source, Source + ".meta", Stash, Path.Combine(Stash, "images"), Path.Combine(Stash, "images.meta")})
                RejectLinks(path);
        }

        private static void RejectLinks(string path)
        {
            for (string current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
                if ((Directory.Exists(current) || File.Exists(current))
                    && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("A shared-pack path is a link: " + current);
        }

        private static void ValidatePair(string[] scenes)
        {
            string target = Path.Combine(Application.dataPath, "Resources", SharedPackImages.Root, SharedPackImages.SharedPack, "images");
            RejectLinks(target);
            var previous = Directory.GetFiles(Source, "*.png").OrderBy(Path.GetFileName, StringComparer.Ordinal).ToArray();
            var current = Directory.GetFiles(target, "*.png").Select(Path.GetFileName).OrderBy(name => name, StringComparer.Ordinal);
            if (previous.Length != 860 || Directory.GetDirectories(Source).Length != 0
                || Directory.GetFiles(Source).Length != 2 * previous.Length
                || !previous.Select(Path.GetFileName).SequenceEqual(current))
                throw new InvalidOperationException("The two shared-image packs no longer have the verified matching layout.");

            string prefix = "Assets/Resources/" + SharedPackImages.Root + SharedPackImages.PreviousPack + SharedPackImages.ImageFolder;
            if (AssetDatabase.GetDependencies(scenes, true).Any(path => path.StartsWith(prefix, StringComparison.Ordinal)))
                throw new InvalidOperationException("A build scene directly references a staged image.");

            using (var sha = SHA256.Create())
                foreach (var file in previous)
                {
                    string other = Path.Combine(target, Path.GetFileName(file));
                    foreach (var path in new[]{file, other, file + ".meta", other + ".meta"}) RejectLinks(path);
                    string expected = Path.GetFileNameWithoutExtension(file);
                    if (Digest(sha, file) != expected || Digest(sha, other) != expected
                        || !ImporterLines(file).SequenceEqual(ImporterLines(other)))
                        throw new InvalidOperationException("Image content or Unity import settings differ: " + file);
                }
        }

        private static string Digest(SHA256 sha, string path)
        {
            using (var stream = File.OpenRead(path))
                return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
        }

        private static string[] ImporterLines(string image) => File.ReadAllLines(image + ".meta")
            .Where(line => !line.StartsWith("guid: ", StringComparison.Ordinal)).ToArray();
    }
}
