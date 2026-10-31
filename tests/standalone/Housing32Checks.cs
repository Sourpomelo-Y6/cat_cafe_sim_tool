using System.Text.Json;
using CatCafePreview.Standalone;
static partial class Program
{
    static void Housing32Checks(string rulesPath,string legacyPath,string output)
    {
        Directory.CreateDirectory(output);
        CafeRules Fresh()=>Decode<CafeRules>(File.ReadAllText(rulesPath));
        CafeRules Quiet(CafeRules r){r.starting_funds=100000;r.store_events.probability=0;r.intake.day=r.regular_introduction.day=r.visiting_cat.day=9999;r.growth.threshold=100000;return r;}
        void Reject(CafeSimulation s,string command){string before=Encode(s.State);bool failed=false;try{s.Execute(command);}catch(InvalidOperationException){failed=true;}catch(ArgumentException){failed=true;}Assert(failed && before==Encode(s.State),"Rejected command mutated state: "+command);}
        void Bad(CafeSimulation s,Action<CafeSnapshot> edit){var bad=Decode<CafeSnapshot>(Encode(s.State));edit(bad);string before=Encode(s.State);bool failed=false;try{s.Restore(bad);}catch(ArgumentException){failed=true;}Assert(failed && Encode(s.State)==before,"Corrupt housing save accepted or changed live state");}
        var sim=new CafeSimulation(Quiet(Fresh()));int[] caps={6,9,12,17,22,27,32};double[] daily={0,20,40,70,100,130,160};double[] costs={500,1000,1500,2000,2500,3000};
        Assert(sim.HousingCapacity(sim.State,1)==6 && sim.HousingMaxStage==6,"Initial capacity/max stage");Reject(sim,"extend_housing");
        var disk=new CafeSaveStore(output,Encode,Decode<CafeSaveEnvelope>,sim.Validate);
        var journal=new CafeReplayJournal(sim.Rules,sim.State,ReplayEncode,ReplayDecode);
        for(int i=0;i<6;i++){
            string kind=sim.NextHousingKind;double funds=sim.State.funds;sim.State.funds=costs[i];Reject(sim,kind);sim.State.funds=costs[i]+1;Assert(sim.HousingReason(kind)=="","Positive balance boundary");sim.State.funds=funds;
            var before=Decode<CafeSnapshot>(Encode(sim.State));sim.Execute(kind);journal.Append(before,sim,new CafeReplayOperation{kind=kind,stamina_policy_version=1,player_policy_version=2});sim.Validate(sim.State);Assert(sim.HousingCapacity(sim.State,1)==caps[i+1] && sim.HousingDailyCost(sim.State,1)==daily[i+1],"Stage capacity or total daily upkeep");
            disk.Save(i+1,sim.State);sim.Restore(disk.Load(i+1).state);
        }
        Assert(sim.State.housing_expenses==10500 && sim.State.funds==89500 && sim.NextHousingKind==null && sim.NextHousingRules==null,"Same-day total or final stage");
        var replayed=journal.Verify(journal.Document);Assert(replayed.HousingCapacity(replayed.State,1)==32,"New extension operations cannot replay");
        Reject(sim,"extend_housing");Reject(sim,"upgrade_housing");Bad(sim,s=>s.housing.extensions[0].stage=4);Bad(sim,s=>s.housing.extensions[0].day=0);Bad(sim,s=>s.housing.extensions[3].day=2);Bad(sim,s=>s.housing.extensions[1]=null);Bad(sim,s=>s.housing_expenses--);
        sim.Execute("day_off");sim.Validate(sim.State);Assert(sim.State.housing_cost==0 && sim.State.history[0].housing_cost==160,"Same-day holiday charged final upkeep once");
        sim.Execute("day_off");sim.Validate(sim.State);Assert(sim.State.history[1].housing_expenses==0 && sim.State.history[1].housing_cost==160,"Recurring upkeep");Bad(sim,s=>s.history[0].housing_cost=40);
        var spaced=new CafeSimulation(Quiet(Fresh()));spaced.Execute("purchase_housing");spaced.Execute("upgrade_housing");spaced.Execute("day_off");spaced.Execute("extend_housing");spaced.Execute("day_off");spaced.Validate(spaced.State);Assert(spaced.State.history[0].housing_cost==40 && spaced.State.history[1].housing_cost==70,"Historical daily cost changed retroactively");
        var admission=new CafeSimulation(Quiet(Fresh()));for(int i=0;i<6;i++)admission.Execute(admission.NextHousingKind);
        while(admission.ActiveCatCount(admission.State)<32 && admission.State.day<80){
            if(admission.RecruitmentRefreshReason()=="")admission.Execute("open_recruitment");
            foreach(var c in admission.RecruitmentCandidates(admission.State))if(admission.RecruitReason(c.cat.id)=="")admission.Execute("recruit_cat",catId:c.cat.id);
            admission.Validate(admission.State);
            if(admission.ActiveCatCount(admission.State)<32){admission.Execute("day_off");}
        }
        Assert(admission.ActiveCatCount(admission.State)==32,"Normal recruitment did not fill 32 slots");
        if(admission.RecruitmentRefreshReason()=="")admission.Execute("open_recruitment");
        var candidate=admission.RecruitmentCandidates(admission.State).FirstOrDefault(c=>!admission.State.cats.Any(x=>x.id==c.cat.id));
        if(candidate==null){while(admission.RecruitmentRefreshReason()!="")admission.Execute("day_off");admission.Execute("open_recruitment");candidate=admission.RecruitmentCandidates(admission.State).First(c=>!admission.State.cats.Any(x=>x.id==c.cat.id));}
        string unchanged=Encode(admission.State);bool fullRejected=false;try{admission.Execute("recruit_cat",catId:candidate.cat.id);}catch(InvalidOperationException){fullRejected=true;}Assert(fullRejected && unchanged==Encode(admission.State),"33rd cat admitted");
        var cat=admission.State.cats[0];cat.activity="dispatched";Assert(admission.ActiveCatCount(admission.State)==32,"Dispatched cat released slot");cat.activity="missing";Assert(admission.ActiveCatCount(admission.State)==32,"Missing cat released slot");cat.activity="adopted";Assert(admission.ActiveCatCount(admission.State)==31,"Adopted cat retained slot");cat.activity="cafe";
        var fullStore=new CafeSaveStore(Path.Combine(output,"full"),Encode,Decode<CafeSaveEnvelope>,admission.Validate);fullStore.Save(1,admission.State);admission.Restore(fullStore.Load(1).state);
        CafeLegacyImport.DecodeBase=text=>Decode<CafeSaveEnvelope>(text).state;
        for(int stage=0;stage<=2;stage++){
            var oldRules=Quiet(Decode<CafeRules>(File.ReadAllText(legacyPath)));var current=Quiet(Fresh());var old=new CafeSimulation(oldRules);if(stage>0)old.Execute("purchase_housing");if(stage>1)old.Execute("upgrade_housing");old.Execute("day_off");
            var envelope=new CafeSaveEnvelope{state=old.State,slot=1,created_at=DateTimeOffset.UtcNow.ToString("o"),equipment_conversation_selected="preserve-me",double_bits=CafeDoublePrecision.Capture(old.State)};
            var migrationDir=Path.Combine(output,"legacy-"+stage);Directory.CreateDirectory(migrationDir);string path=Path.Combine(migrationDir,"slot-1.json");File.WriteAllText(path,Encode(envelope));string original=File.ReadAllText(path);
            var modern=new CafeSimulation(current);var store=new CafeSaveStore(migrationDir,Encode,Decode<CafeSaveEnvelope>,modern.Validate,s=>CafeHousingMigration.Migrate(s,current,oldRules,Decode<CafeSaveEnvelope>,Encode));
            var loaded=store.Load(1);Assert(File.ReadAllText(path)==original,"Read-only migration overwrote source");Assert(loaded.equipment_conversation_selected=="preserve-me" && modern.HousingCapacity(loaded.state,loaded.state.day)==caps[stage] && loaded.state.housing.extensions==null,"Legacy progress/capacity changed");modern.Restore(loaded.state);if(stage==2)modern.Execute("extend_housing");modern.Validate(modern.State);
            var corrupt=Decode<CafeSaveEnvelope>(original);corrupt.state.history[0].housing_cost++;bool rejected=false;try{CafeHousingMigration.Migrate(corrupt,current,oldRules,Decode<CafeSaveEnvelope>,Encode);}catch(ArgumentException){rejected=true;}Assert(rejected,"Migration accepted corrupt old history");
            var unknown=Decode<CafeSaveEnvelope>(original);unknown.state.rules_id="unknown";rejected=false;try{CafeHousingMigration.Migrate(unknown,current,oldRules,Decode<CafeSaveEnvelope>,Encode);}catch(ArgumentException){rejected=true;}Assert(rejected,"Migration accepted unknown rules");
            current.base_cost++;rejected=false;try{CafeHousingMigration.Migrate(Decode<CafeSaveEnvelope>(original),current,oldRules,Decode<CafeSaveEnvelope>,Encode);}catch(ArgumentException){rejected=true;}Assert(rejected,"Migration accepted unrelated balance change");
        }
        // Imported-save anchors are validated before their identity and digest are rebased.
        var lr=Quiet(Decode<CafeRules>(File.ReadAllText(legacyPath)));var nr=Quiet(Fresh());var imported=new CafeSimulation(lr);var anchor=new CafeSaveEnvelope{state=Decode<CafeSnapshot>(Encode(imported.State))};
        imported.State.import_history=new CafeImportHistory{base_json=Encode(anchor),base_digest=CafeLegacyImport.GameDigest(anchor.state),source_hash=new string('a',64)};imported.Execute("purchase_housing");imported.Validate(imported.State);
        var importedSave=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=imported.State}));CafeHousingMigration.Migrate(importedSave,nr,lr,Decode<CafeSaveEnvelope>,Encode);new CafeSimulation(nr).Validate(importedSave.state);Assert(importedSave.state.import_history.operations.Count==1,"Imported operation tape lost");
        // Actual unmodified approved rules also migrate, without fixture balance changes.
        var actualOld=new CafeSimulation(Decode<CafeRules>(File.ReadAllText(legacyPath)));var actualSave=new CafeSaveEnvelope{state=actualOld.State};CafeHousingMigration.Migrate(actualSave,Fresh(),actualOld.Rules,Decode<CafeSaveEnvelope>,Encode);new CafeSimulation(Fresh()).Validate(actualSave.state);
        HousingChecks.Run(Fresh(),Assert,Path.Combine(output,"existing-housing"));
#if CATCAFE_DEV_ACQUISITION
        var plan=new DevAcquisitionPlan{collection_routes=true};
        var routes=CafeDevAcquisition.CreateRoutes(Fresh(),plan);var previousRoutes=CafeDevAcquisition.CreateRoutes(actualOld.Rules,plan);
        Assert(routes.Length==9 && Encode(routes)==Encode(previousRoutes),"R2 changed existing collection routes/targets");
#endif
        Console.WriteLine("PASS housing32: "+assertions+" assertions; all stages, funds/history, 32-cat admission, disk resume and scoped legacy migration.");
    }
}
