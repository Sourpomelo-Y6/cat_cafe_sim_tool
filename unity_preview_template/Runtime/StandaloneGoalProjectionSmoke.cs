using System;
using System.IO;
using System.Linq;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandaloneGoalProjectionSmoke
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-goal-projections");if(i<0)return;
            int observed=Array.IndexOf(args,"--catcafe-observed-goals");try{Run(args[i+1],null,observed<0?null:args[observed+1]);Application.Quit(0);}catch(Exception ex){Directory.CreateDirectory(args[i+1]);File.WriteAllText(Path.Combine(args[i+1],"failure.txt"),ex.ToString());Application.Quit(1);}
        }
        private static int checks;
        private static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
        public static void Run(string output,string completedSave=null,string observedRoot=null)
        {
            Directory.CreateDirectory(output);checks=0;
            foreach(string mode in new[]{"free","popularity","patron","bond"})
            {
                var b=new LocalCafeBackend(Path.Combine(output,mode));b.Simulation.Restore(new CafeSimulation(b.Simulation.Rules,0,mode).State);string before=JsonUtility.ToJson(b.Simulation.State);var view=b.View();Check(view.goal_result==null,"Active goal displayed as completed: "+mode);Check(view.next_goal==null,"Active goal offers next stage: "+mode);Check(view.objective_progress.mode==mode && view.objective_progress.status=="active" && !string.IsNullOrEmpty(view.objective_progress.details),"Missing active progress: "+mode);Check(before==JsonUtility.ToJson(b.Simulation.State),"Goal projection changes gameplay");
                if(mode=="free")continue;
                // Display contract boundary fixtures. They do not claim a played or validated clear.
                var s=b.Simulation.State;if(mode=="popularity"){s.phase="closed";s.goal[0].status="cleared";s.goal[0].resolved_day=s.day;s.popularity=250;}else if(mode=="patron"){s.patron.status="cleared";s.patron.resolved_day=1;}else{s.bond_goal.status="cleared";s.bond_goal.resolved_day=1;}
                view=b.View();string kind=mode=="popularity"?"continue_goal":mode=="patron"?"continue_patron":"continue_bond_goal";Check(view.goal_result!=null && view.goal_result.pending && view.goal_result.kind==kind && view.goal_result.resolved_day==1,"Wrong goal outcome route: "+mode);
                if(mode=="popularity"){Check(view.next_goal.started_day==2 && view.next_goal.deadline==21,"Next stage has zero or wrong dates");b.Simulation.Execute("continue_goal");Check(b.View().goal_result==null && b.View().next_goal!=null,"Confirmed result still blocks view");b.Simulation.Execute("advance_goal");Check(b.View().goal_result==null && b.View().next_goal==null && b.View().objective_progress.summary.Contains("第2段階"),"New stage still displays completed result");}
                else{b.Simulation.Execute(kind);Check(b.View().goal_result==null && b.View().objective_progress.summary.Contains("継続中"),"Other goal confirmation not reflected");}
            }
            if(!string.IsNullOrEmpty(completedSave))
            {
                var save=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(completedSave));save.state.NormalizeJsonStrings();CafeDoublePrecision.Restore(save.state,save.double_bits);var real=new LocalCafeBackend(Path.Combine(output,"observed-clear"));real.Simulation.Restore(save.state);var v=real.View();Check(v.goal_result?.status=="cleared" && v.goal_result.resolved_day==11 && v.next_goal.started_day==12 && v.next_goal.deadline==31,"Real played result failed restore");Check(v.objective_progress.details.Contains("期限：20日目") && v.objective_progress.summary.Contains("260 / 250"),"Real played progress differs");
            }
            if(!string.IsNullOrEmpty(observedRoot))foreach(string mode in new[]{"popularity","patron","bond"})
            {
                var final=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(observedRoot,mode,"result-final.json")));final.state.NormalizeJsonStrings();CafeDoublePrecision.Restore(final.state,final.double_bits);
                var real=new LocalCafeBackend(Path.Combine(output,"played-"+mode));real.Simulation.Restore(final.state);string digest=CafeLegacyImport.GameDigest(final.state);var result=real.View();
                Check(result.goal_result?.pending==true && result.goal_result.status=="cleared" && result.goal_result.resolved_day==final.state.day,"Played final result failed restore: "+mode);
                Check(result.next_goal==null && digest==CafeLegacyImport.GameDigest(final.state),"Final goal invents next stage or mutates state: "+mode);
                if(mode=="patron")Check(final.state.patron.satisfaction>=real.Simulation.Rules.patron.target && real.Simulation.Rules.patron.members.All(m=>final.state.patron.members.Single(v=>v.id==m.id).value>=m.target),"Played patron clear lacks satisfaction");
                if(mode=="bond")Check(real.Simulation.BondQualifying(final.state).Length>=real.Simulation.Rules.bond.target,"Played bond clear lacks qualifying cats");
                real.Simulation.Execute(result.goal_result.kind);Check(real.View().goal_result==null && real.View().objective_progress.summary.Contains("継続中"),"Played goal continuation still blocks: "+mode);
                File.WriteAllText(Path.Combine(output,"played-"+mode+".json"),JsonUtility.ToJson(new Played{mode=mode,day=final.state.day,funds=final.state.funds,popularity=final.state.popularity},true));
            }
            var patronProbe=new LocalCafeBackend(Path.Combine(output,"patron-rules"));patronProbe.Simulation.Restore(new CafeSimulation(patronProbe.Simulation.Rules,0,"patron").State);
            var terms=patronProbe.Simulation.PatronTerms(patronProbe.Simulation.State,"cat-sora","patron_moody_visit");File.WriteAllText(Path.Combine(output,"patron-rules.json"),JsonUtility.ToJson(new PatronRuleReport{rules=patronProbe.Simulation.Rules.patron,terms=terms,destinations=patronProbe.Simulation.PatronDestinations()},true));Check(terms.matched && terms.gain==20,"White cat does not match feature-only patron condition");Check(patronProbe.Simulation.DispatchDestination("patron_moody_visit").name=="気分が変わる有力者への訪問","Missing patron destination name");
            var missing=new LocalCafeBackend(Path.Combine(output,"missing"));var m=missing.Simulation.State.cats[0];m.activity="missing";m.working=false;m.missing_remaining=0;m.kitten=true;string untouched=JsonUtility.ToJson(missing.Simulation.State);var mv=missing.View();Check(mv.required_action=="missing_return" && mv.missing_cats.Length==1 && mv.missing_cats[0].can_resolve,"Missing return not projected");Check(mv.missing_cats[0].cost==missing.Simulation.Rules.kitten_cost,"Wrong kitten cost");Check(untouched==JsonUtility.ToJson(missing.Simulation.State),"Missing projection mutated live state");
            File.WriteAllText(Path.Combine(output,"projection-result.json"),JsonUtility.ToJson(new Report{passed=true,checks=checks},true));Debug.Log("CAT_CAFE_GOAL_PROJECTION_OK "+checks);
        }
        [Serializable] private class PatronRuleReport{public PatronRules rules;public CafePatronMatch terms;public DispatchRules[] destinations;}
        [Serializable] private class Report{public bool passed;public int checks;}
        [Serializable] private class Played{public string mode;public int day;public double funds,popularity;}
    }
}
