using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    public sealed partial class CafeSimulation
    {
        public string ObjectiveStartReason(string choice)
        {
            if(choice=="popularity")return PopularityChallengeReason();
            if(choice!="bond" && choice!="patron")return "開始する目標を選んでください。";
            if(choice=="bond"?State.bond_goal!=null:State.patron!=null)return "この目標は開始済みです。取消・再開始はできません。";
            return ItemsReason();
        }
        private void StartIndependentObjective(string choice)
        {
            string reason=ObjectiveStartReason(choice);if(reason!="")throw new InvalidOperationException(reason);
            if(choice=="bond"){State.bond_goal=new CafeBondGoal {id="bond",started_day=State.day};EvaluateBond();}
            else State.patron=new CafePatron {id="patron",started_day=State.day,members=Rules.patron.members.Select(r=>new CafePatronScore {id=r.id}).ToList()};
            State.revision++;
        }
    }
}
