using System;
using System.Text.RegularExpressions;
using CatCafePreview.Standalone;
using UnityEngine;
namespace CatCafePreview
{
    public static class CafeNarrativeResources
    {
        public static CafeNarrativeCatalog Load()
        {
            var metadata=Resources.Load<TextAsset>("CatCafeContent/catalog");if(metadata==null)return null;
            var pointer=JsonUtility.FromJson<ContentCatalog>(metadata.text);
            if(string.IsNullOrEmpty(pointer?.interactionResource))return null;
            if(!Regex.IsMatch(pointer.interactionResource,"^CatCafeContent/Imports/[a-f0-9]{32}/interaction_texts$")
                || pointer.bundleResource!=pointer.interactionResource.Replace("/interaction_texts","/bundle"))throw new ArgumentException("交流文の素材参照先が不正です。");
            var asset=Resources.Load<TextAsset>(pointer.interactionResource);if(asset==null)throw new ArgumentException("交流文カタログを読み込めません。");
            var result=JsonUtility.FromJson<CafeNarrativeCatalog>(asset.text);result.Validate();
            if(pointer.interactionResource!="CatCafeContent/Imports/"+result.pack_id+"/interaction_texts")throw new ArgumentException("交流文のパックIDが一致しません。");
            return result;
        }
        public static Texture2D Texture(string resource)=>CafeNarrativeCatalog.Resource(resource)?Resources.Load<Texture2D>(resource):null;
    }
}
