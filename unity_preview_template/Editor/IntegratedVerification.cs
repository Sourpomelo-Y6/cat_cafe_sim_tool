using System;
using System.IO;
namespace CatCafePreview.Editor
{
    public static class IntegratedVerification
    {
        public static void VerifyAndBuild()
        {
            string root=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/IntegratedPlaytest/Editor");
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root,"autoplay-pause.txt"),"PASS "+StandaloneIntegratedSmoke.AutoplayPauseChecks(Path.Combine(root,"pause-cases")));
            StandaloneScreenSetup.Build();
        }
    }
}
