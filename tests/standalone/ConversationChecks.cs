using CatCafePreview.Standalone;
static partial class Program
{
    static void TrustDialogueChecks(CafeRules rules)
    {
        foreach(string branch in new[]{"recover","ignore","recovered","departed"})
        {
            bool result=branch=="recovered" || branch=="departed";
            var sim=new CafeSimulation(rules);var e=new CafeTrustEvent{id="trust-1-guest-2",customer_id="guest-2",day=1,status=result || branch=="ignore"?"resolved":"recovery",choice=branch=="ignore"?"ignore":"recover",outcome=result?branch:branch=="ignore"?"departed":null};sim.State.trust_events.Add(e);
            var row=new CafeTrustConversation{event_id=e.id,observe_result=true,response=result?null:CafeTrustDialogue.Start()};CafeTrustDialogue.Sync(row,e);
            var p=row.Current;Assert(p.branch_id==branch,"Trust branch mismatch");
            for(int n=0;n<=3;n++)
            {
                p.node=n;p.page_offset=n<3?2:0;
                var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,trust_conversations=new[]{row},trust_conversation_selected=e.id}));
                CafeTrustDialogue.Validate(save.trust_conversations,save.trust_conversation_selected,save.state);
                Assert(save.trust_conversations[0].Current.node==n && save.trust_conversations[0].Current.page_offset==p.page_offset,"Trust position lost");
                Assert(save.trust_conversations[0].Current.viewed_branches.Length==0,"Unread trust unlocked");
            }
            string before=Encode(sim.State);CafeTrustDialogue.Finish(row);CafeTrustDialogue.Finish(row);Assert(before==Encode(sim.State),"Trust viewing changed effects");
            Assert(p.completed && p.viewed_branches.SequenceEqual(new[]{branch}),"Trust duplicate completion");CafeTrustDialogue.Validate(new[]{row},e.id,sim.State);
            var entries=CafeReplayCatalog.FromSave(new CafeSaveEnvelope{state=sim.State,trust_conversations=new[]{row}},rules,id=>id,id=>"佐藤さん",id=>id);
            Assert(entries.Length==1 && entries[0].lines.Any(l=>l.text.Contains("佐藤さん")),"Trust replay missing actual customer");
            Assert(before==Encode(sim.State),"Trust replay changed gameplay");
            p.branch_id="forged";bool rejected=false;try{CafeTrustDialogue.Validate(new[]{row},e.id,sim.State);}catch(ArgumentException){rejected=true;}Assert(rejected,"Forged trust branch accepted");
        }
        var legacy=new CafeSimulation(rules);foreach(string status in new[]{"waiting","recovery","resolved"}){legacy.State.trust_events.Add(new CafeTrustEvent{id=status,status=status});CafeTrustDialogue.Validate(null,null,legacy.State);Assert(CafeReplayCatalog.FromSave(new CafeSaveEnvelope{state=legacy.State},rules,id=>id,id=>id,id=>id).Length==0,"Legacy trust fabricated viewing");}
    }
    static void Rejected(Action action){bool rejected=false;try{action();}catch(InvalidOperationException){rejected=true;}Assert(rejected,"Invalid choice accepted");}
    static void ConversationChecks(string rulePath)
    {
        var rules=Decode<CafeRules>(File.ReadAllText(rulePath));
        AdoptionDialogueChecks(rules);TrustDialogueChecks(rules);
        foreach(var answer in new[]{"accept","decline"})
        {
            var sim=new CafeSimulation(rules);
            while(sim.State.day<4){if(sim.StoreEventWaiting)sim.Execute("resolve_store_event",choice:sim.CurrentStoreEvent.type=="trouble"?"patch":"decline");sim.Execute("day_off");}
            var p=CafeIntakeConversation.Start();
            for(int i=0;i<=CafeIntakeConversation.Lines("introduction","ナナ").Length;i++)
            {
                p.node=i;var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,conversation=p}));CafeIntakeConversation.Validate(save.conversation,"waiting");sim.Restore(save.state);p=save.conversation;Assert(p.node==i,"Conversation position lost");
            }
            if(sim.StoreEventWaiting){string before=Encode(sim.State);Rejected(()=>sim.Execute("resolve_intake",choice:answer));Assert(before==Encode(sim.State),"Order rejection mutated state");sim.Execute("resolve_store_event",choice:sim.CurrentStoreEvent.type=="trouble"?"patch":"decline");}
            double funds=sim.State.funds;sim.State.funds=rules.intake.cost;Rejected(()=>sim.Execute("resolve_intake",choice:"accept"));Assert(sim.IntakeAcceptReason.Contains("資金"),"Exact cost accepted");sim.State.funds=funds;
            int capacity=rules.housing.initial_capacity;rules.housing.initial_capacity=sim.ActiveCatCount(sim.State);Assert(sim.IntakeAcceptReason.Contains("満員"),"Housing reason missing");Rejected(()=>sim.Execute("resolve_intake",choice:"accept"));rules.housing.initial_capacity=capacity;
            sim.Execute("resolve_intake",choice:answer);CafeIntakeConversation.SyncAnswer(p,sim.State.intake_request.status);
            for(int i=0;i<=CafeIntakeConversation.Lines(p.branch_id,"ナナ").Length;i++)
            {
                p.node=i;var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,conversation=p}));CafeIntakeConversation.Validate(save.conversation,save.state.intake_request.status);sim.Restore(save.state);p=save.conversation;
                string before=Encode(sim.State);for(int j=0;j<5;j++)sim.Execute("resolve_intake",choice:answer);Assert(before==Encode(sim.State),"Repeated answer charged again");
                Assert(sim.State.funds==funds-(answer=="accept"?rules.intake.cost:0),"Expense differs");
            }
            CafeIntakeConversation.Finish(p);CafeIntakeConversation.Finish(p);Assert(p.completed && p.viewed_branches.Length==1,"Completion duplicated");CafeIntakeConversation.Validate(p,sim.State.intake_request.status);
            var old=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State}));Assert(old.conversation==null,"Legacy save fabricated viewing");CafeIntakeConversation.Validate(null,old.state.intake_request.status);
            p.branch_id=answer=="accept"?"declined":"accepted";bool rejected=false;try{CafeIntakeConversation.Validate(p,sim.State.intake_request.status);}catch(ArgumentException){rejected=true;}Assert(rejected,"Mismatched branch accepted");
        }
        int ordered=0;
        for(int seed=0;seed<32;seed++)
        {
            var sim=new CafeSimulation(rules,seed:seed);
            while(sim.State.day<4){if(sim.StoreEventWaiting)sim.Execute("resolve_store_event",choice:sim.CurrentStoreEvent.type=="trouble"?"patch":"decline");sim.Execute("day_off");}
            if(sim.StoreEventWaiting){string before=Encode(sim.State);Rejected(()=>sim.Execute("resolve_intake",choice:"accept"));Rejected(()=>sim.Execute("resolve_intake",choice:"decline"));Assert(before==Encode(sim.State),"Store answer order mutated game");ordered++;sim.Execute("resolve_store_event",choice:sim.CurrentStoreEvent.type=="trouble"?"patch":"decline");}
            var cat=sim.State.cats[0];string activity=cat.activity;cat.activity="missing";cat.missing_remaining=0;Rejected(()=>sim.Execute("resolve_intake",choice:"accept"));Rejected(()=>sim.Execute("resolve_intake",choice:"decline"));cat.activity=activity;
        }
        Assert(ordered>0,"No overlapping event order cases exercised");
        var invalid=CafeIntakeConversation.Start();invalid.node=99;bool invalidRejected=false;try{CafeIntakeConversation.Validate(invalid,"waiting");}catch(ArgumentException){invalidRejected=true;}Assert(invalidRejected,"Invalid node accepted");
        ImageGalleryChecks();
        ReplayArchiveChecks(rules);
        DispatchIntroductionDialogueChecks(rules);
        DispatchIntroductionOrderChecks(rules);
        StudioShoppingOverlapChecks(rules);
        LodgeConversationChecks(rules);
        EncounterConversationChecks(rules);
        EncounterOrderChecks(rules);
        DispatchConversationChecks(rules);
        RegularConversationChecks(rules);
        DayOffProgressChecks(rules);
        IntakeChecks.Run(rules,Assert,Path.GetFullPath("Logs/StandaloneConversation/core-saves"));
        Console.WriteLine("PASS conversation: "+assertions+" assertions; both branches, every node save roundtrip, old saves, funds, housing, order, repeated answers.");
    }

    static void ImageGalleryChecks()
    {
        string a="normal/"+new string('a',64),b="normal/"+new string('b',64),c="normal/"+new string('c',64);string folder=Path.GetFullPath("Logs/StandaloneCGGallery/core/"+Guid.NewGuid().ToString("N"));
        var store=new CafeImageGalleryStore(folder,v=>Encode(v),v=>Decode<CafeImageProfile>(v));var stale=new CafeImageGalleryStore(folder,v=>Encode(v),v=>Decode<CafeImageProfile>(v));
        Assert(store.Load().viewed.Length==0,"CG new profile unlocked");Assert(CafeImageGallery.Key("images/"+new string('a',64)+".png")==a,"CG asset identity wrong");Assert(CafeImageGallery.Key("../hidden.png")==null && CafeImageGallery.Key("")==null,"CG invalid path accepted");
        Assert(store.Merge(new[]{a,a}) && stale.Merge(new[]{b}) && store.Load().viewed.Length==2,"CG union/duplicate lost");string path=Path.Combine(folder,"extra-images-v1.json"),before=File.ReadAllText(path);
        var fault=new CafeImageGalleryStore(folder,v=>throw new IOException("injected"),v=>Decode<CafeImageProfile>(v));Assert(!fault.Merge(new[]{c}) && File.ReadAllText(path)==before,"CG write failure replaced profile");
        File.WriteAllText(path,"corrupt");Assert(store.Load().viewed.SequenceEqual(new[]{a}) && store.Recovered,"CG backup recovery failed");Assert(store.Merge(new[]{a,b}) && store.Load().viewed.Length==2 && File.Exists(path+".invalid"),"CG recovery union failed");File.WriteAllText(path,"corrupt");File.WriteAllText(path+".bak","corrupt");Assert(!store.Merge(new[]{c}) && File.ReadAllText(path)=="corrupt","CG corrupt profile overwritten");
        foreach(var bad in new[]{new CafeImageProfile{version=1,viewed=new[]{a,a}},new CafeImageProfile{version=1,viewed=new[]{"play/"+new string('a',64)}},new CafeImageProfile{viewed=new[]{a}},new CafeImageProfile{version=2,viewed=new[]{a}}}){bool rejected=false;try{CafeImageGallery.Validate(bad);}catch(ArgumentException){rejected=true;}Assert(rejected,"CG invalid record accepted");}
    }
    static void ReplayArchiveChecks(CafeRules defaults)
    {
        string folder=Path.GetFullPath("Logs/StandaloneExtraConversation/core/"+Guid.NewGuid().ToString("N"));
        var store=new CafeReplayStore(folder,v=>Encode(v),v=>Decode<CafeReplayProfile>(v));var stale=new CafeReplayStore(folder,v=>Encode(v),v=>Decode<CafeReplayProfile>(v));
        Assert(store.Load().entries.Length==0,"New profile unlocked");
        foreach(string answer in new[]{"accept","decline"})
        {
            var r=Decode<CafeRules>(Encode(defaults));r.store_events.probability=0;var sim=new CafeSimulation(r);while(sim.State.day<4)sim.Execute("day_off");sim.Execute("resolve_intake",choice:answer);var p=CafeIntakeConversation.Start();CafeIntakeConversation.SyncAnswer(p,sim.State.intake_request.status);
            var save=new CafeSaveEnvelope{state=sim.State,conversation=p};string before=Encode(sim.State);Assert(CafeReplayCatalog.FromSave(save,r,id=>id,id=>id,id=>id).Length==0,"Incomplete view unlocked");p.node=4;CafeIntakeConversation.Finish(p);var rows=CafeReplayCatalog.FromSave(save,r,id=>id,id=>id,id=>id);Assert(rows.Length==1 && rows[0].lines.Length==9 && rows[0].branch_id==p.branch_id,"Wrong replay script");Assert(before==Encode(sim.State),"Replay extraction changed simulation");Assert(store.Merge(rows) && stale.Merge(rows),"Profile merge failed");Assert(store.Load().entries.Length==(answer=="accept"?1:2),"Union lost or duplicated branch");
            save.conversation=null;Assert(CafeReplayCatalog.FromSave(save,r,id=>id,id=>id,id=>id).Length==0,"Legacy response fabricated viewing");
        }
        string path=Path.Combine(folder,"extra-v1.json"),good=File.ReadAllText(path);var broken=new CafeReplayStore(folder,v=>throw new IOException("injected"),v=>Decode<CafeReplayProfile>(v));
        var extra=Decode<CafeReplayEntry>(Encode(Decode<CafeReplayProfile>(good).entries[0]));extra.conversation_id=CafeRegularConversation.Id;Assert(!broken.Merge(new[]{extra}) && File.ReadAllText(path)==good,"Write failure replaced existing profile");
        File.WriteAllText(path,"broken");Assert(store.Load().entries.Length==1 && store.Recovered,"Profile backup not recovered");var saved=Decode<CafeReplayProfile>(good);Assert(store.Merge(saved.entries) && store.Load().entries.Length==2 && File.Exists(path+".invalid"),"Recovery lost union");
        var changed=Decode<CafeReplayProfile>(good);changed.entries[0].lines[0].text="snapshot";Assert(store.Merge(changed.entries) && store.Load().entries[0].lines[0].text!="snapshot","Repeated viewing replaced first snapshot");
        string failureFolder=folder+"-failure";Directory.CreateDirectory(failureFolder);var errorStore=new CafeReplayStore(failureFolder,v=>throw new IOException("injected"),v=>Decode<CafeReplayProfile>(v));Assert(!errorStore.Merge(saved.entries) && errorStore.Error!=null && !File.Exists(Path.Combine(failureFolder,"extra-v1.json")),"Write failure reported success");
        File.WriteAllText(path,"broken");File.WriteAllText(path+".bak","broken");Assert(!store.Merge(saved.entries) && File.ReadAllText(path)=="broken","Corrupt profile overwritten");
        var bad=Decode<CafeReplayProfile>(good);bad.entries[0].branch_id="unseen";bool rejected=false;try{CafeReplayCatalog.Validate(bad);}catch(ArgumentException){rejected=true;}Assert(rejected,"Unknown branch accepted");
    }
    static void DispatchIntroductionDialogueChecks(CafeRules defaults)
    {
        foreach(string destination in new[]{CafeDispatchIntroductionDialogue.Destination,CafeDispatchIntroductionDialogue.StudioDestination})
        foreach(string choice in new[]{"accept","decline"})
        {
            var r=Decode<CafeRules>(Encode(defaults));r.store_events.probability=0;r.dispatch_unlocks=r.dispatch_unlocks.Where(u=>u.id!=destination).ToArray();
            var sim=new CafeSimulation(r);sim.Execute("dispatch_general",catId:destination=="out_of_town_visit"?"cat-tama":"cat-mike",choice:destination);sim.Execute("day_off");var ev=sim.State.dispatches.Single();sim.Execute("resolve_dispatch_choice",source:ev.id,choice:"decline");sim.Execute("day_off");sim.Execute("resolve_activity",source:ev.id,choice:"receive");
            var row=new CafeDispatchIntroductionConversation{event_id=ev.id,progress=CafeDispatchIntroductionDialogue.Start(destination)};var rows=new[]{row};double funds=sim.State.funds;int count=sim.State.cats.Count;
            for(int i=0;i<=CafeDispatchIntroductionDialogue.Lines("introduction","").Length;i++){row.progress.node=i;var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,dispatch_introduction_conversations=rows}));sim.Restore(save.state);CafeDispatchIntroductionDialogue.Validate(save.dispatch_introduction_conversations,ev.id,sim.State);Assert(save.dispatch_introduction_conversations[0].progress.node==i,"Dispatch introduction node lost");}
            double cost=ev.introduction.cost;sim.State.funds=cost;Assert(sim.DispatchIntroductionAcceptReason(ev.id).Contains("資金"),"Exact cost accepted");Rejected(()=>sim.Execute("resolve_dispatch_introduction",source:ev.id,choice:"accept"));sim.State.funds=funds;
            int capacity=r.housing.initial_capacity;r.housing.initial_capacity=count;Assert(sim.DispatchIntroductionAcceptReason(ev.id).Contains("満員"),"Full housing accepted");Rejected(()=>sim.Execute("resolve_dispatch_introduction",source:ev.id,choice:"accept"));r.housing.initial_capacity=capacity;
            var growth=sim.State.growth[0];double oldService=growth.service;string oldSpec=growth.specialization;growth.specialization=null;growth.service=r.growth.threshold;
            Assert(sim.DispatchIntroductionResponseReason(ev.id).Contains("成長"),"Growth priority missing");Rejected(()=>sim.Execute("resolve_dispatch_introduction",source:ev.id,choice:"decline"));growth.service=oldService;growth.specialization=oldSpec;
            sim.State.goal.Add(new CafeGoalStage{status="cleared"});Assert(sim.DispatchIntroductionResponseReason(ev.id).Contains("目標"),"Goal priority missing");Rejected(()=>sim.Execute("resolve_dispatch_introduction",source:ev.id,choice:"decline"));sim.State.goal.RemoveAt(sim.State.goal.Count-1);
            string phase=sim.State.phase;sim.State.phase="open";Assert(sim.DispatchIntroductionResponseReason(ev.id)!="","Open-phase introduction allowed");sim.State.phase=phase;
            sim.Execute("resolve_dispatch_introduction",source:ev.id,choice:choice);CafeDispatchIntroductionDialogue.Sync(row,sim.State.dispatches.Single());string state=Encode(sim.State);for(int n=0;n<5;n++)sim.Execute("resolve_dispatch_introduction",source:ev.id,choice:choice);Assert(state==Encode(sim.State),"Duplicate joining or expense");
            Assert(sim.State.funds==funds-(choice=="accept"?cost:0) && sim.State.cats.Count==count+(choice=="accept"?1:0),"Introduction effect changed");
            for(int i=0;i<=CafeDispatchIntroductionDialogue.Lines(row.progress.branch_id,"").Length;i++){row.progress.node=i;var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,dispatch_introduction_conversations=rows}));sim.Restore(save.state);CafeDispatchIntroductionDialogue.Validate(save.dispatch_introduction_conversations,ev.id,sim.State);Assert(save.dispatch_introduction_conversations[0].progress.node==i,"Introduction result lost");}
            CafeDispatchIntroductionDialogue.Finish(row.progress);CafeDispatchIntroductionDialogue.Finish(row.progress);CafeDispatchIntroductionDialogue.Validate(rows,ev.id,sim.State);Assert(CafeReplayCatalog.FromSave(new CafeSaveEnvelope{state=sim.State,dispatch_introduction_conversations=rows},r,id=>id,id=>id,id=>id).Single().lines.Length==9,"Introduction replay script incomplete");Assert(row.progress.completed && row.progress.viewed_branches.Length==1,"Introduction viewing duplicated");CafeDispatchIntroductionDialogue.Validate(null,null,sim.State);
            row.progress.branch_id=choice=="accept"?"declined":"accepted";bool rejected=false;try{CafeDispatchIntroductionDialogue.Validate(rows,ev.id,sim.State);}catch(ArgumentException){rejected=true;}Assert(rejected,"Forged introduction result allowed");
        }
    }
    static void StudioShoppingOverlapChecks(CafeRules defaults)
    {
        var r=Decode<CafeRules>(Encode(defaults));r.store_events.probability=0;r.dispatch_unlocks=r.dispatch_unlocks.Where(u=>u.id!=CafeDispatchIntroductionDialogue.Destination && u.id!=CafeDispatchIntroductionDialogue.StudioDestination).ToArray();
        // Fixture supplies a second eligible cat; shipping rules are unchanged.
        r.cats.Single(c=>c.id=="cat-kohaku").trait_id="hospitality";
        var sim=new CafeSimulation(r);sim.Execute("dispatch_general",catId:"cat-mike",choice:CafeDispatchIntroductionDialogue.StudioDestination);sim.Execute("dispatch_general",catId:"cat-kohaku",choice:CafeDispatchIntroductionDialogue.Destination);sim.Execute("day_off");
        foreach(var e in sim.State.dispatches)sim.Execute("resolve_dispatch_choice",source:e.id,choice:"decline");sim.Execute("day_off");foreach(var e in sim.State.dispatches)sim.Execute("resolve_activity",source:e.id,choice:"receive");
        Assert(sim.State.dispatches.All(e=>e.introduction.status=="waiting"),"Both introductions not waiting");
        var rows=sim.State.dispatches.Select(e=>new CafeDispatchIntroductionConversation{event_id=e.id,progress=CafeDispatchIntroductionDialogue.Start(e.destination)}).ToArray();
        foreach(var row in rows){sim.Execute("resolve_dispatch_introduction",source:row.event_id,choice:"decline");CafeDispatchIntroductionDialogue.Sync(row,sim.State.dispatches.Single(e=>e.id==row.event_id));row.progress.node=4;CafeDispatchIntroductionDialogue.Finish(row.progress);}
        CafeDispatchIntroductionDialogue.Validate(rows,rows[0].event_id,sim.State);var save=new CafeSaveEnvelope{state=sim.State,dispatch_introduction_conversations=rows};string before=Encode(save);var entries=CafeReplayCatalog.FromSave(save,r,id=>id,id=>id,id=>id);
        Assert(entries.Length==2 && entries.Select(e=>e.Key).Distinct().Count()==2,"Studio/shopping replay collision");Assert(entries.Single(e=>e.conversation_id==CafeDispatchIntroductionDialogue.StudioId).lines.Any(l=>l.speaker=="撮影スタジオの担当者"),"Studio replay used shopping script");Assert(before==Encode(save),"Replay mutated save");
    }
    static void DispatchIntroductionOrderChecks(CafeRules defaults)
    {
        var r=Decode<CafeRules>(Encode(defaults));r.store_events.probability=0;r.dispatch_unlocks=r.dispatch_unlocks.Where(u=>u.id!=CafeDispatchIntroductionDialogue.Destination).ToArray();
        var sim=new CafeSimulation(r);sim.Execute("dispatch_general",catId:"cat-mike",choice:CafeDispatchIntroductionDialogue.Destination);sim.Execute("dispatch_general",catId:"cat-kohaku",choice:"neighborhood_visit");sim.Execute("day_off");string id=sim.State.dispatches.Single(e=>e.cat_id=="cat-mike").id,first=sim.State.dispatches.Single(e=>e.cat_id=="cat-kohaku").id;
        sim.Execute("resolve_dispatch_choice",source:id,choice:"decline");sim.Execute("resolve_activity",source:first,choice:"receive");sim.Execute("dispatch_general",catId:"cat-kohaku",choice:"neighborhood_visit");sim.Execute("day_off");sim.Execute("resolve_activity",source:id,choice:"receive");
        Assert(sim.DispatchIntroductionWaiting && sim.DispatchWaiting,"Introduction/return overlap absent");string before=Encode(sim.State);Rejected(()=>sim.Execute("resolve_dispatch_introduction",source:id,choice:"accept"));Rejected(()=>sim.Execute("resolve_dispatch_introduction",source:id,choice:"decline"));Assert(before==Encode(sim.State) && sim.DispatchIntroductionResponseReason(id).Contains("帰還"),"Return priority changed");
        sim.Execute("resolve_activity",source:sim.State.dispatches.Last().id,choice:"receive");Assert(sim.DispatchIntroductionResponseReason(id)=="","Return did not release introduction");sim.Execute("resolve_dispatch_introduction",source:id,choice:"decline");Assert(!sim.DispatchIntroductionWaiting,"Introduction not resolved after return");
    }
    static void LodgeConversationChecks(CafeRules defaults)
    {
        foreach(string branch in new[]{"search","wait"})
        {
            var r=Decode<CafeRules>(Encode(defaults));r.store_events.probability=0;r.intake.day=r.regular_introduction.day=9999;r.starting_funds=20000;r.dispatch_trouble.base_probability=1;r.dispatch_trouble.stress_probability=0;
            var sim=new CafeSimulation(r);sim.Execute("dispatch_general",catId:"cat-tama",choice:"mountain_lodge_visit");sim.Execute("day_off");
            var e=sim.State.dispatches.Single();var row=new CafeLodgeConversation{event_id=e.id,response=CafeLodgeDialogue.Start(false)};
            void Roundtrip(bool returning)
            {
                var p=row.Current;for(int n=0;n<=CafeLodgeDialogue.Lines(returning,p.branch_id,"").Length;n++)
                {p.node=n;var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,lodge_conversations=new[]{row},lodge_conversation_selected=e.id}));sim.Restore(save.state);e=sim.State.dispatches.Single();CafeLodgeDialogue.Validate(save.lodge_conversations,e.id,sim.State);Assert(save.lodge_conversations[0].Current.node==n,"Lodge position lost");}
            }
            Roundtrip(false);Assert(!CafeLodgeDialogue.Supports(new CafeDispatch{destination="neighborhood_visit"}),"Normal missing event supported");
            double funds=sim.State.funds;sim.State.funds=r.dispatch_trouble.search_cost;Assert(sim.DispatchTroubleReason(e.id,"search")!="" && sim.DispatchTroubleReason(e.id,"wait")=="","Lodge funds boundary blocks free wait");sim.State.funds=funds;
            sim.Execute("resolve_dispatch_trouble",source:e.id,choice:branch);CafeLodgeDialogue.Sync(row,e);Assert(row.Current.node==0 && row.Current.page_offset==0 && CafeLodgeDialogue.ResultPending(row),"Lodge answer sync lost");
            string before=Encode(sim.State);for(int i=0;i<8;i++)sim.Execute("resolve_dispatch_trouble",source:e.id,choice:branch);Assert(before==Encode(sim.State),"Lodge duplicated search cost");
            Assert(sim.State.funds==funds-(branch=="search"?r.dispatch_trouble.search_cost:0),"Lodge cost changed");Roundtrip(false);CafeLodgeDialogue.Finish(row);CafeLodgeDialogue.Finish(row);Assert(!CafeLodgeDialogue.ResultPending(row),"Lodge wait blocks schedule after reading");
            if(branch=="wait"){Assert(e.status=="missing","Wait returned early");Rejected(()=>sim.Execute("resolve_activity",source:e.id,choice:"receive"));for(int d=0;d<r.dispatch_trouble.missing_days;d++){sim.Execute("day_off");e=sim.State.dispatches.Single();Assert(e.status==(d==r.dispatch_trouble.missing_days-1?"waiting":"missing"),"Wait date changed");}}
            row.returning=CafeLodgeDialogue.Start(true);Roundtrip(true);sim.Execute("resolve_activity",source:e.id,choice:"receive");CafeLodgeDialogue.Sync(row,e);Roundtrip(true);CafeLodgeDialogue.Finish(row);CafeLodgeDialogue.Validate(new[]{row},e.id,sim.State);
            before=Encode(sim.State);for(int i=0;i<8;i++)sim.Execute("resolve_activity",source:e.id,choice:"receive");Assert(before==Encode(sim.State),"Lodge duplicate return mutated state");
            Assert(sim.DispatchReceivedReward(e)==0 && !sim.State.growth_operations.Any(o=>o.kind=="dispatch"),"Interrupted dispatch earned rewards");var cat=sim.State.cats.Single(c=>c.id==e.cat_id);Assert(cat.stress==r.dispatch_trouble.return_stress && cat.stamina==sim.MaxStamina(cat.id),"Lodge return health changed");
            var entries=CafeReplayCatalog.FromSave(new CafeSaveEnvelope{state=sim.State,lodge_conversations=new[]{row}},r,x=>x,x=>x,x=>x);Assert(entries.Length==2 && entries.Any(x=>x.branch_id==branch),"Lodge Extra missing branch");Assert(before==Encode(sim.State),"Lodge replay mutated state");CafeLodgeDialogue.Validate(null,null,sim.State);
            row.returning.page_offset=-1;bool bad=false;try{CafeLodgeDialogue.Validate(new[]{row},e.id,sim.State);}catch(ArgumentException){bad=true;}Assert(bad,"Invalid Lodge page accepted");
        }
    }
    static void EncounterConversationChecks(CafeRules defaults)
    {
        foreach(string destination in new[]{CafeEncounterDialogue.Destination,"out_of_town_visit","cat_photo_studio","cat_exercise_class","quiet_reading_salon"})
        foreach(bool first in new[]{true,false})
        foreach(bool high in new[]{true,false})
        {
            var rules=Decode<CafeRules>(Encode(defaults));rules.store_events.probability=0;
            rules.dispatch_unlocks=rules.dispatch_unlocks.Where(r=>r.id!=destination).ToArray();
            if(destination=="cat_exercise_class")rules.cats.Single(c=>c.id=="cat-sora").trait_id="hardy";
            var sim=new CafeSimulation(rules);sim.Execute("dispatch_general",catId:destination=="cat_exercise_class"?"cat-sora":destination=="quiet_reading_salon"?"cat-kohaku":destination=="out_of_town_visit"?"cat-tama":"cat-mike",choice:destination);sim.Execute("day_off");
            var ev=sim.State.dispatches.Single();var row=new CafeEncounterConversation{event_id=ev.id,progress=CafeEncounterDialogue.Start(ev)};var rows=new[]{row};
            string cid=row.progress.conversation_id,branch=first?CafeEncounterDialogue.FirstChoice(cid):CafeEncounterDialogue.SecondChoice(cid);
            var cat=sim.State.cats.Single(c=>c.id==ev.cat_id);cat.fatigue=high?98:2;cat.stress=high?99:2;
            var effect=sim.DispatchEncounterDefinition(destination,ev.started_day).choices.Single(c=>c.id==branch);
            double fatigue=cat.fatigue,stress=cat.stress;
            for(int i=0;i<=CafeEncounterDialogue.Lines("introduction","",cid).Length;i++)
            {row.progress.node=i;var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,encounter_conversations=rows}));sim.Restore(save.state);CafeEncounterDialogue.Validate(save.encounter_conversations,ev.id,sim.State);Assert(save.encounter_conversations[0].progress.node==i,"Encounter intro node lost");}
            double funds=sim.State.funds,baseReward=sim.DispatchReceivedReward(ev);sim.Execute("resolve_dispatch_choice",source:ev.id,choice:branch);CafeEncounterDialogue.Sync(row,sim.State.dispatches.Single());
            cat=sim.State.cats.Single(c=>c.id==ev.cat_id);Assert(cat.fatigue==Math.Max(0,Math.Min(rules.max_fatigue,fatigue+effect.fatigue)) && cat.stress==Math.Max(0,Math.Min(100,stress+effect.stress)),"Encounter clamp or effect differs");
            string state=Encode(sim.State);for(int j=0;j<5;j++)sim.Execute("resolve_dispatch_choice",source:ev.id,choice:branch);Assert(state==Encode(sim.State),"Encounter effects duplicated");
            Assert(sim.State.funds==funds && sim.DispatchReceivedReward(sim.State.dispatches.Single())==Math.Max(0,baseReward+effect.reward),"Encounter reward paid early or changed");
            for(int i=0;i<=CafeEncounterDialogue.Lines(branch,"",cid).Length;i++)
            {row.progress.node=i;var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,encounter_conversations=rows}));sim.Restore(save.state);CafeEncounterDialogue.Validate(save.encounter_conversations,ev.id,sim.State);Assert(save.encounter_conversations[0].progress.branch_id==branch,"Encounter result lost");}
            state=Encode(sim.State);CafeEncounterDialogue.Finish(row.progress);CafeEncounterDialogue.Finish(row.progress);CafeEncounterDialogue.Validate(rows,ev.id,sim.State);Assert(CafeReplayCatalog.FromSave(new CafeSaveEnvelope{state=sim.State,encounter_conversations=rows},rules,id=>id,id=>id,id=>id).Single().lines.Length==7,"Encounter replay script incomplete");Assert(row.progress.completed && row.progress.viewed_branches.Length==1,"Encounter viewed duplicate");
            if(cid==CafeEncounterDialogue.ExerciseId || cid==CafeEncounterDialogue.ReadingId) {
            // A second dispatch for the same cat must own a fresh introduction.
            var returned=sim.State.dispatches.Single();while(returned.remaining>0)sim.Execute("day_off");
            double receipt=sim.State.funds,amount=sim.DispatchReceivedReward(returned);sim.Execute("resolve_activity",source:returned.id,choice:"receive");
            Assert(sim.State.funds==receipt+amount,"Encounter return reward differs");
            double paid=sim.State.funds;try{sim.Execute("resolve_activity",source:returned.id,choice:"receive");}catch(ArgumentException){}Assert(sim.State.funds==paid,"Repeated return paid twice");
            cat=sim.State.cats.Single(c=>c.id==returned.cat_id);cat.fatigue=0;cat.stress=0;
            sim.Execute("dispatch_general",catId:returned.cat_id,choice:destination);sim.Execute("day_off");
            var next=sim.State.dispatches.Single(e=>e.id!=returned.id);var nextRow=new CafeEncounterConversation{event_id=next.id,progress=CafeEncounterDialogue.Start(next)};
            CafeEncounterDialogue.Validate(new[]{row,nextRow},next.id,sim.State);Assert(nextRow.progress.branch_id=="introduction" && nextRow.event_id!=row.event_id,"Same cat later dispatch mixed");
            state=Encode(sim.State);
            }
            Assert(state==Encode(sim.State),"Replay mutated game");CafeEncounterDialogue.Validate(null,null,sim.State);row.progress.node=99;bool rejected=false;try{CafeEncounterDialogue.Validate(rows,ev.id,sim.State);}catch(ArgumentException){rejected=true;}Assert(rejected,"Invalid encounter node accepted");
        }
    }
    static void EncounterOrderChecks(CafeRules defaults)
    {
        var r=Decode<CafeRules>(Encode(defaults));r.store_events.probability=0;r.cats.Single(c=>c.id=="cat-sora").trait_id="hardy";r.dispatch_unlocks=r.dispatch_unlocks.Where(u=>u.id!="cat_exercise_class" && u.id!="quiet_reading_salon").ToArray();
        var sim=new CafeSimulation(r);sim.Execute("dispatch_general",catId:"cat-sora",choice:"cat_exercise_class");sim.Execute("dispatch_general",catId:"cat-kohaku",choice:"quiet_reading_salon");sim.Execute("day_off");
        Assert(sim.State.dispatches.Count(e=>e.encounter?.status=="waiting")==2,"Overlap fixture absent");var ev=sim.State.dispatches.Single(e=>e.cat_id=="cat-sora");var other=sim.State.dispatches.Single(e=>e.cat_id=="cat-kohaku");
        var rows=new[]{new CafeEncounterConversation{event_id=ev.id,progress=CafeEncounterDialogue.Start(ev)},new CafeEncounterConversation{event_id=other.id,progress=CafeEncounterDialogue.Start(other)}};
        CafeEncounterDialogue.Validate(rows,other.id,sim.State);
        Assert(sim.DispatchChoiceReason(ev.id,"accept")=="","Other encounter changed existing answer order");sim.State.phase="open";Assert(sim.DispatchChoiceReason(ev.id,"accept")!="","Open phase allowed response");sim.State.phase="preparation";
        var cat=sim.State.cats.Single(c=>c.id==ev.cat_id);cat.fatigue=98;cat.stress=99;sim.Execute("resolve_dispatch_choice",source:ev.id,choice:"accept");Assert(cat.fatigue==r.max_fatigue && cat.stress==99,"Effect clamp changed");
        Assert(other.encounter.status=="waiting" && sim.DispatchChoiceWaiting,"First answer consumed another event");Rejected(()=>sim.Execute("day_off"));sim.Execute("resolve_dispatch_choice",source:other.id,choice:"decline");Assert(!sim.DispatchChoiceWaiting,"Other encounter no longer answerable");
        foreach(var row in rows)CafeEncounterDialogue.Sync(row,sim.State.dispatches.Single(e=>e.id==row.event_id));CafeEncounterDialogue.Validate(rows,other.id,sim.State);Assert(rows[0].progress.branch_id=="accept" && rows[1].progress.branch_id=="decline" && rows[0].progress.conversation_id!=rows[1].progress.conversation_id,"Multiple cases mixed");
    }
    static void DayOffProgressChecks(CafeRules defaults)
    {
        foreach(bool maximum in new[]{false,true})foreach(bool poor in new[]{false,true})
        {
            var rules=Decode<CafeRules>(Encode(defaults));rules.intake.day=1;rules.starting_funds=poor?2000:10000;
            int count=maximum?12:6;var cats=rules.cats.ToList();while(cats.Count<count){var c=Decode<CatDefinition>(Encode(cats[0]));c.id="qa-full-"+cats.Count;cats.Add(c);}rules.cats=cats.ToArray();
            var sim=new CafeSimulation(rules);var s=sim.State;
            if(maximum){s.housing.purchased_day=s.housing.upgraded_day=1;s.housing_expenses=rules.housing.cost+rules.housing_upgrade.cost;s.expenses=s.housing_expenses;s.funds-=s.housing_expenses;}
            if(poor){s.funds-=s.funds-rules.intake.cost;s.opening_funds=s.funds+s.expenses;rules.starting_funds=s.opening_funds;}
            if(sim.StoreEventWaiting)sim.Execute("resolve_store_event",choice:sim.CurrentStoreEvent.type=="trouble"?"patch":"decline");
            string before=Encode(s);Rejected(()=>sim.PreviewDayOff(v=>Decode<CafeSnapshot>(Encode(v))));Assert(before==Encode(s),"Blocked preview changed live state");
            Assert(sim.IntakeAcceptReason!="","Full intake accepted");sim.Execute("resolve_intake",choice:"decline");before=Encode(s);
            for(int i=0;i<8;i++)sim.Execute("resolve_intake",choice:"decline");Assert(before==Encode(s),"Repeated decline changed state");
            var prediction=sim.PreviewDayOff(v=>Decode<CafeSnapshot>(Encode(v)));Assert(before==Encode(s),"Day-off preview settled real day");sim.Execute("day_off");Assert(Encode(s)==Encode(prediction),"Preview differs from actual day off");Assert(s.day==2 && s.cats.Count==count,"Full roster requires admission or transfer to proceed");
        }
        var probe=new CafeSimulation(defaults);if(probe.StoreEventWaiting)probe.Execute("resolve_store_event",choice:probe.CurrentStoreEvent.type=="trouble"?"patch":"decline");
        probe.State.reservation.status="accepted";probe.State.reservation.visit_day=1;
        Rejected(()=>probe.PreviewDayOff(v=>Decode<CafeSnapshot>(Encode(v))));
        bool isolated=false;try{probe.PreviewDayOff(v=>v);}catch(ArgumentException){isolated=true;}Assert(isolated,"Preview accepted live state");
    }
    static void RegularConversationChecks(CafeRules defaults)
    {
        RegularIntroductionChecks.Run(defaults,Assert,Path.GetFullPath("Logs/StandaloneRegularConversation/core-rules"));
        foreach(string answer in new[]{"accept","decline"})
        {
            var rules=Decode<CafeRules>(Encode(defaults));rules.loyalty.threshold=25;rules.intake.day=2;
            var sim=new CafeSimulation(rules);sim.Execute("start_business");while(sim.State.phase!="closed")sim.Execute("advance_business");sim.Execute("next_day");
            if(sim.StoreEventWaiting)sim.Execute("resolve_store_event",choice:sim.CurrentStoreEvent.type=="trouble"?"patch":"decline");
            var intake=CafeIntakeConversation.Start();intake.node=2;var regular=CafeRegularConversation.Start();
            for(int node=0;node<=CafeRegularConversation.Lines("introduction","").Length;node++)
            {
                regular.node=node;var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,conversation=intake,regular_conversation=regular}));
                CafeIntakeConversation.Validate(save.conversation,"waiting");CafeRegularConversation.Validate(save.regular_conversation,"waiting");
                sim.Restore(save.state);regular=save.regular_conversation;Assert(regular.node==node && save.conversation.node==2,"Independent progress lost");
            }
            sim.Execute("resolve_regular_introduction",choice:answer);CafeRegularConversation.SyncAnswer(regular,sim.State.regular_introduction.status);
            for(int node=0;node<=CafeRegularConversation.Lines(regular.branch_id,"").Length;node++)
            {
                regular.node=node;var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,conversation=intake,regular_conversation=regular}));
                CafeRegularConversation.Validate(save.regular_conversation,save.state.regular_introduction.status);sim.Restore(save.state);regular=save.regular_conversation;
                string before=Encode(sim.State);for(int n=0;n<5;n++)sim.Execute("resolve_regular_introduction",choice:answer);Assert(before==Encode(sim.State),"Repeated regular answer mutated state");
            }
            CafeRegularConversation.Finish(regular);CafeRegularConversation.Finish(regular);CafeRegularConversation.Validate(regular,sim.State.regular_introduction.status);
            Assert(CafeReplayCatalog.FromSave(new CafeSaveEnvelope{state=sim.State,regular_conversation=regular},rules,id=>id,id=>"紹介者の名前",id=>id).Single().lines.Any(l=>l.speaker=="紹介者の名前"),"Regular replay speaker lost");
            Assert(regular.completed && regular.viewed_branches.Length==1 && !intake.completed,"Completion records mixed or duplicated");
            var old=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,conversation=intake}));Assert(old.regular_conversation==null,"Old save fabricated regular viewing");
            foreach(Action<CafeConversationProgress> forge in new Action<CafeConversationProgress>[]{p=>p.conversation_id=CafeIntakeConversation.Id,p=>p.node=999,p=>p.branch_id="forged",p=>p.viewed_branches=new[]{"accepted","accepted"},p=>p.completed=false})
            {
                var bad=Decode<CafeConversationProgress>(Encode(regular));forge(bad);bool rejected=false;try{CafeRegularConversation.Validate(bad,sim.State.regular_introduction.status);}catch(ArgumentException){rejected=true;}Assert(rejected,"Forged regular progress accepted");
            }
        }
        Assert(CafeRegularConversation.Lines("introduction","マロン","紹介者の名前").Any(l=>l.speaker=="紹介者の名前"),"Introducer speaker not used");
    }

    static void DispatchConversationChecks(CafeRules rules)
    {
        foreach(bool cancelFirst in new[]{false,true})
        {
            var sim=new CafeSimulation(rules);string cat="cat-tama",id="dispatch-1-"+cat;
            var row=new CafeDispatchConversation{event_id=id,cat_id=cat,day=1,departure=CafeDispatchDialogue.Start("departure")};
            void Roundtrip()
            {
                var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,dispatch_conversations=new[]{row},dispatch_conversation_selected=id}));
                CafeDispatchDialogue.Validate(save.dispatch_conversations,save.dispatch_conversation_selected,save.state);sim.Restore(save.state);row=save.dispatch_conversations[0];
            }
            string Capture(){var value=Decode<CafeSnapshot>(Encode(sim.State));value.instance_id="";value.revision=0;return Encode(value);}
            string before=Capture();for(int n=0;n<=CafeDispatchDialogue.Lines("departure","introduction","").Length;n++){row.departure.node=n;Roundtrip();Assert(Capture()==before,"Reading changed dispatch game state");}
            if(cancelFirst){row.departure.branch_id="cancelled";for(int n=0;n<=2;n++){row.departure.node=n;Roundtrip();}CafeDispatchDialogue.Finish(row);CafeDispatchDialogue.Finish(row);Roundtrip();Assert(row.departure.viewed_branches.Length==1,"Cancel viewing duplicated");row.departure.branch_id="introduction";row.departure.node=0;row.departure.completed=false;}
            double funds=sim.State.funds;sim.Execute("dispatch_general",catId:cat,choice:CafeDispatchDialogue.Destination);var ev=sim.State.dispatches[0];CafeDispatchDialogue.Sync(row,ev);
            Assert(row.departure.branch_id=="departed" && sim.State.funds==funds && sim.State.cats.Single(c=>c.id==cat).activity=="dispatched","Departure sync changed payment");
            for(int n=0;n<=3;n++){row.departure.node=n;Roundtrip();}CafeDispatchDialogue.Finish(row);Roundtrip();Assert(!CafeDispatchDialogue.ResultPending(row),"Finished departure still pending");
            row.stage="travelling";row.travelling=CafeDispatchDialogue.Start("travelling");for(int n=0;n<=3;n++){row.travelling.node=n;Roundtrip();}CafeDispatchDialogue.Finish(row);Roundtrip();
            if(sim.StoreEventWaiting)sim.Execute("resolve_store_event",choice:sim.CurrentStoreEvent.type=="trouble"?"patch":"decline");sim.Execute("day_off");ev=sim.State.dispatches[0];CafeDispatchDialogue.Sync(row,ev);Assert(row.stage=="return" && row.returning.branch_id=="introduction","Return phase missing");
            for(int n=0;n<=3;n++){row.returning.node=n;Roundtrip();}
            funds=sim.State.funds;sim.Execute("resolve_activity",source:id,choice:"receive");ev=sim.State.dispatches[0];CafeDispatchDialogue.Sync(row,ev);
            Assert(CafeDispatchDialogue.ResultPending(row) && row.returning.branch_id=="received","Received result missing");
            for(int n=0;n<=3;n++){row.returning.node=n;Roundtrip();string answered=Encode(sim.State);for(int j=0;j<5;j++)sim.Execute("resolve_activity",source:id,choice:"receive");Assert(answered==Encode(sim.State),"Return paid twice");}
            Assert(sim.State.funds==funds+ev.reward,"Return reward mismatch");CafeDispatchDialogue.Finish(row);CafeDispatchDialogue.Finish(row);Roundtrip();Assert(row.returning.viewed_branches.Length==1 && !CafeDispatchDialogue.ResultPending(row),"Receipt viewing duplicated");
            Assert(CafeReplayCatalog.FromSave(new CafeSaveEnvelope{state=sim.State,dispatch_conversations=new[]{row}},rules,c=>sim.Definition(c).name,c=>c,c=>c).Length==(cancelFirst?4:3),"Dispatch replay phases lost");
            foreach(Action<CafeDispatchConversation> forge in new Action<CafeDispatchConversation>[]{r=>r.event_id="wrong",r=>r.cat_id="wrong",r=>r.day++,r=>r.stage="unknown",r=>r.returning.node=99,r=>r.returning.branch_id="introduction",r=>r.returning.conversation_id=CafeIntakeConversation.Id,r=>r.returning.viewed_branches=new[]{"received","received"},r=>r.returning.completed=false})
            {
                var bad=Decode<CafeDispatchConversation>(Encode(row));forge(bad);bool rejected=false;try{CafeDispatchDialogue.Validate(new[]{bad},id,sim.State);}catch(ArgumentException){rejected=true;}Assert(rejected,"Forged dispatch conversation accepted");
            }
            CafeDispatchDialogue.Validate(null,null,sim.State);Assert(Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State})).dispatch_conversations==null,"Old save fabricated dispatch viewing");
        }
    }

    static void AdoptionDialogueChecks(CafeRules original)
    {
        foreach(string choice in new[]{"accept","decline"})
        {
            var rules=Decode<CafeRules>(Encode(original));rules.starting_funds=20000;rules.store_events.probability=0;rules.growth.threshold=10000;rules.exchange.ticks=1;rules.exchange.affinity_enthusiastic=rules.exchange.affinity_favorable=80;rules.opening_ticks=2;rules.arrival_ticks=new[]{0,0};rules.patterns=Enumerable.Range(0,7).Select(i=>new WeekPattern{days=Enumerable.Range(0,7).ToArray()}).ToArray();rules.max_probability=rules.stress_per_service_tick=rules.fatigue_per_service_tick=0;
            var sim=new CafeSimulation(rules);sim.Execute("configure_adoption",choice:"on");sim.Execute("start_business");sim.Execute("advance_business");var e=sim.State.adoption.events[0];var row=new CafeAdoptionConversation{event_id=e.id,progress=CafeAdoptionDialogue.Start()};
            void Roundtrip(){var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,adoption_conversations=new[]{row},adoption_conversation_selected=e.id}));sim.Restore(save.state);row=save.adoption_conversations[0];e=sim.State.adoption.events.Single(x=>x.id==row.event_id);CafeAdoptionDialogue.Validate(new[]{row},e.id,sim.State);Assert(row.progress!=null,"Adoption metadata lost");}
            for(int i=0;i<=4;i++){row.progress.node=i;Roundtrip();}
            double funds=sim.State.funds;sim.Execute("resolve_adoption",source:e.id,choice:choice);CafeAdoptionDialogue.Sync(row,e);Assert(row.progress.branch_id==choice && CafeAdoptionDialogue.ResultPending(row),"Adoption branch missing");
            for(int i=0;i<=3;i++){row.progress.node=i;Roundtrip();Assert(CafeReplayCatalog.FromSave(new CafeSaveEnvelope{state=sim.State,adoption_conversations=new[]{row}},rules,x=>sim.Definition(x).name,x=>x,x=>x).Length==0,"Unread adoption unlocked");}
            CafeAdoptionDialogue.Finish(row);CafeAdoptionDialogue.Finish(row);Roundtrip();var entries=CafeReplayCatalog.FromSave(new CafeSaveEnvelope{state=sim.State,adoption_conversations=new[]{row}},rules,x=>sim.Definition(x).name,x=>x,x=>x);Assert(entries.Length==1 && entries[0].branch_id==choice,"Adoption Extra missing");Assert(entries[0].cat_name==sim.Definition(e.cat_id).name && entries[0].lines.Any(l=>l.text.Contains(e.customer_id)),"Adoption identities mixed");Assert(sim.State.funds==funds,"Adoption reward changed");
            CafeAdoptionDialogue.Validate(null,null,sim.State);Assert(CafeReplayCatalog.FromSave(new CafeSaveEnvelope{state=sim.State},rules,x=>x,x=>x,x=>x).Length==0,"Legacy adoption fabricated reading");
            foreach(Action<CafeAdoptionConversation> forge in new Action<CafeAdoptionConversation>[]{r=>r.event_id="wrong",r=>r.progress.branch_id="introduction",r=>r.progress.node=99,r=>r.progress.viewed_branches=new[]{"accept","decline"},r=>r.progress.completed=false}){var bad=Decode<CafeAdoptionConversation>(Encode(row));forge(bad);bool rejected=false;try{CafeAdoptionDialogue.Validate(new[]{bad},bad.event_id,sim.State);}catch(ArgumentException){rejected=true;}Assert(rejected,"Forged adoption metadata accepted");}
        }
    }

}
