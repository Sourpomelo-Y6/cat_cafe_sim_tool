using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneAutoplaySmoke
    {
        private static int checks;
        private static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
        private static PythonShiftCommand Command(LocalCafeBackend b,string kind,int slot=1,string target="popularity",string mode="basic",int days=2)=>new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=target,autoplay_objective=target,autoplay_mode=mode,autoplay_days=days,save_slot=slot,save_id=slot.ToString(),working_cats=new string[0]};
        private static CafeResponse Send(LocalCafeBackend b,PythonShiftCommand c){CafeResponse reply=null;var request=b.Request("/commands",JsonUtility.ToJson(c),r=>reply=r);while(request.MoveNext()){}return reply;}
        private static void Good(LocalCafeBackend b,string kind,int slot=1,string target="popularity",string mode="basic",int days=2){var r=Send(b,Command(b,kind,slot,target,mode,days));Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static void Bad(LocalCafeBackend b,PythonShiftCommand c){string before=JsonUtility.ToJson(b.Simulation.State);Check(Send(b,c).result!=UnityWebRequest.Result.Success,"Rejected operation succeeded");Check(JsonUtility.ToJson(b.Simulation.State)==before,"Rejected operation mutated state");}
        public static int Run(string root)
        {
            checks=0;
            foreach(string target in new[]{"popularity","bond","patron"})foreach(string mode in new[]{"basic","clear","fast"})
            {
                var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.opening_ticks=8;rules.arrival_ticks=new[]{0,0,0};rules.patterns=Enumerable.Range(0,7).Select(i=>new WeekPattern {days=Enumerable.Range(0,7).ToArray()}).ToArray();rules.store_events.probability=0;rules.growth.threshold=1000;rules.starting_funds=20000;
                string directory=Path.Combine(root,"autoplay-"+target+"-"+mode);var b=new LocalCafeBackend(directory,rules);
                if(File.Exists(Path.Combine(directory,"slot-3.json"))){Good(b,"load_game",3);Check(b.View().autoplay.running && b.View().autoplay.paused,"Restart must pause execution");Check(b.Simulation.State.auto_assignment,"Restart active assignment");Good(b,"stop_autoplay");Check(!b.Simulation.State.auto_assignment,"Restart cancel restores setting");}
                Good(b,"new_game",target:target);var setting=Command(b,"set_auto_assignment");setting.choice="manual";Check(Send(b,setting).result==UnityWebRequest.Result.Success,"manual setting");
                Bad(b,Command(b,"start_autoplay",target:target,days:0));Bad(b,Command(b,"start_autoplay",target:target,mode:"invalid"));
                Good(b,"start_autoplay",target:target,mode:mode);Check(b.View().autoplay.running && b.Simulation.State.auto_assignment,"Autoplay start");
                Good(b,"save_game",2);Check(b.View().autoplay.paused,"Save pauses");Bad(b,Command(b,"step_autoplay"));Good(b,"load_game",2);Check(b.View().autoplay.paused && b.Simulation.State.autoplay.operations==0,"Saved preparation run");Good(b,"resume_autoplay");
                Bad(b,Command(b,"day_off"));Bad(b,Command(b,"start_autoplay",target:target));
                var command=Command(b,"step_autoplay");var first=Send(b,command);Check(first.result==UnityWebRequest.Result.Success,"First step");string after=JsonUtility.ToJson(b.Simulation.State);Check(Send(b,command).text==first.text && JsonUtility.ToJson(b.Simulation.State)==after,"Duplicate progressed twice");command.kind="stop_autoplay";Bad(b,command);var stale=Command(b,"step_autoplay");stale.expected_revision--;Bad(b,stale);
                Good(b,"save_game",3);Good(b,"stop_autoplay");Check(!b.Simulation.State.auto_assignment,"Cancel restores preference");Good(b,"load_game",3);Good(b,"resume_autoplay");Good(b,"step_autoplay");Good(b,"pause_autoplay");Good(b,"save_game",4);Good(b,"load_game",4);Good(b,"resume_autoplay");
                int limit=0;while(b.Simulation.State.autoplay.running && limit++<600)Good(b,"step_autoplay");Check(limit<600,"Run exceeded bound");Check(!b.Simulation.State.auto_assignment,"Completion restores manual setting");Good(b,"save_game",5);Good(b,"load_game",5);Check(!b.View().autoplay.running && b.View().autoplay.report.operations>0,"Result saved");
                string report=b.View().autoplay.report.summary;Good(b,"set_auto_assignment",target:"automatic");Check(b.View().autoplay.report.summary==report,"Report changed after manual operation");
            }
            return checks;
        }
        public static void Verify(){string root=Environment.GetEnvironmentVariable("CATCAFE_AUTOPLAY_SMOKE")??Path.Combine(Application.temporaryCachePath,"autoplay-smoke");Directory.CreateDirectory(root);int count=Run(root);File.WriteAllText(Path.Combine(root,"result.json"),"{\"checks\":"+count+"}");Debug.Log("Standalone autoplay PASS: "+count);}
    }
}
