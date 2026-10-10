using System;
using System.IO;
using System.Text;

namespace CatCafePreview.Standalone
{
    [Serializable] public class CafeSaveEnvelope
    {
        public int version = 1, slot;
        public string created_at;
        public CafeSnapshot state;
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
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path);
        }
    }
}
