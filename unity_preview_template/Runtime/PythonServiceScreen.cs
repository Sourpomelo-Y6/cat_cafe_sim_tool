using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonServiceScreen : MonoBehaviour
    {
        public GameObject rosterPanel, seatPanel;
        public RectTransform seatContent;
        public PythonCatCard cardPrefab;
        public Text heading;
        public Button seatsTab, catsTab;
        private PythonCafeState state;
        private string phase;
        private bool showSeats;
        private readonly List<PythonCatCard> cards = new List<PythonCatCard>();
        public bool ShowingSeats { get { return showSeats; } }
        public int OccupiedCount { get { return cards.FindAll(card=>card.gameObject.activeSelf && card.customerIcon.activeSelf).Count; } }
        public void ShowSeats() { showSeats = true; Render(); }
        public void ShowCats() { showSeats = false; Render(); }
        public void Apply(PythonCafeState value)
        {
            state = value;
            string next = value.closed ? "closed" : value.can_set_shifts ? "preparation" : "open";
            if(phase != next) { phase = next; showSeats = next == "open"; }
            Render();
        }
        private void Render()
        {
            if(state == null) return;
            seatsTab.interactable = catsTab.interactable = true;
            seatsTab.gameObject.SetActive(phase == "open"); catsTab.gameObject.SetActive(phase == "open");
            rosterPanel.SetActive(!showSeats); seatPanel.SetActive(showSeats);
            heading.text = phase == "closed" ? "閉店結果・猫の状態" : showSeats ? "席一覧　　人物アイコン：接客中のお客さん" : "猫一覧";
            if(!showSeats)
            {
                if(phase == "open") GetComponent<ContentPreview>().SetServiceDescription("猫の状態\n\nカードで全員の体力と活動を確認できます。\n「席一覧」で接客状況へ戻れます。");
                return;
            }
            if(cards.Count == 0) cards.AddRange(seatContent.GetComponentsInChildren<PythonCatCard>(true));
            var seats = state.seats ?? new PythonSeatState[0];
            while(cards.Count < seats.Length) cards.Add(Instantiate(cardPrefab,seatContent));
            for(int i=0;i<cards.Count;i++)
            {
                var card = cards[i]; card.gameObject.SetActive(i<seats.Length); if(i>=seats.Length) continue;
                var seat = seats[i]; bool occupied = !string.IsNullOrEmpty(seat.customer_id);
                var cat = Array.Find(state.cats,value=>value.cat_id == seat.cat_id);
                card.catName.text = "席 " + (i+1) + (occupied ? "　接客中" : "　空席");
                card.picture.texture = cat == null ? null : GetComponent<ContentPreview>().PythonCatTexture(cat);
                card.picture.enabled = card.picture.texture != null;
                card.customerIcon.SetActive(occupied); card.work.gameObject.SetActive(false); card.rest.gameObject.SetActive(false);
                card.stateText.text = occupied ? (cat == null ? "担当なし" : cat.name) + " / " + seat.customer_name : "お客さんを待っています";
            }
            string text = "来店客一覧\n\n";
            var customers = state.customers ?? new PythonCustomerState[0];
            if(customers.Length == 0) text += "まだお客さんは来ていません。";
            foreach(var customer in customers)
            {
                string label = customer.status == "seated" ? "接客中" : customer.status == "departed" ? "退店済み" : "待機中";
                int index = Array.FindIndex(seats,seat=>seat.seat_id == customer.seat_id);
                text += customer.name + "　" + label + (index >= 0 ? " / 席 " + (index+1) : "") + "\n\n";
            }
            GetComponent<ContentPreview>().SetServiceDescription(text);
        }
        public void Disconnect()
        {
            state = null;
            foreach(var card in seatContent.GetComponentsInChildren<PythonCatCard>(true))
            {
                card.picture.texture = null; card.picture.enabled = false; card.customerIcon.SetActive(false); card.stateText.text = "未接続";
            }
            seatsTab.interactable = catsTab.interactable = false;
        }
    }
}
