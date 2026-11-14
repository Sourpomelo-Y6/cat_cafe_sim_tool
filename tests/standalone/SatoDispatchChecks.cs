using CatCafePreview.Standalone;

static partial class Program
{
    static void SatoDispatchChecks(string rulesPath)
    {
        foreach (var customer in CafeSimulation.NamedDispatchCustomers)
        foreach (var route in new[]{"normal","decline","expired","refused","adopt","decline-adoption"})
        {
            var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));
            rules.starting_funds=100000;
            // Force suspension in one missed visit for the refusal fixture.
            if(route=="refused")rules.discontent.waiting_gain=100;
            rules.store_events.probability=0;
            if(customer==CafeSimulation.Suzuki)
            {
                // Test the same route with Suzuki's arrival slot and calendar.
                (rules.arrival_ticks[0],rules.arrival_ticks[1])=(rules.arrival_ticks[1],rules.arrival_ticks[0]);
                (rules.patterns[0],rules.patterns[1])=(rules.patterns[1],rules.patterns[0]);
            }
            var sim=new CafeSimulation(rules);
            var catId=sim.State.cats[0].id;

            void RoundTrip()
            {
                string encoded=Encode(sim.State);
                if(customer==CafeSimulation.Sato)
                {
                    // Existing Sato saves have no Suzuki fields.
                    var legacy=System.Text.Json.Nodes.JsonNode.Parse(encoded).AsObject();
                    legacy.Remove("suzuki_dispatch");
                    legacy.Remove("suzuki_affinity");
                    encoded=legacy.ToJsonString();
                }
                var copy=Decode<CafeSnapshot>(encoded);
                copy.NormalizeJsonStrings();
                sim.Restore(copy);
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
                if (!serve) { sim.Execute("day_off"); return; }
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
            if (route=="expired" || route=="adopt" || route=="decline-adoption")
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
            sim.Execute("resolve_missing",catId:missing.cat_id);
            RoundTrip();
            Assert(sim.NamedRequest(customer).returned_day==sim.State.day,"Missing cat return was not recorded");
            bool adoption=route=="adopt" || route=="decline-adoption";
            if(adoption)sim.Execute("configure_adoption",choice:"on");
            Day(false);
            var offer=sim.State.adoption?.events.SingleOrDefault(e=>CafeSimulation.IsNamedAdoption(e.source) && e.customer_id==customer);
            Assert((offer!=null)==adoption,"Unexpected return adoption eligibility");
            if(!adoption)continue;
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
        var conflict=new CafeSimulation(conflictRules);
        string sharedCat=conflict.State.cats[0].id;
        foreach(var customer in CafeSimulation.NamedDispatchCustomers)
        {
            conflict.State.relationships.Add(new CafeRelationship {cat_id=sharedCat,customer_id=customer,affinity=90});
            conflict.NamedAffinity(customer).Add(new CafeSatoAffinityScene {cat_id=sharedCat});
            conflict.ChangeDiscontent(conflict.State.discontent,conflict.State.day,customer,50);
        }
        if(conflict.StoreEventWaiting)conflict.Execute("resolve_store_event",choice:"patch");
        conflict.Execute("day_off");
        Assert(conflict.NamedRequest(CafeSimulation.Sato)?.cat_id==sharedCat
            && conflict.NamedRequest(CafeSimulation.Suzuki)==null,"Two customers can claim the same cat");
        conflict.Execute("resolve_sato_dispatch",catId:CafeSimulation.Sato,choice:"decline");
        if(conflict.StoreEventWaiting)conflict.Execute("resolve_store_event",choice:"patch");
        conflict.Execute("day_off");
        Assert(conflict.NamedRequest(CafeSimulation.Suzuki)?.cat_id==sharedCat,"Held request did not recheck after first request ended");
        Console.WriteLine($"PASS named dispatch: {assertions} assertions");
    }
}
