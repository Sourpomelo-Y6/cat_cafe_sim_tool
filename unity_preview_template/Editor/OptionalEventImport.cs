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
        {return ValidateJson(Read(folder),bundle);}
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
            if(bundle==null)bundle=CatOutfitImport.CurrentBundle();string json=Read(folder);var data=ValidateJson(json,bundle);string target=Path.Combine(Root,"Imports",data.pack_id);
            if(!Directory.Exists(target))
            {
                string pending=target+".pending-"+Guid.NewGuid().ToString("N");AssetDatabase.StartAssetEditing();
                try
                {
                    Directory.CreateDirectory(pending);File.WriteAllText(Path.Combine(pending,"optional_events.json"),json,new System.Text.UTF8Encoding(false));Validate(pending,bundle);
                    for(int attempt=0;;attempt++){try{Directory.Move(pending,target);break;}catch(IOException){if(attempt>=3 || Directory.Exists(target))throw;System.Threading.Thread.Sleep(20<<attempt);}}
                }
                finally{try{if(Directory.Exists(pending) && Path.GetFullPath(pending).StartsWith(Path.GetFullPath(Path.Combine(Root,"Imports"))+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(pending,true);}finally{AssetDatabase.StopAssetEditing();}}
            }
            Directory.CreateDirectory(Root);string path=Path.Combine(Root,"catalog.json"),temp=path+".tmp";
            try{File.WriteAllText(temp,JsonUtility.ToJson(new OptionalEventPointer{eventResource="CatCafeEvents/Imports/"+data.pack_id+"/optional_events"},true),new System.Text.UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
            finally{if(File.Exists(temp))File.Delete(temp);}AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
