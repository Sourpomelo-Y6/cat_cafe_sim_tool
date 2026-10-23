using System;
using System.IO;
using UnityEngine;
namespace CatCafePreview.Editor
{
    public static class PatronConversationVerification
    {
        public static void VerifyAndBuild()
        {
            string folder=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/PatronRoundTrip/Editor");Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: "+StandalonePatronConversationSmoke.BackendChecks(Path.Combine(folder,"cases"),false));
            File.WriteAllText(Path.Combine(folder,"ending-regression.txt"),"PASS: "+StandalonePatronEndingSmoke.BackendChecks(Path.Combine(folder,"ending-regression"),false));
            File.WriteAllText(Path.Combine(folder,"dispatch-regression.txt"),"PASS: "+StandaloneRoundTripConversationSmoke.BackendChecks(Path.Combine(folder,"dispatch-regression"),false));
            DispatchConversationVerification.Run();StandaloneScreenSetup.Build();
        }
    }
}
