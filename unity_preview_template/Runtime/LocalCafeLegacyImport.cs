using System;
using System.IO;
using System.Linq;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    [Serializable] public class PythonLegacyImportView
    {
        public string instance_id,path,summary,error;public int revision;public int[] free_slots;public string[] notes;
    }
    public sealed partial class LocalCafeBackend
    {
        private readonly string saveDirectory;
        private CafeLegacyBundle ReadLegacyBundle(string path)
        {
            if(string.IsNullOrEmpty(path) || !Path.IsPathRooted(path) || !string.Equals(Path.GetExtension(path),".json",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("変換済み取込JSONの絶対パスを指定してください。");
            if(new FileInfo(path).Length>32*1024*1024)throw new ArgumentException("取込ファイルは32MB以下にしてください。");
            var value=JsonUtility.FromJson<CafeLegacyBundle>(File.ReadAllText(path));value?.state?.NormalizeJsonStrings();if(value?.state!=null)CafeDoublePrecision.Restore(value.state,value.double_bits);CafeLegacyImport.ValidateBundle(value,Simulation.Rules);return value;
        }
        private bool SlotExists(int slot)=>File.Exists(Path.Combine(saveDirectory,"slot-"+slot+".json")) || File.Exists(Path.Combine(saveDirectory,"slot-"+slot+".json.bak")) || File.Exists(Path.Combine(saveDirectory,"slot-"+slot+".json.tmp"));
        private PythonLegacyImportView LegacyPreview(string route)
        {
            const string prefix="/legacy-import?path=";if(!route.StartsWith(prefix,StringComparison.Ordinal))throw new ArgumentException("変換済みファイルを指定してください。");string path=Uri.UnescapeDataString(route.Substring(prefix.Length));var bundle=ReadLegacyBundle(path);var s=bundle.state;
            return new PythonLegacyImportView {instance_id=Simulation.State.instance_id,revision=Simulation.State.revision,path=path,free_slots=Enumerable.Range(1,10).Where(slot=>!SlotExists(slot)).ToArray(),notes=bundle.notes,summary="Python形式"+bundle.source_version+" / "+s.objective+" / "+s.day+"日目 / "+s.tick+"tick / "+s.phase+"\n猫 "+s.cats.Count+"匹 / 資金 "+s.funds+" / 人気 "+s.popularity+" / 派遣 "+s.dispatches.Count+"件\n変換日時："+bundle.converted_at+"\n元セーブ："+bundle.source_path};
        }
        private void ImportLegacy(string path,int slot)
        {
            if(slot<1 || slot>10 || SlotExists(slot))throw new ArgumentException("未使用の保存スロットを指定してください。既存保存は上書きしません。");
            string target=Path.GetFullPath(Path.Combine(saveDirectory,"slot-"+slot+".json"));if(string.Equals(target,Path.GetFullPath(path),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("取込元と保存先は別ファイルにしてください。");
            var bundle=ReadLegacyBundle(path);var state=bundle.state;string baseJson=EncodeSave(new CafeSaveEnvelope {state=state});state.import_history=new CafeImportHistory {base_json=baseJson,base_digest=bundle.state_digest,source_hash=bundle.source_hash};
            Simulation.Validate(state);saves.SaveNew(slot,state);Simulation.Restore(state);ResetJournal();
        }
    }
}
