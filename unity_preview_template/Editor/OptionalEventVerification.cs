using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;

namespace CatCafePreview.Editor
{
    public static class OptionalEventVerification
    {
        static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static int checks;
        static string output;
        static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        static void Good(LocalCafeBackend b,string kind,string choice=null)=>StandaloneDispatchConversationSmoke.Good(b,kind,choice,cat:"cat-mike");
        public static LocalCafeBackend Create(string path)
        {
            return StandaloneOptionalEventSmoke.Create(path);
        }
        static void Apply(ContentPreview p,LocalCafeBackend b)
        {
            CafeBackends.UseLocal(b);typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(p.GetComponent<PythonStateClient>(),new object[]{b.View()});
        }
        static void Capture(ContentPreview p,string name,int width,int height)
        {
            typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{p,Path.Combine(output,name+".png"),width,height});
        }
        public static void Run()
        {
            bool oldInstant=CafeOptions.Current.instant;CafeOptions.Current.instant=true;
            checks=0;output=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/OptionalEvents/Editor-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(output);
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var p=UnityEngine.Object.FindObjectOfType<ContentPreview>();p.Initialize();
            var client=p.GetComponent<PythonStateClient>();typeof(PythonStateClient).GetField("preview",Hidden).SetValue(client,p);
            var b=Create(Path.Combine(output,"isolated-save"));Apply(p,b);
            Check(!b.OptionalEventListed && b.OptionalEvent==null,"No day-one event or synthetic progress");
            Good(b,"new_game","popularity");Good(b,"day_off");if(b.Simulation.State.phase=="closed")Good(b,"next_day");
            for(int set=0;set<3 && b.Simulation.PlayerCat("cat-mike").affinity<20;set++)
            {
                Good(b,"player_begin");Good(b,"player_step","direct");
                if(b.Simulation.PlayerActive)Good(b,"player_finish");
            }
            Check(b.Simulation.PlayerCat("cat-mike").affinity>=20,"Legal exchange fixture achieved threshold");b.Simulation.Validate(b.Simulation.State);Apply(p,b);
            string intakeStatus=b.Simulation.State.intake_request.status;b.Simulation.State.intake_request.status="waiting";
            Check(b.OptionalEventReason!="" && !b.BeginOptionalEvent(b.Simulation.State.instance_id,b.Simulation.State.revision,""),"Mandatory response has priority");
            b.Simulation.State.intake_request.status=intakeStatus;
            var list=p.GetComponent<PythonEventListPanel>();Check(list.PendingCases().Any(r=>r.optional && r.reason==""),"Optional event list entry");
            Check(b.OptionalEvent==null && !b.ConversationBlocksProgress,"Listing is pure and nonblocking");
            Check(list.RequiredPendingCount==0,"Optional story excluded from required counts and developer collection");
            var detail=p.GetComponent<PythonCatDetailsPanel>();detail.Open("cat-mike");
            var button=detail.panel.transform.Find("Details Card/Optional Cat Event Entry").GetComponent<Button>();Check(button.gameObject.activeSelf && button.interactable,"Cat detail entry");
            Capture(p,"detail-1280",1280,800);Capture(p,"detail-1024",1024,768);
            var view=p.GetComponent<OptionalEventConversationPanel>()??p.gameObject.AddComponent<OptionalEventConversationPanel>();
            string digest=CafeReplayJournal.Digest(b.Simulation.State);
            Check(!b.BeginOptionalEvent("stale",b.Simulation.State.revision,""),"Stale instance rejected");
            Check(!b.BeginOptionalEvent(b.Simulation.State.instance_id,-1,""),"Stale revision rejected");
            Check(view.OpenEvent() && view.Showing && b.OptionalEventOpen,"Explicit open");detail.Close();
            var portrait=(RawImage)typeof(AcquisitionConversationPanel).GetField("portrait",Hidden).GetValue(view);Check(portrait.texture!=null && !string.IsNullOrEmpty(b.OptionalEvent.image_resource),"Frozen normal image resolves");
            Capture(p,"introduction-1280",1280,800);Capture(p,"introduction-1024",1024,768);
            Check(b.ConversationBlocksProgress,"Visible modal blocks business input");view.Close();Check(!b.ConversationBlocksProgress,"Closed optional event does not block business/autoplay");
            var auto=b.View().autoplay;Check(client.CanStartAutoPlay(auto.objectives.First(o=>o.can_start).choice,auto.modes.First().choice,1),"Autoplay remains available after close");
            b.OptionalEvent.progress.node=1;b.OptionalEvent.progress.page_offset=4;Good(b,"save_game");
            var restart=Create(Path.Combine(output,"isolated-save"));Good(restart,"load_game");Check(restart.OptionalEvent.progress.node==1 && restart.OptionalEvent.progress.page_offset==4,"Backend restart restores mid-page");
            Check(!restart.OptionalEventOpen && !restart.ConversationBlocksProgress,"Loading does not auto-open or block");Apply(p,restart);Check(!view.Showing,"UI load stays closed");
            Check(view.OpenEvent(),"Resume through explicit entry");
            var browser=p.GetComponent<PythonSaveBrowser>();browser.OpenSave();Check(browser.panel.activeSelf && browser.panel.transform.GetSiblingIndex()>view.PlaybackHost.transform.GetSiblingIndex(),"Save browser renders above conversation");browser.Close();
            restart.OptionalEvent.progress.node=restart.OptionalEvent.introduction.Length;restart.OptionalEvent.progress.page_offset=0;view.Refresh();Check(view.PlaybackChoosing,"Two choices");Capture(p,"choices-1280",1280,800);
            CafeOptionalEvents.Choose(restart.OptionalEvent,true);Check(!CafeOptionalEvents.Choose(restart.OptionalEvent,false),"Choice idempotency");view.Refresh();Capture(p,"result-1024",1024,768);
            restart.OptionalEvent.progress.node=1;restart.OptionalEvent.progress.page_offset=2;Good(restart,"save_game");Good(restart,"load_game");Apply(p,restart);
            Check(restart.OptionalEvent.progress.branch_id=="accepted" && restart.OptionalEvent.progress.page_offset==2,"Chosen branch resume");
            var row=restart.OptionalEvent;string oldText=row.introduction[0].text;var replacement=CafeOptionalEvents.Start(restart.Simulation.State,"新版","cat-mike","");replacement.introduction[0].text="改稿";Check(row.introduction[0].text==oldText,"Active text frozen");
            row.progress.node=row.accepted.Length;row.progress.page_offset=0;CafeOptionalEvents.Finish(row);restart.RememberViewedConversations();
            Check(restart.Replays.Load().entries.Count(e=>e.conversation_id==row.progress.conversation_id)==1,"Extra unlocks only chosen branch");
            Check(restart.Replays.Load().entries.Single(e=>e.conversation_id==row.progress.conversation_id).image_resource==row.image_resource,"Extra retains frozen image reference");
            Check(!restart.OptionalEventListed,"Completed event not reissued");Good(restart,"save_game");Good(restart,"load_game");Check(restart.OptionalEvent.progress.completed,"Completion persisted");
            Check(digest==CafeReplayJournal.Digest(restart.Simulation.State),"Story preserves gameplay digest");
            var json=File.ReadAllText(Path.Combine(output,"isolated-save/slot-1.json"));var legacy=JsonUtility.FromJson<CafeSaveEnvelope>(json);legacy.optional_events=null;legacy.slot=2;
            string legacyJson=System.Text.RegularExpressions.Regex.Replace(JsonUtility.ToJson(legacy,true),@"\s*""optional_events""\s*:\s*(?:null|\[\s*\])\s*,?","");Check(!legacyJson.Contains("optional_events"),"Old fixture genuinely omits optional field");File.WriteAllText(Path.Combine(output,"isolated-save/slot-2.json"),legacyJson);
            StandaloneDispatchConversationSmoke.Good(restart,"load_game",slot:2);Check(restart.OptionalEvent==null,"Missing old-save field reads empty");
            Apply(p,restart);Check(view.OpenEvent(),"Old save can start event");row=restart.OptionalEvent;row.image_resource="missing/optional-image";view.Refresh();Check(portrait.texture==ContentPreview.CatPlaceholderTexture(),"Missing art fallback");view.Close();
            Good(restart,"new_game","free");Check(restart.OptionalEvent==null && !restart.OptionalEventOpen,"New game clears optional progress");
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" Unity optional-event checks; isolated modified fixture rules; 1280x800 / 1024x768 renders. No production saves or gameplay rules changed.");CafeOptions.Current.instant=oldInstant;Debug.Log("CAT_CAFE_OPTIONAL_EVENT_OK: "+checks);
        }
    }
}
