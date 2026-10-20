using System;
using System.IO;
using UnityEngine;
namespace CatCafePreview.Editor
{
    public static class LodgeConversationVerification
    {
        public static void VerifyAndBuild(){ConversationRendererVerification.Run();string folder=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/LodgeConversation/Editor");Directory.CreateDirectory(folder);int count=StandaloneLodgeConversationSmoke.BackendChecks(Path.Combine(folder,"cases"),false);File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: "+count);StandaloneScreenSetup.Build();}
    }
}
