using System;
using System.Text;
using System.Security.Cryptography;

namespace CatCafePreview.Standalone
{
    // CPython random.Random(str), version=2: UTF8 + SHA512, MT init_by_array.
    // A draw is keyed by seed/day/cat, so save/load needs no mutable RNG state.
    public static class PythonRandom
    {
        public static double Draw(string key)
        {
            byte[] input = Encoding.UTF8.GetBytes(key), digest;
            using (var sha = SHA512.Create()) digest = sha.ComputeHash(input);
            var bytes = new byte[input.Length + digest.Length];
            Buffer.BlockCopy(input, 0, bytes, 0, input.Length);
            Buffer.BlockCopy(digest, 0, bytes, input.Length, digest.Length);
            var words = new uint[(bytes.Length + 3) / 4];
            for (int n = 0; n < bytes.Length; n++) words[n / 4] |= (uint)bytes[bytes.Length - 1 - n] << (8 * (n % 4));
            var mt = new uint[624]; mt[0] = 19650218;
            unchecked
            {
                for (int n = 1; n < 624; n++) mt[n] = 1812433253 * (mt[n - 1] ^ (mt[n - 1] >> 30)) + (uint)n;
                int i = 1, j = 0;
                for (int n = Math.Max(624, words.Length); n > 0; n--)
                {
                    mt[i] = (mt[i] ^ ((mt[i - 1] ^ (mt[i - 1] >> 30)) * 1664525)) + words[j] + (uint)j;
                    i++; j++;
                    if (i >= 624) { mt[0] = mt[623]; i = 1; }
                    if (j >= words.Length) j = 0;
                }
                for (int n = 623; n > 0; n--)
                {
                    mt[i] = (mt[i] ^ ((mt[i - 1] ^ (mt[i - 1] >> 30)) * 1566083941)) - (uint)i;
                    if (++i >= 624) { mt[0] = mt[623]; i = 1; }
                }
            }
            mt[0] = 0x80000000;
            for (int n = 0; n < 624; n++)
            {
                uint y = (mt[n] & 0x80000000) | (mt[(n + 1) % 624] & 0x7fffffff);
                mt[n] = mt[(n + 397) % 624] ^ (y >> 1) ^ ((y & 1) != 0 ? 0x9908b0df : 0);
            }
            return ((Temper(mt[0]) >> 5) * 67108864.0 + (Temper(mt[1]) >> 6)) / 9007199254740992.0;
        }
        private static uint Temper(uint y) { y ^= y >> 11; y ^= (y << 7) & 0x9d2c5680; y ^= (y << 15) & 0xefc60000; return y ^ (y >> 18); }
        public static ulong Hash64(string key)
        {
            byte[] bytes; using (var sha = SHA256.Create()) bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
            ulong value = 0; for (int i = 0; i < 8; i++) value = (value << 8) | bytes[i]; return value;
        }
    }
}
