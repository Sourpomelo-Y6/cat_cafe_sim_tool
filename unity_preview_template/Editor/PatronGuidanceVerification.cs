using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace CatCafePreview.Editor
{
 public static class PatronGuidanceVerification
 {
  public static void VerifyAndBuild()
  {
   string root=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/PatronGuidance");Directory.CreateDirectory(root);int n=0;
   Action<bool,string> check=(ok,label)=>{n++;if(!ok)throw new Exception(label);};
   var b=new LocalCafeBackend(Path.Combine(root,"guidance-fixture"),StandaloneGrowthBatchSmoke.Rules());
   StandaloneGrowthBatchSmoke.Send(b,"new_game",choice:"patron");
   var sim=b.Simulation;var method=typeof(LocalCafeBackend).GetMethod("PatronGrowthGuide",BindingFlags.NonPublic|BindingFlags.Instance);
   Func<CatCafePreview.Standalone.CafeGrowth,string> guide=g=>(string)method.Invoke(b,new object[]{g});
   foreach(var g in sim.State.growth)
   {
    bool white=sim.Definition(g.cat_id).features.Contains("white");
    foreach(string spec in new string[]{null,"service","rest"})
    {
     g.specialization=spec;string text=guide(g);
     check(text.Contains("条件に厳格")==white,"Appearance filtering");
     if(white)check(text.Contains(spec==null?"専門未確定":spec=="service"?"専門は一致":"変更できません"),"Specialization guidance");
    }
    g.specialization=null;
   }
   var adopted=sim.State.cats.First(c=>sim.Definition(c.id).features.Contains("white"));adopted.activity="adopted";check(guide(sim.GrowthCat(adopted.id))=="","Adopted cat guidance");adopted.activity="cafe";
   sim.State.growth[0].service=sim.Rules.growth.threshold;string before=JsonUtility.ToJson(sim.State);string recommendation=sim.RecommendGrowth(sim.State.growth[0]).reason;
   for(int i=0;i<5;i++)b.View();
   check(before==JsonUtility.ToJson(sim.State),"View mutated state");check(recommendation==sim.RecommendGrowth(sim.State.growth[0]).reason,"View changed recommendation");
   check(b.View().patron_goal.details.Contains("白猫 かつ 接客専門"),"Configured strict terms missing");
   sim.State.patron.status="cleared";check(sim.State.growth.All(g=>guide(g)==""),"Cleared goal warning");sim.State.patron=null;check(sim.State.growth.All(g=>guide(g)==""),"Unstarted goal warning");
   n+=StandaloneGrowthBatchSmoke.BackendCases(Path.Combine(root,"growth-regression"));n+=StandaloneGrowthSmoke.Run(Path.Combine(root,"growth-legacy"));
   n+=StandalonePatronConversationSmoke.BackendChecks(Path.Combine(root,"patron-regression"),false);n+=StandalonePatronConversationSmoke.BackendChecks(Path.Combine(root,"patron-regression"),true);
   File.WriteAllText(Path.Combine(root,"editor-result.txt"),"PASS "+n);StandaloneScreenSetup.Build();
  }
 }
}
