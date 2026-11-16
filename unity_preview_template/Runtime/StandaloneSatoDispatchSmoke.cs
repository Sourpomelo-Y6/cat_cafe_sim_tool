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

        private IEnumerator CheckUnlockedGuestSaves(string checkpoints,CafeNamedDispatchSettings guest)
        {
            var folder=Path.Combine(output,guest.customer_id);
            var backend=new LocalCafeBackend(folder);
            CafeBackends.UseLocal(backend);
            SceneManager.LoadScene("CatCafePythonState");
            yield return new WaitForSecondsRealtime(2);
            var client=FindObjectOfType<PythonStateClient>();
            var list=client.GetComponent<PythonEventListPanel>();
            list.HoldConversations=true;

            foreach(string stage in new[]{"affinity","request","reissue"})
            {
                Directory.CreateDirectory(folder);
                File.Copy(Path.Combine(checkpoints,guest.customer_id,stage,"slot-9.json"),Path.Combine(folder,"slot-9.json"),true);
                Command(backend,"load_game");
                Apply(client,backend);
                yield return null;

                var sim=backend.Simulation;
                bool unlocked;
                if(guest.customer_id=="expanded-guest")
                    unlocked=sim.State.expansion.Any(p=>p.seats==4 && p.day<sim.State.day);
                else
                {
                    int guestNumber=int.Parse(guest.customer_id.Substring("guest-".Length));
                    int goalStage=guestNumber>sim.Rules.arrival_ticks.Length+sim.Rules.popular_customer_count?2:1;
                    unlocked=sim.ClearedGoalDay(goalStage)>0;
                }
                Check(unlocked && sim.NamedAffinity(guest.customer_id).Count>0,"Unlocked guest affinity was not restored");
                if(stage=="reissue")
                    Check(sim.State.named_dispatch_archive.Any(e=>e.customer_id==guest.customer_id &&
                        e.request.returned_day+8==sim.State.day),"Reissued request lost its return day");
                if(stage!="affinity")
                    Check(sim.NamedDispatchAwaitingAnswer(guest.customer_id),"Unlocked guest request was not restored");
                OpenRow(list,guest.name+(stage=="affinity"?"との交流":"からの指名依頼"));
                var dialogue=client.GetComponent<SatoDispatchConversationPanel>();
                Check(dialogue.Showing,"Unlocked guest dialogue did not open: "+stage);
                dialogue.Close();
                list.Close();
            }
        }

        private IEnumerator Run()
        {
            var content=JsonUtility.FromJson<ContentCatalog>(Resources.Load<TextAsset>("CatCafeContent/catalog").text);
            var configured=JsonUtility.FromJson<ContentBundle>(Resources.Load<TextAsset>(content.bundleResource).text).named_dispatch;
            var rulesAsset=Resources.Load<TextAsset>("CatCafeStandalone/rules");
            int initialCount=JsonUtility.FromJson<CafeRules>(rulesAsset.text).arrival_ticks.Length;
            var initial=Enumerable.Range(1,initialCount).Select(i=>"guest-"+i).ToArray();
            var settings=configured??Array.Empty<CafeNamedDispatchSettings>();
            var guests=CafeSimulation.NamedDispatchCustomers.Concat(settings
                .Where(r=>r.enabled && initial.Contains(r.customer_id) && !CafeSimulation.NamedDispatchCustomers.Contains(r.customer_id))
                .Select(r=>r.customer_id)).ToArray();
            var args=Environment.GetCommandLineArgs();
            int checkpointArgument=Array.IndexOf(args,"--catcafe-named-dispatch-checkpoints");
            if(checkpointArgument>=0)
            {
                foreach(var guest in settings.Where(r=>r.enabled && !initial.Contains(r.customer_id)))
                {
                    var check=CheckUnlockedGuestSaves(args[checkpointArgument+1],guest);
                    while(check.MoveNext())yield return check.Current;
                }
            }
            foreach(var customer in guests)
            foreach(var route in new[]{"normal","decline","expired","refused","repeat"})
            {
                var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
                rules.store_events.probability=0;
                if(route=="expired" || route=="repeat")rules.satisfaction.satisfied_score=1;
                rules.starting_funds=100000;
                // Force suspension in one missed visit for the refusal fixture.
                if(route=="refused")rules.discontent.waiting_gain=100;
                var setting=configured?.SingleOrDefault(r=>r.customer_id==customer);
                if(setting!=null)rules.named_dispatch=new[]{setting};
                int slot=Array.IndexOf(guests,customer);
                if(slot>0)
                {
                    (rules.arrival_ticks[0],rules.arrival_ticks[slot])=(rules.arrival_ticks[slot],rules.arrival_ticks[0]);
                    int pattern=slot%rules.patterns.Length;
                    (rules.patterns[0],rules.patterns[pattern])=(rules.patterns[pattern],rules.patterns[0]);
                }
                string customerName=setting?.name??CafeSimulation.NamedCustomerName(customer);
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
                if(route=="expired" || route=="repeat")
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
                var returnDialog=client.GetComponent<MissingConversationPanel>();
                Check(returnDialog.Showing,"Missing conversation did not open");
                for(int frame=0;frame<30 && returnDialog.Showing;frame++){returnDialog.Next();yield return null;}
                var records=client.GetComponent<PythonMissingPanel>();
                Check(!returnDialog.Showing && records.panel.activeInHierarchy &&
                      records.panel.transform.GetSiblingIndex()==records.panel.transform.parent.childCount-1,
                    "Finishing the welcome conversation did not reveal the return record");
                Check(records.title.text=="指名猫の帰還" && records.confirm.interactable,
                    "Named return record did not offer confirmation");
                yield return new WaitUntil(()=>client.UIStateReady && client.CurrentState.revision==sim.State.revision);
                records.confirm.onClick.Invoke();
                yield return new WaitUntil(()=>client.UIStateReady);
                Check(sim.NamedRequest(customer).returned_day==sim.State.day,"Missing return was not applied");
                Check(returnDialog.Showing,"Confirmed return did not open the result conversation");
                for(int frame=0;frame<30 && !returnDialog.PlaybackChoosing;frame++)
                {
                    returnDialog.Next();
                    yield return null;
                }
                Check(returnDialog.PlaybackChoosing,"Return access choices did not open");
                bool allow=route=="expired" || route=="repeat";
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
                    int returnDay=sim.State.day;
                    int pastVisits=sim.State.history.Sum(h=>h.customer_records.Count(r=>r.customer_id==customer));
                    Check(pastVisits>0,"Ban route lacks prior visits");
                    yield return new WaitUntil(()=>client.UIStateReady);
                    Command(backend,"configure_adoption",choice:"on");
                    Command(backend,"save_game");
                    Command(backend,"load_game");
                    Check(sim.State.adoption.enabled && sim.NamedRequest(customer).return_access==CafeSimulation.BanReturnAccess,
                        "Adoption setting or ban was lost on load");
                    Check(sim.State.history.Sum(h=>h.customer_records.Count(r=>r.customer_id==customer))==pastVisits,
                        "Ban erased previous visit records");
                    Check(!sim.CustomerSchedule(returnDay).Any(r=>r.customer_id==customer),"Banned customer still scheduled on return day");
                    Prepare(sim);
                    sim.Execute("day_off");
                    Check(!sim.State.adoption.events.Any(e=>CafeSimulation.IsNamedAdoption(e.source) && e.customer_id==customer),
                        "Banned customer made a next-day adoption offer");
                    for(int i=0;i<9;i++)
                    {
                        Check(!sim.CustomerAvailable(customer,sim.State.day) &&
                            sim.CustomerSchedule(sim.State.day).All(r=>r.customer_id!=customer),
                            "Banned customer became available after reload");
                        Prepare(sim);
                        sim.Execute("day_off");
                    }
                    Check(!sim.State.trust_events.Any(e=>e.customer_id==customer),"Trust check opened after ban");
                    Command(backend,"save_game");
                    Command(backend,"load_game");
                    Apply(client,backend);
                    var catRecord=client.GetComponent<PythonCatDetailsPanel>();
                    catRecord.Open(cat);
                    catRecord.RelationshipRecords();
                    Check(catRecord.panel.activeInHierarchy &&
                        catRecord.details.text.Contains(backend.Details.CustomerName(customer)+"（"+customer+"）"),
                        "Past relationship record was not readable");
                    catRecord.Missing();
                    Check(records.panel.activeInHierarchy && records.details.text.Contains(returnDay+"日目に帰還を確認しました。"),
                        "Past named return record was not readable");
                    records.Close();
                    continue;
                }
                yield return new WaitUntil(()=>client.UIStateReady);
                if(route=="repeat")
                {
                    int retryDay=sim.NamedRequest(customer).returned_day+8;
                    while(sim.State.day<retryDay)
                    {
                        Check(!sim.NamedDispatchAwaitingAnswer(customer),"Request reissued early");
                        Prepare(sim);
                        sim.Execute("day_off");
                        if(sim.State.phase=="closed" && sim.GrowthWaiting)
                        {
                            sim.Execute("resolve_growth_auto");
                            sim.Execute("resolve_growth_batch");
                            sim.Execute("next_day");
                        }
                    }
                    Check(sim.NamedDispatchAwaitingAnswer(customer) && !sim.CustomerTrustWaiting,"Request was not reissued on day eight");
                    Command(backend,"save_game");
                    Command(backend,"load_game");
                    Apply(client,backend);
                    OpenRow(list,customerName+"からの指名依頼");
                    Check(dialogue.Showing,"Reissued request did not open after reload");
                    dialogue.Close();
                    continue;
                }
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
                Check(reader.PlaybackKey.StartsWith(CafeSimulation.NamedEpilogueId(customer)),"Wrong epilogue customer");
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
                Check(!sim.NamedDispatchAwaitingAnswer(customer),"Request opened before day eight");
                Prepare(sim);
                sim.Execute("day_off");
                Check(!sim.CustomerTrustWaiting && sim.State.trust_events.Count==0,"Return generated a trust conversation");
                Check(!sim.NamedDispatchAwaitingAnswer(customer),"Adopted cat was requested again");
                Command(backend,"save_game");
                Command(backend,"load_game");
                Check(sim.NamedEpilogueRequest(cat)?.epilogue_read==true,"Epilogue read state was lost");
            }
        }
    }
}
