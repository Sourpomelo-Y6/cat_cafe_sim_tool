using System;
using System.Linq;

namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private string StoreEventLabel()
        {
            var e = Simulation.CurrentStoreEvent;
            string label = e.type == "festival" ? "地域のお祭り" : e.type == "rain" ? "雨の日" : e.type == "supplies" ? "支援物資" : e.type == "trouble" ? "設備トラブル" : e.type == "support" ? "保護団体からの支援依頼" : "イベントなし";
            return label + (e.choice == null ? "" : "（" + ChoiceLabel(e.choice) + "）");
        }
        private static string ChoiceLabel(string choice) => choice == "repair" ? "修理" : choice == "patch" ? "応急処置" : choice == "close" ? "休業" : choice == "full" ? "支援" : choice == "small" ? "少額支援" : "見送り";
        private bool CanPrepare => !Simulation.PlayerActive && Simulation.State.phase == "preparation" && Simulation.State.game_over == null && !Simulation.State.cats.Any(c => c.activity == "missing" && c.missing_remaining == 0 && !Simulation.DispatchMissing(Simulation.State,c.id));
        private PythonStoreEvent StoreEventView()
        {
            if (!Simulation.StoreEventWaiting) return null;
            var e = Simulation.CurrentStoreEvent; var r = Simulation.Rules.store_events;
            string[] choices = e.type == "trouble" ? new[] { "repair", "patch", "close" } : new[] { "full", "small", "decline" };
            return new PythonStoreEvent { kind = e.type, title = StoreEventLabel(), day = e.day, funds = (float)Simulation.State.funds,
                choices = choices.Select(c => new PythonEventChoice { choice = c, label = ChoiceLabel(c), can_select = CanPrepare && !(c == "close" && (Simulation.DispatchIntroductionWaiting || Simulation.DispatchTroubleWaiting || Simulation.DispatchWaiting || Simulation.DispatchChoiceWaiting || Simulation.IntakeWaiting || Simulation.RegularIntroductionWaiting || Simulation.CustomerTrustWaiting || Simulation.GrowthWaiting || Simulation.GoalWaiting || Simulation.PatronWaiting || Simulation.BondWaiting || Simulation.PlayerActive || Simulation.ReservationWaiting || Simulation.ReservationVisitToday)),
                    detail = c == "repair" ? "閉店時に修理費 " + r.trouble_cost : c == "patch" ? "本日は " + Math.Min(Simulation.State.seat_count, r.trouble_seat_loss) + "席を停止" : c == "close" ? "営業せず運営費を精算し翌日へ" : c == "full" ? "今 " + r.support_full_cost + "支出 / 閉店時の人気 +" + r.support_full_popularity : c == "small" ? "今 " + r.support_small_cost + "支出 / 閉店時の人気 +" + r.support_small_popularity : "費用・人気変化なし" }).ToArray() };
        }
        private static string ItemEffect(Standalone.CafeSupply item) => (item.fatigue_relief>0 ? "疲労 −"+item.fatigue_relief : "") + (item.fatigue_relief>0 && item.stress_relief>0 ? " / " : "") + (item.stress_relief>0 ? "ストレス −"+item.stress_relief : "");
        private PythonItems SupplyView()
        {
            var sim=Simulation;var s=sim.State;var inventory=sim.ItemInventory();
            return new PythonItems { count=inventory.Length,
                shop=sim.Rules.item_shop.Select(r=>new PythonItemShop {choice=r.item.id,name=r.item.name,effect=ItemEffect(r.item),cost=(float)r.cost,funds_after=(float)(s.funds-r.cost),
                    ends_game=s.funds==r.cost,count=inventory.Count(i=>i.item.id==r.item.id),can_select=sim.ItemPurchaseReason(r.item.id)=="",reason=sim.ItemPurchaseReason(r.item.id)}).ToArray(),
                owned=inventory.Select(r=>new PythonOwnedItem {choice=r.source,item_id=r.item.id,name=r.item.name,count=1,effect=ItemEffect(r.item),price=(float)sim.ItemSalePrice(r.item.id),
                    funds_after=(float)(s.funds+sim.ItemSalePrice(r.item.id)),can_sell=sim.ItemSaleReason(r.source)=="",reason=sim.ItemSaleReason(r.source),
                    targets=s.cats.Select(c=>new PythonItemTarget {cat_id=c.id,name=sim.Definition(c.id).name,activity=c.activity,can_select=sim.ItemUseReason(r.source,c.id)=="",reason=sim.ItemUseReason(r.source,c.id),
                        changes=(r.item.fatigue_relief>0?"疲労 "+c.fatigue+" → "+Math.Max(0,c.fatigue-r.item.fatigue_relief):"")+(r.item.fatigue_relief>0 && r.item.stress_relief>0?" / ":"")+(r.item.stress_relief>0?"ストレス "+c.stress+" → "+Math.Max(0,c.stress-r.item.stress_relief):"")}).ToArray()}).ToArray(),
                history=string.Join("\n",s.item_purchases.Select(p=>p.day+"日 購入 "+p.source+"："+p.cost).Concat(s.item_uses.Select(u=>u.day+"日 使用 "+u.source+" → "+sim.Definition(u.cat_id).name)).Concat(s.item_sales.Select(p=>p.day+"日 売却 "+p.source+"："+p.price))) };
        }
    }
}
