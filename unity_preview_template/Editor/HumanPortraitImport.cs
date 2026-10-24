using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
namespace CatCafePreview.Editor {
 public static class HumanPortraitImport {
  public static HumanPortraitPack Validate(string folder){
   string json=File.ReadAllText(Path.Combine(folder,"human_portraits.json"));var pack=JsonUtility.FromJson<HumanPortraitPack>(json);pack.Validate();
   foreach(var r in pack.people){var file=Path.Combine(folder,r.image);var data=File.ReadAllBytes(file);if(data.Length<33 || data[0]!=137 || data[1]!=80 || data[2]!=78 || data[3]!=71)throw new ArgumentException("Expected human PNG");using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").ToLowerInvariant()!=Path.GetFileNameWithoutExtension(file))throw new ArgumentException("Human image hash mismatch");var t=new Texture2D(2,2);try{if(!ImageConversion.LoadImage(t,data) || t.width>2048 || t.height>2048 || t.width<32 || t.height<32)throw new ArgumentException("Invalid human PNG");}finally{UnityEngine.Object.DestroyImmediate(t);}}
   return pack;
  }
  public static void Import(string folder){
   var pack=Validate(folder);string json=File.ReadAllText(Path.Combine(folder,"human_portraits.json"));
   string root=Path.Combine(Application.dataPath,"Resources/CatCafeContent");string target=Path.Combine(root,"People",pack.pack_id);string marker=Path.Combine(target,"human_portraits.json");
   if(Directory.Exists(target)){
    if(!File.Exists(marker) || File.ReadAllText(marker)!=json)throw new ArgumentException("Human pack ID collision");
    foreach(var r in pack.people)if(!File.Exists(Path.Combine(target,r.image)) || !File.ReadAllBytes(Path.Combine(target,r.image)).SequenceEqual(File.ReadAllBytes(Path.Combine(folder,r.image))))throw new ArgumentException("Imported human image changed");
   }else{
    string pending=target+".pending";
    if(Directory.Exists(pending))throw new ArgumentException("Human import already pending");
    try{Directory.CreateDirectory(Path.Combine(pending,"people"));foreach(var r in pack.people)File.Copy(Path.Combine(folder,r.image),Path.Combine(pending,r.image),true);File.WriteAllText(Path.Combine(pending,"human_portraits.json"),json);Directory.Move(pending,target);}
    finally{if(Directory.Exists(pending))Directory.Delete(pending,true);}
   }
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   foreach(var r in pack.people){string asset="Assets/Resources/CatCafeContent/People/"+pack.pack_id+"/"+r.image;var importer=(TextureImporter)AssetImporter.GetAtPath(asset);if(importer==null)throw new ArgumentException("Human texture importer missing");importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
   string catalog=Path.Combine(root,"human_portraits.json"),temp=catalog+".tmp";File.WriteAllText(temp,json);if(File.Exists(catalog))File.Replace(temp,catalog,null);else File.Move(temp,catalog);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   Debug.Log("Imported human portraits: "+pack.pack_id+" / "+pack.people.Length);
  }
 }
}
