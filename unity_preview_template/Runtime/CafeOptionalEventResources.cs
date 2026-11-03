using System;
using System.Text.RegularExpressions;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    [Serializable] public sealed class OptionalEventPointer {public string eventResource;}
    public static class CafeOptionalEventResources
    {
        public static CafeOptionalEventCatalog Load()
        {
            var pointer=Resources.Load<TextAsset>("CatCafeEvents/catalog");
            var imagePointer=Resources.Load<TextAsset>("CatCafeContent/catalog");if(imagePointer==null){if(pointer!=null)throw new ArgumentException("明示されたイベント定義に対応する通常素材がありません。");return null;}
            var content=JsonUtility.FromJson<ContentCatalog>(imagePointer.text);
            var bundle=JsonUtility.FromJson<ContentBundle>(Resources.Load<TextAsset>(content.bundleResource).text);
            string resource;
            if(pointer!=null)
            {
                resource=JsonUtility.FromJson<OptionalEventPointer>(pointer.text)?.eventResource;
                if(!Regex.IsMatch(resource??"","^CatCafeEvents/Imports/[a-f0-9]{32}/optional_events$"))throw new ArgumentException("任意イベントの参照先が不正です。");
            }
            else
            {
                if(string.IsNullOrEmpty(bundle.optional_events))return null; // pre-F2 packs keep the legacy story.
                if(bundle.optional_events!="optional_events.json")throw new ArgumentException("任意イベントのファイル名が不正です。");
                resource=content.bundleResource.Substring(0,content.bundleResource.Length-6)+"optional_events";
            }
            var asset=Resources.Load<TextAsset>(resource);if(asset==null)throw new ArgumentException("任意イベント定義が見つかりません。");
            var result=CafeOptionalEventJson.Parse(asset.text);ValidateBinding(result,bundle);
            if(!resource.EndsWith("/"+result.pack_id+"/optional_events",StringComparison.Ordinal))throw new ArgumentException("任意イベントのパックIDが一致しません。");
            return result;
        }
        public static void ValidateBinding(CafeOptionalEventCatalog catalog,ContentBundle bundle)
        {
            catalog.Validate();foreach(var e in catalog.events)if(!Array.Exists(bundle.cats,c=>c.cat_id==e.content_cat_id))throw new ArgumentException("任意イベントの猫素材がありません。");
        }
    }
}
