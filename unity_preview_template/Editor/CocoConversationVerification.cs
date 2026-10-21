using System;
using System.IO;
namespace CatCafePreview.Editor {
 public static class CocoConversationVerification {
  public static void VerifyAndBuild(){string root=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/CocoConversation/Editor");Directory.CreateDirectory(root);int n=StandaloneCocoConversationSmoke.Run(root);File.WriteAllText(Path.Combine(root,"result.txt"),"PASS coco: "+n);StandaloneScreenSetup.Build();}
 }
}
