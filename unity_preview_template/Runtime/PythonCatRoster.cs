using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonCatRoster : MonoBehaviour
    {
        public RectTransform content;
        public PythonCatCard cardPrefab;
        private readonly List<PythonCatCard> cards = new List<PythonCatCard>();
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
            if (cards.Count == 0)
                cards.AddRange(content.GetComponentsInChildren<PythonCatCard>(true));
            var rows=GetComponent<PythonCatVisibility>()?.Filter(state.cats) ?? state.cats;
            while (cards.Count < rows.Length) cards.Add(Instantiate(cardPrefab, content));
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].gameObject.SetActive(i < rows.Length);
                if (i < rows.Length) cards[i].Set(rows[i], state, GetComponent<ContentPreview>(), GetComponent<PythonStateClient>());
            }
        }
        public void RefreshControls() { foreach (var card in cards) card.Refresh(GetComponent<PythonStateClient>()); }
        public void Disconnect()
        {
            foreach (var card in content.GetComponentsInChildren<PythonCatCard>(true))
            {
                card.picture.texture = null; card.picture.enabled = false;
                card.customerIcon.SetActive(false); card.work.interactable = card.rest.interactable = false;
                if(card.details != null) card.details.interactable=false;
                card.stateText.text = "未接続";
            }
        }
    }
}
