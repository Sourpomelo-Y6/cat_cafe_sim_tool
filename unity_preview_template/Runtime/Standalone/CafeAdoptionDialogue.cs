using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeAdoptionConversation
    {
        public string event_id;
        public CafeConversationProgress progress;
    }
    public static class CafeAdoptionDialogue
    {
        public const string Id="adoption.offer.v1";
        public const string Consequences="ゲーム内では復帰できません。接客・派遣・個別交流の対象から外れます。金銭報酬はありません。猫の詳細・関係の記録は保持します。";
        public static CafeConversationProgress Start()=>new CafeConversationProgress{conversation_id=Id,branch_id="introduction",viewed_branches=new string[0]};
        public static CafeConversationLine[] Lines(string branch,string name,string customer,CafeAdoptionEvent e)
        {
            if(branch=="accept")return new[]{
                new CafeConversationLine("あなた",name+"の譲渡を確定しました。これまで店で一緒に過ごした時間も、大切な記録です。"),
                new CafeConversationLine("",customer+"への譲渡が成立した。"+name+"は接客・派遣・個別交流の対象から外れる。金銭報酬はなく、猫の詳細と関係の記録は残る。"),
                new CafeConversationLine("あなた","送り出すと決めたことを、大切に受け止めます。"+name+"との記録は、これからもここで見返せます。")};
            if(branch=="decline")return new[]{
                new CafeConversationLine("あなた","今回は譲渡を見送ります。"+name+"とは、引き続きこの店で過ごすことにします。"),
                new CafeConversationLine("",customer+"からの今回の申し出は見送りとして記録された。譲渡は行わず、"+name+"の在籍と関係の記録を保つ。"),
                new CafeConversationLine("あなた","店で一緒に過ごす選択も、大切にしたいと思います。また申し出があったときは、そのときに考えましょう。")};
            return new[]{
                new CafeConversationLine("",e.day+"日目、"+customer+"から、"+name+"を迎えたいという申し出が届いた。"),
                new CafeConversationLine(customer,name+"を迎えることについて、相談させてください。"),
                new CafeConversationLine("","申し出時の記録では、お客さんへの親しみは"+e.guest_affinity.ToString("0.#")+"、プレイヤー好感度は"+e.player_affinity.ToString("0.#")+"。接客で築いた関係に沿った申し出だ。"),
                new CafeConversationLine("あなた","送り出すことも、この店で一緒に過ごすことも、大切に考えます。変更されることを確認してから決めましょう。")};
        }
        public static void Sync(CafeAdoptionConversation r,CafeAdoptionEvent e)
        {if(r.progress.branch_id=="introduction" && e?.status=="resolved"){r.progress.branch_id=e.choice;r.progress.node=r.progress.page_offset=0;}}
        public static bool ResultPending(CafeAdoptionConversation r)=>r.progress!=null && !r.progress.completed && r.progress.branch_id!="introduction";
        public static void Finish(CafeAdoptionConversation r)
        {var p=r.progress;if(p.branch_id=="introduction" || p.node<3)return;p.completed=true;p.page_offset=0;p.viewed_branches=new[]{p.branch_id};}
        public static void Validate(CafeAdoptionConversation[] rows,string selected,CafeSnapshot state)
        {
            rows=rows??new CafeAdoptionConversation[0];
            if(rows.Length>10000 || rows.Any(r=>r==null) || rows.Select(r=>r.event_id).Distinct().Count()!=rows.Length || !string.IsNullOrEmpty(selected) && !rows.Any(r=>r.event_id==selected))throw new ArgumentException("譲渡会話の一覧が不正です。");
            foreach(var r in rows)
            {
                var e=state.adoption?.events.SingleOrDefault(x=>x.id==r.event_id);var p=r.progress;
                if(e==null || p==null)throw new ArgumentException("譲渡会話の対象が不正です。");
                string branch=e.status=="waiting"?"introduction":e.choice;int count=branch=="introduction"?4:3;
                if(p.conversation_id!=Id || p.branch_id!=branch || p.node<0 || p.node>count || p.page_offset<0 || p.page_offset>4096 || p.node==count && p.page_offset!=0 || p.viewed_branches==null || (p.completed?(branch=="introduction" || p.node!=count || p.viewed_branches.Length!=1 || p.viewed_branches[0]!=branch):p.viewed_branches.Length!=0))throw new ArgumentException("譲渡会話の進行が不正です。");
            }
        }
    }
}
