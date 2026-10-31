param(
    [ValidateSet('race','save-error','resume-error','save-failure','exit-error')][string]$Mode='race',
    [string]$ResumeDirectory,
    [switch]$ShortDialogue
)
$ErrorActionPreference='Stop'
$workspace=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$exe=Join-Path $workspace 'Logs/StandaloneBuild/Latest/CatCafeStandalone.exe'
$assembly=Join-Path (Split-Path -Parent $exe) 'CatCafeStandalone_Data/Managed/Assembly-CSharp.dll'
if(-not(Test-Path -LiteralPath $assembly) -or -not [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($assembly)).Contains('DevWatchRegression')){
    throw 'Build CatCafePreview.Editor.DevAcquisitionSetup.Build to fixed Latest first.'
}
if($Mode -eq 'resume-error'){
    if(-not $ResumeDirectory){throw 'resume-error requires the successful save-error run directory.'}
    $runRoot=(Resolve-Path -LiteralPath $ResumeDirectory).Path
}else{
    if($ResumeDirectory){throw 'Only resume-error accepts ResumeDirectory.'}
    $runRoot=Join-Path $workspace ('Logs/DeveloperWatch/Regression/'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+$Mode+'-'+[guid]::NewGuid().ToString('N'))
}
New-Item -ItemType Directory -Path (Split-Path -Parent $runRoot) -Force|Out-Null
$playerArgs=@('--catcafe-dev-watch',('"'+$runRoot+'"'),'--dev-watch-speed','64','--dev-watch-regression',$Mode,'-logFile',('"'+$runRoot+'-'+$Mode+'-player.log"'))
if($Mode -eq 'resume-error'){$playerArgs+='--dev-watch-resume'}else{$playerArgs+='--dev-watch-start'}
if($ShortDialogue){$playerArgs+='--dev-watch-short-dialogue'}
$watchProcess=Start-Process -FilePath $exe -ArgumentList $playerArgs -WindowStyle Normal -PassThru
Write-Output "Watch regression: $Mode / $runRoot"
$watchProcess.WaitForExit()
$expectedExit=if($Mode -eq 'exit-error'){3}else{0}
if($watchProcess.ExitCode -ne $expectedExit){throw "Unexpected Player exit $($watchProcess.ExitCode): $runRoot"}
$report=Get-Content -LiteralPath (Join-Path $runRoot 'watch-regression.json') -Raw|ConvertFrom-Json
if($report.status -ne 'PASS' -or $report.mode -ne $Mode){throw "Regression failed: $runRoot"}
Write-Output "PASS $Mode / $($report.checks) checks / Player exit $($watchProcess.ExitCode)"
