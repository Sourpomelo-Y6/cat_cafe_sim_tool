using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonEventListPanel : MonoBehaviour
    {
        public GameObject panel;
        public Button open, close;
        public Button[] entries,tabs,detailClose;
        public GameObject[] detailPanels;public RectTransform content;public ScrollRect scroll;public Text title;
        public static readonly string[] GroupNames={"回答・会話待ち","出会い・交流","派遣・帰還","記録"};
        public int SelectedGroup=>group;public int PendingCount=>Enumerable.Range(0,14).Sum(PendingFor);
        private int group=1;private GameObject returnPanel;private Button returnButton;private string returnCaption;
        private readonly HashSet<Button> bound=new HashSet<Button>();
        public Text notice;
        private PythonCafeState current;
        private bool online;
        private string instance,resolution;private int lastPending,lastDay;
        public void Open(){OpenList(true);}
        private void OpenList(bool prioritize)
        {
            if(current==null)return;ClearReturnContext();GetComponent<PythonMenuPanel>()?.Close();GetComponent<PythonMenuPanel>()?.CloseDetails();CloseDetails();
            if(prioritize && PendingCount>0)group=0;panel.SetActive(true);panel.transform.SetAsLastSibling();RefreshControls();if(scroll!=null)scroll.verticalNormalizedPosition=1;
        }
        private void CloseDetails()
        {
            GetComponent<PythonEventRecordPanel>()?.Close();GetComponent<PythonRecruitmentPanel>()?.Close();GetComponent<PythonBusinessLogPanel>()?.Close();GetComponent<PythonClearResultsPanel>()?.Close();GetComponent<PythonFinanceHistoryPanel>()?.Close();
            GetComponent<PythonVisitingCatPanel>()?.Close();GetComponent<PythonMissingPanel>()?.Close();GetComponent<PythonAdoptionPanel>()?.Close();GetComponent<PythonDispatchPanel>()?.Close();
            GetComponent<PythonRegularIntroductionPanel>()?.Close();GetComponent<PythonReservationPanel>()?.Close();GetComponent<PythonCustomerTrustPanel>()?.Close();GetComponent<PythonGeneralDispatchPanel>()?.Close();
            GetComponent<PythonDispatchChoicePanel>()?.Close();GetComponent<PythonDispatchTroublePanel>()?.Close();GetComponent<PythonDispatchIntroductionPanel>()?.Close();
            if(detailPanels!=null)for(int i=1;i<=3;i++)if(detailPanels[i]!=null)detailPanels[i].SetActive(false);
        }
        public void ResetNavigation(){Close();instance=null;group=1;}
        public void Close(){ClearReturnContext();panel.SetActive(false);}
        public void ReturnToBusiness(){Close();CloseDetails();GetComponent<PythonPreparationPanel>()?.Close();GetComponent<PythonClosingPanel>()?.Close();}
        private void ClearReturnContext(){if(returnButton!=null){if(IsAddedReturn(returnButton))returnButton.gameObject.SetActive(false);else returnButton.GetComponentInChildren<Text>().text=returnCaption;}returnPanel=null;returnButton=null;returnCaption=null;}
        private static bool IsAddedReturn(Button button)=>button.name=="Event List Return";
        private void Track(int index,GameObject target){if(detailClose==null || index>=detailClose.Length || detailClose[index]==null)return;returnPanel=target;returnButton=detailClose[index];returnCaption=returnButton.GetComponentInChildren<Text>().text;returnButton.GetComponentInChildren<Text>().text="イベント一覧へ戻る";if(IsAddedReturn(returnButton))returnButton.gameObject.SetActive(true);}
        private void ReturnFromDetail(GameObject source){if(returnPanel==source)OpenList(false);}
        public void SelectGroup(int value){if(value<0 || value>=4)return;group=value;RefreshControls();if(scroll!=null)scroll.verticalNormalizedPosition=1;}
        private bool ResultPending(int i)=>CafeBackends.Standalone && (i==4?(CafeBackends.Local as LocalCafeBackend)?.MissingResultPending==true:i==8?(CafeBackends.Local as LocalCafeBackend)?.ReservationResultPending==true:i==9?(CafeBackends.Local as LocalCafeBackend)?.TrustResultPending==true:i==5?(CafeBackends.Local as LocalCafeBackend)?.AdoptionResultPending==true:i==1?(CafeBackends.Local as LocalCafeBackend)?.IntakeResultPending==true:i==7?(CafeBackends.Local as LocalCafeBackend)?.RegularResultPending==true:i==10?(CafeBackends.Local as LocalCafeBackend)?.DispatchResultPending==true:i==11?(CafeBackends.Local as LocalCafeBackend)?.EncounterResultPending==true:i==12?(CafeBackends.Local as LocalCafeBackend)?.LodgeResultPending==true:i==13 && (CafeBackends.Local as LocalCafeBackend)?.DispatchIntroductionResultPending==true);
        public int PendingFor(int i)
        {
            if(current==null)return 0;
            if(ResultPending(i))return 1;
            switch(i){
                case 1:return current.intake_request?.status=="waiting"?1:0;
                case 2:return current.store_event!=null?1:0;
                case 3:return current.growth_choices?.Length ?? 0;
                case 4:if(CafeBackends.Standalone && (CafeBackends.Local as LocalCafeBackend).MissingResultPending)return 1;return (current.missing_cats ?? new PythonMissingCat[0]).Count(r=>r.status=="waiting");
                case 5:return (current.adoption?.events ?? new PythonAdoptionEvent[0]).Count(r=>r.status=="waiting");
                case 6:return (current.patron_dispatch?.events ?? new PythonDispatchEvent[0]).Count(r=>r.status=="waiting");
                case 7:return current.regular_introduction?.status=="waiting"?1:0;
                case 8:return current.reservation?.status=="waiting"?1:0;
                case 9:return (current.customer_trust?.events ?? new PythonTrustEvent[0]).Count(r=>r.status=="waiting");
                case 10:return (current.general_dispatch?.events ?? new PythonDispatchEvent[0]).Count(r=>r.status=="waiting");
                case 11:return (current.dispatch_choices ?? new PythonDispatchEncounter[0]).Count(r=>r.status=="waiting");
                case 12:return (current.dispatch_troubles ?? new PythonDispatchTrouble[0]).Count(r=>r.status=="waiting");
                case 13:return (current.dispatch_introductions ?? new PythonDispatchIntroduction[0]).Count(r=>r.status=="waiting");
                default:return 0;
            }
        }
        public int GroupFor(int i)
        {
            if(PendingFor(i)>0)return 0;
            switch(i){
                case 0:return current?.visiting_cat?.status=="accepted"?3:1;
                case 1:return current?.intake_request==null?1:3;
                case 7:return current?.regular_introduction==null || current.regular_introduction.status=="untriggered"?1:3;
                case 8:return current?.reservation==null || current.reservation.status=="untriggered" || current.reservation.status=="accepted"?1:3;
                case 4:return current?.missing_cats?.Any(r=>r.status=="missing")==true?2:3;
                case 6:return current?.patron_dispatch?.events?.Any(r=>r.status=="travelling")==true?2:3;
                case 12:return current?.dispatch_troubles?.Any(r=>r.activity_status=="missing")==true?2:3;
                case 10:return current?.general_dispatch?.events?.Any(r=>r.status=="travelling")==true?2:current?.general_dispatch?.events?.Length>0?3:1;
                default:return 3;
            }
        }
        public void Visiting(){Show(0);} public void Intake(){Show(1);} public void Store(){Show(2);} public void Growth(){Show(3);}
        public void Missing(){Show(4);} public void Adoption(){Show(5);} public void Dispatch(){Show(6);}
        public void Regular(){Show(7);}
        public void Reservation(){Show(8);}
        public void Trust(){Show(9);}
        public void GeneralDispatch(){Show(10);}
        public void DispatchChoice(){Show(11);}
        public void DispatchTrouble(){Show(12);}
        public void DispatchIntroduction(){Show(13);}
        public void Expansion(){Show(15);}
        public void SeatEquipment(){Show(16);}
        public void RestSpace(){Show(17);}
        public void WaitingArea(){Show(18);}
        public void CustomerDirectory(){Show(19);}
        public void PetShop(){Show(20);}
        public void ObjectiveStart(){Show(21);}
        public void DispatchComparison(){Show(22);}
        public void ServiceAssignment(){Show(23);}
        public void AutoPlay(){Show(24);}
        public void Items(){Show(14);}
        private bool Available(int i)
        {
            if(current==null)return false;
            switch(i){
                case 0:return current.visiting_cat!=null && current.can_set_shifts && !current.closed && current.game_over==null;
                case 1:return current.intake_request!=null && (current.intake_request.status=="waiting" || current.intake_request.status=="accepted" || current.intake_request.status=="declined");
                case 2:return current.store_event!=null;
                case 3:return current.growth_choices!=null && current.growth_choices.Length>0;
                case 4:return current.missing_cats!=null && current.missing_cats.Length>0;
                case 5:return current.adoption!=null;
                case 6:return current.patron_dispatch?.events?.Length>0 || current.patron_dispatch?.options?.Length>0;
                case 7:return current.regular_introduction!=null;
                case 8:return current.reservation!=null;
                case 9:return current.customer_trust!=null;
                case 19:return current.customer_directory!=null;
                case 20:return current.pet_shop!=null;
                case 21:return current.objective_start!=null;
                case 22:return current.dispatch_comparison!=null;
                case 23:return current.service_assignment!=null;
                case 24:return current.autoplay!=null;
                case 18:return current.waiting_area!=null;
                case 17:return current.rest_space!=null;
                case 16:return current.seat_equipment!=null;
                case 15:return current.expansion!=null;
                case 14:return current.items!=null;
                case 13:return current.dispatch_introductions?.Length>0;
                case 12:return current.dispatch_troubles?.Length>0;
                case 11:return current.dispatch_choices?.Length>0;
                case 10:return current.general_dispatch?.options?.Length>0 || current.general_dispatch?.events?.Length>0;
                default:return false;
            }
        }
        public bool MenuAvailable(int i){return current!=null && online && (i==24 ? GetComponent<PythonStateClient>().CanManageSaves || GetComponent<PythonStateClient>().AutoPlayRunning : GetComponent<PythonStateClient>().CanManageSaves) && Available(i);}
        public void ShowMenu(int i){Show(i,false);}
        private void Show(int i,bool fromList=true)
        {
            if(!MenuAvailable(i))return;
            if(i==1 && CafeBackends.Standalone && (CafeBackends.Local as LocalCafeBackend)?.Conversation?.completed==false){Close();GetComponent<IntakeConversationPanel>()?.Open();return;}
            if(i==7 && CafeBackends.Standalone && (CafeBackends.Local as LocalCafeBackend)?.RegularConversation?.completed==false){Close();GetComponent<RegularConversationPanel>()?.Open();return;}
            if(i==10 && CafeBackends.Standalone)
            {
                var backend=CafeBackends.Local as LocalCafeBackend;var waiting=backend.Simulation.State.dispatches.FirstOrDefault(e=>e.destination==CatCafePreview.Standalone.CafeDispatchDialogue.Destination && e.status=="waiting");
                if(backend.DispatchResultPending || waiting!=null){Close();var general=GetComponent<PythonGeneralDispatchPanel>();if(backend.DispatchResultPending){general.Dialogue.Apply(current,general);general.Dialogue.Open();}else general.Dialogue.OpenEvent(waiting.id);return;}
            }
            if(i==4 && CafeBackends.Standalone){Close();var missing=GetComponent<PythonMissingPanel>();var b=CafeBackends.Local as LocalCafeBackend;var e=b.Simulation.State.missing_cases.FirstOrDefault(x=>x.result!=null && !x.result.completed);missing.Open(e?.cat_id ?? current.missing_cats.FirstOrDefault(x=>x.status=="waiting")?.cat_id ?? current.missing_cats.Last().cat_id);if(GetComponent<MissingConversationPanel>()?.Showing==true)return;}
            if(i==9 && CafeBackends.Standalone){Close();var trustPanel=GetComponent<PythonCustomerTrustPanel>();trustPanel.Open();if(trustPanel.Dialogue.Showing)return;}
            if(i==12 && CafeBackends.Standalone){Close();var troublePanel=GetComponent<PythonDispatchTroublePanel>();troublePanel.Open();if(troublePanel.Dialogue.Showing)return;}
            if(i==5 && CafeBackends.Standalone){Close();var adoptionPanel=GetComponent<PythonAdoptionPanel>();adoptionPanel.Open("");if(adoptionPanel.Dialogue.Showing)return;}
            if(i==11 && CafeBackends.Standalone){Close();var choicePanel=GetComponent<PythonDispatchChoicePanel>();choicePanel.Open();if(choicePanel.Dialogue.Showing)return;}
            if(i==13 && CafeBackends.Standalone){Close();var intro=GetComponent<PythonDispatchIntroductionPanel>();intro.Open();if(intro.Dialogue.Showing)return;}
            if(i==1 && current.intake_request.status!="waiting"){Close();GetComponent<PythonEventRecordPanel>().Open();return;}
            GameObject target=null;
            switch(i){
                case 20:var shop=GetComponent<PythonPetShopPanel>();shop.Open();target=shop.panel;break;
                case 23:var assignment=GetComponent<PythonServiceAssignmentPanel>();assignment.Open();target=assignment.panel;break;
                case 24:var autoplay=GetComponent<PythonAutoPlayPanel>();autoplay.Open();target=autoplay.panel;break;
                case 22:var comparison=GetComponent<PythonDispatchComparisonPanel>();comparison.Open();target=comparison.panel;break;
                case 21:var objectives=GetComponent<PythonObjectiveStartPanel>();objectives.Open();target=objectives.panel;break;
                case 19:var directory=GetComponent<PythonCustomerDirectoryPanel>();directory.Open();target=directory.panel;break;
                case 18:var waitingArea=GetComponent<PythonWaitingAreaPanel>();waitingArea.Open();target=waitingArea.panel;break;
                case 17:var restSpace=GetComponent<PythonRestSpacePanel>();restSpace.Open();target=restSpace.panel;break;
                case 16:var equipment=GetComponent<PythonSeatEquipmentPanel>();equipment.Open();target=equipment.panel;break;
                case 15:var expansion=GetComponent<PythonExpansionPanel>();expansion.Open();target=expansion.panel;break;
                case 14:var items=GetComponent<PythonItemsPanel>();items.Open();target=items.panel;break;
                case 13:var introduction=GetComponent<PythonDispatchIntroductionPanel>();introduction.Open();target=introduction.panel;break;
                case 12:var trouble=GetComponent<PythonDispatchTroublePanel>();trouble.Open();target=trouble.panel;break;
                case 11:var choice=GetComponent<PythonDispatchChoicePanel>();choice.Open();target=choice.panel;break;
                case 10:var general=GetComponent<PythonGeneralDispatchPanel>();general.Open("");target=general.panel;break;
                case 0:var v=GetComponent<PythonVisitingCatPanel>();v.Open();target=v.panel;break;
                case 7:var r=GetComponent<PythonRegularIntroductionPanel>();r.Open();target=r.panel;break;
                case 8:var reservation=GetComponent<PythonReservationPanel>();reservation.Open();target=reservation.panel;break;
                case 9:var trust=GetComponent<PythonCustomerTrustPanel>();trust.Open();target=trust.panel;break;
                case 1:target=GetComponent<PythonIntakePanel>().panel;break;
                case 2:target=GetComponent<PythonStoreEventPanel>().panel;break;
                case 3:target=GetComponent<PythonGrowthPanel>().panel;break;
                case 4:var m=GetComponent<PythonMissingPanel>();m.Open(CafeBackends.Standalone && (CafeBackends.Local as LocalCafeBackend).MissingResultPending?(CafeBackends.Local as LocalCafeBackend).Simulation.State.missing_cases.First(e=>e.result!=null && !e.result.completed).cat_id:current.missing_cats.FirstOrDefault(r=>r.status=="waiting")?.cat_id ?? current.missing_cats[0].cat_id);target=m.panel;break;
                case 5:var a=GetComponent<PythonAdoptionPanel>();a.Open(current.adoption.events?.FirstOrDefault(r=>r.status=="waiting")?.cat_id ?? "");target=a.panel;break;
                case 6:var d=GetComponent<PythonDispatchPanel>();d.Open(current.patron_dispatch.events?.FirstOrDefault(r=>r.status=="waiting")?.cat_id ?? current.patron_dispatch.options?.FirstOrDefault()?.cat_id ?? current.patron_dispatch.events?.FirstOrDefault()?.cat_id);target=d.panel;break;
            }
            if(target!=null){GetComponent<PythonMenuPanel>()?.Close();GetComponent<PythonRecruitmentPanel>()?.Close();Close();target.SetActive(true);GetComponent<ContentPreview>().RefreshPythonControls();target.transform.SetAsLastSibling();if(fromList && i<14)Track(i,target);}
        }
        public void Apply(PythonCafeState s)
        {
            bool changed=instance!=s.instance_id;if(changed){instance=s.instance_id;Close();}current=s;online=true;if(changed || lastDay!=s.day)resolution=null;else if(PendingCount<lastPending)resolution="回答待ちが更新されました。残り "+PendingCount+"件です。";lastPending=PendingCount;lastDay=s.day;if(changed)group=PendingCount>0?0:1;
            if(detailClose!=null)for(int i=0;i<detailClose.Length;i++){var button=detailClose[i];var target=detailPanels[i];if(button!=null && bound.Add(button))button.onClick.AddListener(()=>ReturnFromDetail(target));}
            if(returnPanel!=null && !returnPanel.activeSelf)OpenList(false);RefreshControls();
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();open.interactable=current!=null;close.interactable=true;if(current==null)return;
            var v=current.visiting_cat;var intake=current.intake_request;var missing=current.missing_cats ?? new PythonMissingCat[0];var adoption=current.adoption?.events ?? new PythonAdoptionEvent[0];var dispatch=current.patron_dispatch?.events ?? new PythonDispatchEvent[0];
            var captions=new[]{
                "店先に通う猫："+(v==null ? "対象なし" : v.status=="untriggered" ? "出現前・人気第1段階達成の翌日から" : v.name+" / "+(v.status=="accepted" ? "加入済み" : v.progress+"/"+v.required+"回")+(!current.can_set_shifts ? "（営業準備中に確認）" : "")),
                "保護猫の受け入れ："+(intake==null ? "依頼なし" : intake.name+" / "+(intake.status=="waiting" ? "確認待ち" : intake.status=="accepted" ? "受け入れ済み" : "見送り済み")),
                "店舗イベント："+(current.store_event==null ? "確認待ちなし" : current.store_event.title+" / 確認待ち"),
                "猫の成長："+(current.growth_choices?.Length ?? 0)+"件の選択待ち",
                "猫の家出・帰還："+missing.Count(r=>r.status=="waiting")+"件の帰還待ち / 家出中 "+missing.Count(r=>r.status=="missing")+"匹 / 記録 "+missing.Length+"件",
                "譲渡の申し出："+adoption.Count(r=>r.status=="waiting")+"件の確認待ち / 記録 "+adoption.Length+"件",
                "有力者派遣の帰還："+dispatch.Count(r=>r.status=="waiting")+"件の帰還待ち / 派遣中 "+dispatch.Count(r=>r.status=="travelling")+"匹",
                "常連からの猫紹介："+(current.regular_introduction==null ? "対象なし" : current.regular_introduction.status=="untriggered" ? "まだ届いていません" : current.regular_introduction.name+" / "+(current.regular_introduction.status=="waiting" ? "確認待ち" : current.regular_introduction.status=="accepted" ? "加入済み" : "見送り済み"))};
            for(int i=0;i<entries.Length;i++){entries[i].GetComponentInChildren<Text>().text=i<captions.Length ? captions[i] : i==24 ? "おまかせ：方針・複数日進行・実行結果" : i==23 ? "接客の手動割り当て・自動割り当て設定" : i==22 ? "猫ごとの派遣先比較" : i==21 ? "経営ルール・目標の開始・進捗："+(current.objective_start?.rows ?? new PythonStartObjective[0]).Count(r=>r.can_start)+"件の開始候補" : i==20 ? "ペットショップ：候補 "+(current.pet_shop?.rows?.Length ?? 0)+"匹 / 購入済み "+(current.pet_shop?.rows ?? new PythonShopCat[0]).Count(r=>r.accepted)+"匹" : i==19 ? "お客さんの名簿・来店予定："+(current.customer_directory?.rows?.Length ?? 0)+"名" : i==18 ? "待合スペース："+(current.waiting_area?.status ?? "この営業では利用できません") : i==17 ? "休養スペース："+(current.rest_space?.status ?? "情報なし") : i==16 ? "席の接客設備：所持 "+(current.seat_equipment?.owned?.Length ?? 0)+"個 / "+(current.seat_equipment?.can_manage==true ? "購入・配置" : "条件を確認") : i==15 ? "席数の増設："+(current.expansion?.seats ?? 0)+"席 / "+(current.expansion?.can_expand==true ? "増設可能" : current.expansion?.has_next==true ? "条件を確認" : "追加できる席なし") : i==14 ? "用品・猫のケア：所持 "+(current.items?.count ?? 0)+"個" : i==13 ? "派遣先からの猫紹介："+(current.dispatch_introductions ?? new PythonDispatchIntroduction[0]).Count(e=>e.status=="waiting")+"件の回答待ち / 記録 "+(current.dispatch_introductions?.Length ?? 0)+"件" : i==12 ? "山あいの宿のトラブル："+(current.dispatch_troubles ?? new PythonDispatchTrouble[0]).Count(e=>e.status=="waiting")+"件の回答待ち / 記録 "+(current.dispatch_troubles?.Length ?? 0)+"件" : i==11 ? "派遣中の選択イベント："+(current.dispatch_choices ?? new PythonDispatchEncounter[0]).Count(e=>e.status=="waiting")+"件の回答待ち / 記録 "+(current.dispatch_choices?.Length ?? 0)+"件" : i==10 ? "一般派遣・帰還："+(current.general_dispatch?.events ?? new PythonDispatchEvent[0]).Count(e=>e.status=="waiting")+"件の帰還待ち / 派遣中 "+(current.general_dispatch?.events ?? new PythonDispatchEvent[0]).Count(e=>e.status=="travelling")+"匹" : i==9 ? "顧客の信頼回復："+(current.customer_trust==null ? "対象なし" : (current.customer_trust.events ?? new PythonTrustEvent[0]).Count(e=>e.status=="waiting")+"件の回答待ち / 記録 "+(current.customer_trust.events?.Length ?? 0)+"件") : "特別予約："+(current.reservation==null ? "対象なし" : current.reservation.status=="untriggered" ? "解放前" : current.reservation.visit_day+"日目 / "+(current.reservation.status=="waiting" ? "確認待ち" : current.reservation.status=="declined" ? "辞退済み" : "引き受け済み"));entries[i].interactable=MenuAvailable(i) && (i==6 ? dispatch.Length>0 : i==10 ? current.general_dispatch?.events?.Length>0 || current.general_dispatch?.options?.Length>0 || ResultPending(10) : true);}
            foreach(int index in new[]{1,7,8,10,12})if(ResultPending(index))entries[index].GetComponentInChildren<Text>().text+=" / 結果会話が未完了・続きへ";
            if(current.dispatch_troubles?.Any(e=>e.status=="resolved" && e.activity_status=="missing")==true)entries[12].GetComponentInChildren<Text>().text+=" / 帰還待機中（営業・休業で日程を進める）";
            RefreshGroups();
            if(returnButton!=null && returnPanel!=null && returnPanel.activeSelf)returnButton.GetComponentInChildren<Text>().text="イベント一覧へ戻る";
            notice.text=!online ? "Python未接続です。再接続後にイベントを開けます。" : client.HasPendingResults ? "接客結果が未保存です。保存待ちの内容を確認してください。" : client.AutoPlayRunning ? "おまかせ実行中です。停止後に回答できます。" : !client.CanManageSaves ? "操作を送信しています。完了するまでお待ちください。" : group==0 && PendingCount==0 ? "回答待ちはありません。出会い・交流や記録は別の分類で確認できます。" : "未完了 "+PendingCount+"件。回答・結果会話を完了すると営業を再開できます。店先の猫との交流は任意です。";if(online && client.CanManageSaves && !string.IsNullOrEmpty(resolution))notice.text=resolution+"\n"+notice.text;
        }
        private void RefreshGroups()
        {
            int total=PendingCount;var caption=open.GetComponentInChildren<Text>();caption.text=total>0?"イベント一覧\n要確認 "+total+"件":"イベント一覧";caption.fontSize=total>0?14:17;caption.resizeTextForBestFit=total>0;caption.resizeTextMinSize=10;caption.resizeTextMaxSize=14;
            if(tabs==null || tabs.Length!=4)return;
            int rows=0;for(int i=0;i<entries.Length;i++)
            {
                bool visible=i<14 && GroupFor(i)==group;entries[i].gameObject.SetActive(visible);
                if(visible){entries[i].GetComponent<RectTransform>().anchoredPosition=new Vector2(4,-rows*72);entries[i].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,64);rows++;int pending=PendingFor(i);var label=entries[i].GetComponentInChildren<Text>();label.text=(pending>0 ? ResultPending(i)?"【回答済み・結果会話の続き】":"【回答待ち "+pending+"件】" : group==3 ? Available(i) ? "【記録・閲覧】" : "【現在の記録なし】" : group==2 ? "【派遣・帰還】" : "【任意の交流・候補】")+"\n"+label.text;label.fontSize=16;label.resizeTextForBestFit=true;label.resizeTextMinSize=13;label.resizeTextMaxSize=16;entries[i].GetComponent<Image>().color=pending>0?new Color(1f,.94f,.77f):group==3?new Color(.91f,.94f,.98f):Color.white;}
            }
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(342,rows*72));
            title.text="イベント一覧 / "+GroupNames[group];
            for(int i=0;i<4;i++){int count=i==0?total:Enumerable.Range(0,14).Count(r=>GroupFor(r)==i);tabs[i].GetComponentInChildren<Text>().text=GroupNames[i]+"（"+count+(i==0?"件":"項目")+"）";tabs[i].GetComponentInChildren<Text>().fontSize=16;tabs[i].GetComponent<Image>().color=i==group?new Color(.78f,.88f,.74f):Color.white;tabs[i].interactable=current!=null;}
            if(panel.activeSelf)panel.transform.SetAsLastSibling();
        }
        public void Disconnect(){online=false;RefreshControls();}
    }
}
