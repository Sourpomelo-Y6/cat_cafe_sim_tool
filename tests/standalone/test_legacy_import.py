"""Read-only migration rejection and source-link checks using actual Python saves."""
import copy, json, tempfile, unittest
from pathlib import Path
from generate_reference import ROOT
from cat_cafe_sim.cafe_new_game import create_game, starting_conditions
from cat_cafe_sim.storage.cafe_saves import save_game
from convert_python_save import convert, load_readonly, check_rules, read_json, sha

class LegacyImportTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(dir=ROOT/'Logs/StandaloneLegacyImport')
        self.root=Path(self.temp.name);self.session=create_game(self.root/'source',starting_conditions('free'))
        self.source=save_game(self.session,self.root/'python.json',auto_assign=True)
        self.original=self.source.read_bytes();self.relation=self.session.store.path;self.relationship=self.relation.read_bytes()
    def tearDown(self):self.temp.cleanup()
    def unchanged(self):
        self.assertEqual(self.original,self.source.read_bytes());self.assertEqual(self.relationship,self.relation.read_bytes())
    def test_existing_destination(self):
        target=self.root/'existing.json';target.write_bytes(b'keep')
        with self.assertRaises(ValueError):convert(self.source,target)
        self.assertEqual(b'keep',target.read_bytes());self.unchanged()
    def test_source_destination(self):
        with self.assertRaises(ValueError):convert(self.source,self.source)
        self.unchanged()
    def test_relationship_destination(self):
        with self.assertRaises(ValueError):convert(self.source,self.relation)
        self.unchanged()
    def test_missing_relationship(self):
        with self.assertRaises(ValueError):load_readonly(self.source,self.root/'missing.json')
        self.unchanged()
    def test_moved_relationship(self):
        moved=self.root/'moved.json';moved.write_bytes(self.relationship)
        session,_,path=load_readonly(self.source,moved)
        self.assertEqual(moved.resolve(),path);self.assertEqual(self.session.core.funds,session.core.funds);self.unchanged()
    def test_unsupported_version(self):
        data=read_json(self.source);data['format_version']=999;path=self.root/'bad.json';path.write_text(json.dumps(data),encoding='utf-8')
        with self.assertRaises(ValueError):load_readonly(path)
        self.unchanged()
    def test_duplicate_fields(self):
        path=self.root/'duplicate.json';path.write_text('{"format_version":1,"format_version":2}',encoding='utf-8')
        with self.assertRaises(ValueError):load_readonly(path)
        self.unchanged()
    def test_nonfinite(self):
        path=self.root/'nan.json';path.write_text('{"x":NaN}',encoding='utf-8')
        with self.assertRaises(ValueError):read_json(path)
        self.unchanged()
    def test_changed_rule(self):
        session,_,_=load_readonly(self.source);session.core.management['rules']['starting_funds']+=1
        with self.assertRaises(ValueError):check_rules(session,self.root/'references')
        self.unchanged()
    def test_changed_python_defaults(self):
        from unittest.mock import patch
        from export_standalone_rules import exported_rules
        rules=exported_rules();rules['starting_funds']+=1
        session,_,_=load_readonly(self.source)
        with patch('export_standalone_rules.exported_rules',return_value=rules):
            with self.assertRaises(ValueError):check_rules(session,self.root/'references')
        self.unchanged()
    def test_relationship_conflict(self):
        data=read_json(self.relation);data['cats']['cat-mike']['name']='変更';path=self.root/'conflict.json';path.write_text(json.dumps(data),encoding='utf-8')
        with self.assertRaises(ValueError):load_readonly(self.source,path)
        self.unchanged()
    def test_format1_missing_standard_modules(self):
        from cat_cafe_sim.core.multi_seat_cafe import MultiSeatCafeCore
        current=self.session.core;legacy=MultiSeatCafeCore(current.config,seed=current.seed,start_state=current.start_state,cat_ids=current.roster_ids)
        data=read_json(self.source);data.update(format_version=1,core=legacy.log());path=self.root/'format1.json';path.write_text(json.dumps(data),encoding='utf-8')
        with self.assertRaises(ValueError):convert(path,self.root/'import.json')
        self.assertFalse((self.root/'import.json').exists());self.unchanged()

if __name__=='__main__':unittest.main()
