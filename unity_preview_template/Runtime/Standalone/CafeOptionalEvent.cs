using System;
using System.Linq;

namespace CatCafePreview.Standalone
{
    // Presentation receipt, deliberately outside CafeSnapshot and gameplay digests.
    [Serializable] public sealed class CafeOptionalEvent
    {
        public string event_id, revision, title, cat_id, content_cat_id, cat_name, image_resource;
        public int started_day;
        public string accept_caption, decline_caption;
        public CafeConversationLine[] introduction, accepted, declined;
        public CafeConversationProgress progress;
    }

    public static class CafeOptionalEvents
    {
        public const string MikeId="optional.mike.afternoon";
        public const string MikeRevision="1";
        public const string MikeTitle="少し近づいた午後";
        public static bool Eligible(CafeSnapshot state)
        {
            return state.day>=2 && state.phase=="preparation" && string.IsNullOrEmpty(state.game_over)
                && state.cats.Any(c=>c.id=="cat-mike" && c.activity=="cafe" && c.health=="healthy")
                && state.player_cats.Any(c=>c.cat_id=="cat-mike" && c.affinity>=20);
        }
        public static CafeOptionalEvent Start(CafeSnapshot state,string name,string contentId,string image)
        {
            if(!Eligible(state))throw new InvalidOperationException("ミケのイベント開始条件を満たしていません。");
            return new CafeOptionalEvent {
                event_id=MikeId,revision=MikeRevision,title=MikeTitle,cat_id="cat-mike",content_cat_id=contentId,cat_name=name,
                started_day=state.day,image_resource=image??"",accept_caption="静かに待つ",decline_caption="そっと名前を呼ぶ",
                introduction=new[]{
                    new CafeConversationLine("","営業準備のひととき。窓辺の光を見ていた"+name+"が、こちらへ顔を向けた。"),
                    new CafeConversationLine("あなた","ここにいてもいいかな。今日は、少しゆっくりしよう。"),
                    new CafeConversationLine("",name+"は小さく耳を動かした。すぐには近づかず、あなたの様子を見ている。")},
                accepted=new[]{
                    new CafeConversationLine("","手を伸ばさずに待っていると、"+name+"が一歩ずつ近づいてきた。"),
                    new CafeConversationLine("あなた","来てくれてありがとう。この距離から、ゆっくりでいいよ。")},
                declined=new[]{
                    new CafeConversationLine("あなた",name+"。いつも一緒にいてくれて、ありがとう。"),
                    new CafeConversationLine("","聞き慣れた声に、"+name+"は目を細めた。窓辺に座るふたりの間を、穏やかな時間が流れた。")},
                progress=new CafeConversationProgress{conversation_id=MikeId+".v"+MikeRevision,branch_id="introduction",viewed_branches=new string[0]}
            };
        }
        public static CafeConversationLine[] Lines(CafeOptionalEvent row,string branch)
        {
            return branch=="introduction"?row.introduction:branch=="accepted"?row.accepted:row.declined;
        }
        public static bool Choose(CafeOptionalEvent row,bool accept)
        {
            var p=row.progress;
            if(p.completed || p.branch_id!="introduction" || p.node!=row.introduction.Length)return false;
            p.branch_id=accept?"accepted":"declined";p.node=p.page_offset=0;return true;
        }
        public static void Finish(CafeOptionalEvent row)
        {
            var p=row.progress;if(p.branch_id=="introduction" || p.node!=Lines(row,p.branch_id).Length)return;
            p.completed=true;p.page_offset=0;p.viewed_branches=new[]{p.branch_id};
        }
        public static void Validate(CafeOptionalEvent[] rows)
        {
            if(rows==null)return;
            if(rows.Length>1)throw new ArgumentException("任意イベントの保存件数が不正です。");
            foreach(var r in rows)
            {
                if(r==null || r.event_id!=MikeId || string.IsNullOrEmpty(r.revision) || r.revision.Length>32
                    || r.cat_id!="cat-mike" || string.IsNullOrEmpty(r.content_cat_id) || r.content_cat_id.Length>256 || r.started_day<2
                    || string.IsNullOrEmpty(r.title) || r.title.Length>256 || string.IsNullOrEmpty(r.cat_name) || r.cat_name.Length>256
                    || string.IsNullOrEmpty(r.accept_caption) || r.accept_caption.Length>128 || string.IsNullOrEmpty(r.decline_caption) || r.decline_caption.Length>128
                    || (r.image_resource??"").Length>1024 || new[]{r.introduction,r.accepted,r.declined}.Any(a=>a==null || a.Length<1 || a.Length>20 || a.Any(l=>l==null || string.IsNullOrEmpty(l.text) || l.text.Length>10000 || (l.speaker??"").Length>256)))
                    throw new ArgumentException("任意イベントの保存本文が不正です。");
                var p=r.progress;
                if(p==null || p.conversation_id!=r.event_id+".v"+r.revision || !new[]{"introduction","accepted","declined"}.Contains(p.branch_id))throw new ArgumentException("任意イベントの保存分岐が不正です。");
                var lines=Lines(r,p.branch_id);
                if(p.node<0 || p.node>lines.Length || p.page_offset<0 || (p.node==lines.Length?p.page_offset!=0:p.page_offset>=lines[p.node].text.Length)
                    || p.viewed_branches==null || (p.completed?(p.branch_id=="introduction" || p.node!=lines.Length || !p.viewed_branches.SequenceEqual(new[]{p.branch_id})):p.viewed_branches.Length!=0))
                    throw new ArgumentException("任意イベントの保存位置が不正です。");
            }
        }
    }
}
