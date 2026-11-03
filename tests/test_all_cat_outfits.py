import copy
import json
import os
import tempfile
import unittest
from pathlib import Path
from PIL import Image
from cat_outfits import CatOutfitStore, validate, read_definition, definition_text


class AllCatOutfitsTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.store = CatOutfitStore(self.root)
        self.png = self.root / 'fixture.png'
        Image.new('RGBA', (20, 30), 'purple').save(self.png)

    def test_shared_material_and_individual_clear(self):
        for cid in ('playtest-mike', 'playtest-sora'):
            self.store.adopt('ribbon', self.png, content_id=cid, label=cid)
        data, blobs = self.store.export_data({'playtest-mike': '', 'playtest-sora': ''}, '1' * 32)
        self.assertEqual((data['version'], len(data['outfits']), len(blobs)), (2, 2, 1))
        self.store.adopt('ribbon', content_id='playtest-mike')
        self.assertEqual(self.store.read()['outfits'][0]['content_cat_id'], 'playtest-sora')
        with self.assertRaises(ValueError):
            self.store.export_data({}, '1' * 32)

    def test_legacy_read_export_and_atomic_migration(self):
        self.store.adopt('ribbon', self.png)
        old = self.store.read()
        old['version'] = 1
        old['outfits'][0]['cat_id'] = 'cat-mike'
        raw = json.dumps(old).encode() + b'\n'
        self.store.path.write_bytes(raw)
        self.assertEqual(self.store.read()['version'], 2)
        self.assertEqual(self.store.export_data({'playtest-mike': ''}, '1' * 32)[0]['version'], 2)
        self.assertEqual(self.store.path.read_bytes(), raw)
        backup = self.store.directory / 'manifest.v1.json'
        backup.write_bytes(b'different')
        with self.assertRaises(ValueError):
            self.store.adopt('ribbon', self.png, content_id='playtest-sora')
        self.assertEqual(self.store.path.read_bytes(), raw)
        backup.unlink()
        self.store.adopt('ribbon', self.png, content_id='playtest-sora')
        self.assertEqual(backup.read_bytes(), raw)
        self.assertEqual(len(self.store.read()['outfits']), 2)
        self.assertTrue(self.png.exists())

    def test_contract_boundaries(self):
        self.store.adopt('ribbon', self.png)
        data = self.store.read()
        row = data['outfits'][0]
        data['outfits'] = [dict(row, content_cat_id='c' + str(i)) for i in range(64)]
        validate(data)
        self.assertLessEqual(len(definition_text(data)), 100000)
        bad = copy.deepcopy(data); bad['outfits'].append(dict(row, content_cat_id='extra'))
        with self.assertRaises(ValueError):
            validate(bad)
        for key, value in [('cat_id', 'cat-mike'), ('content_cat_id', 'C'), ('outfit_id', 'future'), ('label', '\U0001f408' * 21)]:
            bad = copy.deepcopy(data); bad['outfits'][0][key] = value
            with self.assertRaises(ValueError):
                validate(bad)
        data['outfits'][0]['label'] = '\U0001f408' * 20
        validate(data)
        text = definition_text(data)
        units = len(text.encode('utf-16-le')) // 2
        read_definition(text + ' ' * (100000 - units))
        with self.assertRaises(ValueError):
            read_definition(text + ' ' * (100001 - units))

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'Tk callbacks')
    def test_gui_switch_resets_candidate_and_renames_one_family(self):
        import tkinter as tk
        from cat_outfit_editor_gui import CatOutfitEditorWindow
        root = tk.Tk(); root.withdraw()
        try:
            catalog = {'playtest-mike': '同名', 'playtest-sora': '同名'}
            view = CatOutfitEditorWindow(root, self.store, lambda: catalog)
            view.candidate = self.png; view.compare(); view.adopt()
            view.content.set(next(label for label, cid in view.contents.items() if cid == 'playtest-sora'))
            view.changed()
            self.assertFalse(view.compared)
            self.assertIsNone(view.candidate)
            view.candidate = self.png; view.compare(); view.adopt()
            view.name.set('ソラのリボン'); view.rename()
            rows = {r['content_cat_id']: r for r in self.store.read()['outfits']}
            self.assertEqual(rows['playtest-sora']['label'], 'ソラのリボン')
            self.assertEqual(rows['playtest-mike']['label'], 'リボン姿')
            catalog.clear(); view.refresh_catalog()
            self.assertEqual(len(view.contents), 2)
            with self.assertRaises(ValueError):
                view.rename()
            view.clear()
            self.assertEqual(len(self.store.read()['outfits']), 1)
        finally:
            root.destroy()
