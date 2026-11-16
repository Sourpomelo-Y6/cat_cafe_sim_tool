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

    static void SatoDispatchChecks(string rulesPath,string bundlePath=null)
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
        var customers=CafeSimulation.NamedDispatchCustomers.Concat(additional.Select(r=>r.customer_id)).ToArray();
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

    static void InitialGuestDispatchBalance(string rulesPath,string bundlePath)
    {
        var bundle=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(bundlePath));
        var settings=Decode<CafeNamedDispatchSettings[]>(bundle["named_dispatch"].ToJsonString());
        var guests=Enumerable.Range(1,6).Select(i=>"guest-"+i).ToArray();
        var preferredSeat=typeof(CafeSimulation).GetMethod("PreferredSeat",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        foreach(int seed in new[]{0,1,42})
        foreach(string target in new string[]{null}.Concat(guests))
        {
            var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));
            rules.stamina_growth=Decode<CafeStaminaRules>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(rulesPath),"stamina_growth.json")));
            rules.named_dispatch=settings;
            var sim=new CafeSimulation(rules,seed);
            string favorite=target==null?null:sim.State.cats.OrderByDescending(c=>sim.AssignmentMatched(c.id,target))
                .ThenBy(c=>c.id,StringComparer.Ordinal).First().id;
            if(target!=null)sim.Execute("set_auto_assignment",choice:"manual");
            var rows=guests.ToDictionary(c=>c,c=>new DispatchBalanceRow());
            int observedDay=0, operations=0;
            while(sim.State.day<=60 && sim.State.game_over==null && operations++<50000)
            {
                var state=sim.State;
                if(state.phase=="preparation" && state.day!=observedDay)
                {
                    observedDay=state.day;
                    foreach(string guest in guests)rows[guest].Observe(sim,guest);
                }
                if(target!=null && sim.AssignmentWaiting)
                {
                    var available=sim.AssignmentCats;
                    string guest=state.queue[0];
                    // Give the chosen pair first refusal when both are available.
                    bool prefer=state.queue.Contains(target) && available.Any(c=>c.id==favorite);
                    if(prefer)guest=target;
                    var cat=prefer?available.Single(c=>c.id==favorite):
                        available.OrderByDescending(c=>sim.AssignmentMatched(c.id,guest))
                            .ThenByDescending(c=>c.stamina).ThenBy(c=>c.id,StringComparer.Ordinal).First();
                    int seat=(int)preferredSeat.Invoke(sim,new object[]{guest,sim.AssignmentFreeSeats});
                    sim.Execute("assign_service",catId:cat.id,seatId:"seat-"+(seat+1),customerId:guest);
                    continue;
                }
                string request=guests.FirstOrDefault(sim.NamedDispatchAwaitingAnswer);
                if(request!=null && state.phase=="preparation")
                {
                    if(target==null)
                        NaturalNamedDispatchContinuation(sim,seed,request);
                    sim.Execute("resolve_sato_dispatch",catId:request,choice:"decline");
                }
                else
                    BalanceExecute(sim,BalanceOp(sim,"popularity","clear",false));
            }
            Assert(sim.State.day==61,"Comparison did not finish 60 days: "+seed+"/"+target);
            sim.Validate(sim.State);
            foreach(string guest in target==null?guests:new[]{target})
            {
                var visits=sim.State.history.SelectMany(d=>d.customer_records)
                    .Where(v=>v.customer_id==guest && v.satisfaction!=null).ToArray();
                Console.WriteLine(Encode(new{seed,policy=target==null?"auto":"same-cat",guest,favorite,
                    services=visits.Length,favorite_services=visits.Count(v=>v.cat_id==favorite),rows[guest].affinity_day,
                    rows[guest].discontent_day,rows[guest].request_day,rows[guest].max_affinity,rows[guest].max_discontent,
                    rows[guest].overlap_days,rows[guest].cat_block_days,rows[guest].staff_block_days,rows[guest].competition_days}));
            }
        }
    }

    static void NaturalNamedDispatchContinuation(CafeSimulation baseline,int seed,string guest)
    {
        var branches=new[]{"open-as-usual","close-after-return","close-from-request"};
        foreach(string branch in branches)
        {
            var sim=new CafeSimulation(baseline.Rules);
            var saved=Decode<CafeSnapshot>(Encode(baseline.State));
            saved.NormalizeJsonStrings();
            sim.Restore(saved);
            var original=sim.NamedRequest(guest);
            int operations=0;
            while(sim.State.game_over==null && operations++<30000)
            {
                var state=sim.State;
                if(original.returned_day>0 && state.day>=original.returned_day+8)break;
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
                var op=BalanceOp(sim,"popularity","clear",false);
                bool close=branch=="close-from-request" ||
                    branch=="close-after-return" && original.return_access==CafeSimulation.AllowReturnAccess;
                if(close && state.phase=="preparation" && op.kind=="start_business")
                    op=new CafeAutoOperation {kind="day_off"};
                BalanceExecute(sim,op);
            }
            Assert(original.returned_day>0 && sim.State.day==original.returned_day+8,"Natural continuation did not reach day eight");
            sim.Validate(sim.State);
            var repeat=sim.NamedRequest(guest);
            Console.WriteLine(Encode(new{seed,branch,guest,request_day=original.requested_day,cat=original.cat_id,
                missing_day=sim.State.missing_cases.Single(m=>CafeSimulation.NamedMissingCustomer(m.reason)==guest).departed_day,
                return_day=original.returned_day,check_day=sim.State.day,discontent=sim.DiscontentScore(guest,sim.State.day),
                reissue_day=repeat==original?(int?)null:repeat.requested_day}));
        }
    }

    sealed class DispatchBalanceRow
    {
        public int? affinity_day, discontent_day, request_day;
        public double max_affinity, max_discontent;
        public int overlap_days, cat_block_days, staff_block_days, competition_days;

        public void Observe(CafeSimulation sim,string guest)
        {
            var s=sim.State;
            double score=sim.DiscontentScore(guest,s.day);
            var affinity=s.relationships.Where(r=>r.customer_id==guest).ToArray();
            max_affinity=Math.Max(max_affinity,affinity.Select(r=>r.affinity).DefaultIfEmpty().Max());
            max_discontent=Math.Max(max_discontent,score);
            if(max_affinity>=80)affinity_day??=s.day;
            if(score>=50)discontent_day??=s.day;
            if(sim.NamedRequest(guest) is CafeSatoDispatch request){request_day??=request.requested_day;return;}
            var pairs=affinity.Where(r=>r.affinity>=80 && sim.NamedAffinity(guest).Any(e=>e.cat_id==r.cat_id)).ToArray();
            if(pairs.Length==0 || score<50)return;
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
