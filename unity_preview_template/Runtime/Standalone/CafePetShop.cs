using System;
using System.Linq;
using System.Collections.Generic;
namespace CatCafePreview.Standalone
{
    [Serializable] public class CafeShopPurchase {public string cat_id;public int day;}
    public sealed partial class CafeSimulation
    {
        private IEnumerable<IntakeRules> ShopRules => Rules.pet_shop ?? new IntakeRules[0];
        private int ShopJoinedCount(CafeSnapshot s,int day) => (s.shop_purchases ?? new List<CafeShopPurchase>()).Count(p=>p!=null && p.day<=day);
        private IEnumerable<CatDefinition> ShopDefinitions(CafeSnapshot s) => ShopRules.Where(r=>(s.shop_purchases ?? new List<CafeShopPurchase>()).Any(p=>p!=null && p.cat_id==r.cat.id)).Select(r=>r.cat);
        private double ShopExpenses(CafeSnapshot s,int day) => s.shop_purchases.Where(p=>p.day==day).Sum(p=>ShopRules.Single(r=>r.cat.id==p.cat_id).cost);
        public string CatPurchaseReason(string id)
        {
            string reason=ItemsReason();if(reason!="")return reason;
            var rule=ShopRules.SingleOrDefault(r=>r.cat.id==id);if(rule==null)return "ペットショップの購入候補を選んでください。";
            if(State.shop_purchases.Any(p=>p.cat_id==id))return "この猫はすでに購入しています。";
            if(State.funds<=rule.cost)return "受け入れ後に資金が残る必要があります。";
            return State.cats.Count>=HousingCapacity(State,State.day)?"飼育スペースが満員です。":"";
        }
        private void PurchaseCat(string id)
        {
            string reason=CatPurchaseReason(id);if(reason!="")throw new InvalidOperationException(reason);
            var rule=ShopRules.Single(r=>r.cat.id==id);JoinCat(rule,shop:true);State.shop_purchases.Add(new CafeShopPurchase {cat_id=id,day=State.day});State.revision++;
        }
        private void ValidateShop(CafeSnapshot s)
        {
            if(s.shop_purchases==null || s.shop_purchases.Any(p=>p==null || p.day<1 || p.day>s.day || !ShopRules.Any(r=>r.cat.id==p.cat_id)) || s.shop_purchases.Select(p=>p.cat_id).Distinct().Count()!=s.shop_purchases.Count)throw new ArgumentException("Invalid shop purchase history");
            var ids=new HashSet<string>(Rules.cats.Select(c=>c.id).Concat(new[]{Rules.intake.cat.id,Rules.regular_introduction.cat.id,Rules.visiting_cat?.cat.id}));
            foreach(var rule in ShopRules)
                if(rule?.cat==null || string.IsNullOrEmpty(rule.cat.id) || !ids.Add(rule.cat.id) || !Range(rule.cost,0,double.MaxValue))throw new ArgumentException("Invalid fixed shop candidate");
            for(int day=1;day<=s.day;day++)
            {
                double expected=ShopExpenses(s,day),actual=day==s.day?s.pet_shop_expenses:s.history[day-1].pet_shop_expenses,total=day==s.day?s.expenses:s.history[day-1].expenses;
                if(actual!=expected || total<actual)throw new ArgumentException("Invalid pet shop expenses");
            }
            foreach(var purchase in s.shop_purchases)
                if(!s.cats.Any(c=>c.id==purchase.cat_id))throw new ArgumentException("Missing purchased cat");
        }
    }
}
