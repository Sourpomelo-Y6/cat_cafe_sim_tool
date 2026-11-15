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

        private static void Serve(CafeSimulation sim,string catId,bool timeout,string customer)
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
                var exchange=sim.State.exchanges.SingleOrDefault(e=>e.customer_id==customer);
                if(!timeout && exchange!=null)exchange.affinity_pending=100;
                sim.Execute("advance_business");
            }
            while(sim.GrowthWaiting)
            {
                sim.Execute("resolve_growth_auto");
                sim.Execute("resolve_growth_batch");
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
            foreach(var customer in CafeSimulation.NamedDispatchCustomers)
            foreach(var route in new[]{"normal","decline","expired","refused"})
            {
                var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
                rules.store_events.probability=0;
                if(route=="expired")rules.satisfaction.satisfied_score=1;
                rules.starting_funds=100000;
                // Force suspension in one missed visit for the refusal fixture.
                if(route=="refused")rules.discontent.waiting_gain=100;
                if(customer==CafeSimulation.Suzuki)
                {
                    (rules.arrival_ticks[0],rules.arrival_ticks[1])=(rules.arrival_ticks[1],rules.arrival_ticks[0]);
                    (rules.patterns[0],rules.patterns[1])=(rules.patterns[1],rules.patterns[0]);
                }
                string customerName=CafeSimulation.NamedCustomerName(customer);
                var backend=new LocalCafeBackend(Path.Combine(output,customer,route),rules);
                var sim=backend.Simulation;
                string cat=sim.State.cats[0].id;
                Serve(sim,cat,false,customer);
                while(sim.NamedRequest(customer)==null && sim.State.day<20)Serve(sim,cat,true,customer);
                Prepare(sim);
                Check(sim.NamedRequest(customer)!=null,"No request: "+route);
                Check(sim.DiscontentScore(customer,sim.State.day)==(route=="refused"?100:50),"Wrong score at request");
                if(route=="refused")sim.Execute("day_off");
                Command(backend,"save_game");
                Command(backend,"load_game");

                CafeBackends.UseLocal(backend);
                SceneManager.LoadScene("CatCafePythonState");
                yield return new WaitForSecondsRealtime(2);
                var client=FindObjectOfType<PythonStateClient>();
                Apply(client,backend);
                var list=client.GetComponent<PythonEventListPanel>();
                list.HoldConversations=true;
                string shortcut=list.open.GetComponentInChildren<Text>().text;
                Check(shortcut.Contains(route=="refused"?"指名派遣・開店前まで":"指名派遣の依頼"),"Preparation shortcut missing");
                var preparation=client.GetComponent<PythonPreparationPanel>();
                Check(preparation.summary.text.Contains(route=="refused"?"今日の開店前まで":(sim.State.day+1)+"日目の開店前まで"),
                    "Preparation panel did not show the deadline");
                if(route=="refused")
                {
                    var festival=backend.View();
                    festival.store_event_notice="地域のお祭り";
                    festival.store_event_label="地域のお祭り";
                    preparation.Apply(festival);
                    Check(preparation.summary.text.Contains("今日の開店前まで") && preparation.summary.text.Contains("地域のお祭り"),
                        "The deadline and store notice did not coexist");
                    Check(preparation.summary.preferredHeight<=preparation.summary.rectTransform.rect.height+1,$"Deadline was clipped in preparation ({preparation.summary.preferredHeight} > {preparation.summary.rectTransform.rect.height})");
                    var generated=new TextGenerator();
                    generated.Populate(preparation.summary.text,preparation.summary.GetGenerationSettings(preparation.summary.rectTransform.rect.size));
                    Check(generated.lineCount==3,"The preparation summary did not render all three lines");
                    preparation.Apply(client.CurrentState);
                }
                var pendingCases=list.PendingCases();
                var requestRow=pendingCases.Single(row=>row.timedRequest);
                Check(!pendingCases.TakeWhile(row=>row!=requestRow).Any(row=>row.optional && !row.timedRequest),
                    "An optional scene was placed before the timed request");
                Check(requestRow.Caption.Contains((sim.NamedRequest(customer).requested_day+1)+"日目の営業準備が回答最終日"),"Last response day missing");
                OpenRow(list,customerName+(route=="refused"?"からの催促":"からの指名依頼"));
                var dialogue=client.GetComponent<SatoDispatchConversationPanel>();
                Check(dialogue.Showing,"Request conversation did not open");
                bool reminder=route=="refused";
                bool showsOpeningDeadline=dialogue.PlaybackHost.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("開店前に"));
                dialogue.Next();
                yield return null;
                showsOpeningDeadline |= dialogue.PlaybackHost.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("開店前に"));
                dialogue.Next();
                yield return null;
                if(reminder)
                    Check(showsOpeningDeadline || dialogue.PlaybackHost.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("開店前に")),"Reminder does not say before opening");
                string position=dialogue.PlaybackKey;
                dialogue.Close();
                OpenRow(list,customerName+(route=="refused"?"からの催促":"からの指名依頼"));
                Check(dialogue.Showing && dialogue.PlaybackKey==position,"Closing lost request position");
                Command(backend,"save_game");
                Command(backend,"load_game");
                Apply(client,backend);
                yield return null;
                OpenRow(list,customerName+(route=="refused"?"からの催促":"からの指名依頼"));
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
                    Check(dialogue.PlaybackHost.GetComponentsInChildren<Text>().Any(t=>t.text.Contains((sim.NamedRequest(customer).requested_day+1)+"日目の営業準備が回答最終日")),"Answer screen deadline missing");
                    dialogue.Answer(route!="decline");
                    for(int frame=0;frame<120 && sim.NamedRequest(customer).accepted_day==0 &&
                        sim.NamedRequest(customer).declined_day==0;frame++)yield return null;
                    Check(route=="decline"?sim.NamedRequest(customer).declined_day>0:sim.NamedRequest(customer).accepted_day>0,"UI answer was not applied");
                    if(route=="decline")continue;
                    yield return null;
                    sim.Execute("day_off");
                    var trip=sim.State.dispatches.Single(e=>CafeSimulation.NamedDestinationCustomer(e.destination)==customer);
                    Apply(client,backend);
                    OpenRow(list,"派遣・帰還");
                    client.GetComponent<PythonGeneralDispatchPanel>().receive.onClick.Invoke();
                    for(int frame=0;frame<120 && trip.status!="resolved";frame++)yield return null;
                    Check(trip.status=="resolved","Dispatch receive was not applied");
                    if(route=="normal")
                    {
                        Check(sim.DiscontentScore(customer,sim.State.day)==25,"Dissatisfaction was not reduced");
                        continue;
                    }
                }

                Check(sim.State.cats.Single(c=>c.id==cat).activity=="missing","Missing state was not applied");
                Command(backend,"save_game");
                Command(backend,"load_game");
                Check(sim.State.cats.Single(c=>c.id==cat).activity=="missing","Missing state was lost on load");
                while(sim.State.cats.Single(c=>c.id==cat).missing_remaining>0)sim.Execute("day_off");
                yield return new WaitUntil(()=>client.UIStateReady);
                Apply(client,backend);
                OpenRow(list,"指名猫の帰還");
                Check(client.GetComponent<MissingConversationPanel>().Showing,"Missing conversation did not open");
                Command(backend,"resolve_missing",choice:sim.State.missing_cases.Single(e=>e.cat_id==cat && e.resolved_day==0).id,cat:cat);
                Check(sim.NamedRequest(customer).returned_day==sim.State.day,"Missing return was not applied");
                Apply(client,backend);
                var returnDialog=client.GetComponent<MissingConversationPanel>();
                for(int frame=0;frame<30 && !returnDialog.PlaybackChoosing;frame++)
                {
                    returnDialog.Next();
                    yield return null;
                }
                Check(returnDialog.PlaybackChoosing,"Return access choices did not open");
                bool allow=route=="expired";
                var returnText=returnDialog.PlaybackHost.GetComponentsInChildren<Text>().Select(t=>t.text).ToArray();
                Check(returnText.Contains("指名猫の帰還") && returnText.Any(t=>t.Contains(allow?"連れ去り":"返還拒否")),"Wrong named return heading");
                Check(returnText.All(t=>!t.Contains("家出")),"Named return used runaway wording");
                yield return new WaitUntil(()=>client.UIStateReady && client.CurrentState.revision==sim.State.revision);
                returnDialog.Answer(allow);
                if(!allow)
                {
                    yield return new WaitUntil(()=>client.UIStateReady && client.CurrentState.revision==sim.State.revision);
                    var confirm=returnDialog.PlaybackHost.GetComponentsInChildren<Button>()
                        .Single(b=>b.GetComponentInChildren<Text>().text=="この内容で回答");
                    Check(sim.NamedReturnAccessPending(customer),"Ban applied before confirmation");
                    confirm.onClick.Invoke();
                }
                yield return new WaitUntil(()=>client.UIStateReady);
                Check(!sim.NamedReturnAccessPending(customer),$"Return access answer was not applied: {customer}/{route}, client revision {client.CurrentState.revision}, simulation revision {sim.State.revision}");
                for(int frame=0;frame<30 && returnDialog.Showing;frame++){returnDialog.Next();yield return null;}
                Check(!returnDialog.Showing,"Return result did not finish");
                if(!allow)
                {
                    Check(!sim.CustomerAvailable(customer,sim.State.day+14),"Banned customer still available");
                    continue;
                }
                yield return new WaitUntil(()=>client.UIStateReady);
                sim.Execute("configure_adoption",choice:"on");
                sim.Execute("day_off");
                var offer=sim.State.adoption.events.Single(e=>CafeSimulation.IsNamedAdoption(e.source) && e.customer_id==customer);
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
                var entry=details.panel.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="Optional Cat Event Entry");
                Check(entry!=null,"Cat details did not show the available epilogue");
                Check(entry.GetComponentInChildren<Text>().text=="後日談・未読","Unread epilogue button absent");
                entry.onClick.Invoke();
                var reader=client.GetComponent<SatoEpiloguePanel>();
                Check(reader.PlaybackKey.StartsWith(customer==CafeSimulation.Suzuki?"suzuki.epilogue.v1":"sato.epilogue.v1"),"Wrong epilogue customer");
                Check(reader.Showing && !details.panel.activeSelf,"Cat details cover the epilogue");
                for(int frame=0;frame<40 && reader.Showing;frame++){reader.Next();yield return null;}
                Check(!reader.Showing,$"Epilogue did not finish: {reader.PlaybackKey}, ready={client.CanManageSaves}, visible={details.panel.activeSelf}, key={client.CurrentState.instance_id}");
                Check(sim.NamedRequest(customer).epilogue_read,"Epilogue did not record the read flag");
                Check(details.panel.activeSelf,"Epilogue did not restore cat details");
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
                details.Close();
                sim.Execute("configure_adoption",choice:"off");

                int due=sim.NamedRequest(customer).returned_day+8;
                while(sim.State.day<due-1){Prepare(sim);sim.Execute("day_off");}
                Check(!sim.CustomerTrustWaiting,"Trust warning opened before day eight");
                Prepare(sim);
                sim.Execute("day_off");
                var warning=sim.State.trust_events.Single(e=>e.customer_id==customer && e.day==due);
                Apply(client,backend);
                var trust=client.GetComponent<TrustConversationPanel>();
                CafeOptions.Current.instant=true;
                Check(trust.OpenEvent(warning.id),"Return trust conversation did not open");
                Check(trust.PlaybackHost.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("来店を受け入れることにした")),"Trust conversation did not recall the return choice");
                for(int frame=0;frame<30 && !trust.PlaybackChoosing;frame++){trust.Next();yield return null;}
                Check(trust.PlaybackChoosing,"Trust response choices did not open");
                Check(trust.PlaybackHost.GetComponentsInChildren<Text>().All(t=>!t.text.Contains("来店停止期限")),"Inactive suspension still displayed a deadline");
                yield return new WaitUntil(()=>client.UIStateReady && client.CurrentState.revision==sim.State.revision);
                trust.Answer(true);
                yield return new WaitUntil(()=>client.UIStateReady);
                Check(warning.status=="recovery","Trust recovery answer was not applied");
                for(int frame=0;frame<30 && trust.Showing;frame++){trust.Next();yield return null;}
                Check(!trust.Showing && !backend.TrustResultPending,"Trust answer conversation did not finish");

                var serviceCat=sim.State.cats.First(c=>c.activity=="cafe" && c.health=="healthy").id;
                while(warning.status=="recovery" && sim.State.day<due+8)Serve(sim,serviceCat,false,customer);
                Check(warning.outcome=="recovered","Satisfied service did not restore trust");
                Apply(client,backend);
                Check(trust.OpenEvent(warning.id),"Recovery result conversation did not open");
                Check(trust.PlaybackHost.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("「満足」") && t.text.Contains("回復済み")),"Recovery result did not explain the satisfied visit");
                for(int frame=0;frame<30 && trust.Showing;frame++){trust.Next();yield return null;}
                Check(!trust.Showing && !backend.TrustResultPending,"Recovery result conversation did not finish");
                Command(backend,"save_game");
                Command(backend,"load_game");
                Check(sim.State.trust_events.Single(e=>e.id==warning.id).outcome=="recovered","Recovery result did not persist");
            }
        }
    }
}
