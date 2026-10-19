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
        public static readonly CafeReplayDefinition[] All={
            new CafeReplayDefinition(CafeOpening.Id,"popularity","オープニング · 人気"),new CafeReplayDefinition(CafeOpening.Id,"patron","オープニング · 有力者"),new CafeReplayDefinition(CafeOpening.Id,"bond","オープニング · 好感度"),new CafeReplayDefinition(CafeOpening.Id,"free","オープニング · 自由営業"),
            new CafeReplayDefinition(CafePopularityEnding.Id,CafePopularityEnding.Branch,"人気目標 · 猫カフェのこれから"),
            new CafeReplayDefinition(CafeIntakeConversation.Id,"accepted","保護猫の受け入れ依頼 · 迎える"),new CafeReplayDefinition(CafeIntakeConversation.Id,"declined","保護猫の受け入れ依頼 · 見送る"),
            new CafeReplayDefinition(CafeRegularConversation.Id,"accepted","常連からの猫紹介 · 迎える"),new CafeReplayDefinition(CafeRegularConversation.Id,"declined","常連からの猫紹介 · 見送る"),
            new CafeReplayDefinition(CafeDispatchDialogue.Id+".departure","departed","近所のお店への訪問 · 出発する"),new CafeReplayDefinition(CafeDispatchDialogue.Id+".departure","cancelled","近所のお店への訪問 · 今回は出発しない"),
            new CafeReplayDefinition(CafeDispatchDialogue.Id+".travelling","travelling","近所のお店への訪問 · 派遣中"),new CafeReplayDefinition(CafeDispatchDialogue.Id+".return","received","近所のお店への訪問 · 帰還"),
            new CafeReplayDefinition(CafeEncounterDialogue.Id,"accept","商店街の追加のお願い · 引き受ける"),new CafeReplayDefinition(CafeEncounterDialogue.Id,"decline","商店街の追加のお願い · 予定どおり終える"),
            new CafeReplayDefinition(CafeDispatchIntroductionDialogue.Id,"accepted","商店街からの猫紹介 · 迎える"),new CafeReplayDefinition(CafeDispatchIntroductionDialogue.Id,"declined","商店街からの猫紹介 · 見送る")};
        public static void Validate(CafeReplayProfile profile)
        {
            if(profile==null || profile.version!=1 || profile.entries==null || profile.entries.Length>All.Length || profile.entries.Any(e=>e==null) || profile.entries.Select(e=>e.Key).Distinct().Count()!=profile.entries.Length)throw new ArgumentException("回想記録の形式が不正です。");
            foreach(var e in profile.entries)if(!All.Any(d=>d.Key==e.Key) || string.IsNullOrEmpty(e.title) || e.title.Length>256 || (e.cat_name??"").Length>256 || (e.cat_id??"").Length>256 || (e.personality??"").Length>1024 || e.lines==null || e.lines.Length==0 || e.lines.Length>100 || e.lines.Any(l=>l==null || l.text==null || l.text.Length>10000 || (l.speaker??"").Length>256))throw new ArgumentException("回想の本文が不正です。");
        }
        public static void ValidateSave(CafeSaveEnvelope s)
        {
            CafeOpening.Validate(s.opening,s.state);CafePopularityEnding.Validate(s.popularity_ending,s.state);CafeIntakeConversation.Validate(s.conversation,s.state.intake_request.status);CafeRegularConversation.Validate(s.regular_conversation,s.state.regular_introduction.status);
            CafeDispatchDialogue.Validate(s.dispatch_conversations,s.dispatch_conversation_selected,s.state);CafeEncounterDialogue.Validate(s.encounter_conversations,s.encounter_conversation_selected,s.state);CafeDispatchIntroductionDialogue.Validate(s.dispatch_introduction_conversations,s.dispatch_introduction_conversation_selected,s.state);
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
            var q=rules.intake;add(save.conversation,q.cat.id,q.cat.name,q.personality,b=>CafeIntakeConversation.Lines(b,q.cat.name),true);
            var r=rules.regular_introduction;add(save.regular_conversation,r.cat.id,r.cat.name,r.personality,b=>CafeRegularConversation.Lines(b,r.cat.name,customerName(save.state.regular_introduction.customer_id)),true);
            foreach(var row in save.dispatch_conversations??new CafeDispatchConversation[0])
            {
                string name=catName(row.cat_id),image=contentId(row.cat_id);
                add(row.departure,image,name,"近所のお店への訪問",b=>CafeDispatchDialogue.Lines("departure",b,name),true);
                add(row.travelling,image,name,"派遣中",b=>CafeDispatchDialogue.Lines("travelling",b,name),false);
                add(row.returning,image,name,"帰還",b=>CafeDispatchDialogue.Lines("return",b,name),true);
            }
            foreach(var row in save.encounter_conversations??new CafeEncounterConversation[0])
            {var ev=save.state.dispatches.Single(e=>e.id==row.event_id);string name=catName(ev.cat_id);add(row.progress,contentId(ev.cat_id),name,"商店街への派遣中",b=>CafeEncounterDialogue.Lines(b,name),true);}
            foreach(var row in save.dispatch_introduction_conversations??new CafeDispatchIntroductionConversation[0])
            {var ev=save.state.dispatches.Single(e=>e.id==row.event_id);var c=rules.dispatch_introductions.Single(x=>x.destination==ev.destination).candidate;string name=ev.introduction.name;add(row.progress,"intro-"+ev.destination,name,c.personality,b=>CafeDispatchIntroductionDialogue.Lines(b,name,c.personality,c.features_label,c.trait),true);}
            if(save.popularity_ending?.progress.completed==true){var e=save.popularity_ending;string name=string.IsNullOrEmpty(e.cat_id)?"":catName(e.cat_id);result.Add(new CafeReplayEntry{conversation_id=CafePopularityEnding.Id,branch_id=CafePopularityEnding.Branch,title="人気目標 · 猫カフェのこれから",cat_id=string.IsNullOrEmpty(e.cat_id)?"":contentId(e.cat_id),cat_name=name,personality="達成時のカフェ",lines=CafePopularityEnding.Lines(CafePopularityEnding.Record(save.state),name)});}
            if(save.opening?.outcome=="viewed"){var o=save.opening;string id=o.cat_ids.FirstOrDefault()??"";result.Add(new CafeReplayEntry{conversation_id=CafeOpening.Id,branch_id=o.mode,title=All.Single(d=>d.id==CafeOpening.Id && d.branch==o.mode).title,cat_id=id==""?"":contentId(id),cat_name=o.cat_names.FirstOrDefault()??"猫たちのカフェ",personality="1日目の営業前",lines=CafeOpening.Lines(o)});}
            return result.ToArray();
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
