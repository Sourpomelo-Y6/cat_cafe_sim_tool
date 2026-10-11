using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandalonePlayerSmoke
    {
        private static int checks;
        private static void Check(bool value,string message){checks++;if(!value)throw new Exception("Player bond: "+message);}
        private static string Command(LocalCafeBackend b,string kind,string choice=null,string cat=null,string target=null,int slot=1)=>JsonUtility.ToJson(new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,target_type=target,save_slot=slot,save_id=slot.ToString()});
        private static CafeResponse Request(LocalCafeBackend b,string command){CafeResponse response=null;var it=b.Request("/commands",command,r=>response=r);while(it.MoveNext()){}return response;}
        private static void Good(LocalCafeBackend b,string kind,string choice=null,string cat=null,string target=null,int slot=1){var r=Request(b,Command(b,kind,choice,cat,target,slot));Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static CafeSnapshot Clone(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();CafeDoublePrecision.Restore(s,CafeDoublePrecision.Capture(b.Simulation.State));return s;}
        private static string Capture(LocalCafeBackend b){var s=Clone(b);s.instance_id="";s.revision=0;return JsonUtility.ToJson(s);}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null,string cat=null,string target=null){string before=Capture(b);Check(Request(b,Command(b,kind,choice,cat,target)).result==UnityWebRequest.Result.ProtocolError,"Invalid "+kind+" accepted");Check(before==Capture(b),"Rejected operation changed state");}
        private static void Resume(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Resume changed exchange or affinity");}
        private static void Events(LocalCafeBackend b)
        {
            while(b.View().growth_choices.Length>0){var r=b.View().growth_choices[0];Good(b,r.kind,r.choices[0].choice,r.cat_id);}
            if(b.Simulation.IntakeWaiting){Good(b,"purchase_housing");Good(b,"resolve_intake","accept");Check(b.Simulation.State.player_cats.Count==6 && b.Simulation.PlayerCat(b.Simulation.Rules.intake.cat.id).affinity==0,"Joined cat affinity not initialized");}
            if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction","decline",b.Simulation.Rules.regular_introduction.cat.id);
        }
        private static void Complete(LocalCafeBackend b)
        {
            int guard=0;while(b.Simulation.PlayerActive)
            {
                Check(guard++<11,"Exchange did not finish");var view=b.View().player_interaction;var e=b.Simulation.ActivePlayer.exchange;
                if(view.actions.Single(a=>a.choice=="connect").can_select){Good(b,"player_step","connect");continue;}
                if(e.stamina<=20){Good(b,"player_step","pause");continue;}
                // Python's public two-step test policy; copies retain exact doubles.
                var definition=b.Simulation.Definition(e.cat_id);string action=null,target=null;double bestGain=double.NegativeInfinity,bestStamina=double.NegativeInfinity;
                foreach(var candidate in view.actions.Where(a=>a.can_select).Select(a=>new[]{a.choice,(string)null}).Concat(view.targets.Where(t=>t.can_select).Select(t=>new[]{"switch",t.choice})))
                {
                    var trial=CopyExchange(e);b.Simulation.StepExchange(trial,definition,candidate[0],candidate[1]);var futures=new System.Collections.Generic.List<CafeExchange>();
                    if(trial.end_reason!=null)futures.Add(trial);
                    else foreach(string follow in new[]{"direct","adapt","intense","feint","pause","connect"})
                    {var future=CopyExchange(trial);try{b.Simulation.StepExchange(future,definition,follow);futures.Add(future);}catch(ArgumentException){}}
                    var score=futures.OrderByDescending(f=>f.affinity_pending).ThenByDescending(f=>f.stamina).First();double gain=score.affinity_pending-e.affinity_pending;
                    if(gain>bestGain || gain==bestGain && score.stamina>bestStamina){bestGain=gain;bestStamina=score.stamina;action=candidate[0];target=candidate[1];}
                }
                Good(b,"player_step",action,target:target);
            }
        }
        private static CafeExchange CopyExchange(CafeExchange source)
        {
            var result=new CafeExchange();foreach(var field in typeof(CafeExchange).GetFields()){var value=field.GetValue(source);field.SetValue(result,value is Array array?array.Clone():value);}return result;
        }
        public static void Verify()
        {
            string root=Environment.GetEnvironmentVariable("CATCAFE_PLAYER_SMOKE")??"player-bond-smoke";Directory.CreateDirectory(root);int total=Run(Path.Combine(root,"manual"),false)+Run(Path.Combine(root,"goal"),true);File.WriteAllText(Path.Combine(root,"result.json"),"{\"passed\":true,\"checks\":"+total+"}");
        }
        public static int Run(string path,bool goal)
        {
            checks=0;var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.starting_funds=20000;rules.store_events.probability=0;rules.rules_id+="-player-"+goal;var b=new LocalCafeBackend(path,rules);
            int last=goal?6:4;if(File.Exists(Path.Combine(path,"slot-"+last+".json")))foreach(int slot in Enumerable.Range(2,last-1))
            {Good(b,"load_game",slot:slot);Check(slot>3 || b.Simulation.PlayerActive,"Cross-process active interaction lost");Check(slot!=6 || b.Simulation.State.bond_goal.continued,"Cross-process continuation lost");}
            Good(b,"new_game",goal?"bond":"free");Reject(b,"player_step","direct");Good(b,"player_finish");
            string payload=Command(b,"player_begin",cat:"cat-mike");var reply=Request(b,payload);Check(reply.result==UnityWebRequest.Result.Success,"Begin failed");string before=Capture(b);Check(Request(b,payload).text==reply.text && before==Capture(b),"Duplicate begin consumed another set");Resume(b,2);
            Reject(b,"day_off");Reject(b,"set_shifts");Reject(b,"dispatch_general","neighborhood_visit","cat-sora");Reject(b,"player_begin",cat:"cat-sora");Reject(b,"player_step","invalid");Reject(b,"player_step","switch",target:"invalid");Reject(b,"player_step","direct",target:"ball");
            Good(b,"player_step","direct");Resume(b,3);double affinity=b.Simulation.PlayerCat("cat-mike").affinity;Check(affinity==0,"Affinity applied before finish");Good(b,"player_finish");Good(b,"player_finish");Resume(b,4);
            if(!goal)
            {
                Good(b,"player_begin",cat:"cat-sora");Complete(b);Good(b,"player_begin",cat:"cat-tama");Complete(b);Check(b.Simulation.PlayerRemaining==0,"Shared daily count differs");Reject(b,"player_begin",cat:"cat-mugi");Events(b);Good(b,"day_off");Events(b);Check(b.Simulation.PlayerRemaining==3,"New day did not reset shared count");return checks;
            }
            int days=0;while(b.Simulation.State.bond_goal.status=="active")
            {
                Check(days++<150,"Six-cat goal did not complete");Events(b);
                while(b.Simulation.PlayerRemaining>0 && b.Simulation.State.bond_goal.status=="active")
                {
                    var cat=b.View().cats.Where(c=>c.can_player_start && c.affinity<rules.bond.affinity).OrderBy(c=>c.affinity).ThenBy(c=>c.cat_id).FirstOrDefault();if(cat==null)break;Good(b,"player_begin",cat:cat.cat_id);Complete(b);
                }
                if(b.Simulation.State.bond_goal.status=="cleared")break;Good(b,"day_off");
            }
            Check(b.Simulation.State.cats.Count==6 && b.Simulation.State.clear_results.Single().value==6 && b.View().required_action=="bond_result" && b.View().ending.can_continue,"Six-cat result not projected");Resume(b,5);Reject(b,"day_off");Reject(b,"player_begin",cat:"cat-mike");
            for(int field=0;field<8;field++)
            {
                var bad=Clone(b);if(field==0)bad.player_cats[0].affinity++;else if(field==1)bad.player_cats[0].total++;else if(field==2)bad.player_cats[0].today++;else if(field==3)bad.player_sessions[0].exchange.stamina++;else if(field==4)bad.player_sessions[0].actions[0].action="invalid";else if(field==5)bad.bond_goal.achieved_cats.RemoveAt(0);else if(field==6)bad.clear_results[0].value++;else bad.player_sessions[0].stress_after++;
                bool rejected=false;string captured=Capture(b);try{b.Simulation.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected && captured==Capture(b),"Forged player save accepted: "+field);
            }
            string record=JsonUtility.ToJson(b.Simulation.State.clear_results.Single());Good(b,"continue_bond_goal");Reject(b,"continue_bond_goal");Good(b,"day_off");Events(b);Good(b,"player_begin",cat:"cat-mike");Good(b,"player_step","direct");Good(b,"player_finish");Check(record==JsonUtility.ToJson(b.Simulation.State.clear_results.Single()),"Fixed clear record changed");Resume(b,6);return checks;
        }
    }
}
