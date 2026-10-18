using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeEncounterConversation
    {
        public string event_id;
        public CafeConversationProgress progress;
    }
    // Text and viewing metadata only. All effects belong to CafeSimulation.
    public static class CafeEncounterDialogue
    {
        public const string Id="dispatch.extra_work.v1",Destination="shopping_street_event";
        public static bool Supports(CafeDispatch e)=>e?.destination==Destination && e.encounter?.id=="extra_work";
        public static CafeConversationProgress Start()=>new CafeConversationProgress{conversation_id=Id,branch_id="introduction",viewed_branches=new string[0]};
        public static CafeConversationLine[] Lines(string branch,string name)
        {
            if(branch=="accept")return new[]{
                new CafeConversationLine("あなた","追加の交流をお願いします。"+name+"の様子も、引き続き見ていてください。"),
                new CafeConversationLine("商店街の担当者","わかりました。追加の分も含めて、帰還のときにお礼をお渡ししますね。"),
                new CafeConversationLine("",name+"が耳を動かし、目の前の人へ顔を向ける。追加の仕事を引き受けることになった。疲労とストレスの変化は、すでに反映されている。")};
            if(branch=="decline")return new[]{
                new CafeConversationLine("あなた","今回は、予定どおりの仕事までにしておきます。この子のペースも大切にしたいので。"),
                new CafeConversationLine("商店街の担当者","承知しました。それでは、予定していた交流を終えたら帰還の準備をしますね。"),
                new CafeConversationLine("",name+"はその場で座り直した。追加の仕事は引き受けず、もとの予定を続ける。今回の回答による報酬・疲労・ストレスの増減はない。")};
            return new[]{
                new CafeConversationLine("","商店街のイベントへ派遣中の"+name+"について、担当者から連絡が届いた。"),
                new CafeConversationLine("商店街の担当者","予定していた交流に加えて、もう少しお仕事をお願いできますか。追加で引き受けていただいた分は、帰還時のお礼に加えます。"),
                new CafeConversationLine("",name+"が顔を上げ、ひげを小さく揺らす。連絡の内容を確かめながら、このあとの予定を考える。"),
                new CafeConversationLine("あなた","追加の交流には疲れも伴いますね。報酬だけでなく、疲労やストレスがどのくらい変わるか確認してから決めましょう。")};
        }
        public static void Sync(CafeEncounterConversation row,CafeDispatch ev)
        {
            if(ev?.encounter?.status=="resolved" && row.progress.branch_id=="introduction")
            {row.progress.branch_id=ev.encounter.choice;row.progress.node=0;row.progress.completed=false;}
        }
        public static bool ResultPending(CafeEncounterConversation r)=>r.progress!=null && !r.progress.completed && r.progress.branch_id!="introduction";
        public static void Finish(CafeConversationProgress p)
        {
            if(p==null || p.branch_id=="introduction" || p.node<Lines(p.branch_id,"").Length)return;
            p.completed=true;p.viewed_branches=(p.viewed_branches??new string[0]).Concat(new[]{p.branch_id}).Distinct().ToArray();
        }
        public static void Validate(CafeEncounterConversation[] rows,string selected,CafeSnapshot state)
        {
            rows=rows??new CafeEncounterConversation[0];
            if(rows.Length>10000 || rows.Any(r=>r==null) || rows.Select(r=>r.event_id).Distinct().Count()!=rows.Length || !string.IsNullOrEmpty(selected) && !rows.Any(r=>r.event_id==selected))throw new ArgumentException("派遣中の会話一覧が不正です。");
            foreach(var r in rows)
            {
                var ev=state.dispatches.SingleOrDefault(e=>e.id==r.event_id);var p=r.progress;
                if(!Supports(ev) || ev.encounter.status=="scheduled" || p==null || p.conversation_id!=Id || (ev.encounter.status=="waiting"?p.branch_id!="introduction":p.branch_id!=ev.encounter.choice) || p.node<0 || p.node>Lines(p.branch_id,"").Length || p.viewed_branches==null || p.viewed_branches.Distinct().Count()!=p.viewed_branches.Length || p.viewed_branches.Any(b=>b!=p.branch_id || b=="introduction") || p.completed && (p.branch_id=="introduction" || p.node!=Lines(p.branch_id,"").Length || p.viewed_branches.Length!=1) || !p.completed && p.viewed_branches.Length!=0)throw new ArgumentException("派遣中の会話進行が不正です。");
            }
        }
    }
}
