using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class ContentPreview : MonoBehaviour
    {
        private ContentBundle bundle;
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
        private int selected;
        private string sceneKey = "normal";

        public string CurrentCatId { get { return bundle == null ? "" : bundle.cats[selected].cat_id; } }
        public string CurrentSceneKey { get { return sceneKey; } }
        public string DisplayText { get { return description == null ? "" : description.text; } }
        public bool HasImage { get { return picture != null && picture.texture != null; } }
        private void Start() { Initialize(); }
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
                resourceRoot = catalog.bundleResource.Substring(0, catalog.bundleResource.Length - "bundle".Length);
                cats.options.Clear();
                foreach (var cat in bundle.cats) cats.options.Add(new Dropdown.OptionData(cat.name + "（" + cat.cat_id + "）"));
                cats.SetValueWithoutNotify(0);
                cats.RefreshShownValue();
                cats.interactable = true;
                foreach (var button in sceneButtons) button.interactable = true;
                ShowScene("normal");
            }
            catch (Exception ex) { bundle = null; status.text = ex.Message; description.text = "制作ツールで採用済み素材を書き出して取り込んでください。"; }
        }

        public void ShowScene(string key)
        {
            sceneKey = key;
            if (bundle == null) return;
            var cat = bundle.cats[selected];
            var scene = Array.Find(cat.scenes, value => value.key == key);
            if (scene == null) return;
            description.text = cat.name + "\n\n" + cat.introduction + "\n\n" + scene.label + "\n" + scene.text;
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
            if (bundle == null || index < 0 || index >= bundle.cats.Length) throw new ArgumentOutOfRangeException("index");
            cats.SetValueWithoutNotify(index);
            selected = index;
            ShowScene(sceneKey);
        }

    }
}
