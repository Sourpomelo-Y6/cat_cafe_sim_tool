using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using CatCafePreview.Standalone;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatCafePreview
{
    public sealed class StandaloneSatoDispatchSmoke : MonoBehaviour
    {
        private string output;
        private int checks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            int i=Array.IndexOf(args,"--catcafe-sato-check");
            if(i<0 || i+1>=args.Length)return;
            var qa=new GameObject("Sato verification").AddComponent<StandaloneSatoDispatchSmoke>();
            DontDestroyOnLoad(qa);
            qa.output=Path.GetFullPath(args[i+1]);
            Directory.CreateDirectory(qa.output);
            Application.runInBackground=true;
            qa.StartCoroutine(qa.Guard());
        }

        private IEnumerator Guard()
        {
            var run=Run();
            while(true)
            {
                bool more;
                try { more=run.MoveNext(); }
                catch(Exception error)
                {
                    File.WriteAllText(Path.Combine(output,"failure.txt"),error.ToString());
                    Application.Quit(1);
                    yield break;
                }
                if(!more)break;
                yield return run.Current;
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS "+checks+" Sato Player checks");
            Application.Quit(0);
        }

        private void Check(bool ok,string message)
        {
            checks++;
            if(!ok)throw new InvalidOperationException(message);
        }

        private void Command(LocalCafeBackend backend,string kind,string choice=null,string cat=null)
        {
            var state=backend.Simulation.State;
            CafeResponse reply=null;
            var request=backend.Request("/commands",JsonUtility.ToJson(new PythonShiftCommand {
                kind=kind, choice=choice, cat_id=cat, save_id="9", save_slot=9, working_cats=Array.Empty<string>(),
                instance_id=state.instance_id, expected_revision=state.revision, request_id=Guid.NewGuid().ToString("N")
            }),r=>reply=r);
            while(request.MoveNext()){}
            Check(reply?.result==UnityEngine.Networking.UnityWebRequest.Result.Success,kind+": "+reply?.error);
        }

        private static void Prepare(CafeSimulation sim)
        {
            if(sim.IntakeWaiting)sim.Execute("resolve_intake",choice:"decline");
            if(sim.RegularIntroductionWaiting)sim.Execute("resolve_regular_introduction",choice:"decline");
        }

        private static void Serve(CafeSimulation sim,string catId,bool timeout)
        {
            Prepare(sim);
            sim.Execute("set_shifts",working:new[]{catId});
            if(timeout)
            {
                var cat=sim.State.cats.Single(c=>c.id==catId);
                cat.stamina=0;
                cat.cannot_continue=true;
            }
            sim.Execute("start_business");
            while(sim.State.phase=="open")
            {
                var exchange=sim.State.exchanges.SingleOrDefault(e=>e.customer_id==CafeSimulation.Sato);
                if(!timeout && exchange!=null)exchange.affinity_pending=100;
                sim.Execute("advance_business");
            }
            sim.Execute("next_day");
        }

        private static void Apply(PythonStateClient client,LocalCafeBackend backend)
        {
            typeof(PythonStateClient).GetMethod("Accept",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(client,new object[]{backend.View()});
        }

        private void OpenRow(PythonEventListPanel list,string title)
        {
            list.Open();
            var button=list.content.GetComponentsInChildren<Button>().Single(b=>
                b.name.StartsWith("Pending Case",StringComparison.Ordinal) &&
                b.GetComponentInChildren<Text>().text.Contains(title));
            Check(button.interactable,"Inactive row: "+button.GetComponentInChildren<Text>().text);
            button.onClick.Invoke();
        }

        private IEnumerator Run()
        {
            foreach(var route in new[]{"normal","decline","expired","refused"})
            {
                var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
                rules.store_events.probability=0;
                rules.starting_funds=100000;
                var backend=new LocalCafeBackend(Path.Combine(output,route),rules);
                var sim=backend.Simulation;
                string cat=sim.State.cats[0].id;
                Serve(sim,cat,false);
                while(sim.State.sato_dispatch==null && sim.State.day<20)Serve(sim,cat,true);
                Prepare(sim);
                Check(sim.State.sato_dispatch!=null,"No request: "+route);
                if(route=="refused")Serve(sim,cat,true);
                Command(backend,"save_game");
                Command(backend,"load_game");

                CafeBackends.UseLocal(backend);
                SceneManager.LoadScene("CatCafePythonState");
                yield return new WaitForSecondsRealtime(2);
                var client=FindObjectOfType<PythonStateClient>();
                Apply(client,backend);
                var list=client.GetComponent<PythonEventListPanel>();
                list.HoldConversations=true;
                OpenRow(list,route=="refused"?"佐藤さんからの催促":"佐藤さんからの指名依頼");
                var dialogue=client.GetComponent<SatoDispatchConversationPanel>();
                Check(dialogue.Showing,"Request conversation did not open");
                dialogue.Next();
                yield return null;
                dialogue.Next();
                yield return null;
                string position=dialogue.PlaybackKey;
                Command(backend,"save_game");
                Command(backend,"load_game");
                Apply(client,backend);
                yield return null;
                OpenRow(list,route=="refused"?"佐藤さんからの催促":"佐藤さんからの指名依頼");
                Check(dialogue.PlaybackKey==position,"Request page was lost");
                if(route=="expired")
                {
                    dialogue.Close();
                    sim.Execute("day_off");
                    sim.Execute("day_off");
                }
                else
                {
                    for(int frame=0;frame<30 && !dialogue.PlaybackChoosing;frame++)
                    {
                        dialogue.Next();
                        yield return null;
                    }
                    Check(dialogue.PlaybackChoosing,"Request choices did not open");
                    dialogue.Answer(route!="decline");
                    for(int frame=0;frame<120 && sim.State.sato_dispatch.accepted_day==0 &&
                        sim.State.sato_dispatch.declined_day==0;frame++)yield return null;
                    Check(route=="decline"?sim.State.sato_dispatch.declined_day>0:sim.State.sato_dispatch.accepted_day>0,"UI answer was not applied");
                    if(route=="decline")continue;
                    yield return null;
                    sim.Execute("day_off");
                    var trip=sim.State.dispatches.Single(e=>e.destination==CafeSimulation.SatoDestination);
                    Apply(client,backend);
                    OpenRow(list,"派遣・帰還");
                    client.GetComponent<PythonGeneralDispatchPanel>().receive.onClick.Invoke();
                    for(int frame=0;frame<120 && trip.status!="resolved";frame++)yield return null;
                    Check(trip.status=="resolved","Dispatch receive was not applied");
                    if(route=="normal")
                    {
                        Check(sim.DiscontentScore(CafeSimulation.Sato,sim.State.day)==50,"Dissatisfaction was not reduced");
                        continue;
                    }
                }

                Check(sim.State.cats.Single(c=>c.id==cat).activity=="missing","Missing state was not applied");
                Command(backend,"save_game");
                Command(backend,"load_game");
                Check(sim.State.cats.Single(c=>c.id==cat).activity=="missing","Missing state was lost on load");
                while(sim.State.cats.Single(c=>c.id==cat).missing_remaining>0)sim.Execute("day_off");
                for(int frame=0;frame<120 && !client.CanManageSaves;frame++)yield return null;
                Apply(client,backend);
                OpenRow(list,"指名猫の帰還");
                Check(client.GetComponent<MissingConversationPanel>().Showing,"Missing conversation did not open");
                Command(backend,"resolve_missing",choice:sim.State.missing_cases.Single(e=>e.cat_id==cat && e.resolved_day==0).id,cat:cat);
                Check(sim.State.sato_dispatch.returned_day==sim.State.day,"Missing return was not applied");
                if(route!="expired")continue;
                sim.Execute("configure_adoption",choice:"on");
                sim.Execute("day_off");
                var offer=sim.State.adoption.events.Single(e=>e.source==CafeSimulation.SatoAdoptionSource);
                Command(backend,"save_game");
                Command(backend,"load_game");
                sim.Execute("resolve_adoption",source:offer.id,choice:"accept");
                for(int day=0;day<3;day++)
                {
                    Check(!sim.SatoEpilogueAvailable(cat),"Early epilogue");
                    sim.Execute("day_off");
                }
                Command(backend,"save_game");
                Command(backend,"load_game");
                Apply(client,backend);
                foreach(var open in client.GetComponents<AcquisitionConversationPanel>().Where(p=>p.Showing))open.Close();
                var details=client.GetComponent<PythonCatDetailsPanel>();
                details.Open(cat);
                var entry=details.panel.GetComponentsInChildren<Button>().Single(b=>b.name=="Optional Cat Event Entry");
                Check(entry.GetComponentInChildren<Text>().text=="後日談・未読","Unread epilogue button absent");
                entry.onClick.Invoke();
                var reader=client.GetComponent<SatoEpiloguePanel>();
                Check(reader.Showing,"Epilogue did not open");
                for(int frame=0;frame<40 && reader.Showing;frame++){reader.Next();yield return null;}
                Check(!reader.Showing && sim.State.sato_dispatch.epilogue_read,"Epilogue was not marked read");
                Check(!backend.ConversationBlocksProgress || backend.AdoptionResultPending || backend.MissingResultPending,"Epilogue blocked progress");
                Command(backend,"save_game");
                Command(backend,"load_game");
                Apply(client,backend);
                foreach(var open in client.GetComponents<AcquisitionConversationPanel>().Where(p=>p.Showing))open.Close();
                details.Open(cat);
                Check(entry.GetComponentInChildren<Text>().text=="後日談","Read label was not restored");
                entry.onClick.Invoke();
                Check(reader.Showing,"Epilogue cannot be reopened");
                reader.Close();
            }
        }
    }
}
