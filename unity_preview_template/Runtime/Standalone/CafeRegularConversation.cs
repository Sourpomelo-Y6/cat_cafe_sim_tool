namespace CatCafePreview.Standalone
{
    // Fixed scenario revision: keep published node order stable for saves and Extra.
    public static class CafeRegularConversation
    {
        public const string Id="regular-introduction.regular-rescue-1.v1";
        public static CafeConversationProgress Start()=>new CafeConversationProgress{conversation_id=Id,branch_id="introduction",viewed_branches=new string[0]};
        public static CafeConversationLine[] Lines(string branch,string name,string customer="常連のお客さん")
        {
            if(string.IsNullOrEmpty(customer))customer="常連のお客さん";
            if(branch=="accepted")return new[]{
                new CafeConversationLine("あなた","この子を迎えます。まずは落ち着いて休める場所を用意しますね。"),
                new CafeConversationLine(customer,"ありがとうございます。ここでの暮らしに、ゆっくり慣れてくれるといいですね。"),
                new CafeConversationLine("",name+"は近くで足を止め、ゆっくりと目を細めた。こちらも腰を下ろし、同じ目線で見守る。"),
                new CafeConversationLine("","受け入れの手続きが終わった。今日は休養の予定だ。出勤は営業準備で決めよう。")};
            if(branch=="declined")return new[]{
                new CafeConversationLine("あなた","紹介してくださって、ありがとうございます。今回は、受け入れを見送ります。"),
                new CafeConversationLine(customer,"わかりました。この子のことを考えてくださって、ありがとうございます。"),
                new CafeConversationLine("",name+"はのんびりと前足を伸ばした。その穏やかな仕草を見守りながら、紹介へのお礼を伝えた。"),
                new CafeConversationLine("","今回の紹介は見送った。費用・人気・常連度は変わらない。この紹介が再び届くことはない。")};
            return new[]{
                new CafeConversationLine("","営業準備の時間。お店に通ってくれる"+customer+"から、猫の紹介が届いた。"),
                new CafeConversationLine(customer,"猫を迎えることについて、ご相談したくて。この子がここで過ごせるか、考えていただけますか。"),
                new CafeConversationLine(customer,"名前は"+name+"。穏やかな甘えん坊で、のんびりした子です。まずは、この子のペースで見てあげてください。"),
                new CafeConversationLine("",name+"がゆっくり顔を上げた。茶トラの短い毛がやわらかく動き、そばにいる人へ静かに近づいてくる。"),
                new CafeConversationLine("あなた","紹介してくださって、ありがとうございます。過ごす場所や費用を確認してから、お返事しますね。")};
        }
        public static void Validate(CafeConversationProgress p,string status)=>CafeIntakeConversation.ValidateProgress(p,status,Id,"untriggered",(branch,name)=>Lines(branch,name));
        public static void SyncAnswer(CafeConversationProgress p,string status)=>CafeIntakeConversation.SyncAnswer(p,status);
        public static void Finish(CafeConversationProgress p)
        {
            if(p.branch_id=="introduction" || p.node<Lines(p.branch_id,"").Length)return;
            p.completed=true;p.viewed_branches=new[]{p.branch_id};
        }
    }
}
