using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
namespace CatCafePreview.Standalone
{
    [Serializable] public class CafeDailyBaseline { public int day; public string cat_id, fatigue; }
    [Serializable] public class CafeDailyRecord
    {
        public int day; public string cat_id,day_type,shift,count,ticks,spent,fatigue,health,xp,affinity;
        public string Text()=>"日目："+day+"\n営業区分："+day_type+"\n予定："+shift+"\n接客件数："+count+"\n接客行動数："+ticks+"\n体力消耗："+spent+"\n疲労変化："+fatigue+"\n体調変化："+health+"\n獲得経験："+xp+"\n親しみ増減合計："+affinity;
    }
    // Observes settled facts only. Optional save metadata never enters gameplay or replay hashes.
    public sealed class CafeDailyRecords
    {
        private readonly CafeSimulation sim;
        public List<CafeDailyBaseline> Baselines=new List<CafeDailyBaseline>();
        public List<CafeDailyRecord> Records=new List<CafeDailyRecord>();
        private readonly Dictionary<string,string> shifts=new Dictionary<string,string>(),healthBefore=new Dictionary<string,string>();
        private bool dayOff; private string epoch;
        public CafeDailyRecords(CafeSimulation simulation,bool fresh=false)
        {
            sim=simulation;epoch=sim.State.instance_id;
            sim.DailyStateObserved=Sync;
            sim.DailyFatigueReset=id=>{Sync();var row=Baselines.FirstOrDefault(b=>b.day==sim.State.day && b.cat_id==id);if(row!=null)row.fatigue=sim.State.cats.Single(c=>c.id==id).fatigue.ToString("R",CultureInfo.InvariantCulture);};
            sim.BeforeDailyClose=BeforeClose;sim.AfterDailyClose=AfterClose;sim.BeginDailyRecord=BeginDay;
            if(fresh)BeginDay();
        }
        public void Reset(bool fresh){epoch=sim.State.instance_id;Baselines.Clear();Records.Clear();if(fresh)BeginDay();}
        public void Restore(CafeDailyBaseline[] baselines,CafeDailyRecord[] records){epoch=sim.State.instance_id;Baselines=(baselines??new CafeDailyBaseline[0]).ToList();Records=(records??new CafeDailyRecord[0]).ToList();}
        public void Sync()
        {
            if(epoch!=sim.State.instance_id)Reset(false);
            if(sim.State.phase=="closed")foreach(var r in Records.Where(r=>r.day==sim.State.day))
            {
                var c=sim.State.cats.Single(c=>c.id==r.cat_id);r.spent=CafeDetailPresentation.Number(sim.Rules.max_stamina-c.stamina);var baseline=Baselines.FirstOrDefault(b=>b.day==r.day && b.cat_id==r.cat_id);if(baseline!=null)r.fatigue=CafeDetailPresentation.Number(double.Parse(baseline.fatigue,CultureInfo.InvariantCulture))+" → "+CafeDetailPresentation.Number(c.fatigue);
                var ops=sim.State.growth_operations.Where(o=>o.day==r.day && o.cat_id==r.cat_id).ToArray();r.xp="接客＋"+CafeDetailPresentation.Number(ops.Count(o=>o.kind=="service")*sim.Rules.growth.service_xp)+"・休養＋"+CafeDetailPresentation.Number(ops.Count(o=>o.kind=="rest")*sim.Rules.growth.rest_xp)+"・派遣＋"+CafeDetailPresentation.Number(ops.Count(o=>o.kind=="dispatch")*sim.Rules.growth.dispatch_xp);
            }
            if(sim.State.phase=="preparation" && Baselines.Any(b=>b.day==sim.State.day))
                foreach(var c in sim.State.cats.Where(c=>!Baselines.Any(b=>b.cat_id==c.id)))Baselines.Add(new CafeDailyBaseline{day=sim.State.day,cat_id=c.id,fatigue=c.fatigue.ToString("R",CultureInfo.InvariantCulture)});
        }
        private void BeginDay()
        {
            Sync();Baselines=sim.State.cats.Select(c=>new CafeDailyBaseline{day=sim.State.day,cat_id=c.id,fatigue=c.fatigue.ToString("R",CultureInfo.InvariantCulture)}).ToList();
        }
        private void BeforeClose(bool off)
        {
            Sync();dayOff=off;shifts.Clear();healthBefore.Clear();
            foreach(var c in sim.State.cats){shifts[c.id]=c.activity=="cafe"?(c.working?"出勤":"休養"):c.activity=="dispatched"?"派遣中":c.activity=="adopted"?"譲渡済み":"行方不明";healthBefore[c.id]=c.health;}
        }
        private void AfterClose()
        {
            var s=sim.State;foreach(var c in s.cats)
            {
                var ops=s.growth_operations.Where(o=>o.day==s.day && o.cat_id==c.id).ToArray();var services=ops.Where(o=>o.kind=="service").ToArray();var before=Baselines.FirstOrDefault(b=>b.day==s.day && b.cat_id==c.id);
                if(before==null)continue; // Partial old days have no proven start; retain the existing missing-value projection.
                string outcome=healthBefore[c.id]=="sick"?(c.health=="sick"?"療養中":"回復"):(c.health=="sick"?"発症":"健康維持");
                var row=new CafeDailyRecord{day=s.day,cat_id=c.id,day_type=dayOff?"休業":"営業",shift=c.activity=="missing"?"行方不明":c.activity=="adopted"?"譲渡済み":shifts[c.id],count=services.Length.ToString(CultureInfo.InvariantCulture),ticks=c.service_ticks.ToString(CultureInfo.InvariantCulture),spent=CafeDetailPresentation.Number(sim.Rules.max_stamina-c.stamina),fatigue=before==null?"記録なし":CafeDetailPresentation.Number(double.Parse(before.fatigue,CultureInfo.InvariantCulture))+" → "+CafeDetailPresentation.Number(c.fatigue),health=outcome+" · "+(c.health=="sick"?"療養あと"+c.recovery_remaining+"日":"健康"),xp="接客＋"+CafeDetailPresentation.Number(services.Length*sim.Rules.growth.service_xp)+"・休養＋"+CafeDetailPresentation.Number(ops.Count(o=>o.kind=="rest")*sim.Rules.growth.rest_xp)+"・派遣＋"+CafeDetailPresentation.Number(ops.Count(o=>o.kind=="dispatch")*sim.Rules.growth.dispatch_xp),affinity=CafeDetailPresentation.Number(services.Sum(o=>o.affinity_delta))};
                Records.RemoveAll(r=>r.day==row.day && r.cat_id==row.cat_id);Records.Add(row);
            }
        }
        public CafeDailyRecord Find(int day,string id){return epoch==sim.State.instance_id?Records.FirstOrDefault(r=>r.day==day && r.cat_id==id):null;}
        private static bool Numeric(string value,double min,double max){double n;return value!=null && value.Length<=32 && double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out n) && !double.IsNaN(n) && !double.IsInfinity(n) && n>=min && n<=max;}
        private static bool Count(string value){int n;return int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out n) && n>=0;}
        private static bool Facts(CafeDailyRecord r)
        {
            if(!Count(r.count) || !Count(r.ticks) || !Numeric(r.spent,0,100) || !Numeric(r.affinity,-1e12,1e12))return false;
            var fatigue=(r.fatigue??"").Split(new[]{" → "},StringSplitOptions.None);if(r.fatigue!="記録なし" && (fatigue.Length!=2 || !Numeric(fatigue[0],0,100) || !Numeric(fatigue[1],0,100)))return false;
            if(!Regex.IsMatch(r.health??"",@"\A(健康維持|発症|療養中|回復) · (健康|療養あと[1-9][0-9]*日)\z"))return false;
            var xp=Regex.Match(r.xp??"",@"\A接客＋(.+)・休養＋(.+)・派遣＋(.+)\z");return xp.Success && Enumerable.Range(1,3).All(i=>Numeric(xp.Groups[i].Value,0,1e12));
        }
        public static void Validate(CafeSaveEnvelope save)
        {
            var keys=new HashSet<string>();var rows=save.daily_records??new CafeDailyRecord[0];if(rows.Length>100000)throw new ArgumentException("日次記録が多すぎます。");
            foreach(var r in rows){if(r==null || r.day<1 || r.day>save.state.day || r.day==save.state.day && save.state.phase!="closed" || !save.state.cats.Any(c=>c.id==r.cat_id) || !keys.Add(r.day+":"+r.cat_id) || !new[]{"営業","休業"}.Contains(r.day_type) || !new[]{"出勤","休養","派遣中","譲渡済み","行方不明"}.Contains(r.shift) || new[]{r.count,r.ticks,r.spent,r.fatigue,r.health,r.xp,r.affinity}.Any(v=>string.IsNullOrEmpty(v) || v.Length>256) || !Facts(r))throw new ArgumentException("保存済みの日次記録が不正です。");}
            keys.Clear();var baselines=save.daily_baselines??new CafeDailyBaseline[0];if(baselines.Length>save.state.cats.Count)throw new ArgumentException("日次開始記録が不正です。");
            foreach(var b in baselines){double value;if(b==null || b.day!=save.state.day || !save.state.cats.Any(c=>c.id==b.cat_id) || !keys.Add(b.cat_id) || b.fatigue==null || b.fatigue.Length>32 || !double.TryParse(b.fatigue,NumberStyles.Float,CultureInfo.InvariantCulture,out value) || double.IsNaN(value) || double.IsInfinity(value) || value<0 || value>100)throw new ArgumentException("日次開始記録が不正です。");}
        }
    }
}
