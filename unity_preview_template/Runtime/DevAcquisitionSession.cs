#if CATCAFE_DEV_ACQUISITION
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CatCafePreview.Standalone;

namespace CatCafePreview
{
    // Compiled only by the explicit development verification build entry.
    public sealed class DevAcquisitionSession : MonoBehaviour
    {
        CafeDevAcquisition run;
        string output;
        static string failure;
        bool autoStart;
        public static string Output(string[] args)
        {
            int i=Array.IndexOf(args,"--catcafe-dev-acquisition");if(i<0)return null;
            if(i+1>=args.Length || !Path.IsPathRooted(args[i+1]))throw new ArgumentException("Development acquisition requires an absolute run directory");
            if(args.Any(a=>a=="--catcafe-playtest" || a=="--catcafe-route" || a=="--catcafe-http" || a=="--catcafe-profile" || a.EndsWith("-check",StringComparison.Ordinal) || a=="-catCafeSmoke"))throw new ArgumentException("Development acquisition cannot share another QA/profile entry");
            string root=Path.GetFullPath(args[i+1]);CafeDevAcquisition.RejectLinks(root);
            string normal=Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(Within(root,normal) || Within(normal,root))throw new ArgumentException("Development output must be separate from the normal player profile");
            return root;
        }
        static bool Within(string a,string b)=>a.Equals(b,StringComparison.OrdinalIgnoreCase) || a.StartsWith(b+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();if(!args.Contains("--catcafe-dev-acquisition"))return;
            var go=new GameObject("開発専用・通常条件収集検証");DontDestroyOnLoad(go);var host=go.AddComponent<DevAcquisitionSession>();
            SceneManager.sceneLoaded+=host.HideNormalScene;
            try
            {
                string root=Output(args);host.output=root;int i=Array.IndexOf(args,"--dev-acquisition-plan");
                if(i<0 || i+1>=args.Length || !Path.IsPathRooted(args[i+1]))throw new ArgumentException("--dev-acquisition-plan requires an absolute JSON file");
                var plan=JsonUtility.FromJson<DevAcquisitionPlan>(File.ReadAllText(args[i+1]));
                var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
                rules.stamina_growth=JsonUtility.FromJson<CafeStaminaRules>(Resources.Load<TextAsset>("CatCafeStandalone/stamina_growth").text);
                // Hash the complete resolved catalog inputs, not obsolete Imports on disk.
                var content=new List<string>{Application.unityVersion,Application.version};
                foreach(string resource in new[]{"CatCafeStandalone/presentation","CatCafeContent/catalog","CatCafeContent/human_portraits"})
                {
                    var asset=Resources.Load<TextAsset>(resource);content.Add(resource+"\n"+(asset==null?"missing":asset.text));
                    if(resource=="CatCafeContent/catalog" && asset!=null)
                    {var catalog=JsonUtility.FromJson<ContentReference>(asset.text);foreach(string link in new[]{catalog.bundleResource,catalog.interactionResource})if(!string.IsNullOrEmpty(link)){var linked=Resources.Load<TextAsset>(link);content.Add(link+"\n"+(linked==null?"missing":linked.text));}}
                }
                host.run=new CafeDevAcquisition(root,rules,plan,string.Join("\n",content),o=>JsonUtility.ToJson(o),DecodeSave,text=>JsonUtility.FromJson<DevAcquisitionCheckpoint>(text),text=>JsonUtility.FromJson<DevAcquisitionResult>(text),args.Contains("--dev-acquisition-resume"));
                host.autoStart=args.Contains("--dev-acquisition-start");if(host.autoStart)host.run.Start();
                Application.runInBackground=true;
            }
            catch(Exception e){failure=e.ToString();Debug.LogError(failure);Application.Quit(4);}
        }
        [Serializable] sealed class ContentReference {public string bundleResource,interactionResource;}
        static CafeSaveEnvelope DecodeSave(string text)
        {
            var save=JsonUtility.FromJson<CafeSaveEnvelope>(text);save.state.NormalizeJsonStrings();CafeDoublePrecision.Restore(save.state,save.double_bits);return save;
        }
        void HideNormalScene(Scene scene,LoadSceneMode mode){foreach(var root in scene.GetRootGameObjects())root.SetActive(false);}
        void Update()
        {
            if(run==null || run.Finished)return;
            string pause=Path.Combine(output,"pause.request");
            if(run.Status=="running" && File.Exists(pause))
            {
                File.Delete(pause);run.Pause("external_pause");if(autoStart)Application.Quit(0);return;
            }
            for(int i=0;i<4 && run.Status=="running";i++)run.Step();
            if(run.Finished && autoStart)Application.Quit(run.Status=="completed"?0:run.Status=="error"?3:2);
        }
        void OnGUI()
        {
            GUI.Box(new Rect(10,10,720,220),"開発専用・通常条件収集検証（内部コマンド検証）");
            if(run==null){GUI.Label(new Rect(25,45,690,150),failure??"初期化中");return;}
            GUI.Label(new Rect(25,45,690,70),run.Objective+" / "+run.Status+" / day="+run.Simulation.State.day+" / operations="+run.Operations+"\n"+run.Reason);
            if(!run.Finished && GUI.Button(new Rect(25,120,190,40),run.Status=="running"?"一時停止して保存":"明示的に実行・再開"))
            {if(run.Status=="running")run.Pause();else run.Start();}
            if(GUI.Button(new Rect(235,120,190,40),"保存して終了")){if(run.Status=="running")run.Pause("user_exit");Application.Quit();}
            GUI.Label(new Rect(25,175,690,40),"画面読了・CG実表示・物理入力は確認対象外。通常のセーブと共有しません。");
        }
        void OnApplicationQuit(){if(run!=null){if(run.Status=="running")run.Pause("application_quit");run.Dispose();run=null;}}
        void OnDestroy(){SceneManager.sceneLoaded-=HideNormalScene;}
    }
}
#endif
