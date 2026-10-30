#if CATCAFE_DEV_ACQUISITION
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class DevAcquisitionPlan
    {
        public int version=1,seed=0,max_days=150,max_operations=100000,max_seconds=7200,free_days=30,checkpoint_operations=200;
        public long max_log_bytes=268435456;
        public bool collection_routes;
        public string[] objectives={"popularity","patron","bond","free"};
        public string[] source_versions=new string[0];
        public void Validate()
        {
            if(version!=1 || seed<0 || max_days<1 || max_operations<1 || max_seconds<1 || free_days<1 || checkpoint_operations<1 || max_log_bytes<4096 || objectives==null || objectives.Length<1 || objectives.Length>4 || objectives.Distinct().Count()!=objectives.Length || objectives.Any(x=>!new[]{"popularity","patron","bond","free"}.Contains(x)))
                throw new ArgumentException("Invalid development acquisition plan");
        }
    }
    [Serializable] public sealed class DevAcquisitionTarget
    {
        public string category,id,status="unreached",reason="not_observed";
        public bool currently_owned,assigned;
        public string[] evidence_routes=new string[0];
    }
    [Serializable] public sealed class DevAcquisitionResult
    {
        public string mode="normal-acquisition",layer="internal-commands",objective,route_id,status,reason,state_digest;
        public string[] assigned_targets=new string[0];
        public string completion_scope="selected_objective_or_free_duration; not_all_collection_targets";
        public int seed,day,operations;
        public DevAcquisitionTarget[] targets;
    }
    [Serializable] public sealed class DevAcquisitionCheckpoint
    {
        public int version=1,route,operations,slot;
        public double elapsed_seconds;
        public string identity,save_sha256,status="paused",reason;
        public bool prepared;
    }
    [Serializable] public sealed class DevAcquisitionEvent
    {
        public string session,objective,route_id,before_digest,after_digest,error;
        public int sequence,day,tick;
        public CafeAutoOperation operation;
    }
    [Serializable] public sealed class DevAcquisitionSummary
    {
        public string mode="normal-acquisition",layer="internal-commands",status="running",reason;
        public DevAcquisitionResult[] routes;
        public DevAcquisitionTarget[] union;
        public string[] pending_objectives;
    }
    // This runner owns a separate simulation. It never edits the player's backend or its profile.
    // All progression below uses ordinary Execute/PlanAutoplay, never grants money/XP/flags.
    public sealed partial class CafeDevAcquisition : IDisposable
    {
        public const string RunnerVersion="2";
        readonly CafeRules rules;
        readonly DevCollectionRoute[] routes;
        readonly Func<object,string> encode;
        readonly Func<string,CafeSaveEnvelope> decodeSave;
        readonly Func<string,DevAcquisitionCheckpoint> decodeCheckpoint;
        readonly Func<string,DevAcquisitionResult> decodeResult;
        readonly string root,identity,session=Guid.NewGuid().ToString("N");
        readonly FileStream ownership;
        readonly List<DevAcquisitionResult> results=new List<DevAcquisitionResult>();
        readonly System.Diagnostics.Stopwatch watch=System.Diagnostics.Stopwatch.StartNew();
        double previousSeconds;
        int route,operations,slot;
        bool prepared;
        long logBytes;
        CafeInteractionStatistics statistics;
        public readonly DevAcquisitionPlan Plan;
        public CafeSimulation Simulation {get;private set;}
        public string Status {get;private set;}="paused";
        public string Reason {get;private set;}="awaiting_explicit_start";
        public bool Finished=>Status=="completed" || Status=="incomplete" || Status=="error";
        public string Objective=>routes[route].objective;
        public string RouteId=>routes[route].id;
        bool Collecting=>routes[route].targets.Length>0;
        public int Operations=>operations;
        string RouteRoot=>Path.Combine(root,"routes",RouteId);
        public static string Hash(byte[] bytes){using(var h=SHA256.Create())return string.Concat(h.ComputeHash(bytes).Select(b=>b.ToString("x2")));}
        public static void RejectLinks(string path)
        {
            for(var p=new DirectoryInfo(Path.GetFullPath(path));p!=null;p=p.Parent)
                if(p.Exists && (p.Attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("Development output must not follow links");
        }
        public CafeDevAcquisition(string output,CafeRules rules,DevAcquisitionPlan plan,string contentIdentity,
            Func<object,string> encode,Func<string,CafeSaveEnvelope> decodeSave,Func<string,DevAcquisitionCheckpoint> decodeCheckpoint,Func<string,DevAcquisitionResult> decodeResult,bool resume=false)
        {
            plan.Validate();if(!Path.IsPathRooted(output))throw new ArgumentException("Absolute development output required");
            this.root=Path.GetFullPath(output);RejectLinks(root);this.rules=rules;Plan=plan;this.encode=encode;this.decodeSave=decodeSave;this.decodeCheckpoint=decodeCheckpoint;this.decodeResult=decodeResult;routes=CreateRoutes(rules,plan);
            string assembly=typeof(CafeDevAcquisition).Assembly.Location;
            string binaryIdentity=File.Exists(assembly)?Hash(File.ReadAllBytes(assembly)):RunnerVersion;
            identity=Hash(Encoding.UTF8.GetBytes(RunnerVersion+"\n"+binaryIdentity+"\n"+encode(rules)+"\n"+encode(plan)+"\n"+contentIdentity));
            if(!resume && Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())throw new IOException("New run requires an empty output directory");
            if(resume && !File.Exists(Path.Combine(root,"manifest.txt")))throw new IOException("Run manifest missing");
            Directory.CreateDirectory(root);ownership=new FileStream(Path.Combine(root,"run.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            try
            {
                if(resume)
                {
                    if(File.ReadAllText(Path.Combine(root,"manifest.txt"))!=identity)throw new ArgumentException("Rules, content, runner or plan changed");
                    var cp=decodeCheckpoint(File.ReadAllText(Path.Combine(root,"checkpoint.json")));
                    if(cp==null || cp.version!=1 || cp.identity!=identity || cp.route<0 || cp.route>=routes.Length || cp.operations<0 || (cp.slot!=1 && cp.slot!=2) || cp.elapsed_seconds<0)throw new ArgumentException("Invalid checkpoint");
                    route=cp.route;operations=cp.operations;slot=cp.slot;prepared=cp.prepared;previousSeconds=cp.elapsed_seconds;
                    for(int i=0;i<route;i++)results.Add(decodeResult(File.ReadAllText(Path.Combine(root,"routes",routes[i].id,"result.json"))));
                    NewSimulation();string file=Path.Combine(RouteRoot,"saves","slot-"+slot+".json");
                    if(Hash(File.ReadAllBytes(file))!=cp.save_sha256)throw new IOException("Checkpoint save hash mismatch");
                    var save=Store().Load(slot);Simulation.Restore(save.state);statistics.Restore(save.interaction_statistics);
                    Reason="resume_requires_explicit_start";
                }
                else {File.WriteAllText(Path.Combine(root,"manifest.txt"),identity);File.WriteAllText(Path.Combine(root,"environment.txt"),"runner="+RunnerVersion+"\nassembly_sha256="+binaryIdentity+"\nrules_id="+rules.rules_id+"\ncontent_sha256="+Hash(Encoding.UTF8.GetBytes(contentIdentity)));File.WriteAllText(Path.Combine(root,"plan.json"),encode(plan));NewSimulation();Checkpoint("created");}
                logBytes=Directory.GetFiles(root,"events.jsonl",SearchOption.AllDirectories).Sum(f=>new FileInfo(f).Length);
                WriteSummary();
            }
            catch {ownership.Dispose();throw;}
            watch.Restart();
        }
        void NewSimulation(){Directory.CreateDirectory(RouteRoot);RejectLinks(RouteRoot);Simulation=new CafeSimulation(rules,Plan.seed,Objective);statistics=new CafeInteractionStatistics(Simulation);}
        CafeSaveStore Store(){RejectLinks(Path.Combine(RouteRoot,"saves"));return new CafeSaveStore(Path.Combine(RouteRoot,"saves"),s=>{s.double_bits=CafeDoublePrecision.Capture(s.state);s.interaction_statistics=statistics.Data;return encode(s);},decodeSave,Simulation.Validate);}
        public void Start(){if(Status!="paused")throw new InvalidOperationException("Only a paused run can start");Status="running";Reason="";watch.Restart();}
        double Elapsed=>previousSeconds+(Status=="running"?watch.Elapsed.TotalSeconds:0);
        public void Pause(string reason="user_pause")
        {
            if(Status!="running")return;previousSeconds+=watch.Elapsed.TotalSeconds;Status="paused";Reason=reason;Checkpoint(reason);WriteSummary();
        }
        public void Checkpoint(string reason)
        {
            RejectLinks(root);RejectLinks(RouteRoot);int next=slot==1?2:1;Store().Save(next,Simulation.State);
            var cp=new DevAcquisitionCheckpoint{route=route,operations=operations,slot=next,identity=identity,save_sha256=Hash(File.ReadAllBytes(Path.Combine(RouteRoot,"saves","slot-"+next+".json"))),elapsed_seconds=Elapsed,status=Status,reason=reason,prepared=prepared};
            Atomic(Path.Combine(root,"checkpoint.json"),encode(cp));slot=next;
        }
        static void Atomic(string path,string value)
        {
            string tmp=path+".tmp";using(var f=new FileStream(tmp,FileMode.Create,FileAccess.Write,FileShare.None)){var b=Encoding.UTF8.GetBytes(value);f.Write(b,0,b.Length);f.Flush(true);}
            if(File.Exists(path))CafeSaveStore.ReplaceWithRetry(tmp,path);else File.Move(tmp,path);
        }
        public void Step()
        {
            if(Status!="running")return;
            try
            {
                var s=Simulation.State;
                if(s.game_over!=null){EndRoute("unreached","game_over/"+s.game_over);return;}
                string impossible=CollectionImpossible();if(impossible!=null){EndRoute("unreached",impossible);return;}
                if(Collecting && AssignedComplete()){EndRoute("acquired","assigned_collection_completed");return;}
                if(!Collecting && Objective!="free" && Simulation.AutoTargetStatus(Objective)=="cleared" && (Objective!="popularity" || s.goal.Count==rules.goal_stages.Length)){EndRoute("acquired","objective_completed");return;}
                if(!Collecting && Objective=="free" && s.day>Plan.free_days){EndRoute("acquired","free_duration_completed");return;}
                if(s.goal.Any(g=>g.status=="expired")){EndRoute("unreached","objective_expired");return;}
                if(s.day>Plan.max_days || operations>=Plan.max_operations || Elapsed>=Plan.max_seconds){EndRoute("unreached","limit");return;}
                string log=Path.Combine(RouteRoot,"events.jsonl");
                if(logBytes+4096>Plan.max_log_bytes){EndRoute("unreached","log_limit");return;}
                var op=NextOperation();if(op==null){EndRoute("unreached","unknown_prompt_or_stalled");return;}
                var row=new DevAcquisitionEvent{session=session,sequence=operations+1,objective=Objective,route_id=RouteId,day=s.day,tick=s.tick,operation=op,before_digest=CafeReplayJournal.Digest(s)};
                try {Simulation.Execute(op.kind,op.working,op.cat_id,op.choice,op.source,op.seat_id);}
                catch(Exception e){row.error=e.Message;AppendEvent(log,row);throw;}
                operations++;row.after_digest=CafeReplayJournal.Digest(Simulation.State);AppendEvent(log,row);
                if(row.before_digest==row.after_digest){EndRoute("unreached","no_progress/"+op.kind);return;}
                if(operations%Plan.checkpoint_operations==0){Simulation.Validate(Simulation.State);Checkpoint("periodic");WriteSummary();}
            }
            catch(Exception e)
            {
                Status="error";Reason=e.ToString();
                // Preserve the last good checkpoint; do not overwrite it with a failed operation.
                Atomic(Path.Combine(root,"failure.txt"),Reason);WriteSummary();
            }
        }
        void AppendEvent(string file,DevAcquisitionEvent row){string text=encode(row)+"\n";File.AppendAllText(file,text);logBytes+=Encoding.UTF8.GetByteCount(text);}
        CafeAutoOperation Op(string kind,string cat=null,string choice=null,string source=null,string seat=null,string[] working=null)=>new CafeAutoOperation{kind=kind,cat_id=cat,choice=choice,source=source,seat_id=seat,working=working};
        CafeAutoOperation NextOperation()
        {
            var sim=Simulation;var s=sim.State;
            if(sim.AdoptionWaiting)return Op("resolve_adoption",choice:"decline",source:s.adoption.events.First(e=>e.status=="waiting").id);
            if(sim.PlayerActive)return sim.PlanAutoplay(Objective=="bond"?"bond":"popularity","fast");
            if(sim.GrowthWaiting)
            {
                var sora=s.growth.FirstOrDefault(g=>g.cat_id=="cat-sora" && sim.GrowthPendingKind(g)=="resolve_growth" && sim.GrowthSelection(g)?.choice!="service");
                if(Objective=="patron" && sora!=null)return Op("resolve_growth_stage",sora.cat_id,"service");
                return Op(s.growth.Any(g=>sim.GrowthPendingKind(g)!=null && sim.GrowthSelection(g)==null)?"resolve_growth_auto":"resolve_growth_batch");
            }
            if(sim.DispatchTroubleWaiting)return Op("resolve_dispatch_trouble",choice:"wait",source:s.dispatches.First(e=>e.trouble?.status=="waiting").id);
            if(sim.DispatchChoiceWaiting){var e=s.dispatches.First(x=>x.encounter?.status=="waiting");return Op("resolve_dispatch_choice",choice:sim.DispatchEncounterDefinition(e.destination,e.started_day).choices.Last().id,source:e.id);}
            if(sim.DispatchWaiting)return Op("resolve_activity",choice:"receive",source:s.dispatches.First(e=>e.status=="waiting").id);
            var missing=s.cats.FirstOrDefault(c=>c.activity=="missing" && c.missing_remaining==0);if(missing!=null)return Op("resolve_missing",missing.id);
            if(sim.ReservationWaiting)return Op("resolve_reservation",choice:"accept");
            if(sim.GoalWaiting)return Op(s.goal.Last().status=="cleared" && s.goal.Count<rules.goal_stages.Length?"advance_goal":"continue_goal");
            if(sim.PatronWaiting)return Op("continue_patron");if(sim.BondWaiting)return Op("continue_bond_goal");
            if(sim.StoreEventWaiting)return Op("resolve_store_event",choice:sim.CurrentStoreEvent.type=="trouble"?(s.funds>rules.store_events.trouble_cost+300?"repair":"patch"):"decline");
            if(sim.CustomerTrustWaiting)return Op("resolve_customer_trust",choice:"recover",source:s.trust_events.First(e=>e.status=="waiting").id);
            if(sim.DispatchIntroductionWaiting){var e=s.dispatches.First(x=>x.introduction?.status=="waiting");return Op("resolve_dispatch_introduction",choice:sim.DispatchIntroductionAcceptReason(e.id)=="" && WantCat("intro-"+e.destination)?"accept":"decline",source:e.id);}
            if(sim.IntakeWaiting)return Op("resolve_intake",choice:sim.IntakeAcceptReason=="" && WantCat(rules.intake.cat.id)?"accept":"decline");
            if(sim.RegularIntroductionWaiting)return Op("resolve_regular_introduction",choice:sim.RegularIntroductionAcceptReason=="" && WantCat(rules.regular_introduction.cat.id)?"accept":"decline");
            if(s.phase=="preparation" && sim.ItemsReason()=="")
            {
                if(WantCat(rules.visiting_cat.cat.id) && sim.VisitingAdmissionReason()=="")return Op("resolve_visiting_cat",choice:"accept");
                if(WantCat(rules.visiting_cat.cat.id) && s.visiting_cat?.status=="visiting" && sim.VisitingDailyReason()=="")return Op("resolve_visiting_cat",choice:"interact");
                if(Collecting){var collection=PlanCollection();if(collection!=null)return collection;}
                // Keep the original four-route policy unchanged when collection is disabled.
                if(!Collecting && Objective=="free" && s.funds>2500)
                {
                    foreach(var item in rules.item_shop)if(!s.item_purchases.Any(p=>p.item_id==item.item.id) && sim.ItemPurchaseReason(item.item.id)=="")return Op("purchase_item",choice:item.item.id);
                    foreach(var e in rules.equipment)if(!s.equipment_purchases.Any(p=>p.kind==e.id) && sim.EquipmentPurchaseReason(e.id)=="")return Op("purchase_seat_equipment",choice:e.id,seat:"seat-1");
                    if(sim.HousingReason("purchase_housing")=="")return Op("purchase_housing");
                    var cat=rules.pet_shop.FirstOrDefault(p=>sim.CatPurchaseReason(p.cat.id)=="");if(cat!=null)return Op("purchase_cat",cat.cat.id);
                    if(sim.HousingReason("upgrade_housing")=="")return Op("upgrade_housing");
                }
                // Train the strict patron's service specialist with normal shifts, as in the existing UI route.
                bool training=Objective=="patron" && s.growth.Any(g=>g.cat_id=="cat-sora" && g.specialization==null);
                if(training)
                {
                    var workers=s.cats.Where(c=>(c.id=="cat-sora" || c.id=="cat-mike") && c.activity=="cafe" && c.health=="healthy" && c.stamina>0).Select(c=>c.id).ToArray();
                    if(!s.cats.Where(c=>c.working).Select(c=>c.id).OrderBy(x=>x).SequenceEqual(workers.OrderBy(x=>x)))return Op("set_shifts",working:workers);
                    return workers.Length>0?Op("start_business"):Op("day_off");
                }
                if(!prepared){prepared=true;if(!s.auto_assignment)return Op("set_auto_assignment",choice:"automatic");}
                if(!Collecting && Objective=="free" && s.day>=3 && !s.dispatches.Any(e=>e.destination=="neighborhood_visit"))
                {var cat=s.cats.FirstOrDefault(c=>sim.DispatchReason(c.id,"neighborhood_visit")=="");if(cat!=null)return Op("dispatch_general",cat.id,"neighborhood_visit");}
            }
            return sim.PlanAutoplay(Objective=="free"?"popularity":Objective,"fast");
        }
        void EndRoute(string status,string reason)
        {
            Simulation.Validate(Simulation.State);var result=Result(status,reason);Atomic(Path.Combine(RouteRoot,"result.json"),encode(result));results.Add(result);Checkpoint(reason);
            if(route+1<routes.Length)
            {
                route++;operations=0;slot=0;prepared=false;previousSeconds=0;watch.Restart();NewSimulation();Checkpoint("next_route");WriteSummary();return;
            }
            Status=results.All(r=>r.status=="acquired")?"completed":"incomplete";Reason=reason;WriteSummary();
        }
        public DevAcquisitionResult Result(string status=null,string reason=null)=>new DevAcquisitionResult{objective=Objective,route_id=RouteId,assigned_targets=routes[route].targets,completion_scope=Collecting?"assigned_collection_targets":"selected_objective_or_free_duration; not_all_collection_targets",status=status??Status,reason=reason??Reason,seed=Plan.seed,day=Simulation.State.day,operations=operations,state_digest=CafeReplayJournal.Digest(Simulation.State),targets=Targets()};
        DevAcquisitionTarget[] Targets()
        {
            var s=Simulation.State;var list=new List<DevAcquisitionTarget>();
            Action<string,string,bool,bool,string> add=(cat,id,got,owned,why)=>list.Add(new DevAcquisitionTarget{category=cat,id=id,status=got?"acquired":"unreached",currently_owned=owned,reason=got?"normal_command_state":string.IsNullOrEmpty(why)?"available_not_pursued":why});
            var owned=new HashSet<string>(s.cats.Select(c=>Simulation.ContentImageId(c.id)));
            var active=new HashSet<string>(s.cats.Where(c=>c.activity!="adopted").Select(c=>Simulation.ContentImageId(c.id)));
            var ids=rules.cats.Select(c=>c.id).Concat(new[]{rules.intake.cat.id,rules.regular_introduction.cat.id,rules.visiting_cat.cat.id}).Concat(rules.pet_shop.Select(c=>c.cat.id)).Concat(rules.recruitment_catalog.Select(c=>c.cat.id)).Concat(rules.dispatch_introductions.Select(c=>"intro-"+c.destination));
            foreach(string id in ids)add("cat",id,owned.Contains(id),active.Contains(id),"admission_not_observed");
            foreach(var e in rules.equipment)add("equipment",e.id,s.equipment_purchases.Any(p=>p.kind==e.id),s.equipment_purchases.Any(p=>p.kind==e.id),Simulation.EquipmentPurchaseReason(e.id));
            for(int i=1;i<=rules.expansion_costs.Length;i++)add("facility","seat-expansion-"+i,s.expansion.Count>=i,s.expansion.Count>=i,Simulation.ExpansionReason());
            add("facility","rest_space",s.rest_space.purchased_day>0,s.rest_space.purchased_day>0,Simulation.RestSpaceReason("purchase_rest_space"));
            add("facility","rest_space_upgrade",s.rest_space.upgraded_day>0,s.rest_space.upgraded_day>0,Simulation.RestSpaceReason("upgrade_rest_space"));
            add("facility","rest_space_soundproof",s.rest_space.soundproofed_day>0,s.rest_space.soundproofed_day>0,Simulation.RestSpaceReason("soundproof_rest_space"));
            add("facility","waiting_area",s.waiting_area.purchased_day>0,s.waiting_area.purchased_day>0,Simulation.WaitingAreaReason("purchase_waiting_area"));
            add("facility","waiting_area_upgrade",s.waiting_area.upgraded_day>0,s.waiting_area.upgraded_day>0,Simulation.WaitingAreaReason("upgrade_waiting_area"));
            add("facility","housing",s.housing.purchased_day>0,s.housing.purchased_day>0,Simulation.HousingReason("purchase_housing"));
            add("facility","housing_upgrade",s.housing.upgraded_day>0,s.housing.upgraded_day>0,Simulation.HousingReason("upgrade_housing"));
            foreach(var item in rules.item_sale_prices)
            {
                bool got=s.item_purchases.Any(p=>p.item_id==item.id) || s.store_events.Any(e=>e.type=="supplies" && rules.store_events.supply.id==item.id) || s.dispatches.Any(e=>e.status=="resolved" && Simulation.DispatchDestination(e.destination).item_reward?.id==item.id);
                add("supply",item.id,got,Simulation.ItemInventory().Any(i=>i.item.id==item.id),"source_not_acquired");
            }
            foreach(var d in rules.dispatch_destinations.Concat(new[]{rules.patron.destination}).Concat(rules.patron.members.Select(m=>new DispatchRules{id=m.id})))
            {
                add("dispatch",d.id,s.dispatches.Any(e=>e.destination==d.id && e.status=="resolved"),false,Simulation.DispatchUnlockReason(d.id));
                if(!string.IsNullOrEmpty(d.unsupported_reason)){list.Last().status="excluded";list.Last().reason="unimplemented/"+d.unsupported_reason;}
                else if(Simulation.IsPatronDestination(d.id) && Objective!="patron"){list.Last().status="excluded";list.Last().reason="requires_patron_route";}
            }
            for(int i=0;i<rules.goal_stages.Length;i++)add("objective","popularity/stage-"+(i+1),s.goal.Count>i && s.goal[i].status=="cleared",false,"objective_not_cleared");
            add("objective","patron",s.patron?.status=="cleared",false,"objective_not_cleared");add("objective","bond",s.bond_goal?.status=="cleared",false,"objective_not_cleared");add("start",Objective,true,false,"");
            foreach(string id in new[]{"service","rest","dispatch"})add("specialization",id,s.growth.Any(g=>g.specialization==id),false,"not_selected");
            foreach(string id in new[]{"play","contact","quiet"})add("mastery_group",id,s.growth.Any(g=>CafeSimulation.LearnedGroups(g).Contains(id)),false,"not_learned");
            foreach(var type in rules.types)add("mastery_type",type.id,s.growth.Any(g=>CafeSimulation.LearnedTypes(g).Contains(type.id)),false,"not_learned");
            foreach(var d in CafeReplayCatalog.All)list.Add(new DevAcquisitionTarget{category="replay",id=d.Key,status="excluded",reason="internal_runner_does_not_observe_dialogue_reading"});
            list.Add(new DevAcquisitionTarget{category="image",id="rendered-gallery",status="excluded",reason="internal_runner_does_not_render_images"});
            foreach(var target in list)target.assigned=routes[route].targets.Contains(target.category+"/"+target.id);
            return list.ToArray();
        }
        public void WriteSummary()
        {
            var rows=results.ToList();if(!rows.Any(r=>r.route_id==RouteId))rows.Add(Result());
            var union=rows.SelectMany(r=>r.targets).GroupBy(t=>t.category+"/"+t.id).Select(g=>
            {
                var best=g.FirstOrDefault(t=>t.status=="acquired")??g.FirstOrDefault(t=>t.status!="excluded")??g.First();
                return new DevAcquisitionTarget{category=best.category,id=best.id,status=best.status,reason=best.reason,assigned=g.Any(t=>t.assigned),evidence_routes=rows.Where(r=>r.targets.Any(t=>t.category==best.category && t.id==best.id && t.status=="acquired")).Select(r=>r.route_id).ToArray()};
            }).ToArray();
            Atomic(Path.Combine(root,"summary.json"),encode(new DevAcquisitionSummary{status=Status,reason=Reason,routes=rows.ToArray(),union=union,pending_objectives=routes.Select(r=>r.id).Except(rows.Select(r=>r.route_id)).ToArray()}));
            var csv=new StringBuilder("scope,category,id,status,reason,currently_owned,assigned,evidence_routes\n");
            Action<string,DevAcquisitionTarget> line=(scope,t)=>csv.AppendLine(string.Join(",",new[]{scope,t.category,t.id,t.status,t.reason,t.currently_owned.ToString(),t.assigned.ToString(),string.Join(";",t.evidence_routes)}.Select(v=>"\""+(v??"").Replace("\"","\"\"")+"\"")));
            foreach(var row in rows)foreach(var target in row.targets)line(row.route_id,target);
            foreach(var target in union)line("union",target);
            Atomic(Path.Combine(root,"coverage.csv"),csv.ToString());
        }
        public void Dispose(){ownership.Dispose();}
    }
}
#endif
