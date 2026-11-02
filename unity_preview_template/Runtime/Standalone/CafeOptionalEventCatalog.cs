using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Globalization;

namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeOptionalDefinition
    {
        public string event_id,cat_id,content_cat_id,revision,title,accept_caption,decline_caption;
        public int min_day,min_affinity;
        public CafeConversationLine[] introduction,accepted,declined;
        public CafeOptionalDefinition Copy()=>new CafeOptionalDefinition{event_id=event_id,cat_id=cat_id,content_cat_id=content_cat_id,revision=revision,title=title,accept_caption=accept_caption,decline_caption=decline_caption,min_day=min_day,min_affinity=min_affinity,introduction=CopyLines(introduction),accepted=CopyLines(accepted),declined=CopyLines(declined)};
        public static CafeConversationLine[] CopyLines(CafeConversationLine[] lines)=>lines.Select(l=>new CafeConversationLine(l.speaker,l.text)).ToArray();
        public string Digest()
        {
            var values=new[]{event_id,cat_id,content_cat_id,min_day.ToString(CultureInfo.InvariantCulture),min_affinity.ToString(CultureInfo.InvariantCulture),title,accept_caption,decline_caption}
                .Concat(introduction.Concat(accepted).Concat(declined).SelectMany(l=>new[]{l.speaker,l.text}));
            var canonical=string.Concat(values.Select(s=>s.Length.ToString(CultureInfo.InvariantCulture)+":"+s));
            using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical)).Select(b=>b.ToString("x2")));
        }
        static bool Text(string s,int max,bool empty=false)=>s!=null && (empty || !string.IsNullOrWhiteSpace(s)) && s.Length<=max && !s.Contains("<") && !s.Contains(">") && !s.Any(c=>c<32 && c!='\n' && c!='\t');
        static bool Label(string s,int max,bool empty=false)=>Text(s,max,empty) && s.IndexOfAny(new[]{'\n','\t'})<0;
        public void Validate()
        {
            if(event_id!=CafeOptionalEvents.MikeId || cat_id!="cat-mike" || content_cat_id!="playtest-mike" || min_day<1 || min_day>9999 || min_affinity<0 || min_affinity>100
                || !Label(title,30) || !Label(accept_caption,24) || !Label(decline_caption,24))throw new ArgumentException("任意イベント定義の項目が不正です。");
            var branches=new[]{introduction,accepted,declined};var counts=new[]{3,2,2};
            for(int i=0;i<3;i++)if(branches[i]==null || branches[i].Length!=counts[i] || branches[i].Any(l=>l==null || !Label(l.speaker,7,true) || !Text(l.text,1000)))throw new ArgumentException("任意イベントの本文・分岐が不正です。");
            if(revision!=Digest())throw new ArgumentException("任意イベントの本文と版が一致しません。");
        }
        public bool Eligible(CafeSnapshot s)=>s.day>=min_day && s.phase=="preparation" && string.IsNullOrEmpty(s.game_over)
            && s.cats.Any(c=>c.id==cat_id && c.activity=="cafe" && c.health=="healthy") && s.player_cats.Any(c=>c.cat_id==cat_id && c.affinity>=min_affinity);
        public CafeOptionalEvent Start(CafeSnapshot state,string name,string image)
        {
            Validate();if(!Eligible(state))throw new InvalidOperationException("任意イベントの開始条件を満たしていません。");
            return new CafeOptionalEvent{event_id=event_id,revision=revision,title=title,cat_id=cat_id,content_cat_id=content_cat_id,cat_name=name,image_resource=image??"",started_day=state.day,
                accept_caption=accept_caption,decline_caption=decline_caption,introduction=CopyLines(introduction),accepted=CopyLines(accepted),declined=CopyLines(declined),
                progress=new CafeConversationProgress{conversation_id=event_id+".v"+revision,branch_id="introduction",viewed_branches=new string[0]}};
        }
    }
    [Serializable] public sealed class CafeOptionalEventCatalog
    {
        public int version=1;public string pack_id;public CafeOptionalDefinition[] events;
        public void Validate()
        {
            if(version!=1 || !Regex.IsMatch(pack_id??"","^[a-f0-9]{32}$") || events==null || events.Length>1 || events.Any(e=>e==null))throw new ArgumentException("任意イベントパックが不正です。");
            foreach(var row in events)row.Validate();
        }
        public CafeOptionalEventCatalog Copy()=>new CafeOptionalEventCatalog{version=version,pack_id=pack_id,events=events.Select(e=>e.Copy()).ToArray()};
    }
}
