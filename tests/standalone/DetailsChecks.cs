using System.Text.Json;
using CatCafePreview.Standalone;
static partial class Program
{
    static void DetailsCases(string[] args)
    {
        var rules=Decode<CafeRules>(File.ReadAllText(args[1]));var catalog=Decode<CafePresentationCatalog>(File.ReadAllText(args[2]));using var fixture=JsonDocument.Parse(File.ReadAllText(args[3]));int cases=0,cats=0;
        CafeLegacyImport.DecodeBase=text=>ReplayDecode(text).state;
        foreach(var row in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            string path=row.GetProperty("bundle").GetString();var bundle=Decode<CafeLegacyBundle>(File.ReadAllText(path));bundle.state.NormalizeJsonStrings();CafeDoublePrecision.Restore(bundle.state,bundle.double_bits);CafeLegacyImport.ValidateBundle(bundle,rules);
            var baseline=Decode<CafeSnapshot>(Encode(bundle.state));bundle.state.import_history=new CafeImportHistory{source_hash=bundle.source_hash,base_digest=bundle.state_digest,base_json=ReplayEncode(new CafeSaveEnvelope{state=baseline})};var sim=new CafeSimulation(rules);sim.Restore(bundle.state);var display=new CafeDetailPresentation(sim,catalog);
            foreach(var cat in row.GetProperty("cats").EnumerateArray())
            {
                string id=cat.GetProperty("id").GetString(),context=Path.GetFileName(Path.GetDirectoryName(path))+" "+id,before=Encode(sim.State);
                Assert(display.Profile(id)==cat.GetProperty("profile").GetString(),context+" profile differs\n"+display.Profile(id)+"\nexpected\n"+cat.GetProperty("profile").GetString());
                Assert(display.Forecast(id)==cat.GetProperty("forecast").GetString(),context+" forecast differs\n"+display.Forecast(id)+"\nexpected\n"+cat.GetProperty("forecast").GetString());Assert(display.ContentId(id)==cat.GetProperty("content_id").GetString(),context+" content ID differs");
                string relationships=display.Relationships(id);foreach(var pair in cat.GetProperty("relationships").EnumerateArray())Assert(relationships.Contains(display.CustomerName(pair.GetProperty("id").GetString())+"（"+pair.GetProperty("id").GetString()+"）\n確定済み親しみ："+pair.GetProperty("affinity").GetString()+" / "+pair.GetProperty("stage").GetString()),context+" relationship differs");
                string history=display.History(id);foreach(var day in cat.GetProperty("history").EnumerateArray()){int number=day.GetProperty("day").GetInt32();var section=history.Split("日目："+number+"\n");Assert(section.Length==2,context+" missing history day "+number);string ticks=day.GetProperty("ticks").GetString();Assert(section[1].Split("\n\n")[0].Contains("接客行動数："+ticks+"\n"),context+" history ticks differ day "+number);}
                string events=display.Events(id);Assert(!string.IsNullOrEmpty(events),context+" events blank");Assert(Encode(sim.State)==before,context+" viewing mutated gameplay");
                sim.Restore(ReplayDecode(ReplayEncode(new CafeSaveEnvelope{state=sim.State})).state);Assert(display.Profile(id)==cat.GetProperty("profile").GetString() && display.Forecast(id)==cat.GetProperty("forecast").GetString() && display.History(id)==history && display.Events(id)==events && display.Relationships(id)==relationships,context+" save reload changed projections");cats++;
            }
            cases++;
        }
        var native=new CafeSimulation(rules);native.Execute("start_business");while(native.State.phase!="closed")native.Execute("advance_business");var nativeView=new CafeDetailPresentation(native,catalog);
        foreach(var forward in fixture.RootElement.GetProperty("forward").EnumerateArray())
        {
            if(forward.GetProperty("mode").GetString()=="next")native.Execute("next_day");
            foreach(var cat in forward.GetProperty("cats").EnumerateArray())
            {
                string id=cat.GetProperty("id").GetString();Assert(nativeView.Profile(id)==cat.GetProperty("profile").GetString(),"native profile differs");Assert(nativeView.Forecast(id)==cat.GetProperty("forecast").GetString(),"native forecast differs");string history=nativeView.History(id);
                foreach(var day in cat.GetProperty("history").EnumerateArray())foreach(var field in new[]{("count","接客件数"),("ticks","接客行動数"),("xp","獲得経験"),("delta","親しみ増減合計")})Assert(history.Contains(field.Item2+"："+day.GetProperty(field.Item1).GetString()+"\n") || history.EndsWith(field.Item2+"："+day.GetProperty(field.Item1).GetString(),StringComparison.Ordinal),"native history differs "+id+" "+field.Item1+"\n"+history);
                if(forward.GetProperty("mode").GetString()=="closed")Assert(history.Contains("体力消耗："+cat.GetProperty("history")[0].GetProperty("spent").GetString()+"\n"),"closed net stamina differs "+id);else Assert(history.Contains("体力消耗：記録なし"),"lost net stamina was fabricated");
                native.Restore(ReplayDecode(ReplayEncode(new CafeSaveEnvelope{state=native.State})).state);Assert(nativeView.History(id)==history,"native history changed on save reload");
            }
        }
        foreach(var probe in fixture.RootElement.GetProperty("probes").EnumerateArray())
        {
            var cat=native.State.cats.Single(c=>c.id=="cat-mike");cat.fatigue=probe.GetProperty("fatigue").GetDouble();cat.stress=probe.GetProperty("stress").GetDouble();cat.health=probe.GetProperty("sick").GetBoolean()?"sick":"healthy";cat.recovery_remaining=probe.GetProperty("remaining").GetInt32();Assert(nativeView.Forecast(cat.id)==probe.GetProperty("forecast").GetString(),"forecast boundary differs\n"+nativeView.Forecast(cat.id)+"\nexpected\n"+probe.GetProperty("forecast").GetString());
        }
        var returned=new CafeSimulation(rules);returned.Execute("dispatch",catId:"cat-sora",choice:"neighborhood_visit");while(!returned.DispatchWaiting)returned.Execute("day_off");var trip=returned.State.dispatches.Single();returned.Execute("resolve_activity",source:trip.id,choice:"receive");double fixedReward=returned.DispatchReceivedReward(trip);var tripView=new CafeDetailPresentation(returned,catalog);string received=tripView.Events("cat-sora");Assert(received.Contains("帰還報酬の受取") && received.Contains("報酬 "+CafeDetailPresentation.Number(fixedReward)),"received dispatch details missing");returned.GrowthCat("cat-sora").specialization="dispatch";Assert(tripView.Events("cat-sora").Split("帰還報酬の受取")[1].Split("\n\n")[0]==received.Split("帰還報酬の受取")[1].Split("\n\n")[0],"display recalculated departed reward using current growth");
        var fresh=new CafeDetailPresentation(new CafeSimulation(rules),catalog);foreach(var name in fixture.RootElement.GetProperty("names").EnumerateArray())Assert(fresh.CustomerName(name.GetProperty("id").GetString())==name.GetProperty("name").GetString(),"customer name differs "+name.GetProperty("id").GetString());
        File.WriteAllText(args[4],Encode(new{passed=true,cases,cats,checks=assertions}));Console.WriteLine($"PASS: {cases} cases, {cats} cats, {assertions} display checks");
    }
}
