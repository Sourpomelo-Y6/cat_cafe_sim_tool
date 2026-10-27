using CatCafePreview.Standalone;
static partial class Program
{
 static void SpecialLegacySave(string rulesPath,string pack,string output)
 {
  var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));rules.store_events.probability=0;
  var sim=new CafeSimulation(rules);var n=new CafeNarratives(sim);var store=new CafeSaveStore(Path.Combine(output,"saves"),ReplayEncode,ReplayDecode,sim.Validate);
  var save=store.Load(2);CafeNarratives.Validate(save);sim.Restore(save.state);n.Restore(save.narratives);
  string rows=Encode(n.Rows),state=Encode(sim.State),text=n.Read(sim.ActivePlayer.id);
  Assert(text==File.ReadAllText(Path.Combine(output,"result.txt")).Replace("\r\n","\n"),"Actual pre-task old save prose differs");
  n.SetCatalog(Decode<CafeNarrativeCatalog>(File.ReadAllText(Path.Combine(pack,"interaction_texts.json")))) ;n.Read(sim.ActivePlayer.id);
  Assert(rows==Encode(n.Rows) && state==Encode(sim.State),"Old save catalog update/view changed receipts/state");
  foreach(var r in n.Rows)if(!string.IsNullOrEmpty(r.image_resource))Assert(File.Exists(Path.Combine("C:/Users/hi-wa/unity_workspase/CatCafeSim/Assets/Resources",r.image_resource+".png")),"Past image unavailable");
  int count=n.Rows.Count;sim.Execute("player_step",choice:"pause");Assert(n.Rows.Count==count+1 && Encode(n.Rows.Take(count).ToList())==rows,"New action changed past old-save prose");
  Console.WriteLine(assertions+" actual old-save assertions passed (original store-event fixture probability 0)");
 }
 static CafeRules SpecialRules(string path,string scene)
 {
  var rules=Decode<CafeRules>(File.ReadAllText(path));rules.store_events.probability=0;
  var c=rules.exchange;c.direct_gain=scene=="connect"?1:100;c.tension_enthusiastic=scene=="open_up"?0:80;c.tension_favorable=c.tension_neutral=c.tension_enthusiastic;
  c.enthusiastic_threshold=0;c.confused_threshold=0;
  var cat=rules.cats.Single(x=>x.id=="cat-mike");cat.type_preferences=cat.type_preferences.Select(x=>1.0).ToArray();cat.intensity_preferences=cat.intensity_preferences.Select(x=>1.0).ToArray();cat.boredom_decay=0;
  return rules;
 }
 static void SpecialNarrativeChecks(string rulesPath,string pack,string output,bool resume)
 {
  Directory.CreateDirectory(output);
  var source=Decode<CafeNarrativeCatalog>(File.ReadAllText(Path.Combine(pack,"interaction_texts.json")));source.Validate();Assert(source.version==4,"Special catalog v4");
  if(resume)
  {
   foreach(string scene in new[]{"switch","connect","open_up","simultaneous"})
   {
    var sim=new CafeSimulation(SpecialRules(rulesPath,scene));var n=new CafeNarratives(sim);
    var store=new CafeSaveStore(Path.Combine(output,scene),ReplayEncode,ReplayDecode,sim.Validate);var save=store.Load(1);CafeNarratives.Validate(save);sim.Restore(save.state);n.Restore(save.narratives);
    Assert(Encode(n.Rows)==File.ReadAllText(Path.Combine(output,scene+"-rows.json")),"Separate process changed text/id/image "+scene);
    string before=Encode(sim.State);n.Read(sim.ActivePlayer.id);Assert(before==Encode(sim.State),"Resume read consumed resources");
    n.SetCatalog(source);sim.Execute("player_step",choice:"direct");Assert(n.Rows.Count==save.narratives.Length+1,"New action after resumed save");
   }
   Console.WriteLine(assertions+" separate-process special assertions passed");return;
  }
  foreach(string scene in new[]{"switch","connect","open_up","simultaneous"})foreach(string sides in new[]{"both","speech","cat","none"})
  {
   var catalog=Decode<CafeNarrativeCatalog>(Encode(source));catalog.entries=catalog.entries.Where(e=>e.NarrativeScene=="normal" || (e.NarrativeScene==scene && (sides=="both" || sides=="speech" && e.kind=="speech" || sides=="cat" && e.kind=="cat_response"))).ToArray();catalog.revision=catalog.ComputeRevision();catalog.Validate();
   var rules=SpecialRules(rulesPath,scene);var sim=new CafeSimulation(rules);var n=new CafeNarratives(sim);n.SetCatalog(catalog);
   var baseline=new CafeSimulation(Decode<CafeRules>(Encode(rules)));var fallback=new CafeNarratives(baseline);
   foreach(var s in new[]{sim,baseline})
   {
    s.Execute("player_begin",catId:"cat-mike");
    if(scene=="switch"){s.Execute("player_step",choice:"switch",source:"ball");s.Execute("player_step",choice:"switch",source:"teaser");}
    else{s.Execute("player_step",choice:"direct");s.Execute("player_step",choice:scene=="open_up"?"direct":"connect");}
   }
   var row=n.Rows.Last();var old=fallback.Rows.Last();Assert(row.narrative_scene==scene,"Confirmed scene differs "+scene+"/"+row.narrative_scene);
   bool speech=sides=="both" || sides=="speech",cat=sides=="both" || sides=="cat";
   var dedicatedSpeech=catalog.Find("speech","player","teaser",row.action,"any","cat-mike",scene);
   var dedicatedCat=catalog.Find("cat_response","cat-mike","teaser",row.action,"any","cat-mike",scene);
   Assert((dedicatedSpeech!=null)==speech && (dedicatedCat!=null)==cat,"Independent fields");
   if(speech)Assert(row.speech_id==dedicatedSpeech.variants[0].id,"Speech id");
   else if(scene!="open_up")Assert(row.speech==old.speech,"Speech fallback");
   if(cat){Assert(row.cat_text_id==dedicatedCat.variants[0].id,"Cat id");Assert(!row.cat_response.Contains("は心を開いた様子を見せた。"),"Double special suffix");}
   else if(scene!="open_up")Assert(row.cat_response==old.cat_response,"Cat fallback must not concatenate other scenes");
   Assert(CafeReplayJournal.Digest(sim.State)==CafeReplayJournal.Digest(baseline.State),"Narratives altered game result");
   string before=Encode(sim.State),text=row.text;n.Read(sim.ActivePlayer.id);Assert(before==Encode(sim.State) && text==row.text,"Viewing mutated state");
   if(sides=="both")
   {
    var store=new CafeSaveStore(Path.Combine(output,scene),s=>{s.narratives=n.Rows.ToArray();return ReplayEncode(s);},ReplayDecode,sim.Validate);store.Save(1,sim.State);var save=store.Load(1);CafeNarratives.Validate(save);
    n.SetCatalog(null);n.Restore(save.narratives);Assert(n.Rows.Last().text==text,"Pack removal regenerated past text");
    File.WriteAllText(Path.Combine(output,scene+"-rows.json"),Encode(n.Rows));
   }
  }
  foreach(string operation in new[]{"pause","switch"})
  {
   var rules=SpecialRules(rulesPath,"open_up");var sim=new CafeSimulation(rules);var n=new CafeNarratives(sim);
   var noSpecial=Decode<CafeNarrativeCatalog>(Encode(source));noSpecial.entries=noSpecial.entries.Where(e=>e.NarrativeScene=="normal").ToArray();noSpecial.revision=noSpecial.ComputeRevision();n.SetCatalog(noSpecial);
   sim.Execute("player_begin",catId:"cat-mike");sim.Execute("player_step",choice:"direct");sim.Execute("player_step",choice:operation,source:operation=="switch"?"ball":null);
   var row=n.Rows.Last();Assert(row.narrative_scene=="open_up","Pending opening lost on "+operation);
   if(operation=="switch")Assert(row.speech=="今度はこれにしよう。" && row.narration=="次の働きかけまで、少し間を置いた。","Opening on switch changed legacy fixed fallback");
   else Assert(row.narration.StartsWith(noSpecial.rest_narration) && row.cat_response.EndsWith("ミケは心を開いた様子を見せた。"),"Opening on pause lost rest fallback");
  }
  // v1 receipts remain loadable; missing scene never reconstructs past prose.
  var legacy=new CafeSimulation(Decode<CafeRules>(File.ReadAllText(rulesPath)));var oldN=new CafeNarratives(legacy);legacy.Execute("player_begin",catId:"cat-mike");legacy.Execute("player_step",choice:"direct");var oldRow=oldN.Rows.Last();oldRow.narrative_scene=null;
  var legacySave=new CafeSaveEnvelope{state=legacy.State,narratives=oldN.Rows.ToArray()};CafeNarratives.Validate(legacySave);oldN.SetCatalog(source);oldN.Restore(legacySave.narratives);Assert(oldN.Rows.Last().text==oldRow.text,"Old text changed");legacy.Execute("player_step",choice:"direct");Assert(oldN.Rows.Count==2,"Old save new action");
  Console.WriteLine(assertions+" special narrative assertions passed");
 }
}
