using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private void ResetJournal()=>Journal=new CafeReplayJournal(Simulation.Rules,Simulation.State,EncodeSave,DecodeSave);
        private static string Optional(string value)=>string.IsNullOrEmpty(value)?null:value;
        private static CafeReplayOperation ReplayOperation(PythonShiftCommand cmd)
        {
            var op=new CafeReplayOperation {kind=cmd.kind,working=cmd.kind=="set_shifts"?cmd.working_cats:null,cat_id=Optional(cmd.cat_id),choice=Optional(cmd.choice),source=Optional(cmd.choice),seat_id=Optional(cmd.seat_id),customer_id=Optional(cmd.customer_id)};
            switch(cmd.kind)
            {
                case "start_autoplay":op.objective=cmd.autoplay_objective;op.mode=cmd.autoplay_mode;op.days=cmd.autoplay_days;op.choice=op.source=null;break;
                case "step_autoplay":case "stop_autoplay":case "pause_autoplay":case "resume_autoplay":case "save_game":op.choice=op.source=null;break;
                case "player_step":op.source=Optional(cmd.target_type);break;
                case "receive_patron":case "receive_general":op.kind="resolve_activity";op.source=cmd.choice;op.choice="receive";break;
                case "resolve_dispatch_introduction":case "resolve_dispatch_trouble":case "resolve_dispatch_choice":case "resolve_activity":case "resolve_customer_trust":case "resolve_adoption":op.source=Optional(cmd.event_id);break;
                case "set_auto_assignment":case "configure_adoption":op.source=null;break;
                case "purchase_waiting_area":case "upgrade_waiting_area":case "purchase_rest_space":case "upgrade_rest_space":case "soundproof_rest_space":case "open_recruitment":case "recruit_cat":case "purchase_cat":op.choice=op.source=null;break;
            }
            return op;
        }
        private PythonBusinessLogView BusinessLog(string route)
        {
            bool export=route=="/business-log/export";int offset=0;string path="";
            if(!export && route!="/business-log")
            {
                if(!route.StartsWith("/business-log?",StringComparison.Ordinal))throw new ArgumentException("ログの要求が不正です。");
                var keys=new HashSet<string>();foreach(string pair in route.Substring("/business-log?".Length).Split('&'))
                {int separator=pair.IndexOf('=');if(separator<0)throw new ArgumentException("ログの指定が不正です。");string key=pair.Substring(0,separator),value=Uri.UnescapeDataString(pair.Substring(separator+1).Replace("+"," "));if(!keys.Add(key))throw new ArgumentException("ログの指定が重複しています。");if(key=="offset"){if(!int.TryParse(value,out offset) || offset<0)throw new ArgumentException("表示位置が不正です。");}else if(key=="path")path=value;else throw new ArgumentException("ログの指定が不正です。");}
            }
            var resultState=Simulation.State;var data=Journal.Document;string source="現在の営業";
            if(path!="")
            {
                if(!Path.IsPathRooted(path) || !string.Equals(Path.GetExtension(path),".json",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("営業ログJSONの絶対パスを指定してください。");
                var file=new FileInfo(path);if(file.Length>32*1024*1024)throw new ArgumentException("営業ログは32MB以下にしてください。");data=JsonUtility.FromJson<CafeReplayDocument>(File.ReadAllText(path));
                if(data?.operations!=null)foreach(var row in data.operations)if(row?.automatic_operation!=null && string.IsNullOrEmpty(row.automatic_operation.kind))row.automatic_operation=null;
                resultState=Journal.Verify(data).State;source=path;
            }
            string exported=export?JsonUtility.ToJson(data,true):null;if(export && (data.operations.Count>50000 || System.Text.Encoding.UTF8.GetByteCount(exported)>32*1024*1024))throw new ArgumentException("営業ログが出力上限を超えています。保存・ロードで新しい記録区間を開始してください。");
            int total=data.operations.Count;offset=total==0?0:Math.Min(offset,(total-1)/20*20);
            return new PythonBusinessLogView {instance_id=Simulation.State.instance_id,revision=Simulation.State.revision,source=source,offset=offset,total=total,rows=data.operations.Skip(offset).Take(20).Select((r,i)=>new PythonBusinessLogRow {index=offset+i,label=(offset+i+1)+" / "+CafeReplayJournal.Label(r.operation.kind),details=r.details}).ToArray(),summary="記録元："+source+"\nログ形式：Unity "+data.format_version+" / 操作数："+total+"\n結果："+resultState.day+"日目 / "+resultState.tick+"tick / "+resultState.phase+" / 資金 "+resultState.funds+" / 人気 "+resultState.popularity+" / 完了接客 "+resultState.completed+"\n最終状態："+data.final_digest,notes=path==""?"営業セーブとは別に出力します。新規開始・ロード以前の操作は補いません。":"独立した状態でリプレイ検証済みです。現在の営業と保存ファイルは変更していません。",log_json=exported};
        }
    }
}
