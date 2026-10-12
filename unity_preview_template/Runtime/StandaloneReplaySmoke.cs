using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneReplaySmoke
    {
        private static int checks;
        private static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
        private static CafeResponse Request(LocalCafeBackend b,string route,string body=null){CafeResponse r=null;var work=b.Request(route,body,x=>r=x);while(work.MoveNext()){}return r;}
        private static PythonShiftCommand Command(LocalCafeBackend b,string kind,string choice="",string cat="",string customer="",string seat="",int slot=1)=>new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,customer_id=customer,seat_id=seat,working_cats=new string[0],save_slot=slot,save_id=slot.ToString(),autoplay_objective=choice,autoplay_mode="fast",autoplay_days=2};
        private static void Good(LocalCafeBackend b,PythonShiftCommand c){var r=Request(b,"/commands",JsonUtility.ToJson(c));Check(r.result==UnityWebRequest.Result.Success,c.kind+": "+r.error);}
        private static PythonBusinessLogView View(LocalCafeBackend b,string route){var r=Request(b,route);Check(r.result==UnityWebRequest.Result.Success,"Log failed: "+r.error);return JsonUtility.FromJson<PythonBusinessLogView>(r.text);}
        private static void VerifyLog(LocalCafeBackend b,string path)
        {
            string before=JsonUtility.ToJson(b.Simulation.State);var files=Directory.Exists(Path.GetDirectoryName(path))?Directory.GetFiles(Path.GetDirectoryName(path),"slot-*.json").ToDictionary(p=>p,p=>File.ReadAllBytes(p)):null;
            var export=View(b,"/business-log/export");File.WriteAllText(path,export.log_json);byte[] raw=File.ReadAllBytes(path);var loaded=View(b,"/business-log?path="+Uri.EscapeDataString(path)+"&offset=0");Check(loaded.total==b.Journal.Document.operations.Count && loaded.notes.Contains("リプレイ検証"),"Imported replay projection");Check(before==JsonUtility.ToJson(b.Simulation.State) && raw.SequenceEqual(File.ReadAllBytes(path)),"Replay mutated live/source file");if(files!=null)foreach(var f in files)Check(f.Value.SequenceEqual(File.ReadAllBytes(f.Key)),"Replay rewrote gameplay save");
        }
        public static int Run(string root)
        {
            checks=0;Directory.CreateDirectory(root);
            foreach(string mode in new[]{"free","popularity","bond","patron"})
            {
                var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.starting_funds=20000;rules.store_events.probability=0;rules.growth.threshold=1000;rules.opening_ticks=6;rules.exchange.ticks=2;rules.arrival_ticks=new[]{0,0};rules.patterns=Enumerable.Range(0,7).Select(i=>new WeekPattern {days=Enumerable.Range(0,7).ToArray()}).ToArray();
                string folder=Path.Combine(root,mode);var b=new LocalCafeBackend(folder,rules);string path=Path.Combine(folder,"replay.json");Directory.CreateDirectory(folder);
                if(File.Exists(path)){string state=JsonUtility.ToJson(b.Simulation.State);View(b,"/business-log?path="+Uri.EscapeDataString(path));Check(state==JsonUtility.ToJson(b.Simulation.State),"Cross-process replay touched new game");}
                Good(b,Command(b,"new_game",mode));Check(View(b,"/business-log").total==0,"New game did not reset base");
                Good(b,Command(b,"set_auto_assignment","manual"));Good(b,Command(b,"start_business"));Good(b,Command(b,"assign_service",cat:"cat-mugi",customer:"guest-1",seat:"seat-1"));Good(b,Command(b,"assign_service",cat:"cat-sora",customer:"guest-2",seat:"seat-2"));VerifyLog(b,path);
                var command=Command(b,"advance_business");var first=Request(b,"/commands",JsonUtility.ToJson(command));Check(first.result==UnityWebRequest.Result.Success,"Advance");int count=b.Journal.Document.operations.Count;Check(Request(b,"/commands",JsonUtility.ToJson(command)).text==first.text && count==b.Journal.Document.operations.Count,"Duplicate log row");command.expected_revision--;Request(b,"/commands",JsonUtility.ToJson(command));Check(count==b.Journal.Document.operations.Count,"Rejected request logged");
                while(b.Simulation.State.phase!="closed")Good(b,Command(b,"advance_business"));Good(b,Command(b,"next_day"));Good(b,Command(b,"player_begin",cat:"cat-mugi"));Good(b,Command(b,"player_step","direct"));Good(b,Command(b,"player_finish"));VerifyLog(b,path);Check(b.Journal.Document.operations.Any(r=>r.details.Contains("反応：") && r.details.IndexOf("反応：")<r.details.IndexOf("操作の記録")),"Action/reaction detail missing");
                if(mode!="free"){Good(b,Command(b,"start_autoplay",mode));Good(b,Command(b,"step_autoplay"));VerifyLog(b,path);Good(b,Command(b,"save_game",slot:2));VerifyLog(b,path);Good(b,Command(b,"resume_autoplay"));Good(b,Command(b,"step_autoplay"));Good(b,Command(b,"stop_autoplay"));VerifyLog(b,path);}
                else Good(b,Command(b,"save_game",slot:2));
                var current=View(b,"/business-log?offset=999999");Check(current.offset==(current.total-1)/20*20,"Page clamp");
                foreach(string route in new[]{"/business-log?offset=-1","/business-log?offset=oops","/business-log?extra=x","/business-log?path=relative.json","/business-log/export?path=x"})Check(Request(b,route).result!=UnityWebRequest.Result.Success,"Bad log request accepted");
                string json=View(b,"/business-log/export").log_json;var data=JsonUtility.FromJson<CafeReplayDocument>(json);data.final_digest="bad";File.WriteAllText(Path.Combine(folder,"corrupt.json"),JsonUtility.ToJson(data));Check(Request(b,"/business-log?path="+Uri.EscapeDataString(Path.Combine(folder,"corrupt.json"))).result!=UnityWebRequest.Result.Success,"Corrupt replay accepted");
                Good(b,Command(b,"load_game",slot:2));Check(View(b,"/business-log").total==0,"Load did not start new segment");if(b.Simulation.State.autoplay?.running==true)Good(b,Command(b,"stop_autoplay"));else Good(b,Command(b,"set_auto_assignment","automatic"));VerifyLog(b,Path.Combine(folder,"resumed.json"));
            }
            // Exercise two complete 20-row pages and immutable saved-file paging.
            var paging=new LocalCafeBackend(Path.Combine(root,"paging"));for(int i=0;i<43;i++)Good(paging,Command(paging,"set_auto_assignment",i%2==0?"automatic":"manual"));Check(View(paging,"/business-log").rows.Length==20 && View(paging,"/business-log?offset=20").rows[0].index==20 && View(paging,"/business-log?offset=99999").offset==40,"Paging boundaries");
            return checks;
        }
        public static void Verify(){string root=Environment.GetEnvironmentVariable("CATCAFE_REPLAY_SMOKE")??Path.Combine(Application.temporaryCachePath,"replay-smoke");int count=Run(root);File.WriteAllText(Path.Combine(root,"result.json"),"{\"checks\":"+count+"}");Debug.Log("Standalone replay PASS: "+count);}
    }
}
