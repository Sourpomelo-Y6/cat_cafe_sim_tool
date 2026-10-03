param([Parameter(Mandatory=$true)][string]$ProjectPath)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
if (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'))) {
    throw 'Unityプロジェクトのルートを指定してください。'
}
$templateRoot = Join-Path $PSScriptRoot 'unity_preview_template'
$targetRoot = Join-Path $projectRoot 'Assets/CatCafePreview'
$previewFiles = @('Runtime/ContentBundle.cs', 'Runtime/CatInteractionState.cs', 'Runtime/ContentPreview.cs', 'Editor/ContentPreviewSetup.cs', 'Editor/ContentPreviewLayout.cs', 'Fonts/MPLUS1p-Regular.ttf', 'Fonts/OFL.txt', 'Fonts/README.md')
foreach ($relative in $previewFiles) {
    $target = Join-Path $targetRoot $relative
    if (Test-Path -LiteralPath $target) { throw "既存のファイルがあります。上書きしません: $target" }
}
foreach ($relative in $previewFiles) {
    $target = Join-Path $targetRoot $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $templateRoot $relative) -Destination $target
}
$readmeTarget = Join-Path $targetRoot 'README.md'
if (-not (Test-Path -LiteralPath $readmeTarget)) {
    Copy-Item -LiteralPath (Join-Path $templateRoot 'README.md') -Destination $readmeTarget
}
Write-Output "Installed Unity preview: $targetRoot"
