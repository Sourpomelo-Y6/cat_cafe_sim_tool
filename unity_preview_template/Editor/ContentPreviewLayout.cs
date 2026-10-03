using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatCafePreview.Editor
{
    public static class ContentPreviewLayout
    {
        private static Font font;
        public static Font LoadFont()
        {
            const string path = "Assets/CatCafePreview/Fonts/MPLUS1p-Regular.ttf";
            var asset = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (asset != null) return asset;
            throw new ArgumentException("同梱フォントを配置してください: " + path);
        }
        private static void Bind(SerializedObject serialized, string name, UnityEngine.Object value)
        {
            serialized.FindProperty(name).objectReferenceValue = value;
        }
        public static void ConfigureCatDropdown(Dropdown dropdown)
        {
            var label = dropdown.itemText;
            if (label == null || dropdown.template == null) throw new ArgumentException("Dropdown item template is missing.");
            var item = label.GetComponentInParent<Toggle>(true).GetComponent<RectTransform>();
            float height = Mathf.Max(40, item.sizeDelta.y, label.preferredHeight + 8);
            item.sizeDelta = new Vector2(item.sizeDelta.x, height);
            var content = item.parent.GetComponent<RectTransform>();
            content.sizeDelta = new Vector2(content.sizeDelta.x, height + 8);
            label.rectTransform.offsetMin = new Vector2(24, 4);
            label.rectTransform.offsetMax = new Vector2(-12, -4);
            label.alignment = TextAnchor.MiddleLeft;
            label.supportRichText = false;
            dropdown.template.sizeDelta = new Vector2(dropdown.template.sizeDelta.x, Mathf.Max(220, dropdown.template.sizeDelta.y));
        }
        public static void Build(ContentPreview preview)
        {
            font = LoadFont();
            if (Camera.main == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera));
                cameraObject.transform.SetParent(preview.transform, false);
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.96f, 0.95f, 0.91f);
            }
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(preview.transform, false);
            var canvasObject = new GameObject("Content Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(preview.transform, false);
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
            var cats = dropdown.GetComponent<Dropdown>();
            cats.interactable = false;
            cats.options.Clear();
            cats.options.Add(new Dropdown.OptionData("素材を取り込んでください"));
            cats.RefreshShownValue();
            foreach (var text in dropdown.GetComponentsInChildren<Text>(true)) { text.font = font; text.fontSize = 18; }
            ConfigureCatDropdown(cats);
            Label(dropdown.transform, "▼", 708, 6, 24, 26, 16);
            var names = new[] { "通常", "遊ぶ", "撫でられる", "眠る" };
            var buttons = new Button[4];
            for (int index = 0; index < names.Length; index++)
            {
                string key = ContentValidation.SceneKeys[index];
                var button = DefaultControls.CreateButton(new DefaultControls.Resources());
                button.name = "Scene " + key;
                button.transform.SetParent(root, false);
                Position(button.GetComponent<RectTransform>(), 24 + index * 180, 128, 166, 40);
                var text = button.GetComponentInChildren<Text>();
                text.font = font; text.fontSize = 20; text.text = names[index];
                buttons[index] = button.GetComponent<Button>();
                UnityEventTools.AddStringPersistentListener(buttons[index].onClick, preview.ShowScene, key);
            }
            var imagePanel = new GameObject("Image Panel", typeof(RectTransform), typeof(Image));
            imagePanel.transform.SetParent(root, false);
            Position(imagePanel.GetComponent<RectTransform>(), 24, 188, 750, 550);
            imagePanel.GetComponent<Image>().color = new Color(0.89f, 0.9f, 0.86f);
            var pictureObject = new GameObject("Cat Image", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            pictureObject.transform.SetParent(imagePanel.transform, false);
            Stretch(pictureObject.GetComponent<RectTransform>());
            var picture = pictureObject.GetComponent<RawImage>();
            picture.enabled = false;
            var aspect = pictureObject.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var emptyImage = Label(imagePanel.transform, "この場面の画像は未登録です", 20, 240, 710, 70, 22);
            emptyImage.alignment = TextAnchor.MiddleCenter;
            var scrollObject = new GameObject("Description Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(root, false);
            Position(scrollObject.GetComponent<RectTransform>(), 804, 188, 452, 550);
            scrollObject.GetComponent<Image>().color = Color.white;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObject.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            var description = Label(viewport.transform, "", 12, 12, 428, 526, 22);
            var rect = description.rectTransform;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(-24, 0);
            rect.anchoredPosition = new Vector2(12, -12);
            description.verticalOverflow = VerticalWrapMode.Overflow;
            var fitter = description.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = rect; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var status = Label(root, "編集画面で配置を調整できます。Playで素材を表示します。", 24, 752, 1232, 36, 18);
            UnityEventTools.AddPersistentListener(cats.onValueChanged, preview.SelectCat);
            var serialized = new SerializedObject(preview);
            Bind(serialized, "cats", cats);
            Bind(serialized, "picture", picture);
            Bind(serialized, "aspect", aspect);
            Bind(serialized, "description", description);
            Bind(serialized, "status", status);
            Bind(serialized, "emptyImage", emptyImage);
            Bind(serialized, "scroll", scroll);
            var array = serialized.FindProperty("sceneButtons");
            array.arraySize = buttons.Length;
            for (int index = 0; index < buttons.Length; index++) array.GetArrayElementAtIndex(index).objectReferenceValue = buttons[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Text Label(Transform parent, string value, float x, float y, float width, float height, int size)
        {
            var obj = new GameObject(value == "" ? "Description" : value.Substring(0, Math.Min(value.Length, 20)), typeof(RectTransform), typeof(Text));
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
