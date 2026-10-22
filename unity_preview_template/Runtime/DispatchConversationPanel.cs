using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    // The shared ADV view owns text/input/layout; this adapter only supplies a dispatch scenario.
    public sealed class DispatchConversationPanel : AcquisitionConversationPanel
    {
        GameObject host;PythonGeneralDispatchPanel owner;
        CafeDispatchConversation Row=>Backend.DispatchConversation;
        CafeDispatch Event=>Backend.Simulation.State.dispatches.FirstOrDefault(e=>e.id==Row?.DispatchId && e.destination==Row.Destination);
        PythonDispatchOption Option=>state?.general_dispatch?.options?.FirstOrDefault(o=>o.cat_id==Row?.cat_id && o.choice==Row?.Destination);
        PythonDispatchEvent Return=>state?.general_dispatch?.events?.FirstOrDefault(e=>e.choice==Row?.DispatchId);
        public GameObject Panel=>host;
        public void Apply(PythonCafeState value,PythonGeneralDispatchPanel original)
        {
            owner=original;
            if(host==null){host=new GameObject("Nearby Shop Conversation",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));host.transform.SetParent(owner.panel.transform.parent,false);host.SetActive(false);}
            Bind(value,host,owner.title.font);if(Showing || Row?.stage=="return" && GetComponents<AcquisitionConversationPanel>().Any(other=>other!=this && other.Showing))owner.HideForConversation();
        }
        public void Departure(string cat,string destination=CafeDispatchDialogue.Destination){Backend.BeginDispatchDeparture(cat,destination);Apply(Client.CurrentState,GetComponent<PythonGeneralDispatchPanel>());Open();}
        public bool OpenEvent(string id){if(!Backend.BeginDispatchEvent(id))return false;Apply(Client.CurrentState,GetComponent<PythonGeneralDispatchPanel>());Open();return true;}
        protected override CafeConversationProgress ResolveProgress(){Backend.PrepareDispatchConversation();return Row?.Current;}
        protected override bool Available=>base.Available && Row!=null && (Event!=null || Row.day==state.day);
        protected override void SetOpen(bool open){Backend.DispatchConversationOpen=open;}
        protected override CafeConversationLine[] Lines(string branch)=>CafeDispatchDialogue.Lines(Row.stage,branch,Backend.Simulation.Definition(Row.cat_id).name,Row.Destination);
        protected override PythonIntakeState Request=>new PythonIntakeState{cat_id=Backend.Details.ContentId(Row.cat_id),name=Backend.Simulation.Definition(Row.cat_id).name,personality=Row.stage=="departure"?CafeDispatchDialogue.Title(Row.Destination):Row.stage=="travelling"?"派遣中 · あと "+(Event?.remaining??0)+"回の閉店":"帰還 · "+(Event?.status=="resolved"?"受取済み":"確認待ち"),can_decline=true};
        protected override bool CanAnswer(bool yes)=>!yes || (Row.stage=="departure"?Row.day==state.day && Option?.can_select==true:Row.stage=="return" && Return?.can_select==true);
        protected override bool ThreeLinePages=>true;
        protected override string Title=>CafeDispatchDialogue.Title(Row.Destination);
        protected override string SceneLabel=>state.day+"日目 · "+(Row.stage=="departure"?(progress.branch_id=="departed"?"出発済み":progress.branch_id=="cancelled"?"今回は出発しない":"出発前"):Row.stage=="travelling"?"派遣中":Event?.status=="resolved"?"報酬受取済み":"帰還の確認");
        protected override string ChoicePrompt=>Row.stage=="departure"?"この猫に、"+Title+"をお願いしましょうか。":"帰還の報酬を受け取りますか。";
        protected override string AcceptCaption=>Row.stage=="departure"?"この猫で出発する":"報酬を受け取る";
        protected override string DeclineCaption=>Row.stage=="departure"?"今回は出発しない":"あとで確認する";
        protected override string AcceptEffect=>Row.stage=="departure"?(Option?.can_select==true?"出発可能":"選べない理由："+(Option?.reason??"派遣先の情報がありません。"))+"\n"+("対象："+Request.name+" / 資金："+state.funds+"\n出発費用：0 / 帰還まで："+Option?.days+"回の閉店 / 報酬見込み："+Option?.reward)+"\n"+Option?.detail:(Return?.can_select==true?"受取可能":"選べない理由："+(Return?.reason??"帰還を確認してください。"))+"\n確定報酬："+Return?.reward+" / "+Supply+" / 派遣経験1\n"+Return?.detail;
        string Supply {get{var item=Backend.Simulation.DispatchDestination(Row.Destination).item_reward;return item==null || string.IsNullOrEmpty(item.id)?"用品なし":"用品："+item.name;}}
        protected override string DeclineEffect=>Row.stage=="departure"?"出発・費用・日程の変更なし。条件が整えば再び相談できます。":"未受取のまま保持します。帰還確認が終わるまで既存の進行制限が残ります。";
        protected override string Notice=>Row.stage=="travelling"?"状況の閲覧中です。閉じた後、営業・休業で日程を進めてください。":progress.branch_id=="introduction"?"営業・自動進行は停止中。閉じても相談と会話の位置を保持します。":"手続きは確定済みです。結果会話を最後まで読み、営業・自動進行は手動で再開してください。";
        protected override string HistoryPrefix=>progress.branch_id=="introduction" || Row.stage=="travelling"?"":string.Join("\n\n",Lines("introduction").Select(l=>(string.IsNullOrEmpty(l.speaker)?"地の文":l.speaker)+"\n"+l.text))+"\n\n【"+(progress.branch_id=="departed"?"出発":progress.branch_id=="received"?"受取":"出発しない")+"】\n\n";
        protected override void SendAnswer(bool yes)
        {
            if(!yes){if(Row.stage=="return"){Close();return;}progress.branch_id="cancelled";progress.node=progress.page_offset=0;return;}
            if(Row.stage=="departure")Client.DispatchGeneral(Row.cat_id,Row.Destination);else Client.ReceiveGeneral(Row.DispatchId);
        }
        protected override void Finish(){CafeDispatchDialogue.Finish(Row);}
        protected override void AfterClose()
        {
            base.AfterClose();
            if(!Backend.DispatchResultPending)
            {
                GetComponent<PythonGrowthPanel>()?.Apply(Client.CurrentState);
                GetComponent<PythonGoalPanel>()?.Apply(Client.CurrentState);
                GetComponent<PythonDispatchIntroductionPanel>()?.Apply(Client.CurrentState);
            }
        }
        public override void Open(){base.Open();if(Showing)owner.HideForConversation();}
    }
}
