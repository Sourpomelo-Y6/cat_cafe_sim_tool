import copy
import hashlib
import json
import os
from pathlib import Path
import tempfile
import unittest
from PIL import Image
from state_images import StateImageStore, STATES, validate, png


class StateImagesTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.store = StateImageStore(self.root / 'source')
        self.image = self.root / 'test.png'
        Image.new('RGBA', (32, 48), 'red').save(self.image)
        self.catalog = {'playtest-mike': 'ミケ'}

    def test_adopt_clear_export_and_no_default_sidecar(self):
        self.assertEqual(self.store.export_data({}, '1' * 32), (None, {}))
        for state in STATES:
            self.store.adopt(state, self.image)
        self.assertEqual(len(self.store.read()['bindings']), 3)
        pack = self.store.export(self.root / 'exports', self.catalog)
        data = validate(json.loads((pack / 'state_images.json').read_text(encoding='utf-8')), self.catalog)
        for row in data['bindings']:
            self.assertEqual(hashlib.sha256((pack / row['image']).read_bytes()).hexdigest(), Path(row['image']).stem)
        for state in STATES:
            self.store.adopt(state)
        self.assertTrue(self.image.exists())
        self.assertEqual(self.store.export_data({}, '1' * 32)[0]['bindings'], [])

    def test_reject_contract_and_corruption(self):
        self.store.adopt('stress_high', self.image)
        data = self.store.read()
        for key, value in [('version', True), ('pack_id', '../bad'), ('bindings', [data['bindings'][0]] * 2)]:
            bad = copy.deepcopy(data); bad[key] = value
            with self.assertRaises(ValueError):
                validate(bad)
        for key, value in [('state', []), ('state', 'unknown'), ('image', '../test.png'), ('content_cat_id', 'other/')]:
            bad = copy.deepcopy(data); bad['bindings'][0][key] = value
            with self.assertRaises(ValueError):
                validate(bad)
        self.store.image_path('stress_high').write_bytes(b'broken')
        with self.assertRaises(ValueError):
            self.store.export_data(self.catalog, '1' * 32)
        self.store.path.write_text('{"version":1,"version":1}', encoding='utf-8')
        original = self.store.path.read_bytes()
        with self.assertRaises(ValueError):
            self.store.adopt('stress_high')
        self.assertEqual(original, self.store.path.read_bytes())

    def test_png_and_comparison_change(self):
        digest = hashlib.sha256(self.image.read_bytes()).hexdigest()
        Image.new('RGB', (32, 32), 'blue').save(self.image)
        with self.assertRaises(ValueError):
            self.store.adopt('stress_high', self.image, digest)
        for content in (b'not png', self.image.read_bytes()[:33]):
            with self.assertRaises(ValueError):
                png(content)
        Image.new('RGB', (2049, 1)).save(self.image)
        with self.assertRaises(ValueError):
            self.store.adopt('stress_high', self.image)

    def test_full_export_optional_sidecar(self):
        from content_images import ImageRegistry
        from content_texts import TextStore
        from unity_export import export_bundle
        registry = ImageRegistry(self.root / 'source')
        texts = TextStore(self.root / 'texts.json')
        folder, _ = export_bundle(self.catalog, registry, texts, self.root / 'exports', profiles={})
        self.assertFalse((folder / 'state_images.json').exists())
        self.store.adopt('stress_high', self.image)
        folder, _ = export_bundle(self.catalog, registry, texts, self.root / 'exports', profiles={})
        data = validate(json.loads((folder / 'state_images.json').read_text(encoding='utf-8')), self.catalog)
        self.assertEqual(data['pack_id'], json.loads((folder / 'bundle.json').read_text(encoding='utf-8'))['pack_id'])
        self.assertTrue((folder / data['bindings'][0]['image']).exists())

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'Set CAT_CAFE_TEST_GUI=1 for Tk callbacks')
    def test_editor_compare_adopt_and_clear(self):
        import tkinter as tk
        from state_image_editor_gui import StateImageEditorWindow
        root = tk.Tk(); root.withdraw()
        try:
            view = StateImageEditorWindow(root, self.store, lambda: self.catalog)
            view.candidate = self.image
            with self.assertRaises(ValueError):
                view.adopt()
            view.compare(); view.adopt()
            self.assertEqual(len(self.store.read()['bindings']), 1)
            view.clear()
            self.assertEqual(self.store.read()['bindings'], [])
        finally:
            root.destroy()


if __name__ == '__main__':
    unittest.main()
