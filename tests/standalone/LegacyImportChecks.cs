using System.Text.Json;
using CatCafePreview.Standalone;
static partial class Program
{
    static void LegacyTool(string[] args)
    {
        CafeLegacyImport.DecodeBase=text=>ReplayDecode(text).state;
        var rules=Decode<CafeRules>(File.ReadAllText(args[1]));
        if(args[0]=="--legacy-template")
        {File.WriteAllText(args[2],Encode(new[]{"free","popularity","bond","patron"}.Select(mode=>new CafeSimulation(rules,objective:mode).State).ToArray()));return;}
        if(args[0]=="--finalize-import")
        {
            var bundle=Decode<CafeLegacyBundle>(File.ReadAllText(args[2]));bundle.state.NormalizeJsonStrings();bundle.state_digest=CafeLegacyImport.GameDigest(bundle.state);bundle.double_bits=CafeDoublePrecision.Capture(bundle.state);CafeLegacyImport.ValidateBundle(bundle,rules);
            if(File.Exists(args[3]))throw new ArgumentException("変換先は存在しない別名を指定してください。");string temp=args[3]+".tmp-"+Guid.NewGuid().ToString("N");
            try{using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){var bytes=System.Text.Encoding.UTF8.GetBytes(Encode(bundle));stream.Write(bytes);stream.Flush(true);}File.Move(temp,args[3]);}finally{if(File.Exists(temp))File.Delete(temp);}
            Console.WriteLine("PASS: validated standalone import bundle");return;
        }
        throw new ArgumentException("Unknown import verification mode");
    }
    static void LegacyCases(string path)
    {
        CafeLegacyImport.DecodeBase=text=>ReplayDecode(text).state;
        using var fixtures=JsonDocument.Parse(File.ReadAllText(path));int scenarios=0,boundaries=0;
        foreach(var fixture in fixtures.RootElement.GetProperty("cases").EnumerateArray())
        {
            var rules=Decode<CafeRules>(fixture.GetProperty("rules").GetRawText());var bundle=Decode<CafeLegacyBundle>(File.ReadAllText(fixture.GetProperty("bundle").GetString()));CafeDoublePrecision.Restore(bundle.state,bundle.double_bits);CafeLegacyImport.ValidateBundle(bundle,rules);
            var sim=new CafeSimulation(rules);var baseline=Decode<CafeSnapshot>(Encode(bundle.state));
            bundle.state.import_history=new CafeImportHistory {source_hash=bundle.source_hash,base_digest=bundle.state_digest,base_json=ReplayEncode(new CafeSaveEnvelope {state=baseline})};sim.Restore(bundle.state);
            foreach(var operation in fixture.GetProperty("operations").EnumerateArray())
            {
                var op=Decode<CafeReplayOperation>(operation.GetRawText());CafeReplayJournal.Apply(sim,op);using var state=JsonDocument.Parse(Encode(sim.State));Equal(operation.GetProperty("expected"),state.RootElement,"legacy "+fixture.GetProperty("variant")+" day"+sim.State.day+" tick"+sim.State.tick);sim.Restore(Decode<CafeSnapshot>(Encode(sim.State)));boundaries++;
            }
            if(sim.State.objective!="free" && sim.AutoStartReason()=="")
            {
                sim.StartAutoplay(sim.State.objective,"fast",2);sim.StepAutoplay();sim.PauseAutoplay();
                sim.Restore(ReplayDecode(ReplayEncode(new CafeSaveEnvelope {state=sim.State})).state);
                sim.ResumeAutoplay();sim.StepAutoplay();sim.CancelAutoplay();sim.Validate(sim.State);
            }
            string before=Encode(sim.State);var bad=Decode<CafeSnapshot>(before);bad.funds++;bool rejected=false;try{sim.Restore(bad);}catch(ArgumentException){rejected=true;}Assert(rejected && before==Encode(sim.State),"Altered imported state accepted");scenarios++;
            bad=Decode<CafeSnapshot>(before);bad.import_history.operations.RemoveAt(0);rejected=false;try{sim.Restore(bad);}catch(ArgumentException){rejected=true;}Assert(rejected && before==Encode(sim.State),"Altered import suffix accepted");
            bad=Decode<CafeSnapshot>(before);var broken=ReplayDecode(bad.import_history.base_json);broken.state.funds++;bad.import_history.base_json=ReplayEncode(broken);rejected=false;try{sim.Restore(bad);}catch(ArgumentException){rejected=true;}Assert(rejected && before==Encode(sim.State),"Altered import base accepted");
        }
        Console.WriteLine($"PASS: {scenarios} legacy import scenarios, {boundaries} continuation boundaries, {assertions} assertions.");
    }
}
