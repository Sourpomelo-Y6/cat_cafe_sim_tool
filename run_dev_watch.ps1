param(
    [string]$ResumeDirectory,
    [ValidateSet(1,2,4,16,64)][int]$Speed=1,
    [switch]$Collection,
    [string[]]$Routes,
    [ValidateRange(4,8)][int]$Days=8,
    [switch]$Start,
    [switch]$ShortDialogue,
    [switch]$ExitOnComplete,
    [switch]$Wait
)
$ErrorActionPreference='Stop'
$workspace=Split-Path -Parent $PSScriptRoot
$exe=Join-Path $workspace 'Logs/StandaloneBuild/Latest/CatCafeStandalone.exe'
$assembly=Join-Path (Split-Path $exe -Parent) 'CatCafeStandalone_Data/Managed/Assembly-CSharp.dll'
if(-not (Test-Path -LiteralPath $assembly) -or -not [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($assembly)).Contains('DevWatchSession')){
    throw 'Build CatCafePreview.Editor.DevAcquisitionSetup.Build to fixed Latest before starting watch mode.'
}
if($ResumeDirectory){$run=(Resolve-Path -LiteralPath $ResumeDirectory).Path}else{
    $run=Join-Path $workspace ('Logs/DeveloperWatch/Runs/'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N'))
}
New-Item -ItemType Directory -Path (Split-Path $run -Parent) -Force|Out-Null
$arguments=@('--catcafe-dev-watch',('"'+$run+'"'),'--dev-watch-days',$Days,'--dev-watch-speed',$Speed,'-logFile',('"'+$run+'-player.log"'))
if($ResumeDirectory){$arguments+='--dev-watch-resume'}
if($Collection -or $Routes){$arguments+='--dev-watch-collection'}
if($Routes){$arguments+=@('--dev-watch-routes',($Routes -join ','))}
if($Start){$arguments+='--dev-watch-start'}
if($ShortDialogue){$arguments+='--dev-watch-short-dialogue'}
if($ExitOnComplete){$arguments+='--dev-watch-exit-on-complete'}
$process=Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Normal -PassThru
Write-Output "Watch run: $run"
Write-Output "Process: $($process.Id)"
Write-Output 'Use the on-screen Run, Pause, Step and speed controls. Normal player saves are isolated.'
if($Wait){$process.WaitForExit();Write-Output "Player exit: $($process.ExitCode)"}
