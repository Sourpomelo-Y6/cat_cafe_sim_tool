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
        IntakeChecks.Run(rules,Assert,Path.GetFullPath("Logs/StandaloneConversation/core-saves"));
        Console.WriteLine("PASS conversation: "+assertions+" assertions; both branches, every node save roundtrip, old saves, funds, housing, order, repeated answers.");
    }
}
