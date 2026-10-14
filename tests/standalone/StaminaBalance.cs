using CatCafePreview.Standalone;
static partial class Program
{
    static void StaminaBalance(string rulesPath,string output)
    {
        var results=new List<object>();
        foreach(string target in new[]{"bond","popularity","service-after-player"})foreach(string mode in new[]{"clear","fast"})foreach(int seed in new[]{0,1,42})foreach(string variant in new[]{"10-fixed","20-fixed","10-growth","20-growth"})
        {
            var r=Decode<CafeRules>(File.ReadAllText(rulesPath));r.stamina_growth=CafeStaminaRules.Provisional();r.stamina_growth.increment=variant.EndsWith("fixed")?0:10;
            var sim=new CafeSimulation(r,seed,target=="service-after-player"?"free":target);sim.LegacyPlayerReplay=variant.StartsWith("10");
            double fatigue=0,stress=0;int sick=0,missing=0,exhausted=0;var shifts=new Dictionary<string,int>();int days=0,steps=0;string failure=null;
            try
            {
                while(sim.State.day<=(target=="service-after-player"?40:60) && sim.State.game_over==null && steps++<50000)
                {
                    var s=sim.State;
                    if(s.day!=days){days=s.day;fatigue+=s.cats.Sum(c=>c.fatigue);stress+=s.cats.Sum(c=>c.stress);sick+=s.cats.Count(c=>c.health=="sick");missing+=s.cats.Count(c=>c.activity=="missing");}
                    string kind=null,cat=null,choice=null,source=null;
                    var g=s.growth.FirstOrDefault(g=>sim.GrowthPendingKind(g)!=null);
                    if(g!=null){kind=sim.GrowthPendingKind(g);cat=g.cat_id;choice=kind=="resolve_growth"?"rest":kind=="resolve_growth_mastery"?sim.MasteryChoices(g).First():sim.TypeMasteryChoices(g).First();}
                    else if(sim.StoreEventWaiting){kind="resolve_store_event";choice=sim.CurrentStoreEvent.type=="trouble"?"patch":"decline";}
                    else if(sim.CustomerTrustWaiting){kind="resolve_customer_trust";source=s.trust_events.First(e=>e.status=="waiting").id;choice="recover";}
                    else if(s.cats.Any(c=>c.activity=="missing" && c.missing_remaining==0)){kind="resolve_missing";cat=s.cats.First(c=>c.activity=="missing" && c.missing_remaining==0).id;}
                    else if(sim.IntakeWaiting){kind="resolve_intake";choice=sim.ActiveCatCount(s)<sim.HousingCapacity(s,s.day) && s.funds>r.intake.cost+2000?"accept":"decline";}
                    else if(sim.RegularIntroductionWaiting){kind="resolve_regular_introduction";choice=sim.ActiveCatCount(s)<sim.HousingCapacity(s,s.day) && s.funds>r.regular_introduction.cost+2000?"accept":"decline";}
                    else if(sim.ReservationWaiting){kind="resolve_reservation";choice="decline";}
                    else if(sim.BondWaiting)break;
                    else if(sim.GoalWaiting){if(s.goal.Count==r.goal_stages.Length || s.goal.Last().status=="expired")break;kind="advance_goal";}
                    if(kind!=null){sim.Execute(kind,catId:cat,choice:choice,source:source);continue;}
                    CafeAutoOperation op;
                    if(target=="service-after-player" && sim.PlayerActive)
                    {var exchange=sim.ActivePlayer.exchange;op=new CafeAutoOperation {kind="player_step",choice=exchange.connect_pending?"connect":exchange.stamina<=20?"pause":"direct"};}
                    else if(target=="service-after-player" && s.phase=="preparation" && sim.PlayerRemaining>0 && s.cats.Any(c=>sim.PlayerCat(c.id).today==0 && sim.PlayerBeginReason(c.id)==""))
                    {op=new CafeAutoOperation {kind="player_begin",cat_id=s.cats.Where(c=>sim.PlayerCat(c.id).today==0 && sim.PlayerBeginReason(c.id)=="").OrderBy(c=>c.id,StringComparer.Ordinal).First().id};}
                    else op=sim.PlanAutoplay(target=="service-after-player"?"popularity":target,mode);
                    if(op.kind=="start_business")foreach(var c in s.cats.Where(c=>c.working)){shifts.TryGetValue(c.id,out int n);shifts[c.id]=n+1;}
                    sim.Execute(op.kind,op.working,op.cat_id,op.choice,op.source,op.seat_id);
                    if(s.phase=="closed")exhausted+=s.cats.Count(c=>c.stamina==0);
                }
                sim.Validate(sim.State);
            }
            catch(Exception ex){failure=ex.Message;}
            var state=sim.State;var servicesByCat=state.history.SelectMany(h=>h.customer_records).Where(v=>v.cat_id!=null && v.satisfaction!=null).Select(v=>v.cat_id).Concat(state.visits.Where(v=>v.satisfaction!=null).Select(v=>v.cat_id)).GroupBy(id=>id).ToDictionary(g=>g.Key,g=>g.Count());var row=new {target,mode,seed,variant,day=state.day,clear_day=target=="bond"?(int?)state.bond_goal?.resolved_day:state.goal.LastOrDefault()?.resolved_day,revenue=state.history.Sum(h=>h.revenue)+state.revenue,funds=state.funds,popularity=state.popularity,services=state.history.Sum(h=>h.completed)+state.completed,cat_days_sick=sick,cat_days_missing=missing,fatigue_sum=fatigue,stress_sum=stress,exhausted_observations=exhausted,maximum=state.cats.Max(c=>sim.MaxStamina(c.id)),services_by_cat=servicesByCat,shifts,game_over=state.game_over,failure,steps};
            results.Add(row);File.WriteAllText(output,Encode(results));Console.WriteLine(Encode(row));
        }
    }
}
