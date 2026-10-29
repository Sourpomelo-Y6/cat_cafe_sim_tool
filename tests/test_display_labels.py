import json
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from display_labels import DisplayLabelStore, DEFAULTS
from interaction_texts import condition, key

class DisplayLabelTests(unittest.TestCase):
    def test_roundtrip_reset_and_invalid_save_preserves_file(self):
        with tempfile.TemporaryDirectory() as tmp:
            store=DisplayLabelStore(Path(tmp)/'labels.json')
            self.assertEqual(store.read()['overrides'], {})
            store.save({'version':1,'overrides':{'modes':{'voice':'おしゃべり'},'reactions':{'confused':'きょとん'}}})
            original=store.path.read_bytes()
            self.assertEqual(store.export_data()['modes'], [{'id':'voice','label':'おしゃべり'}])
            for data in [{'modes':{'new_mode':'追加'}},{'reactions':{'confused':'通常'}},{'actions':{'direct':'a\nb'}},{'modes':{'voice':'遊ぶ'}},{'modes':{'voice':'x'*33}},{'modes':{'voice':'<b>声</b>'}}]:
                with self.assertRaises(ValueError):store.save({'version':1,'overrides':data})
                self.assertEqual(store.path.read_bytes(),original)
            store.save({'version':1,'overrides':{'modes':{'voice':DEFAULTS['modes']['voice']}}})
            self.assertEqual(store.export_data()['modes'],[])
    def test_session_aliases_keep_ids_and_condition_keys(self):
        script="""import sys;sys.path.insert(0,sys.argv[1]);import display_labels as d;d.DEFAULT_PATH=__import__('pathlib').Path(sys.argv[2]);d._session=d.DisplayLabelStore().read()['overrides'];import interaction_texts as t;import interaction_scenes as s;import image_tool_gui as g;assert t.MODES['voice']=='おしゃべり';assert t.REACTIONS['confused']=='きょとん';assert s.MODE_LABELS['voice']=='おしゃべり';assert dict(g.SCENES)['voice']=='おしゃべり';assert t.condition('cat_response','cat-mike',mode='voice',reaction='confused')['scene_key']=='voice'"""
        with tempfile.TemporaryDirectory() as tmp:
            store=DisplayLabelStore(Path(tmp)/'labels.json');store.save({'version':1,'overrides':{'modes':{'voice':'おしゃべり'},'reactions':{'confused':'きょとん'}}})
            subprocess.run([sys.executable,'-X','utf8','-c',script,str(Path(__file__).resolve().parents[1]),str(store.path)],check=True)
    def test_special_alias_does_not_add_normal_reaction(self):
        script="""import sys;sys.path.insert(0,sys.argv[1]);import display_labels as d;d._session={'reactions':{'open_up':'安心する','simultaneous':'息ぴったり'}};import interaction_texts as t;import interaction_scenes as s;assert t.NARRATIVE_SCENES['open_up']=='安心する';assert dict(s.SPECIAL_ACTIONS)['simultaneous']=='息ぴったり';assert 'open_up' not in t.reactions('cat_response','teaser');assert len(t.REACTIONS)==6"""
        subprocess.run([sys.executable,'-X','utf8','-c',script,str(Path(__file__).resolve().parents[1])],check=True)
    def test_export_carries_aliases_without_rewriting_content(self):
        from unity_export import export_bundle
        from content_images import ImageRegistry
        from content_texts import TextStore
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);registry=ImageRegistry(root/'source');texts=TextStore(registry.directory/'texts.json');texts.save('cat-mike',{'introduction':'声をかけるミケ','scenes':{'normal':'元の文章'}})
            labels=DisplayLabelStore(root/'labels.json');labels.save({'version':1,'overrides':{'modes':{'voice':'おしゃべり'},'reactions':{'confused':'きょとん'}}})
            before=texts.path.read_bytes();c=condition('cat_response','cat-mike',mode='voice',reaction='confused');before_key=key(c)
            folder,_=export_bundle({'cat-mike':'ミケ'},registry,texts,root/'out',display_labels=labels)
            data=json.loads((folder/'bundle.json').read_text(encoding='utf8'));self.assertEqual(data['display_labels'],labels.export_data());self.assertEqual(data['cats'][0]['introduction'],'声をかけるミケ');self.assertEqual(texts.path.read_bytes(),before);self.assertEqual(key(c),before_key)
    def test_corrupt_file_is_not_overwritten_on_read(self):
        with tempfile.TemporaryDirectory() as tmp:
            store=DisplayLabelStore(Path(tmp)/'labels.json');store.path.write_text('{broken',encoding='utf8')
            with self.assertRaises(ValueError):store.read()
            self.assertEqual(store.path.read_text(),'{broken')

@unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI')=='1','requires display')
class DisplayLabelGuiTests(unittest.TestCase):
    def test_save_invalid_and_reset(self):
        import tkinter as tk
        from display_labels_gui import DisplayLabelsWindow
        with tempfile.TemporaryDirectory() as tmp:
            root=tk.Tk();root.withdraw()
            try:
                store=DisplayLabelStore(Path(tmp)/'labels.json');ui=DisplayLabelsWindow(root,store)
                ui.fields['modes','voice'].set('おしゃべり');ui.fields['reactions','confused'].set('きょとん');self.assertTrue(ui.save())
                original=store.path.read_bytes();ui.fields['modes','voice'].set('遊ぶ')
                with patch('tkinter.messagebox.showerror'):self.assertFalse(ui.save())
                self.assertEqual(store.path.read_bytes(),original);ui.reset();self.assertTrue(ui.save());self.assertEqual(store.read()['overrides'],{})
            finally:root.destroy()
if __name__=='__main__':unittest.main()
