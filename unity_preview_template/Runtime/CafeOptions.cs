using System;
using System.IO;
using System.Text;
using UnityEngine;
namespace CatCafePreview
{
    [Serializable] public sealed class CafeOptionsData
    {
        public int version=1,charactersPerSecond=40;
        public bool instant,fullscreen;
        public CafeOptionsData Copy()=>new CafeOptionsData{version=version,charactersPerSecond=charactersPerSecond,instant=instant,fullscreen=fullscreen};
        public void Validate(){if(version!=1 || charactersPerSecond<10 || charactersPerSecond>80)throw new ArgumentException("設定ファイルの形式が不正です。");}
    }
    public sealed class CafeOptionsStore
    {
        public readonly string Path;public string Notice {get;private set;}public bool Recovered {get;private set;}
        bool unreadable;
        public CafeOptionsStore(string path){Path=path;}
        CafeOptionsData Read(string file){var d=JsonUtility.FromJson<CafeOptionsData>(File.ReadAllText(file,Encoding.UTF8));if(d==null)throw new ArgumentException();d.Validate();if(!File.ReadAllText(file).Contains("\"version\""))throw new ArgumentException();return d;}
        public CafeOptionsData Load()
        {
            Notice=null;Recovered=unreadable=false;if(!File.Exists(Path) && !File.Exists(Path+".bak"))return new CafeOptionsData();
            try{return Read(Path);}catch(Exception){try{var d=Read(Path+".bak");Recovered=true;Notice="バックアップの設定を読み込みました。保存すると修復します。";return d;}catch(Exception){unreadable=true;Notice="設定を読み込めません。初期設定で表示しています。元のファイルは保持しています。";return new CafeOptionsData();}}
        }
        public bool Save(CafeOptionsData d)
        {
            string temp=Path+".tmp";
            try
            {
                d.Validate();Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));using(var f=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){var b=Encoding.UTF8.GetBytes(JsonUtility.ToJson(d,true));f.Write(b,0,b.Length);f.Flush(true);}Read(temp);
                if(File.Exists(Path))Standalone.CafeSaveStore.ReplaceWithRetry(temp,Path,(s,t,b)=>File.Replace(s,t,Recovered||unreadable?Path+".invalid":b));else File.Move(temp,Path);
                Notice=null;Recovered=unreadable=false;return true;
            }
            catch(Exception){Notice="設定を保存できませんでした。変更は反映していません。保存先を確認して再試行してください。";return false;}
            finally{if(File.Exists(temp))try{File.Delete(temp);}catch(IOException){}}
        }
    }
    public static class CafeOptions
    {
        public static CafeOptionsData Current {get;private set;}=new CafeOptionsData();
        public static CafeOptionsStore Store {get;private set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Initialize()
        {
            string path=System.IO.Path.Combine(Application.persistentDataPath,"options-v1.json");var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-options-check");if(i<0)i=Array.IndexOf(args,"--catcafe-opening-check");if(i>=0 && i+1<args.Length)path=System.IO.Path.Combine(System.IO.Path.GetFullPath(args[i+1]),"profile/options-v1.json");
            Store=new CafeOptionsStore(path);Current=Store.Load();if(File.Exists(path) || Store.Recovered)ApplyScreen();
        }
        public static bool Save(CafeOptionsData draft){if(Store==null || !Store.Save(draft))return false;Current=draft.Copy();ApplyScreen();return true;}
        static void ApplyScreen(){if(!Application.isEditor)Screen.fullScreenMode=Current.fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed;}
        public static float Reveal(float previous,float seconds,int length)=>Current.instant?length:Math.Min(length,previous+seconds*Current.charactersPerSecond);
    }
}
