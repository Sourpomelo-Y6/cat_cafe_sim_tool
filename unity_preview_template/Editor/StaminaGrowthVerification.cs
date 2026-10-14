using System;
using System.IO;
using System.Linq;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor
{
    public static class StaminaGrowthVerification
    {
        public static void Verify()
        {
            int checks=0;void Check(bool ok){checks++;if(!ok)throw new Exception("Stamina verification failed: "+checks);}
            var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
            rules.stamina_growth=JsonUtility.FromJson<CafeStaminaRules>(Resources.Load<TextAsset>("CatCafeStandalone/stamina_growth").text);
            rules.starting_funds=10000000;rules.store_events.probability=0;
            var sim=new CafeSimulation(rules);
            string Encode(CafeSaveEnvelope value){value.double_bits=CafeDoublePrecision.Capture(value.state);return JsonUtility.ToJson(value);}
            CafeSaveEnvelope Decode(string json){var value=JsonUtility.FromJson<CafeSaveEnvelope>(json);value.state.NormalizeJsonStrings();CafeDoublePrecision.Restore(value.state,value.double_bits);return value;}
            string output=Environment.GetEnvironmentVariable("CATCAFE_STAMINA_OUTPUT");if(string.IsNullOrEmpty(output))throw new Exception("CATCAFE_STAMINA_OUTPUT required");Directory.CreateDirectory(output);
            var store=new CafeSaveStore(output,Encode,Decode,sim.Validate);
            for(int i=1;i<=21;i++)
            {
                if(sim.IntakeWaiting)sim.Execute("resolve_intake",choice:"decline");
                foreach(var g in sim.State.growth.ToArray())if(sim.GrowthPendingKind(g)=="resolve_growth")sim.Execute("resolve_growth",catId:g.cat_id,choice:"rest");
                sim.Execute("day_off");var beforeState=sim.State;string beforeJson=JsonUtility.ToJson(sim.State);var digest=CafeReplayJournal.Digest(sim.State);store.Save(1,sim.State);sim.Restore(store.Load(1).state);
                if(CafeReplayJournal.Digest(sim.State)!=digest){foreach(var field in typeof(CafeSnapshot).GetFields()){var a=new CafeSnapshot();var b=new CafeSnapshot();field.SetValue(a,field.GetValue(beforeState));field.SetValue(b,field.GetValue(sim.State));if(CafeReplayJournal.Digest(a)!=CafeReplayJournal.Digest(b))Debug.LogError("Changed field: "+field.Name+" before null="+(field.GetValue(beforeState)==null)+" after null="+(field.GetValue(sim.State)==null));}File.WriteAllText(Path.Combine(output,"before.json"),beforeJson);File.WriteAllText(Path.Combine(output,"after.json"),JsonUtility.ToJson(sim.State));}
                Check(CafeReplayJournal.Digest(sim.State)==digest);Check(sim.State.cats.All(c=>sim.MaxStamina(c.id)==(i<20?100:110)));
            }
            sim.Execute("player_begin",catId:"cat-tama");for(int i=0;i<4;i++)sim.Execute("player_step",choice:"pause");
            string history=sim.PlayerHistory(sim.ActivePlayer);store.Save(1,sim.State);sim.Restore(store.Load(1).state);Check(sim.ActivePlayer.max_stamina==110);Check(sim.PlayerHistory(sim.ActivePlayer)==history);
            sim.Execute("player_finish");store.Save(1,sim.State);sim.Restore(store.Load(1).state);Check(sim.State.cats.Single(c=>c.id=="cat-tama").stamina==110);
            // Actual pre-growth Unity precision tape: same zero additions must not shift existing slots.
            var old=new CafeSimulation(rules);old.State.stamina_growth=null;
            string encoded=Encode(new CafeSaveEnvelope{state=old.State});var restored=Decode(encoded);old.Restore(restored.state);Check(old.MaxStamina("cat-tama")==100);
            var journal=new CafeReplayJournal(rules,sim.State,Encode,Decode);
            void Run(CafeReplayOperation op){var before=journal.CaptureBefore(sim.State);CafeReplayJournal.Apply(sim,op);journal.Append(before,sim,op);}
            Run(new CafeReplayOperation{kind="player_begin",cat_id="cat-sora"});Run(new CafeReplayOperation{kind="player_step",choice="pause"});Run(new CafeReplayOperation{kind="player_finish"});
            Check(CafeReplayJournal.Digest(journal.Verify(JsonUtility.FromJson<CafeReplayDocument>(JsonUtility.ToJson(journal.Document))).State)==CafeReplayJournal.Digest(sim.State));
            string legacyRules=Environment.GetEnvironmentVariable("CATCAFE_STAMINA_LEGACY_RULES"),legacyFolder=Environment.GetEnvironmentVariable("CATCAFE_STAMINA_LEGACY_REPLAYS");
            if(!string.IsNullOrEmpty(legacyRules) && !string.IsNullOrEmpty(legacyFolder))
            {
                var oldRules=JsonUtility.FromJson<CafeRules>(File.ReadAllText(legacyRules));
                foreach(string file in new[]{"replay.json","resumed.json"})
                {var tape=JsonUtility.FromJson<CafeReplayDocument>(File.ReadAllText(Path.Combine(legacyFolder,file)));var verifier=new CafeReplayJournal(oldRules,new CafeSimulation(oldRules).State,Encode,Decode);Check(CafeReplayJournal.Digest(verifier.Verify(tape).State)==tape.final_digest);}
            }
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"success\":true,\"checks\":"+checks+"}");Debug.Log("PASS stamina Unity: "+checks);
        }
    }
}
