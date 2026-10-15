using CatCafePreview.Standalone;
static partial class Program
{
    static void SubjectCatalogChecks(string rulesPath,string pack,string output)
    {
        Directory.CreateDirectory(output);
        var catalog=Decode<CafeNarrativeCatalog>(File.ReadAllText(Path.Combine(pack,"interaction_texts.json")));
        catalog.Validate();Assert(catalog.version==2,"v2 fixture required");
        Assert(catalog.Find("speech","player","teaser","intense","any","cat-mike").target_cat=="cat-mike","legacy speech priority lost");
        Assert(catalog.Find("speech","player","teaser","intense","any","cat-tama").target_cat=="*","Mike speech leaked to Tama");
        Assert(catalog.Find("speech","guest-2","pet","direct","any","cat-tama").subject_id=="author-suzuki","explicit person mapping unused");
        var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));rules.store_events.probability=0;
        CafeSimulation New()=>new CafeSimulation(Decode<CafeRules>(Encode(rules)));
        var sim=New();var rows=new CafeNarratives(sim);rows.SetCatalog(catalog);
        sim.Execute("player_begin",catId:"cat-tama");
        for(int i=0;i<3;i++)sim.Execute("player_step",choice:"pause");
        Assert(rows.Rows.Count==3 && rows.Rows.Select(r=>r.speech_id).Distinct().Count()==3,"new subject candidates do not rotate");
        Assert(rows.Rows.All(r=>r.text.Contains("タマ") && !r.text.Contains("ミケ") && !r.text.Contains("{cat")),"wrong cat name/placeholder");
        Assert(rows.Rows.All(r=>r.cat_pool.Contains("author-tama") && !string.IsNullOrEmpty(r.image_resource)),"wrong cat pool/image");
        string session=sim.ActivePlayer.id,text=rows.Read(session),state=Encode(sim.State);
        for(int i=0;i<20;i++)Assert(rows.Read(session)==text,"reading changed receipt");
        Assert(state==Encode(sim.State),"reading consumed gameplay resources");
        var saves=new CafeSaveStore(output,s=>{s.narratives=rows.Rows.ToArray();return ReplayEncode(s);},ReplayDecode,sim.Validate);
        saves.Save(1,sim.State);var saved=saves.Load(1);CafeNarratives.Validate(saved);sim.Restore(saved.state);rows.Restore(saved.narratives);
        Assert(rows.Read(session)==text,"reload changed finished prose");
        sim.Execute("player_step",choice:"pause");Assert(rows.Rows.Last().speech_id==rows.Rows[0].speech_id,"saved rotation lost");
        var changed=Decode<CafeNarrativeCatalog>(Encode(catalog));changed.cat_bindings=changed.cat_bindings.Where(c=>c.cat_id!="cat-tama").ToArray();
        changed.entries=changed.entries.Where(e=>e.subject_id!="author-tama" && e.target_cat!="author-tama").ToArray();
        changed.image_bindings=changed.image_bindings.Where(i=>i.cat_id!="cat-tama").ToArray();changed.revision=changed.ComputeRevision();
        rows.SetCatalog(changed);CafeNarratives.Validate(saved);Assert(rows.Read(session).Contains(saved.narratives[0].text),"disabled subject altered history");
        foreach(string reaction in new[]{"neutral","favorable","enthusiastic","turn_away","listless"})
        {
            var r=Decode<CafeRules>(Encode(rules));var cat=r.cats.Single(c=>c.id=="cat-tama");
            cat.type_preferences[0]=reaction=="turn_away"?0:reaction=="neutral"?.6:reaction=="enthusiastic"?2:1;
            var s=new CafeSimulation(r);if(reaction=="listless")s.State.cats.Single(c=>c.id=="cat-tama").stamina=10;
            var n=new CafeNarratives(s);n.SetCatalog(catalog);s.Execute("player_begin",catId:"cat-tama");s.Execute("player_step",choice:"direct");
            var row=n.Rows.Single();Assert(row.cat_pool==catalog.Find("cat_response","cat-tama","teaser","direct",row.reaction,"cat-tama").Key(),"actual reaction mismatch");
            Assert(row.reaction==reaction,"reaction boundary fixture failed: "+reaction+" / "+row.reaction);
        }
        foreach(var pattern in rules.patterns)pattern.days=new[]{0,1,2,3,4,5,6};
        rules.arrival_ticks=new[]{0,25};
        var service=New();var visits=new CafeNarratives(service);visits.CustomerName=id=>id;visits.SetCatalog(catalog);
        service.Execute("set_shifts",working:new[]{"cat-tama"});service.Execute("start_business");
        while(service.State.phase=="open")service.Execute("advance_business");
        File.WriteAllText(Path.Combine(output,"service.json"),Encode(visits.Rows));
        Assert(visits.Rows.Any(r=>r.customer_id=="guest-2" && r.speech.Contains("タマちゃん")),"new guest speech unused");
        foreach(var visit in visits.Rows.GroupBy(r=>r.session))Assert(visit.Select(r=>r.customer_id+"/"+r.cat_id).Distinct().Count()==1,"customer/cat histories mixed");
        var baseline=New();var decorated=New();var observer=new CafeNarratives(decorated);observer.SetCatalog(catalog);
        foreach(var s in new[]{baseline,decorated})s.Execute("player_begin",catId:"cat-tama");
        for(int i=0;i<10;i++){baseline.Execute("player_step",choice:"pause");decorated.Execute("player_step",choice:"pause");Assert(CafeReplayJournal.Digest(baseline.State)==CafeReplayJournal.Digest(decorated.State),"catalog changed RNG/state");}
        File.WriteAllText(Path.Combine(output,"summary.txt"),assertions+" multi-subject assertions passed\n"+text);
        Console.WriteLine(assertions+" multi-subject assertions passed");
    }
}
