using System;
using System.IO;
namespace CatCafePreview.Editor {
 public static class EquipmentConversationVerification {
  public static void VerifyAndBuild(){string root=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/EquipmentConversation/Editor");Directory.CreateDirectory(root);int n=StandaloneEquipmentConversationSmoke.Run(root);File.WriteAllText(Path.Combine(root,"result.txt"),"PASS: "+n);StandaloneScreenSetup.Build();}
 }
}
