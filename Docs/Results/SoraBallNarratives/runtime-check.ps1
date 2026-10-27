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
$prior = @{}
foreach ($reaction in @('neutral','favorable','confused','listless','enthusiastic','turn_away')) {
    $prior[$reaction] = $catalog.Find('cat_response','cat-sora','ball','direct',$reaction,'cat-sora','normal')
    if ($null -ne $prior[$reaction]) { throw "Existing direct or common $reaction" }
}
foreach ($reaction in @('neutral','favorable','confused','listless')) {
    $raw = $data.entries.$reaction
    $entry = [CatCafePreview.Standalone.NarrativeEntry]::new()
    $entry.kind='cat_response'; $entry.subject_id=$raw.condition.subject_id; $entry.mode='ball'; $entry.action='any'; $entry.reaction=$reaction; $entry.scene_key='ball'; $entry.target_cat=''; $entry.narrative_scene='normal'
    $entry.pool_key="cat_response/$($entry.subject_id)/ball/any/$reaction/ball"
    $entry.variants = @($raw.variants | Where-Object text | ForEach-Object { $v=[CatCafePreview.Standalone.NarrativeVariant]::new(); $v.id=$_.id; $v.text=$_.text; $v.slot=$_.slot; $v })
    $catalog.entries += $entry
}
$catalog.revision = $catalog.ComputeRevision()
$narratives.SetCatalog($catalog)
$common = $catalog.Find('cat_response','cat-sora','ball','direct','neutral','cat-sora','normal')
$specific = [CatCafePreview.Standalone.NarrativeEntry]::new()
$specific.kind=$common.kind; $specific.subject_id=$common.subject_id; $specific.mode='ball'; $specific.action='direct'; $specific.reaction='neutral'; $specific.scene_key='ball__direct'; $specific.target_cat=''; $specific.narrative_scene='normal'; $specific.pool_key='fixture'; $specific.variants=$common.variants
$catalog.entries += $specific
if ($catalog.Find('cat_response','cat-sora','ball','direct','neutral','cat-sora','normal') -ne $specific -or $catalog.Find('cat_response','cat-sora','ball','adapt','neutral','cat-sora','normal') -ne $common) { throw 'Specific operation precedence failed' }
$catalog.entries = @($catalog.entries | Where-Object { $_ -ne $specific })
foreach ($reaction in @('neutral','favorable','confused','listless','enthusiastic','turn_away')) {
    foreach ($action in @('direct','adapt','intense','feint')) {
        $narratives.Rows.Clear()
        for ($i=0; $i -lt 2; $i++) {
            $row=[CatCafePreview.Standalone.CafeNarratives]::Compose('プレイヤー','ball',$action,$reaction,50,45,$false,$false,'ソラ')
            $row.cat_id='cat-sora'; $row.narrative_scene='normal'
            [void]$apply.Invoke($narratives,@($row,'player','プレイヤー',50.0,45.0,$false))
            if ($reaction -in @('enthusiastic','turn_away')) { if ($row.cat_text_id) { throw 'Fallback failed' } }
            else {
                $expected=$data.entries.$reaction.variants[$i]
                if ($row.cat_text_id -ne $expected.id -or $row.cat_response -ne $expected.text.Replace('{cat}','ソラ') -or $row.cat_pool -ne "cat_response/$($data.entries.$reaction.condition.subject_id)/ball/any/$reaction/ball") { throw "Selection failed: $reaction/$action/$i" }
            }
            $narratives.Rows.Add($row)
            Write-Output "$reaction/$action/$i id=$($row.cat_text_id) pool=$($row.cat_pool) text=$($row.cat_response)"
        }
    }
}
Write-Output "PASS 48 checks; assembly SHA256 $((Get-FileHash $assembly -Algorithm SHA256).Hash)"
