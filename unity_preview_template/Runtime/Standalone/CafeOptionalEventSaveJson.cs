using System;
using System.Collections.Generic;
using System.Linq;
namespace CatCafePreview.Standalone
{
    // Strict only for the optional field; the full gameplay envelope keeps its existing codec.
    public static class CafeOptionalEventSaveJson
    {
        static Dictionary<string,object> Obj(object value,params string[] keys)
        {var row=value as Dictionary<string,object>;if(row==null || row.Count!=keys.Length || keys.Any(k=>!row.ContainsKey(k)))throw new ArgumentException("任意イベント保存の項目が不正です。");return row;}
        static string Str(object value,bool nullable=false)=>value is string?(string)value:nullable && value==null?null:throw new ArgumentException("任意イベント保存の文字列が不正です。");
        static int Int(object value)=>value is int?(int)value:throw new ArgumentException("任意イベント保存の整数が不正です。");
        static CafeConversationLine[] Lines(object value)
        {var rows=value as object[];if(rows==null)throw new ArgumentException("任意イベント保存の本文が不正です。");return rows.Select(v=>{var r=Obj(v,"speaker","text");return new CafeConversationLine(Str(r["speaker"],true),Str(r["text"]));}).ToArray();}
        static string[] Artwork(object value)
        {
            if(value==null)return null;
            if(!(value is object[] assets))throw new ArgumentException("任意イベント保存の画像指定が不正です。");
            return assets.Length==0?null:assets.Select(asset=>Str(asset)).ToArray();
        }
        static CafeOptionalEvent Row(object value)
        {
            var keysForRow=new List<string>{"event_id","revision","title","cat_id","content_cat_id","cat_name","image_resource","started_day","accept_caption","decline_caption","introduction","accepted","declined","progress"};
            if(value is Dictionary<string,object> candidate)
                foreach(string key in new[]{"scene_art","artwork","milestone","first_meeting"})if(candidate.ContainsKey(key))keysForRow.Add(key);
            var r=Obj(value,keysForRow.ToArray());
            if(r.ContainsKey("first_meeting") && !(r["first_meeting"] is bool))throw new ArgumentException("初対面の保存形式が不正です。");
            var artwork=r.ContainsKey("artwork")?Artwork(r["artwork"]):null;
            var raw=r["progress"] as Dictionary<string,object>;if(raw==null)throw new ArgumentException("任意イベント保存の進行が不正です。");var keys=new List<string>{"conversation_id","branch_id","node","completed","viewed_branches"};if(raw.ContainsKey("page_offset"))keys.Add("page_offset");var p=Obj(raw,keys.ToArray());
            if(!(p["completed"] is bool) || !(p["viewed_branches"] is object[]))throw new ArgumentException("任意イベント保存の進行型が不正です。");
            return new CafeOptionalEvent{event_id=Str(r["event_id"]),revision=Str(r["revision"]),title=Str(r["title"]),cat_id=Str(r["cat_id"]),content_cat_id=Str(r["content_cat_id"]),cat_name=Str(r["cat_name"]),image_resource=Str(r["image_resource"],true),first_meeting=r.ContainsKey("first_meeting") && (bool)r["first_meeting"],started_day=Int(r["started_day"]),milestone=r.ContainsKey("milestone")?Int(r["milestone"]):0,artwork=artwork,scene_art=r.ContainsKey("scene_art")?Int(r["scene_art"]):0,accept_caption=Str(r["accept_caption"]),decline_caption=Str(r["decline_caption"]),introduction=Lines(r["introduction"]),accepted=Lines(r["accepted"]),declined=Lines(r["declined"]),progress=new CafeConversationProgress{conversation_id=Str(p["conversation_id"]),branch_id=Str(p["branch_id"]),node=Int(p["node"]),page_offset=p.ContainsKey("page_offset")?Int(p["page_offset"]):0,completed=(bool)p["completed"],viewed_branches=((object[])p["viewed_branches"]).Select(v=>Str(v)).ToArray()}};
        }
        public static CafeOptionalEvent[] ReadSave(string json)
        {
            int at=0;Action space=()=>{while(at<json.Length && char.IsWhiteSpace(json[at]))at++;};
            Func<string> quoted=()=>{int start=at;if(at>=json.Length || json[at++]!='"')throw new ArgumentException("保存JSONが不正です。");bool closed=false;while(at<json.Length){char c=json[at++];if(c=='\\'){if(at>=json.Length)break;at++;}else if(c=='"'){closed=true;break;}}if(!closed)throw new ArgumentException("保存JSONが不正です。");return json.Substring(start,at-start);};
            space();if(at>=json.Length || json[at++]!='{')throw new ArgumentException("保存JSONが不正です。");bool found=false;CafeOptionalEvent[] result=null;
            while(true)
            {
                space();if(at<json.Length && json[at]=='}')break;
                string key=(string)CafeOptionalEventJson.ReadValue(quoted());space();if(at>=json.Length || json[at++]!=':')throw new ArgumentException("保存JSONが不正です。");space();int start=at,depth=0;
                while(at<json.Length)
                {
                    char c=json[at];if(c=='"'){quoted();continue;}if(depth==0 && (c==',' || c=='}'))break;
                    if(c=='[' || c=='{')depth++;if(c==']' || c=='}'){depth--;if(depth<0)throw new ArgumentException("保存JSONが不正です。");}at++;
                }
                if(key=="optional_events")
                {
                    if(found)throw new ArgumentException("任意イベントの保存項目が重複しています。");found=true;if(at-start>CafeOptionalEvents.MaxSaveJson)throw new ArgumentException("任意イベントの保存項目が大きすぎます。");string value=json.Substring(start,at-start).Trim();
                    if(value.Length>CafeOptionalEvents.MaxSaveJson)throw new ArgumentException("任意イベントの保存項目が大きすぎます。");
                    if(value!="null")
                    {
                        var array=CafeOptionalEventJson.ReadValue(value,CafeOptionalEvents.MaxSaveJson,true) as object[];if(array==null || array.Length>CafeOptionalEvents.MaxRecords)throw new ArgumentException("任意イベントの保存配列が不正です。");
                        result=array.Select(Row).ToArray();
                    }
                }
                space();if(at>=json.Length)throw new ArgumentException("保存JSONが不正です。");if(json[at]=='}')break;if(json[at++]!=',')throw new ArgumentException("保存JSONが不正です。");
            }
            CafeOptionalEvents.Validate(result);return result??new CafeOptionalEvent[0];
        }
    }
}
