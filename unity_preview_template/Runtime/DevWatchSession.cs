#if CATCAFE_DEV_ACQUISITION
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    // A separate opt-in UI driver. It does not own or edit a second simulation.
    // All gameplay and dialogue progress comes from the existing screen handlers.
    public sealed partial class DevWatchSession : MonoBehaviour
    {
        [Serializable] public sealed class Plan {public int version=2,days=8;public bool collection;public string[] route_ids=new string[0];public string objective="popularity",identity;}
        [Serializable] public sealed class Checkpoint {public int version=2,commands,ui_actions,route_index;public bool route_finished;public string identity,save_sha256,dialogue,state_digest;}
        [Serializable] sealed class Status
        {
            public string layer="ui-handlers",status,action,dialogue,phase,error,state_digest,route_id,objective;public string[] assigned_targets;public int assigned_acquired;
            public int day,tick,commands,ui_actions;public float speed;public bool paused,short_dialogue,physical_input_verified=false;
            public string[] viewed_replay_keys,viewed_image_keys;
        }
        [Serializable] sealed class CommandRecord {public int sequence,day,tick;public string command,error,state_digest;}
        [Serializable] sealed class UIRecord {public int sequence;public string action,dialogue;}
        [Serializable] sealed class ContentReference {public string bundleResource,interactionResource;}
        Plan plan;Checkpoint checkpoint;LocalCafeBackend backend;FileStream ownership;
        string root,next="実行を押すと、タイトルから通常画面を操作します。",error="",pendingKey="",lastKey="";
        Action pending;float due,lastChange,speed=1;int commands,uiActions,preparedDay;
        bool paused=true,single,started,resume,loaded,saveRequested,saving,saved,quitAfterSave,completeAfterSave,complete,shortDialogue,exitOnComplete;
        Rect window=new Rect(-1,12,650,156);Font font;bool compact;Texture2D toolbarBackground;GUIStyle toolbarStyle,reasonStyle;
        PythonStateClient Client=>FindObjectOfType<PythonStateClient>();
        AcquisitionConversationPanel Dialog=>Client==null?null:Client.GetComponents<AcquisitionConversationPanel>().LastOrDefault(p=>p.Showing);
        public static string Output(string[] args)
        {
            int i=Array.IndexOf(args,"--catcafe-dev-watch");if(i<0)return null;
            if(i+1>=args.Length || !Path.IsPathRooted(args[i+1]))throw new ArgumentException("Watch mode needs an absolute output directory");
            if(args.Any(a=>a=="--catcafe-dev-acquisition" || a=="--catcafe-playtest" || a=="--catcafe-route" || a=="--catcafe-http" || a=="--catcafe-profile" || a=="-catCafeSmoke" || a.EndsWith("-check",StringComparison.Ordinal)))throw new ArgumentException("Watch mode cannot share another QA/profile entry");
            string output=Path.GetFullPath(args[i+1]);CafeDevAcquisition.RejectLinks(output);
            string normal=Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(output.Equals(normal,StringComparison.OrdinalIgnoreCase) || output.StartsWith(normal+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || normal.StartsWith(output.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Watch output must be separate from the normal profile");
            return output;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();if(!args.Contains("--catcafe-dev-watch"))return;
            var go=new GameObject("開発専用・観戦自動プレイ");DontDestroyOnLoad(go);var host=go.AddComponent<DevWatchSession>();
            try {host.Initialize(args);}catch(Exception e){host.error=e.ToString();Debug.LogError(e);Application.Quit(4);}
        }
        void Initialize(string[] args)
        {
            root=Output(args);resume=args.Contains("--dev-watch-resume");exitOnComplete=args.Contains("--dev-watch-exit-on-complete");shortDialogue=args.Contains("--dev-watch-short-dialogue");
            if(!resume && Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())throw new IOException("New watch run needs an empty directory");
            Directory.CreateDirectory(root);ownership=new FileStream(Path.Combine(root,"watch.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            var identity=new StringBuilder("watch-v2\n");identity.Append(CafeDevAcquisition.Hash(File.ReadAllBytes(typeof(DevWatchSession).Assembly.Location)));
            foreach(string name in new[]{"CatCafeStandalone/rules","CatCafeStandalone/stamina_growth","CatCafeStandalone/presentation","CatCafeContent/catalog","CatCafeContent/human_portraits"})
            {
                var asset=Resources.Load<TextAsset>(name);identity.Append(asset?.text??"missing");
                if(name=="CatCafeContent/catalog" && asset!=null){var reference=JsonUtility.FromJson<ContentReference>(asset.text);foreach(string link in new[]{reference.bundleResource,reference.interactionResource})if(!string.IsNullOrEmpty(link))identity.Append(Resources.Load<TextAsset>(link)?.text??"missing");}
            }
            string fingerprint=CafeDevAcquisition.Hash(Encoding.UTF8.GetBytes(identity.ToString()));
            if(resume)
            {
                plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Path.Combine(root,"watch-plan.json")));
                checkpoint=JsonUtility.FromJson<Checkpoint>(File.ReadAllText(Path.Combine(root,"watch-checkpoint.json")));
                if(plan==null || plan.version!=2 || plan.identity!=fingerprint || checkpoint==null || checkpoint.version!=2 || checkpoint.identity!=fingerprint)throw new IOException("Watch build, rules or checkpoint changed");

                commands=checkpoint.commands;uiActions=checkpoint.ui_actions;routeIndex=checkpoint.route_index;next="保存した会話と営業を読み込みます。実行を押してください。";
            }
            else
            {
                plan=new Plan{identity=fingerprint,collection=args.Contains("--dev-watch-collection")};int selected=Array.IndexOf(args,"--dev-watch-routes");if(selected>=0)plan.route_ids=args[selected+1].Split(',');int i=Array.IndexOf(args,"--dev-watch-days");if(i>=0)plan.days=int.Parse(args[i+1]);
                if(plan.days<4 || plan.days>8)throw new ArgumentException("This watch preview supports 4..8 days");
                Write("watch-plan.json",JsonUtility.ToJson(plan,true));
            }
            int rate=Array.IndexOf(args,"--dev-watch-speed");if(rate>=0)speed=float.Parse(args[rate+1],System.Globalization.CultureInfo.InvariantCulture);
            if(speed!=1 && speed!=2 && speed!=4 && speed!=16 && speed!=64)throw new ArgumentException("Watch speed must be 1, 2, 4, or verification-only 16 / 64");
            PrepareRoutes();if(resume && CafeDevAcquisition.Hash(File.ReadAllBytes(SaveFile))!=checkpoint.save_sha256)throw new IOException("Watch save hash mismatch");
            backend=new LocalCafeBackend(SaveDirectory,playOpening:true);backend.CommandObserved+=Observe;CafeBackends.UseLocal(backend);
            paused=!args.Contains("--dev-watch-start");lastChange=Time.realtimeSinceStartup;Application.runInBackground=true;Publish();
        }
        string SaveDirectory=>plan.collection?Path.Combine(root,"routes",CurrentRoute.id,"profile","standalone-v1"):Path.Combine(root,"profile","standalone-v1");
        string SaveFile=>Path.Combine(SaveDirectory,"slot-9.json");
        string Key()=>backend==null?"":backend.Simulation.State.instance_id+"/"+backend.Simulation.State.revision+"/"+(Dialog?.PlaybackKey??"")+"/"+(FindObjectOfType<PythonTitleScreen>()?.ConfirmingStart.ToString()??"");
        void Write(string name,string text)
        {
            string path=Path.Combine(root,name),tmp=path+".tmp";File.WriteAllText(tmp,text);
            if(File.Exists(path))CafeSaveStore.ReplaceWithRetry(tmp,path);else File.Move(tmp,path);
        }
        void Publish()
        {
            if(backend==null)return;var s=backend.Simulation.State;
            Write("watch-status.json",JsonUtility.ToJson(new Status{route_id=CurrentRoute?.id,objective=CurrentRoute?.objective,assigned_targets=CurrentRoute?.targets,assigned_acquired=AssignedAcquired(),status=error!=""?"error":complete?"completed":paused?"paused":"running",action=next,error=error,dialogue=Dialog?.PlaybackKey??"",phase=s.phase,day=s.day,tick=s.tick,commands=commands,ui_actions=uiActions,speed=speed,paused=paused,short_dialogue=shortDialogue,state_digest=CafeReplayJournal.Digest(s),viewed_replay_keys=backend.Replays.Load().entries.Select(e=>e.Key).ToArray(),viewed_image_keys=backend.Gallery.Load().viewed},true));
        }
        void Observe(string json,CafeResponse response)
        {
            commands++;var cmd=JsonUtility.FromJson<PythonShiftCommand>(json);var s=backend.Simulation.State;
            File.AppendAllText(Path.Combine(root,"watch-commands.jsonl"),JsonUtility.ToJson(new CommandRecord{sequence=commands,day=s.day,tick=s.tick,command=json,error=response.error,state_digest=CafeReplayJournal.Digest(s)})+"\n");
            if(response.result!=UnityWebRequest.Result.Success){Fail(cmd.kind+": "+response.error);return;}
            if(cmd.kind=="save_game")
            {
                checkpoint=new Checkpoint{route_index=routeIndex,route_finished=completeAfterSave,ui_actions=uiActions,identity=plan.identity,commands=commands,save_sha256=CafeDevAcquisition.Hash(File.ReadAllBytes(SaveFile)),dialogue=Dialog?.PlaybackKey??"",state_digest=CafeReplayJournal.Digest(s)};
                Write("watch-checkpoint.json",JsonUtility.ToJson(checkpoint,true));saved=true;
            }
            if(cmd.kind=="load_game")
            {
                if(CafeReplayJournal.Digest(s)!=checkpoint.state_digest){Fail("保存と再開の状態が一致しません。");return;}
                loaded=true;
            }
            Publish();
        }
        void Fail(string message){error=message;paused=true;pending=null;next="停止しました。記録を確認してください。";Publish();if(exitOnComplete)Application.Quit(3);}
        void Schedule(string message,Action action,bool dialogue=false)
        {
            next=message;pending=action;pendingKey=Key();due=Time.realtimeSinceStartup+(dialogue?(shortDialogue?.25f:1.6f):.8f)/speed;Publish();
        }
        void Update()
        {
            if(backend==null || error!="" || complete)return;
            try
            {
                if(File.Exists(Path.Combine(root,"pause.request"))){File.Delete(Path.Combine(root,"pause.request"));RequestSave(false);}
                if(saving){if(saved){saving=false;if(completeAfterSave && plan.collection){FinishCollectionRoute();return;}complete=completeAfterSave;next=complete?"観戦ルートを完走し、検証専用スロットへ保存しました。":"保存しました。実行で続行できます。";Publish();if(quitAfterSave || complete && exitOnComplete)Application.Quit(0);}return;}
                if(saveRequested){var c=Client;if(c!=null && c.CanSaveNow){saveRequested=false;saving=true;saved=false;next="通常の保存処理で会話位置と営業を保存しています。";c.SaveToSlot(9);}return;}
                if(resume && !loaded && !started)
                {
                    var title=FindObjectOfType<PythonTitleScreen>();if(title!=null && title.CanManageSaves){started=true;title.ShowSaves();title.ResumeSaved("9");next="保存した営業と会話を読み込みました。実行で続行できます。";}return;
                }
                if(GameOptionsPanel.IsOpen){paused=true;pending=null;return;}
                if(paused && !single)
                {
                    string idleKey=Key();if(lastKey!=idleKey){lastKey=idleKey;Publish();}
                    lastChange=Time.realtimeSinceStartup;return;
                }
                string key=Key();if(lastKey!=key){lastKey=key;lastChange=Time.realtimeSinceStartup;}
                if(Time.realtimeSinceStartup-lastChange>120 || commands>(plan.collection?100000:10000)){Fail("進行待ちの上限に達しました。" );return;}
                if(pending!=null && key!=pendingKey)pending=null;
                if(pending==null){if(nextRoutePending)Schedule("このルートは完了です。次は「"+watchRoutes[routeIndex+1].id+"」を別のセーブで始めます。",AdvanceWatchRoute,true);else Choose();return;}
                if(!single && Time.realtimeSinceStartup<due)return;
                var operation=pending;pending=null;single=false;uiActions++;
                File.AppendAllText(Path.Combine(root,"watch-ui-actions.jsonl"),JsonUtility.ToJson(new UIRecord{sequence=uiActions,action=next,dialogue=Dialog?.PlaybackKey??""})+"\n");
                operation();Publish();
            }
            catch(Exception e){Fail(e.ToString());}
        }
        static Button Button(GameObject host,params string[] captions)
        {foreach(string text in captions){var b=host.GetComponentsInChildren<Button>().FirstOrDefault(x=>x.gameObject.activeInHierarchy && x.IsInteractable() && x.GetComponentInChildren<Text>()?.text==text);if(b!=null)return b;}return null;}
        void Click(Button b,string reason){if(b!=null)Schedule(reason,()=>{if(b!=null && b.gameObject.activeInHierarchy && b.IsInteractable())b.onClick.Invoke();});}
        void Choose()
        {
            var title=FindObjectOfType<PythonTitleScreen>();
            if(title!=null)
            {
                if(!title.CanManageSaves)return;
                if(resume){if(!started)Schedule("検証用スロット9を読み込み、会話の続きへ進みます。",()=>{started=true;title.ShowSaves();title.ResumeSaved("9");});return;}
                if(!title.conditionsPanel.activeSelf){Schedule("はじめから：選んだ目標の開始条件を確認します。",()=>{title.ShowConditions();int i=Array.FindIndex(title.CurrentState.new_game_options,o=>o.choice==(CurrentRoute?.objective??plan.objective));title.objectives.value=i;title.SelectObjective(i);});return;}
                Click(title.startGame,title.ConfirmingStart?"この開始内容でカフェを開きます。":"目標・初期資金・猫を確認し、開始の確認へ進みます。");return;
            }
            var c=Client;if(c==null || c.CurrentState==null)return;
            if(!started)started=true;
            if(resume && !loaded)return;
            if(c.Automatic){Schedule("観戦の速度で1操作ずつ進めるため、自動進行を止めます。",c.PauseAutomaticForSave);return;}
            if(c.AutoPlayRunning){Schedule("通常のおまかせを止め、観戦操作へ戻します。",c.StopAutoPlay);return;}
            var reservation=c.GetComponent<PythonReservationPanel>();if(reservation.Confirming){Click(reservation.confirm,"予約の回答内容を確認して確定します。");return;}
            var d=Dialog;
            if(d!=null)
            {
                var confirm=Button(d.PlaybackHost,"この内容で回答");if(confirm!=null){Click(confirm,"回答内容を確認し、確定します。");return;}
                if(d.PlaybackChoosing)
                {
                    string[] captions=plan.collection?CollectionAnswers(d):d is EquipmentConversationPanel?new[]{"見送り","修理","応急処置"}:d is AdoptionConversationPanel?new[]{"今回は見送る"}:d is ReservationConversationPanel?new[]{"引き受ける（確認へ）","辞退する（確認へ）"}:d is TrustConversationPanel?new[]{"回復を試みる"}:new[]{"迎える","見送る"};
                    var choice=Button(d.PlaybackHost,captions);if(choice==null){Fail("未対応、または選べる回答がない会話："+d.PlaybackKey);return;}
                    Click(choice,"相談に回答します：「"+choice.GetComponentInChildren<Text>().text+"」。選べる通常条件を確認済みです。");return;
                }
                if(!shortDialogue && !d.PlaybackTextReady)return;
                Schedule("会話を読み進めます。猫や相手の話を画面で確認します。",d.Next,true);return;
            }
            if(!c.CanSendCommands)return;
            var state=c.CurrentState;var s=backend.Simulation.State;
            if(state.game_over!=null){Fail("経営終了："+s.game_over);return;}
            if(plan.collection && CollectionPendingResult())return;
            var growth=c.GetComponent<PythonGrowthPanel>();
            if(growth.panel.activeInHierarchy){
                if(plan.collection && CurrentRoute.objective=="patron"){
                    int row=Array.FindIndex(state.growth_choices,g=>g.cat_id=="cat-sora" && g.choices.Any(o=>o.choice=="service") && g.selected!="service");
                    if(row>=0){if(c.GrowthRowIndex!=row){Schedule("派遣条件に必要なソラの成長を確認します。",()=>{c.GrowthRowIndex=row;growth.Apply(state);});return;}
                        Click(growth.choices[Array.FindIndex(state.growth_choices[row].choices,o=>o.choice=="service")],"ソラの接客専門を選びます。");return;}
                }
                Click(Button(growth.panel,"一覧をまとめて確定","猫に合う内容で自動決定"),"猫の適性に合う成長案を確認し、確定します。");return;}
            if(c.CanContinueGoal){Schedule("目標の結果を確認して営業を続けます。",c.ContinueGoal);return;}
            if(c.CanAdvanceGoal){Schedule("次の人気目標へ進みます。",c.AdvanceGoal);return;}
            if(!string.IsNullOrEmpty(state.required_action))
            {
                int route=state.required_action=="growth"?3:state.required_action=="store_event"?2:state.required_action=="intake_request"?1:state.required_action=="regular_introduction"?7:state.required_action=="reservation"?8:state.required_action=="customer_trust"?9:state.required_action=="missing_return"?4:state.required_action=="adoption"?5:-1;
                if(plan.collection && CollectionRequired(state.required_action))return;
                if(route<0){Fail("未対応の確認待ち："+state.required_action);return;}
                Schedule("確認待ちの出来事を開き、会話で内容を確かめます。",()=>c.GetComponent<PythonEventListPanel>().ShowMenu(route));return;
            }
            var eventList=c.GetComponent<PythonEventListPanel>();
            if(eventList.panel.activeInHierarchy && eventList.PendingCount==0){Schedule("出来事の確認を終え、営業画面へ戻ります。",eventList.ReturnToBusiness);return;}
            if(plan.collection){ChooseCollectionOperation();return;}
            if(s.day>plan.days){completeAfterSave=true;RequestSave(false);return;}
            var prep=c.GetComponent<PythonPreparationPanel>();
            if(s.phase=="preparation")
            {
                if(!prep.panel.activeInHierarchy){Schedule("今日の営業準備を開きます。",prep.Open);return;}
                if(prep.HasProposal){Click(prep.confirmProposal,"出勤・休養の案を確認して確定します。");return;}
                if(preparedDay!=s.day){Schedule("疲れた猫を休ませる出勤案を作ります。",()=>{preparedDay=s.day;prep.ProposeRest();});return;}
                if(c.CanProgressBusiness){Click(prep.start,"出勤を確認しました。今日の営業を始めます。");return;}
                if(c.CanDayOff){Schedule("今日は猫を休ませ、休業します。",c.DayOff);return;}
            }
            if(c.CanProgressBusiness)
            {
                if(s.phase=="closed")
                {
                    var closing=c.GetComponent<PythonClosingPanel>();if(!closing.panel.activeInHierarchy){Schedule("閉店結果を開いて、売上と猫の状態を確認します。",closing.Open);return;}
                    Click(closing.next,"売上と猫の状態を確認し、翌日へ進みます。");return;
                }
                Click(c.GetComponent<ContentPreview>().GetSceneButton(2),"接客を1刻進めます。猫とお客さんの様子を見守ります。");return;
            }
            var events=c.GetComponent<PythonEventListPanel>();if(events.PendingCount>0){Schedule("まだ読んでいない結果会話を開きます。",()=>{events.Open();var entry=events.entries.FirstOrDefault(b=>b.gameObject.activeInHierarchy && b.IsInteractable());if(entry!=null)entry.onClick.Invoke();});return;}
        }
        void RequestSave(bool quit){if(Client==null)return;paused=true;single=false;pending=null;quitAfterSave=quit;saveRequested=true;next="進行を止め、検証専用セーブへ保存します。";Publish();}
        void OnGUI()
        {
            if(font==null)font=FindObjectsOfType<Text>().Select(t=>t.font).FirstOrDefault(f=>f!=null);if(font!=null)GUI.skin.font=font;
            if(toolbarStyle==null)
            {
                toolbarBackground=new Texture2D(1,1,TextureFormat.RGBA32,false);toolbarBackground.SetPixel(0,0,new Color(.14f,.19f,.16f,1));toolbarBackground.Apply();
                toolbarStyle=new GUIStyle(GUI.skin.window);foreach(var state in new[]{toolbarStyle.normal,toolbarStyle.onNormal,toolbarStyle.active,toolbarStyle.onActive,toolbarStyle.focused,toolbarStyle.onFocused}){state.background=toolbarBackground;state.textColor=Color.white;}
                reasonStyle=new GUIStyle(GUI.skin.label){wordWrap=true,fontSize=14};reasonStyle.normal.textColor=Color.white;
            }
            if(window.x<0)window.x=Math.Max(0,Screen.width-window.width-12);window.x=Mathf.Clamp(window.x,0,Math.Max(0,Screen.width-window.width));window.y=Mathf.Clamp(window.y,0,Math.Max(0,Screen.height-window.height));window.height=compact?78:156;
            window=GUI.Window(GetInstanceID(),window,id=>
            {
                GUI.Label(new Rect(12,23,550,42),next,reasonStyle);
                if(GUI.Button(new Rect(575,25,63,28),compact?"開く":"たたむ"))compact=!compact;
                if(compact){GUI.DragWindow(new Rect(0,0,650,20));return;}
                bool locked=backend==null || error!="" || complete || saving || saveRequested;GUI.enabled=!locked;
                if(GUI.Button(new Rect(12,70,100,30),paused?"実行・再開":"一時停止")){paused=!paused;single=false;pending=null;lastChange=Time.realtimeSinceStartup;Publish();}
                if(GUI.Button(new Rect(120,70,85,30),"1操作")){paused=true;single=true;pending=null;}
                if(GUI.Button(new Rect(213,70,66,30),"1倍")){speed=1;pending=null;Publish();}
                if(GUI.Button(new Rect(287,70,66,30),"2倍")){speed=2;pending=null;Publish();}
                if(GUI.Button(new Rect(361,70,66,30),"4倍")){speed=4;pending=null;Publish();}
                if(GUI.Button(new Rect(435,70,195,30),"会話短縮："+(shortDialogue?"入":"切"))){shortDialogue=!shortDialogue;pending=null;Publish();}
                GUI.enabled=backend!=null && Client!=null && !saving && !saveRequested;
                if(GUI.Button(new Rect(12,108,140,30),complete?"終了":"保存して終了")){if(complete)Application.Quit(0);else RequestSave(true);}
                GUI.enabled=true;var s=backend?.Simulation.State;GUI.Label(new Rect(165,109,465,30),(complete?"完走":paused?"停止中":"観戦中")+" / "+s?.day+"日目 "+s?.tick+"刻 / 操作 "+commands+" / "+speed+"倍");
                GUI.DragWindow(new Rect(0,0,650,20));
            },"開発専用・観戦自動プレイ "+(plan?.collection==true?CurrentRoute?.id:"")+"（ドラッグで移動）",toolbarStyle);
        }
        void OnApplicationQuit(){ownership?.Dispose();ownership=null;}
    }
}
#endif
