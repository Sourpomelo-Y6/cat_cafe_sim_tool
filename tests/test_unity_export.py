import json
import os
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from content_images import ImageRegistry
from content_texts import TextStore
from unity_export import export_bundle

ASSET = Path(__file__).resolve().parents[2] / 'Docs/Prototypes/CalicoActionBackgroundPreview-v1.png'


class UnityExportTests(unittest.TestCase):
    def test_snapshot_deduplication_fallback_and_fresh_exports(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            registry = ImageRegistry(root / 'source')
            texts = TextStore(registry.directory / 'texts.json')
            registry.register('a', 'normal', ASSET)
            registry.register('a', 'play', ASSET)
            missing = registry.register('b', 'sleep', ASSET)
            missing.unlink()
            texts.save('a', {'introduction': 'ミケの紹介', 'scenes': {'play': '遊ぶミケ'}})
            before = texts.path.read_bytes()
            folder, report = export_bundle({'a': 'ミケ', 'b': 'タマ'}, registry, texts, root / 'exports')
            data = json.loads((folder / 'bundle.json').read_text(encoding='utf-8'))
            self.assertEqual(report['cat_count'], 2)
            self.assertEqual(report['image_count'], 1)
            self.assertEqual(len(data['cats'][0]['scenes']), 4)
            image = data['cats'][0]['scenes'][0]['image']
            self.assertEqual((folder / image).read_bytes(), ASSET.read_bytes())
            self.assertEqual(data['cats'][0]['scenes'][1]['text'], '遊ぶミケ')
            self.assertEqual(data['cats'][1]['scenes'][3]['image'], '')
            self.assertIn('読', '\n'.join(report['warnings']))
            self.assertEqual(texts.path.read_bytes(), before)
            second, _ = export_bundle({'a': 'ミケ'}, registry, texts, root / 'exports')
            self.assertNotEqual(folder, second)
            self.assertEqual(json.loads((folder / 'bundle.json').read_text(encoding='utf-8')), data)

    def test_corrupt_sources_do_not_create_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            registry = ImageRegistry(root / 'source')
            registry.directory.mkdir()
            texts = TextStore(registry.directory / 'texts.json')
            registry.path.write_text('{bad', encoding='utf-8')
            with self.assertRaises(ValueError):
                export_bundle({'a': '猫'}, registry, texts, root / 'exports')
            self.assertFalse((root / 'exports').exists())
            registry.path.unlink()
            with self.assertRaises(ValueError):
                export_bundle({'a': '猫'}, registry, texts, registry.directory)
            registry.register('a', 'normal', ASSET).write_bytes(b'not a png')
            folder, report = export_bundle({'a': '猫'}, registry, texts, root / 'exports')
            self.assertEqual(report['image_count'], 0)
            self.assertTrue(folder.exists())

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'requires a display')
    def test_gui_exports_adopted_content_only(self):
        import tkinter as tk
        from image_tool_gui import ContentImageToolWindow
        root = tk.Tk()
        root.withdraw()
        self.addCleanup(root.destroy)
        with tempfile.TemporaryDirectory() as directory:
            registry = ImageRegistry(Path(directory) / 'source')
            tool = ContentImageToolWindow(root, lambda: {'a': 'ミケ'}, registry=registry)
            tool.select_file(ASSET)  # 未採用の候補は出力しない。
            with patch('tkinter.filedialog.askdirectory', return_value=directory):
                tool.export_unity()
            folders = list(Path(directory).glob('CatCafeContent-*'))
            data = json.loads((folders[0] / 'bundle.json').read_text(encoding='utf-8'))
            self.assertEqual(data['cats'][0]['scenes'][0]['image'], '')
            self.assertIn('書き出しました', tool.notice.get())
            tool.window.destroy()
            root.update()
