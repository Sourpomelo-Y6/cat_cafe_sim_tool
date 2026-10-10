using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public static class PythonDispatchDisplay
    {
        public static PythonDispatchComparisonRow Find(PythonCafeState state,string catId,string kind,string choice){var cat=Array.Find(state?.dispatch_comparison?.cats ?? new PythonDispatchComparisonCat[0],r=>r.cat_id==catId);return Array.Find(cat?.rows ?? new PythonDispatchComparisonRow[0],r=>r.kind==kind && r.choice==choice);}
        public static string N(float value)=>value.ToString("0.#");
        public static string Status(PythonDispatchEvent row)=>row.status=="travelling"?(row.needs_response?"選択待ち":"派遣中"):row.status=="waiting"?"帰還待ち":row.status=="missing"?"行方不明":row.status=="resolved"?"受取済み":"対応待ち";
        public static void Size(Text text){var scroll=text.GetComponentInParent<ScrollRect>(true);if(scroll!=null)text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(scroll.viewport.rect.height,text.preferredHeight+12));}
        public static string Terms(PythonCafeState state,string catId,string kind,PythonDispatchOption option){if(option==null)return "この猫の派遣先はありません。";var row=Find(state,catId,kind,option.choice);return "費用："+(row==null?"詳細で確認":N(row.cost))+" / 帰還まで："+option.days+"回の閉店\n報酬："+N(option.reward)+(kind=="patron"?" / 満足度加点 ＋"+N(option.satisfaction):"")+"\n参加条件："+(option.can_select?"派遣できます。":option.reason)+(string.IsNullOrEmpty(row?.unlock)?"":"\n解放条件："+row.unlock)+"\n\n"+option.detail;}
        public static string Summary(PythonCafeState state,string catId,string kind,PythonDispatchOption option){var cat=Array.Find(state?.cats ?? new PythonCatState[0],r=>r.cat_id==catId);var row=Find(state,catId,kind,option?.choice);return "対象："+(cat?.name ?? "猫なし")+" / "+(cat==null?"":PythonCatCard.Activity(cat,false))+" / 資金："+N(state?.funds ?? 0)+"\n"+(option==null?"この猫の派遣先はありません。":"費用："+(row==null?"詳細で確認":N(row.cost))+" / 帰還まで："+option.days+"回の閉店 / 報酬："+N(option.reward));}
    }
}
