using System;
using UnityEngine;

namespace CatCafePreview
{
    public static class SharedPackImages
    {
        public const string PreviousPack = "cbe0df94e7a5401a8f466406c105b493";
        public const string SharedPack = "ad3e7e84e8c7491fb8aad895e243de4e";
        public const string Root = "CatCafeContent/Imports/";
        public const string ImageFolder = "/images/";

        internal static Texture2D Load(string resource)
        {
            if (string.IsNullOrEmpty(resource)) return null;
            var original = Resources.Load<Texture2D>(resource);
            if (original != null) return original;
            string prefix = Root + PreviousPack + ImageFolder;
            if (!resource.StartsWith(prefix, StringComparison.Ordinal)) return null;
            string hash = resource.Substring(prefix.Length);
            return Standalone.CafeNarrativeCatalog.Hex(hash, 64)
                ? Resources.Load<Texture2D>(Root + SharedPack + ImageFolder + hash)
                : null;
        }
    }
}
