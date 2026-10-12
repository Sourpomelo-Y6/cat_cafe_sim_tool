using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
namespace CatCafePreview.Standalone
{
    [Serializable] public class CafeReplayOperation
    {
        public string kind,cat_id,choice,source,seat_id,customer_id,objective,mode;
        public int days;public string[] working;
    }
    [Serializable] public class CafeReplayRow
    {
        public int day,tick;public CafeReplayOperation operation,automatic_operation;
        public string state_digest,details;
        public string[] events;
    }
    [Serializable] public class CafeReplayDocument
    {
        public int format_version=1;public string mode_id="unity-cafe-replay",rules_id,base_json,base_digest,final_digest;
        public List<CafeReplayRow> operations=new List<CafeReplayRow>();
    }
    // Runtime journal is deliberately outside compact gameplay saves. Reload creates a new base.
    public sealed class CafeReplayJournal
    {
        private readonly CafeRules rules;
        private readonly Func<CafeSaveEnvelope,string> encode;
        private readonly Func<string,CafeSaveEnvelope> decode;
        public CafeReplayDocument Document {get;private set;}
        public CafeReplayJournal(CafeRules rules,CafeSnapshot initial,Func<CafeSaveEnvelope,string> encode,Func<string,CafeSaveEnvelope> decode)
        {
            this.rules=rules;this.encode=encode;this.decode=decode;
            Document=new CafeReplayDocument {rules_id=rules.rules_id,base_json=encode(new CafeSaveEnvelope {state=initial}),base_digest=Digest(initial),final_digest=Digest(initial)};
        }
        // Only presentation before-values are needed; never serialize the entire historical save per tick.
        public CafeSnapshot CaptureBefore(CafeSnapshot s)=>new CafeSnapshot {day=s.day,tick=s.tick,phase=s.phase,funds=s.funds,popularity=s.popularity,cats=s.cats.Select(c=>new CafeCat {id=c.id,fatigue=c.fatigue,stress=c.stress,health=c.health,activity=c.activity}).ToList(),visits=s.visits.Select(v=>new CafeVisit {id=v.id,cat_id=v.cat_id,departure=v.departure}).ToList()};
        private static readonly Dictionary<Type,FieldInfo[]> CanonicalFields=new Dictionary<Type,FieldInfo[]>();
        private static FieldInfo[] Fields(Type type){if(!CanonicalFields.TryGetValue(type,out var fields)){fields=type.GetFields(BindingFlags.Instance|BindingFlags.Public).OrderBy(f=>f.Name,StringComparer.Ordinal).ToArray();CanonicalFields[type]=fields;}return fields;}
        // Independent of Unity/.NET JSON formatting, float printers, process IDs and command epochs.
        public static string Digest(CafeSnapshot state)
        {
            var text=new StringBuilder();Canonical(state,text);using(var hash=SHA256.Create())return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())).Select(b=>b.ToString("x2")));
        }
        private static void Canonical(object value,StringBuilder text)
        {
            if(value==null){text.Append("null;");return;}
            if(value is string str){text.Append(str.Length).Append(':').Append(str).Append(';');return;}
            if(value is double number){text.Append(BitConverter.DoubleToInt64Bits(number).ToString("x16")).Append(';');return;}
            if(value is bool flag){text.Append(flag?'1':'0').Append(';');return;}
            if(value is int integer){text.Append(integer.ToString(CultureInfo.InvariantCulture)).Append(';');return;}
            if(value is System.Collections.IEnumerable sequence){text.Append('[');foreach(var item in sequence)Canonical(item,text);text.Append(']');return;}
            text.Append('{');foreach(var field in Fields(value.GetType()))
            {if(value is CafeDayResult && field.Name=="legacy_service_ticks" && field.GetValue(value)==null)continue;
                if(value is CafeSnapshot && field.Name=="import_history" && field.GetValue(value)==null)continue;
                if(value is CafeSnapshot && (field.Name=="instance_id" || field.Name=="revision") || value is CafeAutoRun && field.Name=="run_id")continue;text.Append(field.Name).Append(':');Canonical(field.GetValue(value),text);}text.Append('}');
        }
        public void Append(CafeSnapshot before,CafeSimulation sim,CafeReplayOperation operation,CafeAutoOperation automatic=null)
        {
            var after=sim.State;var events=new List<string>(sim.ReplayActions);
            foreach(var v in after.visits){var old=before.day==after.day?before.visits.Find(x=>x.id==v.id):null;if(old==null)events.Add("来店："+v.id+" / "+v.arrival+"tick");if(v.cat_id!=null && old?.cat_id!=v.cat_id)events.Add("接客開始："+v.id+" / "+v.cat_id+" / seat-"+(v.service_seat+1));if(v.departure!=null && old?.departure!=v.departure)events.Add("退店："+v.id+" / "+v.departure+" / 売上 "+N(v.bill)+" / "+v.satisfaction?.level);}
            foreach(var cat in after.cats){var old=before.cats.Find(c=>c.id==cat.id);if(old==null)events.Add("猫の加入："+cat.id);else if(old.fatigue!=cat.fatigue || old.stress!=cat.stress || old.health!=cat.health || old.activity!=cat.activity)events.Add(cat.id+"：疲労 "+N(old.fatigue)+" → "+N(cat.fatigue)+" / ストレス "+N(old.stress)+" → "+N(cat.stress)+" / "+old.health+" → "+cat.health+" / "+old.activity+" → "+cat.activity);}
            if(before.phase!=after.phase || before.day!=after.day)events.Add("営業："+before.day+"日目 "+before.phase+" → "+after.day+"日目 "+after.phase);
            events.Add("資金："+N(before.funds)+" → "+N(after.funds)+" / 人気："+N(before.popularity)+" → "+N(after.popularity));
            var row=new CafeReplayRow {day=before.day,tick=before.tick,operation=operation,automatic_operation=automatic==null?null:new CafeReplayOperation {kind=automatic.kind,working=automatic.working,cat_id=automatic.cat_id,choice=automatic.choice,source=automatic.source,seat_id=automatic.seat_id},events=events.ToArray(),state_digest=Digest(after)};
            row.details=before.day+"日目 / "+before.tick+"tick\n\n出来事・行動の結果\n"+string.Join("\n\n",events)+"\n\n操作の記録\n"+Describe(operation)+(row.automatic_operation==null?"":"\nおまかせの選択："+Describe(row.automatic_operation));Document.operations.Add(row);Document.final_digest=row.state_digest;
        }
        private static string N(double value)=>value.ToString("G17",CultureInfo.InvariantCulture);
        public static string Describe(CafeReplayOperation op)=>Label(op.kind)+" / "+op.cat_id+" / "+op.customer_id+" / "+op.seat_id+" / "+op.choice+" / "+op.source+" / "+string.Join(",",op.working??new string[0])+" / "+op.objective+" / "+op.mode+" / "+op.days;
        public static string Label(string kind)
        {switch(kind){case "start_business":return "営業開始";case "advance_business":return "営業を進行";case "set_shifts":return "出勤・休養の設定";case "next_day":return "翌日へ";case "day_off":return "休業";case "assign_service":return "接客の手動配置";case "player_begin":return "プレイヤー交流開始";case "player_step":return "プレイヤー交流の行動";case "player_finish":return "プレイヤー交流終了";case "start_autoplay":return "おまかせ開始";case "step_autoplay":return "おまかせ進行";case "stop_autoplay":return "おまかせ中止";case "pause_autoplay":return "おまかせ一時停止";case "resume_autoplay":return "おまかせ再開";case "save_game":return "営業保存";default:return kind;}}
        public static CafeAutoOperation Apply(CafeSimulation sim,CafeReplayOperation op)
        {
            if(op==null || string.IsNullOrEmpty(op.kind))throw new ArgumentException("リプレイの操作がありません。");
            if(sim.State.autoplay?.running==true && !new[]{"step_autoplay","stop_autoplay","pause_autoplay","resume_autoplay","save_game"}.Contains(op.kind))throw new ArgumentException("おまかせ中の通常操作は再生できません。");
            sim.ReplayActions.Clear();switch(op.kind)
            {
                case "start_autoplay":sim.StartAutoplay(op.objective,op.mode,op.days);break;
                case "step_autoplay":return sim.StepAutoplay();
                case "pause_autoplay":sim.PauseAutoplay();break;
                case "resume_autoplay":sim.ResumeAutoplay();break;
                case "stop_autoplay":sim.CancelAutoplay();break;
                // Replay save semantics without touching any gameplay files.
                case "save_game":sim.PauseAutoplay();sim.State.revision++;break;
                case "new_game":case "load_game":throw new ArgumentException("新規開始・ロードはログの起点にしてください。");
                default:sim.Execute(op.kind,op.working,op.cat_id,op.choice,op.source,op.seat_id,op.customer_id);break;
            }
            return null;
        }
        public CafeSimulation Verify(CafeReplayDocument data)
        {
            if(data==null || data.format_version!=1 || data.mode_id!="unity-cafe-replay" || data.rules_id!=rules.rules_id || data.operations==null || data.operations.Count>50000 || string.IsNullOrEmpty(data.base_json) || data.base_digest==null || data.final_digest==null)throw new ArgumentException("対応するUnity営業ログとルールが必要です。");
            foreach(var row in data.operations){NormalizeOperation(row?.operation);NormalizeOperation(row?.automatic_operation);if(row?.automatic_operation!=null && string.IsNullOrEmpty(row.automatic_operation.kind))row.automatic_operation=null;}
            var envelope=decode(data.base_json);if(envelope==null || envelope.version!=1 || envelope.state==null)throw new ArgumentException("営業ログの起点が不正です。");var sim=new CafeSimulation(rules);bool basePaused=envelope.state.autoplay?.paused??false;sim.Restore(envelope.state);if(sim.State.autoplay?.running==true)sim.State.autoplay.paused=basePaused;
            if(Digest(sim.State)!=data.base_digest)throw new ArgumentException("営業ログの起点が一致しません。");
            var reconstructed=new CafeReplayJournal(rules,sim.State,encode,decode);
            foreach(var row in data.operations)
            {
                if(row==null || row.day!=sim.State.day || row.tick!=sim.State.tick)throw new ArgumentException("営業ログの操作順が不正です。");var before=reconstructed.CaptureBefore(sim.State);var automatic=Apply(sim,row.operation);reconstructed.Append(before,sim,row.operation,automatic);var actual=reconstructed.Document.operations.Last();
                if(row.state_digest!=actual.state_digest || row.details!=actual.details || row.events==null || !row.events.SequenceEqual(actual.events) || DescribeNullable(row.automatic_operation)!=DescribeNullable(actual.automatic_operation))throw new ArgumentException("営業リプレイが一致しません："+reconstructed.Document.operations.Count+"操作目");
            }
            if(Digest(sim.State)!=data.final_digest)throw new ArgumentException("営業ログの最終結果が一致しません。");sim.Validate(sim.State);return sim;
        }
        private static void NormalizeOperation(CafeReplayOperation op)
        {
            if(op==null)return;
            foreach(var field in typeof(CafeReplayOperation).GetFields())if(field.FieldType==typeof(string) && field.Name!="kind" && (string)field.GetValue(op)=="")field.SetValue(op,null);
        }
        private static string DescribeNullable(CafeReplayOperation op)=>op==null?null:Describe(op);
    }
    public sealed partial class CafeSimulation
    {
        public readonly List<string> ReplayActions=new List<string>();
        private string ReplayBefore(CafeExchange e)=>"分類："+e.mode+" / 関心："+e.engagement.ToString("G17",CultureInfo.InvariantCulture)+" / 体力："+e.stamina.ToString("G17",CultureInfo.InvariantCulture)+" / テンション："+e.tension.ToString("G17",CultureInfo.InvariantCulture)+" / 親しみの見込み："+e.affinity_pending.ToString("G17",CultureInfo.InvariantCulture)+" / 心の通い："+e.connect_count+" / 心を開いた："+e.open_up_count+" / 同時発動："+e.simultaneous_count+" / ボーナス："+e.bonus.ToString("G17",CultureInfo.InvariantCulture);
        private void RecordReplayAction(CafeExchange e,string action,string target,string before,int openedBefore)=>ReplayActions.Add("営業時刻："+State.tick+(e.customer_id=="player"?" / 個別交流":" / 席：seat-"+(e.seat+1))+" / 猫："+e.cat_id+" / お客さん："+e.customer_id+"\n行動："+action+" / 対象："+target+" / 反応："+(e.open_up_count>openedBefore?"open_up":action=="connect"?"received":action=="finish"?"manual":e.previous_reaction)+"\n変更前："+before+"\n変更後："+ReplayBefore(e)+"\n交流終了："+e.end_reason);
    }
}
