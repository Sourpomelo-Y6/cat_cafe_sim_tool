using System;
using System.Linq;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        // Run the real command on an isolated snapshot; observing a preview never settles a day.
        private PythonDayOff DayOffView()
        {
            var s=Simulation.State;
            var row=new PythonDayOff();
            try
            {
                var copy=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(s));
                copy.NormalizeJsonStrings();CafeDoublePrecision.Restore(copy,CafeDoublePrecision.Capture(s));
                var after=Simulation.PreviewDayOff(_=>copy);
                row.can_select=true;row.cost=(float)(s.funds-after.funds);row.funds_after=(float)after.funds;
                row.day_after=after.day;row.ends_game=after.game_over!=null;
                row.details="来客・接客なしで休業します。\n支出："+row.cost.ToString("0.#")+" / 資金："+s.funds.ToString("0.#")+" → "+row.funds_after.ToString("0.#")
                    +"\n"+(row.ends_game?"精算で経営終了となり、翌日には進みません。":after.day+"日目の準備へ進みます。目標・派遣・療養の日程も進みます。")
                    +"\n\n"+string.Join("\n\n",s.cats.Select(c=>{var a=after.cats.Find(v=>v.id==c.id);return Simulation.Definition(c.id).name+"\n体力 "+c.stamina.ToString("0.#")+" → "+a.stamina.ToString("0.#")+" / 疲労 "+c.fatigue.ToString("0.#")+" → "+a.fatigue.ToString("0.#")+" / ストレス "+c.stress.ToString("0.#")+" → "+a.stress.ToString("0.#")+"\n健康："+(a.health=="healthy"?"健康":"療養あと"+a.recovery_remaining+"日")+" / 活動："+(a.activity=="cafe"?"在店":a.activity=="dispatched"?"派遣中":a.activity=="adopted"?"譲渡済み":"家出中");}));
            }
            catch(InvalidOperationException ex)
            {
                row.reason=ex.Message+"\n"+(Simulation.PlayerActive?"「交流へ戻る」から、このセットを終了してください。":Simulation.ReservationVisitToday?"本日は営業し、予約の接客を行ってください。":s.game_over!=null?"経営結果を確認してください。":s.phase=="open"?"営業を閉店まで進めてください。":s.phase=="closed"?"閉店結果を確認し、翌日へ進めてください。":"イベント一覧の「回答・会話待ち」から該当イベントを開き、回答・結果確認を完了してください。受け入れ・紹介は「見送る」でも解消できます。");
            }
            if(s.autoplay?.running==true){row.can_select=false;row.reason="おまかせ実行中です。おまかせ画面で中止してから休業してください。";}
            return row;
        }
    }
}
