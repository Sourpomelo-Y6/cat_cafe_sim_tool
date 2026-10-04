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
        public int Count { get { return cards.Count; } }
        public int CustomerCount { get { return cards.FindAll(card => card.customerIcon.activeSelf).Count; } }
        public PythonCatCard GetCard(int index) { return cards[index]; }
        public void Apply(PythonCafeState state)
        {
            if (content == null || cardPrefab == null) return;
            if (cards.Count == 0)
                cards.AddRange(content.GetComponentsInChildren<PythonCatCard>(true));
            while (cards.Count < state.cats.Length) cards.Add(Instantiate(cardPrefab, content));
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].gameObject.SetActive(i < state.cats.Length);
                if (i < state.cats.Length) cards[i].Set(state.cats[i], state, GetComponent<ContentPreview>(), GetComponent<PythonStateClient>());
            }
        }
        public void RefreshControls() { foreach (var card in cards) card.Refresh(GetComponent<PythonStateClient>()); }
        public void Disconnect()
        {
            foreach (var card in content.GetComponentsInChildren<PythonCatCard>(true))
            {
                card.picture.texture = null; card.picture.enabled = false;
                card.customerIcon.SetActive(false); card.work.interactable = card.rest.interactable = false;
                card.stateText.text = "未接続";
            }
        }
    }
}
