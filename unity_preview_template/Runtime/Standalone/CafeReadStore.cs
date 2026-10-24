using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
namespace CatCafePreview.Standalone {
 [Serializable] public sealed class CafeReadRange { public string key; public int start,end; }
 [Serializable] public sealed class CafeReadProfile { public int version=1; public CafeReadRange[] ranges=new CafeReadRange[0]; }
 // Independent of business saves and Extra unlocks. Only acknowledged, fully visible pages enter this store.
 public sealed class CafeReadStore {
  readonly string path; readonly Func<CafeReadProfile,string> encode; readonly Func<string,CafeReadProfile> decode;
  CafeReadProfile profile; public string Error {get;private set;}
  public CafeReadStore(string directory,Func<CafeReadProfile,string> encode,Func<string,CafeReadProfile> decode){path=Path.Combine(directory,"conversation-read-v1.json");this.encode=encode;this.decode=decode;profile=Load();}
  static void Validate(CafeReadProfile p){if(p==null || p.version!=1 || p.ranges==null || p.ranges.Length>100000 || p.ranges.Any(r=>r==null || r.key==null || r.key.Length!=64 || r.key.Any(c=>!Uri.IsHexDigit(c)) || r.start<0 || r.end<=r.start || r.end>100000))throw new ArgumentException("Invalid read profile");}
  CafeReadProfile Read(string file){string json=File.ReadAllText(file,Encoding.UTF8);if(!json.Contains("\"version\"") || !json.Contains("\"ranges\""))throw new ArgumentException();var p=decode(json);Validate(p);return p;}
  CafeReadProfile Load(){Error=null;if(!File.Exists(path))return new CafeReadProfile();try{return Read(path);}catch(Exception){Error="既読記録を読み込めません。安全のため未読として扱います。";return new CafeReadProfile();}}
  public static string Key(string id,string branch,CafeConversationLine[] lines,int node){
   // Length framing prevents ambiguous concatenation; the entire branch acts as its exact text revision.
   var b=new StringBuilder();foreach(var s in new[]{"read-v1",id,branch,node.ToString()}.Concat(lines.SelectMany(l=>new[]{l.speaker,l.text}))){var t=s??"";b.Append(t.Length).Append(':').Append(t);}
   using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(b.ToString()))).Replace("-","").ToLowerInvariant();
  }
  public bool Contains(string key,int start,int end){int covered=start;foreach(var r in profile.ranges.Where(r=>r.key==key).OrderBy(r=>r.start)){if(r.start>covered)break;covered=Math.Max(covered,r.end);if(covered>=end)return true;}return false;}
  public bool Remember(string key,int start,int end){
   if(start>=end)return false;profile=Load();if(Error!=null)return false;if(Contains(key,start,end))return true;
   int low=start,high=end;var keep=profile.ranges.Where(r=>r.key!=key || r.end<start || r.start>end).ToList();foreach(var r in profile.ranges.Where(r=>r.key==key && r.end>=start && r.start<=end)){low=Math.Min(low,r.start);high=Math.Max(high,r.end);}keep.Add(new CafeReadRange{key=key,start=low,end=high});var next=new CafeReadProfile{ranges=keep.ToArray()};string temp=path+".tmp";
   try{Validate(next);Directory.CreateDirectory(Path.GetDirectoryName(path));using(var f=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){var bytes=Encoding.UTF8.GetBytes(encode(next));f.Write(bytes,0,bytes.Length);f.Flush(true);}Read(temp);if(File.Exists(path))CafeSaveStore.ReplaceWithRetry(temp,path,(s,t,b)=>File.Replace(s,t,b));else File.Move(temp,path);profile=next;return true;}
   catch(Exception){Error="既読記録を保存できません。未保存部分は再起動後に未読として扱います。";return false;}
   finally{if(File.Exists(temp))try{File.Delete(temp);}catch(IOException){}}
  }
 }
}
