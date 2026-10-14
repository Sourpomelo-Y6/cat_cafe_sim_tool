using System;
namespace CatCafePreview.Editor
{
    public static class StandaloneGoalPlaytestSetup
    {
        public static void VerifyAndBuild()
        {
            StandaloneGoalProjectionSmoke.Run(Environment.GetEnvironmentVariable("CATCAFE_GOAL_QA_OUTPUT"),Environment.GetEnvironmentVariable("CATCAFE_GOAL_OBSERVED_SAVE"),Environment.GetEnvironmentVariable("CATCAFE_GOAL_OBSERVED_DIRECTORY"));
            StandaloneScreenSetup.Build();
        }
    }
}
