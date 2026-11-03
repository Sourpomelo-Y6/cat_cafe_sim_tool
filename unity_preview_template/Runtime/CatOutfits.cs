using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    [Serializable] public class CatOutfitPointer {public string resource;}
    public sealed class CatOutfitBinding {public string cat_id,content_cat_id,outfit_id,label,image;}
    public sealed class CatOutfitPack
    {
        public int version;public string pack_id;public CatOutfitBinding[] outfits;

        static Dictionary<string,object> Obj(object v,params string[] keys)
        {var d=v as Dictionary<string,object>;if(d==null || d.Count!=keys.Length || keys.Any(k=>!d.ContainsKey(k)))throw new ArgumentException("見た目画像の項目が不正です。");return d;}
        static string Str(object v)=>v as string??throw new ArgumentException("見た目画像の文字列が不正です。");
        public static CatOutfitPack Parse(string json)
        {
            var d=Obj(CafeOptionalEventJson.ReadValue(json),"version","pack_id","outfits");
            if(!(d["version"] is int) || ((int)d["version"]!=1 && (int)d["version"]!=2))throw new ArgumentException("見た目画像の版が不正です。");
            int version=(int)d["version"];string id=Str(d["pack_id"]);if(!Regex.IsMatch(id,"\\A[a-f0-9]{32}\\z"))throw new ArgumentException("見た目画像のパックIDが不正です。");
            var rows=d["outfits"] as object[];if(rows==null || rows.Length>(version==1?1:64))throw new ArgumentException("見た目画像の対応件数が上限を超えています。");
            var seen=new HashSet<string>();
            var outfits=rows.Select(v=>{var r=version==1?Obj(v,"cat_id","content_cat_id","outfit_id","label","image"):Obj(v,"content_cat_id","outfit_id","label","image");var b=new CatOutfitBinding{cat_id=version==1?Str(r["cat_id"]):null,content_cat_id=Str(r["content_cat_id"]),outfit_id=Str(r["outfit_id"]),label=Str(r["label"]),image=Str(r["image"])};
                if((version==1 && (b.cat_id!="cat-mike" || b.content_cat_id!="playtest-mike")) || !Regex.IsMatch(b.content_cat_id,"\\A[a-z][a-z0-9_-]{0,63}\\z") || b.outfit_id!="ribbon" || string.IsNullOrWhiteSpace(b.label) || b.label.Length>40 || !seen.Add(b.content_cat_id+":"+b.outfit_id) || !Regex.IsMatch(b.image,"\\Aimages/[a-f0-9]{64}\\.png\\z"))throw new ArgumentException("見た目画像の猫・状態・画像・重複が不正です。");return b;}).ToArray();
            return new CatOutfitPack{version=version,pack_id=id,outfits=outfits};
        }
    }
    public static class CatOutfits
    {
        static string identity;static CatOutfitPack pack;static string root;
        static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        public static string Label=>LabelFor("cat-mike","playtest-mike","ribbon");
        public static string LabelFor(string catId,string contentId,string outfitId="ribbon")
        {ImageFor(catId,contentId,outfitId);return Binding(catId,contentId,outfitId)?.label??"リボン姿";}
        static CatOutfitBinding Binding(string catId,string contentId,string outfitId)=>pack?.outfits.FirstOrDefault(b=>b.content_cat_id==contentId && b.outfit_id==outfitId && (pack.version==2 || b.cat_id==catId));
        public static bool Eligible(PythonCatState cat)
        {
            return cat!=null && !string.IsNullOrEmpty(cat.cat_id) && cat.health_status=="healthy" && cat.activity=="cafe" && new[]{cat.stress,cat.fatigue,cat.affinity}.All(v=>!float.IsNaN(v) && !float.IsInfinity(v));
        }
        public static Texture2D Resolve(PythonCatState cat)=>Eligible(cat) && CatStateMark.Resolve(cat)==0 && cat.outfit_id=="ribbon"?ImageFor(cat.cat_id,cat.content_cat_id,cat.outfit_id):null;
        public static string Reason(PythonCatState cat)
        {
            if(string.IsNullOrEmpty(cat?.outfit_id))return "通常を選択中";
            if(cat.outfit_id!="ribbon")return "未対応の見た目を選択中。従来画像を表示します。";
            if(ImageFor(cat.cat_id,cat.content_cat_id,cat.outfit_id)==null)return "リボン姿を選択中／画像未登録";
            if(!Eligible(cat))return "リボン姿を選択中／現在の体調・活動では従来画像を表示";
            if(CatStateMark.Resolve(cat)!=0)return "リボン姿を選択中／現在は状態表示を優先";
            return "リボン姿を選択中";
        }
        public static Texture2D ImageFor(string contentId)=>ImageFor("cat-mike",contentId,"ribbon");
        public static Texture2D ImageFor(string catId,string contentId,string outfitId="ribbon")
        {

            var pointer=Resources.Load<TextAsset>("CatCafeOutfits/catalog");string json=pointer==null?"":pointer.text;
            if(identity!=json)
            {
                identity=json;pack=null;root=null;textures.Clear();
                if(json!="")try
                {
                    var p=JsonUtility.FromJson<CatOutfitPointer>(json);
                    if(p==null || !Regex.IsMatch(p.resource??"","\\ACatCafeOutfits/Imports/[a-f0-9]{32}/cat_outfits\\z"))return null;
                    var asset=Resources.Load<TextAsset>(p.resource);if(asset==null)return null;pack=CatOutfitPack.Parse(asset.text);
                    if(p.resource!="CatCafeOutfits/Imports/"+pack.pack_id+"/cat_outfits"){pack=null;return null;}
                    root=p.resource.Substring(0,p.resource.Length-"cat_outfits".Length);
                }catch(ArgumentException){pack=null;}
            }
            var row=Binding(catId,contentId,outfitId);
            if(row==null)return null;
            string path=root+row.image.Substring(0,row.image.Length-4);Texture2D result;
            if(!textures.TryGetValue(path,out result)){result=Resources.Load<Texture2D>(path);textures[path]=result;}
            return result;
        }
    }
}
