using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class EncounterConversationPanel : AcquisitionConversationPanel
    {
        GameObject host;PythonDispatchChoicePanel owner;
        CafeEncounterConversation Row=>Backend.EncounterConversation;
        PythonDispatchEncounter Event=>state?.dispatch_choices?.FirstOrDefault(e=>e.event_id==Row?.event_id);
        PythonDispatchEncounterOption Option(bool first)=>Event?.choices?.FirstOrDefault(o=>o.choice==(first?CafeEncounterDialogue.FirstChoice(progress.conversation_id):CafeEncounterDialogue.SecondChoice(progress.conversation_id)));
        public GameObject Panel=>host;
        public void Apply(PythonCafeState value,PythonDispatchChoicePanel original)
        {
            owner=original;if(host==null){host=new GameObject("Extra Work Conversation",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));host.transform.SetParent(owner.panel.transform.parent,false);host.SetActive(false);}
            Bind(value,host,owner.title.font);if(Showing)owner.HideForConversation();
        }
        public bool OpenEvent(string id){if(!Backend.BeginEncounterConversation(id))return false;Apply(Client.CurrentState,GetComponent<PythonDispatchChoicePanel>());Open();return true;}
        protected override CafeConversationProgress ResolveProgress(){Backend.PrepareEncounterConversation();return Row?.progress;}
        protected override void SetOpen(bool open){Backend.EncounterConversationOpen=open;}
        protected override CafeConversationLine[] Lines(string branch)
        {
            var lines=CafeEncounterDialogue.Lines(branch,Event.label,progress.conversation_id);
            if(CafeEncounterDialogue.IsProduct(progress.conversation_id) && branch!="introduction" && Event.changes!=null)
            {
                var c=Event.changes;
                lines[lines.Length-1].text+="\n確定：疲労 "+c.fatigue_before+" → "+c.fatigue_after+" / ストレス "+c.stress_before+" → "+c.stress_after+"。帰還金銭報酬 "+Event.return_reward+"。";
            }
            return lines;
        }
        protected override PythonIntakeState Request=>new PythonIntakeState{cat_id=Backend.Details.ContentId(Event.cat_id),name=Event.label,personality=Title+" · 派遣中",can_decline=true};
        protected override bool CanAnswer(bool yes)=>Option(yes)?.can_select==true && Client.CanRespondDispatchChoice(Row.event_id,yes?CafeEncounterDialogue.FirstChoice(progress.conversation_id):CafeEncounterDialogue.SecondChoice(progress.conversation_id));
        protected override bool ThreeLinePages=>CafeEncounterDialogue.IsProduct(progress?.conversation_id) || progress?.conversation_id==CafeEncounterDialogue.ExerciseId || progress?.conversation_id==CafeEncounterDialogue.ReadingId;
        protected override string Title=>CafeEncounterDialogue.Title(progress.conversation_id);
        protected override string SceneLabel=>state.day+"日目 · "+(progress.branch_id=="introduction"?"派遣中の相談":"回答済み · "+Event.selected_label);
        protected override string ChoicePrompt=>"猫の状態と両方の効果を確認して選んでください。";
        protected override string AcceptCaption=>Option(true)?.label;
        protected override string DeclineCaption=>Option(false)?.label;
        protected override string AcceptEffect=>Effects(true);
        protected override string DeclineEffect=>Effects(false);
        string Effects(bool first)
        {
            var o=Option(first);var dispatch=Backend.Simulation.State.dispatches.First(e=>e.id==Row.event_id);
            var item=Backend.Simulation.DispatchDestination(dispatch.destination).item_reward;
            string available=o.can_select?"選択可能":"選べない理由："+o.reason;
            string reward="\n帰還金銭報酬："+Event.return_reward+" → "+o.return_reward+"（増減 "+o.reward_delta+"）";
            string burden="\n疲労："+o.changes.fatigue_before+" → "+o.changes.fatigue_after+" / ストレス："+o.changes.stress_before+" → "+o.changes.stress_after;
            if(CafeEncounterDialogue.IsProduct(progress.conversation_id))return available+" / 既存用品："+(item==null?"なし":item.name)+"（両択とも帰還時）"+reward+burden+"\n負担は回答時。即時入出金・日程変更・追加用品なし。";
            return available+"\n帰還報酬："+Event.return_reward+" → "+o.return_reward+"（増減 "+o.reward_delta+"）"+burden+"\n疲労・ストレスは回答時、報酬は帰還時に反映。所持金の即時増減なし。日程の変更なし。";
        }
        protected override string HistoryPrefix=>progress.branch_id=="introduction"?"":string.Join("\n\n",Lines("introduction").Select(l=>(string.IsNullOrEmpty(l.speaker)?"地の文":l.speaker)+"\n"+l.text))+"\n\n【"+Event.selected_label+"】\n\n";
        protected override void SendAnswer(bool yes){Client.RespondDispatchChoice(Row.event_id,yes?CafeEncounterDialogue.FirstChoice(progress.conversation_id):CafeEncounterDialogue.SecondChoice(progress.conversation_id));}
        protected override void Finish(){CafeEncounterDialogue.Finish(progress);}
        public override void Open(){base.Open();if(Showing)owner.HideForConversation();}
    }
}
