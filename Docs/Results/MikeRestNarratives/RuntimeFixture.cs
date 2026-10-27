using System.Text.Json;
using CatCafePreview.Standalone;
var opt=new JsonSerializerOptions{IncludeFields=true};
var path="C:/Users/hi-wa/unity_workspase/CatCafeSim/Assets/Resources/CatCafeContent/Imports/d4594a5f9a7f483ca16ddfca4ea48486/interaction_texts.json";
var catalog=JsonSerializer.Deserialize<CafeNarrativeCatalog>(File.ReadAllText(path),opt);
var rules=JsonSerializer.Deserialize<CafeRules>(File.ReadAllText("../../cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json"),opt); rules.exchange.pause_recovery=1; rules.store_events.probability=0;
var doc=JsonDocument.Parse(File.ReadAllText("../../cat_cafe_sim_tool/Docs/Results/MikeRestNarratives/registration.json")); var data=doc.RootElement.GetProperty("after"); var c=data.GetProperty("condition");
var entry=new NarrativeEntry{kind=c.GetProperty("kind").GetString(),subject_id=c.GetProperty("subject_id").GetString(),mode="pause",action="pause",reaction="listless",scene_key="pause",target_cat="",pool_key="cat_response/cat-mike/pause/pause/listless/pause",narrative_scene="normal",variants=JsonSerializer.Deserialize<NarrativeVariant[]>(data.GetProperty("variants").GetRawText(),opt)};
void Run(bool added){var copy=JsonSerializer.Deserialize<CafeNarrativeCatalog>(JsonSerializer.Serialize(catalog,opt),opt); if(added)copy.entries=copy.entries.Append(entry).ToArray(); copy.revision=copy.ComputeRevision(); var r=JsonSerializer.Deserialize<CafeRules>(JsonSerializer.Serialize(rules,opt),opt); var sim=new CafeSimulation(r); sim.State.cats.Single(x=>x.id=="cat-mike").stamina=10; var n=new CafeNarratives(sim); n.SetCatalog(copy); sim.Execute("player_begin",catId:"cat-mike"); for(int i=0;i<3;i++){sim.Execute("player_step",choice:"pause"); var row=n.Rows.Last(); if(row.reaction!="listless")throw new Exception("reaction"); if(added && (row.cat_text_id!=entry.variants[i].id || row.cat_response!=entry.variants[i].text))throw new Exception("selection"); if(!added && !string.IsNullOrEmpty(row.cat_text_id))throw new Exception("existing selection"); Console.WriteLine($"{added} {row.reaction} {row.cat_text_id ?? "default"} {row.cat_response}"); }}
Run(false); Run(true);

