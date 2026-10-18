using System;
using System.IO;
using UnityEngine;
namespace CatCafePreview.Editor
{
    public static class OptionsVerification
    {
        public static void VerifyAndBuild()
        {
            ConversationRendererVerification.Run();DispatchConversationVerification.Run();string folder=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/StandaloneOptions/Editor");Directory.CreateDirectory(folder);int count=StandaloneOptionsSmoke.StorageChecks(Path.Combine(folder,"storage"));File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: "+count+" option storage checks");StandaloneScreenSetup.Build();
        }
    }
}
