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
    public static class AllModeNarrativeVerification
    {
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("CATCAFE_NARRATIVE_OUTPUT");Directory.CreateDirectory(output);
            string pointer=Path.Combine(Application.dataPath,"Resources/CatCafeContent/catalog.json");byte[] original=File.ReadAllBytes(pointer);
            try
            {
                ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_IMPORT_PACK"));
                var catalog=CafeNarrativeResources.Load();if(catalog.version!=3)throw new Exception("Expected all-mode v3 catalog");
                int checkedActions=0;
                foreach(var type in Rules().types)foreach(string action in type.actions)
                {
                    var probe=Backend(output);var sim=probe.Simulation;
                    sim.Execute("player_begin",catId:"cat-tama");if(type.id!="teaser")sim.Execute("player_step",choice:"switch",source:type.id);
                    sim.Execute("player_step",choice:action);var row=probe.Narratives.Rows.Last();
                    if(row.mode!=type.id || string.IsNullOrEmpty(row.speech_id) || string.IsNullOrEmpty(row.cat_text_id) || string.IsNullOrEmpty(row.image_resource) || row.text.Contains("{cat") || row.text.Contains("ミケ"))throw new Exception("Unity action mismatch: "+type.id+"/"+action);
                    checkedActions++;
                }
                EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
                var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;var client=preview.GetComponent<PythonStateClient>();
                typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
                var player=preview.GetComponent<PythonPlayerPanel>();
                foreach(string mode in new[]{"voice","presence","brush"})
                {
                    var b=Backend(output);var sim=b.Simulation;sim.Execute("player_begin",catId:"cat-tama");sim.Execute("player_step",choice:"switch",source:mode);
                    sim.Execute("player_step",choice:"direct");string session=sim.ActivePlayer.id,text=b.Narratives.Read(session);
                    Command(b,"save_game");var reopened=Backend(output);Command(reopened,"load_game");
                    if(reopened.Narratives.Read(session)!=text)throw new Exception("Unity disk save/load changed "+mode);
                    string state=JsonUtility.ToJson(reopened.Simulation.State);
                    CafeBackends.UseLocal(reopened);typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{reopened.View()});player.Open();
                    if(!player.history.text.Contains("タマ") || player.history.text.Contains("{cat"))throw new Exception("Player UI missing expanded prose");
                    Capture(preview,Path.Combine(output,"player-"+mode+".png"),1280,800);player.Close();
                    for(int i=0;i<10;i++)if(reopened.Narratives.Read(session)!=text)throw new Exception("Repeated view changed prose");
                    if(JsonUtility.ToJson(reopened.Simulation.State)!=state)throw new Exception("Reading consumed gameplay");
                }
                var service=Backend(output);var s=service.Simulation;s.Execute("set_shifts",working:new[]{"cat-tama"});s.Execute("start_business");
                while(service.Narratives.Rows.Count==0)s.Execute("advance_business");
                var exchange=s.State.exchanges.Single();exchange.mode="presence";exchange.previous_reaction="neutral";exchange.last_group=null;exchange.streak=0;
                s.Execute("advance_business");var last=service.Narratives.Rows.Last();
                if(last.mode!="presence" || string.IsNullOrEmpty(last.cat_text_id))throw new Exception("Service did not use presence text");
                CafeBackends.UseLocal(service);var view=service.View();typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{view});
                var seat=view.seats.First(r=>r.cat_id=="cat-tama");var cat=view.cats.Single(r=>r.cat_id=="cat-tama");
                var panel=preview.gameObject.AddComponent<PythonSeatNarrativePanel>();string before=JsonUtility.ToJson(s.State);
                panel.Open(seat,Array.IndexOf(view.seats,seat),cat);
                if(!panel.body.text.Contains("見守った") || !panel.body.text.Contains("タマ") || panel.body.text.Contains("{cat"))throw new Exception("Presence service UI mismatch");
                foreach(var size in new[]{new Vector2Int(1024,768),new Vector2Int(1280,800)})Capture(preview,Path.Combine(output,"seat-presence-"+size.x+".png"),size.x,size.y);
                if(before!=JsonUtility.ToJson(s.State))throw new Exception("Seat view consumed gameplay");
                File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: Unity v3 import/Resources, "+checkedActions+" actual operations across 8 modes; voice/presence/brush player screens, actual saved prose/load and read-only checks; presence service at two resolutions.");
                Debug.Log("CAT_CAFE_ALL_MODE_NARRATIVES_OK");
            }
            finally{File.WriteAllBytes(pointer,original);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);}
        }
        public static void VerifyActive()
        {
            string output=Environment.GetEnvironmentVariable("CATCAFE_NARRATIVE_OUTPUT");Directory.CreateDirectory(output);
            var catalog=CafeNarrativeResources.Load();if(catalog==null)throw new Exception("Active catalog missing");
            var b=new LocalCafeBackend(Path.Combine(output,"active-saves"));
            if(b.Narratives.Catalog?.pack_id!=catalog.pack_id)throw new Exception("Normal backend used fallback instead of active catalog");
            b.Simulation.Execute("player_begin",catId:"cat-mike");b.Simulation.Execute("player_step",choice:"direct");
            var row=b.Narratives.Rows.Last();if(row.catalog_revision!=catalog.revision || string.IsNullOrEmpty(row.cat_text_id))throw new Exception("Active authored narrative missing");
            File.WriteAllText(Path.Combine(output,"active-result.txt"),"PASS: normal Resources and LocalCafeBackend load active authored pack "+catalog.pack_id+" v"+catalog.version+"; real action selected authored candidate "+row.cat_text_id);
            Debug.Log("CAT_CAFE_ALL_MODE_ACTIVE_OK");
        }
        private static CafeRules Rules(){var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);rules.store_events.probability=0;return rules;}
        private static LocalCafeBackend Backend(string output)=>new LocalCafeBackend(Path.Combine(output,"saves"),Rules());
        private static void Command(LocalCafeBackend b,string kind)
        {
            CafeResponse reply=null;var cmd=new PythonShiftCommand{kind=kind,save_id="2",save_slot=2,instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,request_id=Guid.NewGuid().ToString("N")};
            var routine=b.Request("/commands",JsonUtility.ToJson(cmd),response=>reply=response);while(routine.MoveNext()){}
            if(reply==null || reply.result!=UnityEngine.Networking.UnityWebRequest.Result.Success)throw new Exception(reply?.text??"No response");
        }
        private static void Capture(ContentPreview preview,string path,int width,int height)=>typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{preview,path,width,height});
    }
}
