using System;
using System.Collections;
using System.Reflection;
namespace CatCafePreview.Standalone
{
    // Only the verified pre-R2 rules are supported. Never relabel an unknown save.
    public static class CafeHousingMigration
    {
        public const string LegacyRulesId="2613fc4f2312feaf713cfafe96de283a1578fa67c6241f0e2da4dbc539939d8f";
        static bool Equal(object a,object b)
        {
            if(a==null || b==null)return a==b;
            if(a.GetType()!=b.GetType())return false;
            if(a is string || a.GetType().IsPrimitive)return a.Equals(b);
            if(a is IEnumerable x && b is IEnumerable y){var i=x.GetEnumerator();var j=y.GetEnumerator();while(i.MoveNext()){if(!j.MoveNext() || !Equal(i.Current,j.Current))return false;}return !j.MoveNext();}
            foreach(var f in a.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance))if(!Equal(f.GetValue(a),f.GetValue(b)))return false;
            return true;
        }
        public static void Migrate(CafeSaveEnvelope save,CafeRules current,CafeRules legacy,Func<string,CafeSaveEnvelope> decode,Func<CafeSaveEnvelope,string> encode)
        {
            if(save?.state==null || save.state.rules_id==current.rules_id)return;
            if(legacy==null || legacy.rules_id!=LegacyRulesId || save.state.rules_id!=LegacyRulesId || (legacy.housing_extensions?.Length ?? 0)!=0)
                throw new ArgumentException("この保存のゲームルールは移行対象ではありません。");
            foreach(var f in typeof(CafeRules).GetFields())if(f.Name!="rules_id" && f.Name!="housing_extensions" && !Equal(f.GetValue(current),f.GetValue(legacy)))
                throw new ArgumentException("収容拡張以外のゲームルールが異なるため移行できません。");
            var old=new CafeSimulation(legacy);old.Validate(save.state);CafeInteractionStatistics.Validate(save.interaction_statistics,save.state);
            // Imported Python saves retain their validated anchor and operation tape.
            // Rebase only its rules identity/digest, after validating it under the old rules.
            var imported=save.state.import_history;
            if(imported!=null){var baseline=decode(imported.base_json);baseline.state.rules_id=current.rules_id;imported.base_json=encode(baseline);imported.base_digest=CafeLegacyImport.GameDigest(baseline.state);}
            save.state.rules_id=current.rules_id;
            new CafeSimulation(current).Validate(save.state);
        }
    }
}
