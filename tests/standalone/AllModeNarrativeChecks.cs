using CatCafePreview.Standalone;
static partial class Program
{
    static void MikeImageChecks(string rulesPath,string pack,string output)
    {
        Directory.CreateDirectory(output);
        var catalog=Decode<CafeNarrativeCatalog>(File.ReadAllText(Path.Combine(pack,"interaction_texts.json")));catalog.Validate();
        var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));rules.store_events.probability=0;
        foreach(string mode in rules.types.Select(t=>t.id))
        {
            var prep=catalog.image_bindings.Single(i=>i.cat_id=="cat-mike" && i.scene_key==mode);
            foreach(string action in new[]{"direct","adapt"})
            {
                var execution=catalog.image_bindings.Single(i=>i.cat_id=="cat-mike" && i.scene_key==mode+"__"+action);
                Assert(execution.image!=prep.image,"Preparation/execution not distinct: "+mode);
                Assert(File.Exists(Path.Combine(pack,execution.image)),"Execution PNG missing");
                var positive=Decode<CafeRules>(Encode(rules));int modeIndex=Array.FindIndex(positive.types,t=>t.id==mode);
                positive.cats.Single(c=>c.id=="cat-mike").type_preferences[modeIndex]=12/(positive.exchange.direct_gain*positive.types[modeIndex].gain);
                var sim=new CafeSimulation(positive);var n=new CafeNarratives(sim);n.SetCatalog(catalog);
                sim.Execute("player_begin",catId:"cat-mike");if(mode!="teaser")sim.Execute("player_step",choice:"switch",source:mode);
                sim.Execute("player_step",choice:action);var row=n.Rows.Last();
                Assert(row.image_scene_key==mode+"__"+action && row.image_resource.EndsWith(execution.image[..^4]),"Actual action did not select execution: "+mode+"/"+row.reaction);
                string before=CafeReplayJournal.Digest(sim.State);n.Read(sim.ActivePlayer.id);
                Assert(before==CafeReplayJournal.Digest(sim.State),"Reading consumed gameplay");
                var store=new CafeSaveStore(Path.Combine(output,mode+action),s=>{s.narratives=n.Rows.ToArray();return ReplayEncode(s);},ReplayDecode,sim.Validate);
                store.Save(1,sim.State);var save=store.Load(1);CafeNarratives.Validate(save);n.Restore(save.narratives);
                Assert(n.Rows.Last().image_resource==row.image_resource,"Reload changed saved image");
            }
            foreach(string reaction in new[]{"turn_away","confused","listless"})
            {
                var r=Decode<CafeRules>(Encode(rules));var cat=r.cats.Single(c=>c.id=="cat-mike");int index=Array.FindIndex(r.types,t=>t.id==mode);
                if(reaction=="turn_away")cat.type_preferences[index]=0;
                if(reaction=="confused")cat.type_preferences[index]=2/(r.exchange.direct_gain*r.types[index].gain);
                var sim=new CafeSimulation(r);if(reaction=="listless")sim.State.cats.Single(c=>c.id=="cat-mike").stamina=10;
                var n=new CafeNarratives(sim);n.SetCatalog(catalog);sim.Execute("player_begin",catId:"cat-mike");
                if(mode!="teaser")sim.Execute("player_step",choice:"switch",source:mode);sim.Execute("player_step",choice:"direct");
                var row=n.Rows.Last();Assert(row.reaction==reaction,"Fixture reaction differs: "+mode+"/"+reaction+"/"+row.reaction);
                Assert(row.image_scene_key==mode && row.image_resource.EndsWith(prep.image[..^4]),"Refusal/confusion/fatigue used execution image");
            }
        }
        File.WriteAllText(Path.Combine(output,"result.txt"),assertions+" assertions passed: Mike eight-mode preparation/execution, negative reactions, saved receipts, read-only display.");
        Console.WriteLine(assertions+" Mike image assertions passed");
    }
    static void ValidateNarrativeCatalog(string path)
    {
        var c=Decode<CafeNarrativeCatalog>(File.ReadAllText(path));c.Validate();
        Console.WriteLine("PASS catalog version "+c.version+", "+c.entries.Length+" entries, "+c.pack_id);
    }
    static void AllModeNarrativeChecks(string rulesPath,string pack,string output)
    {
        Directory.CreateDirectory(output);
        var catalog=Decode<CafeNarrativeCatalog>(File.ReadAllText(Path.Combine(pack,"interaction_texts.json")));
        catalog.Validate();Assert(catalog.version==3,"All-mode catalog must be v3");
        var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));rules.store_events.probability=0;
        CafeRules Copy()=>Decode<CafeRules>(Encode(rules));
        var receipts=new List<CafeNarrativeRow>();
        foreach(var type in rules.types)
        {
            Assert(CafeNarrativeCatalog.Actions(type.id).Order().SequenceEqual(type.actions.Concat(new[]{"any"}).Order()),"Catalog actions differ from simulation: "+type.id);
            foreach(string catId in new[]{"cat-mike","cat-tama"})foreach(string action in type.actions)
            {
                var sim=new CafeSimulation(Copy());var n=new CafeNarratives(sim);n.SetCatalog(catalog);
                sim.Execute("player_begin",catId:catId);if(type.id!="teaser")sim.Execute("player_step",choice:"switch",source:type.id);
                sim.Execute("player_step",choice:action);var row=n.Rows.Last();receipts.Add(row);
                Assert(row.mode==type.id && row.action==action,"Actual operation lost");
                Assert(row.cat_pool==catalog.Find("cat_response",catId,type.id,action,row.reaction,catId).Key(),"Wrong actual reaction pool");
                Assert(row.speech_pool==catalog.Find("speech","player",type.id,action,"any",catId).Key(),"Wrong speech operation");
                string name=sim.Definition(catId).name;
                Assert(row.text.Contains(name) && !row.text.Contains("{cat"),"Name expansion failed");
                Assert(row.human_action==catalog.action_texts.Single(a=>a.mode==type.id && a.action==action).text.Replace("{human}","プレイヤー").Replace("{cat}",name),"Action prose differs from actual movement");
                string selectedScene=catId=="cat-mike" && (action=="direct" || action=="adapt") && new[]{"turn_away","confused","listless"}.Contains(row.reaction)?type.id:CafeNarrativeCatalog.Scene(type.id,action);
                var image=catalog.image_bindings.Single(i=>i.cat_id==catId && i.scene_key==selectedScene);
                Assert(row.image_resource.EndsWith(image.image[..^4]) && row.image_scene_key==image.resolved_scene_key,"Wrong cat/mode image");
                string text=n.Read(sim.ActivePlayer.id),state=CafeReplayJournal.Digest(sim.State);
                for(int i=0;i<3;i++)Assert(n.Read(sim.ActivePlayer.id)==text,"Reading changed new-mode text");
                Assert(state==CafeReplayJournal.Digest(sim.State),"Reading consumed gameplay");
            }
            foreach(string reaction in CafeNarrativeCatalog.Reactions)
            {
                var r=Copy();var cat=r.cats.Single(c=>c.id=="cat-tama");cat.intensity_preferences=new[]{.8,1,1.3};
                double score=reaction=="confused"?2:reaction=="neutral"?7.2:reaction=="favorable"?12:24;
                int index=Array.FindIndex(r.types,t=>t.id==type.id);
                cat.type_preferences[index]=reaction=="turn_away"?0:score/(r.exchange.direct_gain*r.types[index].gain);
                var sim=new CafeSimulation(r);if(reaction=="listless")sim.State.cats.Single(c=>c.id=="cat-tama").stamina=10;
                var n=new CafeNarratives(sim);n.SetCatalog(catalog);sim.Execute("player_begin",catId:"cat-tama");
                if(type.id!="teaser")sim.Execute("player_step",choice:"switch",source:type.id);
                sim.Execute("player_step",choice:"direct");var row=n.Rows.Last();
                Assert(row.reaction==reaction,"Boundary fixture failed: "+type.id+" "+reaction+" / "+row.reaction);
                Assert(row.cat_pool==catalog.Find("cat_response","cat-tama",type.id,"direct",reaction,"cat-tama").Key(),"Boundary used wrong text");
            }
            foreach(string invalid in new[]{"intense","feint"}.Except(type.actions))
            {
                var sim=new CafeSimulation(Copy());var n=new CafeNarratives(sim);n.SetCatalog(catalog);sim.Execute("player_begin",catId:"cat-tama");sim.Execute("player_step",choice:"switch",source:type.id);
                string before=CafeReplayJournal.Digest(sim.State);int count=n.Rows.Count;bool rejected=false;
                try{sim.Execute("player_step",choice:invalid);}catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}
                Assert(rejected && count==n.Rows.Count && before==CafeReplayJournal.Digest(sim.State),"Invalid operation consumed resources: "+type.id+"/"+invalid);
            }
            // Customer service uses the same observer after an actual StepExchange.
            // Force only the fixture's current mode so automatic customer policy cannot hide a mode.
            var service=new CafeSimulation(Copy());var serviceRows=new CafeNarratives(service);serviceRows.SetCatalog(catalog);
            service.Execute("set_shifts",working:new[]{"cat-tama"});service.Execute("start_business");
            while(serviceRows.Rows.Count==0)service.Execute("advance_business");
            var e=service.State.exchanges.Single();e.mode=type.id;e.previous_reaction="neutral";e.streak=0;e.last_group=null;
            service.Execute("advance_business");var last=serviceRows.Rows.Last();
            Assert(last.mode==type.id && last.customer_id=="guest-1" && !string.IsNullOrEmpty(last.speech_id) && !string.IsNullOrEmpty(last.cat_text_id),"Customer service lacks authored mode: "+type.id);
        }
        foreach(string mode in new[]{"ball","plush","tunnel","brush","voice","presence"})
        {
            var r=Copy();var cat=r.cats.Single(c=>c.id=="cat-tama");cat.boredom_decay=0;
            var sim=new CafeSimulation(r);var n=new CafeNarratives(sim);n.SetCatalog(catalog);sim.Execute("player_begin",catId:"cat-tama");sim.Execute("player_step",choice:"switch",source:mode);
            for(int i=0;i<3;i++)sim.Execute("player_step",choice:"direct");
            var rows=n.Rows.Where(row=>row.action=="direct").ToArray();
            Assert(rows.Select(row=>row.speech_id).Distinct().Count()==3 && rows.Select(row=>row.cat_text_id).Distinct().Count()==3,"Three new-mode candidates did not rotate: "+mode);
            string session=sim.ActivePlayer.id,prose=n.Read(session);
            var saves=new CafeSaveStore(Path.Combine(output,mode),s=>{s.narratives=n.Rows.ToArray();return ReplayEncode(s);},ReplayDecode,sim.Validate);
            saves.Save(1,sim.State);var saved=saves.Load(1);CafeNarratives.Validate(saved);sim.Restore(saved.state);n.Restore(saved.narratives);
            Assert(n.Read(session)==prose,"Save/load changed new-mode prose");
            sim.Execute("player_step",choice:"direct");Assert(n.Rows.Last().speech_id==rows[0].speech_id,"New-mode rotation did not resume");
        }
        var baseline=new CafeSimulation(Copy());var decorated=new CafeSimulation(Copy());var observer=new CafeNarratives(decorated);observer.SetCatalog(catalog);
        foreach(var sim in new[]{baseline,decorated})sim.Execute("player_begin",catId:"cat-tama");
        foreach(var type in rules.types)
        {
            if(type.id!="teaser")foreach(var sim in new[]{baseline,decorated})sim.Execute("player_step",choice:"switch",source:type.id);
            foreach(var sim in new[]{baseline,decorated})sim.Execute("player_step",choice:"direct");
            Assert(CafeReplayJournal.Digest(baseline.State)==CafeReplayJournal.Digest(decorated.State),"Narrative mode changed RNG/rules");
        }
        var bad=Decode<CafeNarrativeCatalog>(Encode(catalog));bad.image_bindings=bad.image_bindings.Skip(1).ToArray();bad.revision=bad.ComputeRevision();bool denied=false;
        try{bad.Validate();}catch(ArgumentException){denied=true;}Assert(denied,"Incomplete all-mode image map accepted");
        File.WriteAllText(Path.Combine(output,"receipts.json"),Encode(receipts));
        File.WriteAllText(Path.Combine(output,"summary.txt"),assertions+" all-mode narrative assertions passed\nModes: "+string.Join(",",rules.types.Select(t=>t.id)));
        Console.WriteLine(assertions+" all-mode narrative assertions passed");
    }
}
