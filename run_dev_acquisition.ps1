param(
    [string]$PlanPath,
    [string]$ResumeDirectory,
    [switch]$Start,
    [switch]$Wait
)
$ErrorActionPreference='Stop'
$workspace=Split-Path -Parent $PSScriptRoot
$exe=Join-Path $workspace 'Logs/StandaloneBuild/Latest/CatCafeStandalone.exe'
if(-not (Test-Path -LiteralPath $exe)){throw 'Build the dedicated DevAcquisitionSetup.Build player first.'}
$assembly=Join-Path (Split-Path $exe -Parent) 'CatCafeStandalone_Data/Managed/Assembly-CSharp.dll'
if(-not (Test-Path -LiteralPath $assembly) -or -not [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($assembly)).Contains('CafeDevAcquisition')){
    throw 'Latest is a normal player. Build CatCafePreview.Editor.DevAcquisitionSetup.Build before using this launcher.'
}
$runs=Join-Path $workspace 'Logs/DeveloperAcquisition/Runs'
if($ResumeDirectory){
    $run=(Resolve-Path -LiteralPath $ResumeDirectory).Path
    $PlanPath=Join-Path $run 'plan.json'
}else{
    $run=Join-Path $runs ((Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N'))
    if(-not $PlanPath){
        $plans=Join-Path $workspace 'Logs/DeveloperAcquisition/Plans'
        New-Item -ItemType Directory -Path $plans -Force|Out-Null
        $PlanPath=Join-Path $plans ((Split-Path $run -Leaf)+'.json')
        $versions=@()
        foreach($repo in @('cat_cafe_sim_tool','Docs','cat_cafe_sim')){
            $path=Join-Path $workspace $repo
            $revision=git -C $path rev-parse HEAD
            $dirty=[bool](git -C $path status --porcelain)
            $versions+="$repo=$revision dirty=$dirty"
        }
        @{version=1;seed=0;max_days=150;max_operations=100000;max_seconds=7200;free_days=30;checkpoint_operations=200;max_log_bytes=268435456;objectives=@('popularity','patron','bond','free');source_versions=$versions}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath $PlanPath -Encoding UTF8
    }
}
$PlanPath=(Resolve-Path -LiteralPath $PlanPath).Path
$arguments=@('--catcafe-dev-acquisition',('"'+$run+'"'),'--dev-acquisition-plan',('"'+$PlanPath+'"'),'-logFile',('"'+$run+'-player.log"'))
if($ResumeDirectory){$arguments+='--dev-acquisition-resume'}
if($Start){$arguments+='--dev-acquisition-start'}
New-Item -ItemType Directory -Path (Split-Path $run -Parent) -Force|Out-Null
$process=Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Development run: $run"
Write-Output "Process: $($process.Id)"
Write-Output 'Omit -Start to use the dedicated run/pause screen. Resume requires -ResumeDirectory and an explicit -Start or on-screen start.'
if($Wait){$process.WaitForExit();Write-Output "Player exit: $($process.ExitCode)"}
