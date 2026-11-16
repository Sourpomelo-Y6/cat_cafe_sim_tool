using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CatCafePreview
{
    public static class SharedPackImages
    {
        public const string PreviousPack = "cbe0df94e7a5401a8f466406c105b493";
        public const string SharedPack = "ad3e7e84e8c7491fb8aad895e243de4e";
        public const string Root = "CatCafeContent/Imports/";
        public const string ImageFolder = "/images/";

        public const string AliasResource = "CatCafeSharedPackAliases";

        [Serializable] public sealed class Alias
        {
            public string from, to;
            public long sourceBytes;
        }

        [Serializable] public sealed class AliasCatalog { public Alias[] aliases; }

        private static readonly Regex ImageResource = new Regex(@"\ACatCafeContent/Imports/[a-f0-9]{32}/images/[a-f0-9]{64}\z");
        private static Dictionary<string,string> aliases;

        public static bool ValidAlias(Alias alias) => alias != null && alias.from != alias.to
            && ImageResource.IsMatch(alias.from ?? "") && ImageResource.IsMatch(alias.to ?? "")
            && alias.from.Substring(alias.from.Length - 64) == alias.to.Substring(alias.to.Length - 64);

        internal static IReadOnlyDictionary<string,string> Aliases
        {
            get
            {
                if (aliases != null) return aliases;
                var map = new Dictionary<string,string>(StringComparer.Ordinal);
                var asset = Resources.Load<TextAsset>(AliasResource);
                if (asset != null)
                {
                    var catalog = JsonUtility.FromJson<AliasCatalog>(asset.text);
                    if (catalog?.aliases != null)
                        foreach (var alias in catalog.aliases)
                            if (ValidAlias(alias)) map.Add(alias.from, alias.to);
                }
                aliases = map;
                return aliases;
            }
        }

        internal static Texture2D Load(string resource)
        {
            if (string.IsNullOrEmpty(resource)) return null;
            var original = Resources.Load<Texture2D>(resource);
            if (original != null) return original;
            if (Aliases.TryGetValue(resource, out var shared)) return Resources.Load<Texture2D>(shared);
            string prefix = Root + PreviousPack + ImageFolder;
            if (!resource.StartsWith(prefix, StringComparison.Ordinal)) return null;
            string hash = resource.Substring(prefix.Length);
            return Standalone.CafeNarrativeCatalog.Hex(hash, 64)
                ? Resources.Load<Texture2D>(Root + SharedPack + ImageFolder + hash)
                : null;
        }
    }
}
