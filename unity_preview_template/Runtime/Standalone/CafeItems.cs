using System;
using System.Linq;
using System.Collections.Generic;
namespace CatCafePreview.Standalone
{
    [Serializable] public class ItemShopRule { public double cost, sale_price; public CafeSupply item; }
    [Serializable] public class CafeItemPurchase { public string source, item_id; public int day; public double cost; }
    [Serializable] public class CafeItemSale { public string source; public int day; public double price; }
    public class CafeOwnedSupply { public string source; public int day; public CafeSupply item; }
    public sealed partial class CafeSimulation
    {
        private IEnumerable<CafeOwnedSupply> ItemRewards(CafeSnapshot s) => s.dispatches.Where(e=>e.status=="resolved").Select(e=>new CafeOwnedSupply {source=e.id,day=e.resolved_day,item=Rules.dispatch.item_reward})
            .Concat(s.store_events.Where(e=>e.type=="supplies").Select(e=>new CafeOwnedSupply {source="store-event-"+e.day,day=e.day,item=Rules.store_events.supply}))
            .Concat(s.item_purchases.Select(p=>new CafeOwnedSupply {source=p.source,day=p.day,item=Rules.item_shop.Single(r=>r.item.id==p.item_id).item}));
        public CafeOwnedSupply[] ItemInventory() => ItemRewards(State).Where(r=>!State.item_uses.Any(u=>u.source==r.source) && !State.item_sales.Any(u=>u.source==r.source)).ToArray();
        public string ItemsReason()
        {
            if(DispatchWaiting)return "派遣の帰還結果を確認してください。";
            if(State.game_over!=null)return "経営終了後は用品を操作できません。";
            if(State.cats.Any(c=>c.activity=="missing" && c.missing_remaining==0))return "帰還結果を確認してください。";
            if(GoalWaiting)return "人気目標の結果を確認してください。";
            if(ReservationWaiting)return "特別予約に回答してください。";
            if(CustomerTrustWaiting)return "顧客の信頼回復イベントに回答してください。";
            if(GrowthWaiting)return "猫の成長・習熟を選択してください。";
            if(StoreEventWaiting)return "店舗イベントに回答してください。";
            if(IntakeWaiting || RegularIntroductionWaiting)return "加入イベントに回答してください。";
            return State.phase=="preparation" ? "" : "用品は営業準備中に操作してください。";
        }
        public string ItemPurchaseReason(string id)
        {string reason=ItemsReason();if(reason!="")return reason;var r=Rules.item_shop.SingleOrDefault(r=>r.item.id==id);return r==null?"購入する用品を選んでください。":State.funds<r.cost?"購入する資金が足りません。":"";}
        public string ItemSaleReason(string source)
        {string reason=ItemsReason();if(reason!="")return reason;var item=ItemInventory().SingleOrDefault(r=>r.source==source);if(item==null)return "未使用の所持品を選んでください。";return Finite(State.funds+ItemSalePrice(item.item.id))?"":"売却後の資金が大きすぎます。";}
        public double ItemSalePrice(string id) => Rules.item_shop.Single(r=>r.item.id==id).sale_price;
        public string ItemUseReason(string source,string id)
        {
            string reason=ItemsReason();if(reason!="")return reason;var reward=ItemInventory().SingleOrDefault(r=>r.source==source);if(reward==null)return "所持している用品を選んでください。";
            var cat=State.cats.SingleOrDefault(c=>c.id==id && c.activity=="cafe");if(cat==null)return "在店している猫を選んでください。";
            return reward.item.stress_relief>0 && cat.stress>0 || reward.item.fatigue_relief>0 && cat.fatigue>0 ? "" : "対象の疲労・ストレスは0です。用品は消費しません。";
        }
        private void PurchaseItem(string id)
        {
            string reason=ItemPurchaseReason(id);if(reason!="")throw new InvalidOperationException(reason);var r=Rules.item_shop.Single(r=>r.item.id==id);
            State.item_purchases.Add(new CafeItemPurchase {source="item-purchase-"+(State.item_purchases.Count+1),item_id=id,day=State.day,cost=r.cost});State.funds-=r.cost;State.expenses+=r.cost;State.item_expenses+=r.cost;CheckEnd();
        }
        private void SellItem(string source)
        {
            string reason=ItemSaleReason(source);if(reason!="")throw new InvalidOperationException(reason);double price=ItemSalePrice(ItemInventory().Single(r=>r.source==source).item.id);
            State.item_sales.Add(new CafeItemSale {source=source,day=State.day,price=price});State.funds+=price;State.item_sales_income+=price;
        }
        private void UseItem(string source,string id)
        {
            string reason=ItemUseReason(source,id);if(reason!="")throw new ArgumentException(reason);var item=ItemInventory().Single(r=>r.source==source).item;var cat=State.cats.Single(c=>c.id==id);
            var use=new CafeSupplyUse {source=source,cat_id=id,day=State.day,before=item.stress_relief>0?cat.stress:0,before_fatigue=item.fatigue_relief>0?cat.fatigue:0};
            use.after=Math.Max(0,use.before-item.stress_relief);use.after_fatigue=Math.Max(0,use.before_fatigue-item.fatigue_relief);
            if(item.stress_relief>0)cat.stress=use.after;if(item.fatigue_relief>0)cat.fatigue=use.after_fatigue;State.item_uses.Add(use);
        }
        private void ValidateItems(CafeSnapshot s)
        {
            if(s.item_purchases==null || s.item_sales==null || s.item_uses==null)throw new ArgumentException("Invalid items");int previous=1,index=1;
            foreach(var p in s.item_purchases){var r=p==null?null:Rules.item_shop.SingleOrDefault(r=>r.item.id==p.item_id);if(r==null || p.source!="item-purchase-"+index++ || p.day<previous || p.day>s.day || p.cost!=r.cost)throw new ArgumentException("Invalid item purchase");previous=p.day;}
            var rewards=ItemRewards(s).ToArray();var consumed=new HashSet<string>();previous=1;
            foreach(var u in s.item_uses)
            {
                var r=u==null?null:rewards.SingleOrDefault(r=>r.source==u.source);var cat=u==null?null:s.cats.SingleOrDefault(c=>c.id==u.cat_id);
                int joined=cat==null?int.MaxValue:Rules.cats.Any(c=>c.id==cat.id)?1:cat.id==Rules.intake.cat.id?s.intake_request.resolved_day:s.regular_introduction.resolved_day;
                if(r==null || cat==null || !consumed.Add(u.source) || u.day<Math.Max(previous,Math.Max(r.day,joined)) || u.day>s.day || !Range(u.before,0,100) || !Range(u.before_fatigue,0,Rules.max_fatigue)
                    || u.before==0 && u.before_fatigue==0 || r.item.stress_relief==0 && u.before!=0 || r.item.fatigue_relief==0 && u.before_fatigue!=0
                    || u.after!=Math.Max(0,u.before-r.item.stress_relief) || u.after_fatigue!=Math.Max(0,u.before_fatigue-r.item.fatigue_relief))throw new ArgumentException("Invalid item use");previous=u.day;
            }
            previous=1;
            foreach(var sale in s.item_sales){var r=sale==null?null:rewards.SingleOrDefault(r=>r.source==sale.source);if(r==null || !consumed.Add(sale.source) || sale.day<Math.Max(previous,r.day) || sale.day>s.day || sale.price!=ItemSalePrice(r.item.id))throw new ArgumentException("Invalid item sale");previous=sale.day;}
            for(int day=1;day<=s.day;day++)
            {
                double cost=s.item_purchases.Where(p=>p.day==day).Sum(p=>p.cost),income=s.item_sales.Where(p=>p.day==day).Sum(p=>p.price);
                double savedCost=day==s.day?s.item_expenses:s.history[day-1].item_expenses,savedIncome=day==s.day?s.item_sales_income:s.history[day-1].item_sales_income;
                if(cost!=savedCost || income!=savedIncome || !Range(savedCost,0,double.MaxValue) || !Range(savedIncome,0,double.MaxValue))throw new ArgumentException("Items disagree with finance history");
            }
        }
    }
}
