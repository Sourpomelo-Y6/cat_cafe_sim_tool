using System.Text.Json;
using CatCafePreview.Standalone;
static partial class Program
{
    static void DailyCases(string[] args)
    {
        var rules=Decode<CafeRules>(File.ReadAllText(args[1]));using var fixture=JsonDocument.Parse(File.ReadAllText(args[2]));int cases=0;
        foreach(var row in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            if(row.TryGetProperty("skip",out _))continue;int seed=row.GetProperty("seed").GetInt32();string mode=row.GetProperty("mode").GetString();var sim=new CafeSimulation(rules,seed);if(new[]{"recover","recovering","risk","item"}.Contains(mode))foreach(var c in sim.State.cats){c.fatigue=80;if(mode!="risk" && mode!="item"){c.health="sick";c.recovery_remaining=mode=="recover"?1:2;}}
            if(mode=="runaway")foreach(var c in sim.State.cats)c.stress=80;
            var records=new CafeDailyRecords(sim,true);
            if(row.GetProperty("choice").ValueKind==JsonValueKind.String)sim.Execute("resolve_store_event",choice:row.GetProperty("choice").GetString());
            if(mode=="item"){sim.Execute("purchase_item",choice:rules.item_shop.First(r=>r.item.fatigue_relief>0).item.id);sim.Execute("use_item",source:"item-purchase-1",catId:"cat-mike");}
            if(new[]{"dispatch","received"}.Contains(mode))sim.Execute("dispatch",catId:"cat-sora",choice:rules.dispatch.id);
            if(mode=="purchase")sim.Execute("purchase_cat",catId:rules.pet_shop[0].cat.id);
            if(mode=="rest")sim.Execute("set_shifts",working:new[]{"cat-mike","cat-tama"});
            if(new[]{"off","recover","recovering","risk"}.Contains(mode))sim.Execute("day_off");else{sim.Execute("start_business");while(sim.State.phase!="closed"){
                // Persist the baseline and previous receipts on every step, simulating arbitrary mid-day saves.
                var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,daily_baselines=records.Baselines.ToArray(),daily_records=records.Records.ToArray()}));CafeDailyRecords.Validate(save);sim.Restore(save.state);records.Restore(save.daily_baselines,save.daily_records);sim.Execute("advance_business");
            }}
            if(mode=="received"){sim.Execute("resolve_activity",source:sim.State.dispatches.Single().id,choice:"receive");records.Sync();}
            foreach(var cat in row.GetProperty("cats").EnumerateArray()){string id=cat.GetProperty("id").GetString();Assert(records.Find(1,id)?.Text()==cat.GetProperty("text").GetString(),seed+" "+mode+" "+id+" differs\n"+records.Find(1,id)?.Text()+"\nPython\n"+cat.GetProperty("text").GetString());}
            string before=Encode(sim.State),facts=Encode(records.Records);foreach(var c in sim.State.cats)records.Find(1,c.id)?.Text();Assert(before==Encode(sim.State),"daily rendering changes gameplay");
            if(sim.State.phase=="closed" && sim.State.game_over==null && !sim.DispatchWaiting)sim.Execute("next_day");Assert(Encode(records.Records)==facts,"next day changed settled facts");cases++;
        }
        var old=new CafeSimulation(rules);var missing=new CafeDailyRecords(old);old.Execute("start_business");while(old.State.phase!="closed")old.Execute("advance_business");Assert(missing.Records.Count==0,"old partial day was fabricated");old.Execute("next_day");old.Execute("day_off");Assert(missing.Records.Count==rules.cats.Length,"future day after old load missing");
        File.WriteAllText(args[3],Encode(new{passed=true,cases,checks=assertions}));Console.WriteLine($"PASS {cases} daily cases, {assertions} checks");
    }
}
