import copy
import hashlib
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from PIL import Image
from state_images import StateImageStore, STATES, validate, read_definition, definition_text, MAX_JSON


class StateImagesV2Tests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name); self.store = StateImageStore(self.root / 'source')
        self.image = self.root / 'candidate.png'; Image.new('RGBA', (80, 120), 'purple').save(self.image)
        self.catalog = {'playtest-mike': '同名', 'playtest-sora': '同名'}

    def legacy(self):
        self.store.adopt('stress_high', self.image)
        data = self.store.read(); data['version'] = 1
        for row in data['bindings']: row['cat_id'] = 'cat-mike'
        self.store.path.write_text(json.dumps(data, indent=2), encoding='utf-8')
        return self.store.path.read_bytes()

    def test_legacy_read_export_and_first_edit(self):
        original = self.legacy()
        self.assertEqual(self.store.read()['version'], 2)
        pack = self.store.export(self.root / 'exports', self.catalog)
        data = read_definition((pack / 'state_images.json').read_text(encoding='utf-8'))
        self.assertEqual(data['version'], 2); self.assertNotIn('cat_id', data['bindings'][0])
        self.assertEqual(self.store.path.read_bytes(), original)
        self.assertFalse((self.store.directory / 'manifest.v1.json').exists())
        self.store.adopt('fatigue_high', self.image, content_id='playtest-sora')
        self.assertEqual((self.store.directory / 'manifest.v1.json').read_bytes(), original)
        self.assertEqual(len(self.store.read()['bindings']), 2)

    def test_failed_backup_and_replace_preserve_manifest(self):
        original = self.legacy(); backup = self.store.directory / 'manifest.v1.json'; backup.write_bytes(b'different')
        with self.assertRaises(ValueError): self.store.adopt('fatigue_high', self.image)
        self.assertEqual(self.store.path.read_bytes(), original)
        backup.unlink()
        with patch('state_images.os.replace', side_effect=OSError('locked')):
            with self.assertRaises(OSError): self.store.adopt('fatigue_high', self.image)
        self.assertEqual(self.store.path.read_bytes(), original)
        self.assertEqual(backup.read_bytes(), original)
        self.store.adopt('fatigue_high', self.image)
        self.assertEqual(self.store.read()['version'], 2)

    def test_individual_binding_clear_and_full_export(self):
        for cid in self.catalog:
            for state in STATES: self.store.adopt(state, self.image, content_id=cid)
        self.store.adopt('stress_high', content_id='playtest-sora')
        data, blobs = self.store.export_data(self.catalog, '1' * 32)
        self.assertEqual(len(data['bindings']), 5); self.assertEqual(len(blobs), 1)
        self.assertIsNotNone(self.store.image_path('stress_high'))
        self.assertIsNone(self.store.image_path('stress_high', content_id='playtest-sora'))
        with self.assertRaises(ValueError): self.store.export_data({'playtest-mike': 'ミケ'}, '1' * 32)
        self.assertEqual(len(self.store.read()['bindings']), 5)

    def test_contract_boundaries_and_versions(self):
        self.store.adopt('stress_high', self.image); row = self.store.read()['bindings'][0]
        rows = [dict(row, content_cat_id=f'cat-{i}', state=state) for i in range(64) for state in STATES]
        data = dict(version=2, pack_id='a' * 32, bindings=rows)
        validate(data); self.assertLess(len(definition_text(data)), MAX_JSON)
        for bad in [dict(data, bindings=rows + [dict(row, content_cat_id='extra')]),
                    dict(data, bindings=[dict(row, content_cat_id=f'cat-{i}') for i in range(65)]),
                    dict(data, version=3), dict(data, version=True), dict(data, bindings=[dict(row, cat_id='cat-mike')]),
                    dict(data, bindings=[row, row]), dict(data, bindings=[dict(row, content_cat_id='a' * 65)]),
                    dict(data, bindings=[dict(row, state='unknown')])]:
            with self.assertRaises(ValueError): validate(bad)
        legacy = dict(version=1, pack_id='a' * 32, bindings=[dict(row, cat_id='cat-sora')])
        with self.assertRaises(ValueError): validate(legacy)
        text = json.dumps(dict(version=2, pack_id='a' * 32, bindings=[]))
        read_definition(text + ' ' * (MAX_JSON - len(text)))
        with self.assertRaises(ValueError): read_definition(text + ' ' * (MAX_JSON + 1 - len(text)))
        with self.assertRaises(ValueError): read_definition('{"version":2,"version":2,"pack_id":"'+'a'*32+'","bindings":[]}')

    def test_total_bytes_count_distinct_files(self):
        for cid in self.catalog: self.store.adopt('stress_high', self.image, content_id=cid)
        size = self.image.stat().st_size
        with patch('state_images.MAX_TOTAL_BYTES', size):
            self.assertEqual(len(self.store.export_data(self.catalog, '1' * 32)[1]), 1)
        Image.new('RGBA', (80, 120), 'blue').save(self.image)
        self.store.adopt('fatigue_high', self.image, content_id='playtest-sora')
        with patch('state_images.MAX_TOTAL_BYTES', size):
            with self.assertRaises(ValueError): self.store.export_data(self.catalog, '1' * 32)

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'Set CAT_CAFE_TEST_GUI=1')
    def test_gui_switch_duplicate_names_clear_and_export(self):
        import tkinter as tk
        from state_image_editor_gui import StateImageEditorWindow
        root = tk.Tk(); root.withdraw()
        try:
            gui = StateImageEditorWindow(root, self.store, lambda: self.catalog)
            gui.candidate = self.image; gui.compare(); gui.adopt()
            sora = next(label for label, cid in gui.contents.items() if cid == 'playtest-sora')
            gui.content.set(sora); gui.changed(); self.assertFalse(gui.compared); self.assertIsNone(gui.candidate)
            gui.candidate = self.image; gui.compare(); gui.adopt()
            exports = self.root / 'exports'
            with patch('state_image_editor_gui.filedialog.askdirectory', return_value=str(exports)), patch('state_image_editor_gui.messagebox.askokcancel', return_value=True): gui.export()
            pack = next(exports.iterdir()); self.assertEqual(len(read_definition((pack / 'state_images.json').read_text())['bindings']), 2)
            gui.clear(); self.assertEqual(len(self.store.read()['bindings']), 1)
        finally: root.destroy()
