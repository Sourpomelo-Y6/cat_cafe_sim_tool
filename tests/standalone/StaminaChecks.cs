using CatCafePreview.Standalone;
using System.Reflection;
static partial class Program
{
    static void StaminaResume(string output)
    {
        var sim=new CafeSimulation(Decode<CafeRules>(File.ReadAllText(Path.Combine(output,"resume-rules.json"))));var store=new CafeSaveStore(output,ReplayEncode,ReplayDecode,sim.Validate);
        for(int i=0;i<3;i++){sim.Restore(store.Load(1).state);Assert(sim.State.cats.All(c=>sim.MaxStamina(c.id)==300 && c.stamina==300),"Fresh process keeps cap/current stamina");Assert(CafeReplayJournal.Digest(sim.State)==File.ReadAllText(Path.Combine(output,"resume-digest.txt")),"Fresh process digest");store.Save(1,sim.State);}
        Console.WriteLine($"PASS separate-process stamina resume: {assertions} assertions");
    }
    static void StaminaChecks(string rulesPath,string output)
    {
        Directory.CreateDirectory(output);
        var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));rules.store_events.probability=0;rules.starting_funds=100000000;
        CafeSimulation New()=>new CafeSimulation(Decode<CafeRules>(Encode(rules)));
        void Roundtrip(CafeSimulation sim)
        {
            var digest=CafeReplayJournal.Digest(sim.State);var store=new CafeSaveStore(output,ReplayEncode,ReplayDecode,sim.Validate);
            store.Save(1,sim.State);var resumed=new CafeSimulation(sim.Rules);resumed.Restore(store.Load(1).state);sim.Restore(store.Load(1).state);
            Assert(digest==CafeReplayJournal.Digest(resumed.State) && digest==CafeReplayJournal.Digest(sim.State),"Repeated disk resume must not grow/heal twice");
        }
        void Pending(CafeSimulation sim)
        {
            if(sim.IntakeWaiting)sim.Execute("resolve_intake",choice:"decline");
            foreach(var g in sim.State.growth.ToArray())if(sim.GrowthPendingKind(g)=="resolve_growth")sim.Execute("resolve_growth",catId:g.cat_id,choice:"rest");
        }
        // Ordinary rest experience reaches the first stage after exactly twenty days.
        var rest=New();
        for(int day=1;day<=21;day++)
        {
            Pending(rest);Roundtrip(rest);rest.Execute("day_off");Roundtrip(rest);
            Assert(rest.State.cats.All(c=>rest.MaxStamina(c.id)==(day<20?100:110)),"Rest stage boundary");
        }
        Assert(rest.State.growth.All(g=>g.specialization=="rest"),"Base growth independent of specialization");
        // Unit boundary: several stages, partial stamina, exhaustion flag, cap, and no-op repetition.
        var unit=New();var cat=unit.State.cats.First();cat.stamina=7;cat.cannot_continue=true;
        var record=typeof(CafeSimulation).GetMethod("RecordGrowth",BindingFlags.NonPublic|BindingFlags.Instance);
        unit.Rules.growth.rest_xp=60;
        record.Invoke(unit,new object[]{new CafeGrowthOperation {kind="rest",cat_id=cat.id,day=1}});
        Assert(unit.MaxStamina(cat.id)==130 && cat.stamina==37 && !cat.cannot_continue,"Apply only maximum delta to current stamina");
        unit.Rules.growth.rest_xp=1000;record.Invoke(unit,new object[]{new CafeGrowthOperation {kind="rest",cat_id=cat.id,day=1}});
        Assert(unit.MaxStamina(cat.id)==300 && cat.stamina==207,"Cap at 300, preserve deficit");
        record.Invoke(unit,new object[]{new CafeGrowthOperation {kind="rest",cat_id=cat.id,day=1}});
        Assert(cat.stamina==207 && unit.State.cats.Skip(1).All(c=>unit.MaxStamina(c.id)==100),"No repeated heal at cap and per-cat independence");
        var e=new CafeExchange {cat_id=cat.id,remaining=20,max_stamina=300,stamina=299};unit.StepExchange(e,unit.Definition(cat.id),"pause");Assert(e.stamina==300,"Pause clamps to cat maximum");
        // Service growth, captured history, disk resume, and independently executed replay.
        var service=New();service.Rules.growth.service_xp=20;
        var journal=new CafeReplayJournal(service.Rules,service.State,ReplayEncode,ReplayDecode);
        void Run(CafeReplayOperation op){var before=journal.CaptureBefore(service.State);CafeReplayJournal.Apply(service,op);journal.Append(before,service,op);}
        Run(new CafeReplayOperation {kind="start_business"});bool checkedService=false;
        while(service.State.phase=="open")
        {
            foreach(var g in service.State.growth.Where(g=>service.GrowthPendingKind(g)=="resolve_growth").ToArray())Run(new CafeReplayOperation {kind="resolve_growth",cat_id=g.cat_id,choice="rest"});
            Run(new CafeReplayOperation {kind="advance_business"});Roundtrip(service);
            if(!checkedService && service.State.exchanges.Count>0){var corrupt=Decode<CafeSnapshot>(Encode(service.State));corrupt.exchanges[0].max_stamina=999;bool refused=false;try{service.Restore(corrupt);}catch(ArgumentException){refused=true;}Assert(refused,"Reject changed service recovery ceiling");checkedService=true;}
        }
        Assert(service.State.cats.Any(c=>service.MaxStamina(c.id)>100),"Service experience grows stamina");
        foreach(var g in service.State.growth.Where(g=>service.GrowthPendingKind(g)=="resolve_growth").ToArray())Run(new CafeReplayOperation {kind="resolve_growth",cat_id=g.cat_id,choice="rest"});
        Run(new CafeReplayOperation {kind="next_day"});
        cat=service.State.cats.OrderByDescending(c=>service.MaxStamina(c.id)).First();Assert(cat.stamina==service.MaxStamina(cat.id),"Next day fully restores individual maximum");
        Run(new CafeReplayOperation {kind="player_begin",cat_id=cat.id});
        for(int i=0;i<4;i++)Run(new CafeReplayOperation {kind="player_step",choice="pause"});Roundtrip(service);
        Run(new CafeReplayOperation {kind="player_finish"});
        var session=service.State.player_sessions.Last();string history=service.PlayerHistory(session);double captured=session.max_stamina;
        Assert(captured==service.MaxStamina(cat.id) && session.stamina_rule_version==1,"Session captures maximum and rule version");
        Assert(CafeReplayJournal.Digest(journal.Verify(Decode<CafeReplayDocument>(Encode(journal.Document))).State)==CafeReplayJournal.Digest(service.State),"Grown service/player replay");
        File.WriteAllText(Path.Combine(output,"replay.json"),Encode(journal.Document));
        for(int i=0;i<20;i++){Pending(service);service.Execute("day_off");}Assert(service.MaxStamina(cat.id)>captured,"Later growth fixture");Assert(service.PlayerHistory(session)==history && session.max_stamina==captured,"Past history never uses later maximum");Roundtrip(service);
        var bad=Decode<CafeSnapshot>(Encode(service.State));bad.cats[0].stamina_stage++;var beforeBad=CafeReplayJournal.Digest(service.State);bool rejected=false;
        try{service.Restore(bad);}catch(ArgumentException){rejected=true;}Assert(rejected && beforeBad==CafeReplayJournal.Digest(service.State),"Reject duplicate/tampered stage atomically");
        // Legacy saves stay at 100 and accumulate only subsequent experience.
        var legacy=New();legacy.State.stamina_growth=null;legacy.LegacyStaminaReplay=true;legacy.LegacyPlayerReplay=true;
        legacy.Execute("player_begin",catId:"cat-tama");for(int i=0;i<4;i++)legacy.Execute("player_step",choice:"pause");Roundtrip(legacy);
        Assert(legacy.ActivePlayer.exchange.remaining==6 && legacy.ActivePlayer.max_stamina==0,"Actual legacy ten-turn intermediate save");
        legacy.LegacyPlayerReplay=false;while(legacy.PlayerActive)legacy.Execute("player_step",choice:"pause");
        legacy.Execute("player_begin",catId:"cat-sora");Assert(legacy.ActivePlayer.exchange.remaining==20,"Next legacy-save session uses twenty turns");legacy.Execute("player_finish");
        for(int i=0;i<21;i++){Pending(legacy);legacy.Execute("day_off");}Roundtrip(legacy);Assert(legacy.MaxStamina("cat-tama")==100,"Legacy maximum remains 100");
        CafeLegacyImport.DecodeBase=text=>Decode<CafeSnapshot>(text);string importedBase=Encode(legacy.State);
        legacy.State.import_history=new CafeImportHistory {base_json=importedBase,base_digest=CafeLegacyImport.GameDigest(legacy.State),source_hash=new string('b',64)};
        legacy.LegacyStaminaReplay=false;
        for(int i=0;i<20;i++){Pending(legacy);legacy.Execute("day_off");Roundtrip(legacy);Assert(legacy.MaxStamina("cat-tama")== (i<19?100:110),"No retroactive legacy growth");}
        var custom=Decode<CafeRules>(Encode(rules));custom.stamina_growth=CafeStaminaRules.Provisional();custom.stamina_growth.experience_per_stage=1;
        var accelerated=new CafeSimulation(custom);for(int i=0;i<21;i++){Pending(accelerated);accelerated.Execute("day_off");Roundtrip(accelerated);}
        Assert(accelerated.State.cats.All(c=>accelerated.MaxStamina(c.id)==300 && c.stamina==300),"Configured pace reaches cap and saves");
        custom.stamina_growth.cap=110;Assert(accelerated.MaxStamina("cat-tama")==300,"Saved settings independent from later defaults");
        foreach(string specialization in new[]{"service","rest","dispatch"})
        {var probe=New();var pc=probe.State.cats.First();probe.GrowthCat(pc.id).specialization=specialization;probe.Rules.growth.dispatch_xp=20;record.Invoke(probe,new object[]{new CafeGrowthOperation {kind="dispatch",cat_id=pc.id,day=1}});Assert(probe.MaxStamina(pc.id)==110,"Dispatch/base growth independent of "+specialization);}
        var daily=new CafeSaveEnvelope {state=accelerated.State,daily_records=new[]{new CafeDailyRecord {day=accelerated.State.day-1,cat_id="cat-tama",day_type="営業",shift="出勤",count="0",ticks="0",spent="250",fatigue="0 → 0",health="健康維持 · 健康",xp="接客＋0・休養＋0・派遣＋0",affinity="0"}}};
        CafeDailyRecords.Validate(ReplayDecode(ReplayEncode(daily)));Assert(true,"Daily metadata above 100 survives serialization");daily.daily_records[0].spent="301";rejected=false;try{CafeDailyRecords.Validate(daily);}catch(ArgumentException){rejected=true;}Assert(rejected,"Daily metadata rejects beyond individual maximum");
        File.WriteAllText(Path.Combine(output,"resume-rules.json"),Encode(accelerated.Rules));File.WriteAllText(Path.Combine(output,"resume-digest.txt"),CafeReplayJournal.Digest(accelerated.State));
        legacy.Rules.stamina_growth=CafeStaminaRules.Provisional();legacy.Rules.stamina_growth.increment=30;Roundtrip(legacy);Assert(legacy.MaxStamina("cat-tama")==110,"Imported replay pins original growth settings after tuning");Roundtrip(accelerated);
        Console.WriteLine($"PASS stamina: {assertions} assertions");
    }
}
