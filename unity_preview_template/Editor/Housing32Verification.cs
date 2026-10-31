using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor
{
    // Dedicated fixtures only. No persistentDataPath access and no scene edits.
    public static class Housing32Verification
    {
        static int checks;
        static readonly string Output="C:/Users/hi-wa/source/codex_repos/CatCafeSim/Logs/Housing32/Unity";
        static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
        static string Encode(CafeSaveEnvelope s){if(s.interaction_statistics==null)s.interaction_statistics=CafeInteractionStatistics.Begin(s.state,false);s.double_bits=CafeDoublePrecision.Capture(s.state);return JsonUtility.ToJson(s,true);}
        static CafeSnapshot Copy(CafeSnapshot state){var s=JsonUtility.FromJson<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=state}));s.state.NormalizeJsonStrings();CafeDoublePrecision.Restore(s.state,s.double_bits);return s.state;}
        static CafeRules Rules(string name){var r=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/"+name).text);r.stamina_growth=JsonUtility.FromJson<CafeStaminaRules>(Resources.Load<TextAsset>("CatCafeStandalone/stamina_growth").text);return r;}
        static void Send(LocalCafeBackend b,string kind,int slot=1,string choice=null){var s=b.Simulation.State;CafeResponse reply=null;var it=b.Request("/commands",JsonUtility.ToJson(new PythonShiftCommand{kind=kind,choice=choice,save_slot=slot,save_id=slot.ToString(),instance_id=s.instance_id,expected_revision=s.revision,request_id=Guid.NewGuid().ToString("N"),working_cats=new string[0]}),r=>reply=r);while(it.MoveNext()){}Check(reply!=null && reply.result==UnityWebRequest.Result.Success,kind+": "+reply?.error);}
        static void Save(LocalCafeBackend b,CafeSnapshot state,int slot){b.Simulation.Restore(Copy(state));typeof(LocalCafeBackend).GetMethod("ResetJournal",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,null);Send(b,"save_game",slot);}
        static void Step(CafeSimulation sim)
        {
            var s=sim.State;
            if(s.game_over!=null)throw new Exception("Fixture game over at day "+s.day);
            if(sim.GrowthWaiting){sim.Execute(s.growth.Any(g=>sim.GrowthPendingKind(g)!=null && sim.GrowthSelection(g)==null)?"resolve_growth_auto":"resolve_growth_batch");return;}
            if(sim.StoreEventWaiting){sim.Execute("resolve_store_event",choice:sim.CurrentStoreEvent.type=="trouble"?"patch":"decline");return;}
            var missing=s.cats.FirstOrDefault(c=>c.activity=="missing" && c.missing_remaining==0);if(missing!=null){sim.Execute("resolve_missing",catId:missing.id);return;}
            if(sim.IntakeWaiting){sim.Execute("resolve_intake",choice:"decline");return;}
            if(sim.RegularIntroductionWaiting){sim.Execute("resolve_regular_introduction",choice:"decline");return;}
            if(sim.CustomerTrustWaiting){sim.Execute("resolve_customer_trust",choice:"recover",source:s.trust_events.First(e=>e.status=="waiting").id);return;}
            if(sim.ReservationWaiting){sim.Execute("resolve_reservation",choice:"decline");return;}
            var op=sim.PlanAutoplay("popularity","clear");if(op==null)throw new Exception("Fixture planner blocked");
            sim.Execute(op.kind,op.working,op.cat_id,op.choice,op.source,op.seat_id);
        }
        static void Ready(CafeSimulation s,Func<bool> condition)
        {for(int i=0;i<20000;i++){if(condition())return;Step(s);}throw new Exception("Fixture operation budget exceeded");}
        public static void VerifyAndBuild(){Run();StandaloneBuildStorage.Build(StandaloneScreenSetup.Scenes);}
        public static void Run()
        {
            Directory.CreateDirectory(Output);checks=0;
            // Scene/profile initialization, including Extra, is confined to this QA root.
            string saves=Path.Combine(Output,"profile/standalone-v1");Directory.CreateDirectory(saves);
            var legacy=new CafeSimulation(Rules("housing_legacy_rules"));
            for(int stage=0;stage<=2;stage++)
            {
                if(stage==1)legacy.Execute("purchase_housing");
                if(stage==2){Ready(legacy,()=>legacy.HousingReason("upgrade_housing")=="" && legacy.State.funds>12000);legacy.Execute("upgrade_housing");}
                legacy.Validate(legacy.State);string text=Encode(new CafeSaveEnvelope{state=legacy.State,slot=7+stage,created_at=DateTimeOffset.UtcNow.ToString("o")});string path=Path.Combine(saves,"slot-"+(7+stage)+".json");File.WriteAllText(path,text);
                var current=new LocalCafeBackend(saves);Send(current,"load_game",7+stage);Check(current.View().housing.capacity==new[]{6,9,12}[stage],"old capacity "+stage);Check(File.ReadAllText(path)==text,"legacy read preserved original");Send(current,"save_game",3+stage);var fresh=new LocalCafeBackend(saves);Send(fresh,"load_game",3+stage);Check(fresh.View().housing.capacity==new[]{6,9,12}[stage],"migrated JsonUtility resume "+stage);
            }
            var migration=new LocalCafeBackend(saves);Send(migration,"load_game",9);Save(migration,migration.Simulation.State,2);
            var full=new CafeSimulation(Rules("rules"));full.Restore(Copy(migration.Simulation.State));
            for(int i=0;i<4;i++){Check(full.HousingReason("extend_housing")=="","extension available");full.Execute("extend_housing");full.Validate(full.State);}
            Ready(full,()=>full.State.phase=="preparation" && full.ItemsReason()=="");
            for(int i=0;i<20000 && full.ActiveCatCount(full.State)<32;i++)
            {
                bool acted=false;
                if(full.State.phase=="preparation" && full.ItemsReason()=="" && full.State.funds>2000)
                {
                    if(full.RecruitmentRefreshReason()==""){full.Execute("open_recruitment");acted=true;}
                    var row=full.RecruitmentCandidates(full.State).FirstOrDefault(c=>full.RecruitReason(c.cat.id)=="" && full.State.funds-c.cost>1000);
                    if(row!=null){full.Execute("recruit_cat",catId:row.cat.id);acted=true;}
                }
                if(!acted)Step(full);
                if(full.State.day>180)throw new Exception("Collection day budget exceeded");
            }
            full.Validate(full.State);Check(full.ActiveCatCount(full.State)==32,"32 cats acquired through standard commands");
            var display=new LocalCafeBackend(saves);Save(display,full.State,1);var restarted=new LocalCafeBackend(saves);Send(restarted,"load_game",1);Check(restarted.View().housing.count==32 && restarted.View().housing.stage==6 && !restarted.View().housing.can_expand,"32-cat JsonUtility disk resume");
            File.WriteAllText(Path.Combine(Output,"editor-result.txt"),"PASS: "+checks+" checks; standard-rule legacy 6/9/12 saves, read-only migration, JsonUtility disk resume, standard-command expansion/recruitment to 32. UI and Player resume checked separately.\nDay="+full.State.day+" Funds="+full.State.funds);
            Debug.Log("CAT_CAFE_HOUSING32_EDITOR_OK: "+checks);
        }
    }
}
