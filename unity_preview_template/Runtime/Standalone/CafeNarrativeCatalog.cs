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
    [Serializable] public class NarrativeCatBinding { public string cat_id, content_cat_id, subject_id, name; }
    [Serializable] public class NarrativePerson { public string id, name, game_id, call; }
    [Serializable] public class NarrativeVariant { public string id, text; public int slot; }
    [Serializable] public class NarrativeEntry
    {
        public string kind, subject_id, mode, action, reaction, scene_key, target_cat, pool_key;
        public NarrativeVariant[] variants;
        public string Key()=>pool_key??string.Join("/",kind,subject_id,mode,action,reaction,scene_key);
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
        public static readonly string[] Modes={"teaser","ball","plush","tunnel","pet","brush","voice","presence","pause"};
        private static readonly string[] LegacyModes={"teaser","pet","pause"};
        private string[] CatalogModes=>version>=3?Modes:LegacyModes;
        public static string[] Actions(string mode)
        {
            if(mode=="pause")return new[]{"pause"};
            if(!Modes.Contains(mode))return new string[0];
            var actions=new List<string>{"any","direct","adapt"};
            if(new[]{"teaser","ball","tunnel"}.Contains(mode))actions.Add("intense");
            if(new[]{"teaser","ball","plush","tunnel"}.Contains(mode))actions.Add("feint");
            return actions.ToArray();
        }
        public static string DefaultActionText(string mode,string action)=>DefaultActions.FirstOrDefault(r=>r.mode==mode && r.action==action)?.text;
        private static readonly NarrativeActionText[] DefaultActions={
            new NarrativeActionText{mode="teaser",action="direct",text="{human}はねこじゃらしを床の近くで揺らした。"},
            new NarrativeActionText{mode="teaser",action="adapt",text="{human}は{cat}の様子に合わせてねこじゃらしを動かした。"},
            new NarrativeActionText{mode="teaser",action="intense",text="{human}はねこじゃらしを勢いよく動かした。"},
            new NarrativeActionText{mode="teaser",action="feint",text="{human}はねこじゃらしの動きに緩急をつけた。"},
            new NarrativeActionText{mode="ball",action="direct",text="{human}はボールを床の上で転がした。"},
            new NarrativeActionText{mode="ball",action="adapt",text="{human}は{cat}の様子に合わせてボールを転がした。"},
            new NarrativeActionText{mode="ball",action="intense",text="{human}はボールを勢いよく転がした。"},
            new NarrativeActionText{mode="ball",action="feint",text="{human}はボールを転がす向きや間合いを変えた。"},
            new NarrativeActionText{mode="plush",action="direct",text="{human}はぬいぐるみを{cat}の近くで動かした。"},
            new NarrativeActionText{mode="plush",action="adapt",text="{human}は{cat}の様子に合わせてぬいぐるみを動かした。"},
            new NarrativeActionText{mode="plush",action="feint",text="{human}はぬいぐるみを動かす向きや間合いを変えた。"},
            new NarrativeActionText{mode="tunnel",action="direct",text="{human}はトンネルの入口で{cat}を遊びに誘った。"},
            new NarrativeActionText{mode="tunnel",action="adapt",text="{human}は{cat}の様子に合わせてトンネル遊びに誘った。"},
            new NarrativeActionText{mode="tunnel",action="intense",text="{human}はトンネル越しに、勢いのある動きで{cat}を遊びに誘った。"},
            new NarrativeActionText{mode="tunnel",action="feint",text="{human}はトンネルの入口で動きに緩急をつけた。"},
            new NarrativeActionText{mode="brush",action="direct",text="{human}は{cat}の毛並みに沿ってブラシを動かした。"},
            new NarrativeActionText{mode="brush",action="adapt",text="{human}は{cat}の様子に合わせて、そっとブラシを動かした。"},
            new NarrativeActionText{mode="voice",action="direct",text="{human}は{cat}に穏やかに声をかけた。"},
            new NarrativeActionText{mode="voice",action="adapt",text="{human}は{cat}の様子を見ながら、声の調子や間合いを合わせた。"},
            new NarrativeActionText{mode="presence",action="direct",text="{human}は{cat}のそばで静かに様子を見守った。"},
            new NarrativeActionText{mode="presence",action="adapt",text="{human}は{cat}の様子に合わせて距離を保ち、静かに見守った。"},
            new NarrativeActionText{mode="pet",action="direct",text="{human}は{cat}の頭をなでた。"},
            new NarrativeActionText{mode="pet",action="adapt",text="{human}は{cat}の様子に合わせ、そっとなでた。"},
            new NarrativeActionText{mode="pause",action="pause",text="{human}は手を止め、{cat}が落ち着くのを待った。"},
        };
        public static string Scene(string mode,string action)=>action=="any" || action=="pause"?mode:mode+"__"+action;
        public static string[] ImageChoices(string scene)
        {
            string mode=scene.Split(new[]{"__"},StringSplitOptions.None)[0];
            return new[]{scene,mode,mode=="teaser"?"play":"normal","normal"}.Distinct().ToArray();
        }
        public void Validate()
        {
            if((version!=1 && version!=2 && version!=3) || !Hex(pack_id,32) || !Hex(revision,64) || cat_bindings==null || cat_bindings.Length>500
                || people==null || people.Length>500
                || entries==null || entries.Length>50000 || image_bindings==null || image_bindings.Length!=CatalogModes.Sum(m=>Actions(m).Length)*cat_bindings.Length
                || action_texts==null || narration_texts==null || !Text(rest_narration,500))throw new ArgumentException("交流文カタログの見出し・版・対応が不正です。");
            var cats=new HashSet<string>();var games=new HashSet<string>();
            foreach(var c in cat_bindings)if(c==null || !Id(c.cat_id) || !games.Add(c.cat_id) || c.content_cat_id==null || c.content_cat_id.Length>100 || !cats.Add(version==1?c.cat_id:c.subject_id) || version>=2 && (!Id(c.subject_id) || !Text(c.name,100)))throw new ArgumentException("Invalid cat binding");
            if(version==1 && (cat_bindings.Length!=1 || cat_bindings[0].cat_id!="cat-mike" || people.Length!=2))throw new ArgumentException("Invalid legacy bindings");
            games.Clear();var persons=new HashSet<string>();
            foreach(var p in people)if(p==null || (version==1?(p.id!="player" && p.id!="guest-1"):!Id(p.id) || !Id(p.game_id) || !games.Add(p.game_id) || !Text(p.call,100) || !Template(p.call,false)) || !persons.Add(p.id) || !Text(p.name,100))throw new ArgumentException("交流文の人物が不正です。");
            var keys=new HashSet<string>();var ids=new HashSet<string>();
            foreach(var e in entries)
            {
                if(e==null || e.kind!="speech" && e.kind!="cat_response" || (e.kind=="speech"?!persons.Contains(e.subject_id):!cats.Contains(e.subject_id))
                    || !CatalogModes.Contains(e.mode) || !Actions(e.mode).Contains(e.action) || e.scene_key!=Scene(e.mode,e.action)
                    || (e.kind=="speech"?e.reaction!="any":!Reactions.Contains(e.reaction) || e.mode=="pause" && e.reaction!="neutral" && e.reaction!="listless")
                    || version>=2 && !PoolValid(e,cats)
                    || !keys.Add(e.Key()) || e.variants==null || e.variants.Length<1 || e.variants.Length>3)throw new ArgumentException("交流文の条件・候補が不正です。");
                int previous=0;var texts=new HashSet<string>();
                foreach(var v in e.variants)
                {
                    if(v==null || !Hex(v.id,32) || !ids.Add(v.id) || v.slot<=previous || v.slot>3 || !Text(v.text,200) || version>=2 && !Template(v.text,true) || !texts.Add(v.text))throw new ArgumentException("交流文の候補ID・順番・本文が不正です。");
                    previous=v.slot;
                }
            }
            var scenes=new HashSet<string>();
            foreach(var i in image_bindings)
            {
                if(i==null || !cat_bindings.Any(c=>c.cat_id==i.cat_id && c.content_cat_id==i.content_cat_id) || !KnownScene(i.scene_key)
                    || !scenes.Add(i.cat_id+"/"+i.scene_key) || i.image==null || i.resolved_scene_key==null
                    || (i.image==""?i.resolved_scene_key!="":!Regex.IsMatch(i.image,"^images/[a-f0-9]{64}\\.png$") || !ImageChoices(i.scene_key).Contains(i.resolved_scene_key) || i.content_cat_id==""))throw new ArgumentException("交流文の画像対応が不正です。");
            }
            if(action_texts.Length!=CatalogModes.Sum(m=>Actions(m).Count(a=>a!="any")) || narration_texts.Length!=6)throw new ArgumentException("交流文の共通文が不足しています。");
            keys.Clear();
            foreach(var a in action_texts)if(a==null || a.action=="any" || !CatalogModes.Contains(a.mode) || !Actions(a.mode).Contains(a.action) || !keys.Add(a.mode+"/"+a.action) || !Text(a.text,500)
                || a.text.Replace("{human}","").Replace("{cat}","").Any(c=>c=='{' || c=='}'))throw new ArgumentException("交流文の行動文が不正です。");
            keys.Clear();foreach(var n in narration_texts)if(n==null || !Reactions.Contains(n.reaction) || !keys.Add(n.reaction) || !Text(n.text,500))throw new ArgumentException("交流文の地の文が不正です。");
            if(ComputeRevision()!=revision)throw new ArgumentException("交流文カタログの内容とrevisionが一致しません。書き出し直してください。");
        }
        private bool KnownScene(string scene)=>CatalogModes.Any(m=>Actions(m).Any(a=>Scene(m,a)==scene));
        private static bool PoolValid(NarrativeEntry e,HashSet<string> cats)
        {
            string basis=string.Join("/",e.kind,e.subject_id,e.mode,e.action,e.reaction,e.scene_key);
            if(e.kind=="cat_response")return e.target_cat=="" && e.pool_key==basis;
            return (e.target_cat=="*" || cats.Contains(e.target_cat)) &&
                (e.pool_key==basis+"/"+e.target_cat || e.target_cat=="cat-mike" && e.pool_key==basis);
        }
        public static bool Id(string s)=>s!=null && Regex.IsMatch(s,"^[a-z][a-z0-9_-]{0,99}$");
        private static bool Template(string s,bool person)=>s!=null && !s.Replace("{cat}","").Replace(person?"{cat_call}":"{cat}","").Any(c=>c=='{' || c=='}');
        public bool SupportsCat(string id)=>cat_bindings.Any(c=>c.cat_id==id);
        public string CatName(string id)=>cat_bindings.FirstOrDefault(c=>c.cat_id==id)?.name;
        public string Expand(string text,string person,string cat)=>text.Replace("{cat_call}",(people.FirstOrDefault(p=>(version==1?p.id:p.game_id)==person)?.call??"{cat}").Replace("{cat}",cat)).Replace("{cat}",cat);
        public NarrativeEntry Find(string kind,string subject,string mode,string action,string reaction,string catId="cat-mike")
        {
            string cat=version==1?catId:cat_bindings.FirstOrDefault(c=>c.cat_id==catId)?.subject_id;
            if(version>=2)subject=kind=="speech"?people.FirstOrDefault(p=>p.game_id==subject)?.id:cat;
            foreach(var scope in new[]{cat,"*"})foreach(var operation in new[]{action,"any"})
            {var found=entries.FirstOrDefault(e=>e.kind==kind && e.subject_id==subject && e.mode==mode && e.action==operation && e.reaction==reaction && (kind!="speech" || (version==1?"cat-mike":e.target_cat)==scope));if(found!=null)return found;}
            return null;
        }
        public string ContentId(string fallback,string catId="cat-mike")=>cat_bindings.FirstOrDefault(c=>c.cat_id==catId)?.content_cat_id is string id && id!=""?id:fallback;
        public string ComputeRevision()
        {
            var b=new StringBuilder();Canonical(b,this,true,version);
            using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(b.ToString())).Select(v=>v.ToString("x2")));
        }
        private static void Quoted(StringBuilder b,string s)
        {
            b.Append('"');foreach(char c in s){switch(c){case '"':b.Append("\\\"");break;case '\\':b.Append("\\\\");break;case '\n':b.Append("\\n");break;case '\r':b.Append("\\r");break;case '\t':b.Append("\\t");break;default:if(c<32)b.Append("\\u"+((int)c).ToString("x4"));else b.Append(c);break;}}b.Append('"');
        }
        private static void Canonical(StringBuilder b,object value,bool root=false,int format=1)
        {
            if(value==null){b.Append("null");return;}if(value is string s){Quoted(b,s);return;}if(value is int n){b.Append(n.ToString(CultureInfo.InvariantCulture));return;}
            if(value is Array rows){b.Append('[');bool comma=false;foreach(var row in rows){if(comma)b.Append(',');comma=true;Canonical(b,row,false,format);}b.Append(']');return;}
            b.Append('{');bool first=true;foreach(var f in value.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public).OrderBy(f=>f.Name,StringComparer.Ordinal))
            {if(root && (f.Name=="revision" || f.Name=="pack_id"))continue;
            if(format==1 && ((value is NarrativeCatBinding && (f.Name=="subject_id" || f.Name=="name")) || (value is NarrativePerson && (f.Name=="game_id" || f.Name=="call")) || (value is NarrativeEntry && (f.Name=="target_cat" || f.Name=="pool_key"))))continue;if(!first)b.Append(',');first=false;Quoted(b,f.Name);b.Append(':');Canonical(b,f.GetValue(value),false,format);}b.Append('}');
        }
    }
}
