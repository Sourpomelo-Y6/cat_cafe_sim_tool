using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor
{
    public static class OptionalEventImport
    {
        static string Root=>Path.Combine(Application.dataPath,"Resources/CatCafeEvents");
        static string Read(string folder)
        {string path=Path.Combine(folder,"optional_events.json");if(new FileInfo(path).Length>4000003)throw new ArgumentException("イベントJSONが大きすぎます。");return File.ReadAllText(path);}
        public static CafeOptionalEventCatalog Validate(string folder,ContentBundle bundle)
        {return ValidatePack(folder,bundle,out _);}

        static string[] Backgrounds(CafeOptionalEventCatalog data) =>
            data.events.Select(e=>e.artwork?[0]).Where(CafeOptionalEvents.IsCustomBackground).Distinct().ToArray();

        static string CustomPath(string asset)=>Path.Combine("event_backgrounds",asset+".png");
        static string StoredBackground(string asset)=>Path.Combine(Application.dataPath,"Resources",CafeOptionalEventResources.BackgroundDirectory,asset+".png");

        static CafeOptionalEventCatalog ValidatePack(string folder,ContentBundle bundle,out string json)
        {
            json=Read(folder);
            var data=ValidateJson(json,bundle);
            string target=Path.Combine(Root,"Imports",data.pack_id);
            string shared=Path.Combine(folder,CafeOptionalEventResources.SharedBackgroundFile);
            string existingShared=Path.Combine(target,CafeOptionalEventResources.SharedBackgroundFile);
            bool hasShared=File.Exists(shared);
            long total=0;
            if(Directory.Exists(target) && hasShared!=File.Exists(existingShared))
                throw new ArgumentException("同じイベントパックIDの共通背景が変更されています。");
            if(hasShared)
            {
                var bytes=ReadBackground(shared,ref total);
                if(File.Exists(existingShared) && !File.ReadAllBytes(existingShared).SequenceEqual(bytes))
                    throw new ArgumentException("同じイベントパックIDの共通背景が変更されています。");
            }
            foreach(string asset in Backgrounds(data))
            {
                var bytes=ReadBackground(Path.Combine(folder,CustomPath(asset)),ref total);
                string stored=StoredBackground(asset);
                if(asset!="bg-"+CatStateImageImport.Hash(bytes) ||
                    File.Exists(stored) && !File.ReadAllBytes(stored).SequenceEqual(bytes))
                    throw new ArgumentException("個別背景が登録時の内容と一致しません。");
            }
            return data;
        }

        static byte[] ReadBackground(string path,ref long total)
        {
            if(!File.Exists(path))throw new ArgumentException("イベント背景が不足しています。");
            const int limit=16*1024*1024;
            long size=new FileInfo(path).Length;
            if(size>limit || total+size>256L*1024*1024)
                throw new ArgumentException("イベント背景は1枚16MiB・合計256MiB以内にしてください。");
            byte[] bytes=File.ReadAllBytes(path);
            if(bytes.Length!=size)throw new ArgumentException("取り込み中にイベント背景が変更されました。");
            CatStateImageImport.CheckPng(bytes);
            total+=bytes.Length;
            return bytes;
        }

        static void ConfigureBackground(string file)
        {
            string path="Assets"+file.Substring(Application.dataPath.Length).Replace('\\','/');
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer==null)throw new ArgumentException("イベント背景をUnityに読み込めませんでした。");
            if(importer.npotScale==TextureImporterNPOTScale.None && !importer.mipmapEnabled) return;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.mipmapEnabled=false;
            importer.SaveAndReimport();
        }
        static CafeOptionalEventCatalog ValidateJson(string json,ContentBundle bundle)
        {
            var data=CafeOptionalEventJson.Parse(json);CafeOptionalEventResources.ValidateBinding(data,bundle);
            string target=Path.Combine(Root,"Imports",data.pack_id),existing=Path.Combine(target,"optional_events.json");
            if(Directory.Exists(target) && (!File.Exists(existing) || File.ReadAllText(existing)!=json))throw new ArgumentException("同じイベントパックIDの内容が変更されています。");return data;
        }
        public static string ReplacementSummary(string folder)
        {
            var bundle=File.Exists(Path.Combine(folder,"bundle.json"))?JsonUtility.FromJson<ContentBundle>(File.ReadAllText(Path.Combine(folder,"bundle.json"))):CatOutfitImport.CurrentBundle();var next=Validate(folder,bundle);CafeOptionalEventCatalog current=null;
            try{current=CafeOptionalEventResources.Load();}catch(ArgumentException){}
            int removed=current==null?0:current.events.Count(e=>!next.events.Any(n=>n.event_id==e.event_id && n.content_cat_id==e.content_cat_id));
            return next.events.Length+"件へ全体置換します。外れる定義："+removed+"。開始済み・完了済みの記録は保持します。空の定義は新規開始を無効にします。"+(next.version==1?"\n旧v1はミケ個体専用です。他系統の新規開始は無効になります。":"");
        }
        public static void Import(string folder,ContentBundle bundle=null)
        {
            var activation = new ContentImportActivation();
            Prepare(folder, bundle, activation);
            activation.Publish();
        }

        internal static void Prepare(string folder, ContentBundle bundle, ContentImportActivation activation)
        {
            if(bundle==null)bundle=CatOutfitImport.CurrentBundle();
            var data=ValidatePack(folder,bundle,out string json);
            string target=Path.Combine(Root,"Imports",data.pack_id);
            string shared=Path.Combine(folder,CafeOptionalEventResources.SharedBackgroundFile);
            string targetShared=Path.Combine(target,CafeOptionalEventResources.SharedBackgroundFile);
            foreach(string asset in Backgrounds(data))
            {
                string stored=StoredBackground(asset);
                if(File.Exists(stored))continue;
                long total=0;
                byte[] bytes=ReadBackground(Path.Combine(folder,CustomPath(asset)),ref total);
                if(asset!="bg-"+CatStateImageImport.Hash(bytes))throw new ArgumentException("取り込み中に個別背景が変更されました。");
                Directory.CreateDirectory(Path.GetDirectoryName(stored));
                string tempImage=stored+".tmp";
                try{File.WriteAllBytes(tempImage,bytes);File.Move(tempImage,stored);}
                finally{if(File.Exists(tempImage))File.Delete(tempImage);}
            }
            if(!Directory.Exists(target))
            {
                string pending=target+".pending-"+Guid.NewGuid().ToString("N");AssetDatabase.StartAssetEditing();
                try
                {
                    Directory.CreateDirectory(pending);
                    File.WriteAllText(Path.Combine(pending,"optional_events.json"),json,new System.Text.UTF8Encoding(false));
                    if(File.Exists(shared))
                    {
                        string pendingShared=Path.Combine(pending,CafeOptionalEventResources.SharedBackgroundFile);
                        Directory.CreateDirectory(Path.GetDirectoryName(pendingShared));
                        long total=0;
                        File.WriteAllBytes(pendingShared,ReadBackground(shared,ref total));
                    }
                    for(int attempt=0;;attempt++){try{Directory.Move(pending,target);break;}catch(IOException){if(attempt>=3 || Directory.Exists(target))throw;System.Threading.Thread.Sleep(20<<attempt);}}
                }
                finally{try{if(Directory.Exists(pending) && Path.GetFullPath(pending).StartsWith(Path.GetFullPath(Path.Combine(Root,"Imports"))+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(pending,true);}finally{AssetDatabase.StopAssetEditing();}}
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string asset in Backgrounds(data))ConfigureBackground(StoredBackground(asset));
            if(File.Exists(targetShared))ConfigureBackground(targetShared);
            activation.Add(Path.Combine(Root, "catalog.json"),
                JsonUtility.ToJson(new OptionalEventPointer { eventResource = "CatCafeEvents/Imports/" + data.pack_id + "/optional_events" }, true));
        }
    }
}
