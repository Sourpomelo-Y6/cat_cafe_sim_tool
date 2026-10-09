using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonBusinessLogVerification
    {
        const string Key="CatCafeBusinessLogTest";static PythonBusinessLogVerification(){EditorApplication.update+=Update;}
        public static void Run(){int port=int.Parse(Environment.GetEnvironmentVariable("CAT_CAFE_LOG_TEST_PORT"));if(port<=0 || port==8190)throw new Exception("Use an isolated server");EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=port;serialized.ApplyModifiedPropertiesWithoutUndo();SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+120);EditorApplication.EnterPlaymode();}
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Log Play verification timed out at "+stage);if(!EditorApplication.isPlaying)return;var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();if(client==null || !client.CanManageSaves)return;var p=client.GetComponent<PythonBusinessLogPanel>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var view=(PythonBusinessLogView)typeof(PythonBusinessLogPanel).GetField("selected",flags).GetValue(p);
                if(stage==1){p.exportDirectory=Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/BusinessLogPlayTest"));client.GetComponent<PythonMenuPanel>().Open();client.GetComponent<PythonMenuPanel>().Select(13);SessionState.SetInt(Key,2);}
                else if(stage==2 && view!=null && p.export.interactable){int index=Array.FindIndex(view.rows,r=>r.details.Contains("反応："));if(index<0)throw new Exception("Live action details missing");p.operations.value=index;p.Select(index);Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{client.GetComponent<ContentPreview>(),"Logs/BusinessLogLive.png"});SessionState.SetInt(Key+"total",view.total);p.export.onClick.Invoke();SessionState.SetInt(Key,3);}
                else if(stage==3 && p.notice.text.StartsWith("出力しました：\n")){string file=p.notice.text.Substring("出力しました：\n".Length);if(!File.Exists(file) || !File.ReadAllText(file).Contains("cafe-human-cat"))throw new Exception("Log file missing");SessionState.SetString(Key+"file",file);if(p.path.text!=file)throw new Exception("Export path was not filled for replay");p.inspect.onClick.Invoke();SessionState.SetInt(Key,4);}
                else if(stage==4 && view!=null && view.source==SessionState.GetString(Key+"file","") && p.summary.interactable){if(view.total!=SessionState.GetInt(Key+"total",0) || !view.notes.Contains("リプレイ検証") || p.export.interactable)throw new Exception("Saved replay did not match");if(typeof(PythonStateClient).GetField("pending",flags).GetValue(client)!=null)throw new Exception("Replay modified business");string file=SessionState.GetString(Key+"file","");string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/BusinessLogPlayTest"));if(Path.GetDirectoryName(Path.GetFullPath(file))!=folder)throw new Exception("Unexpected test export path");File.Delete(file);p.Close();Debug.Log("CAT_CAFE_BUSINESS_LOG_PLAY_OK: live HTTP / latest actions / UI export / verified file replay / no game commands");SessionState.SetInt(Key,0);EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(0);}
            }
            catch(Exception error){SessionState.SetInt(Key,0);Debug.LogException(error);EditorApplication.Exit(1);}
        }
    }
}
