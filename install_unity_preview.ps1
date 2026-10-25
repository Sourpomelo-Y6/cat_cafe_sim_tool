param([Parameter(Mandatory=$true)][string]$ProjectPath)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
if (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'))) {
    throw 'Unityプロジェクトのルートを指定してください。'
}
$templateRoot = Join-Path $PSScriptRoot 'unity_preview_template'
$targetRoot = Join-Path $projectRoot 'Assets/CatCafePreview'
$previewFiles = @('Runtime/ContentBundle.cs', 'Runtime/CatInteractionState.cs', 'Runtime/ContentPreview.cs', 'Editor/ContentPreviewSetup.cs', 'Editor/ContentPreviewLayout.cs', 'Fonts/MPLUS1p-Regular.ttf', 'Fonts/OFL.txt', 'Fonts/README.md')
$previewFiles += @('Runtime/HumanPortraitResources.cs','Editor/HumanIconsApplyVerification.cs','Runtime/StandaloneHumanIconsSmoke.cs','Editor/HumanPortraitImport.cs','Editor/HumanPortraitVerification.cs')
$previewFiles += @('Runtime/Standalone/CafePlayerStaminaGuidance.cs', 'Runtime/StandalonePlayerStaminaSmoke.cs')
$previewFiles += @('Editor/CatTexturePolicy.cs', 'Runtime/StandaloneTexturePolicySmoke.cs')
$previewFiles += @('Runtime/StandaloneRoundTripConversationSmoke.cs','Editor/RoundTripConversationVerification.cs')
$previewFiles += @('Runtime/StandaloneEquipmentConversationSmoke.cs','Editor/EquipmentConversationVerification.cs','Runtime/EquipmentConversationPanel.cs','Runtime/LocalCafeEquipmentConversation.cs','Runtime/Standalone/CafeEquipmentDialogue.cs')
$previewFiles += @('Runtime/PythonStateClient.cs', 'Runtime/PythonCatCard.cs', 'Runtime/PythonCatRoster.cs', 'Editor/PythonStateSetup.cs', 'Editor/PythonShiftVerification.cs', 'Editor/PythonRosterSetup.cs')
$previewFiles += @('Runtime/PythonServiceScreen.cs', 'Editor/PythonServiceSetup.cs', 'Editor/PythonServiceLayout.cs')
$previewFiles += @('Runtime/PythonSavePanel.cs', 'Editor/PythonSaveSetup.cs', 'Editor/PythonSaveVerification.cs')
$previewFiles += @('Runtime/PythonIntakePanel.cs', 'Editor/PythonIntakeSetup.cs', 'Editor/PythonIntakeVerification.cs')
$previewFiles += @('Runtime/PythonVisitingCatPanel.cs', 'Editor/PythonVisitingCatSetup.cs')
$previewFiles += @('Runtime/PythonEventListPanel.cs', 'Runtime/PythonEventCases.cs', 'Runtime/StandalonePendingCasesSmoke.cs', 'Editor/PendingCasesVerification.cs', 'Editor/PythonEventListSetup.cs')
$previewFiles += @('Runtime/PythonRegularIntroductionPanel.cs', 'Editor/PythonRegularIntroductionSetup.cs')
$previewFiles += @('Runtime/PythonReservationPanel.cs', 'Editor/PythonReservationSetup.cs')
$previewFiles += @('Runtime/PythonCustomerTrustPanel.cs', 'Editor/PythonCustomerTrustSetup.cs')
$previewFiles += @('Runtime/PythonGeneralDispatchPanel.cs', 'Editor/PythonGeneralDispatchSetup.cs')
$previewFiles += @('Runtime/PythonDispatchChoicePanel.cs', 'Editor/PythonDispatchChoiceSetup.cs')
$previewFiles += @('Runtime/PythonDispatchTroublePanel.cs', 'Editor/PythonDispatchTroubleSetup.cs')
$previewFiles += @('Runtime/PythonDispatchIntroductionPanel.cs', 'Editor/PythonDispatchIntroductionSetup.cs')
$previewFiles += @('Runtime/PythonItemsPanel.cs', 'Editor/PythonItemsSetup.cs', 'Editor/PythonItemsLayout.cs', 'Editor/PythonItemsCareVerification.cs')
$previewFiles += @('Runtime/PythonDayOffPanel.cs', 'Editor/PythonDayOffSetup.cs')
$previewFiles += @('Runtime/PythonExpansionPanel.cs', 'Editor/PythonExpansionSetup.cs')
$previewFiles += @('Runtime/PythonSeatEquipmentPanel.cs', 'Editor/PythonSeatEquipmentSetup.cs')
$previewFiles += @('Runtime/PythonRestSpacePanel.cs', 'Editor/PythonRestSpaceSetup.cs')
$previewFiles += @('Runtime/PythonWaitingAreaPanel.cs', 'Editor/PythonWaitingAreaSetup.cs')
$previewFiles += @('Runtime/PythonCustomerDirectoryPanel.cs', 'Editor/PythonCustomerDirectorySetup.cs', 'Editor/PythonCustomerDirectoryLayout.cs', 'Editor/PythonCustomerDirectoryReviewVerification.cs')
$previewFiles += @('Runtime/PythonHousingPanel.cs', 'Editor/PythonHousingSetup.cs', 'Editor/PythonHousingVerification.cs')
$previewFiles += @('Runtime/PythonGoalPanel.cs', 'Editor/PythonGoalSetup.cs', 'Editor/PythonGoalVerification.cs')
$previewFiles += @('Runtime/PythonGrowthPanel.cs', 'Editor/PythonGrowthSetup.cs', 'Editor/PythonGrowthVerification.cs')
$previewFiles += @('Runtime/PythonStoreEventPanel.cs', 'Editor/PythonStoreEventSetup.cs', 'Editor/PythonStoreEventVerification.cs')
$previewFiles += @('Runtime/PythonTitleScreen.cs', 'Runtime/PythonReturnToTitle.cs', 'Editor/PythonTitleSetup.cs', 'Editor/PythonTitleVerification.cs', 'Editor/PythonStartFlowLayout.cs', 'Editor/PythonStartFlowVerification.cs', 'Editor/PythonManagementFlowVerification.cs')
$previewFiles += @('Art/cat-cafe-title-background-v1.png', 'Art/cat-cafe-title-background-v1.png.meta')
$previewFiles += @('Resources/CatCafeDefaults/cat-silhouette-v1.png')
$previewFiles += @('Runtime/PythonNewGamePanel.cs', 'Editor/PythonNewGameSetup.cs')
$previewFiles += @('Runtime/PythonCatDetailsPanel.cs', 'Editor/PythonCatDetailsSetup.cs')
$previewFiles += @('Runtime/PythonAdoptionPanel.cs', 'Runtime/PythonGameOverPanel.cs', 'Editor/PythonAdoptionSetup.cs', 'Editor/PythonGameOverSetup.cs', 'Editor/PythonGameOverVerification.cs', 'Editor/PythonAdoptionVerification.cs')
$previewFiles += @('Runtime/PythonMissingPanel.cs', 'Editor/PythonMissingSetup.cs', 'Editor/PythonMissingVerification.cs')
$previewFiles += @('Runtime/PythonDispatchPanel.cs', 'Editor/PythonDispatchSetup.cs', 'Editor/PythonDispatchVerification.cs')
$previewFiles += @('Runtime/PythonSeatNarrativePanel.cs', 'Runtime/StandaloneNarrativeSmoke.cs', 'Editor/NarrativeVerification.cs')
$previewFiles += @('Editor/ConversationRendererVerification.cs', 'Editor/DispatchConversationVerification.cs', 'Runtime/StandaloneDispatchConversationSmoke.cs', 'Runtime/Standalone/CafeDispatchConversation.cs', 'Runtime/LocalCafeDispatchConversation.cs', 'Runtime/DispatchConversationPanel.cs', 'Runtime/StandaloneEncounterConversationSmoke.cs', 'Editor/EncounterConversationVerification.cs', 'Runtime/StandaloneDispatchIntroductionConversationSmoke.cs', 'Editor/DispatchIntroductionConversationVerification.cs', 'Runtime/DispatchIntroductionConversationPanel.cs', 'Runtime/LocalCafeDispatchIntroductionConversation.cs', 'Runtime/Standalone/CafeDispatchIntroductionDialogue.cs', 'Runtime/EncounterConversationPanel.cs', 'Runtime/LocalCafeEncounterConversation.cs', 'Runtime/Standalone/CafeEncounterConversation.cs', 'Runtime/Standalone/CafeRegularConversation.cs', 'Runtime/StandaloneRegularConversationSmoke.cs', 'Editor/ExtraConversationVerification.cs', 'Runtime/StandaloneExtraConversationSmoke.cs', 'Runtime/LocalCafeReplayArchive.cs', 'Runtime/Standalone/CafeReplayArchive.cs', 'Editor/CGGalleryVerification.cs', 'Runtime/StandaloneCGGallerySmoke.cs', 'Runtime/CatImageGalleryPanel.cs', 'Runtime/CatImageGalleryCatalog.cs', 'Runtime/ViewedCatImages.cs', 'Runtime/Standalone/CafeImageGallery.cs', 'Runtime/TitleExtraPanel.cs', 'Runtime/ReplayConversationPanel.cs', 'Runtime/AcquisitionConversationPanel.cs', 'Runtime/RegularConversationPanel.cs', 'Runtime/ConversationVisuals.cs', 'Runtime/IntakeConversationPanel.cs', 'Runtime/LocalCafeConversation.cs', 'Runtime/StandaloneConversationSmoke.cs', 'Editor/StandaloneBuildStorage.cs')
$previewFiles += @('Runtime/Standalone/CafeGrowthBatch.cs', 'Runtime/StandaloneGrowthBatchSmoke.cs', 'Editor/GrowthBatchVerification.cs')
$previewFiles += @('Runtime/StandaloneIntegratedSmoke.cs', 'Runtime/StandaloneRoutePlaytest.cs', 'Editor/IntegratedVerification.cs')
$previewFiles += @('Runtime/StandaloneUIFixSmoke.cs', 'Editor/UIFixVerification.cs')
$previewFiles += @('Runtime/Standalone/CafePatronEnding.cs', 'Runtime/StandalonePatronEndingSmoke.cs', 'Editor/PatronEndingVerification.cs')
$previewFiles += @('Runtime/Standalone/CafeBondEnding.cs', 'Runtime/StandaloneBondEndingSmoke.cs', 'Editor/BondEndingVerification.cs')
$previewFiles += @('Runtime/Standalone/CafePopularityEnding.cs', 'Runtime/LocalCafePopularityEnding.cs', 'Runtime/PopularityEndingPanel.cs', 'Runtime/StandaloneEndingSmoke.cs', 'Editor/EndingVerification.cs')
$previewFiles += @('Runtime/Standalone/CafeOpening.cs', 'Runtime/LocalCafeOpening.cs', 'Runtime/OpeningPanel.cs', 'Runtime/StandaloneOpeningSmoke.cs', 'Editor/OpeningVerification.cs')
$previewFiles += @('Runtime/Standalone/CafeReadStore.cs','Runtime/StandaloneReadSkipSmoke.cs')
$previewFiles += @('Runtime/CafeOptions.cs', 'Runtime/GameOptionsPanel.cs', 'Runtime/StandaloneOptionsSmoke.cs', 'Editor/OptionsVerification.cs')
$previewFiles += @('Runtime/CafeNarrativeResources.cs', 'Editor/CatalogVerification.cs', 'Editor/SubjectCatalogVerification.cs', 'Editor/AllModeNarrativeVerification.cs')
$previewFiles += @('Runtime/PlayerInteractionLayout.cs', 'Runtime/StandalonePlayerLayoutSmoke.cs', 'Editor/PlayerImageLayoutVerification.cs', 'Runtime/PythonPlayerPanel.cs', 'Editor/PythonPlayerSetup.cs', 'Editor/PythonPlayerLayout.cs', 'Editor/PythonPlayerVerification.cs')
$previewFiles += @('Runtime/PythonAutoPanel.cs', 'Editor/PythonAutoSetup.cs', 'Editor/PythonAutoVerification.cs')
$previewFiles += @('Runtime/PythonPreparationPanel.cs', 'Editor/PythonPreparationSetup.cs', 'Editor/PythonPreparationVerification.cs')
$previewFiles += @('Runtime/PythonBusinessHeader.cs', 'Editor/PythonBusinessHeaderSetup.cs', 'Editor/PythonBusinessHeaderVerification.cs', 'Editor/PythonBusinessDashboardLayout.cs', 'Editor/PythonBusinessDashboardVerification.cs')
$previewFiles += @('Runtime/PythonPreparationRoster.cs', 'Editor/PythonPreparationLayout.cs', 'Editor/PythonPreparationRosterVerification.cs')
$previewFiles += @('Runtime/PythonClosingReview.cs', 'Editor/PythonClosingReviewLayout.cs', 'Editor/PythonClosingReviewVerification.cs')
$previewFiles += @('Runtime/PythonClosingPanel.cs', 'Editor/PythonClosingSetup.cs', 'Editor/PythonClosingVerification.cs')
$previewFiles += @('Editor/PythonMultiDayVerification.cs')
$previewFiles += @('Runtime/PythonPresentation.cs', 'Editor/PythonPresentationSetup.cs')
$previewFiles += @('Runtime/PythonDispatchDisplay.cs', 'Editor/PythonDispatchLayout.cs', 'Editor/PythonDispatchNavigationVerification.cs')
$previewFiles += @('Runtime/PythonCatAcquisitionNavigation.cs', 'Editor/PythonCatAcquisitionLayout.cs', 'Editor/PythonCatAcquisitionVerification.cs')
$previewFiles += @('Runtime/PythonSaveBrowser.cs', 'Editor/PythonSaveBrowserLayout.cs', 'Editor/PythonSaveBrowserVerification.cs')
$previewFiles += @('Editor/PythonFacilityLayout.cs', 'Editor/PythonFacilityVerification.cs')
$previewFiles += @('Editor/PythonCatDetailsLayout.cs', 'Editor/PythonCatRosterLayout.cs', 'Editor/PythonCatRosterVerification.cs')
$previewFiles += @('Runtime/PythonEventRecordPanel.cs', 'Editor/PythonEventReviewLayout.cs', 'Editor/PythonEventReviewVerification.cs')
$previewFiles += @('Runtime/PythonEventPresentation.cs', 'Editor/PythonEventLayout.cs', 'Editor/PythonEventLayoutVerification.cs')
$previewFiles += @('Editor/PythonSaveLayout.cs')
$previewFiles += @('Runtime/PythonFinanceHistoryPanel.cs', 'Editor/PythonFinanceHistorySetup.cs', 'Editor/PythonFinanceHistoryLayout.cs', 'Editor/PythonFinanceHistoryReviewVerification.cs')
$previewFiles += @('Runtime/PythonPetShopPanel.cs', 'Editor/PythonPetShopSetup.cs')
$previewFiles += @('Runtime/PythonObjectiveStartPanel.cs', 'Editor/PythonObjectiveStartSetup.cs', 'Editor/PythonObjectiveReviewLayout.cs', 'Editor/PythonObjectiveReviewVerification.cs')
$previewFiles += @('Runtime/PythonLegacySavePanel.cs', 'Editor/PythonLegacySaveSetup.cs')
$previewFiles += @('Runtime/PythonDispatchComparisonPanel.cs', 'Editor/PythonDispatchComparisonSetup.cs')
$previewFiles += @('Runtime/PythonServiceAssignmentPanel.cs', 'Editor/PythonServiceAssignmentSetup.cs')
$previewFiles += @('Runtime/PythonAutoPlayPanel.cs', 'Editor/PythonAutoPlaySetup.cs', 'Editor/PythonAutoPlayVerification.cs')
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
