using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace CatCafePreview.Standalone
{
    // The small authored-event contract deliberately has no bool/null/script values.
    // Strict types and duplicate/unknown keys cannot be checked by JsonUtility alone.
    public static class CafeOptionalEventJson
    {
        static Dictionary<string,object> Object(object value,params string[] keys)
        {
            var data=value as Dictionary<string,object>;
            if(data==null || data.Count!=keys.Length || keys.Any(k=>!data.ContainsKey(k)))throw new ArgumentException("任意イベントJSONの項目が不正です。");
            return data;
        }
        static string Text(object value)=>value as string??throw new ArgumentException("任意イベントJSONの文字列が不正です。");
        static int Number(object value)=>value is int?(int)value:throw new ArgumentException("任意イベントJSONの整数が不正です。");
        static object[] Array(object value)=>value as object[]??throw new ArgumentException("任意イベントJSONの配列が不正です。");
        static CafeConversationLine[] Lines(object value)=>Array(value).Select(v=>{var row=Object(v,"speaker","text");return new CafeConversationLine(Text(row["speaker"]),Text(row["text"]));}).ToArray();
        public static CafeOptionalEventCatalog Parse(string json)
        {
            var root=Object(new Reader(json).Read(),"version","pack_id","events");
            var result=new CafeOptionalEventCatalog{version=Number(root["version"]),pack_id=Text(root["pack_id"]),events=Array(root["events"]).Select(v=>{
                var r=Object(v,"event_id","cat_id","content_cat_id","min_day","min_affinity","title","accept_caption","decline_caption","introduction","accepted","declined","revision");
                return new CafeOptionalDefinition{event_id=Text(r["event_id"]),cat_id=Text(r["cat_id"]),content_cat_id=Text(r["content_cat_id"]),min_day=Number(r["min_day"]),min_affinity=Number(r["min_affinity"]),title=Text(r["title"]),accept_caption=Text(r["accept_caption"]),decline_caption=Text(r["decline_caption"]),revision=Text(r["revision"]),introduction=Lines(r["introduction"]),accepted=Lines(r["accepted"]),declined=Lines(r["declined"])};
            }).ToArray()};result.Validate();return result;
        }
        sealed class Reader
        {
            readonly string text;int at;
            public Reader(string json){if(json==null || json.Length>100000)throw new ArgumentException("任意イベントJSONが大きすぎます。");text=json;}
            ArgumentException Bad()=>new ArgumentException("任意イベントJSONの構文が不正です。");
            void Space(){while(at<text.Length && " \t\n\r".IndexOf(text[at])>=0)at++;}
            bool Eat(char c){Space();if(at<text.Length && text[at]==c){at++;return true;}return false;}
            void Need(char c){if(!Eat(c))throw Bad();}
            public object Read(){var value=Value(0);Space();if(at!=text.Length)throw Bad();return value;}
            object Value(int depth)
            {
                Space();if(depth>10 || at>=text.Length)throw Bad();
                if(text[at]=='"')return String();
                if(Eat('{'))
                {
                    var result=new Dictionary<string,object>();if(Eat('}'))return result;
                    do{Space();var key=String();Need(':');if(result.ContainsKey(key))throw Bad();result.Add(key,Value(depth+1));}while(Eat(','));Need('}');return result;
                }
                if(Eat('[')){var result=new List<object>();if(Eat(']'))return result.ToArray();do{result.Add(Value(depth+1));}while(Eat(','));Need(']');return result.ToArray();}
                int start=at;if(text[at]=='-')at++;if(at>=text.Length || text[at]<'0' || text[at]>'9')throw Bad();
                if(text[at]=='0')at++;else while(at<text.Length && text[at]>='0' && text[at]<='9')at++;
                if(!int.TryParse(text.Substring(start,at-start),NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out int number))throw Bad();return number;
            }
            string String()
            {
                if(at>=text.Length || text[at++]!='"')throw Bad();var value=new StringBuilder();bool ended=false;
                while(at<text.Length)
                {
                    char c=text[at++];if(c=='"'){ended=true;break;}if(c<32)throw Bad();
                    if(c!='\\'){value.Append(c);continue;}if(at>=text.Length)throw Bad();c=text[at++];
                    switch(c)
                    {
                        case '"':case '\\':case '/':value.Append(c);break;
                        case 'n':value.Append('\n');break;case 'r':value.Append('\r');break;case 't':value.Append('\t');break;case 'b':value.Append('\b');break;case 'f':value.Append('\f');break;
                        case 'u':if(at+4>text.Length || !ushort.TryParse(text.Substring(at,4),NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture,out ushort code))throw Bad();value.Append((char)code);at+=4;break;
                        default:throw Bad();
                    }
                }
                if(!ended)throw Bad();string result=value.ToString();
                for(int i=0;i<result.Length;i++)if(char.IsHighSurrogate(result[i])){if(i+1>=result.Length || !char.IsLowSurrogate(result[++i]))throw Bad();}else if(char.IsLowSurrogate(result[i]))throw Bad();
                return result;
            }
        }
    }
}
