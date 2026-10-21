using System;
using System.IO;
namespace CatCafePreview.Editor {
 public static class ReservationConversationVerification {
  public static void VerifyAndBuild(){ConversationRendererVerification.Run();string root=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/ReservationConversation/Editor");Directory.CreateDirectory(root);int n=StandaloneReservationConversationSmoke.Run(root);File.WriteAllText(Path.Combine(root,"result.txt"),"PASS: "+n);StandaloneScreenSetup.Build();}
 }
}
