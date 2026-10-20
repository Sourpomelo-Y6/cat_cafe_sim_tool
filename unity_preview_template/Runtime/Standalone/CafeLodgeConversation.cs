using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeLodgeConversation
    {
        public string event_id;
        public CafeConversationProgress response,returning;
        public CafeConversationProgress Current=>response?.completed==false?response:returning;
    }
    // Viewing metadata only. The dispatch record is the authority for every effect.
    public static class CafeLodgeDialogue
    {
        public const string Id="dispatch.mountain_lodge_trouble.v1";
        public static bool Supports(CafeDispatch e)=>e?.destination=="mountain_lodge_visit" && CafeSimulation.DispatchInterrupted(e);
        public static CafeConversationProgress Start(bool returning)=>new CafeConversationProgress{conversation_id=Id+(returning?".return":".response"),branch_id="introduction",viewed_branches=new string[0]};
        public static CafeConversationLine[] Lines(bool returning,string branch,string name)
        {
            if(returning)return branch=="received"?new[]{
                new CafeConversationLine("あなた",name+"、おかえりなさい。帰還の確認を終えました。今の体調を見て、店での予定を考えましょう。"),
                new CafeConversationLine("","帰還処理は済んでいる。中断した派遣の報酬と派遣経験は0。体力は最大へ、ストレスは既定値へ反映済みだ。健康状態と出勤予定は猫の詳細で確認しよう。")}:new[]{
                new CafeConversationLine("","山あいの宿への派遣を中断した"+name+"の記録が、帰還確認待ちになっている。"),
                new CafeConversationLine("あなた","帰還の手続きを確認しましょう。確認後の体調と出勤予定も、改めて見ておきます。")};
            if(branch=="search")return new[]{
                new CafeConversationLine("あなた","費用を払って捜索を依頼します。"),
                new CafeConversationLine("","捜索の回答と費用の支払いが確定した。派遣記録は帰還確認待ちになっている。店への帰還処理は、別途確認する必要がある。"),
                new CafeConversationLine("あなた","この会話を終えたら、イベント一覧の「一般派遣・帰還」または「山あいの宿・トラブル対応」で帰還を確認しましょう。")};
            if(branch=="wait")return new[]{
                new CafeConversationLine("あなた","費用をかけず、帰還を待つことにします。"),
                new CafeConversationLine("","待機の回答が確定した。まだ帰還の確認はできない。家出当日を除く待機日程が終わるまで、猫は店外・出勤予定から外れたままだ。"),
                new CafeConversationLine("あなた","この結果会話を終えて閉じ、営業か休業で日程を進めましょう。帰還確認待ちになったら、イベント一覧で確認します。")};
            return new[]{
                new CafeConversationLine("","山あいの宿へ派遣中の"+name+"について、連絡が届いた。"),
                new CafeConversationLine("宿の担当者",name+"が宿からいなくなっています。派遣は中断となりました。"),
                new CafeConversationLine("あなた","今は帰還が確定していないのですね。捜索を依頼するか、帰還を待つか、費用と日程を確認して決めます。")};
        }
        public static void Sync(CafeLodgeConversation row,CafeDispatch e)
        {
            if(!Supports(e))return;
            if(row.response?.branch_id=="introduction" && e.trouble.status=="resolved")SetBranch(row.response,e.trouble.choice);
            if(row.returning?.branch_id=="introduction" && e.status=="resolved")SetBranch(row.returning,"received");
        }
        static void SetBranch(CafeConversationProgress p,string branch){p.branch_id=branch;p.node=p.page_offset=0;p.completed=false;}
        public static bool ResultPending(CafeLodgeConversation r)=>new[]{r.response,r.returning}.Any(p=>p!=null && !p.completed && p.branch_id!="introduction");
        public static void Finish(CafeLodgeConversation r)
        {
            var p=r.Current;if(p==null || p.branch_id=="introduction" || p.node<Lines(ReferenceEquals(p,r.returning),p.branch_id,"").Length)return;
            p.completed=true;p.page_offset=0;p.viewed_branches=new[]{p.branch_id};
        }
        public static void Validate(CafeLodgeConversation[] rows,string selected,CafeSnapshot state)
        {
            rows=rows??new CafeLodgeConversation[0];
            if(rows.Length>10000 || rows.Any(r=>r==null) || rows.Select(r=>r.event_id).Distinct().Count()!=rows.Length || !string.IsNullOrEmpty(selected) && !rows.Any(r=>r.event_id==selected))throw new ArgumentException("宿トラブル会話の一覧が不正です。");
            foreach(var r in rows)
            {
                var e=state.dispatches.SingleOrDefault(x=>x.id==r.event_id);
                if(!Supports(e) || r.response==null && r.returning==null || r.returning!=null && (e.status=="missing" || r.response?.completed==false))throw new ArgumentException("宿トラブル会話の対象が不正です。");
                ValidatePhase(r.response,false,e.trouble.status=="resolved"?e.trouble.choice:"introduction");
                ValidatePhase(r.returning,true,e.status=="resolved"?"received":"introduction");
            }
        }
        static void ValidatePhase(CafeConversationProgress p,bool returning,string branch)
        {
            if(p==null)return;var lines=Lines(returning,branch,"");
            if(p.conversation_id!=Id+(returning?".return":".response") || p.branch_id!=branch || p.node<0 || p.node>lines.Length || p.page_offset<0 || p.node==lines.Length && p.page_offset!=0 || p.node<lines.Length && p.page_offset>lines[p.node].text.Length+256 || p.viewed_branches==null || (p.completed?(branch=="introduction" || p.node!=lines.Length || p.viewed_branches.Length!=1 || p.viewed_branches[0]!=branch):p.viewed_branches.Length!=0))throw new ArgumentException("宿トラブル会話の進行が不正です。");
        }
    }
}
