using System;
using System.Linq;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class CatGalleryImage
    {
        public string key,name,resource,cat_id;
        public Texture2D Texture()=>Resources.Load<Texture2D>(resource);
    }
    public static class CatImageGalleryCatalog
    {
        public static CatGalleryImage[] Load()
        {
            var asset=Resources.Load<TextAsset>("CatCafeContent/catalog");if(asset==null)return new CatGalleryImage[0];
            try
            {
                var c=JsonUtility.FromJson<ContentCatalog>(asset.text);if(!System.Text.RegularExpressions.Regex.IsMatch(c.bundleResource??"","^CatCafeContent/Imports/[a-f0-9]{32}/bundle$"))return new CatGalleryImage[0];
                var b=JsonUtility.FromJson<ContentBundle>(Resources.Load<TextAsset>(c.bundleResource).text);ContentValidation.Validate(b);string root=c.bundleResource.Substring(0,c.bundleResource.Length-6);
                return b.cats.Select(cat=>new{cat,scene=cat.scenes.FirstOrDefault(s=>s.key=="normal")}).Where(x=>CafeImageGallery.Key(x.scene?.image)!=null).Select(x=>new CatGalleryImage{key=CafeImageGallery.Key(x.scene.image),cat_id=x.cat.cat_id,name=x.cat.name,resource=root+x.scene.image.Substring(0,x.scene.image.Length-4)}).GroupBy(x=>x.key).Select(g=>g.First()).ToArray();
            }
            catch(Exception){return new CatGalleryImage[0];}
        }
    }
}
