using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonEquipmentSeat { public string seat_id,item_id,description; }
    [Serializable] public class PythonEquipmentKind { public string kind_id,name,effect; public float cost,funds_after; public bool can_purchase; }
    [Serializable] public class PythonOwnedEquipment { public string item_id,name,effect,seat_id; public int day; public float cost; }
    [Serializable] public class PythonSeatEquipment { public bool can_manage; public string reason,details; public PythonEquipmentSeat[] seats; public PythonEquipmentKind[] catalog; public PythonOwnedEquipment[] owned; }
    public class PythonSeatEquipmentPanel : MonoBehaviour
    {
        public GameObject panel,confirmation;
        public Text title,details,notice,confirmationText,summary;
        public Button seatSelector,kindSelector,itemSelector,purchase,equip,remove,close,confirm,cancel;
        private PythonCafeState current;
        private string identity,instance,seatId,kindId,itemId,chosenKind,chosenSeat,chosenItem;
        private bool online;
        private PythonSeatEquipment Data => current?.seat_equipment;
        private PythonEquipmentSeat Seat => Data?.seats?.FirstOrDefault(r=>r.seat_id==seatId);
        private PythonEquipmentKind Kind => Data?.catalog?.FirstOrDefault(r=>r.kind_id==kindId);
        private PythonOwnedEquipment Item => Data?.owned?.FirstOrDefault(r=>r.item_id==itemId);
        private PythonStateClient Client => GetComponent<PythonStateClient>();
        public void Open(){if(Data==null || !Client.CanManageSaves)return;Clear();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();Canvas.ForceUpdateCanvases();details.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition=1;}
        private void Clear(){chosenKind=null;confirmation.SetActive(false);}
        public void Close(){Clear();panel.SetActive(false);}
        private static string Next(string[] ids,string selected){if(ids.Length==0)return null;return ids[(Array.IndexOf(ids,selected)+1)%ids.Length];}
        public void NextSeat(){if(Data==null)return;Clear();seatId=Next(Data.seats.Select(r=>r.seat_id).ToArray(),seatId);Render();}
        public void NextKind(){if(Data==null)return;Clear();kindId=Next(Data.catalog.Select(r=>r.kind_id).ToArray(),kindId);Render();}
        public void NextItem(){if(Data==null)return;Clear();itemId=Next(Data.owned.Select(r=>r.item_id).ToArray(),itemId);Render();}
        public void Purchase(){Choose("purchase_seat_equipment",kindId);}
        public void Equip(){if(Item!=null)Choose("equip_seat",itemId);}
        public void Remove(){if(!string.IsNullOrEmpty(Seat?.item_id))Choose("equip_seat","");}
        private void Choose(string action,string selection)
        {
            if(!online || !Client.CanSeatEquipment(action,seatId,selection))return;
            chosenKind=action;chosenSeat=seatId;chosenItem=selection;
            string text=action=="purchase_seat_equipment" ? Kind.name+"を購入・設置しますか？\n"+Kind.effect+"\n費用："+Kind.cost.ToString("0.#")+" / 資金："+current.funds.ToString("0.#")+" → "+Kind.funds_after.ToString("0.#")
                : selection=="" ? "設備を取り外しますか？\n費用：0 / 取り外した設備は保管します。" : Item.name+"を設置・移動しますか？\n"+Item.effect+"\n費用：0\n現在の配置："+(string.IsNullOrEmpty(Item.seat_id) ? "保管中" : Item.seat_id);
            confirmationText.text=text+"\n設置先："+seatId+"\n現在："+Seat.description+"\n\n交換で外れた設備は保管します。";
            confirmation.SetActive(true);confirmation.transform.SetAsLastSibling();RefreshControls();
        }
        public void Confirm(){if(!online || chosenKind==null || !Client.CanSeatEquipment(chosenKind,chosenSeat,chosenItem))return;string action=chosenKind,seat=chosenSeat,item=chosenItem;Clear();Client.SeatEquipment(action,seat,item);}
        public void Cancel(){Clear();RefreshControls();}
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;seatId=kindId=itemId=null;Close();}
            string key=state.instance_id+"/"+state.revision;if(identity!=key){identity=key;Clear();}
            current=state;online=true;if(state.game_over!=null || Data==null)Close();
            if(Seat==null)seatId=Data?.seats?.FirstOrDefault()?.seat_id;
            if(Kind==null)kindId=Data?.catalog?.FirstOrDefault()?.kind_id;
            if(Item==null)itemId=Data?.owned?.FirstOrDefault()?.item_id;Render();
        }
        private void Render()
        {
            title.text="席の接客設備";if(summary!=null)summary.text="資金："+(current?.funds ?? 0).ToString("0.#")+" / 所持設備："+(Data?.owned?.Length ?? 0)+"個 / 設置先："+(seatId ?? "なし")+"\n"+(Kind==null?"購入候補なし":"購入："+Kind.name+" / 費用："+Kind.cost.ToString("0.#")+" / 購入後の資金："+Kind.funds_after.ToString("0.#"));
            seatSelector.GetComponentInChildren<Text>().text="設置先："+(seatId ?? "なし")+"　▶";
            kindSelector.GetComponentInChildren<Text>().text="購入："+(Kind?.name ?? "なし")+"　▶";
            itemSelector.GetComponentInChildren<Text>().text="所持："+(Item==null ? "なし" : Item.name+" / "+itemId)+"　▶";
            string text=Data?.details ?? "設備の情報を取得していません。";
            if(Data!=null){text="選んだ席の設備\n"+(Seat?.description ?? "席なし")+"\n\n"+(Kind==null?"":"購入候補："+Kind.name+"\n効果："+Kind.effect+"\n\n")+"購入と配置の条件\n"+(Data.can_manage?"営業準備中に操作できます。":Data.reason)+"\n"+(Kind==null?"購入候補なし":Kind.can_purchase?"購入できます。":"購入できない理由：購入後に資金が残る必要があります。")+"\n\n"+text+"\n\n各席の設備\n"+string.Join("\n",Data.seats.Select(r=>r.seat_id+"："+r.description));
                if(Kind!=null)text+="\n\n購入候補："+Kind.name+" / 費用 "+Kind.cost.ToString("0.#")+"\n"+Kind.effect+" / 購入後の資金 "+Kind.funds_after.ToString("0.#");
                text+="\n\n購入済み設備・履歴\n"+(Data.owned.Length==0 ? "まだありません。" : string.Join("\n\n",Data.owned.Select(r=>r.name+"（"+r.item_id+"）/ "+r.day+"日目 / 費用 "+r.cost.ToString("0.#")+"\n"+r.effect+" / "+(string.IsNullOrEmpty(r.seat_id) ? "保管中" : r.seat_id))));}
            details.text=text;details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(270,details.preferredHeight+12));RefreshControls();
        }
        public void RefreshControls()
        {
            purchase.GetComponentInChildren<Text>().text="購入"+(Kind==null?"":" "+Kind.cost.ToString("0.#"));equip.GetComponentInChildren<Text>().text="設置・移動 無料";remove.GetComponentInChildren<Text>().text="取り外し 無料";
            bool idle=online && Client.CanManageSaves && chosenKind==null;
            seatSelector.interactable=idle && Data?.seats?.Length>1;kindSelector.interactable=idle && Data?.catalog?.Length>1;itemSelector.interactable=idle && Data?.owned?.Length>1;
            purchase.interactable=idle && Kind!=null && Client.CanSeatEquipment("purchase_seat_equipment",seatId,kindId);
            equip.interactable=idle && Item!=null && Seat?.item_id!=itemId && Client.CanSeatEquipment("equip_seat",seatId,itemId);
            remove.interactable=idle && !string.IsNullOrEmpty(Seat?.item_id) && Client.CanSeatEquipment("equip_seat",seatId,"");
            confirm.interactable=online && chosenKind!=null && Client.CanSeatEquipment(chosenKind,chosenSeat,chosenItem);close.interactable=cancel.interactable=true;
            notice.text=!online ? "Python未接続です。" : !Client.CanManageSaves ? "操作を送信しています。" : !string.IsNullOrEmpty(Data?.reason) ? Data.reason : Kind!=null && !Kind.can_purchase ? "購入後に資金が残る必要があります。配置・取り外しは無料です。" : "上の選択ボタンで席・購入候補・所持品を切り替えます。";
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
