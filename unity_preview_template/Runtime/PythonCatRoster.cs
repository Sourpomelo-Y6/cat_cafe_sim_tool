using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonCatRoster : MonoBehaviour
    {
        public RectTransform content;
        public PythonCatCard cardPrefab;
        private readonly List<PythonCatCard> cards = new List<PythonCatCard>();
        private GameObject markLegend;
        private Button allCats, careCats, returnToAll;
        private GameObject emptyNotice;
        private PythonCafeState current;
        private string instance;
        private bool careOnly, online;
        public bool CareOnly => careOnly;
        public Button AllCatsButton => allCats;
        public Button CareCatsButton => careCats;
        public Button ReturnToAllButton => returnToAll;

        public sealed class Position
        {
            internal string instance;
            internal string[] ids;
            internal int index;
            internal float offset;
        }

        public Position RememberPosition(string catId = null)
        {
            var active = cards.Where(c => c.gameObject.activeSelf && c.state != null).ToArray();
            if (active.Length == 0) return null;
            int index = Array.FindIndex(active, c => c.state.cat_id == catId);
            if (index < 0)
                index = Array.FindIndex(active, c => -((RectTransform)c.transform).anchoredPosition.y + ((RectTransform)c.transform).rect.height > content.anchoredPosition.y);
            if (index < 0) index = active.Length - 1;
            return new Position
            {
                instance = this.instance,
                ids = active.Select(c => c.state.cat_id).ToArray(),
                index = index,
                offset = -((RectTransform)active[index].transform).anchoredPosition.y - content.anchoredPosition.y
            };
        }

        public void RestorePosition(Position position)
        {
            if (position == null || position.instance != instance) return;
            var active = cards.Where(c => c.gameObject.activeSelf && c.state != null).ToArray();
            string target = position.ids.Skip(position.index).FirstOrDefault(id => active.Any(c => c.state.cat_id == id))
                ?? position.ids.Take(position.index).Reverse().FirstOrDefault(id => active.Any(c => c.state.cat_id == id));
            var scroll = content.GetComponentInParent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scroll.StopMovement();
            var card = active.FirstOrDefault(c => c.state.cat_id == target);
            float y = card == null ? 0 : -((RectTransform)card.transform).anchoredPosition.y - position.offset;
            y = Mathf.Clamp(y, 0, Mathf.Max(0, content.rect.height - scroll.viewport.rect.height));
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
        }

        public void ChangeCareFilter(bool value)
        {
            if (!online || careOnly == value) return;
            careOnly = value;
            Apply(current);
            var scroll = content.GetComponentInParent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1;
        }

        private Button FilterButton(Transform parent, string name, string caption, float x, float y, float width, UnityEngine.Events.UnityAction action)
        {
            var button = Instantiate(cardPrefab.work, parent);
            button.name = name;
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, 32);
            var text = button.GetComponentInChildren<Text>();
            text.text = caption;
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
            return button;
        }

        private void EnsureFilterControls(ScrollRect scroll)
        {
            if (allCats != null) return;
            allCats = FilterButton(scroll.transform, "All Cats Filter", "全員", 10, 0, 180, () => ChangeCareFilter(false));
            careCats = FilterButton(scroll.transform, "Care Cats Filter", "ケア対象", 198, 0, 180, () => ChangeCareFilter(true));
            emptyNotice = new GameObject("No Care Cats", typeof(RectTransform));
            var rect = (RectTransform)emptyNotice.transform;
            rect.SetParent(scroll.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -80);
            rect.sizeDelta = new Vector2(500, 100);
            var message = new GameObject("Message", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            message.transform.SetParent(rect, false);
            message.rectTransform.anchorMin = message.rectTransform.anchorMax = message.rectTransform.pivot = new Vector2(0, 1);
            message.rectTransform.sizeDelta = new Vector2(500, 36);
            message.font = cardPrefab.catName.font;
            message.fontSize = 18;
            message.color = new Color(.16f,.21f,.18f);
            message.raycastTarget = false;
            message.text = "ケア対象の猫はいません";
            returnToAll = FilterButton(rect, "Return To All Cats", "全員表示へ戻る", 0, -44, 220, () => ChangeCareFilter(false));
            emptyNotice.SetActive(false);
        }

        public void LayoutMarkLegend(ScrollRect scroll, float height)
        {
            const float legendHeight = 60;
            EnsureFilterControls(scroll);
            var viewport = scroll.viewport;
            viewport.anchoredPosition = new Vector2(viewport.anchoredPosition.x, -legendHeight);
            viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - legendHeight);
            if (markLegend == null)
            {
                markLegend = new GameObject("Roster State Legend", typeof(RectTransform));
                var legendRect = (RectTransform)markLegend.transform;
                legendRect.SetParent(scroll.transform, false);
                legendRect.anchorMin = legendRect.anchorMax = legendRect.pivot = new Vector2(0, 1);
                legendRect.anchoredPosition = new Vector2(10, -36);
                legendRect.sizeDelta = new Vector2(390, 24);
                for (int kind = 1; kind <= 3; kind++)
                {
                    float x = (kind - 1) * 130;
                    CatStateMarkGraphic.Create(legendRect, "Legend Mark " + kind, new Vector2(x, -3), 18).ShowKind(kind);
                    var caption = new GameObject("Legend Caption " + kind, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                    var rect = caption.rectTransform;
                    rect.SetParent(legendRect, false);
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                    rect.anchoredPosition = new Vector2(x + 22, 0);
                    rect.sizeDelta = new Vector2(108, 24);
                    caption.font = cardPrefab.catName.font;
                    caption.fontSize = 14;
                    caption.color = new Color(.16f,.21f,.18f);
                    caption.alignment = TextAnchor.MiddleLeft;
                    caption.raycastTarget = false;
                    caption.text = CatStateMark.Caption(kind);
                }
            }
            markLegend.SetActive(true);
        }
        private void EnsureScrollNavigation()
        {
            var scroll=content.GetComponentInParent<ScrollRect>();if(scroll==null)return;
            scroll.scrollSensitivity=28;
            if(scroll.verticalScrollbar!=null)return;
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.name="Cat Roster Scrollbar";bar.transform.SetParent(scroll.transform,false);
            var rect=bar.GetComponent<RectTransform>();rect.anchorMin=new Vector2(1,0);rect.anchorMax=new Vector2(1,1);rect.offsetMin=new Vector2(4,0);rect.offsetMax=new Vector2(18,0);
            var control=bar.GetComponent<Scrollbar>();control.direction=Scrollbar.Direction.BottomToTop;bar.GetComponent<Image>().color=new Color(.91f,.90f,.84f);control.targetGraphic.color=new Color(.58f,.64f,.51f);
            scroll.verticalScrollbar=control;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        }
        public int Count { get { return cards.FindAll(card=>card.gameObject.activeSelf).Count; } }
        public int CustomerCount { get { return cards.FindAll(card => card.gameObject.activeSelf && card.customerIcon.activeSelf).Count; } }
        public PythonCatCard GetCard(int index) { return cards.FindAll(card=>card.gameObject.activeSelf)[index]; }
        public void Apply(PythonCafeState state)
        {
            if (content == null || cardPrefab == null) return;
            EnsureScrollNavigation();
            var scroll = content.GetComponentInParent<ScrollRect>();
            EnsureFilterControls(scroll);
            if (cards.Count == 0)
                cards.AddRange(content.GetComponentsInChildren<PythonCatCard>(true));
            var position = RememberPosition();
            bool newSession = instance != state.instance_id;
            if (newSession) careOnly = false;
            instance = state.instance_id;
            current = state;
            online = true;
            var all = GetComponent<PythonCatVisibility>()?.Filter(state.cats) ?? state.cats;
            var care = all.Where(cat => { int kind = CatStateMark.Resolve(cat); return kind == 1 || kind == 2; }).ToArray();
            var rows = careOnly ? care : all;
            bool changed = !cards.Where(c => c.gameObject.activeSelf).Select(c => c.state?.cat_id).SequenceEqual(rows.Select(c => c.cat_id));
            while (cards.Count < rows.Length) cards.Add(Instantiate(cardPrefab, content));
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].gameObject.SetActive(i < rows.Length);
                if (i < rows.Length) cards[i].Set(rows[i], state, GetComponent<ContentPreview>(), GetComponent<PythonStateClient>());
            }
            allCats.GetComponentInChildren<Text>().text = "全員（" + all.Length + "）";
            careCats.GetComponentInChildren<Text>().text = "ケア対象（" + care.Length + "）";
            emptyNotice.SetActive(careOnly && rows.Length == 0);
            RefreshControls();
            if (changed && !newSession) RestorePosition(position);
            if (newSession) scroll.verticalNormalizedPosition = 1;
        }
        public void RefreshControls()
        {
            foreach (var card in cards) card.Refresh(GetComponent<PythonStateClient>());
            if (allCats == null) return;
            allCats.interactable = online && careOnly;
            careCats.interactable = online && !careOnly;
            returnToAll.interactable = online;
        }
        public void Disconnect()
        {
            online = false;
            RefreshControls();
            if (emptyNotice != null) emptyNotice.SetActive(false);
            if (markLegend != null) markLegend.SetActive(false);
            foreach (var card in content.GetComponentsInChildren<PythonCatCard>(true))
            {
                card.picture.texture = null; card.picture.enabled = false;
                card.customerIcon.SetActive(false); card.work.interactable = card.rest.interactable = false;
                if(card.details != null) card.details.interactable=false;
                card.stateText.text = "未接続";
                card.ClearRosterMark();
            }
        }
    }
}
