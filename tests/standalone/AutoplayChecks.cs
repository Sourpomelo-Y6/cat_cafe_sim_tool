using System.Text.Json;
using CatCafePreview.Standalone;
static partial class Program
{
    static void AutoplayCases(string path)
    {
        using var fixtures=JsonDocument.Parse(File.ReadAllText(path));int cases=0,boundaries=0;
        foreach(var fixture in fixtures.RootElement.GetProperty("cases").EnumerateArray())
        {
            string variant=fixture.GetProperty("variant").GetString();var sim=new CafeSimulation(Decode<CafeRules>(fixture.GetProperty("rules").GetRawText()),fixture.GetProperty("seed").GetInt32(),fixture.GetProperty("objective").GetString());
            sim.Execute("set_auto_assignment",choice:"manual");sim.StartAutoplay(fixture.GetProperty("objective").GetString(),fixture.GetProperty("mode").GetString(),fixture.GetProperty("days").GetInt32());
            foreach(var step in fixture.GetProperty("operations").EnumerateArray())
            {
                var op=sim.StepAutoplay();Assert(op!=null,variant+" unexpected stop "+sim.State.autoplay.reason+" "+sim.State.autoplay.message);
                if(op.working!=null)op.working=op.working.OrderBy(id=>id,StringComparer.Ordinal).ToArray();using var command=JsonDocument.Parse(Encode(op));foreach(string key in new[]{"kind","working","cat_id","choice","source","seat_id"})Equal(step.GetProperty(key),command.RootElement.GetProperty(key),variant+" command "+boundaries+" "+key);
                using var state=JsonDocument.Parse(Encode(sim.State));Equal(step.GetProperty("expected"),state.RootElement,variant+" day"+sim.State.day+" tick"+sim.State.tick);boundaries++;
                sim.PauseAutoplay();var saved=Decode<CafeSnapshot>(Encode(sim.State));sim.Restore(saved);Assert(sim.State.autoplay.paused,"Load should remain paused");sim.ResumeAutoplay();
            }
            Assert(sim.StepAutoplay()==null,variant+" expected stop");var run=sim.State.autoplay;Assert(run.reason==fixture.GetProperty("reason").GetString(),variant+" reason "+run.reason+" != "+fixture.GetProperty("reason"));Assert(run.days==fixture.GetProperty("completed_days").GetInt32() && run.operations==fixture.GetProperty("operation_count").GetInt32(),variant+" counters");Assert(!sim.State.auto_assignment,"Manual assignment was not restored");sim.Restore(Decode<CafeSnapshot>(Encode(sim.State)));cases++;
        }
        Console.WriteLine($"PASS: {cases} autoplay scenarios, {boundaries} operation boundaries, {assertions} assertions.");
    }
}
