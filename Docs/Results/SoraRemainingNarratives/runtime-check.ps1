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
$narratives = [CatCafePreview.Standalone.CafeNarratives]::new($sim)
$apply = [CatCafePreview.Standalone.CafeNarratives].GetMethod('ApplyCatalog', [Reflection.BindingFlags]'Instance,NonPublic')

foreach ($mode in @('tunnel','brush','voice','presence')) {
  $actions = if ($mode -eq 'tunnel') { @('direct','adapt','intense','feint') } else { @('direct','adapt') }
  $reactions = @('neutral','favorable','enthusiastic','turn_away','confused','listless')
  foreach ($reaction in $reactions) {
    foreach ($action in $actions) {
      if ($null -ne $catalog.Find('cat_response','cat-sora',$mode,$action,$reaction,'cat-sora','normal')) { throw "existing catalog condition $mode/$action/$reaction" }
    }
    $key="$mode/$reaction"; $raw=$data.entries.$key
    if ($null -eq $raw) { continue }
    $entry=[CatCafePreview.Standalone.NarrativeEntry]::new()
    $entry.kind='cat_response'; $entry.subject_id=$raw.condition.subject_id; $entry.mode=$mode; $entry.action='any'; $entry.reaction=$reaction; $entry.scene_key=$mode; $entry.target_cat=''; $entry.narrative_scene='normal'
    $entry.pool_key="cat_response/$($entry.subject_id)/$mode/any/$reaction/$mode"
    $entry.variants=@($raw.variants | Where-Object text | ForEach-Object { $v=[CatCafePreview.Standalone.NarrativeVariant]::new(); $v.id=$_.id; $v.text=$_.text; $v.slot=$_.slot; $v })
    $catalog.entries += $entry
  }
}
$catalog.revision=$catalog.ComputeRevision(); $narratives.SetCatalog($catalog)
foreach ($mode in @('tunnel','brush','voice','presence')) {
 $actions = if ($mode -eq 'tunnel') { @('direct','adapt','intense','feint') } else { @('direct','adapt') }
 $key = if ($mode -eq 'brush') { 'turn_away' } else { 'neutral' }
 $common=$catalog.Find('cat_response','cat-sora',$mode,'direct',$key,'cat-sora','normal')
 $specific=[CatCafePreview.Standalone.NarrativeEntry]::new()
 $specific.kind=$common.kind; $specific.subject_id=$common.subject_id; $specific.mode=$mode; $specific.action='direct'; $specific.reaction=$key; $specific.scene_key="${mode}__direct"; $specific.target_cat=''; $specific.narrative_scene='normal'; $specific.pool_key='fixture'; $specific.variants=$common.variants
 $catalog.entries += $specific
 if ($catalog.Find('cat_response','cat-sora',$mode,'direct',$key,'cat-sora','normal') -ne $specific -or $catalog.Find('cat_response','cat-sora',$mode,'adapt',$key,'cat-sora','normal') -ne $common) { throw 'precedence failed' }
 $catalog.entries=@($catalog.entries | Where-Object { $_ -ne $specific })
 foreach ($reaction in @('neutral','favorable','enthusiastic','turn_away','confused','listless')) {
  foreach ($action in $actions) {
   $narratives.Rows.Clear(); $key="$mode/$reaction"; $raw=$data.entries.$key
   for ($i=0; $i -lt 2; $i++) {
    $row=[CatCafePreview.Standalone.CafeNarratives]::Compose('プレイヤー',$mode,$action,$reaction,50,45,$false,$false,'ソラ')
    $row.cat_id='cat-sora'; $row.narrative_scene='normal'
    [void]$apply.Invoke($narratives,@($row,'player','プレイヤー',50.0,45.0,$false))
    if ($null -eq $raw) { if ($row.cat_text_id) { throw "fallback $key" } }
    else { $expected=$raw.variants[$i]; if ($row.cat_text_id -ne $expected.id -or $row.cat_response -ne $expected.text.Replace('{cat}','ソラ') -or $row.cat_pool -ne "cat_response/$($raw.condition.subject_id)/$mode/any/$reaction/$mode") { throw "selection $key/$action/$i" } }
    $narratives.Rows.Add($row); Write-Output "$key/$action/$i id=$($row.cat_text_id) pool=$($row.cat_pool) text=$($row.cat_response)"
   }
  }
 }
}
Write-Output "PASS 120 selection/fallback checks, four precedence checks; assembly SHA256 $((Get-FileHash $assembly -Algorithm SHA256).Hash)"
