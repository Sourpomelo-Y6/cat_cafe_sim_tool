using CatCafePreview.Standalone;
static partial class Program
{
    static void Rejected(Action action){bool rejected=false;try{action();}catch(InvalidOperationException){rejected=true;}Assert(rejected,"Invalid choice accepted");}
    static void ConversationChecks(string rulePath)
    {
        var rules=Decode<CafeRules>(File.ReadAllText(rulePath));
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
        EncounterConversationChecks(rules);
        EncounterOrderChecks(rules);
        DispatchConversationChecks(rules);
        RegularConversationChecks(rules);
        DayOffProgressChecks(rules);
        IntakeChecks.Run(rules,Assert,Path.GetFullPath("Logs/StandaloneConversation/core-saves"));
        Console.WriteLine("PASS conversation: "+assertions+" assertions; both branches, every node save roundtrip, old saves, funds, housing, order, repeated answers.");
    }

    static void EncounterConversationChecks(CafeRules defaults)
    {
        foreach(string branch in new[]{"accept","decline"})
        {
            var rules=Decode<CafeRules>(Encode(defaults));rules.store_events.probability=0;
            rules.dispatch_unlocks=rules.dispatch_unlocks.Where(r=>r.id!=CafeEncounterDialogue.Destination).ToArray();
            var sim=new CafeSimulation(rules);sim.Execute("dispatch_general",catId:"cat-mike",choice:CafeEncounterDialogue.Destination);sim.Execute("day_off");
            var ev=sim.State.dispatches.Single();var row=new CafeEncounterConversation{event_id=ev.id,progress=CafeEncounterDialogue.Start()};var rows=new[]{row};
            for(int i=0;i<=CafeEncounterDialogue.Lines("introduction","").Length;i++)
            {row.progress.node=i;var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,encounter_conversations=rows}));sim.Restore(save.state);CafeEncounterDialogue.Validate(save.encounter_conversations,ev.id,sim.State);Assert(save.encounter_conversations[0].progress.node==i,"Encounter intro node lost");}
            double funds=sim.State.funds,baseReward=sim.DispatchReceivedReward(ev);sim.Execute("resolve_dispatch_choice",source:ev.id,choice:branch);CafeEncounterDialogue.Sync(row,sim.State.dispatches.Single());
            string state=Encode(sim.State);for(int j=0;j<5;j++)sim.Execute("resolve_dispatch_choice",source:ev.id,choice:branch);Assert(state==Encode(sim.State),"Encounter effects duplicated");
            Assert(sim.State.funds==funds && sim.DispatchReceivedReward(sim.State.dispatches.Single())==baseReward+(branch=="accept"?100:0),"Encounter reward paid early or changed");
            for(int i=0;i<=CafeEncounterDialogue.Lines(branch,"").Length;i++)
            {row.progress.node=i;var save=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State,encounter_conversations=rows}));sim.Restore(save.state);CafeEncounterDialogue.Validate(save.encounter_conversations,ev.id,sim.State);Assert(save.encounter_conversations[0].progress.branch_id==branch,"Encounter result lost");}
            CafeEncounterDialogue.Finish(row.progress);CafeEncounterDialogue.Finish(row.progress);CafeEncounterDialogue.Validate(rows,ev.id,sim.State);Assert(row.progress.completed && row.progress.viewed_branches.Length==1,"Encounter viewed duplicate");
            CafeEncounterDialogue.Validate(null,null,sim.State);row.progress.node=99;bool rejected=false;try{CafeEncounterDialogue.Validate(rows,ev.id,sim.State);}catch(ArgumentException){rejected=true;}Assert(rejected,"Invalid encounter node accepted");
        }
    }
    static void EncounterOrderChecks(CafeRules defaults)
    {
        var r=Decode<CafeRules>(Encode(defaults));r.store_events.probability=0;r.dispatch_unlocks=r.dispatch_unlocks.Where(u=>u.id!=CafeEncounterDialogue.Destination && u.id!="out_of_town_visit").ToArray();
        var sim=new CafeSimulation(r);sim.Execute("dispatch_general",catId:"cat-mike",choice:CafeEncounterDialogue.Destination);sim.Execute("dispatch_general",catId:"cat-tama",choice:"out_of_town_visit");sim.Execute("day_off");
        Assert(sim.State.dispatches.Count(e=>e.encounter?.status=="waiting")==2,"Overlap fixture absent");var ev=sim.State.dispatches.Single(e=>e.cat_id=="cat-mike");var other=sim.State.dispatches.Single(e=>e.cat_id=="cat-tama");
        Assert(sim.DispatchChoiceReason(ev.id,"accept")=="","Other encounter changed existing answer order");sim.State.phase="open";Assert(sim.DispatchChoiceReason(ev.id,"accept")!="","Open phase allowed response");sim.State.phase="preparation";
        var cat=sim.State.cats.Single(c=>c.id==ev.cat_id);cat.fatigue=98;cat.stress=99;sim.Execute("resolve_dispatch_choice",source:ev.id,choice:"accept");Assert(cat.fatigue==r.max_fatigue && cat.stress==100,"Effect clamp changed");
        Assert(other.encounter.status=="waiting" && sim.DispatchChoiceWaiting,"First answer consumed another event");Rejected(()=>sim.Execute("day_off"));sim.Execute("resolve_dispatch_choice",source:other.id,choice:"rest");Assert(!sim.DispatchChoiceWaiting,"Other encounter no longer answerable");
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
            foreach(Action<CafeDispatchConversation> forge in new Action<CafeDispatchConversation>[]{r=>r.event_id="wrong",r=>r.cat_id="wrong",r=>r.day++,r=>r.stage="unknown",r=>r.returning.node=99,r=>r.returning.branch_id="introduction",r=>r.returning.conversation_id=CafeIntakeConversation.Id,r=>r.returning.viewed_branches=new[]{"received","received"},r=>r.returning.completed=false})
            {
                var bad=Decode<CafeDispatchConversation>(Encode(row));forge(bad);bool rejected=false;try{CafeDispatchDialogue.Validate(new[]{bad},id,sim.State);}catch(ArgumentException){rejected=true;}Assert(rejected,"Forged dispatch conversation accepted");
            }
            CafeDispatchDialogue.Validate(null,null,sim.State);Assert(Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=sim.State})).dispatch_conversations==null,"Old save fabricated dispatch viewing");
        }
    }

}
