param(
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [string]$UnityPath,
    [string]$ExecuteMethod = 'CatCafePreview.Editor.StandaloneScreenSetup.Build'
)
$ErrorActionPreference = 'Stop'
$workspaceRoot = Split-Path -Parent $PSScriptRoot
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
$versionFile = Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'
if (-not (Test-Path -LiteralPath $versionFile)) { throw 'Specify a Unity project root.' }
if (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile')) {
    throw 'Close this Unity project before running the batch build.'
}
if (-not $UnityPath) {
    $versionLine = Get-Content -LiteralPath $versionFile | Where-Object { $_ -match '^m_EditorVersion:' }
    $version = ($versionLine -split ':',2)[1].Trim()
    $UnityPath = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe"
    if (-not (Test-Path -LiteralPath $UnityPath)) {
        $hubEditors = Join-Path $env:APPDATA 'UnityHub/editors-v2.json'
        if (Test-Path -LiteralPath $hubEditors) {
            $installed = (Get-Content -LiteralPath $hubEditors -Raw | ConvertFrom-Json).data | Where-Object { $_.version -eq $version } | Select-Object -First 1
            if ($installed) { $UnityPath = @($installed.location)[0] }
        }
    }
}
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Unity not found: $UnityPath" }
$editorRoot = Join-Path $projectRoot 'Assets/CatCafePreview/Editor'
foreach ($name in @('StandaloneBuildStorage.cs', 'StandaloneScreenSetup.cs', 'StandaloneCafeSetup.cs')) {
    $target = Join-Path $editorRoot $name
    if (-not (Test-Path -LiteralPath $target)) { throw "Missing build integration: $target" }
    if ([IO.File]::ReadAllText($target).Contains('BuildPipeline.BuildPlayer') -and $name -ne 'StandaloneBuildStorage.cs') {
        throw "Update $target to call StandaloneBuildStorage before building."
    }
}
$logRoot = Join-Path $workspaceRoot 'Logs/StandaloneBuild/Runs'
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
$logPath = Join-Path $logRoot ((Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [guid]::NewGuid().ToString('N') + '.log')
$oldWorkspace = $env:CATCAFE_WORKSPACE_ROOT
$oldBuild = $env:CATCAFE_STANDALONE_BUILD
try {
    $env:CATCAFE_WORKSPACE_ROOT = $workspaceRoot
    $env:CATCAFE_STANDALONE_BUILD = Join-Path $workspaceRoot 'Logs/StandaloneBuild/Latest/CatCafeStandalone.exe'
    $arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $projectRoot + '"'), '-executeMethod', $ExecuteMethod, '-logFile', ('"' + $logPath + '"'))
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    # Wait for the Editor itself, not persistent licensing/crash-handler children.
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity failed ($($process.ExitCode)). Log: $logPath" }
    if (-not [IO.File]::ReadAllText($logPath).Contains('CAT_CAFE_STANDALONE_BUILD_OK:')) { throw "No successful build recorded. Log: $logPath" }
    Write-Output "Build: $env:CATCAFE_STANDALONE_BUILD"
    Write-Output "Log: $logPath"
} finally {
    $env:CATCAFE_WORKSPACE_ROOT = $oldWorkspace
    $env:CATCAFE_STANDALONE_BUILD = $oldBuild
}
