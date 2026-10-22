using System;
using System.IO;
using UnityEngine;
namespace CatCafePreview.Editor
{
    public static class EncounterConversationVerification
    {
        public static void VerifyAndBuild()
        {
            // Legacy integration runner requires migration for daily result conversations.
            // Exercise the encounter backend here and the shared renderer in the Player smoke.
            string folder=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/StandaloneEncounterConversation/Editor");Directory.CreateDirectory(folder);
            int count=StandaloneEncounterConversationSmoke.BackendChecks(Path.Combine(folder,"cases"),false);File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: "+count+" backend checks");
            StandaloneScreenSetup.Build();
        }
    }
}
