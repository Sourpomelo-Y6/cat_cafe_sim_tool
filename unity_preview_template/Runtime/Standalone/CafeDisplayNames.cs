using System;
using System.Collections.Generic;
using System.Linq;
namespace CatCafePreview {
 [Serializable] public class ContentDisplayName { public string id,label; }
 [Serializable] public class ContentDisplayLabels { public int version; public ContentDisplayName[] modes,actions,reactions; }
 public static class CafeDisplayNames {
  static readonly string[][] Keys={new[]{"teaser","ball","plush","tunnel","pet","brush","voice","presence","pause"},new[]{"direct","adapt","intense","feint","pause","switch","connect"},new[]{"neutral","favorable","enthusiastic","turn_away","confused","listless","connect","open_up","simultaneous","received"}};
  public static void Validate(ContentDisplayLabels data) {
   if(data==null)return;if(data.version!=1)throw new ArgumentException("Unsupported display label version");
   var groups=new[]{data.modes,data.actions,data.reactions};
   for(int i=0;i<groups.Length;i++) {
    var ids=new HashSet<string>();var names=new HashSet<string>();
    foreach(var row in groups[i]??new ContentDisplayName[0])if(row==null || !Keys[i].Contains(row.id) || !ids.Add(row.id) || string.IsNullOrWhiteSpace(row.label) || row.label!=row.label.Trim() || row.label.Length>32 || row.label.Any(c=>char.IsControl(c) || c=='<' || c=='>') || !names.Add(row.label))throw new ArgumentException("Invalid display label");
   }
  }
  public static string Resolve(ContentDisplayLabels data,string group,string id,string fallback) {
   var rows=data==null?null:group=="modes"?data.modes:group=="actions"?data.actions:group=="reactions"?data.reactions:null;
   return rows?.FirstOrDefault(r=>r.id==id)?.label??fallback;
  }
  public static Func<string,string,string,string> Lookup;
  public static string Label(string group,string id,string fallback)=>Lookup==null?fallback:Lookup(group,id,fallback);
 }
}
