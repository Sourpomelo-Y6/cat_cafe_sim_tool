using System;
using System.Linq;
using System.Collections.Generic;
namespace CatCafePreview.Standalone
{
    [Serializable] public class EquipmentRule { public string id,name,group; public double cost,engagement,tension; }
    [Serializable] public class CafeEquipmentPurchase { public string id,kind; public int day; }
    [Serializable] public class CafeEquipmentMove { public int day,seat; public string item; }
    [Serializable] public class CafeEquipmentPlacement { public int seat; public string item; }
    public sealed partial class CafeSimulation
    {
        public EquipmentRule Installed(int seat) => Installed(State,seat);
        private EquipmentRule Installed(CafeSnapshot s,int seat)
        {
            var place=s.equipment_placements.SingleOrDefault(p=>p.seat==seat);
            var purchase=place==null?null:s.equipment_purchases.Single(p=>p.id==place.item);
            return purchase==null?null:Rules.equipment.Single(r=>r.id==purchase.kind);
        }
        public string EquipmentReason() => ItemsReason().Replace("用品","接客設備");
        public string EquipmentPurchaseReason(string kind)
        {
            string reason=EquipmentReason();if(reason!="")return reason;
            var r=Rules.equipment.SingleOrDefault(r=>r.id==kind);
            return r==null?"購入する設備を選んでください。":State.funds<=r.cost?"購入後に資金が残る必要があります。":"";
        }
        private int EquipmentSeat(string id)
        {
            if(!int.TryParse((id??"").Replace("seat-",""),out int n) || id!="seat-"+n || n<1 || n>State.seat_count)throw new ArgumentException("席を選んでください。");
            return n-1;
        }
        private void PlaceEquipment(int seat,string item)
        {
            State.equipment_moves.Add(new CafeEquipmentMove {day=State.day,seat=seat,item=item??""});
            State.equipment_placements.RemoveAll(p=>p.seat==seat || p.item==item);
            if(item!=null)State.equipment_placements.Add(new CafeEquipmentPlacement {seat=seat,item=item});
            State.equipment_placements.Sort((a,b)=>a.seat.CompareTo(b.seat));
        }
        private void PurchaseEquipment(string seatId,string kind)
        {
            string reason=EquipmentPurchaseReason(kind);if(reason!="")throw new InvalidOperationException(reason);int seat=EquipmentSeat(seatId);var r=Rules.equipment.Single(r=>r.id==kind);
            var p=new CafeEquipmentPurchase {id="equipment-"+(State.equipment_purchases.Count+1),kind=kind,day=State.day};State.equipment_purchases.Add(p);
            State.funds-=r.cost;State.expenses+=r.cost;State.equipment_expenses+=r.cost;PlaceEquipment(seat,p.id);
        }
        private void EquipSeat(string seatId,string item)
        {
            string reason=EquipmentReason();if(reason!="")throw new InvalidOperationException(reason);int seat=EquipmentSeat(seatId);
            if(string.IsNullOrEmpty(item))item=null;
            if(item!=null && !State.equipment_purchases.Any(p=>p.id==item))throw new ArgumentException("購入済みの設備を選んでください。");
            if(State.equipment_placements.Find(p=>p.seat==seat)?.item==item)return;
            PlaceEquipment(seat,item);
        }
        private void ValidateEquipment(CafeSnapshot s)
        {
            if(s.equipment_purchases==null || s.equipment_placements==null || s.equipment_moves==null)throw new ArgumentException("Invalid equipment");int day=1,index=1;
            foreach(var p in s.equipment_purchases)
            {
                if(p==null || p.id!="equipment-"+index++ || !Rules.equipment.Any(r=>r.id==p.kind) || p.day<day || p.day>s.day)throw new ArgumentException("Invalid equipment purchase");day=p.day;
            }
            if(s.equipment_placements.Select(p=>p.seat).Distinct().Count()!=s.equipment_placements.Count || s.equipment_placements.Select(p=>p.item).Distinct().Count()!=s.equipment_placements.Count)throw new ArgumentException("Duplicate equipment placement");
            foreach(var p in s.equipment_placements)if(p.seat<0 || p.seat>=s.seat_count || !s.equipment_purchases.Any(r=>r.id==p.item))throw new ArgumentException("Unowned equipment placement");
            for(int d=1;d<=s.day;d++)if(s.equipment_purchases.Where(p=>p.day==d).Sum(p=>Rules.equipment.Single(r=>r.id==p.kind).cost)!=(d==s.day?s.equipment_expenses:s.history[d-1].equipment_expenses))throw new ArgumentException("Equipment finance differs");
            var replay=new Dictionary<int,string>();day=1;var firstInstalled=new HashSet<string>();
            foreach(var move in s.equipment_moves)
            {
                int seats=2+s.expansion.Count(p=>p.day<=move.day);
                var purchase=s.equipment_purchases.Find(p=>p.id==move.item);
                if(move.day<day || move.day>s.day || move.seat<0 || move.seat>=seats || !string.IsNullOrEmpty(move.item) && (purchase==null || purchase.day>move.day))throw new ArgumentException("Invalid equipment move");
                if(purchase!=null && firstInstalled.Add(purchase.id) && move.day!=purchase.day)throw new ArgumentException("Missing purchase placement");
                foreach(var key in replay.Where(p=>p.Key==move.seat || p.Value==move.item).Select(p=>p.Key).ToArray())replay.Remove(key);
                if(!string.IsNullOrEmpty(move.item))replay[move.seat]=move.item;day=move.day;
            }
            if(firstInstalled.Count!=s.equipment_purchases.Count || replay.Count!=s.equipment_placements.Count || s.equipment_placements.Any(p=>!replay.ContainsKey(p.seat) || replay[p.seat]!=p.item))throw new ArgumentException("Equipment placements differ from journal");
            foreach(var e in s.exchanges)
            {
                var r=Installed(s,e.seat);
                if(e.equipment_group!=(r?.group??"") || e.equipment_engagement!=(r?.engagement??1) || e.equipment_tension!=(r?.tension??1) || e.service_group!=(s.goal.Count>0?ServiceGroup(e.customer_id):""))throw new ArgumentException("Exchange equipment differs");
            }
        }
        private bool ServiceEquipmentAt(CafeSnapshot s,int day,int seat,string group)
        {
            string item=null;
            foreach(var move in s.equipment_moves.Where(m=>m.day<=day))
            {if(move.seat==seat)item=move.item;else if(move.item==item)item=null;}
            var purchase=s.equipment_purchases.Find(p=>p.id==item);
            return purchase!=null && Rules.equipment.Single(r=>r.id==purchase.kind).group==group;
        }
        private string ServiceGroup(string id) => id=="advanced-quiet"?"quiet":id=="advanced-play"?"play":(id=="advanced-contact" || id=="advanced-longhair")?"contact":"";
        private int PreferredSeat(string customer,int[] free)
        {
            string group=State.goal.Count>0 && customer!="advanced-longhair"?ServiceGroup(customer):"";
            foreach(int i in free)if(group!="" && Installed(i)?.group==group)return i;return free[0];
        }
    }
}
