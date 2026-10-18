using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeImageProfile {public int version;public string[] viewed=new string[0];}
    // Identity follows the actual bitmap, so replacing an image does not unlock its unseen replacement.
    public static class CafeImageGallery
    {
        public static string Key(string image)=>Regex.IsMatch(image??"","^images/[a-f0-9]{64}\\.png$")?"normal/"+image.Substring(7,64):null;
        public static void Validate(CafeImageProfile p)
        {
            if(p==null || p.version!=1 || p.viewed==null || p.viewed.Length>10000 || p.viewed.Distinct().Count()!=p.viewed.Length || p.viewed.Any(k=>!Regex.IsMatch(k??"","^normal/[a-f0-9]{64}$")))throw new ArgumentException("CG閲覧記録の形式が不正です。");
        }
    }
    public sealed class CafeImageGalleryStore
    {
        readonly string path;readonly Func<CafeImageProfile,string> encode;readonly Func<string,CafeImageProfile> decode;
        public string Error {get;private set;}
        public bool Recovered {get;private set;}
        public CafeImageGalleryStore(string directory,Func<CafeImageProfile,string> encode,Func<string,CafeImageProfile> decode){path=Path.Combine(directory,"extra-images-v1.json");this.encode=encode;this.decode=decode;}
        CafeImageProfile Read(string file){var p=decode(File.ReadAllText(file,Encoding.UTF8));CafeImageGallery.Validate(p);return p;}
        public CafeImageProfile Load()
        {
            Error=null;Recovered=false;if(!File.Exists(path) && !File.Exists(path+".bak"))return new CafeImageProfile{version=1};
            try{return Read(path);}catch(Exception){try{var p=Read(path+".bak");Recovered=true;return p;}catch(Exception){Error="CG閲覧記録を読み込めません。記録は上書きしていません。";return new CafeImageProfile{version=1};}}
        }
        public bool Merge(IEnumerable<string> keys)
        {
            var p=Load();if(Error!=null)return false;bool recovered=Recovered;var rows=p.viewed.Concat(keys).Distinct().OrderBy(k=>k,StringComparer.Ordinal).ToArray();var next=new CafeImageProfile{version=1,viewed=rows};CafeImageGallery.Validate(next);
            if(rows.Length==p.viewed.Length && !recovered)return true;string temp=path+".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));using(var f=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){var bytes=Encoding.UTF8.GetBytes(encode(next));f.Write(bytes,0,bytes.Length);f.Flush(true);}Read(temp);
                if(File.Exists(path))CafeSaveStore.ReplaceWithRetry(temp,path,(s,t,b)=>File.Replace(s,t,recovered?path+".invalid":b));else File.Move(temp,path);return true;
            }
            catch(Exception){Error="CG閲覧記録を保存できません。営業セーブは変更していません。";return false;}
            finally{if(File.Exists(temp))try{File.Delete(temp);}catch(IOException){}}
        }
    }
}
