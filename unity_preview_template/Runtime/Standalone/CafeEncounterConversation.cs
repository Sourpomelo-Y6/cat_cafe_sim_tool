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
        public const string RestId="dispatch.rest_stop.v1", PhotoId="dispatch.extra_photo_session.v1";
        public const string ExerciseId="dispatch.exercise_program.v1", ReadingId="dispatch.reading_extension.v1";
        public static bool Supports(CafeDispatch e)=>e!=null && ((e.destination=="cat_exercise_class" && e.encounter?.id=="exercise_program") || (e.destination=="quiet_reading_salon" && e.encounter?.id=="reading_extension") || (e.destination==Destination && e.encounter?.id=="extra_work") || (e.destination=="out_of_town_visit" && e.encounter?.id=="rest_stop") || (e.destination=="cat_photo_studio" && e.encounter?.id=="extra_photo_session"));
        public static string ConversationId(CafeDispatch e)=>e.destination=="cat_exercise_class"?ExerciseId:e.destination=="quiet_reading_salon"?ReadingId:e.destination=="out_of_town_visit"?RestId:e.destination=="cat_photo_studio"?PhotoId:Id;
        public static string FirstChoice(string id)=>id==RestId?"rest":"accept";
        public static string SecondChoice(string id)=>id==RestId?"continue":"decline";
        public static string Title(string id)=>id==ExerciseId?"追加の運動":id==ReadingId?"触れ合いの延長":id==RestId?"郊外での休憩":id==PhotoId?"追加撮影":"商店街からの追加のお願い";
        public static CafeConversationProgress Start(CafeDispatch e){var p=Start();p.conversation_id=ConversationId(e);return p;}
        public static CafeConversationLine[] Lines(string branch,string name,string id)
        {
            if(id==Id)return Lines(branch,name);
            if(id==ExerciseId || id==ReadingId)
            {
                bool exercise=id==ExerciseId, accept=branch=="accept";
                string activity=exercise?"追加の運動":"触れ合いの延長", contact=exercise?"運動教室担当":"サロン担当者";
                if(branch=="introduction")return new[]{
                    new CafeConversationLine("",name+"の派遣先から、"+activity+"について相談が届いた。"),
                    new CafeConversationLine(contact,exercise?"予定のプログラムのあとに、追加の運動に参加できます。参加分のお礼は帰還時にお渡しします。":"予定の時間のあとも、触れ合いを延長していただけますか。延長分のお礼は帰還時にお渡しします。"),
                    new CafeConversationLine("",name+"の今の状態を確かめ、参加する場合と予定どおり終える場合を比べる。"),
                    new CafeConversationLine("あなた","報酬だけでなく、疲労とストレスの変化も確認して決めましょう。")};
                return new[]{
                    new CafeConversationLine("あなた",accept?activity+"に参加します。"+name+"の様子を見ながらお願いします。":"今回は予定どおり終えてください。"),
                    new CafeConversationLine(contact,accept?"承知しました。参加分のお礼は帰還時にまとめます。":"承知しました。追加分には参加せず、もとの予定で終えますね。"),
                    new CafeConversationLine("",name+(accept?"は"+activity+"に参加することになった。":"は追加分には参加せず、予定どおり終えることになった。")+"回答による疲労・ストレスの変化は反映済み。報酬は帰還時に受け取る。")};
            }
            bool rest=id==RestId, first=branch==FirstChoice(id);
            string person=rest?"訪問先の担当者":"撮影スタジオの担当者";
            if(branch=="introduction")return new[]{
                new CafeConversationLine("",name+"の派遣先から、このあとの予定について連絡が届いた。"),
                new CafeConversationLine(person,rest?"移動の途中で、休憩を取れます。費用は帰還時の報酬から差し引く形になります。":"予定の撮影に加えて、もう少し撮影を続けていただけますか。追加のお礼は帰還時にお渡しします。"),
                new CafeConversationLine("",name+"が体の向きを変え、周囲を見渡している。連絡を確かめ、このあとの過ごし方を考える。"),
                new CafeConversationLine("あなた",rest?"休憩する場合と予定どおり進む場合の報酬や体調の変化を、確認しましょう。":"切り上げる場合にも負担はありますね。どちらの変化も確認して決めましょう。")};
            return new[]{
                new CafeConversationLine("あなた",rest?(first?"報酬の一部を使って、休憩をお願いします。":"今回は予定どおり進めてください。"):(first?"追加撮影をお願いします。様子も引き続き見ていてください。":"追加の撮影は断って、ここで切り上げます。")),
                new CafeConversationLine(person,rest?(first?"承知しました。休憩分は帰還時のお礼から差し引きます。":"承知しました。もとの予定で進めますね。"):(first?"ありがとうございます。追加のお礼は帰還時にまとめますね。":"承知しました。撮影を切り上げて、帰還の準備をしますね。")),
                new CafeConversationLine("",name+(rest?(first?"は休憩を取ることになった。所持金からの支出はなく、帰還報酬が減額される。":"はもとの予定を続けることになった。"):(first?"は追加の撮影を続けることになった。":"は撮影を切り上げることになった。切り上げる場合の疲労とストレスの変化も反映された。"))+"疲労・ストレスは回答時、報酬は帰還時に反映される。")};
        }
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
            {row.progress.branch_id=ev.encounter.choice;row.progress.node=row.progress.page_offset=0;row.progress.completed=false;}
        }
        public static bool ResultPending(CafeEncounterConversation r)=>r.progress!=null && !r.progress.completed && r.progress.branch_id!="introduction";
        public static void Finish(CafeConversationProgress p)
        {
            if(p==null || p.branch_id=="introduction" || p.node<Lines(p.branch_id,"",p.conversation_id).Length)return;
            p.completed=true;p.page_offset=0;p.viewed_branches=(p.viewed_branches??new string[0]).Concat(new[]{p.branch_id}).Distinct().ToArray();
        }
        public static void Validate(CafeEncounterConversation[] rows,string selected,CafeSnapshot state)
        {
            rows=rows??new CafeEncounterConversation[0];
            if(rows.Length>10000 || rows.Any(r=>r==null) || rows.Select(r=>r.event_id).Distinct().Count()!=rows.Length || !string.IsNullOrEmpty(selected) && !rows.Any(r=>r.event_id==selected))throw new ArgumentException("派遣中の会話一覧が不正です。");
            foreach(var r in rows)
            {
                var ev=state.dispatches.SingleOrDefault(e=>e.id==r.event_id);var p=r.progress;
                if(!Supports(ev) || ev.encounter.status=="scheduled" || p==null || p.conversation_id!=ConversationId(ev) || (ev.encounter.status=="waiting"?p.branch_id!="introduction":p.branch_id!=ev.encounter.choice) || p.node<0 || p.node>Lines(p.branch_id,"",p.conversation_id).Length || p.page_offset<0 || p.node==Lines(p.branch_id,"",p.conversation_id).Length && p.page_offset!=0 || p.node<Lines(p.branch_id,"",p.conversation_id).Length && p.page_offset>Lines(p.branch_id,"",p.conversation_id)[p.node].text.Length+256 || p.viewed_branches==null || p.viewed_branches.Distinct().Count()!=p.viewed_branches.Length || p.viewed_branches.Any(b=>b!=p.branch_id || b=="introduction") || p.completed && (p.branch_id=="introduction" || p.node!=Lines(p.branch_id,"",p.conversation_id).Length || p.viewed_branches.Length!=1) || !p.completed && p.viewed_branches.Length!=0)throw new ArgumentException("派遣中の会話進行が不正です。");
            }
        }
    }
}
