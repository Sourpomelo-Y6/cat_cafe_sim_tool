using System;
using System.IO;
namespace CatCafePreview.Editor
{
    public static class UIFixVerification
    {
        public static void VerifyAndBuild()
        {
            string root=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/UIFix1to7/Editor");Directory.CreateDirectory(root);
            int count=StandaloneAutoplaySmoke.Run(Path.Combine(root,"autoplay"));
            File.WriteAllText(Path.Combine(root,"result.txt"),"PASS: "+count+" autoplay regression checks");
            StandaloneScreenSetup.Build();
        }
    }
}
