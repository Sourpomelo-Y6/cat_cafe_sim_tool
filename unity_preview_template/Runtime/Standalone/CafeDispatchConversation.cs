using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeDispatchConversation
    {
        public string event_id,cat_id,stage="departure";
        public int day;
        public CafeConversationProgress departure,travelling,returning;
        public CafeConversationProgress Current=>stage=="departure"?departure:stage=="travelling"?travelling:returning;
    }
    public static class CafeDispatchDialogue
    {
        public const string Destination="neighborhood_visit",Id="dispatch.neighborhood_visit.v1";
        public static CafeConversationProgress Start(string stage)=>new CafeConversationProgress{conversation_id=Id+"."+stage,branch_id=stage=="travelling"?"travelling":"introduction",viewed_branches=new string[0]};
        public static CafeConversationLine[] Lines(string stage,string branch,string name)
        {
            if(stage=="travelling")return new[]{
                new CafeConversationLine("","派遣の記録を開いた。"+name+"は、近所のお店への訪問に出ている。"),
                new CafeConversationLine("あなた","帰ってくるまで、店内の猫たちと営業の準備を進めましょう。残りの日程は派遣の記録で確認できます。"),
                new CafeConversationLine("","派遣の日程は営業の閉店や休業で進む。この会話を読んでも日数や報酬は変わらない。")};
            if(stage=="return")
            {
                if(branch=="received")return new[]{
                    new CafeConversationLine("お店の人","訪問してくれて、ありがとうございました。こちらが今回のお礼です。"),
                    new CafeConversationLine("あなた",name+"、おかえりなさい。まずは様子を見てから、次の予定を決めましょう。"),
                    new CafeConversationLine("","報酬とケア用品、派遣の経験は受け取り済みだ。出勤には自動で戻らないので、営業準備で予定を確認しよう。")};
                return new[]{
                    new CafeConversationLine("",name+"が近所のお店から戻ってきた。訪問の記録と、帰還時の状態を確かめる。"),
                    new CafeConversationLine("お店の人","訪問のお礼をお渡ししたいので、受け取りの確認をお願いします。"),
                    new CafeConversationLine("あなた","ありがとうございます。今回の報酬と、この子の状態を確認しますね。")};
            }
            if(branch=="departed")return new[]{
                new CafeConversationLine("あなた","それでは、"+name+"にお願いします。店内の出勤予定も確認しておきましょう。"),
                new CafeConversationLine("お店の人","ありがとうございます。訪問が終わったら、帰還の確認をお願いします。"),
                new CafeConversationLine("",name+"の派遣手続きが終わった。出勤予定から外れ、帰還を待つことになった。")};
            if(branch=="cancelled")return new[]{
                new CafeConversationLine("あなた","今回は、まだ出発しないでおきます。猫たちの状態を確認してから、改めて考えますね。"),
                new CafeConversationLine("","出発はしていない。費用や日程は変わらず、条件が整えば同じ日にもう一度相談できる。")};
            return new[]{
                new CafeConversationLine("","営業準備の時間。近所のお店への訪問について、相談が届いている。"),
                new CafeConversationLine("お店の人","こちらへの訪問をお願いできますか。どの子に来てもらうかは、体調やお店の予定を見て決めてください。"),
                new CafeConversationLine("あなた","今回は"+name+"を候補に考えています。出発の条件と、帰還までの日程を確認しましょう。"),
                new CafeConversationLine("",name+"が顔を上げた。出発前に様子を見て、無理のない予定かもう一度確かめる。")};
        }
        public static void Sync(CafeDispatchConversation row,CafeDispatch ev)
        {
            if(ev==null)return;
            if(row.departure?.branch_id=="introduction"){row.departure.branch_id="departed";row.departure.node=0;row.departure.completed=false;}
            if(ev.status=="resolved" && row.returning?.branch_id=="introduction"){row.returning.branch_id="received";row.returning.node=0;}
            if(ev.status=="waiting" && row.departure?.completed!=false){row.returning=row.returning??Start("return");row.stage="return";}
        }
        public static bool ResultPending(CafeDispatchConversation row)=>new[]{row.departure,row.returning}.Any(p=>p!=null && !p.completed && p.branch_id!="introduction");
        public static void Finish(CafeDispatchConversation row)
        {
            var p=row.Current;if(p==null || p.branch_id=="introduction" || p.node<Lines(row.stage,p.branch_id,"").Length)return;
            p.completed=true;p.viewed_branches=(p.viewed_branches??new string[0]).Concat(new[]{p.branch_id}).Distinct().ToArray();
        }
        public static void Validate(CafeDispatchConversation[] rows,string selected,CafeSnapshot state)
        {
            rows=rows??new CafeDispatchConversation[0];
            if(rows.Length>10000 || rows.Any(r=>r==null) || rows.Select(r=>r.event_id).Distinct().Count()!=rows.Length || !string.IsNullOrEmpty(selected) && !rows.Any(r=>r.event_id==selected))throw new ArgumentException("派遣会話の一覧が不正です。");
            foreach(var r in rows)
            {
                var ev=state.dispatches.SingleOrDefault(e=>e.id==r.event_id);
                if(!state.cats.Any(c=>c.id==r.cat_id) || r.day<1 || r.day>state.day || r.event_id!="dispatch-"+r.day+"-"+r.cat_id || ev!=null && ev.destination!=Destination || r.stage!="departure" && r.stage!="travelling" && r.stage!="return" || r.Current==null || ev==null && (r.travelling!=null || r.returning!=null) || r.departure==null && ev==null)throw new ArgumentException("派遣会話の対象が不正です。");
                ValidatePhase(r.departure,"departure",ev);ValidatePhase(r.travelling,"travelling",ev);ValidatePhase(r.returning,"return",ev);
            }
        }
        static void ValidatePhase(CafeConversationProgress p,string stage,CafeDispatch ev)
        {
            if(p==null)return;
            string[] allowed=stage=="departure"?new[]{"introduction","departed","cancelled"}:stage=="return"?new[]{"introduction","received"}:new[]{"travelling"};
            string[] completed=stage=="departure"?new[]{"departed","cancelled"}:stage=="return"?new[]{"received"}:new[]{"travelling"};
            bool invalid=p.conversation_id!=Id+"."+stage || !allowed.Contains(p.branch_id) || p.node<0 || p.node>Lines(stage,p.branch_id,"").Length || p.viewed_branches==null;
            if(!invalid)invalid=p.viewed_branches.Distinct().Count()!=p.viewed_branches.Length || p.viewed_branches.Any(b=>!completed.Contains(b)) || p.completed && (p.branch_id=="introduction" || p.node!=Lines(stage,p.branch_id,"").Length || !p.viewed_branches.Contains(p.branch_id)) || !p.completed && p.viewed_branches.Any(b=>b!="cancelled");
            if(stage=="departure")invalid|=ev==null?p.branch_id=="departed" || (p.viewed_branches??new string[0]).Contains("departed"):p.branch_id!="departed";
            if(stage=="return")invalid|=ev==null || ev.status=="travelling" || (ev.status=="resolved"?p.branch_id!="received":p.branch_id!="introduction");
            if(stage=="travelling")invalid|=ev==null;
            if(invalid)throw new ArgumentException("派遣会話の保存進行が不正です。");
        }
    }
}
