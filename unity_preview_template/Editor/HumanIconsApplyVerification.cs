using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace CatCafePreview.Editor {
 public static class HumanIconsApplyVerification {
  public static void VerifyOkadaAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9","guest-10","guest-11","guest-12","advanced-white","advanced-longhair","advanced-quiet","advanced-play","advanced-contact","vip-calico","expanded-guest"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=20)throw new Exception("Twenty customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_OKADA_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyGotoAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9","guest-10","guest-11","guest-12","advanced-white","advanced-longhair","advanced-quiet","advanced-play","advanced-contact","vip-calico"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=19)throw new Exception("Nineteen customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_GOTO_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyAokiAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9","guest-10","guest-11","guest-12","advanced-white","advanced-longhair","advanced-quiet","advanced-play","advanced-contact"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=18)throw new Exception("Eighteen customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_AOKI_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyNishimuraAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9","guest-10","guest-11","guest-12","advanced-white","advanced-longhair","advanced-quiet","advanced-play"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=17)throw new Exception("Seventeen customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_NISHIMURA_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyFukudaAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9","guest-10","guest-11","guest-12","advanced-white","advanced-longhair","advanced-quiet"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=16)throw new Exception("Sixteen customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_FUKUDA_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifySakamotoAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9","guest-10","guest-11","guest-12","advanced-white","advanced-longhair"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=15)throw new Exception("Fifteen customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_SAKAMOTO_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyFujiwaraAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9","guest-10","guest-11","guest-12","advanced-white"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=14)throw new Exception("Fourteen customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_FUJIWARA_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyYamadaAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9","guest-10","guest-11","guest-12"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=13)throw new Exception("Thirteen customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_YAMADA_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyYoshidaAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9","guest-10","guest-11"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=12)throw new Exception("Twelve customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_YOSHIDA_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyKatoAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9","guest-10"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=11)throw new Exception("Eleven customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_KATO_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyKobayashiAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8","guest-9"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=10)throw new Exception("Ten customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_KOBAYASHI_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyNakamuraAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7","guest-8"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=9)throw new Exception("Nine customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_NAKAMURA_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyYamamotoAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6","guest-7"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=8)throw new Exception("Eight customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_YAMAMOTO_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyWatanabeAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=7)throw new Exception("Seven customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_WATANABE_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyItoAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=6)throw new Exception("Six customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_ITO_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyTanakaAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=5)throw new Exception("Five customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_TANAKA_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyTakahashiAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=4)throw new Exception("Four customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_TAKAHASHI_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyAndBuild(){
   string folder=Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED");ContentPreviewSetup.Import(folder);var textures=new[]{"guest-1","guest-2","reservation-longhair"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=3)throw new Exception("Three customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("temporary-sato")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer did not fall back");
   // Apply the existing policy to referenced cat textures (including historical packs) before the ordinary build guard.
   CatTexturePolicy.ApplyAll();
   Debug.Log("CAT_CAFE_THREE_ICONS_IMPORTED");StandaloneScreenSetup.Build();
  }
 }
}
