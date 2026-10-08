param([Parameter(Mandatory=$true)][string]$ProjectPath)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
if (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'))) {
    throw 'Unityプロジェクトのルートを指定してください。'
}
$templateRoot = Join-Path $PSScriptRoot 'unity_preview_template'
$targetRoot = Join-Path $projectRoot 'Assets/CatCafePreview'
$previewFiles = @('Runtime/ContentBundle.cs', 'Runtime/CatInteractionState.cs', 'Runtime/ContentPreview.cs', 'Editor/ContentPreviewSetup.cs', 'Editor/ContentPreviewLayout.cs', 'Fonts/MPLUS1p-Regular.ttf', 'Fonts/OFL.txt', 'Fonts/README.md')
$previewFiles += @('Runtime/PythonStateClient.cs', 'Runtime/PythonCatCard.cs', 'Runtime/PythonCatRoster.cs', 'Editor/PythonStateSetup.cs', 'Editor/PythonShiftVerification.cs', 'Editor/PythonRosterSetup.cs')
$previewFiles += @('Runtime/PythonServiceScreen.cs', 'Editor/PythonServiceSetup.cs', 'Editor/PythonServiceLayout.cs')
$previewFiles += @('Runtime/PythonSavePanel.cs', 'Editor/PythonSaveSetup.cs', 'Editor/PythonSaveVerification.cs')
$previewFiles += @('Runtime/PythonIntakePanel.cs', 'Editor/PythonIntakeSetup.cs', 'Editor/PythonIntakeVerification.cs')
$previewFiles += @('Runtime/PythonVisitingCatPanel.cs', 'Editor/PythonVisitingCatSetup.cs')
$previewFiles += @('Runtime/PythonEventListPanel.cs', 'Editor/PythonEventListSetup.cs')
$previewFiles += @('Runtime/PythonRegularIntroductionPanel.cs', 'Editor/PythonRegularIntroductionSetup.cs')
$previewFiles += @('Runtime/PythonReservationPanel.cs', 'Editor/PythonReservationSetup.cs')
$previewFiles += @('Runtime/PythonCustomerTrustPanel.cs', 'Editor/PythonCustomerTrustSetup.cs')
$previewFiles += @('Runtime/PythonGeneralDispatchPanel.cs', 'Editor/PythonGeneralDispatchSetup.cs')
$previewFiles += @('Runtime/PythonDispatchChoicePanel.cs', 'Editor/PythonDispatchChoiceSetup.cs')
$previewFiles += @('Runtime/PythonHousingPanel.cs', 'Editor/PythonHousingSetup.cs', 'Editor/PythonHousingVerification.cs')
$previewFiles += @('Runtime/PythonGoalPanel.cs', 'Editor/PythonGoalSetup.cs', 'Editor/PythonGoalVerification.cs')
$previewFiles += @('Runtime/PythonGrowthPanel.cs', 'Editor/PythonGrowthSetup.cs', 'Editor/PythonGrowthVerification.cs')
$previewFiles += @('Runtime/PythonStoreEventPanel.cs', 'Editor/PythonStoreEventSetup.cs', 'Editor/PythonStoreEventVerification.cs')
$previewFiles += @('Runtime/PythonTitleScreen.cs', 'Runtime/PythonReturnToTitle.cs', 'Editor/PythonTitleSetup.cs', 'Editor/PythonTitleVerification.cs')
$previewFiles += @('Art/cat-cafe-title-background-v1.png', 'Art/cat-cafe-title-background-v1.png.meta')
$previewFiles += @('Resources/CatCafeDefaults/cat-silhouette-v1.png')
$previewFiles += @('Runtime/PythonNewGamePanel.cs', 'Editor/PythonNewGameSetup.cs')
$previewFiles += @('Runtime/PythonCatDetailsPanel.cs', 'Editor/PythonCatDetailsSetup.cs')
$previewFiles += @('Runtime/PythonAdoptionPanel.cs', 'Runtime/PythonGameOverPanel.cs', 'Editor/PythonAdoptionSetup.cs', 'Editor/PythonGameOverSetup.cs', 'Editor/PythonGameOverVerification.cs', 'Editor/PythonAdoptionVerification.cs')
$previewFiles += @('Runtime/PythonMissingPanel.cs', 'Editor/PythonMissingSetup.cs', 'Editor/PythonMissingVerification.cs')
$previewFiles += @('Runtime/PythonDispatchPanel.cs', 'Editor/PythonDispatchSetup.cs', 'Editor/PythonDispatchVerification.cs')
$previewFiles += @('Runtime/PythonPlayerPanel.cs', 'Editor/PythonPlayerSetup.cs', 'Editor/PythonPlayerLayout.cs', 'Editor/PythonPlayerVerification.cs')
$previewFiles += @('Runtime/PythonAutoPanel.cs', 'Editor/PythonAutoSetup.cs', 'Editor/PythonAutoVerification.cs')
$previewFiles += @('Runtime/PythonPreparationPanel.cs', 'Editor/PythonPreparationSetup.cs', 'Editor/PythonPreparationVerification.cs')
$previewFiles += @('Runtime/PythonClosingPanel.cs', 'Editor/PythonClosingSetup.cs', 'Editor/PythonClosingVerification.cs')
$previewFiles += @('Editor/PythonMultiDayVerification.cs')
$previewFiles += @('Runtime/PythonPresentation.cs', 'Editor/PythonPresentationSetup.cs')
$previewFiles += @('Editor/PythonCatDetailsLayout.cs')
$previewFiles += @('Runtime/PythonEventPresentation.cs', 'Editor/PythonEventLayout.cs', 'Editor/PythonEventLayoutVerification.cs')
$previewFiles += @('Editor/PythonSaveLayout.cs')
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
