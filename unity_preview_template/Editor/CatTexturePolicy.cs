using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CatCafePreview.Editor
{
    // Only opaque, large, square PNGs in exported cat packs. Original files stay intact.
    public class CatTexturePolicy : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        static HashSet<string> references;
        static void RefreshReferences()
        {
            references = new HashSet<string>(Directory.GetFiles("Assets/Resources/CatCafeContent/Imports", "bundle.json", SearchOption.AllDirectories)
                .SelectMany(b => JsonUtility.FromJson<ContentBundle>(File.ReadAllText(b)).cats)
                .SelectMany(c => c.scenes).Select(s => s.image).Where(s => !string.IsNullOrEmpty(s)), StringComparer.Ordinal);
        }
        internal static bool Eligible(string path)
        {
            if (!path.StartsWith("Assets/Resources/CatCafeContent/Imports/", StringComparison.Ordinal) ||
                !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(Path.GetDirectoryName(path)) != "images") return false;
            string pack = Path.GetDirectoryName(Path.GetDirectoryName(path));
            string bundlePath = pack + "/bundle.json";
            if (!File.Exists(bundlePath)) return false;
            string relative = "images/" + Path.GetFileName(path);
            // Some historical packs retain earlier cat images no longer selected by that
            // pack's bundle. Accept only exact content-hash names referenced by another pack.
            if (references == null) RefreshReferences();
            if (!references.Contains(relative)) return false;
            // RGB PNGs without tRNS are opaque by definition. Avoid creating GPU textures
            // for thousands of identical source checks. Other PNG encodings are decoded.
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length < 33 || bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78 || bytes[3] != 71) return false;
            int width = BigEndian(bytes, 16), height = BigEndian(bytes, 20);
            if (width <= 1024 || width != height) return false;
            bool transparent = false;
            for (int offset = 8; offset + 12 <= bytes.Length;)
            {
                int length = BigEndian(bytes, offset);
                if (length < 0 || length > bytes.Length - offset - 12) return false;
                if (bytes[offset + 4] == 't' && bytes[offset + 5] == 'R' && bytes[offset + 6] == 'N' && bytes[offset + 7] == 'S') transparent = true;
                offset += length + 12;
            }
            if (bytes[25] == 2 && !transparent) return true;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!t.LoadImage(bytes) || t.width <= 1024 || t.width != t.height) return false;
                return t.GetPixels32().All(p => p.a == 255);
            }
            finally { UnityEngine.Object.DestroyImmediate(t); }
        }
        static int BigEndian(byte[] bytes, int offset) => (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        internal static void Configure(TextureImporter importer)
        {
            var s = importer.GetPlatformTextureSettings("Standalone");
            s.overridden = true; s.maxTextureSize = 1024; s.format = TextureImporterFormat.BC7;
            s.compressionQuality = 100; s.crunchedCompression = false;
            importer.SetPlatformTextureSettings(s);
            importer.mipmapEnabled = false; importer.isReadable = false;
        }
        // Called explicitly by the pack registration path, never by a build.
        public static void ApplyPack(string folder)
        {
            RefreshReferences();
            folder = "Assets/" + Path.GetFullPath(folder).Substring(Application.dataPath.Length + 1).Replace('\\', '/');
            foreach (var file in Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories))
            {
                string path = file.Replace('\\', '/');
                if (!Eligible(path)) continue;
                var i = (TextureImporter)AssetImporter.GetAtPath(path);
                if (Matches(i)) continue;
                Configure(i); i.SaveAndReimport();
            }
        }
        internal static bool Matches(TextureImporter i)
        {
            var s = i.GetPlatformTextureSettings("Standalone");
            return s.overridden && s.maxTextureSize == 1024 && s.format == TextureImporterFormat.BC7 &&
                s.compressionQuality == 100 && !s.crunchedCompression && !i.mipmapEnabled && !i.isReadable;
        }
        internal static string[] Targets()
        {
            RefreshReferences();
            return Directory.GetFiles("Assets/Resources/CatCafeContent/Imports", "*.png", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/')).Where(Eligible).OrderBy(p => p).ToArray();
        }
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneWindows64) return;
            Validate();
        }
        public static void Validate()
        {
            var bad = Targets().Where(p => !Matches((TextureImporter)AssetImporter.GetAtPath(p))).ToArray();
            if (bad.Length > 0) throw new BuildFailedException("Cat texture policy mismatch (no settings changed):\n" + string.Join("\n", bad));
        }
        public static void VerifyGuardAndRegistration()
        {
            Validate();
            var paths = Targets();
            var before = paths.ToDictionary(p => p, p => File.ReadAllText(p + ".meta"));
            var i = (TextureImporter)AssetImporter.GetAtPath(paths[0]);
            var original = i.GetPlatformTextureSettings("Standalone");
            bool rejected = false;
            try
            {
                var invalid = i.GetPlatformTextureSettings("Standalone"); invalid.overridden = false; i.SetPlatformTextureSettings(invalid);
                try { Validate(); } catch (BuildFailedException e) { rejected = e.Message.Contains(paths[0]); }
            }
            finally { i.SetPlatformTextureSettings(original); }
            if (!rejected) throw new Exception("Read-only guard did not reject mismatch");
            var pointer = JsonUtility.FromJson<ContentCatalog>(Resources.Load<TextAsset>("CatCafeContent/catalog").text);
            ApplyPack("Assets/Resources/" + pointer.bundleResource.Substring(0, pointer.bundleResource.Length - 7));
            if (paths.Any(p => File.ReadAllText(p + ".meta") != before[p])) throw new Exception("Guard/idempotent registration wrote meta");
            Validate();
            Debug.Log("CAT_CAFE_TEXTURE_GUARD_VERIFIED: mismatch rejected; restored in-memory fixture; registration idempotent; zero meta writes");
        }
        public static void ApplyAll()
        {
            foreach (var p in Targets())
            {
                var i = (TextureImporter)AssetImporter.GetAtPath(p);
                if (Matches(i)) continue;
                Configure(i); i.SaveAndReimport();
            }
            Audit();
        }
        public static void VerifyAndBuild()
        {
            Validate();
            Audit();
            StandaloneScreenSetup.Build();
        }
        public static void Representative()
        {
            var pointer = JsonUtility.FromJson<ContentCatalog>(Resources.Load<TextAsset>("CatCafeContent/catalog").text);
            var bundle = JsonUtility.FromJson<ContentBundle>(Resources.Load<TextAsset>(pointer.bundleResource).text);
            string root = "Assets/Resources/" + pointer.bundleResource.Substring(0, pointer.bundleResource.Length - 6);
            foreach (var c in bundle.cats.Where(c => new[] { "playtest-mike", "playtest-tama", "playtest-sora" }.Contains(c.cat_id)))
            foreach (var s in c.scenes.Where(s => new[] { "normal", "teaser__switch", "teaser__direct", "teaser__open_up" }.Contains(s.key)))
            {
                string p = root + s.image;
                if (!Eligible(p)) throw new Exception("Representative excluded: " + p);
                var i = (TextureImporter)AssetImporter.GetAtPath(p); Configure(i); i.SaveAndReimport();
            }
            Audit();
            ContentTextureAudit.Run();
        }
        public static void Audit()
        {
            string output = Environment.GetEnvironmentVariable("CATCAFE_TEXTURE_OUTPUT");
            Directory.CreateDirectory(output);
            var lines = new List<string> { "path,width,height,format,mips,readable,match,runtimeBytes" };
            foreach (var p in Targets())
            {
                var i = (TextureImporter)AssetImporter.GetAtPath(p); var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                lines.Add(string.Join(",", p, t.width, t.height, t.format, t.mipmapCount, t.isReadable, Matches(i), UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)));
                Resources.UnloadAsset(t);
            }
            File.WriteAllLines(Path.Combine(output, "policy.csv"), lines);
            Debug.Log("CAT_CAFE_TEXTURE_POLICY_AUDIT: " + (lines.Count - 1));
        }
        public static void RepresentativeCanvas()
        {
            string output = Environment.GetEnvironmentVariable("CATCAFE_TEXTURE_OUTPUT"); Directory.CreateDirectory(output);
            ContentTextureAudit.Run();
            var p = UnityEngine.Object.FindObjectOfType<PythonPlayerPanel>();
            var preview = p.GetComponent<ContentPreview>();
            var capture = typeof(NarrativeVerification).GetMethod("Capture", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var pointer = JsonUtility.FromJson<ContentCatalog>(Resources.Load<TextAsset>("CatCafeContent/catalog").text);
            var bundle = JsonUtility.FromJson<ContentBundle>(Resources.Load<TextAsset>(pointer.bundleResource).text);
            string root = pointer.bundleResource.Substring(0, pointer.bundleResource.Length - 6);
            foreach (var c in bundle.cats.Where(c => new[] { "playtest-mike", "playtest-tama", "playtest-sora" }.Contains(c.cat_id)))
            foreach (var s in c.scenes.Where(s => new[] { "normal", "teaser__switch", "teaser__direct", "teaser__open_up" }.Contains(s.key)))
            {
                string resource = root + s.image.Substring(0, s.image.Length - 4);
                var imported = Resources.Load<Texture2D>(resource);
                if (imported.width != 1024 || imported.height != 1024 || imported.format != TextureFormat.BC7 || imported.isReadable || imported.mipmapCount != 1)
                    throw new Exception("Unexpected actual importer output: " + resource);
                var source = new Texture2D(2, 2, TextureFormat.RGBA32, false); source.LoadImage(File.ReadAllBytes("Assets/Resources/" + root + s.image));
                foreach (int width in new[] {1280, 1024})
                foreach (bool actual in new[] {false, true})
                {
                    p.picture.texture = actual ? imported : source;
                    capture.Invoke(null, new object[] {preview, Path.Combine(output, c.cat_id + "-" + s.key + "-" + width + "-" + (actual ? "imported" : "source") + ".png"), width, width == 1280 ? 800 : 768});
                }
                UnityEngine.Object.DestroyImmediate(source);
            }
            Debug.Log("CAT_CAFE_ACTUAL_IMPORT_CANVAS_OK: 12 scenes x 2 resolutions x 2 representations");
        }
    }
}
