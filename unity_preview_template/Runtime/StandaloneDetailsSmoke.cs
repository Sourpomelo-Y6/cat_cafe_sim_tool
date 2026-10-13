using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
namespace CatCafePreview
{
    [Serializable] public class DetailReferences{public DetailReferenceCase[] cases;public DetailReferenceName[] names;}
    [Serializable] public class DetailReferenceCase{public string bundle;public DetailReferenceCat[] cats;}
    [Serializable] public class DetailReferenceCat{public string id,profile,forecast,content_id;public DetailReferencePair[] relationships;public DetailReferenceDay[] history;}
    [Serializable] public class DetailReferencePair{public string id,affinity,stage;}
    [Serializable] public class DetailReferenceDay{public int day;public string ticks;}
    [Serializable] public class DetailReferenceName{public string id,name;}
    public static class StandaloneDetailsSmoke
    {
        private static int checks;
        private static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
        private static CafeResponse Request(LocalCafeBackend backend,string route,string body=null){CafeResponse result=null;var operation=backend.Request(route,body,r=>result=r);while(operation.MoveNext()){}return result;}
        private static void Send(LocalCafeBackend backend,string kind,int slot,string path=null){var state=backend.Simulation.State;var reply=Request(backend,"/commands",JsonUtility.ToJson(new PythonShiftCommand{request_id=Guid.NewGuid().ToString("N"),instance_id=state.instance_id,expected_revision=state.revision,kind=kind,choice=kind=="new_game"?"free":"",save_slot=slot,save_id=path??slot.ToString(),working_cats=new string[0]}));Check(reply.result==UnityWebRequest.Result.Success,kind+": "+reply.error);}
        public static int Run(string output,string reference)
        {
            checks=0;Directory.CreateDirectory(output);var fixtures=JsonUtility.FromJson<DetailReferences>(File.ReadAllText(reference));int index=0,cats=0;
            foreach(var row in fixtures.cases)
            {
                var backend=new LocalCafeBackend(Path.Combine(output,"case-"+index++));Send(backend,"import_legacy_save",1,row.bundle);string before=JsonUtility.ToJson(backend.Simulation.State);var view=backend.View();
                foreach(var expected in row.cats)
                {
                    var cat=view.cats.Single(c=>c.cat_id==expected.id);string label=row.bundle+" "+cat.cat_id;Check(cat.profile_details==expected.profile,label+" profile");Check(cat.shift_forecast==expected.forecast,label+" forecast");Check(cat.content_cat_id==expected.content_id,label+" content mapping");
                    foreach(var pair in expected.relationships)Check(cat.records.relationships.Contains(backend.Details.CustomerName(pair.id)+"（"+pair.id+"）\n確定済み親しみ："+pair.affinity+" / "+pair.stage),label+" relationship");
                    foreach(var day in expected.history){var sections=cat.records.history.Split(new[]{"日目："+day.day+"\n"},StringSplitOptions.None);Check(sections.Length==2 && sections[1].Split(new[]{"\n\n"},StringSplitOptions.None)[0].Contains("接客行動数："+day.ticks+"\n"),label+" history");}cats++;
                }
                Check(before==JsonUtility.ToJson(backend.Simulation.State),"Viewing changed game state");Send(backend,"save_game",2);Send(backend,"load_game",2);var loaded=backend.View();
                foreach(var cat in view.cats){var restored=loaded.cats.Single(c=>c.cat_id==cat.cat_id);Check(cat.profile_details==restored.profile_details && cat.shift_forecast==restored.shift_forecast && JsonUtility.ToJson(cat.records)==JsonUtility.ToJson(restored.records),"Unity JSON reload changed cat details");}
            }
            var native=new LocalCafeBackend(Path.Combine(output,"native-history"));Send(native,"new_game",1);Send(native,"start_business",1);while(native.Simulation.State.phase!="closed")Send(native,"advance_business",1);
            Check(native.View().cats.Single(c=>c.cat_id=="cat-tama").records.history.Contains("体力消耗：83"),"Native closed net stamina");Send(native,"next_day",1);Check(native.View().cats.Single(c=>c.cat_id=="cat-tama").records.history.Contains("体力消耗：83"),"Native next day lost known net stamina");Send(native,"save_game",3);Send(native,"load_game",3);Check(native.View().cats.Single(c=>c.cat_id=="cat-tama").records.history.Contains("体力消耗：83"),"Native save reload lost known net stamina");
            var file=Path.Combine(output,"native-history/slot-3.json");var saved=JsonUtility.FromJson<Standalone.CafeSaveEnvelope>(File.ReadAllText(file));saved.display_receipts[0].net_stamina="NaN";File.WriteAllText(file,JsonUtility.ToJson(saved));var live=native.Simulation.State;string untouched=JsonUtility.ToJson(live);var rejected=Request(native,"/commands",JsonUtility.ToJson(new PythonShiftCommand{request_id=Guid.NewGuid().ToString("N"),instance_id=live.instance_id,expected_revision=live.revision,kind="load_game",save_id="3",working_cats=new string[0]}));Check(rejected.result!=UnityWebRequest.Result.Success && rejected.error.Contains("表示記録") && JsonUtility.ToJson(native.Simulation.State)==untouched,"Bad display metadata changed live game");
            string oldSave=Environment.GetEnvironmentVariable("CATCAFE_OLD_SAVE_REFERENCE");if(!string.IsNullOrEmpty(oldSave)){var oldDirectory=Path.Combine(output,"old-save");Directory.CreateDirectory(oldDirectory);File.Copy(oldSave,Path.Combine(oldDirectory,"slot-2.json"));var oldBackend=new LocalCafeBackend(oldDirectory);Send(oldBackend,"load_game",2);Check(oldBackend.Simulation.State.phase=="closed" && oldBackend.Simulation.State.funds==1730,"Old save without display metadata rejected");}
            var sample=new LocalCafeBackend(Path.Combine(output,"names"));foreach(var row in fixtures.names)Check(sample.Details.CustomerName(row.id)==row.name,"Customer name "+row.id);
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":true,\"cases\":"+index+",\"cats\":"+cats+",\"checks\":"+checks+"}");return checks;
        }
    }
}
