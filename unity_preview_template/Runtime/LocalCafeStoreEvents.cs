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
        private bool CanPrepare => Simulation.State.phase == "preparation" && Simulation.State.game_over == null && !Simulation.State.cats.Any(c => c.activity == "missing" && c.missing_remaining == 0);
        private PythonStoreEvent StoreEventView()
        {
            if (!Simulation.StoreEventWaiting) return null;
            var e = Simulation.CurrentStoreEvent; var r = Simulation.Rules.store_events;
            string[] choices = e.type == "trouble" ? new[] { "repair", "patch", "close" } : new[] { "full", "small", "decline" };
            return new PythonStoreEvent { kind = e.type, title = StoreEventLabel(), day = e.day, funds = (float)Simulation.State.funds,
                choices = choices.Select(c => new PythonEventChoice { choice = c, label = ChoiceLabel(c), can_select = CanPrepare && !(c == "close" && (Simulation.IntakeWaiting || Simulation.RegularIntroductionWaiting || Simulation.CustomerTrustWaiting)),
                    detail = c == "repair" ? "閉店時に修理費 " + r.trouble_cost : c == "patch" ? "本日は " + Math.Min(2, r.trouble_seat_loss) + "席を停止" : c == "close" ? "営業せず運営費を精算し翌日へ" : c == "full" ? "今 " + r.support_full_cost + "支出 / 閉店時の人気 +" + r.support_full_popularity : c == "small" ? "今 " + r.support_small_cost + "支出 / 閉店時の人気 +" + r.support_small_popularity : "費用・人気変化なし" }).ToArray() };
        }
        private PythonItems SupplyView()
        {
            var r = Simulation.Rules.store_events.supply;
            return new PythonItems { shop = new PythonItemShop[0], count = Simulation.AvailableSupplies.Length,
                owned = Simulation.AvailableSupplies.Select(source => new PythonOwnedItem { choice = source, item_id = r.id, name = r.name, count = 1, effect = "ストレス −" + r.stress_relief,
                    targets = Simulation.State.cats.Select(cat => new PythonItemTarget { cat_id = cat.id, name = Simulation.Definition(cat.id).name,
                        can_select = CanPrepare && !Simulation.CustomerTrustWaiting && !Simulation.IntakeWaiting && !Simulation.RegularIntroductionWaiting && !Simulation.StoreEventWaiting && cat.activity == "cafe" && cat.stress > 0,
                        changes = cat.stress + " → " + Math.Max(0, cat.stress - r.stress_relief) }).ToArray() }).ToArray() };
        }
    }
}
