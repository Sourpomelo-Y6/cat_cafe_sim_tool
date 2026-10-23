using CatCafePreview.Standalone;
static partial class Program
{
    static CafeAutoOperation BalanceOp(CafeSimulation sim,string target,string mode,bool manual)
    {
        var s=sim.State;
        CafeAutoOperation Op(string k,string c=null,string ch=null,string src=null)=>new(){kind=k,cat_id=c,choice=ch,source=src};
        if(sim.GrowthWaiting){var g=s.growth.FirstOrDefault(x=>x.cat_id=="cat-sora" && sim.GrowthPendingKind(x)=="resolve_growth" && sim.GrowthSelection(x)?.choice!="service");if(manual && g!=null)return Op("resolve_growth_stage",g.cat_id,"service");if(s.growth.Where(x=>sim.GrowthPendingKind(x)!=null).Any(x=>sim.GrowthSelection(x)==null))return Op("resolve_growth_auto");return Op("resolve_growth_batch");}
        if(sim.DispatchChoiceWaiting){var e=s.dispatches.First(x=>x.encounter?.status=="waiting");return Op("resolve_dispatch_choice",ch:sim.DispatchEncounterDefinition(e.destination,e.started_day).choices[0].id,src:e.id);}
        if(sim.DispatchTroubleWaiting)return Op("resolve_dispatch_trouble",ch:"wait",src:s.dispatches.First(x=>x.trouble?.status=="waiting").id);
        if(sim.DispatchIntroductionWaiting)return Op("resolve_dispatch_introduction",ch:"decline",src:s.dispatches.First(x=>x.introduction?.status=="waiting").id);
        if(sim.DispatchWaiting)return Op("resolve_activity",ch:"receive",src:s.dispatches.First(x=>x.status=="waiting").id);
        if(s.cats.Any(c=>c.activity=="missing" && c.missing_remaining==0))return Op("resolve_missing",s.cats.First(c=>c.activity=="missing" && c.missing_remaining==0).id);
        if(sim.CustomerTrustWaiting)return Op("resolve_customer_trust",ch:"recover",src:s.trust_events.First(e=>e.status=="waiting").id);
        if(sim.StoreEventWaiting)return Op("resolve_store_event",ch:sim.CurrentStoreEvent.type=="trouble"?"patch":"decline");
        if(sim.IntakeWaiting)return Op("resolve_intake",ch:sim.IntakeAcceptReason=="" && s.funds>sim.Rules.intake.cost+2000?"accept":"decline");
        if(sim.RegularIntroductionWaiting)return Op("resolve_regular_introduction",ch:sim.RegularIntroductionAcceptReason=="" && s.funds>sim.Rules.regular_introduction.cost+2000?"accept":"decline");
        if(sim.AdoptionWaiting)return Op("resolve_adoption",ch:"decline",src:s.adoption.events.First(e=>e.status=="waiting").id);
        if(sim.ReservationWaiting)return Op("resolve_reservation",ch:"decline");
        if(sim.BondWaiting)return Op("continue_bond_goal");
        if(sim.PatronWaiting)return Op("continue_patron");
        if(sim.GoalWaiting)return Op(s.goal.Last().status=="cleared" && s.goal.Count<sim.Rules.goal_stages.Length?"advance_goal":"continue_goal");
        if(target=="service-after-player" && sim.PlayerActive)return Op("player_step",ch:sim.ActivePlayer.exchange.connect_pending?"connect":sim.ActivePlayer.exchange.stamina<=20?"pause":"direct");
        if(target=="service-after-player" && s.phase=="preparation" && sim.PlayerRemaining>0){var c=s.cats.OrderBy(x=>x.id,StringComparer.Ordinal).FirstOrDefault(c=>sim.PlayerCat(c.id).today==0 && sim.PlayerBeginReason(c.id)=="");if(c!=null)return Op("player_begin",c.id);}
        return sim.PlanAutoplay(target.StartsWith("service-")?"popularity":target,mode);
    }
    static void BalanceExecute(CafeSimulation sim,CafeAutoOperation o)=>sim.Execute(o.kind,o.working,o.cat_id,o.choice,o.source,o.seat_id);
    static void LongBalance(string rulesPath,string output,bool manual=false,bool probe=false)
    {
        Directory.CreateDirectory(output);var results=new List<object>();
        foreach(var target in probe?new[]{"service-control","service-after-player"}:manual?new[]{"patron"}:new[]{"popularity","bond","patron"})foreach(var mode in probe?new[]{"clear"}:new[]{"clear","fast"})foreach(int seed in new[]{0,1,42})
        {
            string id=$"{target}-{mode}-{seed}-"+(manual?"manual":"auto"),dir=Path.Combine(output,id);Directory.CreateDirectory(dir);
            var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));rules.stamina_growth=Decode<CafeStaminaRules>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(rulesPath),"stamina_growth.json")));
            var sim=new CafeSimulation(rules,seed,probe?"free":target);int n=0,lastClosed=0;string failure=null;bool resume=false;var counts=new Dictionary<string,int>();
            using var daily=new StreamWriter(Path.Combine(dir,"daily.jsonl"));
            using var openings=new StreamWriter(Path.Combine(dir,"openings.jsonl"));
            try{while(sim.State.day<=120 && sim.State.game_over==null && n++<100000)
            {
                var s=sim.State;
                if(s.phase=="closed" && lastClosed!=s.day){lastClosed=s.day;daily.WriteLine(Encode(new{day=s.day,funds=s.funds,revenue=s.revenue,popularity=s.popularity,cats=s.cats.Select(c=>new{c.id,c.stamina,max=sim.MaxStamina(c.id),c.fatigue,c.stress,c.health,c.activity}),patron=s.patron}));daily.Flush();}
                if(s.day==60 && !resume)
                {
                    var store=new CafeSaveStore(dir,ReplayEncode,ReplayDecode,sim.Validate);
                    store.Save(1,s);File.WriteAllText(Path.Combine(dir,"checkpoint-digest.txt"),CafeReplayJournal.Digest(s));
                    var twin=new CafeSimulation(rules);twin.Restore(store.Load(1).state);
                    Assert(CafeReplayJournal.Digest(s)==CafeReplayJournal.Digest(twin.State),"Midpoint restore digest "+id);
                    var op=BalanceOp(sim,target,mode,manual);
                    File.WriteAllText(Path.Combine(dir,"checkpoint-next.json"),Encode(op));
                    counts.TryGetValue(op.kind,out int checkpointCount);counts[op.kind]=checkpointCount+1;
                    if(op.kind=="start_business")openings.WriteLine(Encode(new{s.day,cats=s.cats.Select(c=>new{c.id,c.working,c.stamina,max=sim.MaxStamina(c.id),c.fatigue,c.stress})}));
                    BalanceExecute(sim,op);BalanceExecute(twin,op);
                    File.WriteAllText(Path.Combine(dir,"checkpoint-next-digest.txt"),CafeReplayJournal.Digest(sim.State));
                    Assert(CafeReplayJournal.Digest(sim.State)==CafeReplayJournal.Digest(twin.State),"Next operation after restore "+id);
                    sim=twin;resume=true;continue;
                }
                var o=BalanceOp(sim,target,mode,manual);counts.TryGetValue(o.kind,out int count);counts[o.kind]=count+1;
                if(o.kind=="start_business")openings.WriteLine(Encode(new{s.day,cats=s.cats.Select(c=>new{c.id,c.working,c.stamina,max=sim.MaxStamina(c.id),c.fatigue,c.stress})}));
                BalanceExecute(sim,o);
            }sim.Validate(sim.State);}catch(Exception ex){failure=ex.ToString();}
            var state=sim.State;File.WriteAllText(Path.Combine(dir,"final.json"),Encode(state));
            var services=state.history.SelectMany(h=>h.customer_records).Where(v=>v.cat_id!=null && v.satisfaction!=null).GroupBy(v=>v.cat_id).ToDictionary(g=>g.Key,g=>g.Count());
            var row=new{id,target,mode,seed,manual,day=state.day,steps=n,failure,resume,game_over=state.game_over,goal=state.goal,bond=state.bond_goal,patron=state.patron,funds=state.funds,revenue=state.history.Sum(h=>h.revenue),services,counts,dispatches=state.dispatches.Count,dispatch_reward=state.dispatches.Where(d=>d.status=="resolved").Sum(sim.DispatchReceivedReward),cats=state.cats.Select(c=>new{c.id,c.stamina,max=sim.MaxStamina(c.id),specialization=state.growth.Single(g=>g.cat_id==c.id).specialization}),equipment=state.equipment_purchases.Count,housing=state.housing,seats=state.seat_count};results.Add(row);File.WriteAllText(Path.Combine(output,"summary.json"),Encode(results));Console.WriteLine(id+" day="+state.day+" funds="+state.funds+" failure="+failure);
        }
    }
    static void LongBalanceResume(string rulesPath,string output)
    {
        foreach(string dir in Directory.GetDirectories(output).Where(d=>File.Exists(Path.Combine(d,"slot-1.json")))){var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));var sim=new CafeSimulation(rules);var store=new CafeSaveStore(dir,ReplayEncode,ReplayDecode,sim.Validate);sim.Restore(store.Load(1).state);Assert(sim.State.day==60,"Separate process midpoint day");Assert(CafeReplayJournal.Digest(sim.State)==File.ReadAllText(Path.Combine(dir,"checkpoint-digest.txt")),"Separate process state digest");BalanceExecute(sim,Decode<CafeAutoOperation>(File.ReadAllText(Path.Combine(dir,"checkpoint-next.json"))));Assert(CafeReplayJournal.Digest(sim.State)==File.ReadAllText(Path.Combine(dir,"checkpoint-next-digest.txt")),"Separate process next operation digest");Console.WriteLine("PASS resume "+Path.GetFileName(dir));}
    }
}
