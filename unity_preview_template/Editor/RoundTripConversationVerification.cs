using System;
using System.IO;
using UnityEngine;
namespace CatCafePreview.Editor
{
    public static class RoundTripConversationVerification
    {
        public static void VerifyAndBuild()
        {
            string folder=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/StandaloneRoundTripConversation/Editor");Directory.CreateDirectory(folder);
            int checks=StandaloneRoundTripConversationSmoke.BackendChecks(Path.Combine(folder,"cases"),false);
            File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: "+checks);
            DispatchConversationVerification.Run();
            int introductions=StandaloneDispatchIntroductionConversationSmoke.BackendChecks(Path.Combine(folder,"introduction-regression"),false);
            File.WriteAllText(Path.Combine(folder,"introduction-regression.txt"),"PASS: "+introductions);
            StandaloneScreenSetup.Build();
        }
    }
}
