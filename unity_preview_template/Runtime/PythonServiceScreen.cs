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
        private ScrollRect seatScroll;
        private readonly List<PythonCatCard> cards = new List<PythonCatCard>();
        public bool ShowingSeats { get { return showSeats; } }
        public int OccupiedCount { get { return cards.FindAll(card=>card.gameObject.activeSelf && card.customerIcon.activeSelf).Count; } }
        public PythonCatCard GetSeatCard(int index) { return cards[index]; }
        public void ShowSeats() { showSeats = true; Render();GetComponent<PythonBusinessHeader>()?.RefreshControls(); }
        public void ShowCats() { showSeats = false; Render();GetComponent<PythonBusinessHeader>()?.RefreshControls(); }
        public void Apply(PythonCafeState value)
        {
            state = value;
            string next = value.closed ? "closed" : value.can_set_shifts ? "preparation" : "open";
            if(phase != next) { phase = next; showSeats = next == "open"; }
            Render();
        }
        private void Render()
        {
            GetComponent<PythonCatVisibility>()?.RefreshControls();
            if(state == null) return;
            seatsTab.interactable = catsTab.interactable = true;
            seatsTab.gameObject.SetActive(phase == "open"); catsTab.gameObject.SetActive(phase == "open");
            rosterPanel.SetActive(!showSeats); seatPanel.SetActive(showSeats);
            if(serviceStatus!=null)serviceStatus.SetActive(phase == "open");
            if(detailPanel != null)
            {
                detailPanel.anchoredPosition = new Vector2(showSeats ? 992 : 804,phase == "open" ? -262 : -188);
                detailPanel.sizeDelta = new Vector2(showSeats ? 264 : 452,phase == "open" ? 514 : 550);
                detailText.fontSize = showSeats ? 16 : phase == "open" ? 18 : 22;
            }
            var rosterScroll=rosterPanel.GetComponent<ScrollRect>();
            if(rosterScroll!=null){rosterPanel.GetComponent<RectTransform>().anchoredPosition=new Vector2(24,phase=="open" ? -262 : -220);rosterPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,phase=="open" ? 476 : 518);rosterScroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,phase=="open" ? 476 : 518);}
            var visibility=GetComponent<PythonCatVisibility>();if(visibility?.rosterToggle!=null)visibility.rosterToggle.GetComponent<RectTransform>().anchoredPosition=new Vector2(24,phase=="open" ? -94 : -184);
            heading.text = phase == "closed" ? "閉店結果・猫の状態" : showSeats ? "席一覧　　人物アイコン：接客中のお客さん" : "猫一覧";
            if((state.sidebar_progress ?? (CafeBackends.Standalone ? null : state.objective_progress)) != null)
            {
                heading.fontSize=16;heading.lineSpacing=1;
                heading.rectTransform.anchoredPosition=new Vector2(24,-78);
                heading.rectTransform.sizeDelta=new Vector2(750,48);
                var goals=state.sidebar_progress ?? state.objective_progress;heading.text=goals.label + "\n" + goals.summary;
            }

            if(!showSeats)
            {
                if(phase == "open") GetComponent<ContentPreview>().SetServiceDescription("猫の状態\n\nカードで体力・疲労・活動を確認できます。\n「席一覧」で接客状況に戻れます。");
                return;
            }
            if(cards.Count == 0) cards.AddRange(seatContent.GetComponentsInChildren<PythonCatCard>(true));
            if(seatScroll == null) seatScroll = SeatListLayout.Configure(this);
            var seats = state.seats ?? new PythonSeatState[0];
            SeatListLayout.SetHelp(this, seats.Length);
            while(cards.Count < seats.Length) cards.Add(Instantiate(cardPrefab,seatContent));
            for(int i=0;i<cards.Count;i++)
            {
                var card = cards[i]; card.gameObject.SetActive(i<seats.Length); if(i>=seats.Length) continue;
                SeatListLayout.StyleCard(card);
                var seat = seats[i]; bool occupied = !string.IsNullOrEmpty(seat.customer_id);
                var cat = Array.Find(state.cats,value=>value.cat_id == seat.cat_id);
                card.catName.text = "席 " + (i+1) + (occupied ? cat==null?"　担当待ち":"　接客中" : "　空席");
                card.picture.texture = !occupied || cat == null ? null : CafeNarrativeResources.Texture(seat.narrative_image) ?? GetComponent<ContentPreview>().PythonCatTexture(cat);
                card.picture.enabled = card.picture.texture != null;
                HumanPortraitResources.ApplyIcon(card.customerIcon,seat.customer_id); card.customerIcon.SetActive(occupied); card.work.gameObject.SetActive(false); card.rest.gameObject.SetActive(false);
                card.state=cat;
                card.stateText.text = occupied ? "お客さん："+seat.customer_name+"\n担当："+(cat == null ? "未配置" : cat.name)+ (cat==null ? "" : "\n体力 "+cat.stamina.ToString("0.#")+" / "+cat.max_stamina.ToString("0.#")+"・疲労 "+cat.fatigue.ToString("0.#")) : state.service_assignment?.waiting==true ? "担当待ちのお客さんがいます\n手動接客から配置してください" : "お客さんを待っています";
                card.stateText.text += occupied ? "\n直近の行動："+(cat==null?"担当待ち":string.IsNullOrEmpty(seat.latest_action)?"行動の記録なし":seat.latest_action)+"\n猫の反応："+(cat==null?"—":seat.cat_reaction??"—") : "";
                card.stateText.text += "\n"+(string.IsNullOrEmpty(seat.equipment_name) ? "設備なし" : seat.equipment_name);
                if(card.details!=null)
                {
                    card.details.gameObject.SetActive(true);card.details.onClick.RemoveAllListeners();
                    int seatIndex=i;card.details.onClick.AddListener(()=> { var reader=GetComponent<PythonSeatNarrativePanel>()??gameObject.AddComponent<PythonSeatNarrativePanel>();reader.Open(seats[seatIndex],seatIndex,cat); });
                    card.details.GetComponentInChildren<Text>().text="席の詳細";
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
            text += "\n各席の設備\n";foreach(var seat in seats)text+=seat.seat_id+"："+(seat.equipment_description ?? "設備なし")+"\n\n";
            GetComponent<ContentPreview>().SetServiceDescription(text);
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();
            foreach(var card in cards)if(card.details!=null)card.details.interactable=state!=null && client.CanManageSaves;
            if(state==null || serviceSummary==null)return;
            var seats=state.seats ?? new PythonSeatState[0];int occupied=Array.FindAll(seats,seat=>!string.IsNullOrEmpty(seat.customer_id)).Length;
            serviceSummary.text="営業時間 "+state.tick+" / "+state.opening_ticks+"刻　来店 "+(state.customers?.Length ?? 0)+"人　接客中 "+occupied+" / "+seats.Length+"席　待ち "+state.waiting_count+"人　接客完了 "+state.completed_interactions+"件"
                +"\n自動割り当て："+(state.service_assignment?.auto_assign==false ? "OFF" : "ON")+(state.service_assignment?.waiting==true ? "（客・猫・席の配置待ち）" : "")+"　自動進行："+(client.Automatic ? "進行中" : "停止中")+"　速度："+(client.Fast ? "高速（0.25秒 / 刻）" : "通常（1秒 / 刻）");
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
