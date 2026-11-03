using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    [Serializable] public class CatStateImagePointer {public string resource;}
    public sealed class CatStateImageBinding {public string cat_id,content_cat_id,state,image;}
    public sealed class CatStateImagePack
    {
        public int version;public string pack_id;public CatStateImageBinding[] bindings;
        public static string State(int kind)=>kind==1?"stress_high":kind==2?"fatigue_high":kind==3?"affinity_high":"";
        static Dictionary<string,object> Obj(object v,params string[] keys)
        {var d=v as Dictionary<string,object>;if(d==null || d.Count!=keys.Length || keys.Any(k=>!d.ContainsKey(k)))throw new ArgumentException("状態画像の項目が不正です。");return d;}
        static string Str(object v)=>v as string??throw new ArgumentException("状態画像の文字列が不正です。");
        public static CatStateImagePack Parse(string json)
        {
            var d=Obj(CafeOptionalEventJson.ReadValue(json),"version","pack_id","bindings");
            if(!(d["version"] is int) || ((int)d["version"]!=1 && (int)d["version"]!=2))throw new ArgumentException("状態画像の版が不正です。");
            int version=(int)d["version"];bool legacy=version==1;
            string id=Str(d["pack_id"]);if(!Regex.IsMatch(id,"\\A[a-f0-9]{32}\\z"))throw new ArgumentException("状態画像のパックIDが不正です。");
            var rows=d["bindings"] as object[];if(rows==null || rows.Length>(legacy?3:192))throw new ArgumentException("状態画像の対応件数が上限を超えています。");
            var seen=new HashSet<string>();
            var bindings=rows.Select(v=>{var r=Obj(v,legacy?new[]{"cat_id","content_cat_id","state","image"}:new[]{"content_cat_id","state","image"});var b=new CatStateImageBinding{cat_id=legacy?Str(r["cat_id"]):null,content_cat_id=Str(r["content_cat_id"]),state=Str(r["state"]),image=Str(r["image"])};
                if(legacy && (b.cat_id!="cat-mike" || b.content_cat_id!="playtest-mike"))throw new ArgumentException("v1の状態画像はミケ専用です。");
                if(!Regex.IsMatch(b.content_cat_id,"\\A[a-z][a-z0-9_-]{0,63}\\z") || !new[]{State(1),State(2),State(3)}.Contains(b.state) || !seen.Add(b.content_cat_id+"/"+b.state) || !Regex.IsMatch(b.image,"\\Aimages/[a-f0-9]{64}\\.png\\z"))throw new ArgumentException("状態画像の猫・状態・画像・重複が不正です。");return b;}).ToArray();
            if(bindings.Select(b=>b.content_cat_id).Distinct().Count()>64)throw new ArgumentException("状態画像は64画像IDまでです。");
            return new CatStateImagePack{version=version,pack_id=id,bindings=bindings};
        }
    }
    public static class CatStateImages
    {
        static string identity;static CatStateImagePack pack;static string root;
        static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        public static Texture2D Resolve(PythonCatState cat,int kind)
        {
            if(cat==null || string.IsNullOrEmpty(cat.cat_id) || CatStateImagePack.State(kind)=="")return null;
            var pointer=Resources.Load<TextAsset>("CatCafeStateImages/catalog");string json=pointer==null?"":pointer.text;
            if(identity!=json)
            {
                identity=json;pack=null;root=null;textures.Clear();
                if(json!="")try
                {
                    var p=JsonUtility.FromJson<CatStateImagePointer>(json);
                    if(p==null || !Regex.IsMatch(p.resource??"","\\ACatCafeStateImages/Imports/[a-f0-9]{32}/state_images\\z"))return null;
                    var asset=Resources.Load<TextAsset>(p.resource);if(asset==null)return null;pack=CatStateImagePack.Parse(asset.text);
                    if(p.resource!="CatCafeStateImages/Imports/"+pack.pack_id+"/state_images"){pack=null;return null;}
                    root=p.resource.Substring(0,p.resource.Length-"state_images".Length);
                }catch(ArgumentException){pack=null;}
            }
            if(pack?.version==1 && cat.cat_id!="cat-mike")return null;
            var row=pack?.bindings.FirstOrDefault(b=>b.content_cat_id==cat.content_cat_id && b.state==CatStateImagePack.State(kind));
            if(row==null)return null;
            string path=root+row.image.Substring(0,row.image.Length-4);Texture2D result;
            if(!textures.TryGetValue(path,out result)){result=Resources.Load<Texture2D>(path);textures[path]=result;}
            return result;
        }
    }
}
