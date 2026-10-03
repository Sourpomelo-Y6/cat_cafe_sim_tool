using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class ContentPreview : MonoBehaviour
    {
        private ContentBundle bundle;
        private string resourceRoot;
        private Font font;
        private Dropdown cats;
        private RawImage picture;
        private AspectRatioFitter aspect;
        private Text description;
        private Text status;
        private Text emptyImage;
        private ScrollRect scroll;
        private int selected;
        private string sceneKey = "normal";

        public string CurrentCatId { get { return bundle == null ? "" : bundle.cats[selected].cat_id; } }
        public string CurrentSceneKey { get { return sceneKey; } }
        public string DisplayText { get { return description == null ? "" : description.text; } }
        public bool HasImage { get { return picture != null && picture.texture != null; } }
        private void Start() { Initialize(); }
        public void Initialize()
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "MS Gothic" }, 20);
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            BuildUI();
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
                cats.RefreshShownValue();
                cats.interactable = true;
                cats.onValueChanged.AddListener(index => { selected = index; ShowScene(sceneKey); });
                ShowScene("normal");
            }
            catch (Exception ex) { status.text = ex.Message; description.text = "制作ツールで採用済み素材を書き出して取り込んでください。"; }
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
            cats.value = index;
            selected = index;
            ShowScene(sceneKey);
        }

        private void BuildUI()
        {
            if (Camera.main == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.96f, 0.95f, 0.91f);
            }
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var canvasObject = new GameObject("Content Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800);
            scaler.matchWidthOrHeight = 0.5f;
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvas.transform, false);
            Stretch(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = new Color(0.96f, 0.95f, 0.91f);
            var root = background.transform;
            Label(root, "猫カフェ 素材プレビュー", 24, 20, 1200, 40, 28);
            var dropdown = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            dropdown.name = "Cat Selection";
            dropdown.transform.SetParent(root, false);
            Position(dropdown.GetComponent<RectTransform>(), 24, 76, 740, 38);
            cats = dropdown.GetComponent<Dropdown>();
            cats.interactable = false;
            cats.options.Clear();
            cats.options.Add(new Dropdown.OptionData("素材を取り込んでください"));
            cats.RefreshShownValue();
            foreach (var text in dropdown.GetComponentsInChildren<Text>(true)) { text.font = font; text.fontSize = 18; }
            Label(dropdown.transform, "▼", 708, 6, 24, 26, 16);
            var names = new[] { "通常", "遊ぶ", "撫でられる", "眠る" };
            for (int index = 0; index < names.Length; index++)
            {
                string key = ContentValidation.SceneKeys[index];
                var button = DefaultControls.CreateButton(new DefaultControls.Resources());
                button.transform.SetParent(root, false);
                Position(button.GetComponent<RectTransform>(), 24 + index * 180, 128, 166, 40);
                var text = button.GetComponentInChildren<Text>();
                text.font = font; text.fontSize = 20; text.text = names[index];
                button.GetComponent<Button>().onClick.AddListener(() => ShowScene(key));
            }
            var imagePanel = new GameObject("Image Panel", typeof(RectTransform), typeof(Image));
            imagePanel.transform.SetParent(root, false);
            Position(imagePanel.GetComponent<RectTransform>(), 24, 188, 750, 550);
            imagePanel.GetComponent<Image>().color = new Color(0.89f, 0.9f, 0.86f);
            var pictureObject = new GameObject("Cat Image", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            pictureObject.transform.SetParent(imagePanel.transform, false);
            Stretch(pictureObject.GetComponent<RectTransform>());
            picture = pictureObject.GetComponent<RawImage>();
            picture.enabled = false;
            aspect = pictureObject.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            emptyImage = Label(imagePanel.transform, "この場面の画像は未登録です", 20, 240, 710, 70, 22);
            emptyImage.alignment = TextAnchor.MiddleCenter;
            var scrollObject = new GameObject("Description Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(root, false);
            Position(scrollObject.GetComponent<RectTransform>(), 804, 188, 452, 550);
            scrollObject.GetComponent<Image>().color = Color.white;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObject.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            description = Label(viewport.transform, "", 12, 12, 428, 526, 22);
            var rect = description.rectTransform;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(-24, 0);
            rect.anchoredPosition = new Vector2(12, -12);
            description.verticalOverflow = VerticalWrapMode.Overflow;
            var fitter = description.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = rect; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            status = Label(root, "素材を読み込んでいます…", 24, 752, 1232, 36, 18);
        }

        private Text Label(Transform parent, string value, float x, float y, float width, float height, int size)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Position(obj.GetComponent<RectTransform>(), x, y, width, height);
            var text = obj.GetComponent<Text>();
            text.font = font; text.fontSize = size; text.text = value;
            text.supportRichText = false;
            text.color = new Color(0.16f, 0.21f, 0.18f); text.raycastTarget = false;
            return text;
        }
        private static void Position(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
