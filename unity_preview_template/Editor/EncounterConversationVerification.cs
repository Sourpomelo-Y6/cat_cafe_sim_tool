using System;
using System.IO;
using UnityEngine;
namespace CatCafePreview.Editor
{
    public static class EncounterConversationVerification
    {
        public static void VerifyAndBuild()
        {
            ConversationRendererVerification.Run();DispatchConversationVerification.Run();
            string folder=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/StandaloneEncounterConversation/Editor");Directory.CreateDirectory(folder);
            int count=StandaloneEncounterConversationSmoke.BackendChecks(Path.Combine(folder,"cases"),false);File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: "+count+" backend checks");
            StandaloneScreenSetup.Build();
        }
    }
}
