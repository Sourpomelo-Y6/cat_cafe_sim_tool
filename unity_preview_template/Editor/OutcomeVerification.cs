using System;using System.IO;
namespace CatCafePreview.Editor {public static class OutcomeVerification {
 public static void VerifyAndBuild(){string path=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/StandaloneOutcome/Editor");Directory.CreateDirectory(path);int n=StandaloneOutcomeSmoke.BackendChecks(Path.Combine(path,"cases"),false);int a=StandaloneEndingSmoke.BackendChecks(Path.Combine(path,"popularity"),false);int b=StandaloneBondEndingSmoke.BackendChecks(Path.Combine(path,"bond"),false);int c=StandalonePatronEndingSmoke.BackendChecks(Path.Combine(path,"patron"),false);File.WriteAllText(Path.Combine(path,"result.txt"),"PASS: "+n+" outcome; "+a+" popularity; "+b+" bond; "+c+" patron");StandaloneScreenSetup.Build();}
}}
