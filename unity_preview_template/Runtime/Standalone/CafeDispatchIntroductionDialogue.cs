using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeDispatchIntroductionConversation
    {
        public string event_id;
        public CafeConversationProgress progress;
    }
    // Viewing metadata and script only; joining, costs and candidate identity remain game rules.
    public static class CafeDispatchIntroductionDialogue
    {
        public const string Id="dispatch.shopping_street_introduction.v1",Destination="shopping_street_event";
        public static bool Supports(CafeDispatch e)=>e?.destination==Destination && e.introduction!=null;
        public static CafeConversationProgress Start()=>new CafeConversationProgress{conversation_id=Id,branch_id="introduction",viewed_branches=new string[0]};
        public static CafeConversationLine[] Lines(string branch,string name,string personality="",string features="",string trait="")
        {
            if(branch=="accepted")return new[]{
                new CafeConversationLine("あなた","この子を迎えます。まずは落ち着いて休める場所を用意して、少しずつ慣れてもらいましょう。"),
                new CafeConversationLine("商店街の担当者","ありがとうございます。この子のペースで、よろしくお願いします。"),
                new CafeConversationLine("",name+"は少し先へ前足を出し、こちらを見上げた。急に手を伸ばさず、近づいてくるのを静かに待つ。"),
                new CafeConversationLine("","受け入れの手続きが終わった。初期費用は支払い済み。この子は健康・体力全回復、疲労・ストレス・好感度0の休養予定で加わった。出勤は営業準備で決めよう。")};
            if(branch=="declined")return new[]{
                new CafeConversationLine("あなた","今回は、受け入れを見送ります。この子を迎える場所と、無理なく過ごせる予定を整えてから考えたいんです。"),
                new CafeConversationLine("商店街の担当者","わかりました。よく考えてくださって、ありがとうございます。"),
                new CafeConversationLine("",name+"は耳を動かすと、前足のそばへ視線を落とした。小さな仕草を見守り、今回の相談を終える。"),
                new CafeConversationLine("","この候補の受け入れを見送った。費用・人気・受け取った派遣報酬は変わらず、今回の回答は変更できない。")};
            return new[]{
                new CafeConversationLine("","商店街への派遣が終わり、帰還の確認も済んだ営業準備の時間。担当者から、猫の紹介が届いている。"),
                new CafeConversationLine("商店街の担当者","訪問してくださって、ありがとうございました。猫の受け入れについても、ご相談したいことがあります。この子を紹介させてください。"),
                new CafeConversationLine("商店街の担当者","名前は"+name+"。個性は「"+personality+"」、特徴は"+features+"で、特性は「"+trait+"」です。すぐに決めず、様子を見て考えてくださいね。"),
                new CafeConversationLine("",name+"は動くものへ目を向け、前足を少し浮かせた。こちらへ一歩近づくと、立ち止まって様子をうかがう。遊びへの興味を大切にしながら、強く誘いすぎないようにしよう。"),
                new CafeConversationLine("あなた","この子が落ち着いて過ごせる場所と、受け入れの費用を確認しましょう。迎えるかどうかは、そのうえで決めます。")};
        }
        public static void Sync(CafeDispatchIntroductionConversation r,CafeDispatch e){if(e!=null)CafeIntakeConversation.SyncAnswer(r.progress,e.introduction.status);}
        public static bool ResultPending(CafeDispatchIntroductionConversation r)=>r.progress!=null && !r.progress.completed && r.progress.branch_id!="introduction";
        public static void Finish(CafeConversationProgress p){if(p!=null && p.branch_id!="introduction" && p.node>=Lines(p.branch_id,"").Length){p.completed=true;p.viewed_branches=new[]{p.branch_id};}}
        public static void Validate(CafeDispatchIntroductionConversation[] rows,string selected,CafeSnapshot state)
        {
            rows=rows??new CafeDispatchIntroductionConversation[0];
            if(rows.Length>10000 || rows.Any(r=>r==null) || rows.Select(r=>r.event_id).Distinct().Count()!=rows.Length || !string.IsNullOrEmpty(selected) && !rows.Any(r=>r.event_id==selected))throw new ArgumentException("派遣紹介の会話一覧が不正です。");
            foreach(var r in rows)
            {
                var ev=state.dispatches.SingleOrDefault(e=>e.id==r.event_id);var v=ev?.introduction;var q=r.progress;
                if(!Supports(ev) || v.status=="scheduled" || q==null || (v.status=="waiting"?q.branch_id!="introduction":q.branch_id!=v.status))throw new ArgumentException("派遣紹介の会話対象・回答が不正です。");
                CafeIntakeConversation.ValidateProgress(q,v.status,Id,"scheduled",(b,n)=>Lines(b,n));
            }
        }
    }
}
