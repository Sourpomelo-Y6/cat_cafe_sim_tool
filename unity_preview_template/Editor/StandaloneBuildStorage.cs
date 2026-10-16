using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CatCafePreview.Editor
{
    // All verification entry points share one output, even with legacy environment variables.
    public static class StandaloneBuildStorage
    {
        public static void Build(string[] scenes)
        {
            string workspace = Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT");
            string legacy = Environment.GetEnvironmentVariable("CATCAFE_STANDALONE_BUILD");
            if (string.IsNullOrEmpty(workspace))
            {
                if (string.IsNullOrEmpty(legacy)) return; // Existing verification-only calls.
                for (var parent = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(legacy))); parent != null; parent = parent.Parent)
                    if (parent.Name.Equals("Logs", StringComparison.OrdinalIgnoreCase))
                    {
                        workspace = parent.Parent.FullName;
                        break;
                    }
            }
            if (string.IsNullOrEmpty(workspace) || !Directory.Exists(Path.Combine(workspace, "cat_cafe_sim_tool")))
                throw new InvalidOperationException("Set CATCAFE_WORKSPACE_ROOT to the CatCafeSim source workspace. Use build_standalone.ps1.");

            string root = Path.Combine(Path.GetFullPath(workspace), "Logs", "StandaloneBuild");
            Directory.CreateDirectory(root);
            RejectLink(root);
            // An exclusive lock prevents concurrent builds from replacing each other's output.
            using (var buildLock = new FileStream(Path.Combine(root, "build.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                string pending = Path.Combine(root, "Pending");
                string latest = Path.Combine(root, "Latest");
                string previous = Path.Combine(root, "Previous");
                RejectLink(pending); RejectLink(latest); RejectLink(previous);
                // Recover an interrupted promotion before starting another build.
                if (Directory.Exists(previous) && !Directory.Exists(latest)) Directory.Move(previous, latest);
                DeleteOutput(previous);
                DeleteOutput(pending);
                try
                {
                    Directory.CreateDirectory(pending);
                    var report = BuildPipeline.BuildPlayer(scenes, Path.Combine(pending, "CatCafeStandalone.exe"), BuildTarget.StandaloneWindows64, BuildOptions.None);
                    if (report.summary.result != BuildResult.Succeeded)
                        throw new Exception("Standalone build failed: " + report.summary.result);
                    if (!File.Exists(Path.Combine(pending, "CatCafeStandalone.exe")))
                        throw new IOException("Successful build did not produce CatCafeStandalone.exe");
                    if (Directory.Exists(latest)) Directory.Move(latest, previous);
                    try { Directory.Move(pending, latest); }
                    catch
                    {
                        if (Directory.Exists(previous)) Directory.Move(previous, latest);
                        throw;
                    }
                    DeleteOutput(previous);
                    Debug.Log("CAT_CAFE_STANDALONE_BUILD_OK: " + Path.Combine(latest, "CatCafeStandalone.exe"));
                }
                finally { DeleteOutput(pending); }
            }
        }

        private static void RejectLink(string path)
        {
            if (Directory.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Build output must not be a link: " + path);
        }

        private static void DeleteOutput(string path)
        {
            RejectLink(path);
            if (!Directory.Exists(path)) return;
            foreach (string child in Directory.GetDirectories(path)) DeleteOutput(child);
            foreach (string file in Directory.GetFiles(path)) File.Delete(file);
            Directory.Delete(path);
        }
    }
}
