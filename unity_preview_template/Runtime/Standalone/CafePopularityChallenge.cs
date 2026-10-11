using System;
namespace CatCafePreview.Standalone
{
    public sealed partial class CafeSimulation
    {
        public string PopularityChallengeReason()
        {
            if(State.objective=="popularity" || State.goal.Count>0)return "人気挑戦は開始済みです。取消・再挑戦はできません。";
            return ItemsReason();
        }
        private void StartPopularityChallenge()
        {
            string reason=PopularityChallengeReason();if(reason!="")throw new InvalidOperationException(reason);
            State.challenge_started_day=State.day;State.goal.Add(new CafeGoalStage {started_day=State.day});State.revision++;
        }
    }
}
