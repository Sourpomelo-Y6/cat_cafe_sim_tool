using System;
using System.Collections.Generic;
using System.Linq;

namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeInteractionCount
    {
        public string channel, person_id, cat_id, mode, action, target, reaction;
        public int special;
        public long count;
        public bool Normal => action=="direct" || action=="adapt" || action=="intense" || action=="feint";
        internal string Key => string.Join("|", new[]{channel,person_id,cat_id,mode,action,target??"",reaction??"",special.ToString()}.Select(s=>s.Length+":"+s));
    }
    [Serializable] public sealed class CafeInteractionCursor
    {
        public string session, person_id, cat_id;
        public int turn;
    }
    [Serializable] public sealed class CafeInteractionStatisticsData
    {
        public int schema_version=1, started_day, started_tick;
        public bool complete;
        public List<CafeInteractionCount> rows=new List<CafeInteractionCount>();
        public List<CafeInteractionCursor> cursors=new List<CafeInteractionCursor>();
    }
    public sealed class CafeInteractionResult
    {
        public readonly string session;
        public readonly int turn;
        public readonly CafeInteractionCount row;
        public CafeInteractionResult(string session,int turn,CafeInteractionCount row){this.session=session;this.turn=turn;this.row=row;}
    }
    public sealed class CafeInteractionStatistics
    {
        readonly CafeSimulation sim;
        public CafeInteractionStatisticsData Data {get;private set;}
        Dictionary<string,CafeInteractionCount> index=new Dictionary<string,CafeInteractionCount>();
        public CafeInteractionStatistics(CafeSimulation simulation)
        {
            sim=simulation;Reset(true);sim.InteractionsCommitted+=Apply;
        }
        public static string ServiceSession(CafeSnapshot s,CafeVisit v)=>"service:"+s.day+":"+v.id.Length+":"+v.id+":"+v.cat_id.Length+":"+v.cat_id+":"+v.seated_at;
        public static List<CafeInteractionCursor> ActiveCursors(CafeSnapshot s)
        {
            var rows=s.exchanges.Select(e=>s.visits.Single(v=>v.id==e.customer_id)).Select(v=>new CafeInteractionCursor{session=ServiceSession(s,v),person_id=v.id,cat_id=v.cat_id,turn=v.seated_ticks}).ToList();
            rows.AddRange(s.player_sessions.Where(p=>p.exchange.end_reason==null).Select(p=>new CafeInteractionCursor{session=p.id,person_id="player",cat_id=p.cat_id,turn=p.actions.Count}));return rows;
        }
        public static bool MissingFromJson(string text)
        {
            var property=System.Text.RegularExpressions.Regex.Match(text,@"(?<!\\)""interaction_statistics""\s*:");
            return !property.Success || System.Text.RegularExpressions.Regex.IsMatch(text.Substring(property.Index+property.Length),@"^\s*null(?:\s*[,}])");
        }
        public void Reset(bool complete){Data=Begin(sim.State,complete);Reindex();}
        public static CafeInteractionStatisticsData Begin(CafeSnapshot s,bool complete)=>new CafeInteractionStatisticsData{started_day=s.day,started_tick=s.tick,complete=complete,cursors=ActiveCursors(s)};
        public void Restore(CafeInteractionStatisticsData data){Validate(data,sim.State);Data=data??Begin(sim.State,false);Reindex();}
        void Reindex(){index=Data.rows.ToDictionary(r=>r.Key);}
        void Apply(IReadOnlyList<CafeInteractionResult> results)
        {
            // Preflight the entire batch before changing any count.
            var cursors=Data.cursors.ToDictionary(c=>c.session,c=>new CafeInteractionCursor{session=c.session,person_id=c.person_id,cat_id=c.cat_id,turn=c.turn});
            var increments=new Dictionary<string,long>();var accepted=new List<CafeInteractionCount>();
            foreach(var result in results)
            {
                var row=result.row;CheckRow(row,sim.State);CafeInteractionCursor cursor;
                if(!cursors.TryGetValue(result.session,out cursor)){cursor=new CafeInteractionCursor{session=result.session,person_id=row.person_id,cat_id=row.cat_id};cursors.Add(result.session,cursor);}
                if(cursor.person_id!=row.person_id || cursor.cat_id!=row.cat_id || result.turn<1)throw new ArgumentException("交流記録の相手が一致しません。");
                if(result.turn<=cursor.turn)continue;
                if(result.turn!=cursor.turn+1)throw new ArgumentException("交流記録の順序が一致しません。");
                cursor.turn=result.turn;string key=row.Key;long increment;increments.TryGetValue(key,out increment);increments[key]=checked(increment+1);accepted.Add(row);
            }
            checked{long total=Data.rows.Sum(r=>r.count)+accepted.Count;}
            foreach(var pair in increments){CafeInteractionCount current;index.TryGetValue(pair.Key,out current);checked{long total=(current?.count??0)+pair.Value;}}
            foreach(var row in accepted){CafeInteractionCount current;if(!index.TryGetValue(row.Key,out current)){current=new CafeInteractionCount{channel=row.channel,person_id=row.person_id,cat_id=row.cat_id,mode=row.mode,action=row.action,target=row.target,reaction=row.reaction,special=row.special};index.Add(row.Key,current);Data.rows.Add(current);}current.count++;}
            // Completed sessions cannot emit again: only the live command boundary publishes.
            Data.cursors=ActiveCursors(sim.State);
        }
        public IEnumerable<CafeInteractionCount> Select(string cat=null,string person=null,string channel=null)=>Data.rows.Where(r=>(cat==null || r.cat_id==cat) && (person==null || r.person_id==person) && (channel==null || r.channel==channel));
        static readonly string[] Reactions={"neutral","favorable","enthusiastic","confused","listless","turn_away"};
        static void CheckRow(CafeInteractionCount r,CafeSnapshot state)
        {
            if(r==null || !new[]{"service","player"}.Contains(r.channel) || string.IsNullOrEmpty(r.person_id) || r.person_id.Length>256 || !state.cats.Any(c=>c.id==r.cat_id) || (r.channel=="player")!=(r.person_id=="player") || !CafeSimulation.GrowthTypes.Contains(r.mode) || !CafeSimulation.PlayerActions.Contains(r.action) || r.special<0 || r.special>3 || ((r.special&1)!=0)!=(r.action=="connect") || (r.action=="switch"?!CafeSimulation.GrowthTypes.Contains(r.target):!string.IsNullOrEmpty(r.target)) || (r.action=="connect"?!string.IsNullOrEmpty(r.reaction):!Reactions.Contains(r.reaction)))throw new ArgumentException("交流累計の項目が不正です。");
        }
        public static void Validate(CafeInteractionStatisticsData data,CafeSnapshot state)
        {
            if(data==null)return;
            if(data.schema_version!=1 || data.started_day<1 || data.started_day>state.day || data.started_tick<0 || data.started_day==state.day && data.started_tick>state.tick || data.rows==null || data.cursors==null)throw new ArgumentException("交流累計の保存形式が不正です。");
            var keys=new HashSet<string>();long total=0;
            foreach(var row in data.rows){CheckRow(row,state);if(row.count<=0 || !keys.Add(row.Key))throw new ArgumentException("交流累計が重複または不正です。");total=checked(total+row.count);}
            var expected=ActiveCursors(state);if(data.cursors.Count!=expected.Count)throw new ArgumentException("交流累計の再開位置が不正です。");
            keys.Clear();foreach(var c in data.cursors){var e=c==null?null:expected.FirstOrDefault(x=>x.session==c.session);if(e==null || !keys.Add(c.session) || c.person_id!=e.person_id || c.cat_id!=e.cat_id || c.turn!=e.turn)throw new ArgumentException("交流累計の再開位置が一致しません。");}
        }
    }
    public sealed partial class CafeSimulation
    {
        public event Action<IReadOnlyList<CafeInteractionResult>> InteractionsCommitted;
        public Func<CafeSnapshot,CafeSnapshot> InteractionCheckpoint;
        public Action InteractionTransactionStarted,InteractionTransactionAborted;
        List<CafeInteractionResult> interactionBatch;
        T InteractionCommand<T>(Func<T> command,bool mayExchange=true)
        {
            var checkpoint=!mayExchange || InteractionsCommitted==null?null:InteractionCheckpoint?.Invoke(State);
            if(interactionBatch!=null)
            {
                int offset=interactionBatch.Count;
                try{return command();}
                catch{interactionBatch.RemoveRange(offset,interactionBatch.Count-offset);if(checkpoint!=null)State=checkpoint;InteractionTransactionAborted?.Invoke();throw;}
            }
            interactionBatch=new List<CafeInteractionResult>();
            try{InteractionTransactionStarted?.Invoke();T result=command();InteractionsCommitted?.Invoke(interactionBatch);return result;}
            catch{if(checkpoint!=null)State=checkpoint;InteractionTransactionAborted?.Invoke();throw;}
            finally{interactionBatch=null;}
        }
        void ObserveInteraction(CafeExchange e,string mode,string action,string target,int opened,int turn,string session)
        {
            interactionBatch?.Add(new CafeInteractionResult(session,turn,new CafeInteractionCount{channel=e.customer_id=="player"?"player":"service",person_id=e.customer_id,cat_id=e.cat_id,mode=mode,action=action,target=target,reaction=e.previous_reaction,special=(action=="connect"?1:0)|(e.open_up_count>opened?2:0)}));
        }
    }
}
