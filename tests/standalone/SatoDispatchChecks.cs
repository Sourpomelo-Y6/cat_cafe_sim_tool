using CatCafePreview.Standalone;

static partial class Program
{
    static void NamedDispatchTextChecks(CafeNamedDispatchSettings[] settings,CafeNamedDispatchSettings customer)
    {
        const string cat="タマ";
        var baseline=CafeNamedDispatchDialogue.Request(null,customer.customer_id,cat,customer.name,true,false,false);
        var lines=CafeNamedDispatchDialogue.Request(settings,customer.customer_id,cat,customer.name,true,false,false);
        var request = customer.scenes.Single(scene=>scene.id=="request");
        for (int i=0; i<lines.Length; i++)
        {
            var expected = string.IsNullOrWhiteSpace(request.lines[i]) ? baseline[i].text
                : request.lines[i].Replace("{cat}",cat).Replace("{customer}",customer.name);
            Assert(lines[i].text==expected,"Request override or default differs from its setting");
        }
        var reminder=CafeNamedDispatchDialogue.Request(settings,customer.customer_id,cat,customer.name,true,true,false);
        Assert(reminder[0].text==CafeNamedDispatchDialogue.Request(null,customer.customer_id,cat,customer.name,true,true,false)[0].text,
            "Request override spilled into reminder");
        Assert(CafeNamedDispatchDialogue.Epilogue(settings,customer.customer_id,cat,customer.name)[1].text.Contains(cat),
            "Epilogue did not use actual adopted cat");
    }

