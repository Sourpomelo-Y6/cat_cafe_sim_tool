using System;
using System.IO;
using UnityEngine;
namespace CatCafePreview.Editor
{
    public static class CGGalleryVerification
    {
        public static void VerifyAndBuild()
        {
            ConversationRendererVerification.Run();DispatchConversationVerification.Run();
            string folder=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/StandaloneCGGallery/Editor");Directory.CreateDirectory(folder);int count=StandaloneCGGallerySmoke.BackendChecks(Path.Combine(folder,"cases"),false);File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: "+count+" CG backend checks");StandaloneScreenSetup.Build();
        }
    }
}
