$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../../../..").Path
# Reuse the existing C# fixture assembly; no game build, export or Unity import.
$assembly = Join-Path $root 'Logs/MikeRestRuntime/bin/Debug/net8.0/Test.dll'
[void][Reflection.Assembly]::LoadFrom($assembly)
$options = [System.Text.Json.JsonSerializerOptions]::new()
$options.IncludeFields = $true
$catalogPath = 'C:/Users/hi-wa/unity_workspase/CatCafeSim/Assets/Resources/CatCafeContent/Imports/d4594a5f9a7f483ca16ddfca4ea48486/interaction_texts.json'
$catalog = [System.Text.Json.JsonSerializer]::Deserialize([IO.File]::ReadAllText($catalogPath), [CatCafePreview.Standalone.CafeNarrativeCatalog], $options)
$rulesPath = Join-Path $root 'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json'
$rules = [System.Text.Json.JsonSerializer]::Deserialize([IO.File]::ReadAllText($rulesPath), [CatCafePreview.Standalone.CafeRules], $options)
$recordPath = if (Test-Path "$PSScriptRoot/registration.json") { "$PSScriptRoot/registration.json" } else { "$PSScriptRoot/draft-runtime-input.json" }
$data = Get-Content -Raw -Encoding utf8 $recordPath | ConvertFrom-Json
Write-Output "Input record: $recordPath (in-memory only)"
$original = @($catalog.entries)
$added = @($data.entries | ForEach-Object {
 $raw=$_; $entry=[CatCafePreview.Standalone.NarrativeEntry]::new()
 foreach ($field in @('kind','subject_id','mode','action','reaction','scene_key','target_cat','narrative_scene')) { $entry.$field=$raw.condition.$field }
 $entry.pool_key=$data.condition_keys[[array]::IndexOf($data.entries,$raw)]
 $entry.variants=@($raw.variants | Where-Object text | ForEach-Object { $v=[CatCafePreview.Standalone.NarrativeVariant]::new(); $v.id=$_.id; $v.text=$_.text; $v.slot=$_.slot; $v })
 $entry
})
$sim=[CatCafePreview.Standalone.CafeSimulation]::new($rules)
$narratives=[CatCafePreview.Standalone.CafeNarratives]::new($sim)
$apply=[CatCafePreview.Standalone.CafeNarratives].GetMethod('ApplyCatalog',[Reflection.BindingFlags]'Instance,NonPublic')
$checks=0
foreach ($scene in @('switch','connect','open_up','simultaneous')) {
 $actions=if ($scene -eq 'open_up') { @('direct','adapt','pause','switch') } elseif ($scene -eq 'switch') { @('switch') } else { @('connect') }
 foreach ($action in $actions) {
  foreach ($keep in @('both','speech','cat_response','none')) {
   $catalog.entries=$original+@($added | Where-Object { $keep -eq 'both' -or $_.kind -eq $keep })
   $catalog.revision=$catalog.ComputeRevision(); $narratives.SetCatalog($catalog); $narratives.Rows.Clear()
   for ($slot=0; $slot -lt 2; $slot++) {
    $opened=$scene -in @('open_up','simultaneous')
    $row=[CatCafePreview.Standalone.CafeNarratives]::Compose('プレイヤー','brush',$action,'neutral',50,45,$opened,($action -eq 'connect'),'ミケ')
    $row.cat_id='cat-mike'; $row.customer_id='player'; $row.narrative_scene=$scene
    [void]$apply.Invoke($narratives,@($row,'player','プレイヤー',50.0,45.0,$opened))
    foreach ($kind in @('speech','cat_response')) {
     $expected=$added | Where-Object { $_.kind -eq $kind -and $_.narrative_scene -eq $scene }
     $id=if ($kind -eq 'speech') {$row.speech_id} else {$row.cat_text_id}
     $text=if ($kind -eq 'speech') {$row.speech} else {$row.cat_response}
     if ($keep -eq 'both' -or $keep -eq $kind) {
      if ($id -ne $expected.variants[$slot].id -or $text -ne $expected.variants[$slot].text.Replace('{cat_call}','ミケ').Replace('{cat}','ミケ')) {throw "selection $scene/$action/$keep/$slot/$kind"}
     } elseif ($id -in $expected.variants.id -or [string]::IsNullOrWhiteSpace($text)) {throw "fallback $scene/$action/$keep/$kind"}
    }
    if ($row.text.Contains('{')) {throw 'unexpanded placeholder'}
    if ($opened -and $keep -in @('both','cat_response') -and $row.cat_response.Contains('心を開いた様子')) {throw 'duplicate appendix'}
    if ($opened -and $keep -in @('speech','none') -and -not $row.cat_response.Contains('心を開いた様子')) {throw 'missing appendix'}
    $narratives.Rows.Add($row); $checks++
    Write-Output "$scene/$action/$keep/$slot speech=$($row.speech_id) cat=$($row.cat_text_id) text=$($row.text)"
   }
  }
 }
}
Write-Output "PASS $checks C# composed-row selection/fallback checks; assembly SHA256 $((Get-FileHash $assembly -Algorithm SHA256).Hash). In-memory fixture only; no StepExchange, Unity screen or distribution verification."