    static void SatoDispatchChecks(string rulesPath,string bundlePath=null,string checkpointDirectory=null)
    {
        RepeatedSuspensionChecks(rulesPath);
        var json=bundlePath==null?null:System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(bundlePath));
        var configured=json==null?null:Decode<CafeNamedDispatchSettings[]>(json["named_dispatch"].ToJsonString());
        var additional=configured?.Where(r=>r.enabled && !CafeSimulation.NamedDispatchCustomers.Contains(r.customer_id)).ToArray()
            ?? Array.Empty<CafeNamedDispatchSettings>();
        foreach(var guest in additional)
        {
            NamedDispatchTextChecks(configured,guest);
            var disabledRules=Decode<CafeRules>(File.ReadAllText(rulesPath));
            disabledRules.named_dispatch=new[]{
                new CafeNamedDispatchSettings {customer_id=guest.customer_id,name=guest.name,scenes=guest.scenes},
                new CafeNamedDispatchSettings {customer_id=CafeSimulation.Sato,name="佐藤さん",scenes=Array.Empty<CafeNamedDispatchScene>()}};
            var disabled=new CafeSimulation(disabledRules);
            Assert(disabled.NamedCustomers.Contains(CafeSimulation.Suzuki) &&
                !disabled.EnabledNamedCustomers.Contains(CafeSimulation.Sato) &&
                !disabled.EnabledNamedCustomers.Contains(guest.customer_id),"Explicit disabled settings were ignored");
        }
        int initialCount=Decode<CafeRules>(File.ReadAllText(rulesPath)).arrival_ticks.Length;
        var initialIds=Enumerable.Range(1,initialCount).Select(i=>"guest-"+i).ToHashSet();
        var customers=CafeSimulation.NamedDispatchCustomers.Concat(additional.Where(r=>initialIds.Contains(r.customer_id))
            .Select(r=>r.customer_id)).ToArray();
        foreach (var customer in customers)
        foreach (var route in new[]{"normal","decline","expired","refused","adopt","decline-adoption","ban","low-score","legacy-return"})
        {
            var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));
            rules.starting_funds=100000;
            if(route=="low-score")rules.satisfaction.satisfied_score=1;
            // Force suspension in one missed visit for the refusal fixture.
            if(route=="refused")rules.discontent.waiting_gain=100;
            rules.store_events.probability=0;
            var setting=additional.SingleOrDefault(r=>r.customer_id==customer);
            if(setting!=null)rules.named_dispatch=new[]{setting};
            int slot=Array.IndexOf(customers,customer);
            if(slot>0)
            {
                // Give the customer under test the standard fixture's arrival slot and calendar.
                (rules.arrival_ticks[0],rules.arrival_ticks[slot])=(rules.arrival_ticks[slot],rules.arrival_ticks[0]);
                int pattern=slot % rules.patterns.Length;
                (rules.patterns[0],rules.patterns[pattern])=(rules.patterns[pattern],rules.patterns[0]);
            }
            var sim=new CafeSimulation(rules);
            var catId=sim.State.cats[0].id;

            void RoundTrip()
            {
                string encoded=Encode(sim.State);
                if(customer==CafeSimulation.Sato)
                {
                    // Existing Sato saves predate both Suzuki and configurable customers.
                    var legacy=System.Text.Json.Nodes.JsonNode.Parse(encoded).AsObject();
                    legacy.Remove("suzuki_dispatch");
                    legacy.Remove("suzuki_affinity");
                    legacy.Remove("named_dispatch");
                    encoded=legacy.ToJsonString();
                }
                var copy=Decode<CafeSnapshot>(encoded);
                copy.NormalizeJsonStrings();
                sim.Restore(copy);
                if(setting!=null)
                {
                    var withoutPack=Decode<CafeRules>(Encode(rules));
                    withoutPack.named_dispatch=null;
                    new CafeSimulation(withoutPack).Restore(copy);
                }
            }

            void Prepare()
            {
                if(sim.IntakeWaiting) sim.Execute("resolve_intake",choice:"decline");
                if(sim.RegularIntroductionWaiting) sim.Execute("resolve_regular_introduction",choice:"decline");
            }

            void Day(bool serve)
            {
                Prepare();
                if (sim.StoreEventWaiting) sim.Execute("resolve_store_event",choice:"repair");
                if (!serve)
                {
                    sim.Execute("day_off");
                    if (sim.State.phase=="closed")
                    {
                        if (sim.GrowthWaiting)
                        {
                            sim.Execute("resolve_growth_auto");
                            sim.Execute("resolve_growth_batch");
                        }
                        sim.Execute("next_day");
                    }
                    return;
                }
                var cat=sim.State.cats.Single(c=>c.id==catId);
                if(cat.activity=="missing" && cat.missing_remaining==0) sim.Execute("resolve_missing",catId:catId);
                if(cat.activity!="cafe" || cat.health!="healthy") { sim.Execute("day_off"); return; }
                sim.Execute("set_shifts",working:new[]{catId});
                sim.Execute("start_business");
                while(sim.State.phase=="open") sim.Execute("advance_business");
                sim.Execute("next_day");
            }

            Prepare();
            sim.Execute("set_shifts",working:new[]{catId});
            sim.Execute("start_business");
            while(sim.State.phase=="open")
            {
                var exchange=sim.State.exchanges.SingleOrDefault(e=>e.customer_id==customer);
                if(exchange!=null)exchange.affinity_pending=100;
                sim.Execute("advance_business");
            }
            sim.Execute("next_day");
            Assert(sim.NamedAffinity(customer).Count>0,"Sato affinity scene was not recorded after service");
            while(sim.NamedRequest(customer)==null && sim.State.day<65)
            {
                var cat=sim.State.cats.Single(c=>c.id==catId);
                if(cat.activity!="cafe" || cat.health!="healthy") {Day(false);continue;}
                Prepare();
                // A working cat with no available stamina leaves arrivals in the queue.
                sim.Execute("set_shifts",working:new[]{catId});
                cat.stamina=0;
                cat.cannot_continue=true;
                sim.Execute("start_business");
                while(sim.State.phase=="open") sim.Execute("advance_business");
                sim.Execute("next_day");
            }

            Prepare();
            Assert(sim.NamedRequest(customer)!=null,"Sato request was not issued");
            Assert(sim.DiscontentScore(customer,sim.State.day)==(route=="refused"?100:50),"Wrong score at request");
            sim.NamedRequest(customer).conversation.node=1;
            RoundTrip();
            Assert(sim.NamedRequest(customer).conversation.node==1,"Request conversation position was lost");
            if (route=="decline")
            {
                sim.Execute("resolve_sato_dispatch",catId:customer,choice:"decline");
                RoundTrip();
                Assert(sim.NamedRequest(customer).declined_day==sim.State.day,"Decline was not recorded");
                continue;
            }
            if (route!="normal" && route!="refused")
            {
                Day(false);
                Assert(sim.NamedRequest(customer).conversation.node==0,"Reminder did not reset the scene to its first page");
                Day(false);
            }
            else
            {
                if (route=="refused") Day(false);
                sim.Execute("resolve_sato_dispatch",catId:customer,choice:"accept");
                Day(false);
                var trip=sim.State.dispatches.Single(e=>CafeSimulation.NamedDestinationCustomer(e.destination)==customer);
                sim.Execute("resolve_activity",source:trip.id,choice:"receive");
                if (route=="normal")
                {
                    RoundTrip();
                    Assert(sim.DiscontentScore(customer,sim.State.day)==25,"Return did not reduce dissatisfaction");
                    continue;
                }
            }

            var missing=sim.State.missing_cases.Single(e=>CafeSimulation.NamedMissingCustomer(e.reason)==customer);
            Assert(missing.resolved_day==0,"Missing case did not open");
            Assert(sim.State.cats.Single(c=>c.id==catId).activity=="missing","Cat remained available while missing");
            RoundTrip();
            var missingCat=sim.State.cats.Single(c=>c.id==missing.cat_id);
            while (missingCat.missing_remaining>0) Day(false);
            if(route=="legacy-return")
            {
                var journal=new CafeReplayJournal(rules,sim.State,ReplayEncode,ReplayDecode);
                var before=journal.CaptureBefore(sim.State);
                var op=new CafeReplayOperation {kind="resolve_missing",cat_id=missing.cat_id,growth_policy_version=1};
                CafeReplayJournal.Apply(sim,op);
                journal.Append(before,sim,op);
                var recovered=journal.Verify(journal.Document);
                Assert(!recovered.NamedReturnAccessWaiting,"Legacy replay acquired a new pending choice");
                RoundTrip();
                Day(false);
                Assert(!sim.NamedReturnAccessWaiting,"Legacy return acquired a new pending choice");
                continue;
            }
            sim.Execute("resolve_missing",catId:missing.cat_id);
            RoundTrip();
            Assert(sim.NamedRequest(customer).returned_day==sim.State.day,"Missing cat return was not recorded");
            Assert(sim.NamedReturnAccessPending(customer),"Return choice did not persist");
            int returnedDay=sim.State.day;
            string access=route=="ban"?"ban":"allow";
            sim.Execute("resolve_named_return_access",catId:customer,choice:access);
            RoundTrip();
            Assert(!sim.NamedReturnAccessPending(customer),"Return answer did not persist");
            if(route=="ban")
            {
                sim.Execute("configure_adoption",choice:"on");
                for(int i=0;i<9;i++)Day(false);
                Assert(Enumerable.Range(returnedDay,14).All(day=>!sim.CustomerAvailable(customer,day)
                    && sim.CustomerSchedule(day).All(row=>row.customer_id!=customer)),"Banned customer returned");
                Assert(sim.State.adoption.events.Count==0 && !sim.State.trust_events.Any(e=>e.customer_id==customer)
                    && sim.NamedRequest(customer).requested_day<returnedDay,"Banned customer received new event");
                RoundTrip();
                continue;
            }
            bool adoption=route=="adopt" || route=="decline-adoption";
            if(adoption)sim.Execute("configure_adoption",choice:"on");
            Day(false);
            var offer=sim.State.adoption?.events.SingleOrDefault(e=>CafeSimulation.IsNamedAdoption(e.source) && e.customer_id==customer);
            Assert((offer!=null)==adoption,"Unexpected return adoption eligibility");
            if(!adoption)
            {
                if(route=="low-score")
                {
                    while(sim.State.day<returnedDay+7 && sim.DiscontentScore(customer,sim.State.day)>=50)
                        Day(true);
                    Assert(sim.DiscontentScore(customer,sim.State.day)<50,"Low-score route did not improve");
                }
                while(sim.State.day<returnedDay+7)Day(false);
                Assert(sim.NamedRequest(customer).requested_day<returnedDay,"Reissued before day eight");
                RoundTrip();
                Day(false);
                Assert(!sim.CustomerTrustWaiting && sim.State.trust_events.Count==0,"Return generated a trust conversation");
                Assert(sim.NamedDispatchAwaitingAnswer(customer)==(route!="low-score"),"Wrong reissue on day eight");
                RoundTrip();
                if(route=="expired")
                {
                    Assert(sim.State.named_dispatch_archive.Single().request.returned_day==returnedDay,"Previous return was lost");
                    Day(false);
                    Day(false);
                    var secondMissing=sim.State.missing_cases.Single(e=>e.resolved_day==0);
                    Assert(secondMissing.departed_day==returnedDay+10,"Reissued request did not expire normally");
                    while(sim.State.cats.Single(c=>c.id==secondMissing.cat_id).missing_remaining>0)Day(false);
                    sim.Execute("resolve_missing",catId:secondMissing.cat_id);
                    sim.Execute("resolve_named_return_access",catId:customer,choice:"allow");
                    RoundTrip();
                    int secondReturn=sim.State.day;
                    while(sim.State.day<secondReturn+8)Day(false);
                    Assert(sim.NamedDispatchAwaitingAnswer(customer) && sim.State.named_dispatch_archive.Count==2,
                        "Second return did not reissue the same event");
                    RoundTrip();
                }
                if(route=="expired" || route=="refused")
                {
                    sim.Execute("resolve_sato_dispatch",catId:customer,choice:"accept");
                    Day(false);
                    var dispatch=sim.State.dispatches.Last();
                    sim.Execute("resolve_activity",source:dispatch.id,choice:"receive");
                    RoundTrip();
                    Assert(sim.NamedRequest(customer).returned_day==sim.State.day &&
                        sim.DiscontentScore(customer,sim.State.day)==25,"Reissued dispatch did not return normally");
                }
                continue;
            }
            RoundTrip();
            bool accept=route=="adopt";
            sim.Execute("resolve_adoption",source:offer.id,choice:accept?"accept":"decline");
            RoundTrip();
            Assert(sim.State.cats.Single(c=>c.id==catId).activity==(accept?"adopted":"cafe"),"Incorrect adoption outcome");
            for(int day=0;day<3;day++)
            {
                Assert(!sim.SatoEpilogueAvailable(catId),"Epilogue unlocked early");
                Day(false);
            }
            RoundTrip();
            Assert(sim.SatoEpilogueAvailable(catId)==accept,"Incorrect epilogue unlock");
            if(accept)
            {
                sim.NamedRequest(customer).epilogue_read=true;
                RoundTrip();
                Assert(sim.NamedRequest(customer).epilogue_read && sim.SatoEpilogueAvailable(catId),"Read epilogue cannot be revisited");
            }
        }
        var conflictRules=Decode<CafeRules>(File.ReadAllText(rulesPath));
        conflictRules.named_dispatch=configured;
        var conflict=new CafeSimulation(conflictRules);
        string sharedCat=conflict.State.cats[0].id;
        foreach(var customer in customers)
        {
            conflict.State.relationships.Add(new CafeRelationship {cat_id=sharedCat,customer_id=customer,affinity=90});
            // Use the same affinity hook as normal service, including the enabled check.
            typeof(CafeSimulation).GetMethod("ObserveSatoAffinity",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(conflict,new object[]{sharedCat,customer,90.0});
            conflict.ChangeDiscontent(conflict.State.discontent,conflict.State.day,customer,50);
        }
        foreach(var customer in customers)
        {
            if(conflict.IntakeWaiting)conflict.Execute("resolve_intake",choice:"decline");
            if(conflict.StoreEventWaiting)conflict.Execute("resolve_store_event",choice:"patch");
            conflict.Execute("day_off");
            Assert(conflict.NamedCustomers.Where(conflict.NamedDispatchAwaitingAnswer).SequenceEqual(new[]{customer})
                && conflict.NamedRequest(customer)?.cat_id==sharedCat,"Wrong priority or overlapping requests for "+customer);
            conflict.Execute("resolve_sato_dispatch",catId:customer,choice:"decline");
        }
        Assert(customers.All(c=>conflict.NamedRequest(c).declined_day>0),"Later request overwrote another customer's record");
        if (customers.Length==6) InitialGuestDispatchFrequency(rulesPath,configured,customers);
        foreach(var guest in additional.Where(r=>!initialIds.Contains(r.customer_id)))
        {
            if(guest.customer_id.StartsWith("expanded-guest-",StringComparison.Ordinal))
                ExpandedCapacityDispatchChecks(rulesPath,configured,guest.customer_id,checkpointDirectory);
            else
                GoalUnlockedDispatchChecks(rulesPath,configured,guest.customer_id,checkpointDirectory);
        }
        if(additional.Any(r=>r.customer_id=="guest-9") && additional.Any(r=>r.customer_id=="guest-11"))
            GoalUnlockedRequestsCompete(rulesPath,configured);
        Console.WriteLine($"PASS named dispatch: {assertions} assertions");
    }

    static void InitialGuestDispatchFrequency(string rulesPath,CafeNamedDispatchSettings[] configured,string[] customers)
    {
        foreach(var mode in new[]{"clear","basic"})
        foreach(int seed in new[]{0,1,42})
        {
            var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));
            rules.stamina_growth=Decode<CafeStaminaRules>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(rulesPath),"stamina_growth.json")));
            rules.named_dispatch=configured;
            var sim=new CafeSimulation(rules,seed);
            int operations=0;
            while(sim.State.day<=60 && sim.State.game_over==null && operations++<50000)
            {
                var customer=customers.FirstOrDefault(sim.NamedDispatchAwaitingAnswer);
                if(customer!=null && sim.State.phase=="preparation")
                    sim.Execute("resolve_sato_dispatch",catId:customer,choice:"decline");
                else
                    BalanceExecute(sim,BalanceOp(sim,"popularity",mode,false));
            }
            Assert(sim.State.day==61,"Frequency run stopped before 60 days: "+mode+"/"+seed);
            sim.Validate(sim.State);
            string requested=string.Join(", ",customers.Where(c=>sim.NamedRequest(c)!=null));
            Console.WriteLine($"60-day {mode}/{seed}: {(requested.Length==0?"no requests":requested)}");
        }
    }

    static void GoalUnlockedDispatchChecks(string rulesPath,CafeNamedDispatchSettings[] settings,string customer,string checkpointDirectory)
    {
        var preferredSeat=typeof(CafeSimulation).GetMethod("PreferredSeat",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        bool checkpointsWritten=false, receivedRequest=false;
        bool expanded=customer=="expanded-guest";
        bool requiresFixture=expanded || customer is "guest-7" or "guest-8" or "guest-11" or "guest-12";
        // Kato's two natural requests were identified in the remaining-guest comparison.
        int[] seeds=customer=="guest-10"?new[]{0,5}:new[]{0,1,42};
        foreach(int seed in seeds)
        {
            var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));
            rules.stamina_growth=Decode<CafeStaminaRules>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(rulesPath),"stamina_growth.json")));
            rules.named_dispatch=settings;
            int stage=!expanded && int.Parse(customer.Substring("guest-".Length))>rules.arrival_ticks.Length+rules.popular_customer_count?2:1;
            string mode=customer is "guest-8" or "guest-11"?"fast":"clear";
            var sim=new CafeSimulation(rules,seed,"popularity");
            string checkpoints=checkpointDirectory!=null && !checkpointsWritten?Path.Combine(checkpointDirectory,customer):null;
            CafeSnapshot affinityCheckpoint=null, unlockCheckpoint=null;
            int UnlockDay()=>expanded?sim.State.expansion.FirstOrDefault(p=>p.seats==4)?.day??0:sim.ClearedGoalDay(stage);
            Assert(!sim.CustomerSchedule(1).Any(a=>a.customer_id==customer),"Guest could visit on the first day");
            var observed=new DispatchBalanceRow();
            int affinityDay=0, operations=0, observedDay=0;
            bool issuedWhileLocked=false;
            while(sim.State.day<=120 && sim.State.game_over==null && operations++<100000)
            {
                var s=sim.State;
                if(s.phase=="preparation" && s.day!=observedDay)
                {
                    observedDay=s.day;
                    observed.Observe(sim,customer,50);
                }
                int unlock=UnlockDay();
                if(unlock==0 || s.day<=unlock)
                    issuedWhileLocked|=sim.CustomerSchedule(s.day).Any(a=>a.customer_id==customer)
                        || sim.NamedRequest(customer)!=null || sim.NamedAffinity(customer).Count>0;
                if(requiresFixture && seed==0 && unlockCheckpoint==null && s.phase=="preparation" && unlock>0 && s.day>unlock)
                    unlockCheckpoint=CloneDispatchSnapshot(s);
                if(s.phase=="preparation" && affinityDay==0 && sim.NamedAffinity(customer).Count>0)
                {
                    affinityDay=s.day;
                    if(checkpoints!=null)affinityCheckpoint=CloneDispatchSnapshot(s);
                }
                var request=sim.NamedCustomers.FirstOrDefault(sim.NamedDispatchAwaitingAnswer);
                if(request!=null && s.phase=="preparation")
                {
                    if(request==customer)
                    {
                        bool reissued=NaturalNamedDispatchContinuation(sim,seed,customer,null,null,preferredSeat,50,checkpoints,mode);
                        if(checkpoints!=null && reissued)
                        {
                            SaveDispatchCheckpoint(sim,checkpoints,"affinity",affinityCheckpoint);
                            SaveDispatchCheckpoint(sim,checkpoints,"request");
                            checkpointsWritten=true;
                        }
                    }
                    sim.Execute("resolve_sato_dispatch",catId:request,choice:"decline");
                }
                else
                    BalanceExecute(sim,BalanceOp(sim,"popularity",mode,false));
            }
            Assert(!issuedWhileLocked,"Locked guest issued an event");
            Assert(sim.State.day==121 && UnlockDay()>0
                && (affinityDay>UnlockDay() || requiresFixture && affinityDay==0),
                "Unlocked guest did not finish the comparison in seed "+seed);
            var requestAfterUnlock=sim.NamedRequest(customer);
            Assert(requestAfterUnlock==null || requestAfterUnlock.requested_day>=affinityDay,
                "Unlocked guest did not receive a request after the affinity scene in seed "+seed);
            if(customer=="guest-10")
                Assert(requestAfterUnlock?.requested_day==100 && requestAfterUnlock.cat_id=="cat-mugi",
                    "Kato's baseline request did not reproduce in seed "+seed);
            receivedRequest|=requestAfterUnlock!=null;
            sim.Validate(sim.State);
            var visits=sim.State.history.SelectMany(h=>h.customer_records).Where(v=>v.customer_id==customer).ToArray();
            if(expanded)
                Assert(sim.State.history[UnlockDay()].customer_records.Any(v=>v.customer_id==customer),
                    "Okada did not arrive on the day after the fourth seat");
            Console.WriteLine(Encode(new{customer,seed,stage,unlocked_after=UnlockDay(),affinity_day=affinityDay,
                visits=visits.Length,services=visits.Count(v=>v.satisfaction!=null),request_day=sim.NamedRequest(customer)?.requested_day,
                observed.max_discontent,observed.overlap_days,observed.staff_block_days}));
            if(requiresFixture && seed==0 && (expanded || requestAfterUnlock==null))
            {
                var shortage=new CafeSimulation(rules);
                shortage.Restore(unlockCheckpoint);
                var fixtureAffinity=ExerciseUnlockedDispatch(shortage,customer,seed);
                Assert(shortage.NamedDispatchAwaitingAnswer(customer),"The unlocked guest fixture did not trigger a request");
                if(expanded)AssertDispatchCapacityBoundary(shortage,customer,4);
                bool reissued=NaturalNamedDispatchContinuation(shortage,seed,customer,null,null,preferredSeat,50,checkpoints,"clear");
                receivedRequest=true;
                if(checkpoints!=null && reissued)
                {
                    SaveDispatchCheckpoint(shortage,checkpoints,"affinity",fixtureAffinity);
                    SaveDispatchCheckpoint(shortage,checkpoints,"request");
                    checkpointsWritten=true;
                }
            }
        }
        Assert(receivedRequest,"No unlocked guest request in the tested seeds: "+customer);
        Assert(checkpointDirectory==null || checkpointsWritten,"No reissue to verify in Windows: "+customer);
    }

    static void ExpandedCapacityDispatchChecks(string rulesPath,CafeNamedDispatchSettings[] settings,string customer,string checkpointDirectory)
    {
        int seats=3+int.Parse(customer.Substring("expanded-guest-".Length));
        var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));
        rules.stamina_growth=Decode<CafeStaminaRules>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(rulesPath),"stamina_growth.json")));
        rules.named_dispatch=settings;
        var sim=new CafeSimulation(rules,0,"popularity");
        int purchaseDay=0, operations=0;
        while(sim.State.day<=90 && sim.State.game_over==null && operations++<50000)
        {
            var s=sim.State;
            if(purchaseDay>0 && s.day>purchaseDay)break;
            var pending=sim.NamedCustomers.FirstOrDefault(sim.NamedDispatchAwaitingAnswer);
            if(pending!=null && s.phase=="preparation")
            {
                sim.Execute("resolve_sato_dispatch",catId:pending,choice:"decline");
                continue;
            }
            if(sim.ItemsReason()=="" && s.funds>=6000)
            {
                if(sim.HousingCapacity(s,s.day)<=seats)
                {
                    sim.Execute(sim.NextHousingKind);
                    continue;
                }
                if(sim.ActiveCatCount(s)<=seats)
                {
                    var recruit=sim.RecruitmentCandidates(s).FirstOrDefault(r=>sim.RecruitReason(r.cat.id)=="");
                    if(recruit!=null)
                    {
                        sim.Execute("recruit_cat",catId:recruit.cat.id);
                        continue;
                    }
                    if(sim.RecruitmentRefreshReason()=="")
                    {
                        sim.Execute("open_recruitment");
                        continue;
                    }
                }
                else if(s.seat_count<seats && sim.ExpansionReason()=="")
                {
                    sim.Execute("expand_seats");
                    if(s.seat_count==seats)
                    {
                        purchaseDay=s.day;
                        Assert(!sim.CustomerSchedule(s.day).Any(a=>a.customer_id==customer),
                            "Expanded guest appeared on the purchase day: "+customer);
                        sim.Execute("day_off");
                    }
                    continue;
                }
            }
            var op=BalanceOp(sim,"popularity","clear",false);
            if(op.kind=="expand_seats")
            {
                var workers=s.cats.Where(c=>c.activity=="cafe" && c.health=="healthy" && c.fatigue<60 && c.stress<60)
                    .Select(c=>c.id).ToArray();
                op=workers.Length==0?new CafeAutoOperation {kind="day_off"}:
                    s.cats.Where(c=>c.working).Select(c=>c.id).SequenceEqual(workers)?new CafeAutoOperation {kind="start_business"}:
                    new CafeAutoOperation {kind="set_shifts",working=workers};
            }
            BalanceExecute(sim,op);
        }
        var state=sim.State;
        Assert(purchaseDay>0 && state.day==purchaseDay+1 && state.seat_count==seats
            && sim.CustomerSchedule(state.day).Any(a=>a.customer_id==customer && a.tick==(seats-4)*6),
            "Expanded guest was not scheduled the day after purchase: "+customer);
        int roster=sim.ActiveCatCount(state), capacity=sim.HousingCapacity(state,state.day);
        Assert(roster>=seats+1 && roster<=capacity,"Could not build the required roster: "+customer);
        Assert(sim.NamedAffinity(customer).Count==0 && sim.NamedRequest(customer)==null,"Expanded guest issued an event before service");
        sim.Validate(state);

        var affinity=ExerciseUnlockedDispatch(sim,customer,0);
        Assert(!sim.State.history.Take(purchaseDay).Any(h=>h.customer_records.Any(v=>v.customer_id==customer)),
            "Expanded guest visited on or before the purchase day: "+customer);
        Assert(sim.State.history[purchaseDay].customer_records.Any(v=>v.customer_id==customer),
            "Expanded guest did not visit on the day after purchase: "+customer);
        Assert(affinity!=null && sim.NamedDispatchAwaitingAnswer(customer),"Expanded guest did not receive a request: "+customer);
        AssertDispatchCapacityBoundary(sim,customer,seats);
        string checkpoints=checkpointDirectory==null?null:Path.Combine(checkpointDirectory,customer);
        bool reissued=NaturalNamedDispatchContinuation(sim,0,customer,null,null,null,50,checkpoints);
        Assert(checkpoints==null || reissued,"No expanded guest reissue to verify in Windows: "+customer);
        if(checkpoints!=null)
        {
            SaveDispatchCheckpoint(sim,checkpoints,"affinity",affinity);
            SaveDispatchCheckpoint(sim,checkpoints,"request");
        }
        Console.WriteLine(Encode(new{customer,seats,purchase_day=purchaseDay,roster,capacity,
            affinity_day=affinity.day,request_day=sim.NamedRequest(customer).requested_day}));
    }

    static void AssertDispatchCapacityBoundary(CafeSimulation fixture,string customer,int seats)
    {
        var sim=new CafeSimulation(fixture.Rules);
        sim.Restore(CloneDispatchSnapshot(fixture.State));
        var s=sim.State;
        var request=sim.NamedRequest(customer);
        var available=s.cats.Where(c=>c.activity=="cafe" && c.health=="healthy" && !c.cannot_continue && c.stamina>0)
            .OrderByDescending(c=>c.id==request.cat_id).ToArray();
        Assert(s.seat_count==seats && available.Length>seats,"Expected at least one more available cat than seats");
        s.named_dispatch.Single(row=>row.customer_id==customer).request=null;
        foreach(var cat in available.Skip(seats))cat.cannot_continue=true;
        var present=typeof(CafeSimulation).GetMethod("PresentNamedDispatch",System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.NonPublic);
        present.Invoke(sim,new object[]{customer});
        Assert(sim.NamedRequest(customer)==null,"The same number of available cats and seats must not permit a dispatch");
        available[seats].cannot_continue=false;
        present.Invoke(sim,new object[]{customer});
        Assert(sim.NamedDispatchAwaitingAnswer(customer),"One extra available cat should permit the request");
    }

    static CafeSnapshot CloneDispatchSnapshot(CafeSnapshot state)
    {
        var copy=Decode<CafeSnapshot>(Encode(state));
        copy.NormalizeJsonStrings();
        return copy;
    }

    static CafeSnapshot ExerciseUnlockedDispatch(CafeSimulation sim,string customer,int seed)
    {
        CafeSnapshot affinity=null;
        int started=sim.State.day, operations=0;
        sim.Execute("set_auto_assignment",choice:"manual");
        while(sim.State.day<started+75 && !sim.NamedDispatchAwaitingAnswer(customer)
            && sim.State.game_over==null && operations++<20000)
        {
            var s=sim.State;
            if(affinity==null && s.phase=="preparation" && sim.NamedAffinity(customer).Count>0)
            {
                sim.Execute("set_auto_assignment",choice:"automatic");
                affinity=CloneDispatchSnapshot(s);
            }
            if(affinity==null && s.phase=="open")
            {
                var exchange=s.exchanges.FirstOrDefault(e=>e.customer_id==customer);
                if(exchange!=null)exchange.affinity_pending=100;
                if(sim.AssignmentWaiting)
                {
                    string guest=s.queue.Contains(customer)?customer:s.queue[0];
                    var assigned=sim.AssignmentCats.OrderByDescending(c=>sim.AssignmentMatched(c.id,guest))
                        .ThenByDescending(c=>c.stamina).ThenBy(c=>c.id,StringComparer.Ordinal).First();
                    sim.Execute("assign_service",catId:assigned.id,seatId:"seat-"+(sim.AssignmentFreeSeats[0]+1),customerId:guest);
                    continue;
                }
            }
            var other=sim.NamedCustomers.FirstOrDefault(c=>c!=customer && sim.NamedDispatchAwaitingAnswer(c));
            if(other!=null && s.phase=="preparation")
            {
                sim.Execute("resolve_sato_dispatch",catId:other,choice:"decline");
                continue;
            }
            var op=BalanceOp(sim,"popularity",customer=="guest-12"?"clear":"fast",false);
            if(s.phase=="preparation" && (op.kind=="set_shifts" || op.kind=="start_business" || op.kind=="expand_seats"))
            {
                if(customer=="guest-12" && affinity==null && op.kind!="expand_seats")
                {
                    BalanceExecute(sim,op);
                    continue;
                }
                var available=s.cats.Where(c=>c.activity=="cafe" && c.health=="healthy").ToArray();
                var workers=(affinity==null?available:available.Take(1)).ToArray();
                if(s.cats.Where(c=>c.working).Select(c=>c.id).SequenceEqual(workers.Select(c=>c.id)))
                {
                    if(affinity!=null)
                    {
                        workers[0].stamina=0;
                        workers[0].cannot_continue=true;
                    }
                    op=new CafeAutoOperation {kind="start_business"};
                }
                else op=new CafeAutoOperation {kind="set_shifts",working=workers.Select(c=>c.id).ToArray()};
            }
            BalanceExecute(sim,op);
        }
        if(!sim.State.auto_assignment)sim.Execute("set_auto_assignment",choice:"automatic");
        if(affinity==null && sim.NamedAffinity(customer).Count>0)
            affinity=CloneDispatchSnapshot(sim.State);
        Console.WriteLine(Encode(new{customer,seed,policy="controlled-fixture-after-unlock",affinity_day=affinity?.day,
            request_day=sim.NamedRequest(customer)?.requested_day,sim.State.seat_count,sim.State.day,sim.State.game_over,
            discontent=sim.DiscontentScore(customer,sim.State.day)}));
        return affinity;
    }

    static void GoalUnlockedRequestsCompete(string rulesPath,CafeNamedDispatchSettings[] settings)
    {
        var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));
        rules.named_dispatch=settings;
        rules.store_events.probability=0;
        rules.goal_stages[0].target=1;
        rules.goal_stages[1].target=2;
        var sim=new CafeSimulation(rules,objective:"popularity");
        sim.Execute("day_off");
        sim.Execute("advance_goal");
        sim.Execute("day_off");
        if(sim.ReservationWaiting)sim.Execute("resolve_reservation",choice:"decline");
        sim.Execute("continue_goal");
        Assert(sim.ClearedGoalDay(2)>0 && new[]{"guest-9","guest-11"}.All(c=>sim.CustomerSchedule(sim.State.day).Any(a=>a.customer_id==c)),
            "Both guests must be unlocked for the conflict fixture");
        string cat=sim.State.cats[0].id;
        var observe=typeof(CafeSimulation).GetMethod("ObserveSatoAffinity",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        foreach(string guest in new[]{"guest-9","guest-11"})
        {
            sim.State.relationships.Add(new CafeRelationship {cat_id=cat,customer_id=guest,affinity=90});
            observe.Invoke(sim,new object[]{cat,guest,90.0});
            sim.ChangeDiscontent(sim.State.discontent,sim.State.day,guest,50);
        }
        sim.Execute("day_off");
        var guests=new[]{"guest-9","guest-11"};
        var pending=guests.Where(sim.NamedDispatchAwaitingAnswer).ToArray();
        Assert(pending.Length==1 && sim.NamedRequest(pending[0]).cat_id==cat,"Two unlocked guests reserved the same cat");
        string first=pending[0], other=guests.Single(c=>c!=first);
        sim.Execute("resolve_sato_dispatch",catId:first,choice:"decline");
        if(sim.IntakeWaiting)sim.Execute("resolve_intake",choice:"decline");
        sim.Execute("day_off");
        Assert(sim.NamedDispatchAwaitingAnswer(other) && sim.NamedRequest(other).cat_id==cat,
            "The other guest did not receive the released cat on the next day");
    }

    static void SaveDispatchCheckpoint(CafeSimulation sim,string directory,string stage,CafeSnapshot snapshot=null)
        => new CafeSaveStore(Path.Combine(directory,stage),ReplayEncode,ReplayDecode,sim.Validate).Save(9,snapshot??sim.State);

    static void RemainingGuestDispatchBalance(string rulesPath,string bundlePath)
    {
        var bundle=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(bundlePath));
        var configured=Decode<CafeNamedDispatchSettings[]>(bundle["named_dispatch"].ToJsonString());
        var candidates=new[]{(id:"guest-7",name:"山本さん",stage:1),(id:"guest-8",name:"中村さん",stage:1),
            (id:"guest-10",name:"加藤さん",stage:1),(id:"guest-12",name:"山田さん",stage:2)};
        var settings=configured.Where(row=>candidates.All(c=>c.id!=row.customer_id)).Concat(candidates.Select(c=>new CafeNamedDispatchSettings
            {customer_id=c.id,name=c.name,enabled=true,scenes=Array.Empty<CafeNamedDispatchScene>()})).ToArray();
        foreach(string mode in new[]{"clear","fast"})
        foreach(int seed in Enumerable.Range(0,10).Append(42))
        {
            var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));
            rules.stamina_growth=Decode<CafeStaminaRules>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(rulesPath),"stamina_growth.json")));
            rules.named_dispatch=settings;
            var sim=new CafeSimulation(rules,seed,"popularity");
            var rows=candidates.ToDictionary(c=>c.id,c=>new DispatchBalanceRow());
            var firstOverlap=new Dictionary<string,int>();
            int lastDay=0, operations=0;
            while(sim.State.day<=120 && sim.State.game_over==null && operations++<100000)
            {
                var s=sim.State;
                if(s.phase=="preparation" && s.day!=lastDay)
                {
                    lastDay=s.day;
                    foreach(var candidate in candidates)
                    {
                        rows[candidate.id].Observe(sim,candidate.id,50);
                        if(sim.DiscontentScore(candidate.id,s.day)>=50 && s.relationships.Any(r=>r.customer_id==candidate.id
                            && r.affinity>=80 && sim.NamedAffinity(candidate.id).Any(e=>e.cat_id==r.cat_id)))
                            firstOverlap.TryAdd(candidate.id,s.day);
                    }
                }
                var request=sim.NamedCustomers.FirstOrDefault(sim.NamedDispatchAwaitingAnswer);
                if(request!=null && s.phase=="preparation")
                    sim.Execute("resolve_sato_dispatch",catId:request,choice:"decline");
                else
                    BalanceExecute(sim,BalanceOp(sim,"popularity",mode,false));
            }
            Assert(sim.State.day==121,"Remaining guest comparison did not finish: "+mode+"/"+seed);
            sim.Validate(sim.State);
            foreach(var candidate in candidates)
            {
                var row=rows[candidate.id];
                var visits=sim.State.history.SelectMany(h=>h.customer_records).Where(v=>v.customer_id==candidate.id).ToArray();
                var request=sim.NamedRequest(candidate.id);
                Console.WriteLine(Encode(new{mode,seed,customer=candidate.id,unlocked_after=sim.ClearedGoalDay(candidate.stage),
                    visits=visits.Length,services=visits.Count(v=>v.satisfaction!=null),row.affinity_day,row.discontent_day,
                    first_overlap_day=firstOverlap.GetValueOrDefault(candidate.id),request_day=request?.requested_day,
                    requested_cat=request?.cat_id,row.max_affinity,row.max_discontent,row.overlap_days,
                    row.cat_block_days,row.staff_block_days,row.competition_days}));
            }
        }
    }

    static void RepeatedSuspensionChecks(string rulesPath)
    {
        var rules = Decode<CafeRules>(File.ReadAllText(rulesPath));
        rules.starting_funds = 100000;
        rules.store_events.probability = 0;
        rules.discontent.waiting_gain = rules.discontent.threshold;
        rules.growth.threshold = int.MaxValue;
        var sim = new CafeSimulation(rules);
        var customers = Enumerable.Range(1, rules.arrival_ticks.Length).Select(i => "guest-" + i).ToArray();
        var cat = sim.State.cats[0];

        for (int i = 0; i <= rules.discontent.suspension_days * 3; i++)
        {
            if (sim.IntakeWaiting) sim.Execute("resolve_intake", choice: "decline");
            sim.Execute("set_shifts", working: new[] { cat.id });
            cat.stamina = 0;
            cat.cannot_continue = true;
            sim.Execute("start_business");
            while (sim.State.phase == "open") sim.Execute("advance_business");
            sim.Execute("next_day");
        }

        foreach (var customer in customers)
        {
            var stops = sim.State.discontent.Single(d => d.customer_id == customer).suspensions;
            Assert(stops.Count >= 2, "Expected repeat suspensions for " + customer);
            Assert(!sim.CustomerAvailable(customer, stops[1].until), "Customer could visit during suspension");
            Assert(sim.CustomerAvailable(customer, stops[1].until + 1), "Customer could not return after suspension");
            Assert(sim.NamedRequest(customer) == null, "Unexpected named dispatch for " + customer);
        }
        Assert(sim.State.trust_events.Count == 0, "Repeated suspension generated a trust event");
        var copy = Decode<CafeSnapshot>(Encode(sim.State));
        copy.NormalizeJsonStrings();
        new CafeSimulation(rules).Restore(copy);
    }

    static void InitialGuestDispatchBalance(string rulesPath,string bundlePath,string focus=null)
    {
        var bundle=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(bundlePath));
        var settings=Decode<CafeNamedDispatchSettings[]>(bundle["named_dispatch"].ToJsonString());
        var guests=Enumerable.Range(1,6).Select(i=>"guest-"+i).ToArray();
        if(focus!=null && !guests.Contains(focus))throw new ArgumentException("Choose an initial guest",nameof(focus));
        int lastDay=focus==null?60:120;
        var preferredSeat=typeof(CafeSimulation).GetMethod("PreferredSeat",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var threshold=(double)typeof(CafeSimulation).GetField("SatoDiscontent",System.Reflection.BindingFlags.Static
            | System.Reflection.BindingFlags.NonPublic).GetRawConstantValue();
        foreach(int seed in Enumerable.Range(0,10).Append(42))
        foreach(string target in new string[]{null}.Concat(focus==null?guests:new[]{focus}))
        {
            var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));
            rules.stamina_growth=Decode<CafeStaminaRules>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(rulesPath),"stamina_growth.json")));
            rules.named_dispatch=settings;
            var sim=new CafeSimulation(rules,seed);
            string chosenGuest=target??focus;
            string favorite=chosenGuest==null?null:sim.State.cats.OrderByDescending(c=>sim.AssignmentMatched(c.id,chosenGuest))
                .ThenBy(c=>c.id,StringComparer.Ordinal).First().id;
            if(target!=null)sim.Execute("set_auto_assignment",choice:"manual");
            var rows=guests.ToDictionary(c=>c,c=>new DispatchBalanceRow());
            var assignmentReasons=focus==null?null:new Dictionary<int,string>();
            var dailyConditions=focus==null?null:new List<object>();
            int observedDay=0, operations=0, multiRequestDays=0, maxSameDayRequests=0;
            while(sim.State.day<=lastDay && sim.State.game_over==null && operations++<100000)
            {
                var state=sim.State;
                if(state.phase=="preparation" && state.day!=observedDay)
                {
                    observedDay=state.day;
                    int requests=guests.Count(c=>sim.NamedRequest(c)?.requested_day==observedDay);
                    if(requests>1)multiRequestDays++;
                    maxSameDayRequests=Math.Max(requests,maxSameDayRequests);
                    foreach(string guest in guests)rows[guest].Observe(sim,guest,threshold);
                    if(focus!=null)
                        dailyConditions.Add(new{day=state.day,discontent=sim.DiscontentScore(focus,state.day),
                            affinity=state.relationships.Where(r=>r.customer_id==focus).Select(r=>new{r.cat_id,r.affinity}).ToArray()});
                }
                if(AssignBalancePair(sim,target,favorite,preferredSeat,assignmentReasons))continue;
                string request=guests.FirstOrDefault(sim.NamedDispatchAwaitingAnswer);
                if(request!=null && state.phase=="preparation")
                {
                    if(focus!=null?request==focus:target==null || request==target)
                        NaturalNamedDispatchContinuation(sim,seed,request,target,favorite,preferredSeat,threshold);
                    sim.Execute("resolve_sato_dispatch",catId:request,choice:"decline");
                }
                else
                    BalanceExecute(sim,BalanceOp(sim,"popularity","clear",false));
            }
            Assert(sim.State.day==lastDay+1,"Comparison did not finish "+lastDay+" days: "+seed+"/"+target);
            sim.Validate(sim.State);
            string policy=target==null?"auto":"same-cat";
            Console.WriteLine(Encode(new{threshold,seed,policy,target,multi_request_days=multiRequestDays,
                max_same_day_requests=maxSameDayRequests}));
            foreach(string guest in focus!=null?new[]{focus}:target==null?guests:new[]{target})
            {
                var visits=sim.State.history.SelectMany(d=>d.customer_records)
                    .Where(v=>v.customer_id==guest && v.satisfaction!=null).ToArray();
                Console.WriteLine(Encode(new{threshold,seed,policy,guest,favorite,
                    services=visits.Length,favorite_services=visits.Count(v=>v.cat_id==favorite),rows[guest].affinity_day,
                    rows[guest].discontent_day,rows[guest].request_day,rows[guest].max_affinity,rows[guest].max_discontent,
                    rows[guest].overlap_days,rows[guest].cat_block_days,rows[guest].staff_block_days,rows[guest].competition_days}));
            }
            if(focus!=null)
            {
                var visits=sim.State.history.SelectMany(h=>h.customer_records.Where(v=>v.customer_id==focus)
                    .Select(v=>new {h.day,v.cat_id,v.satisfaction,v.departure,v.affinity_delta,
                        matched=v.cat_id==null?(bool?)null:sim.AssignmentMatched(v.cat_id,focus),
                        favorite_unavailable=assignmentReasons.GetValueOrDefault(h.day)})).ToArray();
                Console.WriteLine(Encode(new{threshold,seed,policy,guest=focus,favorite,visits,daily_conditions=dailyConditions}));
            }
        }
    }

    static bool AssignBalancePair(CafeSimulation sim,string target,string favorite,System.Reflection.MethodInfo preferredSeat,
        Dictionary<int,string> assignmentReasons=null)
    {
        if(target==null || !sim.AssignmentWaiting)return false;
        var available=sim.AssignmentCats;
        string guest=sim.State.queue[0];
        bool prefer=sim.State.queue.Contains(target) && available.Any(c=>c.id==favorite);
        if(prefer)guest=target;
        var cat=prefer?available.Single(c=>c.id==favorite):
            available.OrderByDescending(c=>sim.AssignmentMatched(c.id,guest))
                .ThenByDescending(c=>c.stamina).ThenBy(c=>c.id,StringComparer.Ordinal).First();
        if(assignmentReasons!=null && guest==target && cat.id!=favorite)
            assignmentReasons.Add(sim.State.day,sim.AssignmentCatReason(favorite));
        int seat=(int)preferredSeat.Invoke(sim,new object[]{guest,sim.AssignmentFreeSeats});
        sim.Execute("assign_service",catId:cat.id,seatId:"seat-"+(seat+1),customerId:guest);
        return true;
    }

    static bool NaturalNamedDispatchContinuation(CafeSimulation baseline,int seed,string guest,
        string target,string favorite,System.Reflection.MethodInfo preferredSeat,double threshold,string checkpoints=null,string mode="clear")
    {
        var branches=new[]{"open-as-usual","close-after-return","close-from-request"};
        string checkpointBranch=guest=="guest-10"?"open-as-usual":"close-from-request";
        bool savedReissue=false;
        foreach(string branch in branches)
        {
            var sim=new CafeSimulation(baseline.Rules);
            var saved=Decode<CafeSnapshot>(Encode(baseline.State));
            saved.NormalizeJsonStrings();
            sim.Restore(saved);
            if(target==null && !sim.State.auto_assignment)
                sim.Execute("set_auto_assignment",choice:"automatic");
            var original=sim.NamedRequest(guest);
            int operations=0;
            while(sim.State.game_over==null && operations++<30000)
            {
                var state=sim.State;
                if(original.returned_day>0 && state.day>=original.returned_day+8)break;
                if(AssignBalancePair(sim,target,favorite,preferredSeat))continue;
                if(sim.NamedReturnAccessPending(guest))
                {
                    sim.Execute("resolve_named_return_access",catId:guest,choice:"allow");
                    continue;
                }
                var other=sim.NamedCustomers.FirstOrDefault(c=>c!=guest && sim.NamedDispatchAwaitingAnswer(c));
                if(other!=null && state.phase=="preparation")
                {
                    sim.Execute("resolve_sato_dispatch",catId:other,choice:"decline");
                    continue;
                }
                var op=BalanceOp(sim,"popularity",mode,false);
                bool close=branch=="close-from-request" ||
                    branch=="close-after-return" && original.return_access==CafeSimulation.AllowReturnAccess;
                if(close && state.phase=="preparation" && (op.kind=="start_business" || op.kind=="expand_seats"))
                    op=new CafeAutoOperation {kind="day_off"};
                BalanceExecute(sim,op);
            }
            Assert(original.returned_day>0 && sim.State.day==original.returned_day+8,"Natural continuation did not reach day eight");
            sim.Validate(sim.State);
            var repeat=sim.NamedRequest(guest);
            if(checkpoints!=null && branch==checkpointBranch && repeat!=original && sim.NamedDispatchAwaitingAnswer(guest))
            {
                SaveDispatchCheckpoint(sim,checkpoints,"reissue");
                savedReissue=true;
            }
            Console.WriteLine(Encode(new{threshold,seed,policy=target==null?"auto":"same-cat",branch,guest,
                request_day=original.requested_day,cat=original.cat_id,
                missing_day=sim.State.missing_cases.Single(m=>CafeSimulation.NamedMissingCustomer(m.reason)==guest).departed_day,
                return_day=original.returned_day,check_day=sim.State.day,discontent=sim.DiscontentScore(guest,sim.State.day),
                reissue_day=repeat==original?(int?)null:repeat.requested_day}));
        }
        return savedReissue;
    }

    sealed class DispatchBalanceRow
    {
        public int? affinity_day, discontent_day, request_day;
        public double max_affinity, max_discontent;
        public int overlap_days, cat_block_days, staff_block_days, competition_days;

        public void Observe(CafeSimulation sim,string guest,double threshold)
        {
            var s=sim.State;
            double score=sim.DiscontentScore(guest,s.day);
            var affinity=s.relationships.Where(r=>r.customer_id==guest).ToArray();
            max_affinity=Math.Max(max_affinity,affinity.Select(r=>r.affinity).DefaultIfEmpty().Max());
            max_discontent=Math.Max(max_discontent,score);
            if(max_affinity>=80)affinity_day??=s.day;
            if(score>=threshold)discontent_day??=s.day;
            if(sim.NamedRequest(guest) is CafeSatoDispatch request){request_day??=request.requested_day;return;}
            var pairs=affinity.Where(r=>r.affinity>=80 && sim.NamedAffinity(guest).Any(e=>e.cat_id==r.cat_id)).ToArray();
            if(pairs.Length==0 || score<threshold)return;
            overlap_days++;
            var available=s.cats.Where(c=>c.activity=="cafe" && c.health=="healthy" && !c.cannot_continue && c.stamina>0).ToArray();
            var eligible=available.Where(c=>c.fatigue<=CafeSimulation.SatoVisit.max_fatigue && pairs.Any(r=>r.cat_id==c.id)).ToArray();
            if(eligible.Length==0)cat_block_days++;
            if(available.Length<=s.seat_count)staff_block_days++;
            if(eligible.Length>0 && eligible.All(c=>sim.NamedCustomers.Any(other=>
                sim.NamedRequest(other) is CafeSatoDispatch active && active.cat_id==c.id && active.returned_day==0 && active.declined_day==0)))
                competition_days++;
        }
    }
}
