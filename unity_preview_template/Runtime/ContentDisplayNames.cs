using System;
using UnityEngine;
namespace CatCafePreview {
 // Unity adapter; the standalone simulation keeps display definitions free of Unity dependencies.
 public static class ContentDisplayNames {
  static ContentDisplayLabels current;static bool loaded;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  public static void Install() {current=null;loaded=false;CafeDisplayNames.Lookup=Label;}
  public static void Validate(ContentDisplayLabels data)=>CafeDisplayNames.Validate(data);
  public static string Resolve(ContentDisplayLabels data,string group,string id,string fallback)=>CafeDisplayNames.Resolve(data,group,id,fallback);
  public static string Label(string group,string id,string fallback) {
   if(!loaded) {
    loaded=true;current=null;
    try {
     var pointer=Resources.Load<TextAsset>("CatCafeContent/catalog");
     string key=pointer==null?null:JsonUtility.FromJson<ContentCatalog>(pointer.text)?.bundleResource;
     if(!string.IsNullOrEmpty(key)){var text=Resources.Load<TextAsset>(key);if(text!=null){var data=JsonUtility.FromJson<ContentBundle>(text.text).display_labels;Validate(data);current=data;}}
    } catch(Exception ex){Debug.LogWarning("表示名は標準を使用します："+ex.Message);}
   }
   return Resolve(current,group,id,fallback);
  }
 }
}
