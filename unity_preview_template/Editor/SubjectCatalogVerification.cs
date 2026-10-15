using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor
{
    public static class SubjectCatalogVerification
    {
        [MenuItem("Cat Cafe/素材/使用中の交流文と対象を確認",false,2)]
        public static void ShowActive()
        {
            try
            {
                var c=CafeNarrativeResources.Load();
                string text=c==null?"交流文パック未設定。試作用の定型文を使います。":
                    "パック："+c.pack_id+"\n版："+c.version+" / "+c.revision+"\n\n猫：\n"+
                    string.Join("\n",c.cat_bindings.Select(r=>r.cat_id+" ← "+(r.subject_id??r.cat_id)+" / 画像 "+r.content_cat_id))+
                    "\n\n人物：\n"+string.Join("\n",c.people.Select(r=>(r.game_id??r.id)+" ← "+r.id+" / "+r.name))+
                    "\n\n登録条件："+c.entries.Length+" / 候補："+c.entries.Sum(e=>e.variants.Length)+
                    "\n専用操作→操作共通文→対象名の定型文の順で補います。ミケ専用文は他の猫へ流用しません。";
                Debug.Log(text);EditorUtility.DisplayDialog("使用中の交流文",text,"閉じる");
            }
            catch(Exception ex){Debug.LogException(ex);EditorUtility.DisplayDialog("交流文の読込エラー",ex.Message,"閉じる");}
        }

        // Isolated fixture import; the user's active authored pack is restored even on failure.
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("CATCAFE_NARRATIVE_OUTPUT");Directory.CreateDirectory(output);
            string pointer=Path.Combine(Application.dataPath,"Resources/CatCafeContent/catalog.json");byte[] original=File.ReadAllBytes(pointer);
            try
            {
                ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_IMPORT_PACK"));
                var catalog=CafeNarrativeResources.Load();if(catalog.version!=2)throw new Exception("Expected v2");
                var probeRules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);probeRules.store_events.probability=0;
                var b=new LocalCafeBackend(Path.Combine(output,"saves"),probeRules);var sim=b.Simulation;
                sim.Execute("player_begin",catId:"cat-tama");for(int i=0;i<4;i++)sim.Execute("player_step",choice:"pause");
                var rows=b.Narratives.Rows.ToArray();
                if(rows.Take(3).Select(r=>r.speech_id).Distinct().Count()!=3 || rows[0].speech_id!=rows[3].speech_id || rows.Any(r=>r.text.Contains("ミケ") || r.text.Contains("{cat")))throw new Exception("Tama rotation/name mismatch");
                string text=b.Narratives.Read(sim.ActivePlayer.id);Command(b,"save_game");
                b=new LocalCafeBackend(Path.Combine(output,"saves"),probeRules);Command(b,"load_game");sim=b.Simulation;
                if(b.Narratives.Read(sim.ActivePlayer.id)!=text)throw new Exception("Tama save/load mismatch");
                EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
                var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;var client=preview.GetComponent<PythonStateClient>();
                typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);CafeBackends.UseLocal(b);
                Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{b.View()});apply();
                var player=preview.GetComponent<PythonPlayerPanel>();player.Open();
                if(!player.history.text.Contains("タマ"))throw new Exception("Tama player prose missing");
                Capture(preview,Path.Combine(output,"player-tama.png"),1280,800);player.Close();sim.Execute("player_finish");
                foreach(var pattern in sim.Rules.patterns)pattern.days=new[]{0,1,2,3,4,5,6};
                sim.Rules.arrival_ticks=new[]{0,25};
                sim.Execute("set_shifts",working:new[]{"cat-tama"});sim.Execute("start_business");
                while(!sim.State.exchanges.Any(e=>e.customer_id=="guest-2") && sim.State.phase=="open")sim.Execute("advance_business");
                sim.Execute("advance_business");apply();var view=b.View();var seat=view.seats.First(s=>s.customer_id=="guest-2");
                var cat=view.cats.Single(c=>c.cat_id=="cat-tama");var panel=preview.gameObject.AddComponent<PythonSeatNarrativePanel>();
                string before=JsonUtility.ToJson(sim.State);panel.Open(seat,Array.IndexOf(view.seats,seat),cat);
                if(!panel.body.text.Contains("タマちゃん"))throw new Exception("Suzuki/Tama seat prose missing");
                foreach(var size in new[]{new Vector2Int(1024,768),new Vector2Int(1280,800)})Capture(preview,Path.Combine(output,"seat-suzuki-"+size.x+".png"),size.x,size.y);
                if(before!=JsonUtility.ToJson(sim.State))throw new Exception("Screen consumed resources");
                File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: v2 import/load, explicit Tama/Suzuki mapping, 3 variants rotate, expanded names, save/load, both screens and read-only state.\n"+text);
                Debug.Log("CAT_CAFE_SUBJECT_CATALOG_OK");
            }
            finally{File.WriteAllBytes(pointer,original);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);}
        }
        private static void Command(LocalCafeBackend b,string kind)
        {
            CafeResponse reply=null;var cmd=new PythonShiftCommand{kind=kind,save_id="2",save_slot=2,instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,request_id=Guid.NewGuid().ToString("N")};
            var r=b.Request("/commands",JsonUtility.ToJson(cmd),response=>reply=response);while(r.MoveNext()){}
            if(reply==null || reply.result!=UnityEngine.Networking.UnityWebRequest.Result.Success)throw new Exception(reply?.text??"No response");
        }
        private static void Capture(ContentPreview preview,string path,int width,int height)=>typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{preview,path,width,height});
    }
}
