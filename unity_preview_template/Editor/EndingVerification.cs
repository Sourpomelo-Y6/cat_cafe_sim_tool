using System;
using System.IO;
namespace CatCafePreview.Editor
{
    public static class EndingVerification
    {
        public static void VerifyAndBuild(){ConversationRendererVerification.Run();DispatchConversationVerification.Run();string path=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/StandaloneEnding/Editor");Directory.CreateDirectory(path);int count=StandaloneEndingSmoke.BackendChecks(Path.Combine(path,"cases-"+Guid.NewGuid().ToString("N")),false);File.WriteAllText(Path.Combine(path,"result.txt"),"PASS: "+count+" ending backend checks");StandaloneScreenSetup.Build();}
    }
}
