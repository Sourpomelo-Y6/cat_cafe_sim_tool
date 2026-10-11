using System.Text.Json;
using CatCafePreview.Standalone;
static class HousingChecks
{
    static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
    static string Encode<T>(T value) => JsonSerializer.Serialize(value,Options);
    static T Decode<T>(string text) => JsonSerializer.Deserialize<T>(text,Options);
    public static void Run(CafeRules original, Action<bool,string> assert, string directory)
    {
        CafeSimulation New(double funds) { var r=Decode<CafeRules>(Encode(original)); r.starting_funds=funds; r.store_events.probability=0; return new CafeSimulation(r); }
        void Reject(CafeSimulation sim, string kind) { string before=Encode(sim.State); bool failed=false; try{sim.Execute(kind);}catch(InvalidOperationException){failed=true;} assert(failed && before==Encode(sim.State),"Housing rejection mutated state: "+kind); }
        void BadSave(CafeSimulation sim, Action<CafeSnapshot> mutate) { string before=Encode(sim.State); var bad=Decode<CafeSnapshot>(before); mutate(bad); bool failed=false; try{sim.Restore(bad);}catch(ArgumentException){failed=true;} assert(failed && before==Encode(sim.State),"Invalid housing save accepted"); }
        var poor=New(original.housing.cost); Reject(poor,"purchase_housing"); Reject(poor,"upgrade_housing");
        var exact=New(original.housing.cost+1); exact.Execute("purchase_housing"); assert(exact.State.funds==1,"Housing must leave positive funds");
        var equal=New(original.housing.cost+original.housing_upgrade.cost); equal.Execute("purchase_housing"); Reject(equal,"upgrade_housing");
        var sim=New(2000); Reject(sim,"upgrade_housing"); sim.Execute("purchase_housing"); Reject(sim,"purchase_housing"); sim.Execute("upgrade_housing"); Reject(sim,"upgrade_housing");
        assert(sim.HousingCapacity(sim.State,1)==12 && sim.State.funds==500 && sim.State.housing_expenses==1500,"Same-day housing purchases mismatch");
        var store=new CafeSaveStore(directory,Encode,Decode<CafeSaveEnvelope>,sim.Validate); store.Save(1,sim.State); sim.Restore(store.Load(1).state);
        BadSave(sim,s=>s.housing.purchased_day=0); BadSave(sim,s=>s.housing.upgraded_day=2); BadSave(sim,s=>s.housing_expenses=500); BadSave(sim,s=>s.housing_cost=40);
        sim.Execute("start_business"); Reject(sim,"upgrade_housing"); while(sim.State.phase!="closed")sim.Execute("advance_business");
        assert(sim.State.housing_cost==40 && sim.State.expenses==1600,"Upgrade upkeep must replace initial upkeep"); BadSave(sim,s=>s.housing_cost=20);
        sim.Execute("next_day"); assert(sim.State.housing_expenses==0 && sim.State.housing_cost==0 && sim.State.history[0].housing_cost==40,"Housing daily reset mismatch");
        BadSave(sim,s=>s.history[0].housing_expenses=500); BadSave(sim,s=>s.history[0].housing_cost=20); store.Save(2,sim.State);
        sim.Execute("day_off"); assert(sim.State.history[1].housing_cost==40 && sim.State.history[1].housing_expenses==0,"Holiday upkeep mismatch");
        var pending=New(2000); while(pending.State.day<original.intake.day)pending.Execute("day_off"); assert(pending.IntakeWaiting,"Intake fixture not waiting"); pending.Execute("purchase_housing"); pending.Execute("resolve_intake",choice:"accept"); pending.Validate(pending.State);
        foreach(bool regularFirst in new[]{false,true})
        {
            var admission=New(2000);admission.Rules.intake.day=2;admission.Rules.loyalty.threshold=25;
            admission.Execute("start_business");while(admission.State.phase!="closed")admission.Execute("advance_business");admission.Execute("next_day");
            assert(admission.IntakeWaiting && admission.RegularIntroductionWaiting,"Housing simultaneous admission fixture missing");
            string first=regularFirst?"resolve_regular_introduction":"resolve_intake",other=regularFirst?"resolve_intake":"resolve_regular_introduction";
            admission.Execute(first,choice:"accept");string full=Encode(admission.State);bool failed=false;
            try{admission.Execute(other,choice:"accept");}catch(InvalidOperationException){failed=true;}
            assert(failed && full==Encode(admission.State),"Full housing admitted seventh cat");admission.Execute("purchase_housing");admission.Execute(other,choice:"accept");admission.Validate(admission.State);
            assert(admission.State.cats.Count==7 && admission.HousingCapacity(admission.State,2)==9,"Expansion during admission lost other request");
        }
        var growth=New(2000); growth.State.growth[0].service=original.growth.threshold; Reject(growth,"purchase_housing");
        var missing=New(2000); missing.State.cats[0].activity="missing"; Reject(missing,"purchase_housing");
        var ended=New(2000); ended.State.game_over="funds"; Reject(ended,"purchase_housing");
        for(int seed=0;seed<200;seed++){var eventSim=new CafeSimulation(original,seed);if(eventSim.StoreEventWaiting){Reject(eventSim,"purchase_housing");break;}}
    }
}
