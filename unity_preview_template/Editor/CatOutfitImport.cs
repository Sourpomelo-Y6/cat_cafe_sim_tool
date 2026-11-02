using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
namespace CatCafePreview.Editor
{
    public static class CatOutfitImport
    {
        static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static string Root=>Path.Combine(Application.dataPath,"Resources/CatCafeOutfits");
        static void CheckPng(byte[] bytes)
        {
            byte[] signature={137,80,78,71,13,10,26,10};
            if(bytes.Length<33 || bytes.Length>16*1024*1024 || !bytes.Take(8).SequenceEqual(signature) || System.Text.Encoding.ASCII.GetString(bytes,12,4)!="IHDR")throw new ArgumentException("見た目画像は16MiB以内のPNGが必要です。");
            Func<int,long> size=i=>(long)bytes[i]*16777216+bytes[i+1]*65536+bytes[i+2]*256+bytes[i+3];
            long width=size(16),height=size(20);if(width<1 || height<1 || width>2048 || height>2048)throw new ArgumentException("見た目画像の長辺は2048px以内にしてください。");
            var texture=new Texture2D(2,2);try{if(!texture.LoadImage(bytes) || texture.width!=width || texture.height!=height)throw new ArgumentException("見た目画像が破損しています。");}finally{UnityEngine.Object.DestroyImmediate(texture);}
        }
        public static CatOutfitPack Validate(string folder,ContentBundle bundle,bool full=false)
        {
            string json=File.ReadAllText(Path.Combine(folder,"cat_outfits.json"));var data=CatOutfitPack.Parse(json);
            if(full && data.pack_id!=bundle.pack_id || data.outfits.Any(b=>!bundle.cats.Any(c=>c.cat_id==b.content_cat_id)))throw new ArgumentException("見た目画像と素材パックの対応が一致しません。");
            string target=Path.Combine(Root,"Imports",data.pack_id),existing=Path.Combine(target,"cat_outfits.json");
            if(Directory.Exists(target) && (!File.Exists(existing) || File.ReadAllText(existing)!=json))throw new ArgumentException("同じ見た目画像パックIDの内容が異なります。新規出力してください。");
            foreach(var row in data.outfits)
            {
                string source=Path.Combine(folder,row.image);if(!File.Exists(source))throw new ArgumentException("見た目画像が不足しています。");
                if(new FileInfo(source).Length>16*1024*1024)throw new ArgumentException("見た目画像が大きすぎます。");
                var bytes=File.ReadAllBytes(source);if(row.image!="images/"+Hash(bytes)+".png")throw new ArgumentException("見た目画像のハッシュが不正です。");CheckPng(bytes);
                if(Directory.Exists(target) && (!File.Exists(Path.Combine(target,row.image)) || !File.ReadAllBytes(Path.Combine(target,row.image)).SequenceEqual(bytes)))throw new ArgumentException("取り込み済み見た目画像が変更されています。");
            }
            return data;
        }
        public static void Import(string folder,ContentBundle bundle=null,bool full=false)
        {
            if(bundle==null){var p=JsonUtility.FromJson<ContentCatalog>(Resources.Load<TextAsset>("CatCafeContent/catalog").text);bundle=JsonUtility.FromJson<ContentBundle>(Resources.Load<TextAsset>(p.bundleResource).text);}
            var data=Validate(folder,bundle,full);string target=Path.Combine(Root,"Imports",data.pack_id);
            if(!Directory.Exists(target))
            {
                string pending=target+".pending-"+Guid.NewGuid().ToString("N");
                try{Directory.CreateDirectory(Path.Combine(pending,"images"));foreach(var image in data.outfits.Select(b=>b.image).Distinct())File.Copy(Path.Combine(folder,image),Path.Combine(pending,image));File.Copy(Path.Combine(folder,"cat_outfits.json"),Path.Combine(pending,"cat_outfits.json"));Directory.Move(pending,target);}
                finally{if(Directory.Exists(pending) && Path.GetFullPath(pending).StartsWith(Path.GetFullPath(Path.Combine(Root,"Imports"))+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(pending,true);}
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var image in data.outfits.Select(b=>b.image).Distinct())
            {
                string path="Assets/Resources/CatCafeOutfits/Imports/"+data.pack_id+"/"+image;
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            string catalog=Path.Combine(Root,"catalog.json"),temp=catalog+".tmp";
            try{File.WriteAllText(temp,JsonUtility.ToJson(new CatOutfitPointer{resource="CatCafeOutfits/Imports/"+data.pack_id+"/cat_outfits"},true),new System.Text.UTF8Encoding(false));if(File.Exists(catalog))File.Replace(temp,catalog,null);else File.Move(temp,catalog);}
            finally{if(File.Exists(temp))File.Delete(temp);}
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
