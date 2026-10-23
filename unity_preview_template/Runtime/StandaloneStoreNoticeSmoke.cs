using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public sealed class StandaloneStoreNoticeSmoke:MonoBehaviour {
 static int checks;string output;
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static CafeRules Rules()=>JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
 static LocalCafeBackend Create(string path,string type){var r=Rules();int seed=Enumerable.Range(0,1000).First(x=>new CafeSimulation(r,x).CurrentStoreEvent.type==type);var b=new LocalCafeBackend(path,r);b.Simulation.Restore(new CafeSimulation(r,seed).State);return b;}
 public static int RunChecks(string path){checks=0;
 foreach(string type in new[]{"festival","rain","supplies",null,"trouble","support"}) {
 var b=Create(Path.Combine(path,type??"none"),type);string before=JsonUtility.ToJson(b.Simulation.State);var view=b.View();bool automatic=type=="festival"||type=="rain"||type=="supplies";
 Check(!string.IsNullOrEmpty(view.store_event_notice)==automatic,"Notice type");for(int i=0;i<10;i++)b.View();Check(before==JsonUtility.ToJson(b.Simulation.State),"Viewing changed state");
 StandaloneDispatchConversationSmoke.Good(b,"save_game");var restored=new LocalCafeBackend(Path.Combine(path,type??"none"),Rules());StandaloneDispatchConversationSmoke.Good(restored,"load_game");Check(restored.View().store_event_notice==view.store_event_notice,"Save restore notice");
 if(type=="supplies") {var c=b.Simulation.State.cats.First();c.stress=30;b.Simulation.Execute("use_item",source:"store-event-1",catId:c.id);Check(b.View().store_event_notice.Contains("使用済み") && b.View().store_event_notice.Contains("所持数：0"),"Use record");StandaloneDispatchConversationSmoke.Good(b,"save_game");StandaloneDispatchConversationSmoke.Good(b,"load_game");Check(b.View().store_event_notice.Contains("使用済み") && b.Simulation.ItemInventory().Length==0,"Used save regranted");var sale=Create(Path.Combine(path,"sold"),type);sale.Simulation.Execute("sell_item",source:"store-event-1");Check(sale.View().store_event_notice.Contains("売却済み") && sale.Simulation.ItemInventory().Length==0,"Sale record");StandaloneDispatchConversationSmoke.Good(sale,"save_game");StandaloneDispatchConversationSmoke.Good(sale,"load_game");Check(sale.View().store_event_notice.Contains("売却済み") && sale.Simulation.ItemInventory().Length==0,"Sale restart regranted");}
 if(type=="festival"){Check(view.store_event_notice.Contains("まだ来店"),"Future shown arrived");b.Simulation.Execute("start_business");for(int i=0;i<20 && !b.Simulation.State.visits.Any(v=>v.id=="event-guest-1");i++)b.Simulation.Execute("advance_business");Check(b.View().store_event_notice.Contains("来店済み"),"Arrival not reflected");}
 if(type=="rain"||type=="supplies"||type==null){b.Simulation.Execute("day_off");Check(!b.View().store_event_notice.StartsWith("1日目"),"Stale next day notice");}
 }
 return checks;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--catcafe-store-notice-check");if(i<0)return;var q=new GameObject("Store notice QA").AddComponent<StandaloneStoreNoticeSmoke>();DontDestroyOnLoad(q);q.output=Path.GetFullPath(a[i+1]);Directory.CreateDirectory(q.output);q.StartCoroutine(q.Run());}
 IEnumerator Run(){try{File.WriteAllText(Path.Combine(output,"backend.txt"),"PASS "+RunChecks(Path.Combine(output,"cases")));}catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());yield break;}SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);var client=FindObjectOfType<PythonStateClient>();if(Environment.GetCommandLineArgs().Contains("--store-notice-resume")){var saved=new LocalCafeBackend(Path.Combine(output,"manual"),Rules());StandaloneDispatchConversationSmoke.Good(saved,"load_game",slot:2);CafeBackends.UseLocal(saved);typeof(PythonStateClient).GetMethod("Accept",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(client,new object[]{saved.View()});client.GetComponent<PythonStoreEventPanel>().OpenNotice();File.WriteAllText(Path.Combine(output,"restart.txt"),saved.View().store_event_notice+"\nphase="+saved.Simulation.State.phase+" tick="+saved.Simulation.State.tick);yield break;}
 foreach(string type in new[]{"festival","rain","supplies",null}){var b=Create(Path.Combine(output,"ui-"+(type??"none")),type);CafeBackends.UseLocal(b);typeof(PythonStateClient).GetMethod("Accept",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(client,new object[]{b.View()});yield return null;var panel=client.GetComponent<PythonStoreEventPanel>();string before=JsonUtility.ToJson(b.Simulation.State);Check(!panel.panel.activeSelf,"Notification forced over other UI");if(type!=null){client.GetComponent<PythonEventListPanel>().ShowMenu(2);yield return null;Check(panel.panel.activeSelf,"List cannot open notice");panel.Second();panel.OpenNotice();Check(before==JsonUtility.ToJson(b.Simulation.State),"UI changed state");if(type=="supplies"){panel.First();yield return null;Check(client.GetComponent<PythonItemsPanel>().panel.activeSelf,"Items route missing");client.GetComponent<PythonItemsPanel>().Close();}panel.panel.SetActive(false);}}
 var manual=Create(Path.Combine(output,"manual"),"supplies");CafeBackends.UseLocal(manual);typeof(PythonStateClient).GetMethod("Accept",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(client,new object[]{manual.View()});client.GetComponent<PythonPreparationPanel>().Open();File.WriteAllText(Path.Combine(output,"ui.txt"),"PASS "+checks);
 }
 }
}
