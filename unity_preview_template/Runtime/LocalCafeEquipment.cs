using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private string EquipmentEffect(Standalone.EquipmentRule r) => (string.IsNullOrEmpty(r.group)?"通常行動":r.group=="play"?"遊び":r.group=="contact"?"触れ合い":"静かな交流")+"：関心×"+r.engagement+" / テンション×"+r.tension;
        private PythonSeatEquipment EquipmentView()
        {
            var s=Simulation.State;var rules=Simulation.Rules;
            return new PythonSeatEquipment {can_manage=Simulation.EquipmentReason()=="",reason=Simulation.EquipmentReason(),details="営業準備中に購入・配置。同じ設備は1席だけに設置できます。取り外した設備は所持品に残ります。",
                seats=Enumerable.Range(0,s.seat_count).Select(i=>new PythonEquipmentSeat {seat_id="seat-"+(i+1),item_id=s.equipment_placements.Find(p=>p.seat==i)?.item,description=Simulation.Installed(i)?.name??"設備なし"}).ToArray(),
                catalog=rules.equipment.Select(r=>new PythonEquipmentKind {kind_id=r.id,name=r.name,effect=EquipmentEffect(r),cost=(float)r.cost,funds_after=(float)(s.funds-r.cost),can_purchase=Simulation.EquipmentPurchaseReason(r.id)==""}).ToArray(),
                owned=s.equipment_purchases.Select(p=>{var r=rules.equipment.Single(r=>r.id==p.kind);var place=s.equipment_placements.Find(e=>e.item==p.id);return new PythonOwnedEquipment {item_id=p.id,name=r.name,effect=EquipmentEffect(r),day=p.day,cost=(float)r.cost,seat_id=place==null?"":"seat-"+(place.seat+1)};}).ToArray()};
        }
        private string SpecialistDescription()
        {
            if(Simulation.State.objective!="popularity")return "";
            var r=Simulation.Rules;string[] days={"月","火","水","木","金","土","日"};
            return "人気第1段階達成翌日から解放。白猫客：白猫で心をつかむ "+r.advanced_customer.connect_count+"回 / 追加料金 "+r.advanced_customer.bonus+"\n"+
                "静かな交流客（"+string.Join("・",r.play_customer.quiet_weekdays.Select(i=>days[i]))+"）：静かな交流スペース＋声かけ・そばにいる計 "+r.quiet_customer.quiet_count+"回 / 追加料金 "+r.quiet_customer.bonus+" / 人気 "+r.quiet_customer.popularity_bonus+"\n"+
                "遊び客（"+string.Join("・",r.play_customer.weekdays.Select(i=>days[i]))+"）：キャットタワー＋遊び計 "+r.play_customer.play_count+"回 / 追加料金 "+r.play_customer.bonus+" / 人気 "+r.play_customer.popularity_bonus+"\n"+
                "触れ合い客（日）：お手入れブラシ＋なでる・ブラッシング計 "+r.contact_customer.contact_count+"回 / 追加料金 "+r.contact_customer.bonus+" / 人気 "+r.contact_customer.popularity_bonus;
        }
    }
}
