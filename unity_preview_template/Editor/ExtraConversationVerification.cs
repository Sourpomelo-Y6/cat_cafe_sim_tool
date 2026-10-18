using System;
using System.IO;
using UnityEngine;
namespace CatCafePreview.Editor
{
    public static class ExtraConversationVerification
    {
        public static void VerifyAndBuild()
        {
            ConversationRendererVerification.Run();DispatchConversationVerification.Run();
            string folder=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/StandaloneExtraConversation/Editor");Directory.CreateDirectory(folder);
            int checks=StandaloneExtraConversationSmoke.BackendChecks(Path.Combine(folder,"cases"),false);File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: "+checks+" Extra backend checks");StandaloneScreenSetup.Build();
        }
    }
}
