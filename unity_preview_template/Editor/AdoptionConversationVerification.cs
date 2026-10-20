using System;
using System.IO;
namespace CatCafePreview.Editor
{
    public static class AdoptionConversationVerification
    {
        public static void VerifyAndBuild(){ConversationRendererVerification.Run();string root=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/AdoptionConversation/Editor");Directory.CreateDirectory(root);int n=StandaloneAdoptionConversationSmoke.BackendChecks(root,false);File.WriteAllText(Path.Combine(root,"result.txt"),"PASS: "+n);StandaloneScreenSetup.Build();}
    }
}
