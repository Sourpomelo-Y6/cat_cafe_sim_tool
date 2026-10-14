using CatCafePreview.Standalone;
static partial class Program
{
    static void PlayerTurnsChecks(string rulesPath,string output)
    {
        var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));
        rules.store_events.probability=0;
        Assert(rules.max_stamina==100 && rules.player.daily_turns==3,"Unchanged stamina/daily sets");
        CafeSimulation New()=>new CafeSimulation(Decode<CafeRules>(Encode(rules)),objective:"bond");
        void Roundtrip(CafeSimulation sim)
        {
            sim.PauseAutoplay();string digest=CafeReplayJournal.Digest(sim.State);
            var store=new CafeSaveStore(output,ReplayEncode,ReplayDecode,sim.Validate);
            store.Save(1,sim.State);sim.Restore(store.Load(1).state);
            Assert(digest==CafeReplayJournal.Digest(sim.State),"Disk save/reload changed state");
        }
        foreach(string scenario in new[]{"complete","exhausted","pause","manual","legacy"})
        {
            var sim=New();sim.LegacyPlayerReplay=scenario=="legacy";
            sim.Execute("player_begin",catId:"cat-tama");
            var session=sim.ActivePlayer;
            Assert(session.exchange.remaining==(scenario=="legacy"?10:20),scenario+" initial limit");
            // Serialize legacy fixtures without the new field, as actual old JSON did.
            if(scenario=="legacy")sim.Restore(Decode<CafeSnapshot>(Encode(sim.State).Replace("\"set_ticks\":0,","")));
            int limit=scenario=="legacy"?10:20;
            for(int i=0;i<limit && sim.PlayerActive;i++)
            {
                string action=scenario=="exhausted"?"intense":scenario=="pause" && i==0?"direct":"pause";
                if(sim.ActivePlayer.exchange.connect_pending)action="connect";
                double stamina=sim.ActivePlayer.exchange.stamina;
                sim.Execute("player_step",choice:action);
                if(scenario=="pause" && i==1)Assert(sim.ActivePlayer.exchange.stamina>stamina,"Pause recovers stamina");
                Roundtrip(sim);
                if(scenario=="manual" && i==3){sim.Execute("player_finish");Roundtrip(sim);break;}
            }
            session=sim.State.player_sessions.Last();
            Assert(!sim.PlayerActive,scenario+" ended");
            Assert(session.exchange.end_reason==(scenario=="exhausted"?"exhausted":scenario=="manual"?"manual":"time_limit"),scenario+" reason");
            if(scenario=="exhausted")Assert(session.actions.Count<20 && session.exchange.stamina==0,"Exhaustion early end");
            if(scenario=="complete")Assert(session.actions.Count==20 && session.exchange.remaining==0,"Twenty turns completed");
            string history=sim.PlayerHistory(session),image=sim.PlayerEventImageKey(session);
            Roundtrip(sim);Assert(history==sim.PlayerHistory(sim.State.player_sessions.Last()) && image==sim.PlayerEventImageKey(sim.State.player_sessions.Last()),"History/image stable");
            Assert(sim.PlayerRemaining==2,"One shared set consumed");
            sim.LegacyPlayerReplay=false;
            sim.Execute("player_begin",catId:"cat-sora");Assert(sim.ActivePlayer.exchange.remaining==20,"New session after legacy reload");sim.Execute("player_finish");
            sim.Execute("player_begin",catId:rules.cats.First(c=>c.id!="cat-tama" && c.id!="cat-sora").id);sim.Execute("player_finish");Assert(sim.PlayerRemaining==0 && sim.PlayerBeginReason("cat-sora")!="","Three shared sets enforced");Roundtrip(sim);
            Console.WriteLine("PASS "+scenario);
        }
        foreach(bool legacy in new[]{false,true})
        {
            var sim=New();sim.LegacyPlayerReplay=legacy;
            var journal=new CafeReplayJournal(rules,sim.State,ReplayEncode,ReplayDecode);
            if(legacy)journal.Document.player_policy_version=0;
            void Run(CafeReplayOperation op){var before=journal.CaptureBefore(sim.State);var auto=CafeReplayJournal.Apply(sim,op);journal.Append(before,sim,op,auto);}
            Run(new CafeReplayOperation {kind="player_begin",cat_id="cat-tama"});
            while(sim.PlayerActive)Run(new CafeReplayOperation {kind="player_step",choice="pause"});
            Assert(CafeReplayJournal.Digest(journal.Verify(Decode<CafeReplayDocument>(Encode(journal.Document))).State)==CafeReplayJournal.Digest(sim.State),"Replay old/new policy");
            Console.WriteLine("PASS replay "+(legacy?"legacy":"20 turns"));
        }
        var corruptSim=New();corruptSim.Execute("player_begin",catId:"cat-tama");
        string original=Encode(corruptSim.State);var corrupt=Decode<CafeSnapshot>(original);corrupt.player_sessions[0].set_ticks=30;
        bool rejected=false;try{corruptSim.Restore(corrupt);}catch(ArgumentException){rejected=true;}
        Assert(rejected && original==Encode(corruptSim.State),"Invalid saved turn limit rejected without mutation");
        CafeLegacyImport.DecodeBase=text=>Decode<CafeSnapshot>(text);
        var imported=New();string baseline=Encode(imported.State);
        imported.State.import_history=new CafeImportHistory {base_json=baseline,base_digest=CafeLegacyImport.GameDigest(imported.State),source_hash=new string('a',64)};
        imported.LegacyPlayerReplay=true;imported.Execute("player_begin",catId:"cat-tama");imported.Execute("player_step",choice:"pause");Roundtrip(imported);
        imported.LegacyPlayerReplay=false;
        while(imported.PlayerActive)imported.Execute("player_step",choice:"pause");
        imported.Execute("player_begin",catId:"cat-sora");Assert(imported.ActivePlayer.exchange.remaining==20,"Imported old history resumes ten then starts twenty");Roundtrip(imported);
        Console.WriteLine("PASS imported mixed old/new history");
        var autoSim=New();autoSim.StartAutoplay("bond","clear",1);
        int steps=0;bool saved=false;
        while(autoSim.State.autoplay.running && steps++<500)
        {
            autoSim.StepAutoplay();
            if(autoSim.PlayerActive && autoSim.ActivePlayer.actions.Count==5 && !saved)
            {Roundtrip(autoSim);Assert(autoSim.State.autoplay.paused,"Autoplay reload pauses");autoSim.ResumeAutoplay();saved=true;}
        }
        Assert(steps<500 && saved && autoSim.State.player_sessions.Count==3,"Autoplay completes three sets and reloads");
        Assert(autoSim.State.player_sessions.All(s=>s.set_ticks==20 && s.exchange.end_reason!=null && s.actions.Count<=20),"Autoplay uses saved twenty turn limits");
        Roundtrip(autoSim);Console.WriteLine($"PASS autoplay: {steps} operations; {assertions} assertions total.");
    }
}
