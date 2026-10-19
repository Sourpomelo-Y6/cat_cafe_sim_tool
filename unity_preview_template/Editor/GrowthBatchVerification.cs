using System;
using System.IO;
namespace CatCafePreview.Editor
{
 public static class GrowthBatchVerification
 {
  public static void VerifyAndBuild(){var root=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/GrowthBatch/Editor");Directory.CreateDirectory(root);int n=StandaloneGrowthBatchSmoke.BackendCases(root);n+=StandaloneGrowthSmoke.Run(Path.Combine(root,"growth-regression"));File.WriteAllText(Path.Combine(root,"result.txt"),"PASS "+n);StandaloneScreenSetup.Build();}
 }
}
