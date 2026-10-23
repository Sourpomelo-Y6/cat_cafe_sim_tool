using System;
using System.IO;
namespace CatCafePreview.Editor {
 public static class PendingCasesVerification {
  public static void VerifyAndBuild(){var root=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/PendingCases/Editor");Directory.CreateDirectory(root);int n=StandaloneTrustConversationSmoke.Run(Path.Combine(root,"Trust"));n+=StandaloneEquipmentConversationSmoke.Run(Path.Combine(root,"Equipment"));n+=StandaloneDispatchConversationSmoke.BackendChecks(Path.Combine(root,"Dispatch"),false);n+=StandaloneRoundTripConversationSmoke.BackendChecks(Path.Combine(root,"RoundTrip"),false);n+=StandaloneEncounterConversationSmoke.BackendChecks(Path.Combine(root,"Encounter"),false);n+=StandaloneDispatchIntroductionConversationSmoke.BackendChecks(Path.Combine(root,"Introduction"),false);n+=StandaloneIntegratedSmoke.AutoplayPauseChecks(Path.Combine(root,"Autoplay"));File.WriteAllText(Path.Combine(root,"result.txt"),"PASS "+n);StandaloneScreenSetup.Build();}
 }
}
