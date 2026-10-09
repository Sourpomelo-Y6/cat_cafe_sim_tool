using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonCatDetailsPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title, details, stateDetails, dispatchNotice, adoptionNotice;
        public ScrollRect scroll;
        public Button interact, previousInteraction, dispatch, missing, adoption,care;
        private bool showingCare;private ScrollRect originScroll;private float originPosition;private bool originSeats;
        public Button[] recordTabs;
        private int recordPage;
        private string instanceId;
        public void Profile() { Records(4); }
        public void GrowthRecords() { Records(0); }
        public void EventRecords() { Records(1); }
        public void RelationshipRecords() { Records(2); }
        public void DailyRecords() { Records(3); }
        private void Records(int page)
        {
            if(!GetComponent<PythonStateClient>().CanManageSaves || current==null || selectedId==null)return;
            recordPage=page;Render();Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=1;
        }
        public void Adoption() { GetComponent<PythonAdoptionPanel>().Open(selectedId); }
        public void Missing() { GetComponent<PythonMissingPanel>().Open(selectedId); }
        public void Dispatch() { GetComponent<PythonDispatchPanel>().Open(selectedId); }
        public Text interactionNotice;
        private string selectedId;
        private PythonCafeState current;
        public void OpenFromList(string catId,bool seats)
        {
            Open(catId);if(!panel.activeSelf)return;var service=GetComponent<PythonServiceScreen>();originSeats=seats;originScroll=(seats?service.seatPanel:service.rosterPanel).GetComponent<ScrollRect>();originPosition=originScroll.verticalNormalizedPosition;
            var close=panel.transform.Find("Details Card/Close").GetComponent<Button>();close.GetComponentInChildren<Text>().text=seats?"席一覧へ戻る":"猫一覧へ戻る";
        }
        public void ResetSelection(){originScroll=null;Close();recordPage=0;}
        public void Care(){var items=GetComponent<PythonItemsPanel>();if(items==null)return;items.OpenForCat(selectedId);showingCare=items.panel.activeSelf;}
        public void Open(string catId)
        {
            var client=GetComponent<PythonStateClient>();
            if(!client.CanManageSaves || current == null) return;
            showingCare=false;originScroll=null;panel.transform.Find("Details Card/Close").GetComponentInChildren<Text>().text="閉じる";
            if(selectedId!=catId)recordPage=0;
            selectedId=catId;
            Render();
            Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=1;
            if(stateDetails!=null){var healthScroll=stateDetails.GetComponentInParent<ScrollRect>();if(healthScroll!=null)healthScroll.verticalNormalizedPosition=1;}
        }
        public void RefreshVisibility(){if(selectedId!=null && Array.Find(GetComponent<ContentPreview>().VisiblePythonCats,row=>row.cat_id==selectedId)==null)Close();}
        public void Close() { if(showingCare)GetComponent<PythonItemsPanel>()?.Close();showingCare=false;panel.SetActive(false); selectedId=null;if(originScroll!=null){var service=GetComponent<PythonServiceScreen>();if(originSeats)service.ShowSeats();else service.ShowCats();Canvas.ForceUpdateCanvases();originScroll.StopMovement();originScroll.verticalNormalizedPosition=originPosition;originScroll=null;} }
        public void Interact()
        {
            var client=GetComponent<PythonStateClient>();var active=current==null ? null : current.player_interaction;
            if(active!=null && active.active && active.cat_id==selectedId)
                GetComponent<PythonPlayerPanel>().Open();
            else client.BeginPlayer(selectedId);
        }
        public void PreviousInteraction() { GetComponent<PythonPlayerPanel>().Open(); }
        public void Apply(PythonCafeState state)
        {
            if(instanceId!=null && instanceId!=state.instance_id){originScroll=null;Close();recordPage=0;}
            instanceId=state.instance_id;current=state;
            if(selectedId != null) Render();
        }
        private void Render()
        {
            var cat=current.cats == null ? null : Array.Find(current.cats,row => row.cat_id==selectedId);
            if(cat == null) { Close(); return; }
            panel.SetActive(true);
            if(!showingCare || GetComponent<PythonItemsPanel>()?.panel.activeSelf!=true){showingCare=false;panel.transform.SetAsLastSibling();}
            title.text=cat.name+"の詳細";
            string health="体調と予定\n"+"体力："+cat.stamina.ToString("0.#")+" / "+cat.max_stamina.ToString("0.#")
                +"\n疲労："+cat.fatigue.ToString("0.#")+" / ストレス："+cat.stress.ToString("0.#")
                +"\n体調："+PythonCatCard.Health(cat)
                +"\n活動："+PythonCatCard.Activity(cat,Array.Exists(current.seats ?? new PythonSeatState[0],seat=>seat.cat_id==cat.cat_id && !string.IsNullOrEmpty(seat.customer_id)))
                +" / 予定："+(cat.activity!="cafe"?"不在":cat.working?"出勤":"休養")
                +"\nプレイヤー好感度："+cat.affinity.ToString("0.#")+" / 100"
                +"\n本日の交流：あと "+cat.remaining_sets+"セット"
                ;
            if(stateDetails!=null)stateDetails.text=health;
            details.text=(stateDetails==null ? health+"\n\n" : "")+"成長・得意な交流\n"+(string.IsNullOrEmpty(cat.growth_details) ? "成長情報はありません。" : cat.growth_details);
            if(recordPage==4)details.text=string.IsNullOrEmpty(cat.profile_details) ? "プロフィール情報はありません。" : cat.profile_details;
            else if(recordPage>0)details.text=cat.records==null ? "記録情報はありません。" : recordPage==1 ? cat.records.events : recordPage==2 ? cat.records.relationships : cat.records.history;
            if(recordTabs!=null)for(int i=0;i<recordTabs.Length;i++)recordTabs[i].GetComponent<Image>().color=i==recordPage ? new Color(.78f,.88f,.74f) : Color.white;
            RefreshControls();
        }
        public void RefreshControls()
        {
            if(care!=null)care.interactable=GetComponent<PythonStateClient>().CanManageSaves && current?.items!=null && selectedId!=null;
            if(recordTabs!=null)foreach(var tab in recordTabs)tab.interactable=GetComponent<PythonStateClient>().CanManageSaves && current!=null && selectedId!=null;
            if(adoption!=null)adoption.interactable=GetComponent<PythonStateClient>().CanManageSaves && selectedId!=null;
            if(missing!=null)missing.interactable=GetComponent<PythonStateClient>().CanManageSaves && selectedId!=null;
            if(dispatch!=null)dispatch.interactable=GetComponent<PythonStateClient>().CanManageSaves && selectedId!=null;
            if(interact==null)return;
            var client=GetComponent<PythonStateClient>();
            var cat=current==null || current.cats==null ? null : Array.Find(current.cats,row=>row.cat_id==selectedId);
            var player=current==null ? null : current.player_interaction;
            bool resume=player!=null && player.active && player.cat_id==selectedId;
            interact.GetComponentInChildren<Text>().text=resume ? "交流を再開" : "猫と交流する";
            interact.interactable=client.CanManageSaves && cat!=null && (resume || cat.can_player_start);
            if(previousInteraction!=null)previousInteraction.interactable=client.CanManageSaves && player!=null && player.cat_id==selectedId;
            string unavailable=!client.CanLeaveResult ? "操作を送信しています。完了までお待ちください。" : !client.CanManageSaves ? "未接続です。再接続後に操作できます。" : null;
            if(interactionNotice!=null)interactionNotice.text=unavailable ?? (cat==null ? "猫を選択してください。" : resume ? "途中の交流を再開できます。" : cat.can_player_start ? "営業準備中に1日3セットまで。体力を使います。" : cat.player_reason);
            if(previousInteraction!=null && !previousInteraction.interactable && unavailable==null && interactionNotice!=null)interactionNotice.text+="\nこの猫の交流の記録はまだありません。";
            if(dispatchNotice!=null)
            {
                var options=current?.patron_dispatch?.options ?? new PythonDispatchOption[0];var events=current?.patron_dispatch?.events ?? new PythonDispatchEvent[0];
                var active=Array.FindLast(events,row=>row.cat_id==selectedId && row.status!="resolved");var available=Array.Find(options,row=>row.cat_id==selectedId && row.can_select);var option=Array.Find(options,row=>row.cat_id==selectedId);
                dispatchNotice.text=unavailable ?? (active!=null ? active.status=="waiting" ? "帰還しています。報酬を確認してください。" : "派遣中：帰還まであと "+active.remaining+"日。" : available!=null ? "派遣先・日数・報酬を確認してから派遣できます。" : option!=null ? "現在は派遣できません。\n"+option.reason : "この猫の派遣先はありません。派遣の画面で条件を確認できます。");
            }
            if(adoptionNotice!=null)
            {
                var offer=Array.FindLast(current?.adoption?.events ?? new PythonAdoptionEvent[0],row=>row.cat_id==selectedId && row.status=="waiting");
                adoptionNotice.text=unavailable ?? (cat!=null && cat.activity=="adopted" ? "譲渡済みです。カフェでの活動には戻せません。履歴を確認できます。" : offer!=null ? "この猫への申し出があります。相手と条件を確認してください。" : "この猫への未回答の申し出はありません。\n申し出の発生："+(current?.adoption!=null && current.adoption.enabled ? "ON" : "OFF")+"\n設定と過去の履歴を確認できます。");
                if(unavailable==null && current?.adoption!=null && !current.adoption.can_configure && !string.IsNullOrEmpty(current.adoption.reason))adoptionNotice.text+="\n設定変更："+current.adoption.reason;
            }

        }
        public void Disconnect()
        {
            current=null;
            RefreshControls();
            if(panel.activeSelf){details.text="未接続です。再接続後に最新の情報を表示します。";if(stateDetails!=null)stateDetails.text=details.text;}
        }
    }
}
