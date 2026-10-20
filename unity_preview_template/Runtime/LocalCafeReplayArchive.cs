using System;
using System.IO;
using System.Linq;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        public CafeReplayStore Replays {get;private set;}
        public string ReplayImportNotice {get;private set;}
        CafeSaveEnvelope ViewingSnapshot()=>new CafeSaveEnvelope{state=Simulation.State,outcome=Outcome,opening=Opening,popularity_ending=PopularityEnding,bond_ending=BondEnding,patron_ending=PatronEnding,conversation=Conversation,regular_conversation=RegularConversation,dispatch_conversations=DispatchConversations,dispatch_conversation_selected=DispatchConversationSelected,lodge_conversations=LodgeConversations,lodge_conversation_selected=LodgeConversationSelected,encounter_conversations=EncounterConversations,encounter_conversation_selected=EncounterConversationSelected,dispatch_introduction_conversations=DispatchIntroductionConversations,dispatch_introduction_conversation_selected=DispatchIntroductionConversationSelected};
        public void RememberViewedConversations()
        {
            try{if(!Replays.Merge(CafeReplayCatalog.FromSave(ViewingSnapshot(),Simulation.Rules,id=>Simulation.Definition(id).name,Details.CustomerName,Details.ContentId)))ReplayImportNotice=Replays.Error;}
            catch(Exception){ReplayImportNotice="一部の会話記録を取り込めませんでした。営業データは変更していません。";}
        }
        public void ImportViewedSaves()
        {
            RememberViewedConversations();int skipped=0;string issue=Replays.Error;
            foreach(int slot in Enumerable.Range(1,10))foreach(string suffix in new[]{"",".bak"})
            {
                string file=Path.Combine(saveDirectory,"slot-"+slot+".json"+suffix);if(!File.Exists(file))continue;
                try
                {
                    var save=DecodeSave(File.ReadAllText(file));if(save==null || save.version!=1 || save.slot!=slot || !DateTimeOffset.TryParse(save.created_at,out _))throw new ArgumentException();
                    var probe=new CafeSimulation(Simulation.Rules);probe.Restore(save.state);
                    var catalog=JsonUtility.FromJson<CafePresentationCatalog>(Resources.Load<TextAsset>("CatCafeStandalone/presentation").text);var detail=new CafeDetailPresentation(probe,catalog);
                    if(!Replays.Merge(CafeReplayCatalog.FromSave(save,Simulation.Rules,id=>probe.Definition(id).name,detail.CustomerName,detail.ContentId)))issue=Replays.Error;
                }
                catch(Exception){skipped++;}
            }
            ReplayImportNotice=issue??(skipped==0?"営業セーブとバックアップの閲覧完了記録を確認しました。":skipped+"件の互換性がない、または不正な保存記録をスキップしました。");
        }
    }
}
