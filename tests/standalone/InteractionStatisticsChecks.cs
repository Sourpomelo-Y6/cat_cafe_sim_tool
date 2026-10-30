using CatCafePreview.Standalone;
using System.Diagnostics;
static partial class Program
{
    static void InteractionStatisticsChecks(string rulesPath,string output)
    {
        Directory.CreateDirectory(output);
        CafeRules Rules(){var r=Decode<CafeRules>(File.ReadAllText(rulesPath));r.store_events.probability=0;return r;}
        CafeSnapshot Clone(CafeSnapshot s)=>ReplayDecode(ReplayEncode(new CafeSaveEnvelope{state=s})).state;
        long Total(CafeInteractionStatistics s)=>s.Data.rows.Sum(r=>r.count);
        Assert(CafeInteractionStatistics.MissingFromJson("{\"state\":{}}"),"absent metadata is legacy");
        Assert(CafeInteractionStatistics.MissingFromJson("{\"interaction_statistics\": null,\"state\":{}}"),"explicit null metadata is legacy");
        Assert(!CafeInteractionStatistics.MissingFromJson("{\"interaction_statistics\":{}}"),"malformed present object is not legacy");
        Assert(!CafeInteractionStatistics.MissingFromJson("{\"interaction_statistics\": 123}"),"invalid present value is not legacy");
        Assert(CafeInteractionStatistics.MissingFromJson("{\"text\":\"\\\"interaction_statistics\\\":{}\"}"),"narrative string cannot impersonate metadata");
        var rules=Rules();var sim=new CafeSimulation(rules);var stats=new CafeInteractionStatistics(sim);var narratives=new CafeNarratives(sim);sim.InteractionCheckpoint=Clone;
        var control=new CafeSimulation(Decode<CafeRules>(Encode(rules)));
        void Both(string command,string choice=null,string source=null,string cat=null,string[] working=null){sim.Execute(command,choice:choice,source:source,catId:cat,working:working);control.Execute(command,choice:choice,source:source,catId:cat,working:working);Assert(CafeReplayJournal.Digest(sim.State)==CafeReplayJournal.Digest(control.State),"statistics never alter gameplay digest: "+command);CafeInteractionStatistics.Validate(stats.Data,sim.State);}
        Both("player_begin",cat:"cat-mike");Assert(Total(stats)==0,"begin is not a hand");Both("player_step","direct");
        Assert(Total(stats)==1 && stats.Select(cat:"cat-mike").Sum(r=>r.count)==1 && stats.Select(person:"player").Sum(r=>r.count)==1 && stats.Select("cat-mike","player").Sum(r=>r.count)==1,"one fact under all perspectives");
        var before=Encode(stats.Data);var session=sim.ActivePlayer;
        for(int i=0;i<25;i++){sim.PlayerHistory(session);sim.PlayerEventImageKey(session);sim.PlayerActionReason("direct");sim.Validate(sim.State);CafeInteractionStatisticsPresentation.Describe(stats,id=>id,id=>id);}
        Assert(before==Encode(stats.Data),"viewing availability and validation never count");
        bool rejected=false;try{sim.Execute("player_step",choice:"invalid");}catch(ArgumentException){rejected=true;}Assert(rejected && before==Encode(stats.Data),"invalid command never counts");
        // The producer can deliver the same result twice within a batch without double counting.
        var apply=typeof(CafeInteractionStatistics).GetMethod("Apply",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        var row=stats.Data.rows.Single();var duplicate=new CafeInteractionResult(session.id,1,new CafeInteractionCount{channel=row.channel,person_id=row.person_id,cat_id=row.cat_id,mode=row.mode,action=row.action,target=row.target,reaction=row.reaction,special=row.special});
        apply.Invoke(stats,new object[]{new[]{duplicate,duplicate}});Assert(before==Encode(stats.Data),"duplicate already applied live turn ignored");
        Both("player_step","switch","pet");Both("player_step","direct");Both("player_step","pause");
        Assert(Total(stats)==4 && stats.Data.rows.Where(r=>r.Normal).Sum(r=>r.count)==2,"auxiliary operations separated");Assert(stats.Data.rows.Single(r=>r.action=="switch").mode=="teaser" && stats.Data.rows.Single(r=>r.action=="switch").target=="pet","switch keeps source and destination");
        var store=new CafeSaveStore(output,envelope=>{envelope.interaction_statistics=stats.Data;return ReplayEncode(envelope);},ReplayDecode,sim.Validate);store.Save(1,sim.State);
        var saved=store.Load(1);CafeInteractionStatistics.Validate(saved.interaction_statistics,saved.state);string atSave=Encode(saved.interaction_statistics);Both("player_step","direct");Assert(Total(stats)==5,"advance after save");
        sim.Restore(saved.state);stats.Restore(saved.interaction_statistics);control.Restore(Clone(sim.State));Assert(Encode(stats.Data)==atSave,"load replaces rather than merges");Both("player_step","direct");Assert(Total(stats)==5,"resume adds only next hand");Both("player_finish");Assert(Total(stats)==5 && stats.Data.cursors.Count==0,"manual finish not an action and prunes cursors");
        sim.Execute("player_begin",catId:"cat-mike");var old=Clone(sim.State);sim.Execute("player_step",choice:"direct");old=Clone(sim.State);sim.Restore(old);stats.Restore(null);Assert(!stats.Data.complete && Total(stats)==0 && stats.Data.cursors.Single().turn==1,"legacy partial starts after existing action");
        sim.Execute("player_step",choice:"pause");Assert(Total(stats)==1,"legacy next hand only");Assert(CafeInteractionStatisticsPresentation.Describe(stats,id=>id,id=>id).Contains("以前の回数は含みません"),"legacy coverage notice");sim.Execute("player_finish");
        sim.Execute("set_shifts",working:new[]{"cat-mike","cat-tama","cat-sora"});sim.Execute("start_business");
        while(sim.State.phase=="open"){sim.Execute("advance_business");CafeInteractionStatistics.Validate(stats.Data,sim.State);}
        Assert(stats.Select(channel:"service").Sum(r=>r.count)>0,"automatic multi-seat services counted");Assert(stats.Select(channel:"player").Sum(r=>r.count)==1,"channels isolated");
        Assert(stats.Select(channel:"service").Sum(r=>r.count)==sim.State.visits.Sum(v=>(long)v.seated_ticks),"every service hand exactly once including pauses and switches");
        sim.Validate(sim.State);store.Save(2,sim.State);var closed=store.Load(2);CafeInteractionStatistics.Validate(closed.interaction_statistics,closed.state);Assert(closed.interaction_statistics.cursors.Count==0,"completed service cursors do not accumulate");
        // Old partial service: no historical inference, but the next tick continues correctly.
        var service=new CafeSimulation(Rules());service.Execute("set_shifts",working:new[]{"cat-mike"});service.Execute("start_business");while(!service.State.exchanges.Any() || service.State.visits.Sum(v=>v.seated_ticks)==0)service.Execute("advance_business");var serviceStats=new CafeInteractionStatistics(service);serviceStats.Reset(false);int serviceBefore=service.State.visits.Sum(v=>v.seated_ticks);service.Execute("advance_business");Assert(Total(serviceStats)==service.State.visits.Sum(v=>v.seated_ticks)-serviceBefore,"legacy service begins at live cursor");
        // Verify all special combinations through the real player path.
        var coverage=new HashSet<int>();
        foreach(string second in new[]{"direct","pause","switch","connect"})
        {
            var specialRules=Rules();specialRules.exchange.optional_threshold=1;specialRules.exchange.forced_threshold=100000;specialRules.exchange.gauge_cost=1;
            var special=new CafeSimulation(specialRules);var specialStats=new CafeInteractionStatistics(special);special.Execute("player_begin",catId:"cat-mike");special.Execute("player_step",choice:"direct");special.Execute("player_step",choice:second,source:second=="switch"?"pet":null);
            foreach(var r in specialStats.Data.rows)coverage.Add(r.special);
            var last=specialStats.Data.rows.Last();Assert(last.special==(second=="connect"?3:2),"open-up retained alongside "+second);Assert(Total(specialStats)==2,"special overlap still one hand");
            if(second=="connect")Assert(string.IsNullOrEmpty(last.reaction),"connect does not invent neutral reaction");
            CafeInteractionStatistics.Validate(specialStats.Data,special.State);
        }
        var connectRules=Rules();connectRules.exchange.optional_threshold=1;connectRules.exchange.forced_threshold=100000;connectRules.types[0].gain=0;connectRules.exchange.tension_neutral=5;connectRules.exchange.confused_threshold=0;
        var connectSim=new CafeSimulation(connectRules);var connectStats=new CafeInteractionStatistics(connectSim);connectSim.Execute("player_begin",catId:"cat-mike");connectSim.Execute("player_step",choice:"direct");connectSim.Execute("player_step",choice:"connect");coverage.Add(connectStats.Data.rows.Last().special);Assert(coverage.SetEquals(new[]{0,1,2,3}),"all special flags covered");
        var refusalRules=Rules();refusalRules.cats.Single(c=>c.id=="cat-mike").type_preferences[0]=0;var refusal=new CafeSimulation(refusalRules);var refusalStats=new CafeInteractionStatistics(refusal);refusal.Execute("player_begin",catId:"cat-mike");refusal.Execute("player_step",choice:"direct");Assert(refusalStats.Data.rows.Single().reaction=="turn_away" && Total(refusalStats)==1,"actual refusal response counts");
        // Late command failure rolls back state and discards the entire multi-seat batch.
        var fail=new CafeSimulation(Rules());var failStats=new CafeInteractionStatistics(fail);fail.InteractionCheckpoint=Clone;fail.Execute("set_shifts",working:new[]{"cat-mike"});fail.Execute("start_business");while(!fail.State.exchanges.Any())fail.Execute("advance_business");string stateBefore=Encode(fail.State),statsBefore=Encode(failStats.Data);fail.DailyStateObserved=()=>throw new InvalidOperationException("test late failure");try{fail.Execute("advance_business");}catch(InvalidOperationException){}Assert(Encode(fail.State)==stateBefore && Encode(failStats.Data)==statsBefore,"late failure atomic state and statistics");
        // Real autoplay includes many speculative StepExchange calls; only committed hands count.
        var autoRules=Rules();var automatic=new CafeSimulation(autoRules,objective:"bond");var autoStats=new CafeInteractionStatistics(automatic);automatic.InteractionCheckpoint=Clone;var autoControl=new CafeSimulation(Decode<CafeRules>(Encode(autoRules)),objective:"bond");automatic.StartAutoplay("bond","fast",1);autoControl.StartAutoplay("bond","fast",1);
        int autoSteps=0;while(automatic.State.autoplay.running && autoSteps++<500){automatic.StepAutoplay();autoControl.StepAutoplay();Assert(CafeReplayJournal.Digest(automatic.State)==CafeReplayJournal.Digest(autoControl.State),"autoplay speculative search does not alter gameplay");CafeInteractionStatistics.Validate(autoStats.Data,automatic.State);}
        Assert(!automatic.State.autoplay.running && autoSteps<500,"autoplay completes bounded run");Assert(autoStats.Select(channel:"player").Sum(r=>r.count)==automatic.State.player_sessions.Sum(p=>(long)p.actions.Count),"autoplay search not counted as real player actions");Assert(Total(autoStats)>0,"autoplay records actual hands");
        var autoFail=new CafeSimulation(Rules(),objective:"bond");var autoFailStats=new CafeInteractionStatistics(autoFail);autoFail.InteractionCheckpoint=Clone;autoFail.StartAutoplay("bond","basic",1);for(int i=0;i<100 && !autoFail.State.exchanges.Any();i++)autoFail.StepAutoplay();Assert(autoFail.State.exchanges.Any(),"autoplay failure fixture has active service");long autoBefore=Total(autoFailStats);int seatedBefore=autoFail.State.visits.Sum(v=>v.seated_ticks);autoFail.DailyStateObserved=()=>throw new InvalidOperationException("test nested failure");autoFail.StepAutoplay();Assert(!autoFail.State.autoplay.running && autoFail.State.autoplay.reason=="blocked" && Total(autoFailStats)==autoBefore && autoFail.State.visits.Sum(v=>v.seated_ticks)==seatedBefore,"caught nested autoplay failure discards tentative hands");
        // Explicit corruption must never be silently treated as an old save.
        for(int variant=0;variant<6;variant++)
        {
            var bad=Decode<CafeInteractionStatisticsData>(Encode(stats.Data));if(variant==0)bad.schema_version++;if(variant==1)bad.rows[0].count=-1;if(variant==2)bad.rows.Add(Decode<CafeInteractionCount>(Encode(bad.rows[0])));if(variant==3)bad.rows[0].cat_id="missing";if(variant==4)bad.cursors.Add(new CafeInteractionCursor{session="missing"});if(variant==5)bad.rows[0].reaction="unknown";
            rejected=false;try{CafeInteractionStatistics.Validate(bad,sim.State);}catch(ArgumentException){rejected=true;}Assert(rejected,"reject corrupt statistics "+variant);
        }
        var empty=new CafeSimulation(Rules());var emptyStats=new CafeInteractionStatistics(empty);Assert(emptyStats.Data.complete && Total(emptyStats)==0,"new game starts complete and empty");
        // Representative sparse save size, without retaining an enormous fixture.
        var large=Decode<CafeInteractionStatisticsData>(Encode(stats.Data));var seed=large.rows.First();large.rows.Clear();for(int i=0;i<10000;i++)large.rows.Add(new CafeInteractionCount{channel="service",person_id="test-person-"+i,cat_id=seed.cat_id,mode=seed.mode,action="direct",reaction="neutral",count=100});
        var timer=Stopwatch.StartNew();string largeJson=Encode(large);var roundTrip=Decode<CafeInteractionStatisticsData>(largeJson);CafeInteractionStatistics.Validate(roundTrip,sim.State);timer.Stop();
        File.WriteAllText(Path.Combine(output,"summary.txt"),$"PASS: {assertions} assertions\n10000 sparse rows: {System.Text.Encoding.UTF8.GetByteCount(largeJson)} bytes compact JSON; round-trip/validation {timer.ElapsedMilliseconds} ms\n");Console.WriteLine(File.ReadAllText(Path.Combine(output,"summary.txt")));
    }
}

