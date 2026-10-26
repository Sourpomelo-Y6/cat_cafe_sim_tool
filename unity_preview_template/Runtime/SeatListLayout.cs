using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    // Shared by the live screen and both editor regeneration paths.
    public static class SeatListLayout
    {
        public static readonly Vector2 CardSize = new Vector2(230, 350);

        public static ScrollRect Configure(PythonServiceScreen service)
        {
            var panel = service.seatPanel.GetComponent<RectTransform>();
            Place(panel, 24, 262, 966, 514);
            var scroll = panel.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 40;
            Place(scroll.viewport, 0, 26, 944, 488);
            // Preserve the current offset when upgrading an existing live screen.
            var offset = service.seatContent.anchoredPosition.y;
            Place(service.seatContent, 0, 0, 944, 0);
            service.seatContent.anchoredPosition = new Vector2(0, offset);
            var grid = service.seatContent.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = CardSize;
            grid.spacing = new Vector2(8, 8);
            grid.padding = new RectOffset();
            service.seatContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var hint = panel.Find("Seat Scroll Help");
            if (hint == null)
            {
                var go = new GameObject("Seat Scroll Help", typeof(RectTransform), typeof(Text));
                go.transform.SetParent(panel, false);
                hint = go.transform;
            }
            Place((RectTransform)hint, 0, 0, 944, 24);
            var text = hint.GetComponent<Text>();
            text.font = service.heading.font;
            text.fontSize = 16;
            text.color = new Color(.16f, .21f, .18f);
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            text.supportRichText = false;

            var bar = panel.Find("Seat Scrollbar");
            if (bar == null)
            {
                var go = DefaultControls.CreateScrollbar(new DefaultControls.Resources());
                go.name = "Seat Scrollbar";
                go.transform.SetParent(panel, false);
                bar = go.transform;
            }
            Place((RectTransform)bar, 950, 26, 16, 488);
            var scrollbar = bar.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.GetComponent<Image>().color = new Color(.77f, .81f, .75f);
            scrollbar.targetGraphic.color = new Color(.28f, .43f, .34f);
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }

        public static void SetHelp(PythonServiceScreen service, int count)
        {
            service.seatPanel.transform.Find("Seat Scroll Help").GetComponent<Text>().text =
                "席一覧：全" + count + "席" + (count > 4 ? "　下の席へ：ホイール ／ 右のバーをドラッグ" : "");
        }

        public static void StyleCard(PythonCatCard card)
        {
            Place(card.stateText.rectTransform, 10, 146, 210, 164);
            card.stateText.fontSize = 15;
            card.stateText.resizeTextForBestFit = false;
            card.stateText.lineSpacing = 1;
            card.stateText.horizontalOverflow = HorizontalWrapMode.Wrap;
            card.stateText.verticalOverflow = VerticalWrapMode.Truncate;
            card.stateText.supportRichText = false;
            if (card.details != null) Place(card.details.GetComponent<RectTransform>(), 10, 314, 210, 30);
        }

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
