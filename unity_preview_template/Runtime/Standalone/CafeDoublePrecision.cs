using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CatCafePreview.Standalone
{
    // JsonUtility may round a double on read. Preserve IEEE bits separately so
    // save/resume cannot change an action at a stamina or engagement boundary.
    public static class CafeDoublePrecision
    {
        private static void Walk(object value, Action<object,FieldInfo> visit)
        {
            if (value == null || value is string || value.GetType().IsPrimitive) return;
            if (value is IEnumerable collection) { foreach (object item in collection) Walk(item,visit); return; }
            foreach (var f in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(f => f.Name,StringComparer.Ordinal))
                if (f.FieldType == typeof(double)) visit(value,f); else Walk(f.GetValue(value),visit);
        }
        public static string[] Capture(CafeSnapshot state)
        {
            var bits = new List<string>();
            Walk(state,(owner,field) => bits.Add(Convert.ToBase64String(BitConverter.GetBytes((double)field.GetValue(owner)))));
            return bits.ToArray();
        }
        public static void Restore(CafeSnapshot state, string[] bits)
        {
            if (bits == null) return;
            var fields = new List<KeyValuePair<object,FieldInfo>>(); Walk(state,(owner,field) => fields.Add(new KeyValuePair<object,FieldInfo>(owner,field)));
            if (fields.Count != bits.Length) throw new ArgumentException("保存の数値記録が一致しません。");
            for (int i=0;i<fields.Count;i++)
            {
                byte[] bytes = Convert.FromBase64String(bits[i]); if (bytes.Length != 8) throw new ArgumentException("保存の数値記録が不正です。");
                double precise = BitConverter.ToDouble(bytes,0), json = (double)fields[i].Value.GetValue(fields[i].Key);
                if (double.IsNaN(precise) || double.IsInfinity(precise) || Math.Abs(precise-json) > 1e-10*Math.Max(1,Math.Abs(precise))) throw new ArgumentException("保存の数値と復元記録が矛盾しています。");
                fields[i].Value.SetValue(fields[i].Key,precise);
            }
        }
    }
}
