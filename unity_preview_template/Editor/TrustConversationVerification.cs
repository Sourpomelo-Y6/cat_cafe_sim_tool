using System;
using System.IO;
namespace CatCafePreview.Editor {
 public static class TrustConversationVerification {
  public static void VerifyAndBuild(){ConversationRendererVerification.Run();string root=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/TrustConversation/Editor");Directory.CreateDirectory(root);int n=StandaloneTrustConversationSmoke.Run(root);File.WriteAllText(Path.Combine(root,"result.txt"),"PASS: "+n);StandaloneScreenSetup.Build();}
 }
}
