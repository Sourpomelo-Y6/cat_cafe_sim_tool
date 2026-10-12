using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneLegacyImportSmoke
    {
        [Serializable] private class Cases {public Case[] cases;}
        [Serializable] private class Case {public string variant,bundle;public Operation[] operations;}
        [Serializable] private class Operation {public string kind,source,choice,cat_id;}
        private static int checks;
        private static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
        private static CafeResponse Request(LocalCafeBackend b,string route,string body=null){CafeResponse r=null;var work=b.Request(route,body,x=>r=x);while(work.MoveNext()){}return r;}
        private static PythonShiftCommand Command(LocalCafeBackend b,string kind,int slot=1,string path=null)=>new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,save_slot=slot,save_id=path??slot.ToString(),working_cats=new string[0]};
        private static void Good(LocalCafeBackend b,PythonShiftCommand c){var r=Request(b,"/commands",JsonUtility.ToJson(c));Check(r.result==UnityWebRequest.Result.Success,c.kind+": "+r.error);}
        private static void VerifyLog(LocalCafeBackend b,string folder)
        {
            var log=Request(b,"/business-log/export");Check(log.result==UnityWebRequest.Result.Success,"Imported business log export");string logPath=Path.Combine(folder,"import-log.json");File.WriteAllText(logPath,JsonUtility.FromJson<PythonBusinessLogView>(log.text).log_json);Check(Request(b,"/business-log?path="+Uri.EscapeDataString(logPath)).result==UnityWebRequest.Result.Success,"Imported business log replay");
        }
        public static int Run(string root)
        {
            string manifest=Environment.GetEnvironmentVariable("CATCAFE_LEGACY_FIXTURES");if(string.IsNullOrEmpty(manifest))return 0;
            checks=0;Directory.CreateDirectory(root);
            foreach(var test in JsonUtility.FromJson<Cases>(File.ReadAllText(manifest)).cases)
            {
                string folder=Path.Combine(root,test.variant);Directory.CreateDirectory(folder);var b=new LocalCafeBackend(folder);
                byte[] original=File.ReadAllBytes(test.bundle);
                if(File.Exists(Path.Combine(folder,"slot-2.json"))){Good(b,Command(b,"load_game",2));Check(b.Simulation.State.import_history!=null,"Cross-process import anchor missing");}
                // Separate runs use a fresh empty slot, while slot 2 verifies restart persistence.
                string before=JsonUtility.ToJson(b.Simulation.State);var preview=Request(b,"/legacy-import?path="+Uri.EscapeDataString(test.bundle));Check(preview.result==UnityWebRequest.Result.Success,"Import preview failed");
                var view=JsonUtility.FromJson<PythonLegacyImportView>(preview.text);int slot=view.free_slots.First();
                Check(before==JsonUtility.ToJson(b.Simulation.State) && original.SequenceEqual(File.ReadAllBytes(test.bundle)),"Preview mutated input/state");
                Good(b,Command(b,"import_legacy_save",slot,test.bundle));string saved=File.ReadAllText(Path.Combine(folder,"slot-"+slot+".json"));string live=JsonUtility.ToJson(b.Simulation.State);
                Check(Request(b,"/commands",JsonUtility.ToJson(Command(b,"import_legacy_save",slot,test.bundle))).result!=UnityWebRequest.Result.Success && live==JsonUtility.ToJson(b.Simulation.State) && saved==File.ReadAllText(Path.Combine(folder,"slot-"+slot+".json")),"Existing import slot overwritten");
                for(int index=0;index<test.operations.Length;index++){var op=test.operations[index];var command=Command(b,op.kind);command.event_id=op.source;command.choice=op.choice;command.cat_id=op.cat_id;Good(b,command);if(index==0 || index==test.operations.Length-1)VerifyLog(b,folder);Good(b,Command(b,"save_game",2));Good(b,Command(b,"load_game",2));}
                Check(original.SequenceEqual(File.ReadAllBytes(test.bundle)),"Import source modified");
                VerifyLog(b,folder);
            }
            return checks;
        }
        public static void Verify(){string root=Environment.GetEnvironmentVariable("CATCAFE_LEGACY_SMOKE")??Path.Combine(Application.temporaryCachePath,"legacy-import-smoke");int count=Run(root);File.WriteAllText(Path.Combine(root,"result.json"),"{\"checks\":"+count+"}");Debug.Log("Standalone legacy import PASS: "+count);}
    }
}
