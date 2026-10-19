using System;
using System.IO;
namespace CatCafePreview.Editor
{
    public static class OpeningVerification
    {
        public static void VerifyAndBuild()
        {
            string path=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/StandaloneOpening/Editor");Directory.CreateDirectory(path);
            int count=StandaloneOpeningSmoke.BackendChecks(Path.Combine(path,"cases-"+Guid.NewGuid().ToString("N")),false);File.WriteAllText(Path.Combine(path,"result.txt"),"PASS "+count+" opening backend checks");
            ConversationRendererVerification.Run();DispatchConversationVerification.Run();StandaloneScreenSetup.Build();
        }
    }
}
