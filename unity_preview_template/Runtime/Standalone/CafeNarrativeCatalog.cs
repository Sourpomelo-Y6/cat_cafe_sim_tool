using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CatCafePreview.Standalone
{
    [Serializable] public class NarrativeCatBinding { public string cat_id, content_cat_id; }
    [Serializable] public class NarrativePerson { public string id, name; }
    [Serializable] public class NarrativeVariant { public string id, text; public int slot; }
    [Serializable] public class NarrativeEntry
    {
        public string kind, subject_id, mode, action, reaction, scene_key;
        public NarrativeVariant[] variants;
        public string Key()=>string.Join("/",kind,subject_id,mode,action,reaction,scene_key);
    }
    [Serializable] public class NarrativeImage { public string cat_id,content_cat_id,scene_key,resolved_scene_key,image; }
    [Serializable] public class NarrativeActionText { public string mode,action,text; }
    [Serializable] public class NarrativeReactionText { public string reaction,text; }
    [Serializable] public class CafeNarrativeCatalog
    {
        public int version;
        public string pack_id,revision,rest_narration;
        public NarrativeCatBinding[] cat_bindings;
        public NarrativePerson[] people;
        public NarrativeEntry[] entries;
        public NarrativeImage[] image_bindings;
        public NarrativeActionText[] action_texts;
        public NarrativeReactionText[] narration_texts;
        public static readonly string[] Reactions={"neutral","favorable","enthusiastic","turn_away","confused","listless"};
        public static bool Hex(string s,int count)=>s!=null && Regex.IsMatch(s,"^[a-f0-9]{"+count+"}$");
        public static bool Resource(string s)=>!string.IsNullOrEmpty(s) && Regex.IsMatch(s,"^CatCafeContent/Imports/[a-f0-9]{32}/images/[a-f0-9]{64}$");
        private static bool Text(string s,int limit)=>!string.IsNullOrWhiteSpace(s) && s==s.Trim() && s.Length<=limit && !s.Any(c=>c<32 && c!='\n' && c!='\t');
        public static string[] Actions(string mode)=>mode=="pause"?new[]{"pause"}:mode=="pet"?new[]{"any","direct","adapt"}:mode=="teaser"?new[]{"any","direct","adapt","intense","feint"}:new string[0];
        public static string Scene(string mode,string action)=>action=="any" || action=="pause"?mode:mode+"__"+action;
        public static string[] ImageChoices(string scene)
        {
            string mode=scene.Split(new[]{"__"},StringSplitOptions.None)[0];
            return new[]{scene,mode,mode=="teaser"?"play":"normal","normal"}.Distinct().ToArray();
        }
        public void Validate()
        {
            if(version!=1 || !Hex(pack_id,32) || !Hex(revision,64) || cat_bindings==null || cat_bindings.Length!=1
                || cat_bindings[0]==null || cat_bindings[0].cat_id!="cat-mike" || cat_bindings[0].content_cat_id==null
                || cat_bindings[0].content_cat_id.Length>200 || people==null || people.Length!=2
                || entries==null || entries.Length>1000 || image_bindings==null || image_bindings.Length!=9
                || action_texts==null || narration_texts==null || !Text(rest_narration,500))throw new ArgumentException("交流文カタログの見出し・版・対応が不正です。");
            var persons=new HashSet<string>();
            foreach(var p in people)if(p==null || (p.id!="player" && p.id!="guest-1") || !persons.Add(p.id) || !Text(p.name,100))throw new ArgumentException("交流文の人物が不正です。");
            var keys=new HashSet<string>();var ids=new HashSet<string>();
            foreach(var e in entries)
            {
                if(e==null || e.kind!="speech" && e.kind!="cat_response" || (e.kind=="speech"?!persons.Contains(e.subject_id):e.subject_id!="cat-mike")
                    || !Actions(e.mode).Contains(e.action) || e.scene_key!=Scene(e.mode,e.action)
                    || (e.kind=="speech"?e.reaction!="any":!Reactions.Contains(e.reaction) || e.mode=="pause" && e.reaction!="neutral" && e.reaction!="listless")
                    || !keys.Add(e.Key()) || e.variants==null || e.variants.Length<1 || e.variants.Length>3)throw new ArgumentException("交流文の条件・候補が不正です。");
                int previous=0;var texts=new HashSet<string>();
                foreach(var v in e.variants)
                {
                    if(v==null || !Hex(v.id,32) || !ids.Add(v.id) || v.slot<=previous || v.slot>3 || !Text(v.text,200) || !texts.Add(v.text))throw new ArgumentException("交流文の候補ID・順番・本文が不正です。");
                    previous=v.slot;
                }
            }
            var scenes=new HashSet<string>();
            foreach(var i in image_bindings)
            {
                if(i==null || i.cat_id!="cat-mike" || i.content_cat_id!=cat_bindings[0].content_cat_id || !KnownScene(i.scene_key)
                    || !scenes.Add(i.scene_key) || i.image==null || i.resolved_scene_key==null
                    || (i.image==""?i.resolved_scene_key!="":!Regex.IsMatch(i.image,"^images/[a-f0-9]{64}\\.png$") || !ImageChoices(i.scene_key).Contains(i.resolved_scene_key) || i.content_cat_id==""))throw new ArgumentException("交流文の画像対応が不正です。");
            }
            if(action_texts.Length!=7 || narration_texts.Length!=6)throw new ArgumentException("交流文の共通文が不足しています。");
            keys.Clear();
            foreach(var a in action_texts)if(a==null || a.action=="any" || !Actions(a.mode).Contains(a.action) || !keys.Add(a.mode+"/"+a.action) || !Text(a.text,500)
                || a.text.Replace("{human}","").Replace("{cat}","").Any(c=>c=='{' || c=='}'))throw new ArgumentException("交流文の行動文が不正です。");
            keys.Clear();foreach(var n in narration_texts)if(n==null || !Reactions.Contains(n.reaction) || !keys.Add(n.reaction) || !Text(n.text,500))throw new ArgumentException("交流文の地の文が不正です。");
            if(ComputeRevision()!=revision)throw new ArgumentException("交流文カタログの内容とrevisionが一致しません。書き出し直してください。");
        }
        private static bool KnownScene(string scene)=>new[]{"teaser","pet","pause"}.Any(m=>Actions(m).Any(a=>Scene(m,a)==scene));
        public NarrativeEntry Find(string kind,string subject,string mode,string action,string reaction)
        {
            return entries.FirstOrDefault(e=>e.kind==kind && e.subject_id==subject && e.mode==mode && e.action==action && e.reaction==reaction)
                ?? entries.FirstOrDefault(e=>e.kind==kind && e.subject_id==subject && e.mode==mode && e.action=="any" && e.reaction==reaction);
        }
        public string ContentId(string fallback)=>string.IsNullOrEmpty(cat_bindings[0].content_cat_id)?fallback:cat_bindings[0].content_cat_id;
        public string ComputeRevision()
        {
            var b=new StringBuilder();Canonical(b,this,true);
            using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(b.ToString())).Select(v=>v.ToString("x2")));
        }
        private static void Quoted(StringBuilder b,string s)
        {
            b.Append('"');foreach(char c in s){switch(c){case '"':b.Append("\\\"");break;case '\\':b.Append("\\\\");break;case '\n':b.Append("\\n");break;case '\r':b.Append("\\r");break;case '\t':b.Append("\\t");break;default:if(c<32)b.Append("\\u"+((int)c).ToString("x4"));else b.Append(c);break;}}b.Append('"');
        }
        private static void Canonical(StringBuilder b,object value,bool root=false)
        {
            if(value==null){b.Append("null");return;}if(value is string s){Quoted(b,s);return;}if(value is int n){b.Append(n.ToString(CultureInfo.InvariantCulture));return;}
            if(value is Array rows){b.Append('[');bool comma=false;foreach(var row in rows){if(comma)b.Append(',');comma=true;Canonical(b,row);}b.Append(']');return;}
            b.Append('{');bool first=true;foreach(var f in value.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public).OrderBy(f=>f.Name,StringComparer.Ordinal))
            {if(root && (f.Name=="revision" || f.Name=="pack_id"))continue;if(!first)b.Append(',');first=false;Quoted(b,f.Name);b.Append(':');Canonical(b,f.GetValue(value));}b.Append('}');
        }
    }
}
