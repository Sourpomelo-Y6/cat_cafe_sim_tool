using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class ContentPreview : MonoBehaviour
    {
        private ContentBundle bundle;
        private readonly Dictionary<string, CatInteractionState> interactions = new Dictionary<string, CatInteractionState>();
        public CatInteractionState CurrentInteraction { get { return interactions[CurrentCatId]; } }
        private string resourceRoot;
        [SerializeField] private Dropdown cats;
        [SerializeField] private RawImage picture;
        [SerializeField] private AspectRatioFitter aspect;
        [SerializeField] private Text description;
        [SerializeField] private Text status;
        [SerializeField] private Text emptyImage;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Button[] sceneButtons;
        public bool HasUIBindings { get { return cats != null && picture != null && aspect != null && description != null
            && status != null && emptyImage != null && scroll != null && sceneButtons != null && sceneButtons.Length == 4
            && Array.TrueForAll(sceneButtons, button => button != null); } }
        public Button GetSceneButton(int index) { return sceneButtons[index]; }
        private bool displayOnly;
        private int selected;
        private string sceneKey = "normal";
        private PythonCafeState pythonState;
        public bool PythonMode { get { return GetComponent<PythonStateClient>() != null; } }
        private string selectedPythonId;
        public PythonCatState[] VisiblePythonCats { get { var rows=pythonState?.cats ?? new PythonCatState[0];return GetComponent<PythonCatVisibility>()?.Filter(rows) ?? rows; } }
        public string SelectedPythonCatId { get { var rows=VisiblePythonCats;return cats.value>=0 && cats.value<rows.Length ? rows[cats.value].cat_id : ""; } }

        public string CurrentCatId { get { return bundle == null ? "" : bundle.cats[selected].cat_id; } }
        public string CurrentSceneKey { get { return sceneKey; } }
        public string DisplayText { get { return description == null ? "" : description.text; } }
        public bool HasImage { get { return picture != null && picture.texture != null; } }
        private void Start()
        {
            Initialize();
            if (PythonMode) { ConfigurePythonButtons(); return; }
            var labels = new[] { "次の交流", "遊ぶ", "撫でる", "休む" };
            for (int i = 0; i < sceneButtons.Length; i++) sceneButtons[i].GetComponentInChildren<Text>().text = labels[i];
        }
        public void Initialize()
        {
            if(Application.isPlaying && CafeBackends.Standalone && GetComponent<PythonStateClient>()!=null){if(GetComponent<ViewedCatImages>()==null)gameObject.AddComponent<ViewedCatImages>();GameOptionsPanel.Attach(this);}
            if (!HasUIBindings)
            {
                Debug.LogError("Content Preview UI references are missing. Open the scene through Cat Cafe > Open Content Preview.", this);
                return;
            }
            bundle = null;
            selected = 0;
            picture.texture = null;
            picture.enabled = false;
            emptyImage.enabled = true;
            cats.interactable = false;
            foreach (var button in sceneButtons) button.interactable = false;
            try
            {
                var catalogText = Resources.Load<TextAsset>("CatCafeContent/catalog");
                if (catalogText == null) throw new ArgumentException("素材が未取り込みです。Cat Cafe → Import Content Pack… を使ってください。");
                var catalog = JsonUtility.FromJson<ContentCatalog>(catalogText.text);
                if (catalog == null || !System.Text.RegularExpressions.Regex.IsMatch(catalog.bundleResource ?? "",
                    "^CatCafeContent/Imports/[a-f0-9]{32}/bundle$")) throw new ArgumentException("素材の参照先が不正です。");
                var bundleText = Resources.Load<TextAsset>(catalog.bundleResource);
                if (bundleText == null) throw new ArgumentException("素材パックを読み込めません。");
                bundle = JsonUtility.FromJson<ContentBundle>(bundleText.text);
                ContentValidation.Validate(bundle);
                foreach (var cat in bundle.cats)
                    if (!interactions.ContainsKey(cat.cat_id)) interactions.Add(cat.cat_id, new CatInteractionState());
                resourceRoot = catalog.bundleResource.Substring(0, catalog.bundleResource.Length - "bundle".Length);
                cats.options.Clear();
                foreach (var cat in bundle.cats) cats.options.Add(new Dropdown.OptionData(cat.name + "（" + cat.cat_id + "）"));
                cats.SetValueWithoutNotify(0);
                cats.RefreshShownValue();
                cats.interactable = true;
                foreach (var button in sceneButtons) button.interactable = true;
                DisplaySelection();
            }
            catch (Exception ex) { bundle = null; status.text = ex.Message; description.text = "制作ツールで採用済み素材を書き出して取り込んでください。"; }
        }

        public void ShowScene(string key)
        {
            if (bundle == null) return;
            if (Application.isPlaying && !displayOnly && !PythonMode)
            {
                if (!PerformAction(key)) return;
            }
            sceneKey = key;
            var cat = bundle.cats[selected];
            var scene = Array.Find(cat.scenes, value => value.key == key);
            if (scene == null) return;
            description.text = cat.name + "\n\n" + cat.introduction + "\n\n" + scene.label + "\n" + scene.text;
            if (Application.isPlaying && !PythonMode) description.text += "\n\n" + CurrentInteraction.Summary;
            scroll.verticalNormalizedPosition = 1;
            var texture = scene.image == "" ? null : Resources.Load<Texture2D>(resourceRoot + scene.image.Substring(0, scene.image.Length - 4));
            var registeredTexture = texture;
            texture = texture ?? CatPlaceholderTexture();
            picture.texture = texture;
            picture.enabled = texture != null;
            emptyImage.enabled = texture == null;
            if (texture != null) aspect.aspectRatio = (float)texture.width / texture.height;
            status.text = cat.name + " / " + scene.label + (registeredTexture == null ? "：画像は未登録または読込失敗です。" : "：登録済み素材を表示中");
        }

        public void SelectCat(int index)
        {
            if (PythonMode && pythonState != null) { DisplayPythonCat(index); return; }
            if (bundle == null || index < 0 || index >= bundle.cats.Length) throw new ArgumentOutOfRangeException("index");
            cats.SetValueWithoutNotify(index);
            selected = index;
            sceneKey = "normal";
            // Selection only displays content; it never spends a turn.
            DisplaySelection();
        }

        private void DisplaySelection()
        {
            displayOnly = true;
            try { ShowScene(sceneKey); }
            finally { displayOnly = false; }
        }
        public bool PerformAction(string key)
        {
            if (PythonMode) return false;
            if (bundle == null) return false;
            if (key == "normal") { CurrentInteraction.NextSet(); return true; }
            if (!CurrentInteraction.Act(key))
            {
                status.text = CurrentInteraction.EndReason + "。「次の交流」を押してください。";
                return false;
            }
            return true;
        }

        public void ApplyPythonState(PythonCafeState state)
        {
            var header=GetComponent<PythonBusinessHeader>();if(header!=null)header.Connected=true;
            var feedback=GetComponent<PythonEventPresentation>();if(feedback!=null)feedback.Apply(state);
            var presentation=GetComponent<PythonPresentation>();if(presentation!=null)presentation.Apply(state);
            string previous = selectedPythonId;
            pythonState = state;
            GetComponent<PythonCatVisibility>()?.Apply(state);
            var visible=VisiblePythonCats;
            cats.options.Clear();
            foreach (var cat in visible) cats.options.Add(new Dropdown.OptionData(cat.name));
            int index = Array.FindIndex(visible, cat => cat.cat_id == previous);
            cats.SetValueWithoutNotify(Math.Max(0, index));
            cats.RefreshShownValue();
            cats.interactable = visible.Length>0;
            foreach (var button in sceneButtons) button.interactable = false;
            DisplayPythonCat(cats.value);
            var roster = GetComponent<PythonCatRoster>();
            if (roster != null) roster.Apply(state);
            var service = GetComponent<PythonServiceScreen>();
            if (service != null) service.Apply(state);
            var closing=GetComponent<PythonClosingPanel>();if(closing!=null)closing.Apply(state);
            var intake = GetComponent<PythonIntakePanel>();
            if (intake != null) intake.Apply(state);
            var trust=GetComponent<PythonCustomerTrustPanel>();if(trust!=null)trust.Apply(state);
            var reservation=GetComponent<PythonReservationPanel>();if(reservation!=null)reservation.Apply(state);
            var regular=GetComponent<PythonRegularIntroductionPanel>();if(regular!=null)regular.Apply(state);
            var visiting=GetComponent<PythonVisitingCatPanel>();if(visiting!=null)visiting.Apply(state);
            var housing = GetComponent<PythonHousingPanel>();
            if (housing != null) housing.Apply(state);
            var goal = GetComponent<PythonGoalPanel>();
            if (goal != null) goal.Apply(state);
            var store = GetComponent<PythonStoreEventPanel>();
            if (store != null) store.Apply(state);
            var menu=GetComponent<PythonNewGamePanel>();if(menu!=null) menu.Apply(state);
            var detail = GetComponent<PythonCatDetailsPanel>();
            if(detail != null) detail.Apply(state);
            var growth = GetComponent<PythonGrowthPanel>();
            if (growth != null) growth.Apply(state);
            var player=GetComponent<PythonPlayerPanel>();if(player!=null)player.Apply(state);
            var dispatch=GetComponent<PythonDispatchPanel>();if(dispatch!=null)dispatch.Apply(state);
            var generalDispatch=GetComponent<PythonGeneralDispatchPanel>();if(generalDispatch!=null)generalDispatch.Apply(state);
            var dispatchChoice=GetComponent<PythonDispatchChoicePanel>();if(dispatchChoice!=null)dispatchChoice.Apply(state);
            var dispatchTrouble=GetComponent<PythonDispatchTroublePanel>();if(dispatchTrouble!=null)dispatchTrouble.Apply(state);
            var dispatchIntroduction=GetComponent<PythonDispatchIntroductionPanel>();if(dispatchIntroduction!=null)dispatchIntroduction.Apply(state);
            var items=GetComponent<PythonItemsPanel>();if(items!=null)items.Apply(state);
            var dayOff=GetComponent<PythonDayOffPanel>();if(dayOff!=null)dayOff.Apply(state);
            var expansion=GetComponent<PythonExpansionPanel>();if(expansion!=null)expansion.Apply(state);
            var equipment=GetComponent<PythonSeatEquipmentPanel>();if(equipment!=null)equipment.Apply(state);
            var restSpace=GetComponent<PythonRestSpacePanel>();if(restSpace!=null)restSpace.Apply(state);
            var waitingArea=GetComponent<PythonWaitingAreaPanel>();if(waitingArea!=null)waitingArea.Apply(state);
            var customerDirectory=GetComponent<PythonCustomerDirectoryPanel>();if(customerDirectory!=null)customerDirectory.Apply(state);
            var financeHistory=GetComponent<PythonFinanceHistoryPanel>();if(financeHistory!=null)financeHistory.Apply(state);
            var petShop=GetComponent<PythonPetShopPanel>();if(petShop!=null)petShop.Apply(state);
            GetComponent<PythonRecruitmentPanel>()?.Apply(state);
            var objectiveStart=GetComponent<PythonObjectiveStartPanel>();if(objectiveStart!=null)objectiveStart.Apply(state);
            GetComponent<PythonDispatchComparisonPanel>()?.Apply(state);
            GetComponent<PythonServiceAssignmentPanel>()?.Apply(state);
            GetComponent<PythonAutoPlayPanel>()?.Apply(state);
            GetComponent<PythonLegacySavePanel>()?.Apply(state);
            var missing=GetComponent<PythonMissingPanel>();if(missing!=null)missing.Apply(state);
            var adoption=GetComponent<PythonAdoptionPanel>();if(adoption!=null)adoption.Apply(state);
            var preparation=GetComponent<PythonPreparationPanel>();if(preparation!=null)preparation.Apply(state);
            var ended=GetComponent<PythonGameOverPanel>();if(ended!=null)ended.Apply(state);
            GetComponent<PythonBusinessHeader>()?.RefreshControls();
            PrioritizeRequiredPanel();
            var eventList=GetComponent<PythonEventListPanel>();if(eventList!=null)eventList.Apply(state);
            GetComponent<PythonMenuPanel>()?.Apply(state);
            GetComponent<PythonBusinessLogPanel>()?.Apply(state);
            GetComponent<PythonClearResultsPanel>()?.Apply(state);
            GetComponent<PythonPersistPanel>()?.Apply(state);GetComponent<PythonCatAcquisitionNavigation>()?.Refresh();
        }
        private void AppendObjectiveProgress()
        {
            if(pythonState != null && (pythonState.sidebar_progress ?? (CafeBackends.Standalone ? null : pythonState.objective_progress)) != null)
                description.text = "目標と進捗\n" + (pythonState.sidebar_progress ?? pythonState.objective_progress).details + "\n\n" + description.text;
        }
        public void SetServiceDescription(string text)
        {
            description.text = text;
            AppendObjectiveProgress();
            if (pythonState != null && !string.IsNullOrEmpty(pythonState.required_action))
                description.text += "\n\n進行に必要な操作\n" + pythonState.required_action;
        }

        public Texture2D PlayerTexture(string catId, string key, string imageKey = null, string eventKey = null)
        {
            var cat = bundle == null ? null : Array.Find(bundle.cats, item => item.cat_id == catId);
            if (cat == null) return CatPlaceholderTexture();
            string mode = string.IsNullOrEmpty(imageKey) ? key : imageKey.Split(new[] { "__" }, StringSplitOptions.None)[0];
            string fallback = mode == "brush" || mode == "pet" ? "pet"
                : mode == "teaser" || mode == "ball" || mode == "plush" || mode == "tunnel" ? "play" : "normal";
            if(eventKey != mode + "__open_up" && eventKey != mode + "__simultaneous")eventKey=null;
            foreach (var candidate in new[] { eventKey, imageKey, mode, key, fallback, "normal" })
            {
                var scene = Array.Find(cat.scenes, item => item.key == candidate);
                if (scene == null || string.IsNullOrEmpty(scene.image)) continue;
                var texture = Resources.Load<Texture2D>(resourceRoot + scene.image.Substring(0, scene.image.Length - 4));
                if (texture != null) return texture;
            }
            return CatPlaceholderTexture();
        }

        public static Texture2D CatPlaceholderTexture()
        {
            return Resources.Load<Texture2D>("CatCafeDefaults/cat-silhouette-v1");
        }

        public Texture2D CandidateCatTexture(string catId)
        {
            var content = bundle == null ? null : Array.Find(bundle.cats, item => item.cat_id == catId);
            var scene = content == null ? null : Array.Find(content.scenes, item => item.key == "normal");
            var texture = scene == null || string.IsNullOrEmpty(scene.image) ? null
                : Resources.Load<Texture2D>(resourceRoot + scene.image.Substring(0, scene.image.Length - 4));
            return texture ?? CatPlaceholderTexture();
        }

        public Texture2D PythonCatTexture(PythonCatState cat)
        {
            var content = bundle == null ? null : Array.Find(bundle.cats, item => item.cat_id == cat.content_cat_id);
            if (content == null) return CatPlaceholderTexture();
            string key = cat.working && cat.health_status == "healthy" && cat.activity == "cafe" ? "normal" : "sleep";
            var scene = Array.Find(content.scenes, item => item.key == key);
            var texture = scene == null || string.IsNullOrEmpty(scene.image) ? null
                : Resources.Load<Texture2D>(resourceRoot + scene.image.Substring(0, scene.image.Length - 4));
            return texture ?? CatPlaceholderTexture();
        }

        public void ConfigurePythonButtons()
        {
            var client = GetComponent<PythonStateClient>();
            for (int i = 0; i < sceneButtons.Length; i++)
            {
                sceneButtons[i].onClick = new Button.ButtonClickedEvent();
                sceneButtons[i].GetComponentInChildren<Text>().text = new[] { "出勤にする", "休養にする", "営業開始", "セーブ" }[i];
                sceneButtons[i].interactable = false;
            }
            sceneButtons[0].onClick.AddListener(client.WorkSelected);
            sceneButtons[1].onClick.AddListener(client.RestSelected);
            sceneButtons[2].onClick.AddListener(client.ProgressBusiness);
            sceneButtons[3].onClick.AddListener(client.SaveGame);
        }

        public void RefreshPythonControls()
        {
            GetComponent<PythonEventRecordPanel>()?.Refresh();
            GetComponent<PythonSaveBrowser>()?.Refresh();
            GetComponent<PythonCatAcquisitionNavigation>()?.Refresh();
            var service=GetComponent<PythonServiceScreen>();if(service!=null)service.RefreshControls();
            var navigation=GetComponent<PythonReturnToTitle>();if(navigation!=null)navigation.RefreshControls();
            var closing=GetComponent<PythonClosingPanel>();if(closing!=null)closing.RefreshControls();
            var player=GetComponent<PythonPlayerPanel>();if(player!=null)player.RefreshControls();
            var dispatch=GetComponent<PythonDispatchPanel>();if(dispatch!=null)dispatch.RefreshControls();
            var generalDispatch=GetComponent<PythonGeneralDispatchPanel>();if(generalDispatch!=null)generalDispatch.RefreshControls();
            var dispatchChoice=GetComponent<PythonDispatchChoicePanel>();if(dispatchChoice!=null)dispatchChoice.RefreshControls();
            var dispatchTrouble=GetComponent<PythonDispatchTroublePanel>();if(dispatchTrouble!=null)dispatchTrouble.RefreshControls();
            var dispatchIntroduction=GetComponent<PythonDispatchIntroductionPanel>();if(dispatchIntroduction!=null)dispatchIntroduction.RefreshControls();
            var items=GetComponent<PythonItemsPanel>();if(items!=null)items.RefreshControls();
            var dayOff=GetComponent<PythonDayOffPanel>();if(dayOff!=null)dayOff.RefreshControls();
            var expansion=GetComponent<PythonExpansionPanel>();if(expansion!=null)expansion.RefreshControls();
            var equipment=GetComponent<PythonSeatEquipmentPanel>();if(equipment!=null)equipment.RefreshControls();
            var restSpace=GetComponent<PythonRestSpacePanel>();if(restSpace!=null)restSpace.RefreshControls();
            var waitingArea=GetComponent<PythonWaitingAreaPanel>();if(waitingArea!=null)waitingArea.RefreshControls();
            var customerDirectory=GetComponent<PythonCustomerDirectoryPanel>();if(customerDirectory!=null)customerDirectory.RefreshControls();
            var financeHistory=GetComponent<PythonFinanceHistoryPanel>();if(financeHistory!=null)financeHistory.RefreshControls();
            var petShop=GetComponent<PythonPetShopPanel>();if(petShop!=null)petShop.RefreshControls();
            GetComponent<PythonRecruitmentPanel>()?.RefreshControls();
            var objectiveStart=GetComponent<PythonObjectiveStartPanel>();if(objectiveStart!=null)objectiveStart.RefreshControls();
            GetComponent<PythonDispatchComparisonPanel>()?.RefreshControls();
            GetComponent<PythonServiceAssignmentPanel>()?.RefreshControls();
            GetComponent<PythonAutoPlayPanel>()?.RefreshControls();
            GetComponent<PythonLegacySavePanel>()?.RefreshControls();
            var missing=GetComponent<PythonMissingPanel>();if(missing!=null)missing.RefreshControls();
            var adoption=GetComponent<PythonAdoptionPanel>();if(adoption!=null)adoption.RefreshControls();
            var preparation=GetComponent<PythonPreparationPanel>();if(preparation!=null)preparation.RefreshControls();
            var ended=GetComponent<PythonGameOverPanel>();if(ended!=null)ended.RefreshControls();
            var detail=GetComponent<PythonCatDetailsPanel>();if(detail!=null)detail.RefreshControls();
            var menu=GetComponent<PythonNewGamePanel>();if(menu!=null) menu.RefreshControls();
            var roster = GetComponent<PythonCatRoster>();
            if (roster != null) roster.RefreshControls();
            var client = GetComponent<PythonStateClient>();
            if (client == null || pythonState == null) return;
            var cat = Array.Find(VisiblePythonCats,row=>row.cat_id==SelectedPythonCatId);
            sceneButtons[0].interactable = client.CanChangeShifts && cat!=null && !cat.working && cat.health_status == "healthy" && cat.activity == "cafe";
            sceneButtons[1].interactable = client.CanChangeShifts && cat!=null && cat.working;
            sceneButtons[2].interactable = client.CanProgressBusiness;
            sceneButtons[2].GetComponentInChildren<Text>().text = pythonState.closed ? "翌日へ" : pythonState.can_set_shifts ? "営業開始" : "1刻み進める";
            sceneButtons[3].interactable = client.CanSaveNow;
            var savePanel = GetComponent<PythonSavePanel>();
            if(savePanel != null) savePanel.RefreshControls();
            var intake = GetComponent<PythonIntakePanel>();
            if (intake != null) intake.RefreshControls();
            var trust=GetComponent<PythonCustomerTrustPanel>();if(trust!=null)trust.RefreshControls();
            var reservation=GetComponent<PythonReservationPanel>();if(reservation!=null)reservation.RefreshControls();
            var regular=GetComponent<PythonRegularIntroductionPanel>();if(regular!=null)regular.RefreshControls();
            var eventList=GetComponent<PythonEventListPanel>();if(eventList!=null)eventList.RefreshControls();
            GetComponent<PythonMenuPanel>()?.RefreshControls();
            GetComponent<PythonBusinessLogPanel>()?.RefreshControls();
            GetComponent<PythonClearResultsPanel>()?.RefreshControls();
            GetComponent<PythonCatVisibility>()?.RefreshControls();
            GetComponent<PythonPersistPanel>()?.RefreshControls();
            var visiting=GetComponent<PythonVisitingCatPanel>();if(visiting!=null)visiting.RefreshControls();
            var housing = GetComponent<PythonHousingPanel>();
            if (housing != null) housing.RefreshControls();
            var goal = GetComponent<PythonGoalPanel>();
            if (goal != null) goal.RefreshControls();
            var store = GetComponent<PythonStoreEventPanel>();
            if (store != null) store.RefreshControls();
            var growth = GetComponent<PythonGrowthPanel>();
            if (growth != null) growth.RefreshControls();
            var auto = GetComponent<PythonAutoPanel>();
            if (auto != null) auto.RefreshControls();
            GetComponent<PythonBusinessHeader>()?.RefreshControls();
            PrioritizeRequiredPanel();
            GetComponent<PythonAutoPlayPanel>()?.Focus();
            GetComponent<PythonBusinessLogPanel>()?.Focus();
            GetComponent<PythonClearResultsPanel>()?.Focus();
            GetComponent<PythonFinanceHistoryPanel>()?.Focus();
            GetComponent<PythonPersistPanel>()?.Focus();
        }

        private void PrioritizeRequiredPanel()
        {
            var eventList=GetComponent<PythonEventListPanel>();if(eventList!=null && eventList.panel.activeSelf)return;
            var menu=GetComponent<PythonMenuPanel>();if(menu!=null && menu.panel.activeSelf)return;
            var client=GetComponent<PythonStateClient>();var state=client?.CurrentState;
            if(state==null || state.game_over!=null)return;
            var player=GetComponent<PythonPlayerPanel>();
            if(state.player_interaction!=null && player!=null && player.panel.activeSelf){player.panel.transform.SetAsLastSibling();return;}
            if(string.IsNullOrEmpty(state.required_action))return;
            var intake=GetComponent<PythonIntakePanel>();if(intake!=null && Front(intake.panel,intake.accept,intake.decline))return;
            var trust=GetComponent<PythonCustomerTrustPanel>();if(trust!=null && Front(trust.panel,trust.recover,trust.ignore,trust.confirm))return;
            var reservation=GetComponent<PythonReservationPanel>();if(reservation!=null && Front(reservation.panel,reservation.accept,reservation.decline,reservation.confirm))return;
            var dispatchIntroduction=GetComponent<PythonDispatchIntroductionPanel>();if(dispatchIntroduction!=null && Front(dispatchIntroduction.panel,dispatchIntroduction.accept,dispatchIntroduction.decline,dispatchIntroduction.confirm))return;
            var regular=GetComponent<PythonRegularIntroductionPanel>();if(regular!=null && Front(regular.panel,regular.accept,regular.decline,regular.confirm))return;
            var growth=GetComponent<PythonGrowthPanel>();if(growth!=null && Front(growth.panel,growth.choices))return;
            var store=GetComponent<PythonStoreEventPanel>();if(store!=null && Front(store.panel,store.choices))return;
            var goal=GetComponent<PythonGoalPanel>();if(goal!=null && Front(goal.panel,goal.resume,goal.advance))return;
            var adoption=GetComponent<PythonAdoptionPanel>();if(adoption!=null && Front(adoption.panel,adoption.accept,adoption.decline,adoption.confirmAccept))return;
            var missing=GetComponent<PythonMissingPanel>();if(missing!=null && Front(missing.panel,missing.confirm))return;
            var dispatch=GetComponent<PythonDispatchPanel>();if(dispatch!=null)Front(dispatch.panel,dispatch.receive);
            var generalDispatch=GetComponent<PythonGeneralDispatchPanel>();if(generalDispatch!=null)Front(generalDispatch.panel,generalDispatch.receive);
            var dispatchChoice=GetComponent<PythonDispatchChoicePanel>();if(dispatchChoice!=null)Front(dispatchChoice.panel,dispatchChoice.first,dispatchChoice.second,dispatchChoice.confirm);
            var dispatchTrouble=GetComponent<PythonDispatchTroublePanel>();if(dispatchTrouble!=null)Front(dispatchTrouble.panel,dispatchTrouble.first,dispatchTrouble.second,dispatchTrouble.confirm);
        }
        private static bool Front(GameObject panel,params Button[] buttons)
        {
            if(panel==null || !panel.activeSelf || buttons==null || !Array.Exists(buttons,b=>b!=null && b.gameObject.activeInHierarchy && b.interactable))return false;
            panel.transform.SetAsLastSibling();return true;
        }

        public void PythonNotice(string message)
        {
            GetComponent<PythonPersistPanel>()?.Notify(message);
            GetComponent<PythonSavePanel>()?.Notify(message);
            GetComponent<PythonLegacySavePanel>()?.Notify(message);
            GetComponent<PythonServiceAssignmentPanel>()?.Notify(message);
            GetComponent<PythonAutoPlayPanel>()?.Notify(message);
            var closing=GetComponent<PythonClosingPanel>();if(closing!=null && closing.panel.activeSelf)closing.Notify(message);
            var player=GetComponent<PythonPlayerPanel>();if(player!=null && player.panel.activeSelf)player.notice.text=message;
            var dispatch=GetComponent<PythonDispatchPanel>();if(dispatch!=null && dispatch.panel.activeSelf)dispatch.notice.text=message;
            var generalDispatch=GetComponent<PythonGeneralDispatchPanel>();if(generalDispatch!=null && generalDispatch.panel.activeSelf)generalDispatch.notice.text=message;
            var dispatchChoice=GetComponent<PythonDispatchChoicePanel>();if(dispatchChoice!=null && dispatchChoice.panel.activeSelf)dispatchChoice.notice.text=message;
            var dispatchTrouble=GetComponent<PythonDispatchTroublePanel>();if(dispatchTrouble!=null && dispatchTrouble.panel.activeSelf)dispatchTrouble.notice.text=message;
            var dispatchIntroduction=GetComponent<PythonDispatchIntroductionPanel>();if(dispatchIntroduction!=null && dispatchIntroduction.panel.activeSelf)dispatchIntroduction.notice.text=message;
            var items=GetComponent<PythonItemsPanel>();if(items!=null && items.panel.activeSelf)items.notice.text=message;
            var dayOff=GetComponent<PythonDayOffPanel>();if(dayOff!=null && dayOff.panel.activeSelf)dayOff.notice.text=message;
            var expansion=GetComponent<PythonExpansionPanel>();if(expansion!=null && expansion.panel.activeSelf)expansion.notice.text=message;
            var equipment=GetComponent<PythonSeatEquipmentPanel>();if(equipment!=null && equipment.panel.activeSelf)equipment.notice.text=message;
            var restSpace=GetComponent<PythonRestSpacePanel>();if(restSpace!=null && restSpace.panel.activeSelf)restSpace.notice.text=message;
            var waitingArea=GetComponent<PythonWaitingAreaPanel>();if(waitingArea!=null && waitingArea.panel.activeSelf)waitingArea.notice.text=message;
            var customerDirectory=GetComponent<PythonCustomerDirectoryPanel>();if(customerDirectory!=null && customerDirectory.panel.activeSelf)customerDirectory.notice.text=message;
            var petShop=GetComponent<PythonPetShopPanel>();if(petShop!=null && petShop.panel.activeSelf)petShop.notice.text=message;
            var recruitment=GetComponent<PythonRecruitmentPanel>();if(recruitment!=null && recruitment.panel.activeSelf)recruitment.notice.text=message;
            var objectiveStart=GetComponent<PythonObjectiveStartPanel>();if(objectiveStart!=null && objectiveStart.panel.activeSelf)objectiveStart.notice.text=message;
            var missing=GetComponent<PythonMissingPanel>();if(missing!=null && missing.panel.activeSelf && !string.IsNullOrEmpty(message))missing.notice.text=message;
            var adoption=GetComponent<PythonAdoptionPanel>();if(adoption!=null && adoption.panel.activeSelf)adoption.notice.text=message;
            var ended=GetComponent<PythonGameOverPanel>();if(ended!=null && ended.panel.activeSelf)ended.notice.text=message;
            var detail=GetComponent<PythonCatDetailsPanel>();if(detail!=null && detail.panel.activeSelf && detail.interactionNotice!=null)detail.interactionNotice.text=message;
            var menu=GetComponent<PythonNewGamePanel>();if(menu!=null && menu.panel.activeSelf) menu.notice.text=message;
            status.text = message + "\n" + status.text;
            var intake = GetComponent<PythonIntakePanel>();
            if (intake != null && intake.panel.activeSelf) intake.notice.text = message;
            var housing = GetComponent<PythonHousingPanel>();
            if (housing != null && housing.panel.activeSelf) housing.notice.text = message;
            var goal = GetComponent<PythonGoalPanel>();
            if (goal != null && goal.panel.activeSelf) goal.notice.text = message;
            var store = GetComponent<PythonStoreEventPanel>();
            if (store != null && store.panel.activeSelf) store.notice.text=message;
            var growth = GetComponent<PythonGrowthPanel>();
            if (growth != null && growth.panel.activeSelf) growth.notice.text=message;
        }

        public void RefreshCatVisibility()
        {
            if(pythonState==null)return;
            string previous=selectedPythonId;var visible=VisiblePythonCats;
            cats.ClearOptions();foreach(var cat in visible)cats.options.Add(new Dropdown.OptionData(cat.name));
            int index=Array.FindIndex(visible,cat=>cat.cat_id==previous);cats.SetValueWithoutNotify(Math.Max(0,index));cats.RefreshShownValue();cats.interactable=visible.Length>0;
            DisplayPythonCat(cats.value);GetComponent<PythonCatRoster>()?.Apply(pythonState);RefreshPythonControls();
        }
        private void DisplayPythonCat(int index)
        {
            var visible=VisiblePythonCats;
            if (visible.Length==0){selectedPythonId=null;description.text="表示できる猫がいません。「譲渡済みも表示」で過去の猫を確認できます。";picture.texture=null;picture.enabled=false;emptyImage.enabled=false;return;}
            if (index < 0 || index >= visible.Length) return;
            cats.SetValueWithoutNotify(index);
            var cat = visible[index];selectedPythonId=cat.cat_id;
            int assetIndex = bundle == null ? -1 : Array.FindIndex(bundle.cats, item => item.cat_id == cat.content_cat_id);
            if (assetIndex >= 0)
            {
                selected = assetIndex;
                ShowScene(cat.working && cat.health_status == "healthy" && cat.activity == "cafe" ? "normal" : "sleep");
            }
            else
            {
                picture.texture = CatPlaceholderTexture(); picture.enabled = picture.texture != null; emptyImage.enabled = picture.texture == null;
                if (picture.texture != null) aspect.aspectRatio = (float)picture.texture.width / picture.texture.height;
                description.text = cat.name + "\n対応する素材が未登録です。";
            }
            description.text += "\n\n猫の状態\n体力 " + cat.stamina + "/" + cat.max_stamina
                + "\n予定：" + (cat.activity != "cafe" ? "不在" : cat.working ? "出勤" : "休養") + "\n健康：" + PythonCatCard.Health(cat) + "\n活動：" + PythonCatCard.Activity(cat, pythonState.seats != null && Array.Exists(pythonState.seats, seat => seat.cat_id == cat.cat_id && !string.IsNullOrEmpty(seat.customer_id)))
                + "\n疲労 " + cat.fatigue + " / ストレス " + cat.stress;
            if (pythonState.seats != null)
            {
                description.text += "\n\n席の利用状況";
                foreach (var seat in pythonState.seats)
                {
                    var assigned = Array.Find(pythonState.cats, item => item.cat_id == seat.cat_id);
                    description.text += "\n" + seat.seat_id + "：" + (string.IsNullOrEmpty(seat.customer_id) ? "空席" : "接客中 / " + (assigned == null ? seat.cat_id : assigned.name));
                }
                description.text += "\n待機客 " + pythonState.waiting_count + "人 / 接客完了 " + pythonState.completed_interactions + "件";
            }
            if (pythonState.closed && pythonState.finance != null)
            {
                var finance = pythonState.finance;
                description.text += "\n\n閉店結果\n売上 " + pythonState.revenue + "\n総収入 " + finance.total_income
                    + "\n支出 " + finance.total_expenses + "\n収支 " + finance.net_cash_flow + "\n終了資金 " + finance.closing_funds
                    + "\n\n猫の状態";
                foreach (var item in pythonState.cats)
                    description.text += "\n" + item.name + "：体力 " + item.stamina + " / 疲労 " + item.fatigue + " / ストレス " + item.stress;
            }
            // The business sidebar is for goals and customer information, not the last selected cat.
            description.text="";
            AppendObjectiveProgress();
            GetComponent<PythonBusinessHeader>()?.RefreshControls();
            status.text = (CafeBackends.Standalone ? "営業中：" : "Python接続中：") + pythonState.day + "日目 / 時刻 " + pythonState.tick + "/" + pythonState.opening_ticks
                + " / 資金 " + pythonState.funds + (pythonState.closed ? " / 閉店" : pythonState.can_set_shifts ? " / 営業準備" : " / 営業中");
            if (!string.IsNullOrEmpty(pythonState.required_action))
                description.text += "\n\n進行に必要な操作\n" + pythonState.required_action;
            RefreshPythonControls();
        }

        public void PythonConnectionError(string message)
        {
            var presentation=GetComponent<PythonPresentation>();if(presentation!=null)presentation.Disconnect();
            var navigation=GetComponent<PythonReturnToTitle>();if(navigation!=null)navigation.RefreshControls();
            var closing=GetComponent<PythonClosingPanel>();if(closing!=null)closing.Disconnect();
            var menu=GetComponent<PythonNewGamePanel>();if(menu!=null) menu.Disconnect();
            var roster = GetComponent<PythonCatRoster>();
            if (roster != null) roster.Disconnect();
            var detail = GetComponent<PythonCatDetailsPanel>();
            if(detail != null) detail.Disconnect();
            var player=GetComponent<PythonPlayerPanel>();if(player!=null)player.Disconnect();
            var dispatch=GetComponent<PythonDispatchPanel>();if(dispatch!=null)dispatch.Disconnect();
            var generalDispatch=GetComponent<PythonGeneralDispatchPanel>();if(generalDispatch!=null)generalDispatch.Disconnect();
            var dispatchChoice=GetComponent<PythonDispatchChoicePanel>();if(dispatchChoice!=null)dispatchChoice.Disconnect();
            var dispatchTrouble=GetComponent<PythonDispatchTroublePanel>();if(dispatchTrouble!=null)dispatchTrouble.Disconnect();
            var dispatchIntroduction=GetComponent<PythonDispatchIntroductionPanel>();if(dispatchIntroduction!=null)dispatchIntroduction.Disconnect();
            var items=GetComponent<PythonItemsPanel>();if(items!=null)items.Disconnect();
            var dayOff=GetComponent<PythonDayOffPanel>();if(dayOff!=null)dayOff.Disconnect();
            var expansion=GetComponent<PythonExpansionPanel>();if(expansion!=null)expansion.Disconnect();
            var equipment=GetComponent<PythonSeatEquipmentPanel>();if(equipment!=null)equipment.Disconnect();
            var restSpace=GetComponent<PythonRestSpacePanel>();if(restSpace!=null)restSpace.Disconnect();
            var waitingArea=GetComponent<PythonWaitingAreaPanel>();if(waitingArea!=null)waitingArea.Disconnect();
            var customerDirectory=GetComponent<PythonCustomerDirectoryPanel>();if(customerDirectory!=null)customerDirectory.Disconnect();
            var financeHistory=GetComponent<PythonFinanceHistoryPanel>();if(financeHistory!=null)financeHistory.Disconnect();
            var petShop=GetComponent<PythonPetShopPanel>();if(petShop!=null)petShop.Disconnect();
            GetComponent<PythonRecruitmentPanel>()?.Disconnect();
            var objectiveStart=GetComponent<PythonObjectiveStartPanel>();if(objectiveStart!=null)objectiveStart.Disconnect();
            GetComponent<PythonDispatchComparisonPanel>()?.Disconnect();
            GetComponent<PythonServiceAssignmentPanel>()?.Disconnect();
            GetComponent<PythonAutoPlayPanel>()?.Disconnect();
            GetComponent<PythonBusinessLogPanel>()?.Disconnect();
            GetComponent<PythonClearResultsPanel>()?.Disconnect();
            GetComponent<PythonPersistPanel>()?.Disconnect();
            GetComponent<PythonLegacySavePanel>()?.Disconnect();
            var missing=GetComponent<PythonMissingPanel>();if(missing!=null)missing.Disconnect();
            var adoption=GetComponent<PythonAdoptionPanel>();if(adoption!=null)adoption.Disconnect();
            var preparation=GetComponent<PythonPreparationPanel>();if(preparation!=null)preparation.Disconnect();
            var ended=GetComponent<PythonGameOverPanel>();if(ended!=null)ended.Disconnect();
            var service = GetComponent<PythonServiceScreen>();
            if (service != null) service.Disconnect();
            var savePanel = GetComponent<PythonSavePanel>();
            if(savePanel != null) savePanel.Disconnect();
            var intake = GetComponent<PythonIntakePanel>();
            if (intake != null) intake.Disconnect();
            var trust=GetComponent<PythonCustomerTrustPanel>();if(trust!=null)trust.Disconnect();
            var reservation=GetComponent<PythonReservationPanel>();if(reservation!=null)reservation.Disconnect();
            var regular=GetComponent<PythonRegularIntroductionPanel>();if(regular!=null)regular.Disconnect();
            var eventList=GetComponent<PythonEventListPanel>();if(eventList!=null)eventList.Disconnect();
            GetComponent<PythonMenuPanel>()?.RefreshControls();
            GetComponent<PythonBusinessLogPanel>()?.RefreshControls();
            GetComponent<PythonClearResultsPanel>()?.RefreshControls();
            GetComponent<PythonCatVisibility>()?.RefreshControls();
            var visiting=GetComponent<PythonVisitingCatPanel>();if(visiting!=null)visiting.Disconnect();
            var housing = GetComponent<PythonHousingPanel>();
            if (housing != null) housing.Disconnect();
            var goal = GetComponent<PythonGoalPanel>();
            if (goal != null) goal.Disconnect();
            var store = GetComponent<PythonStoreEventPanel>();
            if (store != null) store.Disconnect();
            var growth = GetComponent<PythonGrowthPanel>();
            if (growth != null) growth.Disconnect();
            var auto = GetComponent<PythonAutoPanel>();
            if (auto != null) auto.RefreshControls();
            cats.interactable = false;
            foreach (var button in sceneButtons) button.interactable = false;
            var header=GetComponent<PythonBusinessHeader>();if(header!=null){header.Connected=false;header.RefreshControls();}GetComponent<PythonEventRecordPanel>()?.Refresh();
            GetComponent<PythonCatAcquisitionNavigation>()?.Refresh();
            GetComponent<PythonSaveBrowser>()?.Refresh();
            status.text = (CafeBackends.Standalone ? "状態取得エラー：" : "Python未接続：") + message;
            description.text = CafeBackends.Standalone ? "ゲーム状態を取得できません。最新状態の取得を再試行します。" : "Python状態サーバーを起動してください。接続は自動で再試行します。";
            picture.texture = null; picture.enabled = false; emptyImage.enabled = true;
        }
    }
}
