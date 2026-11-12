using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CatCafePreview.Standalone;

namespace CatCafePreview
{
    // Explicit development flag only. Saves stay under the supplied verification directory.
    public sealed class StandaloneOptionalEventSmoke : MonoBehaviour
    {
        string output;bool resume;int checks;
        static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--catcafe-optional-event");if(index<0)return;
            var qa=new GameObject("Optional event verification").AddComponent<StandaloneOptionalEventSmoke>();DontDestroyOnLoad(qa);
            qa.output=Path.GetFullPath(args[index+1]);qa.resume=args.Contains("--resume-optional");Directory.CreateDirectory(qa.output);Application.runInBackground=true;qa.StartCoroutine(qa.Guard());
        }
        public static LocalCafeBackend Create(string path)
        {
            var r=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
            r.store_events.probability=0;r.intake.day=r.regular_introduction.day=9999;r.growth.threshold=100000;
            r.exchange.affinity_enthusiastic=r.exchange.affinity_favorable=25;r.exchange.affinity_turn_away_loss=0;r.exchange.direct_gain=100;
            return new LocalCafeBackend(path,r);
        }
        static void Good(LocalCafeBackend b,string kind,string choice=null,int slot=1)=>StandaloneDispatchConversationSmoke.Good(b,kind,choice,cat:"cat-mike",slot:slot);
        void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        void CheckArtwork(AcquisitionConversationPanel view,bool still)
        {
            var artwork=(RawImage)typeof(AcquisitionConversationPanel).GetField("sceneImage",Hidden).GetValue(view);
            var portrait=(RawImage)typeof(AcquisitionConversationPanel).GetField("portrait",Hidden).GetValue(view);
            Check(artwork.enabled && artwork.texture!=null && artwork.texture.name==(still?"mike-approaches-v1":"mike-window-v1"),"Correct scene illustration");
            Check(portrait.enabled!=still,"Portrait switches off for the full illustration");
            var heading=(Text)typeof(AcquisitionConversationPanel).GetField("heading",Hidden).GetValue(view);
            var headingBox=(GameObject)typeof(AcquisitionConversationPanel).GetField("sceneHeading",Hidden).GetValue(view);
            Check(headingBox.GetComponent<RectTransform>().rect.width>=heading.preferredWidth+32,"Title fits on its backing");
            if(still)return;
            var frame=portrait.transform.parent.parent.GetComponent<RectTransform>();
            Check(frame.rect.height>310 && portrait.texture!=ContentPreview.CatPlaceholderTexture(),"Larger portrait uses the frozen cat texture");
        }
        IEnumerator Guard()
        {
            var work=Run();while(true){bool more;try{more=work.MoveNext();}catch(Exception ex){File.WriteAllText(Path.Combine(output,resume?"resume-failure.txt":"failure.txt"),ex.ToString());Debug.LogException(ex);Application.Quit(1);yield break;}if(!more)break;yield return work.Current;}
            File.WriteAllText(Path.Combine(output,resume?"resume-result.txt":"result.txt"),"PASS: "+checks+" Player checks. PID="+System.Diagnostics.Process.GetCurrentProcess().Id+"; explicit isolated fixture; no physical input or natural occurrence claim.");Application.Quit(0);
        }
        IEnumerator Shot(string name)
        {
            // Explicit rendering works even when the verification window is hidden.
            yield return new WaitForEndOfFrame();
            var canvas=FindObjectOfType<ContentPreview>().GetComponentInChildren<Canvas>();
            var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float distance=canvas.planeDistance;
            var previous=RenderTexture.active;var go=new GameObject("Optional event capture camera",typeof(Camera));
            var camera=go.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.white;
            var target=new RenderTexture(Screen.width,Screen.height,24);var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);image.Apply();
                var pixels=image.GetPixels32();Check(pixels.Any(p=>p.r!=pixels[0].r || p.g!=pixels[0].g || p.b!=pixels[0].b),"Capture contains rendered content: "+name);
                File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=previous;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=distance;
                camera.targetTexture=null;target.Release();DestroyImmediate(image);DestroyImmediate(target);DestroyImmediate(go);
            }
            yield return null;
        }
        IEnumerator Run()
        {
            CafeOptions.Current.instant=true;
            var b=Create(Path.Combine(output,"isolated-save"));CafeBackends.UseLocal(b);
            var args=Environment.GetCommandLineArgs();int variant=Array.IndexOf(args,"--optional-authoring-variant");
            if(args.Contains("--optional-milestone-pace")){MeasureMilestonePace();yield break;}
            if(args.Contains("--optional-milestones"))
            {
                var milestones=Milestones(b);
                while(milestones.MoveNext())yield return milestones.Current;
                yield break;
            }
            bool accepted=args.Contains("--optional-accept");
            string branch=accepted?"accepted":"declined";
            if(variant>=0)b.SetOptionalCatalog(CafeOptionalEventJson.Parse(File.ReadAllText(args[variant+1])));
            if(resume)Good(b,"load_game",slot:3);
            else
            {
                Check(!b.OptionalEventListed,"Day one unavailable");Good(b,"day_off");if(b.Simulation.State.phase=="closed")Good(b,"next_day");
                Good(b,"player_begin");Good(b,"player_step","direct");if(b.Simulation.PlayerActive)Good(b,"player_finish");
                Check(b.OptionalEventListed && b.OptionalEventReason=="","Legal threshold fixture");
                if(variant<0)
                {
                    Check(b.Simulation.PlayerCat("cat-mike").affinity==20 && b.Simulation.PlayerBeginReason("cat-mike").Contains("好感度20"),"Affinity overshoot is capped and the next interaction shows guidance");
                    Check(b.OptionalCandidates.Single().affinity_milestone,"Event list includes the milestone");
                    Check(b.Journal.Verify(b.Journal.Document).PlayerCat("cat-mike").affinity==20,"Business log replays the capped interaction");
                    Good(b,"day_off");if(b.Simulation.State.phase=="closed")Good(b,"next_day");
                    Check(b.OptionalEventListed && b.Simulation.PlayerCat("cat-mike").affinity==20,"Daily progression works with an unread milestone");
                }
            }
            string digest=CafeReplayJournal.Digest(b.Simulation.State);
            SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
            var client=FindObjectOfType<PythonStateClient>();client.enabled=false;CafeBackends.UseLocal(b);
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(client,new object[]{b.View()});apply();
            bool compact=resume || !accepted;
            int width=compact?1024:1280;
            Screen.SetResolution(width,compact?768:800,false);yield return new WaitForSecondsRealtime(.5f);
            var view=client.GetComponent<OptionalEventConversationPanel>()??client.gameObject.AddComponent<OptionalEventConversationPanel>();
            Check(!view.Showing && !b.ConversationBlocksProgress,"Does not auto-open or block");
            if(resume && variant>=0)Check(JsonUtility.ToJson(b.OptionalEvent)==File.ReadAllText(Path.Combine(output,"frozen-story.json")),"Started story remains identical after catalogue rewrite across processes");
            if(resume)Check(b.OptionalEvent.progress.branch_id==branch && b.OptionalEvent.progress.node==1 && b.OptionalEvent.progress.page_offset==2,"Different-process result position restored");
            Check(view.OpenEvent(),"Explicit start or resume");Check(b.ConversationBlocksProgress,"Visible event blocks business input");
            CheckArtwork(view,resume && accepted);
            yield return Shot(resume?"resume-1024":"introduction-"+width);
            view.Close();Check(!b.ConversationBlocksProgress,"Close releases business and autoplay");
            Check(view.OpenEvent(),"Reopen retains progress");
            if(!resume)
            {
                b.OptionalEvent.progress.node=b.OptionalEvent.introduction.Length;b.OptionalEvent.progress.page_offset=0;view.Refresh();Check(view.PlaybackChoosing,"Two choices ready");yield return Shot("choices-"+width);
                yield return null;view.Answer(accepted);Check(b.OptionalEvent.progress.branch_id==branch,"UI choice records branch");CheckArtwork(view,accepted);yield return Shot("result-"+width);
                b.OptionalEvent.progress.node=1;b.OptionalEvent.progress.page_offset=2;File.WriteAllText(Path.Combine(output,"frozen-story.json"),JsonUtility.ToJson(b.OptionalEvent));Good(b,"save_game",slot:3);Check(!b.OptionalEvent.progress.completed,"Partial save is unfinished");
            }
            else
            {
                b.OptionalEvent.progress.node=CafeOptionalEvents.Lines(b.OptionalEvent,branch).Length;b.OptionalEvent.progress.page_offset=0;view.Refresh();yield return null;view.Next();
                Check(b.OptionalEvent.progress.completed && !b.OptionalEventListed,"UI completion recorded once");
                Check(b.Replays.Load().entries.Count(e=>e.conversation_id==b.OptionalEvent.progress.conversation_id)==1,"Only chosen branch archived");
                Good(b,"save_game",slot:4);Good(b,"load_game",slot:4);apply();Check(b.OptionalEvent.progress.completed && !b.ConversationBlocksProgress,"Completed save reload");
                var entry=b.Replays.Load().entries.Single(e=>e.conversation_id==b.OptionalEvent.progress.conversation_id);
                if(b.OptionalEvent.artwork!=null)
                    Check(entry.artwork.SequenceEqual(b.OptionalEvent.artwork) && entry.artwork_choice_node==b.OptionalEvent.introduction.Length,"Extra stores the authored artwork and choice boundary");
                else
                    Check(entry.scene_art==CafeOptionalEvents.MikeWindowArt && entry.still_start_node==(accepted?b.OptionalEvent.introduction.Length+1:0),"Extra stores the result artwork boundary");
                var host=new GameObject("Extra artwork verification",typeof(RectTransform),typeof(Image));
                host.transform.SetParent(view.PlaybackHost.transform.parent,false);
                var replay=client.gameObject.AddComponent<ReplayConversationPanel>();
                replay.Play(entry,host,client.GetComponent<PythonCatDetailsPanel>().title.font,()=>{});
                yield return null;replay.Refresh();CheckArtwork(replay,false);
                for(int i=0;i<b.OptionalEvent.introduction.Length+1;i++){yield return null;replay.Next();}
                CheckArtwork(replay,accepted);
                yield return Shot("extra-result-1024");replay.Close();
            }
            Check(digest==CafeReplayJournal.Digest(b.Simulation.State),"Story does not change gameplay digest");
            if(resume && variant<0)
            {
                Check(!b.Simulation.PlayerAtAffinityMilestone("cat-mike"),"Reading the result releases the cap after save and reload");
                Good(b,"player_begin");Good(b,"player_step","direct");if(b.Simulation.PlayerActive)Good(b,"player_finish");
                Check(b.Simulation.PlayerCat("cat-mike").affinity>20,"Subsequent interaction raises affinity above the milestone");
                Check(b.Journal.Verify(b.Journal.Document).PlayerCat("cat-mike").affinity>20,"Business log replays the unlocked interaction");
            }
            if(resume && variant>=0)
            {
                string oldRevision=b.OptionalEvent.revision;Good(b,"new_game","free");Good(b,"day_off");if(b.Simulation.State.phase=="closed")Good(b,"next_day");Good(b,"player_begin");Good(b,"player_step","direct");if(b.Simulation.PlayerActive)Good(b,"player_finish");apply();
                Check(view.OpenEvent() && b.OptionalEvent.revision==b.OptionalDefinition.revision && b.OptionalEvent.revision!=oldRevision,"New start uses revised definition");
                yield return Shot("new-revision-1024");view.Close();
            }
        }
        IEnumerator Milestones(LocalCafeBackend b)
        {
            void Step(string kind,string cat,string choice=null,int slot=1)=>
                StandaloneDispatchConversationSmoke.Good(b,kind,choice,cat:cat,slot:slot);
            void Gain(string cat)
            {
                Step("player_begin",cat);Step("player_step",cat,"direct");
                if(b.Simulation.PlayerActive)Step("player_finish",cat);
            }
            void NextDay()
            {
                Good(b,"day_off");
                if(b.Simulation.State.phase=="closed")Good(b,"next_day");
            }
            double Affinity(string cat)=>b.Simulation.PlayerCat(cat).affinity;
            string[] cats={"cat-mike","cat-tama","cat-sora"};
            Check(!b.OptionalEventListed,"No pending milestone below its threshold");
            NextDay();
            foreach(string cat in cats)
            {
                Gain(cat);
                Check(Affinity(cat)==20 && b.Simulation.PlayerAtAffinityMilestone(cat),"Affinity caps independently: "+cat);
            }
            Check(b.OptionalCandidates.Length==3 && b.OptionalCandidates.All(c=>c.milestone==20),"Three reached milestones appear");
            b.Journal.Verify(b.Journal.Document);
            NextDay();
            Check(cats.All(cat=>Affinity(cat)==20),"Daily progress with unread events");
            SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
            var client=FindObjectOfType<PythonStateClient>();client.enabled=false;CafeBackends.UseLocal(b);
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(client,new object[]{b.View()});apply();
            var view=client.GetComponent<OptionalEventConversationPanel>()??client.gameObject.AddComponent<OptionalEventConversationPanel>();
            foreach(string cat in cats)
            {
                var candidate=b.OptionalForCat(cat);
                Check(view.OpenEvent(cat,candidate.event_id,candidate.revision),"Open another cat while others are unfinished: "+cat);
                b.OptionalEvent.progress.node=1;view.Close();
            }
            Good(b,"save_game",slot:5);Good(b,"load_game",slot:5);apply();
            Check(b.OptionalEvents.Length==3 && b.OptionalEvents.All(r=>r.progress.node==1 && !r.progress.completed),"All positions survive save and load");
            var mike=b.OptionalForCat(cats[0]);
            Check(view.OpenEvent(mike.cat_id,mike.event_id,mike.revision) && b.OptionalEvent.progress.node==1,"Resume Mike's first story");
            var row=b.OptionalEvent;row.progress.node=row.introduction.Length;row.progress.page_offset=0;view.Refresh();
            yield return null;view.Answer(true);
            row.progress.node=row.accepted.Length;row.progress.page_offset=0;view.Refresh();
            yield return null;view.Next();apply();
            Check(row.progress.completed && b.OptionalForCat(cats[0])==null,"First milestone completes");
            Gain(cats[0]);
            Check(Affinity(cats[0])==40 && b.OptionalForCat(cats[0])?.milestone==40,"Mike reaches his second milestone");
            Check(cats.Skip(1).All(cat=>Affinity(cat)==20 && b.OptionalForCat(cat).resume),"Tama and Sora remain independently capped");
            var second=b.OptionalForCat(cats[0]);
            Check(b.BeginOptionalEvent(second.cat_id,second.event_id,second.revision,b.Simulation.State.instance_id,b.Simulation.State.revision,""),"Start Mike's 40 event");
            row=b.OptionalEvent;row.progress.node=row.introduction.Length;CafeOptionalEvents.Choose(row,false);
            row.progress.node=row.declined.Length;b.FinishOptionalEvent(row);
            Gain(cats[0]);
            Check(Affinity(cats[0])==60 && b.OptionalForCat(cats[0])?.milestone==60,"Mike reaches his third milestone");
            CompleteMilestone(b,b.OptionalForCat(cats[0]),true);
            foreach(string cat in cats.Skip(1))CompleteMilestone(b,b.OptionalForCat(cat),false);
            Good(b,"enable_bond_goal");
            for(int level=40;level<=100;level+=20)
            {
                NextDay();
                foreach(string cat in cats.Where(id=>Affinity(id)<level))
                {
                    Gain(cat);
                    var candidate=b.OptionalForCat(cat);
                    Check(Affinity(cat)==level && candidate?.milestone==level,"Event listed at "+level+": "+cat);
                    Check(b.Simulation.PlayerAtAffinityMilestone(cat)==(level<100),"Only milestones below 100 cap affinity: "+cat);
                    if(level==80)
                        Check(b.Simulation.BondQualifying(b.Simulation.State).Contains(cat),"Unread 80 counts toward the bond goal: "+cat);
                    CompleteMilestone(b,candidate,cat!="cat-tama");
                }
            }
            Check(b.OptionalEvents.Count(r=>r.progress.completed)==15,"All fifteen priority stories completed independently");
            b.Journal.Verify(b.Journal.Document);
            Good(b,"save_game",slot:7);Good(b,"load_game",slot:7);
            Check(b.OptionalEvents.Count(r=>r.progress.completed)==15 && !b.OptionalEventListed,"Fifteen completions persist after reload");
            var archive=b.Replays.Load().entries;
            var finale=b.OptionalEvents.Where(r=>r.milestone==100).ToArray();
            Check(finale.Length==3 && finale.All(r=>archive.Any(e=>e.conversation_id==r.progress.conversation_id &&
                e.branch_id==r.progress.branch_id && e.lines.Last().text==CafeOptionalEvents.Lines(r,r.progress.branch_id).Last().text)),
                "Extra retains the chosen finale for all three cats");
            var entry=archive.Single(e=>e.conversation_id==finale[0].progress.conversation_id);
            var host=new GameObject("Extra finale verification",typeof(RectTransform),typeof(Image));
            host.transform.SetParent(view.PlaybackHost.transform.parent,false);
            var replay=client.gameObject.AddComponent<ReplayConversationPanel>();
            replay.Play(entry,host,client.GetComponent<PythonCatDetailsPanel>().title.font,()=>{});
            yield return null;
            Check(replay.Showing && replay.PlaybackKey.StartsWith(entry.conversation_id),"Finale opens in Extra playback");
            replay.Close();
            var priorityCatalog=CafeOptionalEventResources.Load();
            b.SetOptionalCatalog(new CafeOptionalEventCatalog{version=2,pack_id=new string('0',32),events=new CafeOptionalDefinition[0]});
            Good(b,"new_game","free");NextDay();Gain(cats[0]);
            Check(Affinity(cats[0])>20,"Existing game can have affinity above 20");
            Good(b,"save_game",slot:6);
            b.SetOptionalCatalog(priorityCatalog);Good(b,"load_game",slot:6);
            Check(Affinity(cats[0])>20 && b.OptionalForCat(cats[0])?.milestone==20 && !b.Simulation.PlayerAtAffinityMilestone(cats[0]),"Old affinity preserved and earlier story remains available");
            b.SetOptionalCatalog(null);
            var legacy=b.OptionalForCat(cats[0]);
            Check(b.BeginOptionalEvent(legacy.cat_id,legacy.event_id,legacy.revision,b.Simulation.State.instance_id,b.Simulation.State.revision,""),"Legacy Mike receipt starts");
            row=b.OptionalEvent;row.progress.node=row.introduction.Length;CafeOptionalEvents.Choose(row,false);row.progress.node=row.declined.Length;b.FinishOptionalEvent(row);
            Good(b,"save_game",slot:6);
            b.SetOptionalCatalog(priorityCatalog);Good(b,"load_game",slot:6);
            Check(b.OptionalForCat(cats[0])==null && b.OptionalEvents.Single().progress.completed,"Legacy completion prevents duplicate milestone");
        }
        void CompleteMilestone(LocalCafeBackend b,OptionalEventCandidate candidate,bool accept)
        {
            var state=b.Simulation.State;
            Check(candidate!=null && b.BeginOptionalEvent(candidate.cat_id,candidate.event_id,candidate.revision,state.instance_id,state.revision,""),
                "Open milestone");
            var row=b.OptionalEvent;
            row.progress.node=row.introduction.Length;row.progress.page_offset=0;
            Check(CafeOptionalEvents.Choose(row,accept),"Choose milestone branch");
            row.progress.node=CafeOptionalEvents.Lines(row,row.progress.branch_id).Length;
            b.FinishOptionalEvent(row);
        }
        void MeasureMilestonePace()
        {
            var b=new LocalCafeBackend(Path.Combine(output,"normal-rules"));
            var sim=b.Simulation;
            var state=sim.State;
            string[] cats={"cat-mike","cat-tama","cat-sora"};
            var rows=new System.Collections.Generic.List<string>{"cat_id,milestone,day,exchange_sets"};
            string report=Path.Combine(output,"milestone-pace.csv");
            void Record(OptionalEventCandidate pending)
            {
                rows.Add($"{pending.cat_id},{pending.milestone},{state.day},{sim.PlayerCat(pending.cat_id).total}");
                File.WriteAllLines(report,rows);
                CompleteMilestone(b,pending,true);
            }
            void ResolveReturns()
            {
                foreach(var cat in state.cats.Where(c=>c.activity=="missing" && c.missing_remaining==0 && !sim.DispatchMissing(state,c.id)).ToArray())
                    sim.Execute("resolve_missing",catId:cat.id);
                if(!b.MissingResultPending)return;
                foreach(var missing in state.missing_cases.Where(e=>e.result!=null && !e.result.completed))
                {
                    missing.result.node=CafeMissingDialogue.Lines(missing,"result","").Length;
                    CafeMissingDialogue.Finish(missing.result);
                }
            }
            void ResolveGrowth()
            {
                for(int stage=0;sim.GrowthWaiting;stage++)
                {
                    if(stage>=20)throw new InvalidOperationException("Growth choices did not settle");
                    sim.Execute("resolve_growth_auto");
                    sim.Execute("resolve_growth_batch");
                }
            }
            void ReadRequiredResults()
            {
                foreach(var row in b.EquipmentConversations.Where(r=>!r.progress.completed && r.progress.branch_id!="introduction"))
                {
                    row.progress.node=CafeEquipmentDialogue.Lines(row,row.progress.branch_id).Length;
                    CafeEquipmentDialogue.Finish(row);
                }
                foreach(var row in b.TrustConversations.Where(CafeTrustDialogue.Pending))
                {
                    row.Current.node=CafeTrustDialogue.Lines(row.Current==row.result,row.Current.branch_id,"",sim.Rules.trust.popularity_loss).Length;
                    CafeTrustDialogue.Finish(row);
                }
                foreach(var row in b.ReservationConversations.Where(CafeReservationDialogue.Pending))
                {
                    row.Current.node=CafeReservationDialogue.Lines(row,row.Current==row.result,row.Current.branch_id).Length;
                    CafeReservationDialogue.Finish(row);
                }
            }
            sim.Execute("set_auto_assignment",choice:"automatic");
            for(int day=1;day<=30;day++)
            {
                Check(state.day==day && state.phase=="preparation" && state.game_over==null,"Normal business reached preparation on day "+day);
                ResolveReturns();
                if(sim.StoreEventWaiting)sim.Execute("resolve_store_event",choice:sim.CurrentStoreEvent.type=="trouble"?"patch":"decline");
                if(sim.IntakeWaiting)sim.Execute("resolve_intake",choice:"decline");
                if(sim.RegularIntroductionWaiting)sim.Execute("resolve_regular_introduction",choice:"decline");
                if(sim.ReservationWaiting)sim.Execute("resolve_reservation",choice:"decline");
                foreach(var item in state.trust_events.Where(e=>e.status=="waiting").ToArray())
                    sim.Execute("resolve_customer_trust",choice:"ignore",source:item.id);
                ReadRequiredResults();
                ResolveGrowth();
                b.View();
                for(int i=0;i<cats.Length;i++)
                {
                    string cat=cats[i];
                    var pending=b.OptionalForCat(cat);
                    if(pending!=null && b.OptionalReason(pending)=="")
                        Record(pending);
                    if(sim.PlayerCat(cat).affinity>=100 || sim.PlayerBeginReason(cat)!="")continue;
                    sim.Execute("player_begin",catId:cat);
                    while(sim.PlayerActive)
                    {
                        var action=sim.PlanAutoplay("bond","clear");
                        sim.Execute(action.kind,choice:action.choice,source:action.source);
                    }
                    ResolveGrowth();
                    pending=b.OptionalForCat(cat);
                    if(pending!=null && b.OptionalReason(pending)=="")
                        Record(pending);
                }
                if(b.OptionalEvents.Count(r=>r.progress.completed)==15)break;
                var workers=state.cats.Where(c=>c.activity=="cafe" && c.health=="healthy" && c.stamina>0 && !c.cannot_continue)
                    .OrderBy(c=>cats.Contains(c.id)).ThenBy(c=>c.fatigue+c.stress).Take(sim.State.seat_count).Select(c=>c.id).ToArray();
                if(workers.Length==0)sim.Execute("day_off");
                else
                {
                    sim.Execute("set_shifts",working:workers);sim.Execute("start_business");
                    while(state.phase=="open")sim.Execute("advance_business");
                }
                ResolveReturns();
                ResolveGrowth();
                if(state.phase=="closed")sim.Execute("next_day");
            }
            File.WriteAllLines(report,rows);
            Check(b.OptionalEvents.Count(r=>r.progress.completed)==15,"All fifteen milestones reached under shipped rules");
        }
    }
}
