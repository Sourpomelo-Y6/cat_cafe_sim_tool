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
        const int MaxBranchLines = 20;
        static readonly int[] DefaultLineCounts = {3, 2, 2};
        public int schema_version=1;
        public string target_kind;
        public string event_id,cat_id,content_cat_id,revision,title,accept_caption,decline_caption;
        public int min_day,min_affinity,milestone;
        public bool first_meeting;
        public int Milestone()=>milestone!=0?milestone:event_id==CafeOptionalEvents.MikeId && min_affinity==20?20:first_meeting?0:-1;
        public string[] artwork;
        public CafeConversationLine[] introduction,accepted,declined;
        public CafeOptionalDefinition Copy()=>new CafeOptionalDefinition{schema_version=schema_version,target_kind=target_kind,event_id=event_id,cat_id=cat_id,content_cat_id=content_cat_id,revision=revision,title=title,accept_caption=accept_caption,decline_caption=decline_caption,min_day=min_day,min_affinity=min_affinity,milestone=milestone,first_meeting=first_meeting,artwork=artwork?.ToArray(),introduction=CopyLines(introduction),accepted=CopyLines(accepted),declined=CopyLines(declined)};
        public static CafeConversationLine[] CopyLines(CafeConversationLine[] lines)=>lines.Select(l=>new CafeConversationLine(l.speaker,l.text)).ToArray();
        public string Digest()
        {
            var identity=schema_version==1?new[]{event_id,cat_id,content_cat_id}:new[]{"2",event_id,target_kind,content_cat_id,cat_id??""};
            var values=identity.Concat(new[]{min_day.ToString(CultureInfo.InvariantCulture),min_affinity.ToString(CultureInfo.InvariantCulture),title,accept_caption,decline_caption});
            var counts = new[] { introduction.Length, accepted.Length, declined.Length };
            if (!counts.SequenceEqual(DefaultLineCounts))
                values = values.Concat(new[] { "branch_counts" }).Concat(counts.Select(count => count.ToString(CultureInfo.InvariantCulture)));
            if (artwork!=null) values=values.Concat(new[]{"event_artwork"}).Concat(artwork);
            if (first_meeting || milestone!=0) values=values.Concat(new[]{"affinity_milestone",milestone.ToString(CultureInfo.InvariantCulture)});
            values = values.Concat(introduction.Concat(accepted).Concat(declined).SelectMany(l=>new[]{l.speaker,l.text}));
            var canonical=string.Concat(values.Select(s=>s.Length.ToString(CultureInfo.InvariantCulture)+":"+s));
            using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical)).Select(b=>b.ToString("x2")));
        }
        static bool Text(string s,int max,bool empty=false)=>s!=null && (empty || !string.IsNullOrWhiteSpace(s)) && s.Length<=max && !s.Contains("<") && !s.Contains(">") && !s.Any(c=>c<32 && c!='\n' && c!='\t');
        static bool Label(string s,int max,bool empty=false)=>Text(s,max,empty) && s.IndexOfAny(new[]{'\n','\t'})<0;
        public void Validate()
        {
            bool individual=schema_version==1 || target_kind=="individual";
            if((schema_version!=1 && schema_version!=2) || (schema_version==2 && target_kind!="content" && target_kind!="individual")
                || !CafeOptionalEvents.ValidEventId(event_id) || !CafeOptionalEvents.ValidCatId(content_cat_id)
                || (individual && (event_id!=CafeOptionalEvents.MikeId || cat_id!="cat-mike" || content_cat_id!="playtest-mike"))
                || (!individual && cat_id!=null) || min_day<1 || min_day>9999 || min_affinity<0 || min_affinity>100
                || !CafeOptionalEvents.ValidArtwork(artwork) || (schema_version==1 && artwork!=null)
                || (first_meeting && milestone!=0)
                || ((first_meeting || milestone!=0) && (schema_version!=2 || !CafeOptionalEvents.ValidMilestone(milestone) || min_affinity!=milestone || event_id==CafeOptionalEvents.MikeId && milestone!=20))
                || !Label(title,30) || !Label(accept_caption,24) || !Label(decline_caption,24))throw new ArgumentException("任意イベント定義の項目が不正です。");
            var branches = new[] { introduction, accepted, declined };
            for (int i = 0; i < branches.Length; i++)
            {
                var lines = branches[i];
                if (lines == null || lines.Length < 1 || lines.Length > MaxBranchLines
                    || (schema_version == 1 && lines.Length != DefaultLineCounts[i])
                    || lines.Any(l => l == null || !Label(l.speaker, 7, true) || !Text(l.text, 1000)))
                    throw new ArgumentException("任意イベントの本文・分岐が不正です。");
            }
            if(revision!=Digest())throw new ArgumentException("任意イベントの本文と版が一致しません。");
        }
        public bool Matches(string cat,string content)=>content==content_cat_id && (schema_version==2 && target_kind=="content" || cat==cat_id);
        public bool Eligible(CafeSnapshot s)=>Eligible(s,cat_id);
        public bool Eligible(CafeSnapshot s,string cat)=>s.day>=min_day && s.phase=="preparation" && string.IsNullOrEmpty(s.game_over)
            && s.cats.Any(c=>c.id==cat && c.activity=="cafe" && c.health=="healthy") && s.player_cats.Any(c=>c.cat_id==cat && !double.IsNaN(c.affinity) && !double.IsInfinity(c.affinity) && c.affinity>=min_affinity && (!first_meeting || c.affinity==0));
        public CafeOptionalEvent Start(CafeSnapshot state,string name,string image)=>Start(state,cat_id,name,image);
        public CafeOptionalEvent Start(CafeSnapshot state,string cat,string name,string image)
        {
            Validate();if(!Eligible(state,cat))throw new InvalidOperationException("任意イベントの開始条件を満たしていません。");
            return new CafeOptionalEvent{event_id=event_id,revision=revision,title=title,cat_id=cat,content_cat_id=content_cat_id,cat_name=name,image_resource=image??"",started_day=state.day,milestone=milestone,first_meeting=first_meeting,artwork=artwork?.ToArray(),
                accept_caption=accept_caption,decline_caption=decline_caption,introduction=CopyLines(introduction),accepted=CopyLines(accepted),declined=CopyLines(declined),
                progress=new CafeConversationProgress{conversation_id=event_id+".v"+revision,branch_id="introduction",viewed_branches=new string[0]}};
        }
    }
    [Serializable] public sealed class CafeOptionalEventCatalog
    {
        public int version=1;public string pack_id;public CafeOptionalDefinition[] events;
        public void Validate()
        {
            if((version!=1 && version!=2) || !Regex.IsMatch(pack_id??"","\\A[a-f0-9]{32}\\z") || events==null || events.Length>(version==1?1:448) || events.Any(e=>e==null))throw new ArgumentException("任意イベントパックが不正です。");
            if(events.Select(e=>e.event_id).Distinct().Count()!=events.Length || events.Select(e=>e.content_cat_id).Distinct().Count()>64 || events.Select(e=>new{e.content_cat_id,Milestone=e.Milestone()}).Distinct().Count()!=events.Length)throw new ArgumentException("任意イベントが重複しています。");
            foreach(var row in events){if(row.schema_version!=version)throw new ArgumentException("任意イベントの形式が一致しません。");row.Validate();}
        }
        public CafeOptionalEventCatalog Copy()=>new CafeOptionalEventCatalog{version=version,pack_id=pack_id,events=events.Select(e=>e.Copy()).ToArray()};
    }
}
