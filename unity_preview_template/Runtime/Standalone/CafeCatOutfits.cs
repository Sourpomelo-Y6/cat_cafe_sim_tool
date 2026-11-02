using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeCatOutfitSelection {public string cat_id,outfit_id;}
    public static class CafeCatOutfits
    {
        public static CafeCatOutfitSelection[] Copy(CafeCatOutfitSelection[] rows)
        {
            rows=rows??new CafeCatOutfitSelection[0];
            if(rows.Length>1 || rows.Any(r=>r==null || r.cat_id!="cat-mike" || !Regex.IsMatch(r.outfit_id??"","\\A[a-z][a-z0-9_-]{0,31}\\z")))throw new ArgumentException("見た目の保存項目が不正です。");
            return rows.Select(r=>new CafeCatOutfitSelection{cat_id=r.cat_id,outfit_id=r.outfit_id}).ToArray();
        }
        // Find only a root property. Strings elsewhere (including saved dialogue) are not fields.
        // JsonUtility handles the full save; the optional array is parsed strictly before use.
        public static CafeCatOutfitSelection[] ReadSave(string json)
        {
            int at=0;Action space=()=>{while(at<json.Length && char.IsWhiteSpace(json[at]))at++;};
            Func<string> quoted=()=>{int start=at;if(at>=json.Length || json[at++]!='"')throw new ArgumentException("保存JSONが不正です。");bool closed=false;while(at<json.Length){char c=json[at++];if(c=='\\'){if(at>=json.Length)break;at++;}else if(c=='"'){closed=true;break;}}if(!closed)throw new ArgumentException("保存JSONが不正です。");return json.Substring(start,at-start);};
            space();if(at>=json.Length || json[at++]!='{')throw new ArgumentException("保存JSONが不正です。");bool found=false;CafeCatOutfitSelection[] result=null;
            while(true)
            {
                space();if(at<json.Length && json[at]=='}')break;
                string key=(string)CafeOptionalEventJson.ReadValue(quoted());space();if(at>=json.Length || json[at++]!=':')throw new ArgumentException("保存JSONが不正です。");space();int start=at,depth=0;
                while(at<json.Length)
                {
                    char c=json[at];if(c=='"'){quoted();continue;}if(depth==0 && (c==',' || c=='}'))break;
                    if(c=='[' || c=='{')depth++;if(c==']' || c=='}'){depth--;if(depth<0)throw new ArgumentException("保存JSONが不正です。");}at++;
                }
                if(key=="cat_outfits")
                {
                    if(found)throw new ArgumentException("見た目の保存項目が重複しています。");found=true;string value=json.Substring(start,at-start).Trim();
                    if(value.Length>4096)throw new ArgumentException("見た目の保存項目が大きすぎます。");
                    if(value!="null")
                    {
                        var array=CafeOptionalEventJson.ReadValue(value) as object[];if(array==null || array.Length>1)throw new ArgumentException("見た目の保存配列が不正です。");
                        result=array.Select(v=>{var row=v as Dictionary<string,object>;if(row==null || row.Count!=2 || !row.ContainsKey("cat_id") || !row.ContainsKey("outfit_id") || !(row["cat_id"] is string) || !(row["outfit_id"] is string))throw new ArgumentException("見た目の保存項目が不正です。");return new CafeCatOutfitSelection{cat_id=(string)row["cat_id"],outfit_id=(string)row["outfit_id"]};}).ToArray();
                    }
                }
                space();if(at>=json.Length)throw new ArgumentException("保存JSONが不正です。");if(json[at]=='}')break;if(json[at++]!=',')throw new ArgumentException("保存JSONが不正です。");
            }
            return Copy(result);
        }
    }
}
