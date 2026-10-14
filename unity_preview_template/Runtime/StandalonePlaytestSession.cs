using System;
using System.Collections;
using System.IO;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    // Opt-in observation only. Actual inputs still go through the normal screens.
    public sealed class StandalonePlaytestSession : MonoBehaviour, ICafeBackend
    {
        private LocalCafeBackend backend;
        private string folder;
        private int commands,errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--catcafe-playtest");if(index<0)return;
            if(index+1>=args.Length || !Path.IsPathRooted(args[index+1]))throw new ArgumentException("Playtest output needs an absolute path");
            var go=new GameObject("Standalone playtest observer");DontDestroyOnLoad(go);var observer=go.AddComponent<StandalonePlaytestSession>();observer.folder=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(observer.folder);
            observer.backend=new LocalCafeBackend(Path.Combine(observer.folder,"saves"));CafeBackends.UseLocal(observer);Application.runInBackground=true;observer.Record("launch",null);
            var rules=observer.backend.Simulation.Rules;var probe=new CafeSimulation(rules,0,"patron");File.WriteAllText(Path.Combine(observer.folder,"rule-diagnostic.json"),JsonUtility.ToJson(new RuleDiagnostic{featureOnlySpecializationNull=rules.patron.members[0].conditions[0].specialization==null,featureOnlySpecializationEmpty=rules.patron.members[0].conditions[0].specialization=="",whiteMatches=probe.PatronTerms(probe.State,"cat-sora","patron_moody_visit").matched,memberDestination=probe.DispatchDestination("patron_moody_visit").name},true));
        }
        public IEnumerator Request(string route,string commandJson,Action<CafeResponse> completed,int timeout=3)
        {
            yield return backend.Request(route,commandJson,response=>{if(route=="/commands"){commands++;File.WriteAllText(Path.Combine(folder,"response.json"),response.text??"");if(response.result!=UnityEngine.Networking.UnityWebRequest.Result.Success)errors++;Record(commandJson,response.error);}completed(response);},timeout);
        }
        private void Record(string command,string error)
        {
            var s=backend.Simulation.State;var envelope=new CafeSaveEnvelope{state=s,double_bits=CafeDoublePrecision.Capture(s),daily_baselines=backend.DailyRecords.Baselines.ToArray(),daily_records=backend.DailyRecords.Records.ToArray()};
            File.WriteAllText(Path.Combine(folder,"latest.json"),JsonUtility.ToJson(envelope,true));
            File.AppendAllText(Path.Combine(folder,"commands.jsonl"),JsonUtility.ToJson(new Observation{utc=DateTime.UtcNow.ToString("o"),command=command,error=error,day=s.day,tick=s.tick,revision=s.revision,phase=s.phase,objective=s.objective,funds=s.funds,popularity=s.popularity,goal=backend.Simulation.AutoTargetStatus(s.objective),autoplay=s.autoplay?.reason,commands=commands,errors=errors})+"\n");
        }
        private void OnApplicationQuit(){if(backend!=null)Record("quit",null);}
        [Serializable] private class RuleDiagnostic{public bool featureOnlySpecializationNull,featureOnlySpecializationEmpty,whiteMatches;public string memberDestination;}
        [Serializable] private class Observation{public string utc,command,error,phase,objective,goal,autoplay;public int day,tick,revision,commands,errors;public double funds,popularity;}
    }
}
