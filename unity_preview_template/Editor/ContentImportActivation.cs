using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace CatCafePreview.Editor
{
    // Called on the editor thread after every component has been installed and imported.
    internal sealed class ContentImportActivation
    {
        private sealed class Change
        {
            internal string Path, Pending;
            internal byte[] Content, Previous;
        }

        private readonly List<Change> changes = new List<Change>();

        internal void Add(string path, string json) => AddBytes(path, Encoding.UTF8.GetBytes(json));

        internal void AddBytes(string path, byte[] content)
        {
            changes.Add(new Change { Path = path, Content = content });
        }

        internal bool HasDifference() => changes.Any(change => change.Content == null ? File.Exists(change.Path) :
            !File.Exists(change.Path) || !File.ReadAllBytes(change.Path).SequenceEqual(change.Content));

        internal void Publish()
        {
            var applied = new List<Change>();
            try
            {
                foreach (var change in changes)
                {
                    change.Previous = File.Exists(change.Path) ? File.ReadAllBytes(change.Path) : null;
                    if (change.Content != null)
                    {
                        change.Pending = TemporaryPath(change.Path);
                        File.WriteAllBytes(change.Pending, change.Content);
                    }
                }

                AssetDatabase.DisallowAutoRefresh();
                try
                {
                    foreach (var change in changes)
                    {
                        if (change.Pending == null)
                        {
                            if (change.Previous == null) continue;
                            File.Delete(change.Path);
                        }
                        else Replace(change.Pending, change.Path, change.Previous != null);
                        applied.Add(change);
                    }
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
                catch (Exception error)
                {
                    Rollback(applied, error);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    throw;
                }
                finally { AssetDatabase.AllowAutoRefresh(); }
            }
            finally
            {
                foreach (var change in changes)
                    if (change.Pending != null && File.Exists(change.Pending)) File.Delete(change.Pending);
            }
        }

        private static string TemporaryPath(string path) => path + ".pending-" + Guid.NewGuid().ToString("N") + ".tmp";

        private static void Replace(string source, string target, bool exists)
        {
            if (exists) File.Replace(source, target, null);
            else File.Move(source, target);
        }

        private static void Rollback(List<Change> applied, Exception cause)
        {
            var failures = new List<Exception> { cause };
            for (int i = applied.Count - 1; i >= 0; i--)
            {
                var change = applied[i];
                try
                {
                    if (change.Previous == null)
                    {
                        File.Delete(change.Path);
                        continue;
                    }
                    var fallback = TemporaryPath(change.Path);
                    try
                    {
                        File.WriteAllBytes(fallback, change.Previous);
                        Replace(fallback, change.Path, File.Exists(change.Path));
                    }
                    finally { if (File.Exists(fallback)) File.Delete(fallback); }
                }
                catch (Exception rollbackError) { failures.Add(rollbackError); }
            }
            if (failures.Count > 1)
                throw new AggregateException("素材参照の復元に失敗しました。", failures);
        }
    }
}
