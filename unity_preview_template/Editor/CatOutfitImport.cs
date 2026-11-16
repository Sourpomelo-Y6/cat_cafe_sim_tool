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
        public static long AddImageSize(long total,long size)
        {if(total<0 || size<0 || size>16L*1024*1024 || total>256L*1024*1024-size)throw new ArgumentException("見た目画像は1枚16MiB・合計256MiB以内です。");return total+size;}
        public static ContentBundle CurrentBundle()
        {
            var asset=Resources.Load<TextAsset>("CatCafeContent/catalog");if(asset==null)throw new ArgumentException("通常素材パックを先に取り込んでください。");
            var p=JsonUtility.FromJson<ContentCatalog>(asset.text);var bundleAsset=Resources.Load<TextAsset>(p?.bundleResource??"");if(bundleAsset==null)throw new ArgumentException("有効な通常素材パックがありません。");
            var bundle=JsonUtility.FromJson<ContentBundle>(bundleAsset.text);ContentValidation.Validate(bundle);return bundle;
        }
        public static string ReadDefinition(string path)
        {if(new FileInfo(path).Length>400003)throw new ArgumentException("見た目画像JSONが大きすぎます。");return File.ReadAllText(path);}
        public static string ReplacementSummary(string folder)
        {
            bool full=File.Exists(Path.Combine(folder,"bundle.json"));var bundle=full?JsonUtility.FromJson<ContentBundle>(File.ReadAllText(Path.Combine(folder,"bundle.json"))):CurrentBundle();ContentValidation.Validate(bundle);var next=Validate(folder,bundle,full);CatOutfitPack current=null;
            try{var pointer=Resources.Load<TextAsset>("CatCafeOutfits/catalog");if(pointer!=null){var p=JsonUtility.FromJson<CatOutfitPointer>(pointer.text);var asset=Resources.Load<TextAsset>(p.resource);if(asset!=null)current=CatOutfitPack.Parse(asset.text);}}catch(ArgumentException){}
            int removed=current==null?0:current.outfits.Count(b=>!next.outfits.Any(n=>n.content_cat_id==b.content_cat_id && n.outfit_id==b.outfit_id));
            return next.outfits.Select(b=>b.content_cat_id).Distinct().Count()+"画像ID／"+next.outfits.Length+"対応へ全体を切り替えます。現在から外れる対応："+removed+"。\n部分更新やマージは行いません。空の定義は画像定義の全解除です。ゲーム内の選択は保持します。"+(next.version==1?"\nv1はミケ専用です。他猫の見た目画像は非表示になります。":"")+"\nPlayerへは次のビルドで反映されます。";
        }
        static void CheckPng(byte[] bytes)
        {
            byte[] signature={137,80,78,71,13,10,26,10};
            if(bytes.Length<33 || bytes.Length>16*1024*1024 || !bytes.Take(8).SequenceEqual(signature) || System.Text.Encoding.ASCII.GetString(bytes,12,4)!="IHDR")throw new ArgumentException("見た目画像は16MiB以内のPNGが必要です。");
            Func<int,long> size=i=>(long)bytes[i]*16777216+bytes[i+1]*65536+bytes[i+2]*256+bytes[i+3];
            long width=size(16),height=size(20);if(width<1 || height<1 || width>2048 || height>2048)throw new ArgumentException("見た目画像の長辺は2048px以内にしてください。");
            var texture=new Texture2D(2,2);try{if(!texture.LoadImage(bytes) || texture.width!=width || texture.height!=height)throw new ArgumentException("見た目画像が破損しています。");}finally{UnityEngine.Object.DestroyImmediate(texture);}
        }
        public static CatOutfitPack Validate(string folder,ContentBundle bundle,bool full=false)
        {string json;return Validate(folder,bundle,full,out json);}
        static CatOutfitPack Validate(string folder,ContentBundle bundle,bool full,out string json)
        {
            json=ReadDefinition(Path.Combine(folder,"cat_outfits.json"));var data=CatOutfitPack.Parse(json);
            if(full && data.pack_id!=bundle.pack_id || data.outfits.Any(b=>!bundle.cats.Any(c=>c.cat_id==b.content_cat_id)))throw new ArgumentException("見た目画像と素材パックの対応が一致しません。");
            string target=Path.Combine(Root,"Imports",data.pack_id),existing=Path.Combine(target,"cat_outfits.json");
            if(Directory.Exists(target) && (!File.Exists(existing) || File.ReadAllText(existing)!=json))throw new ArgumentException("同じ見た目画像パックIDの内容が異なります。新規出力してください。");
            long total=0;
            foreach(var row in data.outfits.GroupBy(b=>b.image).Select(g=>g.First()))
            {
                string source=Path.Combine(folder,row.image);if(!File.Exists(source))throw new ArgumentException("見た目画像が不足しています。");
                AddImageSize(total,new FileInfo(source).Length);
                var bytes=File.ReadAllBytes(source);total=AddImageSize(total,bytes.Length);if(row.image!="images/"+Hash(bytes)+".png")throw new ArgumentException("見た目画像のハッシュが不正です。");CheckPng(bytes);
                if(Directory.Exists(target) && (!File.Exists(Path.Combine(target,row.image)) || !File.ReadAllBytes(Path.Combine(target,row.image)).SequenceEqual(bytes)))throw new ArgumentException("取り込み済み見た目画像が変更されています。");
            }
            return data;
        }
        public static void Import(string folder,ContentBundle bundle=null,bool full=false)
        {
            var activation = new ContentImportActivation();
            Prepare(folder, bundle, full, activation);
            activation.Publish();
        }

        internal static void Prepare(string folder, ContentBundle bundle, bool full, ContentImportActivation activation)
        {
            if(bundle==null)bundle=CurrentBundle();
            string json;var data=Validate(folder,bundle,full,out json);string target=Path.Combine(Root,"Imports",data.pack_id);
            if(!Directory.Exists(target))
            {
                string pending=target+".pending-"+Guid.NewGuid().ToString("N");
                AssetDatabase.StartAssetEditing();
                try{Directory.CreateDirectory(Path.Combine(pending,"images"));foreach(var image in data.outfits.Select(b=>b.image).Distinct())File.Copy(Path.Combine(folder,image),Path.Combine(pending,image));File.WriteAllText(Path.Combine(pending,"cat_outfits.json"),json,new System.Text.UTF8Encoding(false));Validate(pending,bundle,full);for(int attempt=0;;attempt++){try{Directory.Move(pending,target);break;}catch(IOException){if(attempt>=3 || Directory.Exists(target))throw;System.Threading.Thread.Sleep(20<<attempt);}}}
                finally{try{if(Directory.Exists(pending) && Path.GetFullPath(pending).StartsWith(Path.GetFullPath(Path.Combine(Root,"Imports"))+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(pending,true);}finally{AssetDatabase.StopAssetEditing();}}
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var image in data.outfits.Select(b=>b.image).Distinct())
            {
                string path="Assets/Resources/CatCafeOutfits/Imports/"+data.pack_id+"/"+image;
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            activation.Add(Path.Combine(Root, "catalog.json"),
                JsonUtility.ToJson(new CatOutfitPointer { resource = "CatCafeOutfits/Imports/" + data.pack_id + "/cat_outfits" }, true));
        }
    }
}
