using System;
using System.IO;
using CatCafePreview.Standalone;
using UnityEngine;
namespace CatCafePreview
{
 public static class StandaloneNarrativeSmoke
 {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  private static void Run(){var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--catcafe-narrative-resume");if(index<0)return;string folder=args[index+1];try{var b=new LocalCafeBackend(Path.Combine(folder,"saves"));CafeResponse result=null;var request=b.Request("/commands",JsonUtility.ToJson(new PythonShiftCommand{kind="load_game",save_id="2",request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=0}),r=>result=r);while(request.MoveNext()){}if(result==null || result.result!=UnityEngine.Networking.UnityWebRequest.Result.Success)throw new Exception(result?.error??"load failed");var session=b.Simulation.ActivePlayer;if(session==null)throw new Exception("active set lost");string text=b.Narratives.Read(session.id);if(!File.ReadAllText(Path.Combine(folder,"result.txt")).Contains(text))throw new Exception("prose differs after process restart");foreach(var row in b.Narratives.Rows)if(!string.IsNullOrEmpty(row.image_resource) && CafeNarrativeResources.Texture(row.image_resource)==null)throw new Exception("saved image missing after process restart");string before=JsonUtility.ToJson(b.Simulation.State);for(int i=0;i<20;i++)if(b.Narratives.Read(session.id)!=text)throw new Exception("unstable read");if(before!=JsonUtility.ToJson(b.Simulation.State))throw new Exception("read consumed resources");File.WriteAllText(Path.Combine(folder,"player-resume.txt"),"Unity Player: save resume and 20 stable read-only views passed.");Application.Quit(0);}catch(Exception ex){File.WriteAllText(Path.Combine(folder,"player-failure.txt"),ex.ToString());Application.Quit(1);}}
 }
}
