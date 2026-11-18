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

        // Saved image receipts keep their original paths. Only these retired images are redirected.
        private static readonly Dictionary<string,string> RetiredImages = new Dictionary<string,string>(StringComparer.Ordinal)
        {
            // playtest-mugi/normal
            { "CatCafeContent/Imports/bba09bb3938148c2b29d74af70d7e849/images/0a2a5bc467f58afd8006c02bb1dd49cec1377acc150eda3427be06cc9f040304",
              "CatCafeContent/Imports/ad3e7e84e8c7491fb8aad895e243de4e/images/3278e8f2e625f2cb4174766a65098e5a626989c68095fc6a881f7edc7b246c1a" },
            // playtest-mike/normal
            { "CatCafeContent/Imports/bba09bb3938148c2b29d74af70d7e849/images/40e97c757613942c2bbc8d7e9606e7f0cca4ac60315b2243b664a7d0c0ae2efb",
              "CatCafeContent/Imports/ad3e7e84e8c7491fb8aad895e243de4e/images/341239d6351314493ee21e28f46ff362eb43f945f01a866751851985f68a2719" },
            // playtest-tama/normal
            { "CatCafeContent/Imports/bba09bb3938148c2b29d74af70d7e849/images/5b2792589e8dd466cbabc1947fd5a06829e0dfd41ffdbbc99b34f00863c13e21",
              "CatCafeContent/Imports/ad3e7e84e8c7491fb8aad895e243de4e/images/99a0df52628d4fcbc0d20479a02e9d592ed25e74e8a8f2266cae8ac803e80d93" },
            // playtest-mike/pet
            { "CatCafeContent/Imports/bba09bb3938148c2b29d74af70d7e849/images/7ec070420926a0f7f36b2077072a15d5243447cc6c262ac7a00d30f4c3f4bbac",
              "CatCafeContent/Imports/ad3e7e84e8c7491fb8aad895e243de4e/images/43a1925b8977ae102b3ede796b2ec9720c7c02cda70080ff688b93e06d200b8d" },
            // playtest-mike/sleep
            { "CatCafeContent/Imports/bba09bb3938148c2b29d74af70d7e849/images/94e93fa6d4760b078aa9eb6abb7cfb7dbabdcdacc04ae7ebc147b01c33977fa0",
              "CatCafeContent/Imports/ad3e7e84e8c7491fb8aad895e243de4e/images/3a553669997d76e08be1889e94a0b7f647e4570a289ef907fc81a03193d84bbb" },
            // playtest-sora/normal
            { "CatCafeContent/Imports/bba09bb3938148c2b29d74af70d7e849/images/a160aedfd1348c482fc3b21d450826d9d2ff9d958d9666663a2a0e27f158b988",
              "CatCafeContent/Imports/ad3e7e84e8c7491fb8aad895e243de4e/images/45f74d32f7bc44a7e7691d52d9e3537719077196cef50dbd2ad91074dd2f8938" },
            // playtest-mike/play
            { "CatCafeContent/Imports/bba09bb3938148c2b29d74af70d7e849/images/d5f573292853b1ec87b08de315a6bd276534fcf6d01896ba577175f759845ba0",
              "CatCafeContent/Imports/ad3e7e84e8c7491fb8aad895e243de4e/images/2569dc55c1ed6dd954d8f601bb23bb88faac5b0a8b83e921499c9b64ace11a93" },
            // playtest-kohaku/normal
            { "CatCafeContent/Imports/bba09bb3938148c2b29d74af70d7e849/images/fab7ebb8375fcda7c0a08331cb793d3e3eb10eb11925e4e4406b11329d79efb2",
              "CatCafeContent/Imports/ad3e7e84e8c7491fb8aad895e243de4e/images/b29ca453a3929bb3edb658bf3e68af05765153a7c5bae296d249ca76dafb9150" },
            // guest-1
            { "CatCafeContent/People/be5a0fcda4ecbc7da8f33775604e369e/people/7f80e3ded51f662510c53514f2f5a1e0440b04450eef9a448e8e9ea68799a8f5",
              "CatCafeContent/People/ad3e7e84e8c7491fb8aad895e243de4e/people/0c815814fbaa02bf25092e99e7bc57ed858a9270e52bb679d97bd8117400b7f7" },
        };

        internal static Texture2D Load(string resource)
        {
            if (string.IsNullOrEmpty(resource)) return null;
            if (RetiredImages.TryGetValue(resource, out var replacement)) resource = replacement;
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
