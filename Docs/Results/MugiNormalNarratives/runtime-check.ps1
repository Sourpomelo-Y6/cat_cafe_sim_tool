$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../../../..").Path
$assembly = Join-Path $root 'Logs/MikeRestRuntime/bin/Debug/net8.0/Test.dll'
[void][Reflection.Assembly]::LoadFrom($assembly)
$options = [System.Text.Json.JsonSerializerOptions]::new()
$options.IncludeFields = $true
$catalogPath = 'C:/Users/hi-wa/unity_workspase/CatCafeSim/Assets/Resources/CatCafeContent/Imports/d4594a5f9a7f483ca16ddfca4ea48486/interaction_texts.json'
$catalog = [System.Text.Json.JsonSerializer]::Deserialize([IO.File]::ReadAllText($catalogPath), [CatCafePreview.Standalone.CafeNarrativeCatalog], $options)
$rulesPath = Join-Path $root 'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json'
$rules = [System.Text.Json.JsonSerializer]::Deserialize([IO.File]::ReadAllText($rulesPath), [CatCafePreview.Standalone.CafeRules], $options)
$data = Get-Content -Raw -Encoding utf8 "$PSScriptRoot/registration.json" | ConvertFrom-Json
$sim = [CatCafePreview.Standalone.CafeSimulation]::new($rules)
$step = [CatCafePreview.Standalone.CafeSimulation].GetMethod('StepExchange', [Reflection.BindingFlags]'Instance,Public,NonPublic')
$reach = Get-Content -Raw -Encoding utf8 "$PSScriptRoot/reachability.json" | ConvertFrom-Json
foreach ($item in $reach.witnesses.PSObject.Properties) {
  $exchange = [CatCafePreview.Standalone.CafeExchange]::new()
  $exchange.cat_id='cat-mugi'; $exchange.customer_id='player'; $exchange.remaining=20; $exchange.stamina=$item.Value.initial_stamina
  foreach ($a in $item.Value.actions) { [void]$step.Invoke($sim,@($exchange,$sim.Definition('cat-mugi'),$a.action,$a.target)) }
  $expectedReaction=$item.Name.Split('/')[1]
  if ($exchange.previous_reaction -ne $expectedReaction -or $exchange.open_up_count -ne 0) { throw "reachability $($item.Name)" }
  Write-Output "reachability $($item.Name) reaction=$($exchange.previous_reaction) stamina=$($exchange.stamina)"
}
foreach ($item in $reach.after_rest.PSObject.Properties) {
  $exchange = [CatCafePreview.Standalone.CafeExchange]::new()
  $exchange.cat_id='cat-mugi'; $exchange.customer_id='player'; $exchange.remaining=20; $exchange.stamina=$item.Value.initial_stamina
  foreach ($a in $item.Value.actions) { [void]$step.Invoke($sim,@($exchange,$sim.Definition('cat-mugi'),$a.action,$a.target)) }
  $last=$item.Value.records[-1]
  if ($exchange.previous_reaction -ne $last.reaction -or [Math]::Abs($exchange.stamina-$last.after.stamina) -gt 0.00001) {throw "after rest $($item.Name)"}
  Write-Output "after-rest $($item.Name) reaction=$($exchange.previous_reaction) stamina=$($exchange.stamina)"
}
$narratives = [CatCafePreview.Standalone.CafeNarratives]::new($sim)
$apply = [CatCafePreview.Standalone.CafeNarratives].GetMethod('ApplyCatalog', [Reflection.BindingFlags]'Instance,NonPublic')

