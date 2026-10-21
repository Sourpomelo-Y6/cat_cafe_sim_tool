using System;
using System.IO;
using System.Text;
using System.Threading;

namespace CatCafePreview.Standalone
{
    [Serializable] public class CafeDisplayReceipt {public int day;public string cat_id,net_stamina;}
    [Serializable] public class CafeSaveEnvelope
    {
        public int version = 1, slot;
        public string created_at;
        public CafeSnapshot state;
        public string[] double_bits;
        public CafeDailyBaseline[] daily_baselines;
        public CafeDailyRecord[] daily_records;
        public CafeNarrativeRow[] narratives;
        public CafeOpeningProgress opening;
        public CafeOutcome outcome;
        public CafeEndingProgress popularity_ending,bond_ending,patron_ending;
        public CafeConversationProgress conversation;
        public CafeConversationProgress regular_conversation;
        public CafeDispatchConversation[] dispatch_conversations;
        public string dispatch_conversation_selected;
        public string missing_conversation_selected,missing_conversation_phase;
        public CafeEquipmentConversation[] equipment_conversations;
        public string equipment_conversation_selected;
        public CafeReservationConversation[] reservation_conversations;
        public string reservation_conversation_selected;
        public CafeTrustConversation[] trust_conversations;
        public string trust_conversation_selected;
        public CafeAdoptionConversation[] adoption_conversations;
        public string adoption_conversation_selected;
        public CafeLodgeConversation[] lodge_conversations;
        public string lodge_conversation_selected;
        public CafeEncounterConversation[] encounter_conversations;
        public string encounter_conversation_selected;
        public CafeDispatchIntroductionConversation[] dispatch_introduction_conversations;
        public string dispatch_introduction_conversation_selected;
        public CafeDisplayReceipt[] display_receipts; // Optional UI facts; outside gameplay and replay digests.
    }
    public sealed class CafeSaveStore
    {
        private readonly string directory;
        private readonly Func<CafeSaveEnvelope, string> encode;
        private readonly Func<string, CafeSaveEnvelope> decode;
        private readonly Action<CafeSnapshot> validate;
        public CafeSaveStore(string directory, Func<CafeSaveEnvelope, string> encode, Func<string, CafeSaveEnvelope> decode, Action<CafeSnapshot> validate)
        { this.directory = directory; this.encode = encode; this.decode = decode; this.validate = validate; }
        private string PathFor(int slot)
        {
            if (slot < 1 || slot > 10) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(directory, "slot-" + slot + ".json");
        }
        public CafeSaveEnvelope Load(int slot)
        {
            var value = decode(File.ReadAllText(PathFor(slot), Encoding.UTF8));
            if (value == null || value.version != 1 || value.slot != slot || !DateTimeOffset.TryParse(value.created_at, out _)) throw new ArgumentException("Unsupported save envelope");
            validate(value.state); return value;
        }
        public void SaveNew(int slot,CafeSnapshot state)
        {
            validate(state);string path=PathFor(slot);Directory.CreateDirectory(directory);var value=new CafeSaveEnvelope {slot=slot,created_at=DateTimeOffset.UtcNow.ToString("o"),state=state};string temp=path+".import-"+Guid.NewGuid().ToString("N");
            try{using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){var bytes=Encoding.UTF8.GetBytes(encode(value));stream.Write(bytes,0,bytes.Length);stream.Flush(true);}var verified=decode(File.ReadAllText(temp,Encoding.UTF8));if(verified==null || verified.slot!=slot || verified.version!=1)throw new IOException("Import verification failed");validate(verified.state);File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}
        }
        public void Save(int slot, CafeSnapshot state)
        {
            validate(state); string path = PathFor(slot); Directory.CreateDirectory(directory);
            var value = new CafeSaveEnvelope { slot = slot, created_at = DateTimeOffset.UtcNow.ToString("o"), state = state };
            string temp = path + ".tmp";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(encode(value)); stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
            }
            // A failed write/validation never replaces the previous save.
            var readback = decode(File.ReadAllText(temp, Encoding.UTF8));
            if (readback == null || readback.version != 1 || readback.slot != slot) throw new IOException("Save verification failed");
            validate(readback.state);
            if (File.Exists(path)) ReplaceWithRetry(temp, path); else File.Move(temp, path);
        }
        // Keep the atomic replacement and backup. A short IO conflict may clear;
        // permanent errors still fail without a delete/move fallback.
        internal static void ReplaceWithRetry(string temp, string path, Action<string,string,string> replace = null, Action<int> wait = null)
        {
            replace = replace ?? ((source,target,backup) => File.Replace(source,target,backup));
            wait = wait ?? (delay => Thread.Sleep(delay));
            for (int attempt=0;;attempt++)
            {
                try { replace(temp,path,path+".bak"); return; }
                catch (IOException)
                {
                    if (attempt>=3 || !File.Exists(temp) || !File.Exists(path)) throw;
                    wait(20*(1<<attempt));
                }
            }
        }
    }
}
