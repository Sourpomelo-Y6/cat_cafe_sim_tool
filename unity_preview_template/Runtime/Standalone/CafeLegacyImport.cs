using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace CatCafePreview.Standalone
{
    [Serializable] public class CafeLegacyBundle
    {
        public string kind="cat-cafe-unity-import",rules_id,source_path,source_hash,relationship_path,relationship_hash,converted_at,state_digest;
        public int version=1,source_version;public CafeSnapshot state;public string[] double_bits;public string[] notes;public CatDefinition[] definitions;
    }
    [Serializable] public class CafeImportHistory
    {
        public int version=1;public string base_json,base_digest,source_hash;
        public List<CafeReplayOperation> operations=new List<CafeReplayOperation>();
    }
    public static class CafeLegacyImport
    {
        // Set by the platform codec (Unity JsonUtility or the verification tool's JSON codec).
        public static Func<string,CafeSnapshot> DecodeBase;
        public static string GameDigest(CafeSnapshot state)
        {
            var copy=new CafeSnapshot();foreach(var field in typeof(CafeSnapshot).GetFields())if(field.Name!="import_history")field.SetValue(copy,field.GetValue(state));
            // Unity writes null optional strings as empty strings. Normalize only the import
            // comparison copy so existing non-imported replay digests remain compatible.
            copy.growth_operations=state.growth_operations?.Select(o=>new CafeGrowthOperation {kind=o.kind,cat_id=o.cat_id,customer_id=string.IsNullOrEmpty(o.customer_id)?null:o.customer_id,choice=string.IsNullOrEmpty(o.choice)?null:o.choice,day=o.day,tick=o.tick,actions=o.actions,affinity_delta=o.affinity_delta,stamina_spent=o.stamina_spent}).ToList();
            copy.equipment_moves=state.equipment_moves?.Select(o=>new CafeEquipmentMove {day=o.day,seat=o.seat,item=string.IsNullOrEmpty(o.item)?null:o.item}).ToList();
            return CafeReplayJournal.Digest(copy);
        }
        public static bool Hash(string value)=>value!=null && value.Length==64 && value.All(c=>c>='0' && c<='9' || c>='a' && c<='f');
        public static void NormalizeOperation(CafeReplayOperation operation)
        {
            if(operation==null)return;
            if(CafeStaminaRules.Missing(operation.stamina_rules))operation.stamina_rules=null;
            foreach(var field in typeof(CafeReplayOperation).GetFields())if(field.FieldType==typeof(string) && field.Name!="kind" && string.IsNullOrEmpty((string)field.GetValue(operation)))field.SetValue(operation,null);
            if(operation.kind!="set_shifts")operation.working=null;
        }
        public static void ValidateBundle(CafeLegacyBundle bundle,CafeRules rules)
        {
            if(bundle==null || bundle.kind!="cat-cafe-unity-import" || bundle.version!=1 || bundle.source_version<1 || bundle.source_version>2 || bundle.rules_id!=rules.rules_id || !Hash(bundle.source_hash) || !Hash(bundle.relationship_hash) || !Hash(bundle.state_digest) || bundle.state==null || bundle.state.import_history!=null || !DateTimeOffset.TryParse(bundle.converted_at,out _) || bundle.notes==null)throw new ArgumentException("対応する変換済み取込ファイルではありません。");
            var sim=new CafeSimulation(rules);sim.ValidateImportedBase(bundle.state);
            var expected=sim.ImportDefinitions(bundle.state);if(bundle.definitions==null || bundle.definitions.Length!=expected.Length)throw new ArgumentException("猫の個性・特徴の情報が不足しています。");
            foreach(var definition in expected){var actual=bundle.definitions.SingleOrDefault(c=>c.id==definition.id);if(actual==null)throw new ArgumentException("対応していない猫IDです。");foreach(var field in typeof(CatDefinition).GetFields()){var a=field.GetValue(actual);var b=field.GetValue(definition);if(field.Name=="trait_id" && string.IsNullOrEmpty(a as string) && string.IsNullOrEmpty(b as string))continue;if(a is Array x && b is Array y?!x.Cast<object>().SequenceEqual(y.Cast<object>()):!object.Equals(a,b))throw new ArgumentException("猫 "+definition.id+" の名前・個性・特徴が単独版の標準設定と異なります："+field.Name);}}if(GameDigest(bundle.state)!=bundle.state_digest)throw new ArgumentException("変換済み状態が変更されています。");
        }
    }
    public sealed partial class CafeSimulation
    {
        private int importDepth;
        private T TraceImported<T>(CafeReplayOperation operation,Func<T> action)
        {
            bool record=State.import_history!=null && importDepth==0;if(record){operation.growth_policy_version=1;operation.stamina_policy_version=LegacyStaminaReplay?0:1;operation.player_policy_version=LegacyPlayerReplay?0:2;CafeLegacyImport.NormalizeOperation(operation);}importDepth++;T result;try{result=action();}finally{importDepth--;}
            if(record){operation.stamina_rules=LegacyStaminaReplay?null:State.stamina_growth;State.import_history.operations.Add(operation);}return result;
        }
        private void TraceImported(CafeReplayOperation operation,Action action)=>TraceImported(operation,()=>{action();return 0;});
        // Python compact saves omit past action tapes. Keep their validated current state as an anchor;
        // all changes after import are checked by replay, without inventing prior decisions.
        private void ValidateImportedHistory(CafeSnapshot state)
        {
            var h=state.import_history;if(h.version!=1 || !CafeLegacyImport.Hash(h.source_hash) || !CafeLegacyImport.Hash(h.base_digest) || h.operations==null || h.operations.Count>50000 || string.IsNullOrEmpty(h.base_json) || CafeLegacyImport.DecodeBase==null)throw new ArgumentException("移行起点と操作記録が不正です。");
            var baseline=CafeLegacyImport.DecodeBase(h.base_json);if(baseline==null || baseline.import_history!=null || CafeLegacyImport.GameDigest(baseline)!=h.base_digest)throw new ArgumentException("移行起点が変更されています。");
            var sim=new CafeSimulation(Rules);sim.ValidateImportedBase(baseline);sim.State=baseline;
            try{foreach(var operation in h.operations){if(operation.player_policy_version!=0 && operation.player_policy_version!=2)throw new ArgumentException("Invalid player policy");sim.LegacyStaminaReplay=operation.stamina_policy_version==0;sim.LegacyPlayerReplay=operation.player_policy_version==0;CafeReplayJournal.Apply(sim,operation);}}
            catch(InvalidOperationException ex){throw new ArgumentException("移行後の操作記録を再生できません。",ex);}
            if(CafeLegacyImport.GameDigest(sim.State)!=CafeLegacyImport.GameDigest(state)){
                var changed=typeof(CafeSnapshot).GetFields().Where(f=>f.Name!="import_history" && f.Name!="instance_id" && f.Name!="revision").Where(f=>{var a=new CafeSnapshot();var b=new CafeSnapshot();f.SetValue(a,f.GetValue(sim.State));f.SetValue(b,f.GetValue(state));return CafeLegacyImport.GameDigest(a)!=CafeLegacyImport.GameDigest(b);}).Select(f=>f.Name);
                throw new ArgumentException("移行後の状態が操作記録と一致しません："+string.Join(",",changed));
            }
        }
        public CatDefinition[] ImportDefinitions(CafeSnapshot state)=>Definitions(state).ToArray();
        public void ValidateImportedBase(CafeSnapshot s)
        {
            if(s==null || s.version!=2 || s.rules_id!=Rules.rules_id || s.seed<0 || s.day<1 || !new[]{"free","popularity","bond","patron"}.Contains(s.objective) || !new[]{"preparation","open","closed"}.Contains(s.phase) || s.tick<0 || s.tick>Rules.opening_ticks || (s.phase=="closed")!=(s.tick==Rules.opening_ticks) || s.phase=="preparation" && s.tick!=0 || s.import_history!=null || s.autoplay!=null || s.cats==null || s.history==null || s.history.Count!=s.day-1 || s.visits==null || s.exchanges==null || s.queue==null || s.relationships==null || s.growth==null || s.player_cats==null || s.player_sessions==null || s.store_events==null || s.store_events.Count!=s.day || s.seat_count<2 || s.seat_count>8)throw new ArgumentException("移行できる標準経営セーブの状態が不足しています。");
            if(!Finite(s.funds) || !Finite(s.opening_funds) || !Range(s.revenue,0,double.MaxValue) || !Range(s.expenses,0,double.MaxValue) || !Range(s.popularity,0,Rules.popularity.cap) || Math.Abs(s.funds-(s.opening_funds+s.revenue+s.item_sales_income+s.dispatch_income-s.expenses))>1e-7)throw new ArgumentException("移行する収支が一致しません。");
            ValidateStaminaGrowth(s);var definitions=Definitions(s).ToArray();if(s.cats.Count!=definitions.Length || s.cats.Select(c=>c.id).Distinct().Count()!=s.cats.Count || s.cats.Any(c=>!definitions.Any(d=>d.id==c.id) || !Range(c.stamina,0,MaxStamina(s,c.id)) || !Range(c.fatigue,0,Rules.max_fatigue) || !Range(c.stress,0,100) || !new[]{"healthy","sick"}.Contains(c.health) || !new[]{"cafe","dispatched","missing","adopted"}.Contains(c.activity)))throw new ArgumentException("移行する猫の構成・状態が対応範囲外です。");
            if(!s.player_cats.Select(c=>c.cat_id).OrderBy(c=>c).SequenceEqual(s.cats.Select(c=>c.id).OrderBy(c=>c)) || s.player_cats.Any(c=>!Range(c.affinity,0,Rules.player.max_affinity) || c.today<0 || c.total<c.today) || s.player_cats.Sum(c=>c.today)>Rules.player.daily_turns || s.growth.Count!=s.cats.Count || s.queue.Distinct().Count()!=s.queue.Count || s.queue.Any(id=>!s.visits.Any(v=>v.id==id && v.departure==null)))throw new ArgumentException("移行する交流・待機の状態が不正です。");
            if(s.exchanges.Select(e=>e.seat).Distinct().Count()!=s.exchanges.Count || s.exchanges.Select(e=>e.cat_id).Distinct().Count()!=s.exchanges.Count || s.exchanges.Any(e=>s.phase!="open" || e.seat<0 || e.seat>=s.seat_count || e.remaining<1 || !s.cats.Any(c=>c.id==e.cat_id) || !s.visits.Any(v=>v.id==e.customer_id && v.cat_id==e.cat_id) || (e.max_stamina!=0 && e.max_stamina!=MaxStamina(s,e.cat_id)) || !Range(e.stamina,0,MaxStamina(s,e.cat_id))))throw new ArgumentException("移行する接客途中の状態が不正です。");
            foreach(var session in s.player_sessions){var expected=ReplayPlayer(s,session);foreach(var field in typeof(CafeExchange).GetFields()){var a=field.GetValue(expected);var b=field.GetValue(session.exchange);if(a is Array x && b is Array y? !x.Cast<object>().SequenceEqual(y.Cast<object>()):!object.Equals(a,b))throw new ArgumentException("移行する個別交流の記録が一致しません。");}}
            foreach(var h in s.history)if(h.day<1 || h.day>=s.day || !Finite(h.closing_funds) || h.customer_records==null || Math.Abs(h.closing_funds-(h.opening_funds+h.revenue+h.item_sales_income+h.dispatch_income-h.expenses))>1e-7)throw new ArgumentException("移行する日次収支が一致しません。");
            ValidateRecruitment(s);ValidateShop(s);ValidateRestSpace(s);ValidateWaitingArea(s);ValidateIntake(s);ValidateVisitingCat(s);
        }
    }
}
