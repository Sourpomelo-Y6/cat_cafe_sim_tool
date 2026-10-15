using CatCafePreview.Standalone;
static partial class Program
{
    static void CatalogChecks(string rulesPath,string pack,string output)
    {
        Directory.CreateDirectory(output);
        var catalog=Decode<CafeNarrativeCatalog>(File.ReadAllText(Path.Combine(pack,"interaction_texts.json")));
        catalog.Validate();Assert(catalog.ComputeRevision()==catalog.revision,"Python/C# catalog revision differs");
        var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));rules.store_events.probability=0;
        CafeSimulation New()=>new CafeSimulation(Decode<CafeRules>(Encode(rules)));
        var sim=New();var n=new CafeNarratives(sim);n.SetCatalog(catalog);
        sim.Execute("player_begin",catId:"cat-mike");sim.Execute("player_step",choice:"direct");
        var first=n.Rows.Single();Assert(first.speech_id==catalog.Find("speech","player","teaser","direct","any").variants[0].id,"specific speech not selected");
        Assert(first.cat_text_id==catalog.Find("cat_response","cat-mike","teaser","direct",first.reaction).variants[0].id,"reaction text differs from actual result");
        Assert(first.image_resource.StartsWith("CatCafeContent/Imports/"+catalog.pack_id+"/images/"),"image receipt missing");
        foreach(string mode in new[]{"teaser","pet"})foreach(string action in CafeNarrativeCatalog.Actions(mode).Where(a=>a!="any"))
        {
            var s=New();var rows=new CafeNarratives(s);rows.SetCatalog(catalog);s.Execute("player_begin",catId:"cat-mike");
            if(mode=="pet")s.Execute("player_step",choice:"switch",source:"pet");
            s.Execute("player_step",choice:action);var actual=rows.Rows.Last();
            Assert(actual.cat_pool==catalog.Find("cat_response","cat-mike",mode,action,actual.reaction).Key(),"action selected wrong reaction pool");
            Assert(actual.human_action==catalog.action_texts.Single(a=>a.mode==mode && a.action==action).text.Replace("{human}","プレイヤー").Replace("{cat}","ミケ"),"action prose differs from operation");
        }
        var sparse=Decode<CafeNarrativeCatalog>(Encode(catalog));sparse.entries=sparse.entries.Where(e=>e.kind=="speech").ToArray();
        foreach(var e in sparse.entries)e.variants=e.variants.Take(1).ToArray();sparse.revision=sparse.ComputeRevision();
        var missing=New();var missingRows=new CafeNarratives(missing);missingRows.SetCatalog(sparse);missing.Execute("player_begin",catId:"cat-mike");
        missing.Execute("player_step",choice:"pause");missing.Execute("player_step",choice:"pause");
        Assert(missingRows.Rows[0].speech_id==missingRows.Rows[1].speech_id,"single candidate not reusable");
        Assert(string.IsNullOrEmpty(missingRows.Rows.Last().cat_text_id) && !string.IsNullOrEmpty(missingRows.Rows.Last().cat_response),"missing reaction should use prototype");
        for(int i=0;i<3;i++)sim.Execute("player_step",choice:"pause");
        var pauses=n.Rows.Where(r=>r.action=="pause").ToArray();Assert(pauses.Select(r=>r.speech_id).Distinct().Count()==3,"speech repeats within 3 choices");
        Assert(pauses.Select(r=>r.cat_text_id).Distinct().Count()==3,"cat text repeats within 3 choices");
        string session=sim.ActivePlayer.id,prose=n.Read(session),digest=CafeReplayJournal.Digest(sim.State),image=n.Rows.Last().image_resource;
        for(int i=0;i<30;i++)Assert(n.Read(session)==prose,"read changed prose");Assert(digest==CafeReplayJournal.Digest(sim.State),"read consumed resources");
        var store=new CafeSaveStore(output,s=>{s.narratives=n.Rows.ToArray();return ReplayEncode(s);},ReplayDecode,sim.Validate);
        store.Save(1,sim.State);var save=store.Load(1);CafeNarratives.Validate(save);sim.Restore(save.state);n.Restore(save.narratives);
        Assert(n.Read(session)==prose && n.Rows.Last().image_resource==image,"disk load changed prose/image");
        sim.Execute("player_step",choice:"pause");Assert(n.Rows.Last().speech_id==pauses[0].speech_id,"rotation not resumed from saved candidates");
        var changed=Decode<CafeNarrativeCatalog>(Encode(catalog));foreach(var e in changed.entries)foreach(var v in e.variants)v.text+="（更新）";
        changed.pack_id=new string('a',32);changed.revision=changed.ComputeRevision();n.SetCatalog(changed);
        Assert(n.Rows[0].text==first.text && n.Rows[0].image_resource==first.image_resource,"catalog update changed historical receipt");
        sim.Execute("player_step",choice:"pause");Assert(n.Rows.Last().text.Contains("更新"),"new catalog not used for new actions");
        n.SetCatalog(null);Assert(n.Rows[0].text==first.text,"missing catalog changes saved prose");sim.Execute("player_step",choice:"pause");Assert(string.IsNullOrEmpty(n.Rows.Last().catalog_revision),"missing catalog should use prototype");
        foreach(string reaction in new[]{"listless","turn_away"})
        {
            var r=Decode<CafeRules>(Encode(rules));if(reaction=="turn_away")r.cats.Single(c=>c.id=="cat-mike").type_preferences[0]=0;
            var s=new CafeSimulation(r);if(reaction=="listless")s.State.cats.Single(c=>c.id=="cat-mike").stamina=10;
            var receipt=new CafeNarratives(s);receipt.SetCatalog(catalog);s.Execute("player_begin",catId:"cat-mike");s.Execute("player_step",choice:"direct");
            Assert(receipt.Rows.Single().reaction==reaction,"boundary did not produce expected actual reaction");
            Assert(receipt.Rows.Single().cat_pool.Contains("/"+reaction+"/"),"wrong reaction pool selected");
        }
        var service=New();var serviceRows=new CafeNarratives(service);serviceRows.SetCatalog(catalog);service.Execute("set_shifts",working:new[]{"cat-mike"});service.Execute("start_business");
        while(service.State.phase=="open")service.Execute("advance_business");
        var visits=serviceRows.Rows.GroupBy(r=>r.session).ToArray();Assert(visits.Length>=2,"service turnover not exercised");
        foreach(var visit in visits){Assert(visit.Select(r=>r.customer_id).Distinct().Count()==1,"customers mixed");Assert(serviceRows.Read(visit.Key).Contains(visit.Last().text),"service latest text missing");}
        Assert(serviceRows.Rows.Any(r=>r.customer_id=="guest-1" && !string.IsNullOrEmpty(r.speech_id)),"guest authored speech unused");
        Assert(serviceRows.Rows.Any(r=>r.customer_id!="guest-1" && string.IsNullOrEmpty(r.speech_id)),"unknown customer should retain prototype speech");
        Assert(serviceRows.Seat(null).Contains("空席"),"empty seat shows former visit");
        var baseline=New();var decorated=New();var observer=new CafeNarratives(decorated);observer.SetCatalog(catalog);
        foreach(var s in new[]{baseline,decorated})s.Execute("player_begin",catId:"cat-mike");
        for(int i=0;i<20;i++)
        {
            string action=baseline.ActivePlayer.exchange.connect_pending?"connect":"pause";
            baseline.Execute("player_step",choice:action);decorated.Execute("player_step",choice:action);
            Assert(CafeReplayJournal.Digest(baseline.State)==CafeReplayJournal.Digest(decorated.State),"catalog changed game RNG/rules/state");
        }
        for(int test=0;test<5;test++)
        {
            var bad=Decode<CafeNarrativeCatalog>(Encode(catalog));
            if(test==0)bad.revision=new string('0',64);
            if(test==1)bad.entries[0].variants[0].text+="tampered";
            if(test==2)bad.entries[0].variants[0].id="bad";
            if(test==3)bad.image_bindings[0].image="../outside.png";
            if(test==4)bad.entries[0].reaction="invalid";
            bool rejected=false;try{bad.Validate();}catch(ArgumentException){rejected=true;}Assert(rejected,"invalid catalog accepted "+test);
        }
        var legacy=store.Load(1);legacy.narratives=Decode<CafeNarrativeRow[]>("[{\"version\":1,\"day\":1,\"turn\":1,\"session\":\"old\",\"cat_id\":\"cat-mike\",\"text\":\"旧文章\"}]");
        CafeNarratives.Validate(legacy);n.Restore(legacy.narratives);Assert(n.Read("old").Contains("旧文章"),"legacy receipt not readable");
        legacy.narratives=null;CafeNarratives.Validate(legacy);n.Restore(null);Assert(n.Rows.Count==0,"old save invented text");
        File.WriteAllText(Path.Combine(output,"summary.txt"),assertions+" catalog assertions passed\n"+prose);
        Console.WriteLine(assertions+" catalog assertions passed");
    }
}
