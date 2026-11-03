// Legacy v1 parser snapshot from Tool ca19c8c, retained only for compatibility checks.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor.LegacyStateImages
{
    [Serializable] public class CatStateImagePointer {public string resource;}
    public sealed class CatStateImageBinding {public string cat_id,content_cat_id,state,image;}
    public sealed class CatStateImagePack
    {
        public string pack_id;public CatStateImageBinding[] bindings;
        public static string State(int kind)=>kind==1?"stress_high":kind==2?"fatigue_high":kind==3?"affinity_high":"";
        static Dictionary<string,object> Obj(object v,params string[] keys)
        {var d=v as Dictionary<string,object>;if(d==null || d.Count!=keys.Length || keys.Any(k=>!d.ContainsKey(k)))throw new ArgumentException("状態画像の項目が不正です。");return d;}
        static string Str(object v)=>v as string??throw new ArgumentException("状態画像の文字列が不正です。");
        public static CatStateImagePack Parse(string json)
        {
            var d=Obj(CafeOptionalEventJson.ReadValue(json),"version","pack_id","bindings");
            if(!(d["version"] is int) || (int)d["version"]!=1)throw new ArgumentException("状態画像の版が不正です。");
            string id=Str(d["pack_id"]);if(!Regex.IsMatch(id,"\\A[a-f0-9]{32}\\z"))throw new ArgumentException("状態画像のパックIDが不正です。");
            var rows=d["bindings"] as object[];if(rows==null || rows.Length>3)throw new ArgumentException("状態画像は3状態までです。");
            var seen=new HashSet<string>();
            var bindings=rows.Select(v=>{var r=Obj(v,"cat_id","content_cat_id","state","image");var b=new CatStateImageBinding{cat_id=Str(r["cat_id"]),content_cat_id=Str(r["content_cat_id"]),state=Str(r["state"]),image=Str(r["image"])};
                if(b.cat_id!="cat-mike" || b.content_cat_id!="playtest-mike" || !new[]{State(1),State(2),State(3)}.Contains(b.state) || !seen.Add(b.state) || !Regex.IsMatch(b.image,"\\Aimages/[a-f0-9]{64}\\.png\\z"))throw new ArgumentException("状態画像の猫・状態・画像・重複が不正です。");return b;}).ToArray();
            return new CatStateImagePack{pack_id=id,bindings=bindings};
        }
    }
}
