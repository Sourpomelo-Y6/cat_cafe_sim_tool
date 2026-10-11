using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonPetShop PetShopView()
        {
            var s=Simulation.State;return new PythonPetShop {housing="在籍 "+s.cats.Count+" / 上限 "+Simulation.HousingCapacity(s,s.day),notes="候補と価格は新規開始時の設定で固定されます。準備中に購入でき、同じ候補は再購入できません。",
                rows=(Simulation.Rules.pet_shop ?? new Standalone.IntakeRules[0]).Select(r=>
                {
                    var purchase=s.shop_purchases.SingleOrDefault(p=>p.cat_id==r.cat.id);string reason=Simulation.CatPurchaseReason(r.cat.id);
                    return new PythonShopCat {cat_id=r.cat.id,content_cat_id=r.cat.id,name=r.cat.name,cost=(float)r.cost,funds_after=(float)(s.funds-r.cost),accepted=purchase!=null,accepted_day=purchase?.day??0,can_purchase=reason=="",reason=reason,
                        details="名前 / 個性："+r.cat.name+" / "+r.personality+"\n特徴："+r.features_label+" / "+r.trait+"\n価格："+r.cost+" / 購入後の資金："+(s.funds-r.cost)+"\n"+(purchase==null?"健康・体力全回復・休養予定。疲労・ストレス・好感度・経験は0で加入します。":purchase.day+"日目に購入済み。現在の状態は猫一覧で確認できます。")};
                }).ToArray()};
        }
    }
}
