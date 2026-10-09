using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonEventListPanel : MonoBehaviour
    {
        public GameObject panel;
        public Button open, close;
        public Button[] entries;
        public Text notice;
        private PythonCafeState current;
        private bool online;
        private string instance;
        public void Open(){if(current==null)return;GetComponent<PythonMenuPanel>()?.Close();panel.SetActive(true);panel.transform.SetAsLastSibling();RefreshControls();}
        public void Close(){panel.SetActive(false);}
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
                case 1:return current.intake_request?.status=="waiting";
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
        public void ShowMenu(int i){Show(i);}
        private void Show(int i)
        {
            if(!MenuAvailable(i))return;
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
                case 4:var m=GetComponent<PythonMissingPanel>();m.Open(current.missing_cats.FirstOrDefault(r=>r.status=="waiting")?.cat_id ?? current.missing_cats[0].cat_id);target=m.panel;break;
                case 5:var a=GetComponent<PythonAdoptionPanel>();a.Open(current.adoption.events?.FirstOrDefault(r=>r.status=="waiting")?.cat_id ?? "");target=a.panel;break;
                case 6:var d=GetComponent<PythonDispatchPanel>();d.Open(current.patron_dispatch.events?.FirstOrDefault(r=>r.status=="waiting")?.cat_id ?? current.patron_dispatch.options?.FirstOrDefault()?.cat_id ?? current.patron_dispatch.events?.FirstOrDefault()?.cat_id);target=d.panel;break;
            }
            if(target!=null){GetComponent<PythonMenuPanel>()?.Close();Close();target.SetActive(true);target.transform.SetAsLastSibling();}
        }
        public void Apply(PythonCafeState s){if(instance!=s.instance_id){instance=s.instance_id;Close();}current=s;online=true;RefreshControls();}
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
            for(int i=0;i<entries.Length;i++){entries[i].GetComponentInChildren<Text>().text=i<captions.Length ? captions[i] : i==24 ? "おまかせ：方針・複数日進行・実行結果" : i==23 ? "接客の手動割り当て・自動割り当て設定" : i==22 ? "猫ごとの派遣先比較" : i==21 ? "経営ルール・目標の開始・進捗："+(current.objective_start?.rows ?? new PythonStartObjective[0]).Count(r=>r.can_start)+"件の開始候補" : i==20 ? "ペットショップ：候補 "+(current.pet_shop?.rows?.Length ?? 0)+"匹 / 購入済み "+(current.pet_shop?.rows ?? new PythonShopCat[0]).Count(r=>r.accepted)+"匹" : i==19 ? "お客さんの名簿・来店予定："+(current.customer_directory?.rows?.Length ?? 0)+"名" : i==18 ? "待合スペース："+(current.waiting_area?.status ?? "この営業では利用できません") : i==17 ? "休養スペース："+(current.rest_space?.status ?? "情報なし") : i==16 ? "席の接客設備：所持 "+(current.seat_equipment?.owned?.Length ?? 0)+"個 / "+(current.seat_equipment?.can_manage==true ? "購入・配置" : "条件を確認") : i==15 ? "席数の増設："+(current.expansion?.seats ?? 0)+"席 / "+(current.expansion?.can_expand==true ? "増設可能" : current.expansion?.has_next==true ? "条件を確認" : "追加できる席なし") : i==14 ? "用品・猫のケア：所持 "+(current.items?.count ?? 0)+"個" : i==13 ? "派遣先からの猫紹介："+(current.dispatch_introductions ?? new PythonDispatchIntroduction[0]).Count(e=>e.status=="waiting")+"件の回答待ち / 記録 "+(current.dispatch_introductions?.Length ?? 0)+"件" : i==12 ? "山あいの宿のトラブル："+(current.dispatch_troubles ?? new PythonDispatchTrouble[0]).Count(e=>e.status=="waiting")+"件の回答待ち / 記録 "+(current.dispatch_troubles?.Length ?? 0)+"件" : i==11 ? "派遣中の選択イベント："+(current.dispatch_choices ?? new PythonDispatchEncounter[0]).Count(e=>e.status=="waiting")+"件の回答待ち / 記録 "+(current.dispatch_choices?.Length ?? 0)+"件" : i==10 ? "一般派遣の帰還："+(current.general_dispatch?.events ?? new PythonDispatchEvent[0]).Count(e=>e.status=="waiting")+"件の帰還待ち / 派遣中 "+(current.general_dispatch?.events ?? new PythonDispatchEvent[0]).Count(e=>e.status=="travelling")+"匹" : i==9 ? "顧客の信頼回復："+(current.customer_trust==null ? "対象なし" : (current.customer_trust.events ?? new PythonTrustEvent[0]).Count(e=>e.status=="waiting")+"件の回答待ち / 記録 "+(current.customer_trust.events?.Length ?? 0)+"件") : "特別予約："+(current.reservation==null ? "対象なし" : current.reservation.status=="untriggered" ? "解放前" : current.reservation.visit_day+"日目 / "+(current.reservation.status=="waiting" ? "確認待ち" : current.reservation.status=="declined" ? "辞退済み" : "引き受け済み"));entries[i].interactable=MenuAvailable(i) && (i==6 ? dispatch.Length>0 : i==10 ? current.general_dispatch?.events?.Length>0 : true);}
            notice.text=!online ? "Python未接続です。再接続後にイベントを開けます。" : !client.CanManageSaves ? "操作を送信しています。完了するまでお待ちください。" : "項目を選んで確認します。店先の猫は任意です。確認待ちに回答すると営業を再開できます。";
        }
        public void Disconnect(){online=false;RefreshControls();}
    }
}
