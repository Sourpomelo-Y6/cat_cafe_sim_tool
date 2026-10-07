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
        public Text heading, serviceSummary;
        public GameObject serviceStatus;
        public Slider dayProgress;
        public Button seatsTab, catsTab;
        public RectTransform detailPanel;
        public Text detailText;
        private PythonCafeState state;
        private string phase;
        private bool showSeats;
        private readonly List<PythonCatCard> cards = new List<PythonCatCard>();
        public bool ShowingSeats { get { return showSeats; } }
        public int OccupiedCount { get { return cards.FindAll(card=>card.gameObject.activeSelf && card.customerIcon.activeSelf).Count; } }
        public PythonCatCard GetSeatCard(int index) { return cards[index]; }
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
            if(serviceStatus!=null)serviceStatus.SetActive(showSeats);
            if(detailPanel != null)
            {
                detailPanel.anchoredPosition = new Vector2(showSeats ? 992 : 804,showSeats ? -262 : -188);
                detailPanel.sizeDelta = new Vector2(showSeats ? 264 : 452,showSeats ? 514 : 550);
                detailText.fontSize = showSeats ? 16 : 22;
            }
            heading.text = phase == "closed" ? "閉店結果・猫の状態" : showSeats ? "席一覧　　人物アイコン：接客中のお客さん" : "猫一覧";
            if(state.objective_progress != null)
            {
                heading.fontSize=16;heading.lineSpacing=1;
                heading.rectTransform.anchoredPosition=new Vector2(24,-78);
                heading.rectTransform.sizeDelta=new Vector2(750,48);
                heading.text=state.objective_progress.label + "\n" + state.objective_progress.summary;
            }

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
                card.state=cat;
                card.stateText.text = occupied ? "お客さん："+seat.customer_name+"\n担当："+(cat == null ? "未配置" : cat.name)+ (cat==null ? "" : "\n体力 "+cat.stamina.ToString("0.#")+" / 疲労 "+cat.fatigue.ToString("0.#")) : "お客さんを待っています";
                if(card.details!=null)
                {
                    card.details.gameObject.SetActive(occupied && cat!=null);card.details.onClick.RemoveAllListeners();
                    string catId=cat?.cat_id;card.details.onClick.AddListener(()=>GetComponent<PythonCatDetailsPanel>().Open(catId));
                    card.details.GetComponentInChildren<Text>().text="担当猫の詳細";
                }
            }
            RefreshControls();
            string text = "来店客一覧\n\n";
            var customers = state.customers ?? new PythonCustomerState[0];
            if(customers.Length == 0) text += "まだお客さんは来ていません。";
            foreach(var customer in customers)
            {
                string label = customer.status == "seated" ? "接客中" : customer.status == "departed" ? "退店済み" : "待機中";
                int index = Array.FindIndex(seats,seat=>seat.seat_id == customer.seat_id);
                text += customer.name + "\n" + label + (index >= 0 ? " / 席 " + (index+1) : "") + "\n\n";
            }
            GetComponent<ContentPreview>().SetServiceDescription(text);
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();
            foreach(var card in cards)if(card.details!=null)card.details.interactable=state!=null && card.state!=null && client.CanManageSaves;
            if(state==null || serviceSummary==null)return;
            var seats=state.seats ?? new PythonSeatState[0];int occupied=Array.FindAll(seats,seat=>!string.IsNullOrEmpty(seat.customer_id)).Length;
            serviceSummary.text="営業時間 "+state.tick+" / "+state.opening_ticks+"刻　接客中 "+occupied+" / "+seats.Length+"席　待ち "+state.waiting_count+"人"
                +"\n自動進行："+(client.Automatic ? "進行中" : "停止中")+"　速度："+(client.Fast ? "高速（0.25秒 / 刻）" : "通常（1秒 / 刻）");
            if(dayProgress!=null){dayProgress.maxValue=Math.Max(1,state.opening_ticks);dayProgress.value=state.tick;}
        }
        public void Disconnect()
        {
            state = null;if(serviceSummary!=null)serviceSummary.text="未接続：再接続後に接客状況を更新します。";
            if(dayProgress!=null)dayProgress.value=0;
            foreach(var card in seatContent.GetComponentsInChildren<PythonCatCard>(true))
            {
                card.picture.texture = null; card.picture.enabled = false; card.customerIcon.SetActive(false); card.stateText.text = "未接続";if(card.details!=null)card.details.interactable=false;
            }
            seatsTab.interactable = catsTab.interactable = false;
        }
    }
}
