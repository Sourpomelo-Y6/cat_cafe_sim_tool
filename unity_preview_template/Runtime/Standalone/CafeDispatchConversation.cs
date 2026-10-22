using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeDispatchConversation
    {
        public string event_id,cat_id,destination,stage="departure";
        public string Destination=>string.IsNullOrEmpty(destination)?CafeDispatchDialogue.Destination:destination;
        public string DispatchId=>"dispatch-"+day+"-"+cat_id;
        public int day;
        public CafeConversationProgress departure,travelling,returning;
        public CafeConversationProgress Current=>stage=="departure"?departure:stage=="travelling"?travelling:returning;
    }
    public static class CafeDispatchDialogue
    {
        public const string Destination="neighborhood_visit",Id="dispatch.neighborhood_visit.v1";
        public static readonly string[] Destinations={Destination,"shopping_street_event","out_of_town_visit","cat_exercise_class","quiet_reading_salon","cat_photo_studio","cat_product_trial"};
        public static bool Supports(string destination)=>Destinations.Contains(destination);
        public static string Title(string destination)=>destination=="shopping_street_event"?"商店街の交流会":destination=="out_of_town_visit"?"郊外への出張訪問":destination=="cat_exercise_class"?"猫の運動教室":destination=="quiet_reading_salon"?"静かな読書サロン":destination=="cat_photo_studio"?"猫の撮影スタジオ":destination=="cat_product_trial"?"猫用品の体験会":"近所のお店への訪問";
        public static string ConversationId(string destination)=>"dispatch."+destination+".v1";
        public static string Key(int day,string cat,string destination)=>"dispatch-"+day+"-"+cat+(destination==Destination?"":":"+destination);
        public static CafeConversationProgress Start(string stage,string destination=Destination)=>new CafeConversationProgress{conversation_id=ConversationId(destination)+"."+stage,branch_id=stage=="travelling"?"travelling":"introduction",viewed_branches=new string[0]};
        public static CafeConversationLine[] Lines(string stage,string branch,string name,string destination=Destination)
        {
            if(destination!=Destination)
            {
                if(destination=="cat_exercise_class" || destination=="quiet_reading_salon" || destination=="cat_photo_studio" || destination=="cat_product_trial")
                {
                    bool exercise=destination=="cat_exercise_class",studio=destination=="cat_photo_studio",product=destination=="cat_product_trial";
                    string place=Title(destination),contact=exercise?"教室の担当者":studio?"スタジオ担当者":product?"体験会の担当者":"サロンの担当者",choice=exercise?"追加の運動プログラム":studio?"追加撮影":product?"追加体験・撮影モデル":"延長の触れ合い";
                    var result=Lines(stage,branch,name);
                    if(stage=="departure" && branch=="introduction")
                    {
                        result[1]=new CafeConversationLine(contact,place+"への派遣をお願いできますか。猫の体調と、お店を離れる日程を確かめてから決めてください。");
                        result[3]=new CafeConversationLine("","出発条件・費用・日数と報酬見込みを確かめよう。途中の"+choice+"は、別の相談で回答する。");
                    }
                    if(stage=="travelling")result[1]=new CafeConversationLine("あなた",choice+"の相談は、イベント一覧の『派遣中の選択』で確認しましょう。留守の間も、店内の猫たちと営業の準備を進められます。");
                    if(stage=="return" && branch=="introduction")result[2]=new CafeConversationLine("あなた","途中の"+choice+"への回答を反映した確定報酬と、この子の帰還時の状態を確認しますね。ここで回答をやり直す必要はありません。");
                    if(stage=="return" && branch=="received")result[2]=new CafeConversationLine("","確定した報酬と派遣経験は受け取り済みだ。用品の有無は受取内容に従う。出勤には自動で戻らないので、営業準備で予定を確認しよう。");
                    if(studio && stage=="departure" && branch=="introduction")result[3]=new CafeConversationLine("","出発条件・費用・日数と報酬見込みを確かめよう。追加撮影と帰還後の猫紹介は、別の相談で回答する。");
                    if(studio && stage=="return" && branch=="introduction")result[2]=new CafeConversationLine("あなた","追加撮影への回答を反映した確定報酬を確認します。猫紹介は受領と結果会話の後で、別の相談として伺います。");
                    if(studio && stage=="return" && branch=="received")result[2]=new CafeConversationLine("","確定報酬と派遣経験は受け取り済みだ。結果を読み終えたら、イベント一覧の『派遣先からの猫紹介』で相談を確認しよう。");
                    if(product && stage=="return" && branch=="introduction")result[2]=new CafeConversationLine("あなた","途中の回答を反映した金銭と、既存の用品報酬を確認します。追加体験や撮影モデルへの回答を、ここでやり直す必要はありません。");
                    return result.Select(l=>new CafeConversationLine(l.speaker=="お店の人"?contact:l.speaker,l.text.Replace("近所のお店への訪問",place).Replace("近所のお店",place))).ToArray();
                }
                string title=Title(destination),sender=destination=="shopping_street_event"?"商店街の担当者":"訪問先の担当者";
                var lines=Lines(stage,branch,name);
                bool street=destination=="shopping_street_event";
                if(stage=="departure" && branch=="introduction")
                {
                    lines[1]=new CafeConversationLine(sender,street?"商店街の交流会に来てもらえますか。猫の体調と、留守の間のお店の予定を確認してから決めてください。":"郊外への出張訪問をお願いできますか。猫の体調と、お店を離れる日程を確かめてください。");
                    lines[3]=new CafeConversationLine("",street?"出発条件・費用・日数と報酬見込みを確かめよう。途中の追加仕事と帰還後の猫紹介は、別の相談で回答する。":"出発条件・費用・日数と報酬見込みを確かめよう。途中の休憩は、別の相談で回答する。");
                }
                if(stage=="travelling")lines[1]=new CafeConversationLine("あなた",street?"追加仕事の相談は、イベント一覧の『派遣中の選択』で確認しましょう。留守の間も、店内の猫たちと営業の準備を進められます。":"途中の休憩の相談は、イベント一覧の『派遣中の選択』で確認しましょう。留守の間も、店内の猫たちと営業の準備を進められます。");
                if(stage=="return" && branch=="received")lines[2]=new CafeConversationLine("","確定した報酬と派遣経験は受け取り済みだ。用品の有無は受取内容に従い、ここでは再受領しない。"+(street?"帰還結果を読み終えたら、猫の紹介はイベント一覧の『派遣先からの猫紹介』で確認しよう。":"出勤には自動で戻らないので、営業準備で予定を確認しよう。"));
                if(stage=="return" && branch=="introduction")lines[2]=new CafeConversationLine("あなた","途中の選択を反映した確定報酬と、この子の帰還時の状態を確認しますね。"+(street?"猫の紹介は、受取と帰還結果の確認を終えてから別の相談として伺います。":"途中の休憩も、回答済みの内容で確認しましょう。"));
                return lines.Select(l=>new CafeConversationLine(l.speaker=="お店の人"?sender:l.speaker,l.text.Replace("近所のお店への訪問",title).Replace("近所のお店",title=="商店街の交流会"?"商店街":"郊外の訪問先").Replace("報酬とケア用品、派遣の経験は受け取り済みだ。","今回の報酬と対象の用品、派遣の経験は受け取り済みだ。"))).ToArray();
            }
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
            if(row.departure?.branch_id=="introduction"){row.departure.branch_id="departed";row.departure.node=row.departure.page_offset=0;row.departure.completed=false;}
            if(ev.status=="resolved" && row.returning?.branch_id=="introduction"){row.returning.branch_id="received";row.returning.node=row.returning.page_offset=0;}
            if(ev.status=="waiting" && row.departure?.completed!=false){row.returning=row.returning??Start("return",row.Destination);row.stage="return";}
        }
        public static bool ResultPending(CafeDispatchConversation row)=>new[]{row.departure,row.returning}.Any(p=>p!=null && !p.completed && p.branch_id!="introduction");
        public static void Finish(CafeDispatchConversation row)
        {
            var p=row.Current;if(p==null || p.branch_id=="introduction" || p.node<Lines(row.stage,p.branch_id,"",row.Destination).Length)return;
            p.completed=true;p.page_offset=0;p.viewed_branches=(p.viewed_branches??new string[0]).Concat(new[]{p.branch_id}).Distinct().ToArray();
        }
        public static void Validate(CafeDispatchConversation[] rows,string selected,CafeSnapshot state)
        {
            rows=rows??new CafeDispatchConversation[0];
            if(rows.Length>10000 || rows.Any(r=>r==null) || rows.Select(r=>r.event_id).Distinct().Count()!=rows.Length || !string.IsNullOrEmpty(selected) && !rows.Any(r=>r.event_id==selected))throw new ArgumentException("派遣会話の一覧が不正です。");
            foreach(var r in rows)
            {
                var ev=state.dispatches.SingleOrDefault(e=>e.id==r.DispatchId && e.destination==r.Destination);
                if(!state.cats.Any(c=>c.id==r.cat_id) || r.day<1 || r.day>state.day || !Supports(r.Destination) || r.event_id!=Key(r.day,r.cat_id,r.Destination) || r.stage!="departure" && r.stage!="travelling" && r.stage!="return" || r.Current==null || ev==null && (r.travelling!=null || r.returning!=null) || r.departure==null && ev==null)throw new ArgumentException("派遣会話の対象が不正です。");
                ValidatePhase(r.departure,"departure",ev,r.Destination);ValidatePhase(r.travelling,"travelling",ev,r.Destination);ValidatePhase(r.returning,"return",ev,r.Destination);
            }
        }
        static void ValidatePhase(CafeConversationProgress p,string stage,CafeDispatch ev,string destination)
        {
            if(p==null)return;
            string[] allowed=stage=="departure"?new[]{"introduction","departed","cancelled"}:stage=="return"?new[]{"introduction","received"}:new[]{"travelling"};
            string[] completed=stage=="departure"?new[]{"departed","cancelled"}:stage=="return"?new[]{"received"}:new[]{"travelling"};
            bool invalid=p.conversation_id!=ConversationId(destination)+"."+stage || !allowed.Contains(p.branch_id) || p.node<0 || p.node>Lines(stage,p.branch_id,"").Length || p.viewed_branches==null;
            var lines=Lines(stage,p.branch_id,"",destination);
            invalid|=p.page_offset<0 || p.node==lines.Length && p.page_offset!=0 || p.node>=0 && p.node<lines.Length && p.page_offset>lines[p.node].text.Length+256;
            if(!invalid)invalid=p.viewed_branches.Distinct().Count()!=p.viewed_branches.Length || p.viewed_branches.Any(b=>!completed.Contains(b)) || p.completed && (p.branch_id=="introduction" || p.node!=Lines(stage,p.branch_id,"").Length || !p.viewed_branches.Contains(p.branch_id)) || !p.completed && p.viewed_branches.Any(b=>b!="cancelled");
            if(stage=="departure")invalid|=ev==null?p.branch_id=="departed" || (p.viewed_branches??new string[0]).Contains("departed"):p.branch_id!="departed";
            if(stage=="return")invalid|=ev==null || ev.status=="travelling" || (ev.status=="resolved"?p.branch_id!="received":p.branch_id!="introduction");
            if(stage=="travelling")invalid|=ev==null;
            if(invalid)throw new ArgumentException("派遣会話の保存進行が不正です。");
        }
    }
}
