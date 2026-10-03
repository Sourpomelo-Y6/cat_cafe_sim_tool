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

        public string CurrentCatId { get { return bundle == null ? "" : bundle.cats[selected].cat_id; } }
        public string CurrentSceneKey { get { return sceneKey; } }
        public string DisplayText { get { return description == null ? "" : description.text; } }
        public bool HasImage { get { return picture != null && picture.texture != null; } }
        private void Start()
        {
            Initialize();
            if (PythonMode) return;
            var labels = new[] { "次の交流", "遊ぶ", "撫でる", "休む" };
            for (int i = 0; i < sceneButtons.Length; i++) sceneButtons[i].GetComponentInChildren<Text>().text = labels[i];
        }
        public void Initialize()
        {
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
            picture.texture = texture;
            picture.enabled = texture != null;
            emptyImage.enabled = texture == null;
            if (texture != null) aspect.aspectRatio = (float)texture.width / texture.height;
            status.text = cat.name + " / " + scene.label + (texture == null ? "：画像は未登録または読込失敗です。" : "：登録済み素材を表示中");
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
            string previous = pythonState == null || cats.value >= pythonState.cats.Length ? "" : pythonState.cats[cats.value].cat_id;
            pythonState = state;
            cats.options.Clear();
            foreach (var cat in state.cats) cats.options.Add(new Dropdown.OptionData(cat.name));
            int index = Array.FindIndex(state.cats, cat => cat.cat_id == previous);
            cats.SetValueWithoutNotify(Math.Max(0, index));
            cats.RefreshShownValue();
            cats.interactable = true;
            foreach (var button in sceneButtons) button.interactable = false;
            DisplayPythonCat(cats.value);
        }

        private void DisplayPythonCat(int index)
        {
            if (index < 0 || index >= pythonState.cats.Length) return;
            cats.SetValueWithoutNotify(index);
            var cat = pythonState.cats[index];
            int assetIndex = bundle == null ? -1 : Array.FindIndex(bundle.cats, item => item.cat_id == cat.content_cat_id);
            if (assetIndex >= 0)
            {
                selected = assetIndex;
                ShowScene(cat.working && cat.health_status == "healthy" && cat.activity == "cafe" ? "normal" : "sleep");
            }
            else
            {
                picture.texture = null; picture.enabled = false; emptyImage.enabled = true;
                description.text = cat.name + "\n対応する素材が未登録です。";
            }
            description.text += "\n\nPythonのゲーム状態\n体力 " + cat.stamina + "/" + cat.max_stamina
                + "\n予定：" + (cat.working ? "出勤" : "休養") + "\n健康：" + cat.health_status + "\n活動：" + cat.activity;
            status.text = "Python接続中：" + pythonState.day + "日目 / 時刻 " + pythonState.tick + " / 資金 " + pythonState.funds;
        }

        public void PythonConnectionError(string message)
        {
            cats.interactable = false;
            foreach (var button in sceneButtons) button.interactable = false;
            status.text = "Python未接続：" + message;
            description.text = "Python状態サーバーを起動してください。接続は自動で再試行します。";
            picture.texture = null; picture.enabled = false; emptyImage.enabled = true;
        }
    }
}
