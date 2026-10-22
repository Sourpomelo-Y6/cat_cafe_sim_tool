using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeReplayEntry
    {
        public string conversation_id,branch_id,title,cat_id,cat_name,personality;
        public CafeConversationLine[] lines;
        public string Key=>conversation_id+"/"+branch_id;
    }
    [Serializable] public sealed class CafeReplayProfile {public int version=1;public CafeReplayEntry[] entries=new CafeReplayEntry[0];}
    public sealed class CafeReplayDefinition
    {
        public string id,branch,title;
        public string Key=>id+"/"+branch;
        public CafeReplayDefinition(string id,string branch,string title){this.id=id;this.branch=branch;this.title=title;}
    }
    public static class CafeReplayCatalog
    {
        public static bool IsEnding(string id)=>id==CafeOutcome.Id || id==CafePopularityEnding.Id || id==CafeBondEnding.Id || id==CafePatronEnding.Id;
        public static readonly CafeReplayDefinition[] All={
            new CafeReplayDefinition("dispatch.shopping_street_event.v1.departure","departed","商店街の交流会 · 出発"),
            new CafeReplayDefinition("dispatch.shopping_street_event.v1.departure","cancelled","商店街の交流会 · 今回は出発しない"),
            new CafeReplayDefinition("dispatch.shopping_street_event.v1.travelling","travelling","商店街の交流会 · 派遣中"),
            new CafeReplayDefinition("dispatch.shopping_street_event.v1.return","received","商店街の交流会 · 帰還"),
            new CafeReplayDefinition("dispatch.out_of_town_visit.v1.departure","departed","郊外への出張訪問 · 出発"),
            new CafeReplayDefinition("dispatch.out_of_town_visit.v1.departure","cancelled","郊外への出張訪問 · 今回は出発しない"),
            new CafeReplayDefinition("dispatch.out_of_town_visit.v1.travelling","travelling","郊外への出張訪問 · 派遣中"),
            new CafeReplayDefinition("dispatch.out_of_town_visit.v1.return","received","郊外への出張訪問 · 帰還"),
            new CafeReplayDefinition(CafeCocoDialogue.MeetingId,"meeting","ココ · 初対面"),new CafeReplayDefinition(CafeCocoDialogue.AdmissionId,"accepted","ココ · 迎える"),new CafeReplayDefinition(CafeCocoDialogue.AdmissionId,"later","ココ · 今回は迎えない"),
            new CafeReplayDefinition(CafeEquipmentDialogue.SupportId,"full","支援依頼 · 支援"),new CafeReplayDefinition(CafeEquipmentDialogue.SupportId,"small","支援依頼 · 少額支援"),new CafeReplayDefinition(CafeEquipmentDialogue.SupportId,"decline","支援依頼 · 見送り"),
            new CafeReplayDefinition(CafeEquipmentDialogue.Id,"repair","設備トラブル · 修理"),new CafeReplayDefinition(CafeEquipmentDialogue.Id,"patch","設備トラブル · 応急処置"),new CafeReplayDefinition(CafeEquipmentDialogue.Id,"close","設備トラブル · 休業"),
            new CafeReplayDefinition(CafeMissingDialogue.Id+".notice","departed","店からの家出 · 通知"),new CafeReplayDefinition(CafeMissingDialogue.Id+".waiting","waiting","店からの家出 · 待機"),new CafeReplayDefinition(CafeMissingDialogue.Id+".confirmation","waiting_return","店からの家出 · 帰還待ち"),new CafeReplayDefinition(CafeMissingDialogue.Id+".result","kitten","店からの家出 · 子猫ありの帰還"),new CafeReplayDefinition(CafeMissingDialogue.Id+".result","alone","店からの家出 · 子猫なしの帰還"),
            new CafeReplayDefinition(CafeReservationDialogue.Id+".response","accept","特別予約 · 引き受ける"),new CafeReplayDefinition(CafeReservationDialogue.Id+".response","decline","特別予約 · 辞退する"),new CafeReplayDefinition(CafeReservationDialogue.Id+".result","success","特別予約 · 条件達成"),new CafeReplayDefinition(CafeReservationDialogue.Id+".result","failure","特別予約 · 条件未達"),new CafeReplayDefinition(CafeReservationDialogue.Id+".result","unserved","特別予約 · 未接客"),
            new CafeReplayDefinition(CafeTrustDialogue.Id+".response","recover","顧客の信頼 · 回復を試みる"),new CafeReplayDefinition(CafeTrustDialogue.Id+".response","ignore","顧客の信頼 · 見送る"),new CafeReplayDefinition(CafeTrustDialogue.Id+".result","recovered","顧客の信頼 · 満足接客による回復"),new CafeReplayDefinition(CafeTrustDialogue.Id+".result","departed","顧客の信頼 · 不満接客による永久離脱"),
            new CafeReplayDefinition(CafeAdoptionDialogue.Id,"accept","猫の譲渡申し出 · 譲渡する"),new CafeReplayDefinition(CafeAdoptionDialogue.Id,"decline","猫の譲渡申し出 · 見送る"),
            new CafeReplayDefinition(CafeLodgeDialogue.Id+".response","search","山あいの宿の家出 · 捜索"),new CafeReplayDefinition(CafeLodgeDialogue.Id+".response","wait","山あいの宿の家出 · 待機"),new CafeReplayDefinition(CafeLodgeDialogue.Id+".return","received","山あいの宿の家出 · 帰還確認"),
            new CafeReplayDefinition(CafeOutcome.Id,"expired","未達の結末 · ひとつの区切り"),new CafeReplayDefinition(CafeOutcome.Id,"funds","経営終了 · 資金不足"),new CafeReplayDefinition(CafeOutcome.Id,"popularity","経営終了 · 人気低下"),
            new CafeReplayDefinition(CafeOpening.Id,"popularity","オープニング · 人気"),new CafeReplayDefinition(CafeOpening.Id,"patron","オープニング · 有力者"),new CafeReplayDefinition(CafeOpening.Id,"bond","オープニング · 好感度"),new CafeReplayDefinition(CafeOpening.Id,"free","オープニング · 自由営業"),
            new CafeReplayDefinition(CafePatronEnding.Id,CafePopularityEnding.Branch,"有力者目標 · 店の外へつながる信頼"),
            new CafeReplayDefinition(CafeBondEnding.Id,CafePopularityEnding.Branch,"好感度目標 · 猫たちとのつながり"),
            new CafeReplayDefinition(CafePopularityEnding.Id,CafePopularityEnding.Branch,"人気目標 · 猫カフェのこれから"),
            new CafeReplayDefinition(CafeIntakeConversation.Id,"accepted","保護猫の受け入れ依頼 · 迎える"),new CafeReplayDefinition(CafeIntakeConversation.Id,"declined","保護猫の受け入れ依頼 · 見送る"),
            new CafeReplayDefinition(CafeRegularConversation.Id,"accepted","常連からの猫紹介 · 迎える"),new CafeReplayDefinition(CafeRegularConversation.Id,"declined","常連からの猫紹介 · 見送る"),
            new CafeReplayDefinition(CafeDispatchDialogue.Id+".departure","departed","近所のお店への訪問 · 出発する"),new CafeReplayDefinition(CafeDispatchDialogue.Id+".departure","cancelled","近所のお店への訪問 · 今回は出発しない"),
            new CafeReplayDefinition(CafeDispatchDialogue.Id+".travelling","travelling","近所のお店への訪問 · 派遣中"),new CafeReplayDefinition(CafeDispatchDialogue.Id+".return","received","近所のお店への訪問 · 帰還"),
            new CafeReplayDefinition(CafeEncounterDialogue.MuseumId,"accept","美術館の来館者との交流 · 引き受ける"),new CafeReplayDefinition(CafeEncounterDialogue.MuseumId,"decline","美術館の来館者との交流 · 見送る"),
            new CafeReplayDefinition(CafeEncounterDialogue.ProductId,"accept","追加の用品体験 · 参加する"),new CafeReplayDefinition(CafeEncounterDialogue.ProductId,"decline","追加の用品体験 · 予定どおり終える"),
            new CafeReplayDefinition(CafeEncounterDialogue.ModelId,"accept","用品の撮影モデル · 引き受ける"),new CafeReplayDefinition(CafeEncounterDialogue.ModelId,"decline","用品の撮影モデル · 見送る"),
            new CafeReplayDefinition(CafeEncounterDialogue.ExerciseId,"accept","追加の運動 · 参加する"),new CafeReplayDefinition(CafeEncounterDialogue.ExerciseId,"decline","追加の運動 · 予定どおり終える"),
            new CafeReplayDefinition(CafeEncounterDialogue.ReadingId,"accept","触れ合いの延長 · 参加する"),new CafeReplayDefinition(CafeEncounterDialogue.ReadingId,"decline","触れ合いの延長 · 予定どおり終える"),
            new CafeReplayDefinition(CafeEncounterDialogue.RestId,"rest","郊外での休憩 · 休憩する"),new CafeReplayDefinition(CafeEncounterDialogue.RestId,"continue","郊外での休憩 · 予定どおり進む"),
            new CafeReplayDefinition(CafeEncounterDialogue.PhotoId,"accept","追加撮影 · 続ける"),new CafeReplayDefinition(CafeEncounterDialogue.PhotoId,"decline","追加撮影 · 切り上げる"),
            new CafeReplayDefinition(CafeEncounterDialogue.Id,"accept","商店街の追加のお願い · 引き受ける"),new CafeReplayDefinition(CafeEncounterDialogue.Id,"decline","商店街の追加のお願い · 予定どおり終える"),
            new CafeReplayDefinition(CafeDispatchIntroductionDialogue.StudioId,"accepted","撮影スタジオからの猫紹介 · 迎える"),new CafeReplayDefinition(CafeDispatchIntroductionDialogue.StudioId,"declined","撮影スタジオからの猫紹介 · 見送る"),
            new CafeReplayDefinition(CafeDispatchIntroductionDialogue.Id,"accepted","商店街からの猫紹介 · 迎える"),new CafeReplayDefinition(CafeDispatchIntroductionDialogue.Id,"declined","商店街からの猫紹介 · 見送る")};
        public static void Validate(CafeReplayProfile profile)
        {
            if(profile==null || profile.version!=1 || profile.entries==null || profile.entries.Length>All.Length || profile.entries.Any(e=>e==null) || profile.entries.Select(e=>e.Key).Distinct().Count()!=profile.entries.Length)throw new ArgumentException("回想記録の形式が不正です。");
            foreach(var e in profile.entries)if(!All.Any(d=>d.Key==e.Key) || string.IsNullOrEmpty(e.title) || e.title.Length>256 || (e.cat_name??"").Length>256 || (e.cat_id??"").Length>256 || (e.personality??"").Length>1024 || e.lines==null || e.lines.Length==0 || e.lines.Length>100 || e.lines.Any(l=>l==null || l.text==null || l.text.Length>10000 || (l.speaker??"").Length>256))throw new ArgumentException("回想の本文が不正です。");
        }
        public static void ValidateSave(CafeSaveEnvelope s)
        {
            CafeMissingDialogue.Validate(s.state);CafeOutcome.Validate(s.outcome,s.state);CafePatronEnding.Validate(s.patron_ending,s.state);CafeBondEnding.Validate(s.bond_ending,s.state);CafeOpening.Validate(s.opening,s.state);CafePopularityEnding.Validate(s.popularity_ending,s.state);CafeIntakeConversation.Validate(s.conversation,s.state.intake_request.status);CafeRegularConversation.Validate(s.regular_conversation,s.state.regular_introduction.status);
            CafeCocoDialogue.Validate(s.coco_conversations,s.state);CafeEquipmentDialogue.Validate(s.equipment_conversations,s.equipment_conversation_selected,s.state);CafeReservationDialogue.Validate(s.reservation_conversations,s.reservation_conversation_selected,s.state);CafeTrustDialogue.Validate(s.trust_conversations,s.trust_conversation_selected,s.state);CafeAdoptionDialogue.Validate(s.adoption_conversations,s.adoption_conversation_selected,s.state);CafeLodgeDialogue.Validate(s.lodge_conversations,s.lodge_conversation_selected,s.state);CafeDispatchDialogue.Validate(s.dispatch_conversations,s.dispatch_conversation_selected,s.state);CafeEncounterDialogue.Validate(s.encounter_conversations,s.encounter_conversation_selected,s.state);CafeDispatchIntroductionDialogue.Validate(s.dispatch_introduction_conversations,s.dispatch_introduction_conversation_selected,s.state);
        }
        public static CafeReplayEntry[] FromSave(CafeSaveEnvelope save,CafeRules rules,Func<string,string> catName,Func<string,string> customerName,Func<string,string> contentId)
        {
            ValidateSave(save);var result=new List<CafeReplayEntry>();
            Action<CafeConversationProgress,string,string,string,Func<string,CafeConversationLine[]>,bool> add=(p,id,name,personality,script,intro)=>{
                if(p==null)return;
                foreach(string branch in p.viewed_branches??new string[0])
                {
                    var d=All.FirstOrDefault(x=>x.id==p.conversation_id && x.branch==branch);if(d==null)continue;
                    var lines=(intro?script("introduction"):new CafeConversationLine[0]).Concat(script(branch)).Select(l=>new CafeConversationLine(l.speaker,l.text)).ToArray();
                    result.Add(new CafeReplayEntry{conversation_id=d.id,branch_id=branch,title=d.title,cat_id=id,cat_name=name,personality=personality,lines=lines});
                }
            };
            foreach(var e in save.state.missing_cases)foreach(var phase in new[]{"notice","waiting","confirmation","result"})add(CafeMissingDialogue.Progress(e,phase),contentId(e.cat_id),catName(e.cat_id),"店からの家出・帰還",b=>CafeMissingDialogue.Lines(e,phase,catName(e.cat_id)),false);
            var q=rules.intake;add(save.conversation,q.cat.id,q.cat.name,q.personality,b=>CafeIntakeConversation.Lines(b,q.cat.name),true);
            var r=rules.regular_introduction;add(save.regular_conversation,r.cat.id,r.cat.name,r.personality,b=>CafeRegularConversation.Lines(b,r.cat.name,customerName(save.state.regular_introduction.customer_id)),true);
            foreach(var row in save.dispatch_conversations??new CafeDispatchConversation[0])
            {
                string name=catName(row.cat_id),image=contentId(row.cat_id);
                add(row.departure,image,name,CafeDispatchDialogue.Title(row.Destination),b=>CafeDispatchDialogue.Lines("departure",b,name,row.Destination),true);
                add(row.travelling,image,name,"派遣中",b=>CafeDispatchDialogue.Lines("travelling",b,name,row.Destination),false);
                add(row.returning,image,name,"帰還",b=>CafeDispatchDialogue.Lines("return",b,name,row.Destination),true);
            }
            foreach(var row in save.coco_conversations??new CafeCocoConversation[0])add(row.progress,contentId(rules.visiting_cat.cat.id),rules.visiting_cat.cat.name,rules.visiting_cat.personality,b=>CafeCocoDialogue.Lines(row,b),row.progress.conversation_id==CafeCocoDialogue.AdmissionId);
            foreach(var row in save.equipment_conversations??new CafeEquipmentConversation[0])add(row.progress,"",CafeEquipmentDialogue.Title(row),row.day+"日目",b=>CafeEquipmentDialogue.Lines(row,b),true);
            foreach(var row in save.reservation_conversations??new CafeReservationConversation[0]){add(row.response,"",row.name,"特別予約",b=>CafeReservationDialogue.Lines(row,false,b),true);add(row.result,"",row.name,"特別予約の接客結果",b=>CafeReservationDialogue.Lines(row,true,b),false);}
            foreach(var row in save.trust_conversations??new CafeTrustConversation[0]){var ev=save.state.trust_events.Single(e=>e.id==row.event_id);string name=customerName(ev.customer_id);add(row.response,"",name,"顧客の信頼",b=>CafeTrustDialogue.Lines(false,b,name,rules.trust.popularity_loss),true);add(row.result,"",name,"接客結果",b=>CafeTrustDialogue.Lines(true,b,name,rules.trust.popularity_loss),false);}
            foreach(var row in save.adoption_conversations??new CafeAdoptionConversation[0])
            {var ev=save.state.adoption.events.Single(e=>e.id==row.event_id);string name=catName(ev.cat_id);add(row.progress,contentId(ev.cat_id),name,customerName(ev.customer_id)+"からの譲渡申し出",b=>CafeAdoptionDialogue.Lines(b,name,customerName(ev.customer_id),ev),true);}
            foreach(var row in save.lodge_conversations??new CafeLodgeConversation[0])
            {var ev=save.state.dispatches.Single(e=>e.id==row.event_id);string name=catName(ev.cat_id);add(row.response,contentId(ev.cat_id),name,"山あいの宿・トラブル対応",b=>CafeLodgeDialogue.Lines(false,b,name),true);add(row.returning,contentId(ev.cat_id),name,"山あいの宿・帰還確認",b=>CafeLodgeDialogue.Lines(true,b,name),true);}
            foreach(var row in save.encounter_conversations??new CafeEncounterConversation[0])
            {var ev=save.state.dispatches.Single(e=>e.id==row.event_id);string name=catName(ev.cat_id);add(row.progress,contentId(ev.cat_id),name,CafeEncounterDialogue.Title(row.progress.conversation_id),b=>CafeEncounterDialogue.Lines(b,name,row.progress.conversation_id),true);}
            foreach(var row in save.dispatch_introduction_conversations??new CafeDispatchIntroductionConversation[0])
            {var ev=save.state.dispatches.Single(e=>e.id==row.event_id);var c=rules.dispatch_introductions.Single(x=>x.destination==ev.destination).candidate;string name=ev.introduction.name;add(row.progress,"intro-"+ev.destination,name,c.personality,b=>CafeDispatchIntroductionDialogue.Lines(b,name,c.personality,c.features_label,c.trait,ev.destination),true);}
            if(save.popularity_ending?.progress.completed==true){var e=save.popularity_ending;string name=string.IsNullOrEmpty(e.cat_id)?"":catName(e.cat_id);result.Add(new CafeReplayEntry{conversation_id=CafePopularityEnding.Id,branch_id=CafePopularityEnding.Branch,title="人気目標 · 猫カフェのこれから",cat_id=string.IsNullOrEmpty(e.cat_id)?"":contentId(e.cat_id),cat_name=name,personality="達成時のカフェ",lines=CafePopularityEnding.Lines(CafePopularityEnding.Record(save.state),name)});}
            if(save.patron_ending?.progress.completed==true)result.Add(new CafeReplayEntry{conversation_id=CafePatronEnding.Id,branch_id=CafePopularityEnding.Branch,title="有力者目標 · 店の外へつながる信頼",cat_id="",cat_name="猫たちのカフェ",personality="達成時のカフェ",lines=CafePatronEnding.Lines(CafePatronEnding.Record(save.state),"")});
            if(save.bond_ending?.progress.completed==true){var e=save.bond_ending;string name=string.IsNullOrEmpty(e.cat_id)?"":catName(e.cat_id);result.Add(new CafeReplayEntry{conversation_id=CafeBondEnding.Id,branch_id=CafePopularityEnding.Branch,title="好感度目標 · 猫たちとのつながり",cat_id=string.IsNullOrEmpty(e.cat_id)?"":contentId(e.cat_id),cat_name=name,personality="達成時のカフェ",lines=CafeBondEnding.Lines(CafeBondEnding.Record(save.state),name)});}
            if(save.opening?.outcome=="viewed"){var o=save.opening;string id=o.cat_ids.FirstOrDefault()??"";result.Add(new CafeReplayEntry{conversation_id=CafeOpening.Id,branch_id=o.mode,title=All.Single(d=>d.id==CafeOpening.Id && d.branch==o.mode).title,cat_id=id==""?"":contentId(id),cat_name=o.cat_names.FirstOrDefault()??"猫たちのカフェ",personality="1日目の営業前",lines=CafeOpening.Lines(o)});}
            if(save.outcome?.progress.completed==true){var o=save.outcome;result.Add(new CafeReplayEntry{conversation_id=CafeOutcome.Id,branch_id=o.cause,title=o.Title,cat_id="",cat_name="カフェの記録",personality="結果確定時",lines=o.Lines()});}
            return result.GroupBy(e=>e.Key).Select(g=>g.Last()).ToArray();
        }
    }
    // Separate from gameplay slots. Read/merge/write prevents stale backend instances losing unlocks.
    public sealed class CafeReplayStore
    {
        readonly string path;readonly Func<CafeReplayProfile,string> encode;readonly Func<string,CafeReplayProfile> decode;
        public string Error {get;private set;}
        public bool Recovered {get;private set;}
        public CafeReplayStore(string directory,Func<CafeReplayProfile,string> encode,Func<string,CafeReplayProfile> decode){path=Path.Combine(directory,"extra-v1.json");this.encode=encode;this.decode=decode;}
        CafeReplayProfile Read(string file){var p=decode(File.ReadAllText(file,Encoding.UTF8));CafeReplayCatalog.Validate(p);return p;}
        public CafeReplayProfile Load()
        {
            Error=null;Recovered=false;
            if(!File.Exists(path) && !File.Exists(path+".bak"))return new CafeReplayProfile();
            try{return Read(path);}catch(Exception){try{var p=Read(path+".bak");Recovered=true;return p;}catch(Exception){Error="回想記録を読み込めません。記録は上書きしていません。";return new CafeReplayProfile();}}
        }
        public bool Merge(IEnumerable<CafeReplayEntry> entries)
        {
            var p=Load();if(Error!=null)return false;bool recovered=Recovered;var list=p.entries.ToList();
            foreach(var e in entries){CafeReplayCatalog.Validate(new CafeReplayProfile{entries=new[]{e}});if(!list.Any(x=>x.Key==e.Key))list.Add(e);}
            if(list.Count==p.entries.Length && !recovered)return true;
            p.entries=list.ToArray();string temp=path+".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using(var f=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){var bytes=Encoding.UTF8.GetBytes(encode(p));f.Write(bytes,0,bytes.Length);f.Flush(true);}
                Read(temp);
                if(File.Exists(path))CafeSaveStore.ReplaceWithRetry(temp,path,(source,target,backup)=>File.Replace(source,target,recovered?path+".invalid":backup));else File.Move(temp,path);
                return true;
            }
            catch(Exception){Error="回想記録を保存できません。営業セーブは変更していません。Extraを開き直して再試行できます。";return false;}
            finally{if(File.Exists(temp))try{File.Delete(temp);}catch(IOException){}}
        }
    }
}
