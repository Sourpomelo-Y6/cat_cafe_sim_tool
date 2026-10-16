using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CatCafePreview.Standalone;

namespace CatCafePreview.Editor
{
    // Boundary fixtures use the real exchange/receipt/UI path without changing product rules.
    public static class SeatActionVerification
    {
        private static int checks;
        private static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
        public static void VerifyAndBuild(){Run();StandaloneScreenSetup.Build();}
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("CATCAFE_SEAT_OUTPUT");Directory.CreateDirectory(output);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();
            var client=preview.GetComponent<PythonStateClient>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var screen=preview.GetComponent<PythonServiceScreen>();
            Action<LocalCafeBackend> apply=b=>{CafeBackends.UseLocal(b);typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{b.View()});screen.ShowSeats();Canvas.ForceUpdateCanvases();};
            var catalog=CafeNarrativeResources.Load();
            foreach(string catId in new[]{"cat-mike","cat-tama","cat-sora","cat-kohaku","cat-mugi","request-cat-1","regular-rescue-1","visiting-cat-1"})
            {
                var rules=Rules(catId);var b=new LocalCafeBackend(Path.Combine(output,"fixtures"),rules);var sim=b.Simulation;
                sim.Execute("set_shifts",working:new[]{catId});sim.Execute("start_business");
                while(sim.State.exchanges.Count==0)sim.Execute("advance_business");
                var e=sim.State.exchanges[0];var visit=sim.State.visits.Single(v=>v.id==e.customer_id);
                foreach(string mode in CafeNarrativeCatalog.Modes.Where(m=>m!="pause"))
                foreach(string action in new[]{"switch","direct","adapt","connect"})
                foreach(string result in action=="direct"?new[]{"normal","turn_away","confused","listless","open_up"}:action=="connect"?new[]{"connect","simultaneous"}:new[]{"normal"})
                {
                    e.mode=action=="switch"?(mode=="pet"?"teaser":"pet"):mode;e.remaining=20;e.end_reason=null;e.connect_pending=false;e.open_up_pending=result=="open_up" || result=="simultaneous";e.engagement=0;e.tension=action=="connect"?rules.exchange.optional_threshold:0;e.stamina=result=="listless"?10:100;
                    var definition=JsonUtility.FromJson<CatDefinition>(JsonUtility.ToJson(sim.Definition(catId)));
                    int index=Array.FindIndex(rules.types,t=>t.id==mode);
                    definition.type_preferences[index]=result=="turn_away"?0:result=="confused"?2/(rules.exchange.direct_gain*rules.types[index].gain):1;
                    double before=e.stamina;int opened=e.open_up_count;
                    sim.StepExchange(e,definition,action,action=="switch"?mode:null);
                    sim.ExchangeObserved(e,mode,action,before,opened);visit.seated_ticks++;
                    var row=b.Narratives.LatestSeat(e);var seat=b.View().seats[e.seat];
                    Check(seat.latest_action==CafeNarratives.ActionLabel(row) && seat.cat_reaction==CafeNarratives.ReactionLabel(row),"Wrong receipt labels");
                    if(result=="turn_away" || result=="confused" || result=="listless")Check(row.reaction==result && row.image_scene_key==mode,"Negative image policy");
                    if(result=="open_up" || result=="simultaneous")Check(row.special_reaction==result && row.image_resource==CafeNarrativeResources.SceneResource(catalog,catId,mode+"__"+result),"Special receipt");
                    if(action=="switch")Check(row.image_scene_key==mode+"__switch" || row.image_scene_key==mode,"Preparation image");
                    string beforeRead=JsonUtility.ToJson(sim.State);apply(b);
                    var card=screen.GetSeatCard(e.seat);
                    Check(card.picture.texture==(CafeNarrativeResources.Texture(row.image_resource)??preview.PythonCatTexture(card.state)),"Wrong seat texture");
                    Check(card.stateText.text.Contains(seat.latest_action) && card.stateText.text.Contains(seat.cat_reaction),"Wrong seat text");
                    Check(card.stateText.preferredHeight<=card.stateText.rectTransform.rect.height+1,"Seat text clipped: "+card.stateText.text);
                    Check(beforeRead==JsonUtility.ToJson(sim.State),"View advanced simulation");
                    if(catId=="cat-mike" && (mode=="pet" || mode=="teaser") && (action=="switch" || action=="direct"))
                    {
                        Capture(preview,Path.Combine(output,mode+"-"+action+"-"+result+".png"),1280,800);
                        card.details.onClick.Invoke();var detail=preview.GetComponent<PythonSeatNarrativePanel>();
                        Check(detail.picture.texture==card.picture.texture && detail.body.text.Contains(row.text),"Detail and card disagree");detail.panel.SetActive(false);
                    }
                }
            }
            // Natural multi-seat progression, save/resume, departed customers and legacy records.
            var natural=new LocalCafeBackend(Path.Combine(output,"saves"),Rules("cat-mike"));
            natural.Simulation.Execute("set_shifts",working:new[]{"cat-mike","cat-tama"});natural.Simulation.Execute("start_business");
            for(int tick=0;tick<25 && natural.Simulation.State.phase=="open";tick++)
            {
                natural.Simulation.Execute("advance_business");apply(natural);
                foreach(var seat in natural.View().seats.Where(s=>s.cat_id!=null))
                {var e=natural.Simulation.State.exchanges.Single(x=>x.customer_id==seat.customer_id);Check(seat.narrative_image==natural.Narratives.LatestSeat(e)?.image_resource,"Cross-seat leak");}
                if(tick==15)
                {
                    Send(natural,"save_game");string seats=JsonUtility.ToJson(natural.View().seats[0]);
                    var reload=new LocalCafeBackend(Path.Combine(output,"saves"),Rules("cat-mike"));Send(reload,"load_game");
                    Check(seats==JsonUtility.ToJson(reload.View().seats[0]),"Disk resume changed seat");
                    apply(reload);Check(screen.OccupiedCount>=2,"Need simultaneous occupied seats");Capture(preview,Path.Combine(output,"multiple-normal.png"),1280,800);
                    client.ToggleSpeed();apply(reload);Capture(preview,Path.Combine(output,"multiple-fast.png"),1280,800);client.ToggleSpeed();
                    foreach(int width in new[]{1024,1920})Capture(preview,Path.Combine(output,"multiple-"+width+".png"),width,width==1024?768:1080);
                    var active=reload.Simulation.State.exchanges[0];string originalCat=active.cat_id;active.cat_id=originalCat=="cat-mike"?"cat-tama":"cat-mike";
                    Check(reload.Narratives.LatestSeat(active)==null,"Previous cat receipt leaked");active.cat_id=originalCat;
                    reload.Narratives.Restore(null);apply(reload);Check(screen.GetSeatCard(0).stateText.text.Contains("行動の記録なし"),"Invented legacy action");
                }
            }
            var empty=new LocalCafeBackend(Path.Combine(output,"other-save"),Rules("cat-mike"));apply(empty);
            Check(screen.GetSeatCard(0).picture.texture==null && !screen.GetSeatCard(0).stateText.text.Contains("直近の行動"),"Old save image persisted");
            var waiting=empty.View();waiting.can_set_shifts=false;waiting.service_assignment.waiting=true;screen.Apply(waiting);screen.ShowSeats();
            Check(screen.GetSeatCard(0).stateText.text.Contains("担当待ち"),"Waiting indistinguishable");
            var missing=natural.View();missing.seats[0].narrative_image="CatCafeContent/Imports/00000000000000000000000000000000/images/0000000000000000000000000000000000000000000000000000000000000000";screen.Apply(missing);screen.ShowSeats();
            Check(screen.GetSeatCard(0).picture.texture!=null,"Missing image has no fallback");
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS "+checks+" assertions. Real StepExchange boundary fixtures across eight cats/eight modes; actual multi-seat business, save/reload, layout and speed labels. No timed mouse play performed.");
            Debug.Log("CAT_CAFE_SEAT_ACTION_OK: "+checks);
        }
        private static CafeRules Rules(string id)
        {
            var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.store_events.probability=0;
            if(!rules.cats.Any(c=>c.id==id)){var original=new[]{rules.intake.cat,rules.regular_introduction.cat,rules.visiting_cat.cat}.Single(c=>c.id==id);rules.cats=rules.cats.Concat(new[]{JsonUtility.FromJson<CatDefinition>(JsonUtility.ToJson(original))}).ToArray();original.id="fixture-"+id;}return rules;
        }
        private static void Send(LocalCafeBackend b,string kind)
        {
            CafeResponse response=null;var cmd=new PythonShiftCommand{kind=kind,save_id="2",save_slot=2,instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,request_id=Guid.NewGuid().ToString("N")};
            var routine=b.Request("/commands",JsonUtility.ToJson(cmd),r=>response=r);while(routine.MoveNext()){}Check(response!=null && response.result==UnityEngine.Networking.UnityWebRequest.Result.Success,response?.text??"Save request failed");
        }
        private static void Capture(ContentPreview preview,string path,int width,int height)=>typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{preview,path,width,height});
    }
}
