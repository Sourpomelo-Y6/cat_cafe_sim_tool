using System;
namespace CatCafePreview.Standalone
{
    // Presentation-only progress. Stable IDs are suitable for a future Extra index.
    [Serializable] public sealed class CafeConversationProgress
    {
        public string conversation_id, branch_id;
        public int node;
        public bool completed;
        public string[] viewed_branches;
    }
    public sealed class CafeConversationLine
    {
        public string speaker, text;
        public CafeConversationLine(string speaker,string text){this.speaker=speaker;this.text=text;}
    }
    public static class CafeIntakeConversation
    {
        public const string Id="intake.request-cat-1.v1";
        public static CafeConversationProgress Start()=>new CafeConversationProgress{conversation_id=Id,branch_id="introduction",viewed_branches=new string[0]};
        public static CafeConversationLine[] Lines(string branch,string name)
        {
            if(branch=="accepted")return new[]{
                new CafeConversationLine("あなた","この子を迎えます。まずは、ゆっくり休める場所を用意しましょう。"),
                new CafeConversationLine("依頼人","ありがとうございます。これからの暮らしを、よろしくお願いします。"),
                new CafeConversationLine("",name+"はそばへ寄ると、ゆっくりと目を細めた。急がず、この子の歩幅で慣れてもらおう。"),
                new CafeConversationLine("","受け入れの手続きが終わった。今日は休養の予定だ。出勤は営業準備で決められる。")};
            if(branch=="declined")return new[]{
                new CafeConversationLine("あなた","今回は見送ります。この子を迎える準備ができてから、責任を持って判断したいんです。"),
                new CafeConversationLine("依頼人","わかりました。考えてくださって、ありがとうございます。"),
                new CafeConversationLine("",name+"はのんびりと身を伸ばした。静かな仕草を見送り、いつもの店内へ目を戻す。"),
                new CafeConversationLine("","今回の依頼は見送った。費用や人気は変わらない。この依頼が再び届くことはない。")};
            return new[]{
                new CafeConversationLine("","営業準備のひととき。保護猫を迎えられるか、相談の依頼が届いた。"),
                new CafeConversationLine("依頼人","保護猫の受け入れについて、ご相談があります。この子がここで過ごせるか、考えていただけますか。"),
                new CafeConversationLine("依頼人","名前は"+name+"。穏やかな甘えん坊で、のんびりした子です。急がず様子を見てあげてください。"),
                new CafeConversationLine("",name+"はゆったりと顔を上げた。白黒の長い毛が揺れる。そばにいる人を見つめ、少しずつ距離を縮めてくる。"),
                new CafeConversationLine("あなた","一緒に過ごす場所と、受け入れに必要な費用を確認しましょう。無理なく迎えられるか、よく考えます。")};
        }
        public static void Validate(CafeConversationProgress p,string status)
        {
            if(p==null)return;bool answered=status=="accepted" || status=="declined";
            if(p.conversation_id!=Id || p.branch_id!="introduction" && p.branch_id!="accepted" && p.branch_id!="declined" || p.node<0 || p.node>Lines(p.branch_id,"").Length || p.completed && (!answered || p.branch_id!=status || p.node!=Lines(status,"").Length) || p.branch_id!="introduction" && p.branch_id!=status || status=="scheduled" || p.viewed_branches==null || p.completed && p.viewed_branches.Length!=1 || p.viewed_branches.Length>1 || p.viewed_branches.Length==1 && (!p.completed || p.viewed_branches[0]!=status))throw new ArgumentException("会話の保存進行が不正です。");
        }
        public static void SyncAnswer(CafeConversationProgress p,string status)
        {
            if(p!=null && (status=="accepted" || status=="declined") && p.branch_id=="introduction"){p.branch_id=status;p.node=0;}
        }
        public static void Finish(CafeConversationProgress p)
        {
            if(p.branch_id=="introduction" || p.node<Lines(p.branch_id,"").Length)return;
            p.completed=true;p.viewed_branches=new[]{p.branch_id};
        }
    }
}
