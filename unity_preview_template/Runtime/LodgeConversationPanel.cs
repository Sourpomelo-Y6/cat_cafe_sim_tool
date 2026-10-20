using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class LodgeConversationPanel : AcquisitionConversationPanel
    {
        GameObject host;PythonDispatchTroublePanel owner;
        CafeLodgeConversation Row=>Backend.LodgeConversation;
        CafeDispatch Event=>Backend.Simulation.State.dispatches.FirstOrDefault(e=>e.id==Row?.event_id);
        bool Returning=>ReferenceEquals(progress,Row?.returning);
        PythonDispatchTrouble Trouble=>state?.dispatch_troubles?.FirstOrDefault(e=>e.event_id==Row?.event_id);
        PythonDispatchTroubleOption Option(bool yes)=>Trouble?.choices?.FirstOrDefault(o=>o.choice==(yes?"search":"wait"));
        public GameObject Panel=>host;
        public void Apply(PythonCafeState value,PythonDispatchTroublePanel original)
        {
            owner=original;if(host==null){host=new GameObject("Lodge Trouble Conversation",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));host.transform.SetParent(owner.panel.transform.parent,false);host.SetActive(false);}
            Bind(value,host,owner.title.font);
            if(Showing || Row?.Current?.completed==false && GetComponents<AcquisitionConversationPanel>().Any(other=>other!=this && other.Showing)){owner.HideForConversation();GetComponent<PythonGeneralDispatchPanel>().HideForConversation();}
        }
        public bool OpenEvent(string id){if(!Backend.BeginLodgeConversation(id))return false;Apply(Client.CurrentState,GetComponent<PythonDispatchTroublePanel>());Open();return true;}
        protected override CafeConversationProgress ResolveProgress(){Backend.PrepareLodgeConversation();return Row?.Current;}
        protected override bool ThreeLinePages=>true;
        protected override void SetOpen(bool open){Backend.LodgeConversationOpen=open;}
        protected override CafeConversationLine[] Lines(string branch)=>CafeLodgeDialogue.Lines(Returning,branch,Backend.Simulation.Definition(Event.cat_id).name);
        protected override PythonIntakeState Request=>new PythonIntakeState{cat_id=Backend.Details.ContentId(Event.cat_id),name=Backend.Simulation.Definition(Event.cat_id).name,personality="宿への派遣・家出 "+Event.trouble.missing_day+"日目",can_decline=true};
        protected override bool CanAnswer(bool yes)=>Returning?(!yes || Event.status=="waiting" && Backend.Simulation.DispatchReceiveReason(Event.id)==""):Option(yes)?.can_select==true && Client.CanRespondDispatchTrouble(Event.id,yes?"search":"wait");
        protected override string Title=>Returning?"山あいの宿 · 帰還確認":"山あいの宿 · 家出トラブル";
        protected override string SceneLabel=>state.day+"日目 · "+(Returning?(Event.status=="resolved"?"帰還確認済み":"帰還確認待ち"):Event.trouble.status=="waiting"?"対応の回答待ち":"回答済み · "+(Event.trouble.choice=="search"?"捜索":"待機"))+" / 残り "+Event.remaining+"回の閉店";
        protected override string ChoicePrompt=>Returning?"帰還を確認しますか。体調の反映は確認時の既存処理で行います。":"捜索を依頼するか、帰還を待つか選んでください。今の資金と待機日程を確認しましょう。";
        protected override string AcceptCaption=>Returning?"帰還を確認する":"費用を払って捜索";
        protected override string DeclineCaption=>Returning?"あとで確認する":"費用なしで帰還を待つ";
        protected override string AcceptEffect=>Returning?(CanAnswer(true)?"確認可能":"選べない理由："+Backend.Simulation.DispatchReceiveReason(Event.id))+"\n資金："+state.funds+" / 追加費用：0 / 報酬・派遣経験：0\n帰還確認で体力を最大へ・ストレスを "+Backend.Simulation.Rules.dispatch_trouble.return_stress+"へ設定。健康状態から接客可否を判定。":Effects(true);
        protected override string DeclineEffect=>Returning?"未確認のまま保持。帰還確認はイベント一覧から再開できます。":Effects(false);
        string Effects(bool yes)
        {
            var o=Option(yes);if(o==null)return "選択肢を確認してください。";
            return (o.can_select?"選択可能":"選べない理由："+o.reason)+"\n費用："+o.cost+" / 所持金："+state.funds+" → "+o.funds_after+"\n"+(yes?"回答後は帰還確認待ち。別途帰還確認が必要。":"家出当日を除く "+o.remaining+"回の閉店後に帰還確認待ち。")+"\n中断した派遣の報酬・派遣経験：0";
        }
        protected override string HistoryPrefix=>progress.branch_id=="introduction"?"":string.Join("\n\n",Lines("introduction").Select(l=>(string.IsNullOrEmpty(l.speaker)?"地の文":l.speaker)+"\n"+l.text))+"\n\n";
        protected override string Notice=>progress.branch_id=="introduction"?"閉じた会話はイベント一覧から再開できます。":"手続きは確定済み。結果会話を終えてから進行してください。";
        protected override string EndText=>Returning?"帰還確認は完了しました。会話を終え、体調と出勤予定を確認しましょう。":Event.status=="missing"?"会話を終えて閉じ、営業・休業で残り "+Event.remaining+"回の閉店を進めてください。帰還確認はイベント一覧から。":"会話を終えて閉じ、イベント一覧から帰還確認へ進んでください。";
        protected override void SendAnswer(bool yes){if(Returning){if(!yes){Close();return;}Client.ReceiveGeneral(Event.id);}else Client.RespondDispatchTrouble(Event.id,yes?"search":"wait");}
        protected override void Finish(){CafeLodgeDialogue.Finish(Row);}
        public override void Open(){base.Open();if(Showing){owner.HideForConversation();GetComponent<PythonGeneralDispatchPanel>().HideForConversation();}}
    }
}
