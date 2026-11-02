using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor
{
    public static class OptionalEventImport
    {
        public static CafeOptionalEventCatalog Validate(string folder,ContentBundle bundle)
        {
            var data=CafeOptionalEventJson.Parse(File.ReadAllText(Path.Combine(folder,"optional_events.json")));
            CafeOptionalEventResources.ValidateBinding(data,bundle);
            string existing=Path.Combine(Application.dataPath,"Resources/CatCafeEvents/Imports",data.pack_id,"optional_events.json");
            if(File.Exists(existing) && File.ReadAllText(existing)!=File.ReadAllText(Path.Combine(folder,"optional_events.json")))throw new ArgumentException("同じイベントパックIDの内容が変更されています。");
            return data;
        }
        public static void Import(string folder,ContentBundle bundle=null)
        {
            if(bundle==null)
            {
                var pointer=JsonUtility.FromJson<ContentCatalog>(Resources.Load<TextAsset>("CatCafeContent/catalog").text);
                bundle=JsonUtility.FromJson<ContentBundle>(Resources.Load<TextAsset>(pointer.bundleResource).text);
            }
            var data=Validate(folder,bundle);string json=File.ReadAllText(Path.Combine(folder,"optional_events.json"));
            string root=Path.Combine(Application.dataPath,"Resources/CatCafeEvents"),target=Path.Combine(root,"Imports",data.pack_id);
            string file=Path.Combine(target,"optional_events.json");
            if(File.Exists(file) && File.ReadAllText(file)!=json)throw new ArgumentException("同じイベントパックIDの内容が変更されています。Toolから出力し直してください。");
            Directory.CreateDirectory(target);if(!File.Exists(file))File.WriteAllText(file,json,new System.Text.UTF8Encoding(false));
            Directory.CreateDirectory(root);string path=Path.Combine(root,"catalog.json"),temp=path+".tmp";
            try
            {
                File.WriteAllText(temp,JsonUtility.ToJson(new OptionalEventPointer{eventResource="CatCafeEvents/Imports/"+data.pack_id+"/optional_events"},true),new System.Text.UTF8Encoding(false));
                if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
            }
            finally{if(File.Exists(temp))File.Delete(temp);}
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
