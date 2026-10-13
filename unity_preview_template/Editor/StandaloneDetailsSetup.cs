using System;
using UnityEngine;
namespace CatCafePreview.Editor
{
    public static class StandaloneDetailsSetup
    {
        public static void VerifyAndBuild()
        {
            StandaloneDetailsSmoke.Run(Environment.GetEnvironmentVariable("CATCAFE_DETAILS_OUTPUT") ?? "Logs/StandaloneDetailsEditor-"+Guid.NewGuid().ToString("N"),Environment.GetEnvironmentVariable("CATCAFE_DETAILS_REFERENCE"));
            StandaloneScreenSetup.Build();
        }
    }
}
