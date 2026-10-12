using System.Text.Json;
using CatCafePreview.Standalone;
static partial class Program
{
    static string ReplayEncode(CafeSaveEnvelope e){e.double_bits=CafeDoublePrecision.Capture(e.state);return Encode(e);}
    static CafeSaveEnvelope ReplayDecode(string text){var e=Decode<CafeSaveEnvelope>(text);e.state.NormalizeJsonStrings();CafeDoublePrecision.Restore(e.state,e.double_bits);return e;}
    static void CheckReplayAudit(JsonElement audit,List<string> actual)
    {
        Assert(audit.GetArrayLength()==actual.Count,"Python action count differs");int index=0;
        foreach(var row in audit.EnumerateArray())
        {
            string text=actual[index++];Assert(text.Contains("行動："+row.GetProperty("action").GetString()+" / 対象："+row.GetProperty("target_type").GetString()+" / 反応："+row.GetProperty("reaction").GetString()),"Python action/target/reaction differs: "+text);
            foreach(string side in new[]{"before","after"})
            {
                string marker=side=="before"?"変更前：":"変更後：";string line=text.Split('\n').Single(s=>s.StartsWith(marker));
                foreach(var field in new[]{("engagement","関心"),("stamina","体力"),("tension","テンション"),("affinity_pending","親しみの見込み"),("connect_count","心の通い"),("open_up_count","心を開いた"),("simultaneous_count","同時発動"),("bonus_funds","ボーナス")})
                {var match=System.Text.RegularExpressions.Regex.Match(line,field.Item2+"：([^ /]+)");Assert(match.Success && Math.Abs(double.Parse(match.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture)-row.GetProperty(side).GetProperty(field.Item1).GetDouble())<1e-8,"Python log gauge/counter differs "+field.Item1);}
            }
        }
    }
    static void ReplayCases(string[] paths)
    {
        int scenarios=0,boundaries=0,rejected=0;
        foreach(var path in paths)
        {
            using var data=JsonDocument.Parse(File.ReadAllText(path));if(!data.RootElement.TryGetProperty("cases",out var cases))continue;
            foreach(var fixture in cases.EnumerateArray())
            {
                if(!fixture.TryGetProperty("rules",out _) || !fixture.TryGetProperty("operations",out _))continue;
                var rules=Decode<CafeRules>(fixture.GetProperty("rules").GetRawText());var sim=new CafeSimulation(rules,fixture.TryGetProperty("seed",out var seed)?seed.GetInt32():0,(fixture.TryGetProperty("objective",out var objective)?objective.GetString():"free"));var journal=new CafeReplayJournal(rules,sim.State,ReplayEncode,ReplayDecode);string variant=fixture.TryGetProperty("variant",out var v)?v.GetString():Path.GetFileName(path)+"-"+scenarios;bool automatic=variant.StartsWith("autoplay-");
                void Run(CafeReplayOperation operation){var before=journal.CaptureBefore(sim.State);var actual=CafeReplayJournal.Apply(sim,operation);journal.Append(before,sim,operation,actual);boundaries++;}
                if(automatic){Run(new CafeReplayOperation {kind="set_auto_assignment",choice="manual"});Run(new CafeReplayOperation {kind="start_autoplay",objective=fixture.GetProperty("objective").GetString(),mode=fixture.GetProperty("mode").GetString(),days=fixture.GetProperty("days").GetInt32()});}
                foreach(var step in fixture.GetProperty("operations").EnumerateArray())
                {
                    var operation=automatic?new CafeReplayOperation {kind="step_autoplay"}:Decode<CafeReplayOperation>(step.GetRawText());string before=Encode(sim.State);bool failed=false;
                    try{Run(operation);}catch(ArgumentException){failed=true;}catch(InvalidOperationException){failed=true;}
                    bool expectedFailure=step.TryGetProperty("rejected",out var bad) && bad.GetBoolean();Assert(failed==expectedFailure,variant+" replay rejection "+operation.kind);
                    if(failed){Assert(before==Encode(sim.State),"Rejected replay action mutated state");rejected++;continue;}
                    if(step.TryGetProperty("audit",out var audit))CheckReplayAudit(audit,sim.ReplayActions);
                    using var actualState=JsonDocument.Parse(Encode(sim.State));Equal(step.GetProperty("expected"),actualState.RootElement,variant+" replay comparison day"+sim.State.day+" tick"+sim.State.tick);
                }
                if(automatic){Run(new CafeReplayOperation {kind="step_autoplay"});Assert(sim.State.autoplay.reason==fixture.GetProperty("reason").GetString(),variant+" replay terminal");}
                string live=Encode(sim.State);var decoded=Decode<CafeReplayDocument>(Encode(journal.Document));var replayed=journal.Verify(decoded);Assert(CafeReplayJournal.Digest(replayed.State)==CafeReplayJournal.Digest(sim.State),"Replay final state differs");Assert(live==Encode(sim.State),"Replay mutated live state");
                if(scenarios<8)
                {
                    for(int mutation=0;mutation<7;mutation++)
                    {
                        if(mutation>=4 && journal.Document.operations.Count==0 || mutation==6 && journal.Document.operations.Count<2)continue;
                        var corrupt=Decode<CafeReplayDocument>(Encode(journal.Document));if(mutation==0)corrupt.format_version++;else if(mutation==1)corrupt.rules_id="wrong";else if(mutation==2)corrupt.base_digest="wrong";else if(mutation==3)corrupt.final_digest="wrong";else if(mutation==4)corrupt.operations[0].details="wrong";else if(mutation==5)corrupt.operations[0].state_digest="wrong";else corrupt.operations.Reverse();
                        bool stopped=false;try{journal.Verify(corrupt);}catch(ArgumentException){stopped=true;}catch(InvalidOperationException){stopped=true;}Assert(stopped,"Corrupt replay accepted "+mutation);Assert(live==Encode(sim.State),"Corrupt replay mutated source");
                    }
                }
                scenarios++;
            }
        }
        Console.WriteLine($"PASS: {scenarios} replay/Python scenarios, {boundaries} recorded boundaries, {rejected} rejected operations, {assertions} assertions.");
    }
}
