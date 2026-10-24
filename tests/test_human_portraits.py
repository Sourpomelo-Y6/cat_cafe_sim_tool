import json
import tempfile
import unittest
import os
from unittest.mock import patch
from pathlib import Path
from PIL import Image
from human_portraits import HumanPortraitStore
from interaction_subjects import SubjectRegistry

class HumanPortraitTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name);self.source=self.root/'source';self.source.mkdir()
        self.store=HumanPortraitStore(self.source)
        self.registry=self.store.subjects
        self.registry.save(self.registry.get('guest-1'))
        self.a=self.root/'a.png';Image.new('RGBA',(64,64),(80,120,90,100)).save(self.a)
        self.b=self.root/'b.png';Image.new('RGBA',(64,64),(180,100,90,255)).save(self.b)
    def tearDown(self):self.temp.cleanup()
    def test_register_reload_replace_export_and_alpha(self):
        before=self.registry.path.read_bytes()
        self.store.register('guest-1',self.a)
        row=HumanPortraitStore(self.source).read()['people'][0]
        self.assertEqual(row['game_id'],'guest-1')
        folder,warnings=self.store.export(self.root/'exports');self.assertEqual(warnings,[])
        self.assertEqual(Image.open(folder/row['image']).getpixel((0,0))[3],100)
        self.assertEqual(self.store.export(self.root/'exports')[0],folder)
        self.store.register('guest-1',self.b)
        self.assertNotEqual(self.store.read()['people'][0]['image'],row['image'])
        self.assertEqual(self.registry.path.read_bytes(),before)
        self.assertEqual(len(self.store.read()['people']),1)
    def test_missing_stale_and_unknown(self):
        self.assertRaises(ValueError,self.store.register,'unknown',self.a)
        self.assertRaises(ValueError,self.store.register,'player',self.a)
        self.store.register('guest-1',self.a)
        row=self.store.read()['people'][0];(self.source/row['image']).unlink()
        data,blobs,warnings=self.store.export_data();self.assertEqual(data['people'],[]);self.assertTrue(warnings)
        self.store.register('guest-1',self.a)
        subject=self.registry.get('guest-1');subject['game_id']='temporary-other';self.registry.save(subject)
        self.assertEqual(self.store.export_data()[0]['people'],[])
    def test_duplicate_path_and_wrong_kind(self):
        self.store.register('guest-1',self.a);data=self.store.read();data['people']*=2
        self.assertRaises(ValueError,self.store.validate,data)
        data=self.store.read();data['people'][0]['image']='../a.png';self.assertRaises(ValueError,self.store.validate,data)
        self.assertRaises(ValueError,self.store.register,'cat-mike',self.a)
    def test_clear_preserves_source(self):
        self.store.register('guest-1',self.a);row=self.store.read()['people'][0]
        self.store.remove('guest-1');self.assertEqual(self.store.read()['people'],[]);self.assertTrue((self.source/row['image']).exists())

    def test_full_export_sidecar_is_not_cat_content(self):
        from content_images import ImageRegistry
        from content_texts import TextStore
        from unity_export import export_bundle
        self.store.register('guest-1',self.a)
        folder,report=export_bundle({'a':'猫'},ImageRegistry(self.source),TextStore(self.source/'texts.json'),self.root/'export')
        cats=json.loads((folder/'bundle.json').read_text(encoding='utf-8'))
        people=json.loads((folder/'human_portraits.json').read_text(encoding='utf-8'))
        self.assertEqual([r['cat_id'] for r in cats['cats']],['a'])
        self.assertEqual(people['pack_id'],cats['pack_id'])
        self.assertEqual(people['people'][0]['game_id'],'guest-1')
        self.assertTrue((folder/people['people'][0]['image']).is_file())

@unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI')=='1','requires display')
class HumanPortraitGuiTests(HumanPortraitTests):
    def test_actual_manager_buttons(self):
        import tkinter as tk
        from interaction_subject_gui import SubjectManagerWindow
        root=tk.Tk();root.withdraw()
        try:
            ui=SubjectManagerWindow(root,self.registry);ui.tabs.select(1);root.update();ui.load(self.registry.get('guest-1'))
            with patch('tkinter.filedialog.askopenfilename',return_value=str(self.a)):ui.register_portrait()
            root.update();self.assertEqual(self.store.read()['people'][0]['game_id'],'guest-1')
            with patch('tkinter.filedialog.askopenfilename',return_value=str(self.b)):ui.register_portrait()
            root.update();self.assertEqual(len(self.store.read()['people']),1)
            ui.reload();ui.preview_portrait();root.update()
            with patch('tkinter.filedialog.askdirectory',return_value=str(self.root/'gui-export')):ui.export_portraits()
            self.assertTrue(list((self.root/'gui-export').glob('*/human_portraits.json')))
            ui.remove_portrait();self.assertEqual(self.store.read()['people'],[])
        finally:root.destroy()

if __name__=='__main__':unittest.main()
