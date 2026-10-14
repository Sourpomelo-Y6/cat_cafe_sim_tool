using System;
using System.IO;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor
{
    public static class PlayerTurnsVerification
    {
        public static void Verify()
        {
            int checks=0;
            void Check(bool ok){checks++;if(!ok)throw new Exception("Player turn verification failed: "+checks);}
            var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
            rules.store_events.probability=0;
            foreach(string scenario in new[]{"complete","exhausted","pause","manual","legacy"})
            {
                var sim=new CafeSimulation(rules);
                typeof(CafeSimulation).GetField("LegacyPlayerReplay",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(sim,scenario=="legacy");
                sim.Execute("player_begin",catId:"cat-tama");
                int limit=scenario=="legacy"?10:20;
                Check(sim.ActivePlayer.exchange.remaining==limit);
                int steps=0;
                while(sim.PlayerActive)
                {
                    string action=scenario=="exhausted"?"intense":scenario=="pause" && steps==0?"direct":"pause";
                    if(sim.ActivePlayer.exchange.connect_pending)action="connect";
                    sim.Execute("player_step",choice:action);steps++;
                    var bits=CafeDoublePrecision.Capture(sim.State);
                    var restored=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(sim.State));
                    restored.NormalizeJsonStrings();CafeDoublePrecision.Restore(restored,bits);sim.Restore(restored);
                    Check(steps<=limit);
                    if(scenario=="manual" && steps==4)sim.Execute("player_finish");
                }
                var session=sim.State.player_sessions[0];
                Check(session.exchange.end_reason==(scenario=="exhausted"?"exhausted":scenario=="manual"?"manual":"time_limit"));
                Check(sim.PlayerRemaining==2);
                if(scenario=="complete")Check(steps==20);
                typeof(CafeSimulation).GetField("LegacyPlayerReplay",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(sim,false);sim.Execute("player_begin",catId:"cat-sora");Check(sim.ActivePlayer.exchange.remaining==20);
                sim.Execute("player_finish");sim.Validate(sim.State);
            }
            string output=Environment.GetEnvironmentVariable("CATCAFE_TURNS_RESULT");
            if(!string.IsNullOrEmpty(output))File.WriteAllText(output,"{\"success\":true,\"checks\":"+checks+"}");
            Debug.Log("PASS player turns Unity: "+checks+" checks");
        }
    }
}
