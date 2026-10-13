using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    [Serializable] public class DailyReference{public DailyReferenceCase[] cases;}
    [Serializable] public class DailyReferenceCase{public int seed;public string mode,choice,skip;public DailyReferenceCat[] cats;}
    [Serializable] public class DailyReferenceCat{public string id,text;}
    public static class StandaloneDailySmoke
    {
        private static int checks;
        private static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
        private static CafeResponse Send(LocalCafeBackend b,string kind,string choice=null,string cat=null,string[] working=null,int slot=2)
        {
            var s=b.Simulation.State;CafeResponse reply=null;var op=b.Request("/commands",JsonUtility.ToJson(new PythonShiftCommand{kind=kind,choice=choice,cat_id=cat,working_cats=working,event_id=kind=="resolve_activity"?s.dispatches.Single().id:null,save_slot=slot,save_id=slot.ToString(),instance_id=s.instance_id,expected_revision=s.revision,request_id=Guid.NewGuid().ToString("N")}),r=>reply=r);while(op.MoveNext()){}return reply;
        }
        private static void Command(LocalCafeBackend b,string kind,string choice=null,string cat=null,string[] working=null,int slot=2){var r=Send(b,kind,choice,cat,working,slot);Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        public static void Run(string output,string reference)
        {
            Directory.CreateDirectory(output);checks=0;int cases=0;var refs=JsonUtility.FromJson<DailyReference>(File.ReadAllText(reference));
            foreach(var row in refs.cases)
            {
                if(!string.IsNullOrEmpty(row.skip))continue;string folder=Path.Combine(output,row.seed+"-"+row.mode);var b=new LocalCafeBackend(folder);var sim=b.Simulation;sim.Restore(new CafeSimulation(sim.Rules,row.seed).State);
                if(new[]{"recover","recovering","risk","item"}.Contains(row.mode))foreach(var c in sim.State.cats){c.fatigue=80;if(row.mode!="risk" && row.mode!="item"){c.health="sick";c.recovery_remaining=row.mode=="recover"?1:2;}}
                if(row.mode=="runaway")foreach(var c in sim.State.cats)c.stress=80;
                b.DailyRecords.Reset(true);if(!string.IsNullOrEmpty(row.choice))Command(b,"resolve_store_event",row.choice);if(row.mode=="item"){Command(b,"purchase_item",sim.Rules.item_shop.First(r=>r.item.fatigue_relief>0).item.id);Command(b,"use_item","item-purchase-1","cat-mike");}if(row.mode=="purchase")Command(b,"purchase_cat",cat:sim.Rules.pet_shop[0].cat.id);if(row.mode=="rest")Command(b,"set_shifts",working:new[]{"cat-mike","cat-tama"});if(new[]{"dispatch","received"}.Contains(row.mode))Command(b,"dispatch",sim.Rules.dispatch.id,cat:"cat-sora");
                if(new[]{"off","recover","recovering","risk"}.Contains(row.mode))Command(b,"day_off");else{Command(b,"start_business");while(sim.State.phase!="closed"){if(sim.State.tick==10){Command(b,"save_game");Command(b,"load_game");}Command(b,"advance_business");}}
                if(row.mode=="received")Command(b,"resolve_activity","receive",cat:null);
                foreach(var c in row.cats){Check(b.DailyRecords.Find(1,c.id)?.Text()==c.text,row.seed+" "+row.mode+" "+c.id+" Python daily row differs");Check(b.Details.History(c.id).Contains(c.text),"daily row not projected to actual UI");}
                // Synthetic health fixtures intentionally bypass gameplay setup; production save validation is tested on genuine games.
                if(new[]{"work","rest","off","dispatch","received","purchase"}.Contains(row.mode))
                {
                    string records=JsonUtility.ToJson(new ReceiptList{rows=b.DailyRecords.Records.ToArray()});Command(b,"save_game");var reopened=new LocalCafeBackend(folder);Command(reopened,"load_game");Check(JsonUtility.ToJson(new ReceiptList{rows=reopened.DailyRecords.Records.ToArray()})==records,"new backend reload loses day facts");
                    string path=Path.Combine(folder,"slot-2.json"),goodSave=File.ReadAllText(path);var badRow=JsonUtility.FromJson<CafeSaveEnvelope>(goodSave);badRow.daily_records[0].spent="NaN";File.WriteAllText(path,JsonUtility.ToJson(badRow));string unchanged=JsonUtility.ToJson(reopened.Simulation.State);Check(Send(reopened,"load_game").result!=UnityWebRequest.Result.Success,"invalid day receipt accepted");Check(unchanged==JsonUtility.ToJson(reopened.Simulation.State),"invalid day receipt changed game");
                    var save=JsonUtility.FromJson<CafeSaveEnvelope>(goodSave);save.daily_baselines=new[]{new CafeDailyBaseline{day=999,cat_id="cat-mike",fatigue="NaN"}};File.WriteAllText(Path.Combine(folder,"slot-2.json"),JsonUtility.ToJson(save));string before=JsonUtility.ToJson(reopened.Simulation.State);Check(Send(reopened,"load_game").result!=UnityWebRequest.Result.Success,"invalid day baseline accepted");Check(before==JsonUtility.ToJson(reopened.Simulation.State),"failed load changed current game");
                }
                cases++;
            }
            var fresh=new LocalCafeBackend(Path.Combine(output,"new-game"));Command(fresh,"new_game","free");Command(fresh,"load_game",slot:1);Check(fresh.DailyRecords.Baselines.Count==5,"automatic first save loses baseline");Command(fresh,"day_off");Check(fresh.DailyRecords.Records.All(r=>r.fatigue!="記録なし"),"first autosave reload missing fatigue");Command(fresh,"save_game");Command(fresh,"new_game","free");Check(fresh.DailyRecords.Records.Count==0,"new game inherits previous rows");Command(fresh,"load_game");Check(fresh.DailyRecords.Records.Count==5,"other slot lost previous rows");
            var oldEnvelope=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(output,"new-game","slot-2.json")));oldEnvelope.slot=3;oldEnvelope.daily_records=null;oldEnvelope.daily_baselines=null;File.WriteAllText(Path.Combine(output,"new-game","slot-3.json"),JsonUtility.ToJson(oldEnvelope));Command(fresh,"load_game",slot:3);Check(fresh.DailyRecords.Records.Count==0 && fresh.DailyRecords.Baselines.Count==0,"old save values were fabricated");Command(fresh,"day_off");Check(fresh.DailyRecords.Records.Count==0,"old partial day was fabricated");Command(fresh,"day_off");Check(fresh.DailyRecords.Records.Count==5 && fresh.DailyRecords.Records.All(r=>r.fatigue!="記録なし"),"old save future day not recorded");
            File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Report{passed=true,cases=cases,checks=checks},true));Debug.Log("CAT_CAFE_DAILY_RECORDS_OK "+cases+" cases "+checks+" checks");
        }
        [Serializable] private class ReceiptList{public CafeDailyRecord[] rows;}
        [Serializable] private class Report{public bool passed;public int cases,checks;}
    }
}
