using System.Text.Json;
using CatCafePreview.Standalone;

static class Program
{
    static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
    static string Encode<T>(T value) => JsonSerializer.Serialize(value, Options);
    static T Decode<T>(string text) => JsonSerializer.Deserialize<T>(text, Options);
    static int assertions;
    static void Assert(bool valid, string message) { assertions++; if (!valid) throw new Exception(message); }
    static void Equal(JsonElement expected, JsonElement actual, string path)
    {
        if (expected.ValueKind == JsonValueKind.Object)
            foreach (var p in expected.EnumerateObject()) Equal(p.Value, actual.GetProperty(p.Name), path + "." + p.Name);
        else if (expected.ValueKind == JsonValueKind.Array)
        {
            Assert(expected.GetArrayLength() == actual.GetArrayLength(), path + " length");
            for (int i = 0; i < expected.GetArrayLength(); i++) Equal(expected[i], actual[i], path + "[" + i + "]");
        }
        else if (expected.ValueKind == JsonValueKind.Number) Assert(Math.Abs(expected.GetDouble() - actual.GetDouble()) < 1e-8, path + ": " + expected + " != " + actual);
        else Assert(expected.ToString() == actual.ToString(), path + ": " + expected + " != " + actual);
    }
    static void Main(string[] args)
    {
        var rules = Decode<CafeRules>(File.ReadAllText(args[0]));
        using var data = JsonDocument.Parse(File.ReadAllText(args[1]));
        foreach (var draw in data.RootElement.GetProperty("draws").EnumerateArray()) Assert(PythonRandom.Draw(draw.GetProperty("key").GetString()) == draw.GetProperty("value").GetDouble(), "CPython RNG mismatch");
        int ticks = 0, scenarios = 0;
        foreach (var scenario in data.RootElement.GetProperty("scenarios").EnumerateArray())
        {
            var sim = new CafeSimulation(rules, scenario.GetProperty("seed").GetInt32()); scenarios++;
            foreach (var row in scenario.GetProperty("operations").EnumerateArray())
            {
                string kind = row.GetProperty("kind").GetString();
                var working = row.TryGetProperty("working", out var w) ? w.EnumerateArray().Select(v => v.GetString()).ToArray() : null;
                sim.Execute(kind, working, row.TryGetProperty("cat_id", out var catId) ? catId.GetString() : null); ticks++;
                using var state = JsonDocument.Parse(Encode(sim.State)); Equal(row.GetProperty("expected"), state.RootElement, "seed=" + sim.State.seed + "/day=" + sim.State.day + "/tick=" + sim.State.tick);
                sim.Validate(sim.State);
                // Resume at EVERY operation boundary; compare the continuation next tick.
                sim.Restore(Decode<CafeSnapshot>(Encode(sim.State)));
            }
        }
        foreach (var test in data.RootElement.GetProperty("exchanges").EnumerateArray())
        {
            var sim = new CafeSimulation(rules);
            var initial = Decode<CafeExchange>(test.GetProperty("initial").GetRawText());
            var cat = rules.cats[test.GetProperty("cat_index").GetInt32()];
            foreach (var step in test.GetProperty("steps").EnumerateArray())
            {
                string target = step.TryGetProperty("target", out var t) ? t.GetString() : null;
                sim.StepExchange(initial, cat, step.GetProperty("action").GetString(), target);
                using var state = JsonDocument.Parse(Encode(initial)); Equal(step.GetProperty("expected"), state.RootElement, "exchange");
            }
        }
        var check = new CafeSimulation(rules);
        string before = Encode(check.State);
        try { check.Execute("advance_business"); throw new Exception("Invalid phase accepted"); } catch (InvalidOperationException) { }
        Assert(before == Encode(check.State), "Rejected command mutated state");
        try { check.Execute("set_shifts", new[] { "unknown" }); throw new Exception("Unknown cat accepted"); } catch (ArgumentException) { }
        Assert(before == Encode(check.State), "Rejected shift mutated state");
        string temp = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1])), "save-test-" + Guid.NewGuid());
        var store = new CafeSaveStore(temp, Encode, Decode<CafeSaveEnvelope>, check.Validate);
        store.Save(1, check.State); check.Execute("start_business"); store.Save(1, check.State);
        Assert(File.Exists(Path.Combine(temp, "slot-1.json.bak")), "Overwrite backup absent");
        using (var expected = JsonDocument.Parse(Encode(check.State))) using (var actual = JsonDocument.Parse(Encode(store.Load(1).state))) Equal(expected.RootElement, actual.RootElement, "disk save");
        try { store.Load(11); throw new Exception("Invalid slot accepted"); } catch (ArgumentOutOfRangeException) { }
        before = Encode(check.State);
        var invalid = Decode<CafeSnapshot>(before); invalid.version = 99;
        try { check.Restore(invalid); throw new Exception("Future version accepted"); } catch (ArgumentException) { }
        Assert(before == Encode(check.State), "Rejected load mutated state");
        File.WriteAllText(Path.Combine(temp, "slot-1.json"), "{broken");
        try { store.Load(1); throw new Exception("Corrupt save accepted"); } catch (JsonException) { }
        Console.WriteLine($"PASS: {scenarios} Python/C# scenarios, {ticks} operation boundaries, {assertions} assertions, all-boundary save/resume and disk persistence.");
    }
}