foreach ($mode in @('teaser','pet','ball','plush','tunnel','brush','voice','presence','pause')) {
  $actions = if ($mode -eq 'pause') { @('pause') } elseif ($mode -in @('teaser','ball','tunnel')) { @('direct','adapt','intense','feint') } elseif ($mode -eq 'plush') { @('direct','adapt','feint') } else { @('direct','adapt') }
  $reactions = if ($mode -eq 'pause') { @('neutral','listless') } else { @('neutral','favorable','enthusiastic','turn_away','confused','listless') }
  foreach ($reaction in $reactions) {
    foreach ($action in $actions) {
      if ($null -ne $catalog.Find('cat_response','cat-mugi',$mode,$action,$reaction,'cat-mugi','normal')) { throw "existing catalog condition $mode/$action/$reaction" }
    }
    $key="$mode/$reaction"; $raw=$data.entries.$key
    if ($null -eq $raw) { continue }
    $entry=[CatCafePreview.Standalone.NarrativeEntry]::new()
    $entry.kind='cat_response'; $entry.subject_id=$raw.condition.subject_id; $entry.mode=$mode; $entry.action=$(if ($mode -eq 'pause') {'pause'} else {'any'}); $entry.reaction=$reaction; $entry.scene_key=$mode; $entry.target_cat=''; $entry.narrative_scene='normal'
    $entry.pool_key="cat_response/$($entry.subject_id)/$mode/$($entry.action)/$reaction/$mode"
    $entry.variants=@($raw.variants | Where-Object text | ForEach-Object { $v=[CatCafePreview.Standalone.NarrativeVariant]::new(); $v.id=$_.id; $v.text=$_.text; $v.slot=$_.slot; $v })
    $catalog.entries += $entry
  }
}
$catalog.revision=$catalog.ComputeRevision(); $narratives.SetCatalog($catalog)
foreach ($mode in @('teaser','pet','ball','plush','tunnel','brush','voice','presence','pause')) {
 $actions = if ($mode -eq 'pause') { @('pause') } elseif ($mode -in @('teaser','ball','tunnel')) { @('direct','adapt','intense','feint') } elseif ($mode -eq 'plush') { @('direct','adapt','feint') } else { @('direct','adapt') }
 if ($mode -ne 'pause') {
 $key = if ($mode -eq 'presence') { 'confused' } else { 'neutral' }
 $common=$catalog.Find('cat_response','cat-mugi',$mode,'direct',$key,'cat-mugi','normal')
 $specific=[CatCafePreview.Standalone.NarrativeEntry]::new()
 $specific.kind=$common.kind; $specific.subject_id=$common.subject_id; $specific.mode=$mode; $specific.action='direct'; $specific.reaction=$key; $specific.scene_key="${mode}__direct"; $specific.target_cat=''; $specific.narrative_scene='normal'; $specific.pool_key='fixture'; $specific.variants=$common.variants
 $catalog.entries += $specific
 if ($catalog.Find('cat_response','cat-mugi',$mode,'direct',$key,'cat-mugi','normal') -ne $specific -or $catalog.Find('cat_response','cat-mugi',$mode,'adapt',$key,'cat-mugi','normal') -ne $common) { throw 'precedence failed' }
 $catalog.entries=@($catalog.entries | Where-Object { $_ -ne $specific })
 }
 $reactions=if ($mode -eq 'pause') { @('neutral','listless') } else { @('neutral','favorable','enthusiastic','turn_away','confused','listless') }
 foreach ($reaction in $reactions) {
  foreach ($action in $actions) {
   $narratives.Rows.Clear(); $key="$mode/$reaction"; $raw=$data.entries.$key
   for ($i=0; $i -lt 2; $i++) {
    $row=[CatCafePreview.Standalone.CafeNarratives]::Compose('プレイヤー',$mode,$action,$reaction,50,45,$false,$false,'ムギ')
    $fallback=$row.cat_response
    $row.cat_id='cat-mugi'; $row.narrative_scene='normal'
    [void]$apply.Invoke($narratives,@($row,'player','プレイヤー',50.0,45.0,$false))
    if ($null -eq $raw) { if ($row.cat_text_id -or $row.cat_pool -or $row.cat_response -ne $fallback) { throw "fallback $key" } }
    else { $expected=$raw.variants[$i]; if ($row.cat_text_id -ne $expected.id -or $row.cat_response -ne $expected.text.Replace('{cat}','ムギ') -or $row.cat_pool -ne "cat_response/$($raw.condition.subject_id)/$mode/$($raw.condition.action)/$reaction/$mode") { throw "selection $key/$action/$i" } }
    $narratives.Rows.Add($row); Write-Output "$key/$action/$i id=$($row.cat_text_id) pool=$($row.cat_pool) text=$($row.cat_response)"
   }
  }
 }
}
Write-Output "PASS 35 reachability traces, eight after-rest traces, 280 selection/fallback checks, eight precedence checks; assembly SHA256 $((Get-FileHash $assembly -Algorithm SHA256).Hash)"
