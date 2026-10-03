import os
import sys
import tempfile
import unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from content_images import ImageRegistry
from run import load_catalog, DEFAULT_CATALOG

ASSET = Path(__file__).resolve().parents[2] / 'Docs' / 'Prototypes' / 'CalicoActionBackgroundPreview-v1.png'


class ToolTests(unittest.TestCase):
    def test_catalog_and_copy_registration(self):
        self.assertIn('playtest-mike', load_catalog(DEFAULT_CATALOG))
        with tempfile.TemporaryDirectory() as directory:
            registry = ImageRegistry(directory)
            source = Path(directory) / 'original.png'
            source.write_bytes(ASSET.read_bytes())
            first = registry.register('猫-1', 'normal', source)
            source.unlink()
            self.assertTrue(first.exists())
            registry.register('猫-2', 'sleep', ASSET)
            second = registry.register('猫-1', 'normal', ASSET)
            self.assertNotEqual(first, second)
            self.assertEqual(registry.image_path('猫-1', 'normal'), second)
            self.assertTrue(registry.image_path('猫-2', 'sleep').exists())

    def test_corrupt_catalog_and_manifest(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'cats.json'
            path.write_text('[{"cat_id":"a","name":"猫"},{"cat_id":"a","name":"猫"}]', encoding='utf-8')
            with self.assertRaises(ValueError):
                load_catalog(path)
            registry = ImageRegistry(directory)
            registry.path.write_text('{invalid', encoding='utf-8')
            with self.assertRaises(ValueError):
                registry.register('cat-1', 'normal', ASSET)
            self.assertEqual(registry.path.read_text(encoding='utf-8'), '{invalid')

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'requires a display')
    def test_gui_without_game_session(self):
        import tkinter as tk
        from image_tool_gui import ContentImageToolWindow
        root = tk.Tk()
        root.withdraw()
        self.addCleanup(root.destroy)
        with tempfile.TemporaryDirectory() as directory:
            registry = ImageRegistry(directory)
            tool = ContentImageToolWindow(root, lambda: {'cat-1': 'ミケ', 'cat-2': 'タマ'}, registry=registry)
            root.update()
            tool.select_file(ASSET)
            self.assertIsNotNone(tool.pending)
            tool.save()
            self.assertTrue(registry.image_path('cat-1', 'normal').exists())
            tool.select_file(ASSET)
            tool.scene.set('遊ぶ')
            tool.refresh()
            self.assertIsNone(tool.pending)
            self.assertIn('disabled', tool.save_button.state())
            tool.window.destroy()
            root.update()
