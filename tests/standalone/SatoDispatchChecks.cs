using CatCafePreview.Standalone;

static partial class Program
{
    static void SatoDispatchChecks(string rulesPath)
    {
        foreach (var route in new[]{"normal","decline","expired","refused","adopt","decline-adoption"})
        {
            var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));
            rules.starting_funds=100000;
            // Force suspension in one missed visit for the refusal fixture.
            if(route=="refused")rules.discontent.waiting_gain=100;
            rules.store_events.probability=0;
            var sim=new CafeSimulation(rules);
            var catId=sim.State.cats[0].id;

            void RoundTrip()
            {
                var copy=Decode<CafeSnapshot>(Encode(sim.State));
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
                var exchange=sim.State.exchanges.SingleOrDefault(e=>e.customer_id==CafeSimulation.Sato);
                if(exchange!=null)exchange.affinity_pending=100;
                sim.Execute("advance_business");
            }
            sim.Execute("next_day");
            Assert(sim.State.sato_affinity.Count>0,"Sato affinity scene was not recorded after service");
            while(sim.State.sato_dispatch==null && sim.State.day<65)
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
            Assert(sim.State.sato_dispatch!=null,"Sato request was not issued");
            Assert(sim.DiscontentScore(CafeSimulation.Sato,sim.State.day)==(route=="refused"?100:50),"Wrong score at request");
            sim.State.sato_dispatch.conversation.node=1;
            RoundTrip();
            Assert(sim.State.sato_dispatch.conversation.node==1,"Request conversation position was lost");
            if (route=="decline")
            {
                sim.Execute("resolve_sato_dispatch",choice:"decline");
                RoundTrip();
                Assert(sim.State.sato_dispatch.declined_day==sim.State.day,"Decline was not recorded");
                continue;
            }
            if (route=="expired" || route=="adopt" || route=="decline-adoption")
            {
                Day(false);
                Assert(sim.State.sato_dispatch.conversation.node==0,"Reminder did not reset the scene to its first page");
                Day(false);
            }
            else
            {
                if (route=="refused") Day(false);
                sim.Execute("resolve_sato_dispatch",choice:"accept");
                Day(false);
                var trip=sim.State.dispatches.Single(e=>e.destination==CafeSimulation.SatoDestination);
                sim.Execute("resolve_activity",source:trip.id,choice:"receive");
                if (route=="normal")
                {
                    RoundTrip();
                    Assert(sim.DiscontentScore(CafeSimulation.Sato,sim.State.day)==25,"Return did not reduce dissatisfaction");
                    continue;
                }
            }

            var missing=sim.State.missing_cases.Single(e=>e.reason=="sato_expired" || e.reason=="sato_refused");
            Assert(missing.resolved_day==0,"Missing case did not open");
            Assert(sim.State.cats.Single(c=>c.id==catId).activity=="missing","Cat remained available while missing");
            RoundTrip();
            var missingCat=sim.State.cats.Single(c=>c.id==missing.cat_id);
            while (missingCat.missing_remaining>0) Day(false);
            sim.Execute("resolve_missing",catId:missing.cat_id);
            RoundTrip();
            Assert(sim.State.sato_dispatch.returned_day==sim.State.day,"Missing cat return was not recorded");
            bool adoption=route=="adopt" || route=="decline-adoption";
            if(adoption)sim.Execute("configure_adoption",choice:"on");
            Day(false);
            var offer=sim.State.adoption?.events.SingleOrDefault(e=>e.source==CafeSimulation.SatoAdoptionSource);
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
                sim.State.sato_dispatch.epilogue_read=true;
                RoundTrip();
                Assert(sim.State.sato_dispatch.epilogue_read && sim.SatoEpilogueAvailable(catId),"Read epilogue cannot be revisited");
            }
        }
        Console.WriteLine($"PASS Sato dispatch: {assertions} assertions");
    }
}
