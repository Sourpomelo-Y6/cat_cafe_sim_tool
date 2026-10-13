using System;
namespace CatCafePreview.Editor
{
    public static class StandaloneDailySetup
    {
        public static void VerifyAndBuild()
        {
            StandaloneDailySmoke.Run(Environment.GetEnvironmentVariable("CATCAFE_DAILY_OUTPUT"),Environment.GetEnvironmentVariable("CATCAFE_DAILY_REFERENCE"));
            StandaloneScreenSetup.Build();
        }
    }
}
