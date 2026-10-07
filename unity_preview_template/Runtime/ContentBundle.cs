using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CatCafePreview
{
    [Serializable] public class ContentCatalog { public string bundleResource; }
    [Serializable] public class ContentBundle { public int version; public string pack_id; public CatContent[] cats; }
    [Serializable] public class CatContent { public string cat_id; public string name; public string introduction; public string[] features; public CatAppearance appearance; public SceneContent[] scenes; }
    [Serializable] public class CatAppearance { public string eye_color; public string body_type; public string markings; public string face_shape; public string tail; public string notes; }
    [Serializable] public class SceneContent { public string key; public string label; public string text; public string image; }

    public static class ContentValidation
    {
        public static readonly string[] SceneKeys = { "normal", "play", "pet", "sleep", "teaser", "ball", "plush", "tunnel", "brush", "voice", "presence", "pause" };
        public static bool ValidSceneKey(string key)
        {
            if (Array.IndexOf(SceneKeys, key) >= 0) return true;
            if (string.IsNullOrEmpty(key)) return false;
            var parts = key.Split(new[] { "__" }, StringSplitOptions.None);
            if (parts.Length != 2 || Array.IndexOf(new[] { "teaser", "ball", "plush", "tunnel", "pet", "brush", "voice", "presence" }, parts[0]) < 0) return false;
            if (parts[1] == "intense") return parts[0] == "teaser" || parts[0] == "ball" || parts[0] == "tunnel";
            if (parts[1] == "feint") return parts[0] == "teaser" || parts[0] == "ball" || parts[0] == "plush" || parts[0] == "tunnel";
            return Array.IndexOf(new[] { "direct", "adapt", "pause", "switch", "connect" }, parts[1]) >= 0;
        }
        public static void Validate(ContentBundle bundle)
        {
            if (bundle == null || bundle.version != 1 || !Regex.IsMatch(bundle.pack_id ?? "", "^[a-f0-9]{32}$")
                || bundle.cats == null || bundle.cats.Length == 0)
                throw new ArgumentException("Invalid content bundle header.");
            var ids = new HashSet<string>();
            foreach (var cat in bundle.cats)
            {
                if (cat == null || string.IsNullOrWhiteSpace(cat.cat_id) || !ids.Add(cat.cat_id)
                    || string.IsNullOrWhiteSpace(cat.name) || string.IsNullOrWhiteSpace(cat.introduction)
                    || cat.introduction.Length > 2000 || cat.scenes == null || cat.scenes.Length < 4)
                    throw new ArgumentException("Invalid cat content.");
                var keys = new HashSet<string>();
                foreach (var scene in cat.scenes)
                {
                    if (scene == null || !ValidSceneKey(scene.key) || !keys.Add(scene.key)
                        || string.IsNullOrWhiteSpace(scene.label) || string.IsNullOrWhiteSpace(scene.text)
                        || scene.text.Length > 500 || scene.image == null
                        || (scene.image != "" && !Regex.IsMatch(scene.image, "^images/[a-f0-9]{64}\\.png$")))
                        throw new ArgumentException("Invalid scene content or image path.");
                }
                foreach (var required in new[] { "normal", "play", "pet", "sleep" })
                    if (!keys.Contains(required)) throw new ArgumentException("Missing base scene.");
            }
        }
    }
}
