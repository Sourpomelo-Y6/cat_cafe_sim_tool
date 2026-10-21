using System;
using System.IO;
namespace CatCafePreview.Editor {
 public static class SupportConversationVerification {
  public static void VerifyAndBuild(){string root=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/SupportConversation/Editor");Directory.CreateDirectory(root);int n=StandaloneSupportConversationSmoke.Run(root);int equipment=StandaloneEquipmentConversationSmoke.Run(Path.Combine(root,"equipment-regression"));File.WriteAllText(Path.Combine(root,"result.txt"),"PASS support: "+n+"; equipment: "+equipment);StandaloneScreenSetup.Build();}
 }
}
