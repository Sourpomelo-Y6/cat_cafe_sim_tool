using System;
namespace CatCafePreview
{
    // Python human_cat_interaction defaults: direct, gentle, pause; preferences fixed at 1.
    public sealed class CatInteractionState
    {
        public float Stamina { get; private set; } = 100;
        public float Engagement { get; private set; }
        public int Turns { get; private set; } = 10;
        public string EndReason { get; private set; } = "";
        public string Reaction { get; private set; } = "交流を始めましょう。";
        private string lastKind = "";
        private int streak;
        public bool Act(string key)
        {
            if (key != "play" && key != "pet" && key != "sleep") throw new ArgumentException("Unknown action");
            if (EndReason != "") return false;
            float before = Stamina;
            if (key == "sleep")
            {
                Stamina = Math.Min(100, Stamina + 2);
                Reaction = "休憩しました。体力 +" + (Stamina - before).ToString("0.#");
            }
            else
            {
                streak = lastKind == key ? streak + 1 : 1;
                lastKind = key;
                float score = (key == "play" ? 12 : 8) * Math.Max(.4f, 1 - .15f * (streak - 1));
                float gain = before <= 20 ? 0 : score < 6 ? score * .5f : score;
                Engagement = Math.Min(100, Engagement + gain);
                Stamina = Math.Max(0, Stamina - (key == "play" ? 5 : 2));
                Reaction = before <= 20 ? "疲れて反応が鈍くなっています。" : score < 6 ? "少し戸惑っています。" : score >= 10 ? "楽しそうに反応しました。" : "穏やかに反応しました。";
            }
            Turns--;
            EndReason = Stamina == 0 ? "体力切れ" : Engagement >= 100 ? "交流成功" : Turns == 0 ? "交流セット終了" : "";
            return true;
        }
        public void NextSet()
        {
            Engagement = 0; Turns = 10; EndReason = ""; lastKind = ""; streak = 0;
            Reaction = "新しい交流セットを始めました。";
            // Prototype: a fresh set also permits resting after exhaustion.
        }
        public string Summary => "体力 " + Stamina.ToString("0.#") + "/100　関心度 " + Engagement.ToString("0.#") + "/100　残り " + Turns + "手\n" + Reaction + (EndReason == "" ? "" : "\n" + EndReason + "。「次の交流」で続けられます。");
    }
}
