using System.Text.Json;
using CatCafePreview.Standalone;

static class Program
{
    static readonly JsonSerializerOptions Json=new(){IncludeFields=true,IgnoreReadOnlyProperties=true};
    static string Encode(object o)=>JsonSerializer.Serialize(o,o.GetType(),Json);
    static T Decode<T>(string text)=>JsonSerializer.Deserialize<T>(text,Json);
    static CafeSaveEnvelope Save(string text){var s=Decode<CafeSaveEnvelope>(text);s.state.NormalizeJsonStrings();CafeDoublePrecision.Restore(s.state,s.double_bits);return s;}
    static CafeRules Rules(string path){var r=Decode<CafeRules>(File.ReadAllText(path));r.stamina_growth=Decode<CafeStaminaRules>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(path),"stamina_growth.json")));return r;}
    static CafeDevAcquisition Open(string path,CafeRules rules,DevAcquisitionPlan plan,bool resume=false)=>new(path,rules,plan,"test-content-v1",Encode,Save,Decode<DevAcquisitionCheckpoint>,Decode<DevAcquisitionResult>,resume);
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);Console.WriteLine("PASS "+message);}
    static int Main(string[] args)
    {
        try
        {
            string rulesPath=Path.GetFullPath(args[0]),output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
            if(args.Length>2 && new[]{"collect","collect-pause","collect-resume"}.Contains(args[2]))
            {
                bool resume=args[2]=="collect-resume";
                using var run=Open(Path.Combine(output,"collection"),Rules(rulesPath),new(){collection_routes=true},resume);
                if(resume)Check(run.Status=="paused" && run.RouteId=="collect-catalog-2","collection resume restores distinct route and waits");
                run.Start();while(!run.Finished)
                {
                    run.Step();
                    if(args[2]=="collect-pause" && run.RouteId=="collect-catalog-2" && run.Operations>=75){run.Pause();Check(run.Status=="paused","pause within second catalog cohort");return 0;}
                }
                Console.WriteLine(run.Status+" "+run.Reason);Check(run.Status=="completed","assigned collection routes complete");
                var summary=Decode<DevAcquisitionSummary>(File.ReadAllText(Path.Combine(output,"collection","summary.json")));
                Check(summary.routes.Length==9 && summary.routes.Select(r=>r.route_id).Distinct().Count()==9,"nine distinct independent routes");
                foreach(var r in summary.routes.Where(r=>r.assigned_targets.Length>0))Check(r.assigned_targets.All(id=>r.targets.Any(t=>t.category+"/"+t.id==id && t.status=="acquired")),"assigned targets acquired: "+r.route_id);
                foreach(var c in new[]{("cat",31),("equipment",5),("facility",13),("supply",4),("dispatch",12)})Check(summary.union.Count(t=>t.category==c.Item1 && t.status=="acquired")==c.Item2,"complete union: "+c.Item1);
                Check(summary.routes.All(r=>r.targets.Count(t=>t.category=="cat" && t.currently_owned)<=12),"single route never claims all 31 cats simultaneously");
                Check(summary.union.Where(t=>t.status=="acquired").All(t=>t.evidence_routes.Length>0),"union retains route evidence");return 0;
            }
            if(args.Length>2 && args[2]=="routes")
            {
                using var run=Open(Path.Combine(output,"normal-routes"),Rules(rulesPath),new());run.Start();while(!run.Finished)run.Step();
                Console.WriteLine(run.Status+" "+run.Reason);Check(run.Status=="completed","all four standard route objectives complete");
                var summary=Decode<DevAcquisitionSummary>(File.ReadAllText(Path.Combine(output,"normal-routes","summary.json")));
                Check(summary.routes.Length==4 && summary.routes.All(r=>r.status=="acquired"),"four isolated route results");
                Check(summary.union.Count(t=>t.category=="cat")==31,"31 finite cat families in catalog");
                Check(summary.union.Any(t=>t.status=="unreached"),"objective completion does not grant all collection targets");return 0;
            }
            if(args.Length>2 && args[2]=="restart")
            {
                using var resumed=Open(Path.Combine(output,"split"),Rules(rulesPath),new(){objectives=new[]{"free"},free_days=8},true);
                Check(resumed.Status=="paused","separate process resume is paused");resumed.Start();for(int i=0;i<60;i++)resumed.Step();
                Check(CafeReplayJournal.Digest(resumed.Simulation.State)==File.ReadAllText(Path.Combine(output,"expected-digest.txt")),"separate process next 60 operations reproduce");return 0;
            }
            var plan=new DevAcquisitionPlan{objectives=new[]{"free"},free_days=8};
            using(var baseline=Open(Path.Combine(output,"baseline"),Rules(rulesPath),plan))
            using(var split=Open(Path.Combine(output,"split"),Rules(rulesPath),plan))
            {
                split.Step();Check(split.Operations==0,"new run requires explicit start");baseline.Start();split.Start();
                for(int i=0;i<75;i++){baseline.Step();split.Step();}
                Check(split.Status=="running","75 standard operations without error");split.Pause();
                Check(CafeReplayJournal.Digest(split.Simulation.State)==CafeReplayJournal.Digest(baseline.Simulation.State),"same seed operation stream");
                for(int i=0;i<60;i++)baseline.Step();File.WriteAllText(Path.Combine(output,"expected-digest.txt"),CafeReplayJournal.Digest(baseline.Simulation.State));
                var frozen=CafeReplayJournal.Digest(split.Simulation.State);split.Step();Check(frozen==CafeReplayJournal.Digest(split.Simulation.State),"pause does not advance");
                Check(split.Result().targets.Where(t=>t.category=="replay" || t.category=="image").All(t=>t.status=="excluded"),"internal runner never grants viewed media");
            }
            using(var resumed=Open(Path.Combine(output,"split"),Rules(rulesPath),plan,true))
            {
                resumed.Start();for(int i=0;i<60;i++)resumed.Step();
                Check(CafeReplayJournal.Digest(resumed.Simulation.State)==File.ReadAllText(Path.Combine(output,"expected-digest.txt")),"save resume next operations reproduce");
            }
            bool rejected=false;try{using var bad=Open(Path.Combine(output,"split"),Rules(rulesPath),new(){seed=1,objectives=new[]{"free"},free_days=8},true);}catch(ArgumentException){rejected=true;}Check(rejected,"changed seed/plan rejected");
            rejected=false;try{using var bad=Open(Path.Combine(output,"split"),Rules(rulesPath),plan);}catch(IOException){rejected=true;}Check(rejected,"fresh run refuses existing data");
            using(var limited=Open(Path.Combine(output,"limited"),Rules(rulesPath),new(){objectives=new[]{"free"},max_operations=3}))
            {limited.Start();while(!limited.Finished)limited.Step();Check(limited.Status=="incomplete" && limited.Reason=="limit","operation limit stops with incomplete result");}
            using(var limited=Open(Path.Combine(output,"log-limited"),Rules(rulesPath),new(){objectives=new[]{"free"},max_log_bytes=4096}))
            {limited.Start();while(!limited.Finished)limited.Step();Check(limited.Status=="incomplete" && limited.Reason=="log_limit","log budget stops safely");}
            using(var locked=Open(Path.Combine(output,"locked"),Rules(rulesPath),plan))
            {
                rejected=false;try{using var second=Open(Path.Combine(output,"locked"),Rules(rulesPath),plan,true);}catch(IOException){rejected=true;}Check(rejected,"concurrent owner rejected");
            }
            var cp=Decode<DevAcquisitionCheckpoint>(File.ReadAllText(Path.Combine(output,"locked","checkpoint.json")));
            File.AppendAllText(Path.Combine(output,"locked","routes","free","saves","slot-"+cp.slot+".json")," ");
            rejected=false;try{using var bad=Open(Path.Combine(output,"locked"),Rules(rulesPath),plan,true);}catch(IOException){rejected=true;}Check(rejected,"tampered checkpoint save rejected");
            using(var atomic=Open(Path.Combine(output,"atomic"),Rules(rulesPath),plan))
            {
                string file=Path.Combine(output,"atomic","checkpoint.json");string before=File.ReadAllText(file);
                atomic.Start();for(int i=0;i<10;i++)atomic.Step();
                using(var held=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.None))
                {rejected=false;try{atomic.Pause();}catch(IOException){rejected=true;}Check(rejected,"checkpoint IO failure surfaces");}
                Check(File.ReadAllText(file)==before,"failed checkpoint preserves previous commit");
            }
            using(var old=Open(Path.Combine(output,"atomic"),Rules(rulesPath),plan,true))Check(old.Operations==0,"previous checkpoint still resumes after failed save");
            Console.WriteLine("ALL CHECKS PASSED");return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
