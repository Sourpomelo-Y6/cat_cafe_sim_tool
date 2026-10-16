using System.Text.Json;
using CatCafePreview.Standalone;

static partial class Program
{
    static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
    static string Encode<T>(T value) => JsonSerializer.Serialize(value, Options);
    static T Decode<T>(string text) => JsonSerializer.Deserialize<T>(text, Options);
    static int assertions;
    static void Assert(bool valid, string message) { assertions++; if (!valid) throw new Exception(message); }
    static void Equal(JsonElement expected, JsonElement actual, string path)
    {
        if (expected.ValueKind == JsonValueKind.Object)
            foreach (var p in expected.EnumerateObject()) Equal(p.Value, actual.GetProperty(p.Name), path + "." + p.Name);
        else if (expected.ValueKind == JsonValueKind.Array)
        {
            Assert(expected.GetArrayLength() == actual.GetArrayLength(), path + " length");
            for (int i = 0; i < expected.GetArrayLength(); i++) Equal(expected[i], actual[i], path + "[" + i + "]");
        }
        else if (expected.ValueKind == JsonValueKind.Number) Assert(Math.Abs(expected.GetDouble() - actual.GetDouble()) < 1e-8, path + ": " + expected + " != " + actual);
        else Assert(expected.ToString() == actual.ToString(), path + ": " + expected + " != " + actual);
    }
    static void DispatchCases(string path,ref int scenarios,ref int ticks)
    {
            using var cases=JsonDocument.Parse(File.ReadAllText(path));
            if(cases.RootElement.TryGetProperty("patron_matrix",out var patronMatrix))foreach(var row in patronMatrix.EnumerateArray())
            {
                var rules=Decode<CafeRules>(cases.RootElement.GetProperty("patron_matrix_rules").GetRawText());rules.cats.Single(c=>c.id=="cat-sora").features=Decode<string[]>(row.GetProperty("features").GetRawText());var probe=new CafeSimulation(rules,objective:"patron");probe.State.growth.Single(g=>g.cat_id=="cat-sora").specialization=row.GetProperty("specialization").GetString();
                for(int i=0;i<row.GetProperty("index").GetInt32();i++)probe.State.dispatches.Add(new CafeDispatch {destination="patron_moody_visit",status="resolved"});
                using var matches=JsonDocument.Parse(Encode(rules.patron.members.Select(m=>probe.PatronTerms(probe.State,"cat-sora",m.id)).ToArray()));Equal(row.GetProperty("matches"),matches.RootElement,"patron feature/specialization matrix");
                Assert(probe.DispatchReward("cat-sora","patron_visit")==row.GetProperty("reward").GetDouble(),"Patron reward matrix differs");Assert(probe.PatronGain("cat-sora","patron_visit")==row.GetProperty("gain").GetDouble(),"Patron gain matrix differs");
            }
            if(cases.RootElement.TryGetProperty("probabilities",out var probabilities))
            {
                var probe=new CafeSimulation(Decode<CafeRules>(cases.RootElement.GetProperty("probability_rules").GetRawText()));
                foreach(var row in probabilities.EnumerateArray()){probe.State.cats.Single(c=>c.id=="cat-tama").stress=row.GetProperty("stress").GetDouble();Assert(probe.DispatchTroubleProbability("cat-tama")==row.GetProperty("value").GetDouble(),"Python stress probability differs");}
            }
            if(cases.RootElement.TryGetProperty("draws",out var troubleDraws))foreach(var row in troubleDraws.EnumerateArray())Assert(CafeSimulation.DispatchTroubleDraw(row.GetProperty("seed").GetInt32(),row.GetProperty("id").GetString())==row.GetProperty("value").GetDouble(),"Python dispatch trouble draw differs");
            if(cases.RootElement.TryGetProperty("unlock_boundaries",out var boundaries))foreach(var row in boundaries.EnumerateArray())
            {
                var probe=new CafeSimulation(Decode<CafeRules>(cases.RootElement.GetProperty("unlock_rules").GetRawText()));probe.State.day=3;probe.State.popularity=row.GetProperty("popularity").GetDouble();
                for(int i=0;i<row.GetProperty("returns").GetInt32();i++)probe.State.dispatches.Add(new CafeDispatch {status="resolved"});
                for(int i=0;i<row.GetProperty("stages").GetInt32();i++)probe.State.goal.Add(new CafeGoalStage {started_day=i+1,resolved_day=i+1,status="cleared"});
                var update=typeof(CafeSimulation).GetMethod("UpdateDispatchUnlocks",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);update.Invoke(probe,null);
                using(var current=JsonDocument.Parse(Encode(probe.State.dispatch_unlocks)))Equal(row.GetProperty("expected"),current.RootElement,"dispatch unlock threshold");
                probe.State.popularity=1;update.Invoke(probe,null);using(var current=JsonDocument.Parse(Encode(probe.State.dispatch_unlocks)))Equal(row.GetProperty("after_drop"),current.RootElement,"persistent unlock");
            }
            foreach(var fixture in cases.RootElement.GetProperty("cases").EnumerateArray())
            {
                var dispatch=new CafeSimulation(Decode<CafeRules>(fixture.GetProperty("rules").GetRawText()),seed:fixture.TryGetProperty("seed",out var dispatchSeed)?dispatchSeed.GetInt32():0,objective:fixture.GetProperty("objective").GetString());scenarios++;
                foreach(var op in fixture.GetProperty("operations").EnumerateArray())
                {
                    string beforeSeats=Encode(dispatch.State);bool rejected=false;string error=null;
                    if(op.TryGetProperty("waiting_boundaries",out var waitingBounds))
                    {
                        double funds=dispatch.State.funds;
                        foreach(var row in waitingBounds.EnumerateArray()){dispatch.State.funds=row.GetProperty("funds").GetDouble();Assert((dispatch.WaitingAreaReason(op.GetProperty("kind").GetString())=="")==row.GetProperty("can_execute").GetBoolean(),"Python waiting area funds boundary differs");}
                        dispatch.State.funds=funds;
                    }
                    if(op.TryGetProperty("rest_boundaries",out var restBounds))
                    {
                        double funds=dispatch.State.funds;
                        foreach(var row in restBounds.EnumerateArray()){dispatch.State.funds=row.GetProperty("funds").GetDouble();Assert((dispatch.RestSpaceReason(op.GetProperty("kind").GetString())=="")==row.GetProperty("can_execute").GetBoolean(),"Python rest space funding boundary differs");}
                        dispatch.State.funds=funds;
                    }
                    try{dispatch.Execute(op.GetProperty("kind").GetString(),working:Decode<string[]>(op.GetProperty("working").GetRawText()),choice:op.GetProperty("choice").GetString(),source:op.GetProperty("source").GetString(),catId:op.GetProperty("cat_id").GetString(),seatId:op.TryGetProperty("seat_id",out var seatId)?seatId.GetString():null,customerId:op.TryGetProperty("customer_id",out var customerId)?customerId.GetString():null);}catch(InvalidOperationException ex){rejected=true;error=ex.Message;}catch(ArgumentException ex){rejected=true;error=ex.Message;}
                    Assert(rejected==op.GetProperty("rejected").GetBoolean(),"Python dispatch rejection mismatch ("+fixture.GetProperty("variant")+"): "+op.GetProperty("kind")+" day "+dispatch.State.day+" / "+error);
                    if(rejected)Assert(beforeSeats==Encode(dispatch.State),"Rejected dispatch operation changed state");
                    if(op.TryGetProperty("admission_boundaries",out var admissions))
                    {
                        double funds=dispatch.State.funds;
                        foreach(var row in admissions.EnumerateArray()){dispatch.State.funds=row.GetProperty("funds").GetDouble();Assert((dispatch.DispatchIntroductionAcceptReason(op.GetProperty("source").GetString())=="")==row.GetProperty("can_accept").GetBoolean(),"Python dispatch admission funds boundary differs");}
                        dispatch.State.funds=funds;
                    }
                    if(op.TryGetProperty("recruitment_candidates",out var recruits)){using var candidates=JsonDocument.Parse(Encode(dispatch.RecruitmentCandidates(dispatch.State)));Equal(recruits,candidates.RootElement,"recruitment candidates");}
                    if(op.TryGetProperty("assignment_projection",out var assignment))
                    {
                        Assert(dispatch.AssignmentWaiting==assignment.GetProperty("waiting").GetBoolean() && (dispatch.AssignmentReason()=="")==assignment.GetProperty("can_assign").GetBoolean(),"Python assignment waiting/availability differs");
                        using var cats=JsonDocument.Parse(Encode(dispatch.AssignmentCats.Select(c=>c.id).ToArray()));Equal(assignment.GetProperty("available_cats"),cats.RootElement,"available assignment cats");
                        using var seats=JsonDocument.Parse(Encode(dispatch.AssignmentFreeSeats.Select(i=>"seat-"+(i+1)).ToArray()));Equal(assignment.GetProperty("free_seats"),seats.RootElement,"free assignment seats");
                    }
                    if(op.TryGetProperty("waiting_limits",out var waitingLimits))
                    {
                        Assert(dispatch.QueueCapacity==waitingLimits.GetProperty("capacity").GetInt32() && dispatch.MaxWaitTicks==waitingLimits.GetProperty("wait").GetInt32(),"Python waiting capacity/deadline differs");
                        Assert(dispatch.WaitingAreaDailyCost(dispatch.State,dispatch.State.day)==waitingLimits.GetProperty("daily").GetDouble() && Math.Abs(dispatch.OperatingCostEstimate-waitingLimits.GetProperty("operating").GetDouble())<1e-8,"Python facility/operating estimate differs");
                    }
                    using var current=JsonDocument.Parse(Encode(dispatch.State));Equal(op.GetProperty("expected"),current.RootElement,"dispatch boundary ("+fixture.GetProperty("variant").GetString()+" day "+dispatch.State.day+" tick "+dispatch.State.tick+")");dispatch.Restore(Decode<CafeSnapshot>(Encode(dispatch.State)));ticks++;
                }
                if(dispatch.State.adoption!=null && dispatch.State.adoption.events.Count>0)
                {
                    string before=Encode(dispatch.State);
                    for(int variant=0;variant<8;variant++)
                    {
                        var bad=Decode<CafeSnapshot>(before);var a=bad.adoption;var e=a.events[0];
                        if(variant==0)e.threshold++;else if(variant==1)e.source="invalid";else if(variant==2)e.tick++;else if(variant==3)e.guest_affinity--;else if(variant==4)e.player_affinity++;else if(variant==5)a.events.Add(e);else if(variant==6)e.resolved_day++;else a.events=null;
                        bool rejected=false;try{dispatch.Restore(bad);}catch(ArgumentException){rejected=true;}
                        Assert(rejected && before==Encode(dispatch.State),"Corrupt adoption save accepted or changed state");
                    }
                }
                if(fixture.GetProperty("variant").GetString().StartsWith("waiting-"))
                {
                    string before=Encode(dispatch.State);
                    for(int mutation=0;mutation<9;mutation++)
                    {
                        if(mutation>=7 && dispatch.State.history.Count==0)continue;
                        var bad=Decode<CafeSnapshot>(before);var r=bad.waiting_area;
                        if(mutation==0)bad.waiting_area=null;else if(mutation==1)r.purchased_day=-1;else if(mutation==2)r.purchased_day=bad.day+1;else if(mutation==3)r.purchased_day=1;else if(mutation==4)r.upgraded_day=1;else if(mutation==5)bad.waiting_area_expenses++;else if(mutation==6)bad.waiting_area_cost++;else if(mutation==7)bad.history[0].waiting_area_cost++;else bad.history[0].waiting_area_expenses++;
                        bool rejected=false;try{dispatch.Restore(bad);}catch(ArgumentException){rejected=true;}
                        Assert(rejected && before==Encode(dispatch.State),"Corrupt waiting area save accepted or changed state");
                    }
                }
                if(fixture.GetProperty("variant").GetString().StartsWith("rest-"))
                {
                    string before=Encode(dispatch.State);
                    for(int mutation=0;mutation<8;mutation++)
                    {
                        var bad=Decode<CafeSnapshot>(before);var r=bad.rest_space;
                        if(mutation==0)bad.rest_space=null;else if(mutation==1)r.purchased_day=-1;else if(mutation==2)r.purchased_day=bad.day+1;else if(mutation==3)r.upgraded_day=1;else if(mutation==4)r.soundproofed_day=1;else if(mutation==5)bad.rest_space_expenses++;else if(mutation==6)bad.history[0].rest_space_expenses++;else r.purchased_day=0;
                        if(mutation==7 && dispatch.State.rest_space.purchased_day==0)continue;
                        bool rejected=false;try{dispatch.Restore(bad);}catch(ArgumentException){rejected=true;}
                        Assert(rejected && before==Encode(dispatch.State),"Corrupt rest space save accepted or changed state");
                    }
                }
                if(dispatch.State.recruitment!=null && fixture.GetProperty("operations").EnumerateArray().Any(o=>o.GetProperty("expected").TryGetProperty("recruitment",out var recruits)))
                {
                    string before=Encode(dispatch.State);
                    for(int variant=0;variant<8;variant++)
                    {
                        var bad=Decode<CafeSnapshot>(before);var r=bad.recruitment;
                        if(variant==0)r.id="invalid";else if(variant==1)r.days[0]=0;else if(variant==2)r.days[r.days.Count-1]=bad.day+1;else if(variant==3)r.accepted.Add(new CafeShopPurchase {cat_id="unknown",day=bad.day});else if(variant==4)r.days.Add(r.days.Last()+1);else if(variant==5)r.accepted.Add(null);else if(variant==6)r.days=null;else r.accepted=null;
                        bool rejected=false;try{dispatch.Restore(bad);}catch(ArgumentException){rejected=true;}
                        Assert(rejected && before==Encode(dispatch.State),"Corrupt recruitment save accepted or changed state");
                    }
                }
                if(fixture.GetProperty("operations").EnumerateArray().Any(o=>o.GetProperty("expected").TryGetProperty("shop_purchases",out var purchases)))
                {
                    string before=Encode(dispatch.State);
                    for(int variant=0;variant<8;variant++)
                    {
                        var bad=Decode<CafeSnapshot>(before);
                        if(variant==0)bad.shop_purchases=null;
                        else if(variant==1)bad.shop_purchases.Add(null);
                        else if(variant==2)bad.shop_purchases.Add(new CafeShopPurchase {cat_id="unknown",day=bad.day});
                        else if(variant==3)bad.shop_purchases.Add(new CafeShopPurchase {cat_id="shop-1",day=bad.day+1});
                        else if(variant==4)bad.pet_shop_expenses++;
                        else if(variant==5)bad.shop_purchases.Add(new CafeShopPurchase {cat_id="shop-1",day=0});
                        else if(variant==6 && bad.shop_purchases.Count>0)bad.shop_purchases.Add(new CafeShopPurchase {cat_id=bad.shop_purchases[0].cat_id,day=bad.shop_purchases[0].day});
                        else if(variant==7 && bad.shop_purchases.Count>0)bad.shop_purchases.Clear();
                        else bad.shop_purchases.Add(new CafeShopPurchase {cat_id="shop-2",day=bad.day});
                        bool rejected=false;try{dispatch.Restore(bad);}catch(ArgumentException){rejected=true;}
                        Assert(rejected && before==Encode(dispatch.State),"Corrupt shop save accepted or changed state");
                    }
                }
                if(fixture.GetProperty("operations").EnumerateArray().Any(o=>o.GetProperty("expected").TryGetProperty("visiting_cat",out var visiting)))
                {
                    string before=Encode(dispatch.State);
                    for(int variant=0;variant<8;variant++)
                    {
                        var bad=Decode<CafeSnapshot>(before);var v=bad.visiting_cat;
                        if(variant==0)v.status="invalid";
                        else if(variant==1)v.presented_day++;
                        else if(variant==2)v.accepted_day=bad.day+1;
                        else if(variant==3)v.actions=null;
                        else if(variant==4)v.actions.Add(new CafeVisitingAction {day=bad.day+1,choice="interact"});
                        else if(variant==5)v.actions.Add(new CafeVisitingAction {day=bad.day,choice="accept"});
                        else if(variant==6)bad.visiting_cat=null;
                        else v.status=v.status=="untriggered"?"ready":"untriggered";
                        bool rejected=false;try{dispatch.Restore(bad);}catch(ArgumentException){rejected=true;}
                        Assert(rejected && before==Encode(dispatch.State),"Corrupt visiting save accepted or changed state");
                    }
                }
            }
    }
    static void Main(string[] args)
    {
        if(args.Length>0 && args[0]=="--all-modes"){AllModeNarrativeChecks(args[1],args[2],args[3]);return;}
        if(args.Length>0 && args[0]=="--validate-narratives"){ValidateNarrativeCatalog(args[1]);return;}
        if(args.Length>0 && args[0]=="--subjects"){SubjectCatalogChecks(args[1],args[2],args[3]);return;}
        if(args.Length>0 && args[0]=="--catalog"){CatalogChecks(args[1],args[2],args[3]);return;}
        if(args.Length>0 && args[0]=="--narrative-resume"){NarrativeResume(args[1],args[2]);return;}
        if(args.Length>0 && args[0]=="--narratives"){NarrativeChecks(args[1],args[2]);return;}
        if(args.Length>0 && args[0]=="--stamina-resume"){StaminaResume(args[1]);return;}
        if(args.Length>0 && args[0]=="--stamina-balance"){StaminaBalance(args[1],args[2]);return;}
        if(args.Length>0 && args[0]=="--stamina"){StaminaChecks(args[1],args[2]);return;}
        if(args.Length>0 && args[0]=="--player-turns"){PlayerTurnsChecks(args[1],args[2]);return;}
        if(args.Length>0 && args[0]=="--player-presentation")
        {
            using var cases=JsonDocument.Parse(File.ReadAllText(args[1]));int count=0;
            foreach(var fixture in cases.RootElement.GetProperty("cases").EnumerateArray())
            {
                var sim=new CafeSimulation(Decode<CafeRules>(fixture.GetProperty("rules").GetRawText()),seed:fixture.GetProperty("seed").GetInt32(),objective:fixture.GetProperty("objective").GetString());
                foreach(var op in fixture.GetProperty("operations").EnumerateArray())
                {
                    try { sim.Execute(op.GetProperty("kind").GetString(),catId:op.GetProperty("cat_id").GetString(),choice:op.GetProperty("choice").GetString(),source:op.GetProperty("source").GetString(),working:op.GetProperty("working").ValueKind==JsonValueKind.Null?null:op.GetProperty("working").EnumerateArray().Select(v=>v.GetString()).ToArray()); Assert(!op.GetProperty("rejected").GetBoolean(),"Python rejected operation was accepted"); } catch(InvalidOperationException) { Assert(op.GetProperty("rejected").GetBoolean(),"Unexpected rejected operation"); } catch(ArgumentException) { Assert(op.GetProperty("rejected").GetBoolean(),"Unexpected invalid operation"); }
                    using(var actual=JsonDocument.Parse(Encode(sim.State)))Equal(op.GetProperty("expected"),actual.RootElement,"player reactions");
                    sim.Restore(Decode<CafeSnapshot>(Encode(sim.State)));
                    var expected=op.GetProperty("presentation");if(expected.ValueKind==JsonValueKind.Null)continue;
                    var session=sim.State.player_sessions.Last();string presentationBefore=Encode(sim.State);
                    Assert(CafeSimulation.PlayerActions.Contains("switch") && CafeSimulation.PlayerActions.Length==7,"Player switch button missing");
                    var switchChoice=expected.GetProperty("actions").EnumerateArray().Single(a=>a.GetProperty("choice").GetString()=="switch");
                    Assert((sim.PlayerChoiceReason("switch")=="" && session.exchange.end_reason==null)==switchChoice.GetProperty("can_select").GetBoolean(),"Python switch availability differs");
                    Assert(sim.PlayerHistory(session)==expected.GetProperty("history").GetString(),"Python reaction history differs: "+fixture.GetProperty("variant")+" / "+op.GetProperty("kind"));
                    Assert(sim.PlayerEventImageKey(session)==expected.GetProperty("event_image_key").GetString(),"Python special image differs");
                    Assert(CafeSimulation.PlayerTypeName(session.exchange.mode)==expected.GetProperty("mode").GetString(),"Python mode label differs");
                    if(session.exchange.end_reason!=null)Assert(CafeSimulation.PlayerEndName(session.exchange.end_reason)==expected.GetProperty("reason").GetString(),"Python end label differs");
                    Assert(presentationBefore==Encode(sim.State),"Presentation mutated save");count++;
                }
            }
            Console.WriteLine("PASS: "+count+" Python player presentation comparisons, "+assertions+" assertions.");return;
        }
        if(args.Length>0 && args[0]=="--daily"){DailyCases(args);return;}
        if(args.Length>0 && args[0]=="--details"){DetailsCases(args);return;}
        if(args[0]=="--legacy-template" || args[0]=="--finalize-import"){LegacyTool(args);return;}
        if(args[0]=="--legacy-only"){LegacyCases(args[1]);return;}
        if(args[0]=="--verify-old-replays")
        {
            var replayRules=Decode<CafeRules>(File.ReadAllText(args[1]));
            foreach(string file in args.Skip(2)){var document=Decode<CafeReplayDocument>(File.ReadAllText(file));var journal=new CafeReplayJournal(replayRules,new CafeSimulation(replayRules).State,ReplayEncode,ReplayDecode);journal.Verify(document);}
            Console.WriteLine("PASS: "+(args.Length-2)+" existing Unity replay files.");return;
        }
        int ticks = 0, scenarios = 0;
        if(args[0]=="--replay-only"){ReplayCases(args.Skip(1).ToArray());return;}
        if(args[0]=="--autoplay-only"){AutoplayCases(args[1]);return;}
        if(args[0]=="--dispatch-only"){DispatchCases(args[1],ref scenarios,ref ticks);Console.WriteLine($"PASS: {scenarios} dispatch scenarios, {ticks} boundaries, {assertions} assertions.");return;}
        var rules = Decode<CafeRules>(File.ReadAllText(args[0]));
        using(var reference=JsonDocument.Parse(File.ReadAllText(args[1])))
        if(reference.RootElement.TryGetProperty("longhair_draws",out var draws))foreach(var row in draws.EnumerateArray())
        {
            int seed=row.GetProperty("seed").GetInt32(),day=row.GetProperty("day").GetInt32();double expected=row.GetProperty("value").GetDouble();
            Assert(CafeSimulation.LonghairDraw(seed,day)==expected,"Longhair SHA256 draw differs from Python");
            if(day<=2)continue;
            var config=Decode<CafeRules>(Encode(rules));config.longhair_customer.probability=expected;var probe=new CafeSimulation(config,seed,"popularity");
            probe.State.goal[0].status="cleared";probe.State.goal[0].resolved_day=1;probe.State.goal.Add(new CafeGoalStage {status="cleared",started_day=2,resolved_day=2});
            Assert(!probe.CustomerSchedule(day).Any(a=>a.customer_id=="advanced-longhair"),"Draw equality must reject arrival");
            config.longhair_customer.probability=Math.BitIncrement(expected);Assert(probe.CustomerSchedule(day).Count(a=>a.customer_id=="advanced-longhair")==1,"Probability just above draw must admit arrival");
            config.longhair_customer.probability=0;probe.State.loyalty.Add(new CafeLoyalty {customer_id="advanced-longhair",regular_day=1,score=rules.loyalty.threshold});
            int regularDay=Enumerable.Range(3,7).First(d=>(rules.start_weekday+d-1)%7==probe.ExtraWeekday("advanced-longhair"));
            Assert(probe.CustomerSchedule(regularDay).Count(a=>a.customer_id=="advanced-longhair")==1,"Loyalty arrival must survive failed normal draw");
            config.longhair_customer.probability=1;Assert(probe.CustomerSchedule(regularDay).Count(a=>a.customer_id=="advanced-longhair")==1,"Normal and loyalty arrivals duplicated customer");
            Assert(!probe.CustomerSchedule(regularDay).Any(a=>a.customer_id=="vip-calico"),"VIP unlocked before final stage");
            probe.State.goal.Add(new CafeGoalStage {status="cleared",started_day=3,resolved_day=regularDay});
            Assert(!probe.CustomerSchedule(regularDay).Any(a=>a.customer_id=="vip-calico") && probe.CustomerSchedule(regularDay+1).Count(a=>a.customer_id=="vip-calico")==1,"VIP unlock day boundary differs");
        }
        if(args.Length>11)
        {
            using var matrix=JsonDocument.Parse(File.ReadAllText(args[11]));
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            foreach(var row in matrix.RootElement.GetProperty("cases").EnumerateArray())
            {
                var config=Decode<CafeRules>(Encode(rules));string cat=row.GetProperty("cat").GetString(),id=row.GetProperty("id").GetString();
                config.cats.Single(c=>c.id==cat).features=row.GetProperty("features").EnumerateArray().Select(f=>f.GetString()).ToArray();
                var sim=new CafeSimulation(config,objective:"popularity");
                var result=(string)typeof(CafeSimulation).GetMethod("SpecialistResult",flags).Invoke(sim,new object[]{sim.State,id,cat,row.GetProperty("connect").GetInt32(),row.GetProperty("open").GetInt32(),row.GetProperty("equipment").GetBoolean(),row.GetProperty("count").GetInt32(),row.GetProperty("simultaneous").GetInt32()});
                Assert(result==row.GetProperty("result").GetString(),"Python specialist condition mismatch");
                foreach(string field in new[]{"bonus","popularity"})Assert((double)typeof(CafeSimulation).GetMethod(field=="bonus"?"SpecialistBonus":"SpecialistPopularity",flags).Invoke(sim,new object[]{id,result})==row.GetProperty(field).GetDouble(),"Python specialist reward mismatch");
            }
        }
        if(args.Length>10)
        {
            using var cases=JsonDocument.Parse(File.ReadAllText(args[10]));
            foreach(var fixture in cases.RootElement.GetProperty("cases").EnumerateArray())
            {
                bool testedClear=false;var seats=new CafeSimulation(Decode<CafeRules>(fixture.GetProperty("rules").GetRawText()),seed:fixture.TryGetProperty("seed",out var dispatchSeed)?dispatchSeed.GetInt32():0,objective:fixture.GetProperty("objective").GetString());scenarios++;
                foreach(var op in fixture.GetProperty("operations").EnumerateArray())
                {
                    string beforeSeats=Encode(seats.State);bool rejected=false;
                    try{seats.Execute(op.GetProperty("kind").GetString(),working:Decode<string[]>(op.GetProperty("working").GetRawText()),choice:op.GetProperty("choice").GetString(),source:op.GetProperty("source").GetString(),catId:op.GetProperty("cat_id").GetString(),seatId:op.TryGetProperty("seat_id",out var seatId)?seatId.GetString():null,customerId:op.TryGetProperty("customer_id",out var customerId)?customerId.GetString():null);}catch(InvalidOperationException){rejected=true;}catch(ArgumentException){rejected=true;}
                    Assert(rejected==op.GetProperty("rejected").GetBoolean(),"Python expansion rejection mismatch: "+op.GetProperty("kind")+" day "+seats.State.day);
                    if(rejected)Assert(beforeSeats==Encode(seats.State),"Rejected seat operation changed state");
                    using var current=JsonDocument.Parse(Encode(seats.State));Equal(op.GetProperty("expected"),current.RootElement,"expansion boundary");seats.Restore(Decode<CafeSnapshot>(Encode(seats.State)));ticks++;
                    if(!testedClear && seats.State.clear_results.Count>0)
                    {
                        testedClear=true;
                        for(int field=0;field<9;field++)
                        {
                            var bad=Decode<CafeSnapshot>(Encode(seats.State));var row=bad.clear_results[0];
                            if(field==0)bad.clear_results.Clear();else if(field==1)bad.clear_results.Add(row);else if(field==2)row.day++;else if(field==3)row.target++;else if(field==4)row.value++;else if(field==5)row.cats++;else if(field==6)row.revenue++;else if(field==7)row.funds=0;else row.mode="bond";
                            string clearBefore=Encode(seats.State);bool failed=false;try{seats.Restore(bad);}catch(ArgumentException){failed=true;}Assert(failed && clearBefore==Encode(seats.State),"Forged fixed clear record accepted");
                        }
                        var legacy=Decode<CafeSnapshot>(Encode(seats.State));legacy.version=1;bool legacyFailed=false;try{seats.Restore(legacy);}catch(ArgumentException){legacyFailed=true;}Assert(legacyFailed,"Old save without fixed record migration accepted");
                    }
                }
            }
        }
        if(args.Length>12)DispatchCases(args[12],ref scenarios,ref ticks);
        if(args.Length>9)
        {
            using var cases=JsonDocument.Parse(File.ReadAllText(args[9]));
            foreach(var fixture in cases.RootElement.GetProperty("cases").EnumerateArray())
            {
                var items=new CafeSimulation(Decode<CafeRules>(fixture.GetProperty("rules").GetRawText()));scenarios++;
                foreach(var op in fixture.GetProperty("operations").EnumerateArray())
                {
                    string itemBefore=Encode(items.State);bool rejected=false;
                    try{items.Execute(op.GetProperty("kind").GetString(),choice:op.GetProperty("choice").GetString(),source:op.GetProperty("source").GetString(),catId:op.GetProperty("cat_id").GetString(),seatId:op.TryGetProperty("seat_id",out var seatId)?seatId.GetString():null,customerId:op.TryGetProperty("customer_id",out var customerId)?customerId.GetString():null);}catch(InvalidOperationException){rejected=true;}catch(ArgumentException){rejected=true;}
                    Assert(rejected==op.GetProperty("rejected").GetBoolean(),"Python item rejection mismatch: "+op.GetProperty("kind"));if(rejected)Assert(itemBefore==Encode(items.State),"Rejected item changed state");
                    using var current=JsonDocument.Parse(Encode(items.State));Equal(op.GetProperty("expected"),current.RootElement,"item boundary");items.Restore(Decode<CafeSnapshot>(Encode(items.State)));ticks++;
                }
            }
        }
        if (args.Length > 8)
        {
            using var housingCases=JsonDocument.Parse(File.ReadAllText(args[8]));
            foreach(var fixture in housingCases.RootElement.GetProperty("cases").EnumerateArray())
            {
                var configured=Decode<CafeRules>(Encode(rules));configured.starting_funds=fixture.GetProperty("funds").GetDouble();var housing=new CafeSimulation(configured);
                foreach(var operation in fixture.GetProperty("operations").EnumerateArray())
                {
                    string housingBefore=Encode(housing.State);bool rejected=false;
                    try{housing.Execute(operation.GetProperty("kind").GetString());}catch(InvalidOperationException){rejected=true;}catch(ArgumentException){rejected=true;}
                    Assert(rejected==operation.GetProperty("rejected").GetBoolean(),"Python housing rejection mismatch");
                    if(rejected)Assert(housingBefore==Encode(housing.State),"Rejected housing changed state");
                    using var current=JsonDocument.Parse(Encode(housing.State));Equal(operation.GetProperty("expected"),current.RootElement,"housing boundary");
                    housing.Restore(Decode<CafeSnapshot>(Encode(housing.State)));ticks++;
                }
                scenarios++;
            }
        }
        if (args.Length > 4)
        {
            using var boundary = JsonDocument.Parse(File.ReadAllText(args[4])); var customers = new CafeSimulation(rules);
            foreach (var row in boundary.RootElement.GetProperty("weekdays").EnumerateArray()) Assert(customers.ExtraWeekday(row.GetProperty("customer_id").GetString()) == row.GetProperty("weekday").GetInt32(), "Extra weekday mismatch");
            foreach (var row in boundary.RootElement.GetProperty("steps").EnumerateArray())
            {
                int day = row.GetProperty("day").GetInt32(); string kind = row.GetProperty("kind").GetString(), id = row.GetProperty("customer_id").GetString(), value = row.GetProperty("value").GetString(); customers.State.day = day;
                if (kind == "service") customers.ApplyCustomerService(customers.State.loyalty, customers.State.discontent, day, id, value);
                else if (kind == "departure") customers.ChangeDiscontent(customers.State.discontent, day, id, rules.discontent.waiting_gain);
                using var loyal = JsonDocument.Parse(Encode(customers.State.loyalty)); Equal(row.GetProperty("loyalty"), loyal.RootElement, "loyalty boundary");
                using var unhappy = JsonDocument.Parse(Encode(customers.State.discontent)); Equal(row.GetProperty("discontent"), unhappy.RootElement, "discontent boundary");
                foreach (var query in row.GetProperty("queries").EnumerateArray())
                {
                    string customer = query.GetProperty("customer_id").GetString(); int at = query.GetProperty("day").GetInt32();
                    Assert(customers.DiscontentScore(customer, at) == query.GetProperty("score").GetDouble() && customers.CustomerAvailable(customer, at) == query.GetProperty("available").GetBoolean(), "Suspension boundary mismatch");
                }
                var extras = customers.State.loyalty.Where(r => r.regular_day > 0 && day > r.regular_day && customers.ExtraWeekday(r.customer_id) == (rules.start_weekday + day - 1) % 7).Select(r => new CafeArrival { customer_id = r.customer_id, tick = 0 }).ToArray();
                using var extra = JsonDocument.Parse(Encode(extras)); Equal(row.GetProperty("extra_schedule"), extra.RootElement, "regular arrival boundary");
                customers.State.loyalty = Decode<List<CafeLoyalty>>(Encode(customers.State.loyalty)); customers.State.discontent = Decode<List<CafeDiscontent>>(Encode(customers.State.discontent));
            }
        }
        if (args.Length > 5)
        {
            using var boundaries = JsonDocument.Parse(File.ReadAllText(args[5]));
            foreach (var row in boundaries.RootElement.GetProperty("cases").EnumerateArray())
            {
                var sim = new CafeSimulation(rules); sim.State.day = row.GetProperty("day").GetInt32();
                sim.State.phase = row.GetProperty("preparation").GetBoolean() ? "preparation" : "open";
                sim.State.game_over = row.GetProperty("game_over").GetBoolean() ? "funds" : null;
                sim.State.loyalty = Decode<List<CafeLoyalty>>(row.GetProperty("loyalty").GetRawText()); sim.PresentRegularIntroduction();
                using var actual = JsonDocument.Parse(Encode(sim.State.regular_introduction)); Equal(row.GetProperty("expected"), actual.RootElement, "Regular presentation boundary");
            }
        }
        if (args.Length > 6)
        {
            using var trust = JsonDocument.Parse(File.ReadAllText(args[6]));
            foreach (var scenario in trust.RootElement.GetProperty("scenarios").EnumerateArray())
            {
                var sim = new CafeSimulation(Decode<CafeRules>(scenario.GetProperty("rules").GetRawText())); scenarios++;
                foreach (var row in scenario.GetProperty("operations").EnumerateArray())
                {
                    sim.Execute(row.GetProperty("kind").GetString(), catId: row.TryGetProperty("cat_id", out var catId) ? catId.GetString() : null, working: row.TryGetProperty("working", out var working) ? working.EnumerateArray().Select(v => v.GetString()).ToArray() : null, choice: row.TryGetProperty("choice", out var choice) ? choice.GetString() : null, source: row.TryGetProperty("source", out var source) ? source.GetString() : null);
                    ticks++; using var state = JsonDocument.Parse(Encode(sim.State)); Equal(row.GetProperty("expected"), state.RootElement, "trust " + scenario.GetProperty("name").GetString());
                    sim.Validate(sim.State); sim.Restore(Decode<CafeSnapshot>(Encode(sim.State)));
                }
                TrustChecks.CheckFinal(sim, Assert);
            }
        }
        if (args.Length > 7)
        {
            using var growth = JsonDocument.Parse(File.ReadAllText(args[7]));
            foreach (var fixture in growth.RootElement.GetProperty("cases").EnumerateArray())
            {
                var sim = new CafeSimulation(rules);
                foreach (var row in fixture.GetProperty("operations").EnumerateArray())
                {
                    sim.ApplyGrowthBoundary(Decode<CafeGrowthOperation>(row.GetProperty("operation").GetRawText()));
                    using var actual = JsonDocument.Parse(Encode(sim.GrowthCat("cat-kohaku"))); Equal(row.GetProperty("expected"), actual.RootElement, "growth function boundary");
                }
            }
        }
        using var data = JsonDocument.Parse(File.ReadAllText(args[1]));
        foreach (var row in data.RootElement.GetProperty("satisfaction").EnumerateArray())
        {
            var result = CafeSatisfactionEvaluator.Evaluate(rules.satisfaction, Decode<CafeExchange>(row.GetProperty("initial").GetRawText()), row.GetProperty("matched").GetBoolean());
            using var actual = JsonDocument.Parse(Encode(result)); Equal(row.GetProperty("expected"), actual.RootElement, "satisfaction boundary");
        }
        foreach (var draw in data.RootElement.GetProperty("draws").EnumerateArray()) Assert(PythonRandom.Draw(draw.GetProperty("key").GetString()) == draw.GetProperty("value").GetDouble(), "CPython RNG mismatch");
        if (args.Length > 2)
        {
            using var comparison = JsonDocument.Parse(File.ReadAllText(args[2]));
            foreach (var row in comparison.RootElement.GetProperty("rows").EnumerateArray())
            {
                var day = new CafeSimulation(rules, row.GetProperty("seed").GetInt32());
                day.Execute("start_business"); while (day.State.phase != "closed") day.Execute("advance_business");
                var expected = row.GetProperty("full_python");
                Assert(day.State.funds == expected.GetProperty("funds").GetDouble() && day.State.revenue == expected.GetProperty("revenue").GetDouble()
                    && day.State.expenses == expected.GetProperty("operating_cost").GetDouble() && day.State.popularity == expected.GetProperty("popularity").GetDouble()
                    && day.State.completed == expected.GetProperty("completed_interactions").GetInt32(), "Full Python first day finance mismatch");
                foreach (var cat in day.State.cats) Assert(cat.stamina == expected.GetProperty("cat_stamina").GetProperty(cat.id).GetDouble(), "Full Python first day stamina mismatch");
                foreach (string level in new[] { "satisfied", "normal", "dissatisfied" }) Assert(day.State.visits.Count(v => v.satisfaction != null && v.satisfaction.level == level) == expected.GetProperty("customer_satisfaction").GetProperty(level).GetInt32(), "Full Python first day satisfaction mismatch");
            }
        }
        using var eventCases = args.Length > 3 ? JsonDocument.Parse(File.ReadAllText(args[3])) : JsonDocument.Parse("{\"scenarios\":[]}");
        foreach (var scenario in data.RootElement.GetProperty("scenarios").EnumerateArray().Concat(eventCases.RootElement.GetProperty("scenarios").EnumerateArray()))
        {
            var sim = new CafeSimulation(rules, scenario.GetProperty("seed").GetInt32()); scenarios++;
            if (scenario.TryGetProperty("initial", out var initialState))
            {
                using var actual = JsonDocument.Parse(Encode(sim.State)); Equal(initialState, actual.RootElement, "Initial event");
                sim.Restore(Decode<CafeSnapshot>(Encode(sim.State)));
            }
            foreach (var row in scenario.GetProperty("operations").EnumerateArray())
            {
                string kind = row.GetProperty("kind").GetString();
                var working = row.TryGetProperty("working", out var w) ? w.EnumerateArray().Select(v => v.GetString()).ToArray() : null;
                sim.Execute(kind, working, row.TryGetProperty("cat_id", out var catId) ? catId.GetString() : null, row.TryGetProperty("choice", out var choice) ? choice.GetString() : null, row.TryGetProperty("source", out var source) ? source.GetString() : null); ticks++;
                using var state = JsonDocument.Parse(Encode(sim.State)); Equal(row.GetProperty("expected"), state.RootElement, "seed=" + sim.State.seed + "/day=" + sim.State.day + "/tick=" + sim.State.tick);
                sim.Validate(sim.State);
                // Resume at EVERY operation boundary; compare the continuation next tick.
                sim.Restore(Decode<CafeSnapshot>(Encode(sim.State)));
            }
            if (sim.State.growth_operations.Any(o => o.kind == "service"))
            {
            foreach (Action<CafeSnapshot> forge in new Action<CafeSnapshot>[] {
                s => s.growth[0].service++, s => s.growth[0].selected_day = s.day + 1,
                s => s.growth[0].type_actions[0]++, s => s.growth_operations.Clear(),
                s => s.growth_operations.First(o => o.kind == "service").actions[0]++ })
            {
                string growthBefore = Encode(sim.State); var bad = Decode<CafeSnapshot>(growthBefore); forge(bad); bool failed = false;
                try { sim.Restore(bad); } catch (ArgumentException) { failed = true; }
                Assert(failed && Encode(sim.State) == growthBefore, "Forged growth restore mutated state");
            }
            }
        }
        foreach (var test in data.RootElement.GetProperty("exchanges").EnumerateArray())
        {
            var sim = new CafeSimulation(rules);
            var initial = Decode<CafeExchange>(test.GetProperty("initial").GetRawText());
            var cat = rules.cats[test.GetProperty("cat_index").GetInt32()];
            foreach (var step in test.GetProperty("steps").EnumerateArray())
            {
                string target = step.TryGetProperty("target", out var t) ? t.GetString() : null;
                sim.StepExchange(initial, cat, step.GetProperty("action").GetString(), target);
                using var state = JsonDocument.Parse(Encode(initial)); Equal(step.GetProperty("expected"), state.RootElement, "exchange");
            }
        }
        var check = new CafeSimulation(rules);
        var customerSave = new CafeSimulation(rules); customerSave.Execute("start_business"); while (customerSave.State.phase != "closed") customerSave.Execute("advance_business");
        string validCustomerSave = Encode(customerSave.State); var badCustomer = Decode<CafeSnapshot>(validCustomerSave); badCustomer.loyalty[0].score += 1;
        try { customerSave.Restore(badCustomer); throw new Exception("Forged loyalty accepted"); } catch (ArgumentException) { }
        Assert(Encode(customerSave.State) == validCustomerSave, "Rejected customer load changed state");
        int troubleSeed = Enumerable.Range(0, 1000).First(seed => new CafeSimulation(rules, seed).CurrentStoreEvent.type == "trouble");
        var pending = new CafeSimulation(rules, troubleSeed); string pendingBefore = Encode(pending.State);
        try { pending.Execute("start_business"); throw new Exception("Unanswered event started business"); } catch (InvalidOperationException) { }
        Assert(Encode(pending.State) == pendingBefore, "Pending rejection mutated state");
        var forged = Decode<CafeSnapshot>(pendingBefore); forged.store_events[0].draw += .001;
        try { pending.Restore(forged); throw new Exception("Forged event draw accepted"); } catch (ArgumentException) { }
        Assert(Encode(pending.State) == pendingBefore, "Rejected event load mutated state");
        int supplySeed = Enumerable.Range(0, 1000).First(seed => new CafeSimulation(rules, seed).CurrentStoreEvent.type == "supplies");
        var supplies = new CafeSimulation(rules, supplySeed); string unused = Encode(supplies.State);
        try { supplies.Execute("use_item", catId: supplies.State.cats[0].id, source: supplies.AvailableSupplies[0]); throw new Exception("Zero-stress supply consumed"); } catch (ArgumentException) { }
        Assert(Encode(supplies.State) == unused, "Rejected supply consumption mutated state");
        int supportSeed = Enumerable.Range(0, 1000).First(seed => new CafeSimulation(rules, seed).CurrentStoreEvent.type == "support");
        var insolvent = new CafeSimulation(rules, supportSeed); insolvent.State.opening_funds = insolvent.State.funds = rules.store_events.support_full_cost;
        insolvent.Execute("resolve_store_event", choice: "full");
        Assert(insolvent.State.game_over == "funds" && insolvent.CurrentStoreEvent.status == "resolved" && insolvent.State.popularity == rules.starting_popularity, "Support expense timing or game-over mismatch");
        var capped = new CafeSimulation(rules); capped.State.popularity = rules.popularity.cap - 1;
        capped.Execute("start_business"); while (capped.State.phase != "closed") capped.Execute("advance_business");
        Assert(capped.State.popularity == rules.popularity.cap && capped.State.popularity_gain == 1, "Popularity cap mismatch");
        var holiday = new CafeSimulation(rules); holiday.Execute("day_off");
        Assert(holiday.State.popularity == rules.starting_popularity && holiday.State.history[0].popularity_gain == 0, "Holiday gained popularity");
        string before = Encode(check.State);
        try { check.Execute("advance_business"); throw new Exception("Invalid phase accepted"); } catch (InvalidOperationException) { }
        Assert(before == Encode(check.State), "Rejected command mutated state");
        try { check.Execute("set_shifts", new[] { "unknown" }); throw new Exception("Unknown cat accepted"); } catch (ArgumentException) { }
        Assert(before == Encode(check.State), "Rejected shift mutated state");
        string temp = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1])), "save-test-" + Guid.NewGuid());
        var store = new CafeSaveStore(temp, Encode, Decode<CafeSaveEnvelope>, check.Validate);
        store.Save(1, check.State); check.Execute("start_business"); store.Save(1, check.State);
        Assert(File.Exists(Path.Combine(temp, "slot-1.json.bak")), "Overwrite backup absent");
        using (var expected = JsonDocument.Parse(Encode(check.State))) using (var actual = JsonDocument.Parse(Encode(store.Load(1).state))) Equal(expected.RootElement, actual.RootElement, "disk save");
        try { store.Load(11); throw new Exception("Invalid slot accepted"); } catch (ArgumentOutOfRangeException) { }
        before = Encode(check.State);
        var invalid = Decode<CafeSnapshot>(before); invalid.version = 99;
        try { check.Restore(invalid); throw new Exception("Future version accepted"); } catch (ArgumentException) { }
        Assert(before == Encode(check.State), "Rejected load mutated state");
        File.WriteAllText(Path.Combine(temp, "slot-1.json"), "{broken");
        try { store.Load(1); throw new Exception("Corrupt save accepted"); } catch (JsonException) { }
        string retryTarget=Path.Combine(temp,"retry.json"),retryTemp=retryTarget+".tmp";
        File.WriteAllText(retryTarget,"previous");File.WriteAllText(retryTemp,"new");int attempts=0,waits=0;
        CafeSaveStore.ReplaceWithRetry(retryTemp,retryTarget,(a,b,c)=>{if(++attempts<3)throw new IOException("temporary conflict");File.Replace(a,b,c);},delay=>waits++);
        Assert(attempts==3 && waits==2 && File.ReadAllText(retryTarget)=="new" && File.ReadAllText(retryTarget+".bak")=="previous","Transient replacement did not preserve backup");
        File.WriteAllText(retryTemp,"uncommitted");attempts=0;bool retryFailed=false;
        try{CafeSaveStore.ReplaceWithRetry(retryTemp,retryTarget,(a,b,c)=>{attempts++;throw new IOException("persistent conflict");},delay=>{});}catch(IOException){retryFailed=true;}
        Assert(retryFailed && attempts==4 && File.ReadAllText(retryTarget)=="new" && File.ReadAllText(retryTemp)=="uncommitted","Persistent replacement lost save or retried forever");
        attempts=0;retryFailed=false;try{CafeSaveStore.ReplaceWithRetry(retryTemp,retryTarget,(a,b,c)=>{attempts++;throw new UnauthorizedAccessException();},delay=>{});}catch(UnauthorizedAccessException){retryFailed=true;}
        Assert(retryFailed && attempts==1 && File.ReadAllText(retryTarget)=="new","Non-IO permission error was retried");
        HousingChecks.Run(rules, Assert, Path.Combine(temp, "housing"));
        IntakeChecks.Run(rules, Assert, Path.Combine(temp, "intake"));
        RegularIntroductionChecks.Run(rules, Assert, Path.Combine(temp, "regular"));
        var precision = Decode<CafeSnapshot>(Encode(check.State)); precision.cats[0].stamina = 21.599999999999998;
        var preciseBits = CafeDoublePrecision.Capture(precision); precision.cats[0].stamina = 21.6;
        CafeDoublePrecision.Restore(precision, preciseBits);
        Assert(BitConverter.DoubleToInt64Bits(precision.cats[0].stamina) == BitConverter.DoubleToInt64Bits(21.599999999999998), "Rounded JSON double was not restored exactly");
        bool precisionFailed = false; try { CafeDoublePrecision.Restore(precision, preciseBits.Skip(1).ToArray()); } catch (ArgumentException) { precisionFailed = true; }
        Assert(precisionFailed, "Truncated numeric restore accepted");
        precision.cats[0].stamina = 1; precisionFailed = false; try { CafeDoublePrecision.Restore(precision, preciseBits); } catch (ArgumentException) { precisionFailed = true; }
        Assert(precisionFailed, "Numeric restore disagreed with JSON");
        Console.WriteLine($"PASS: {scenarios} Python/C# scenarios, {ticks} operation boundaries, {assertions} assertions, all-boundary save/resume and disk persistence.");
    }
}
