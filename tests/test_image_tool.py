import os
import sys
import tempfile
import unittest
import time
from unittest.mock import patch
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from content_images import ImageRegistry
from run import load_catalog, DEFAULT_CATALOG

ASSET = Path(__file__).resolve().parents[2] / 'Docs' / 'Prototypes' / 'CalicoActionBackgroundPreview-v1.png'


class ToolTests(unittest.TestCase):
    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'requires a display')
    def test_combination_selector_and_registration(self):
        import tkinter as tk
        from image_tool_gui import ContentImageToolWindow
        root = tk.Tk();root.withdraw();self.addCleanup(root.destroy)
        with tempfile.TemporaryDirectory() as directory:
            registry = ImageRegistry(Path(directory) / 'images')
            tool = ContentImageToolWindow(root, lambda: {'cat-a': 'ミケ'}, registry=registry)
            tool.scene.set('ボール遊び');tool.refresh()
            tool.action.set('フェイントを入れる');tool.refresh()
            self.assertEqual(tool.selection(), ('cat-a', 'ball__feint'))
            tool.select_file(ASSET);tool.save()
            self.assertTrue(registry.image_path('cat-a', 'ball__feint').is_file())
            self.assertIsNone(registry.image_path('cat-a', 'ball'))
            tool.scene.set('ブラッシング');tool.refresh()
            self.assertEqual(tool.action.get(), '動作の共通画像')
            self.assertNotIn('フェイントを入れる', tool.action_selector['values'])
            tool.action.set('相手に合わせる');tool.refresh()
            self.assertEqual(tool.selection(), ('cat-a', 'brush__adapt'))
            tool.scene.set('通常');tool.refresh()
            self.assertEqual(tool.selection(), ('cat-a', 'normal'))
            self.assertTrue(tool.action_selector.instate(['disabled']))
            tool.scene.set('ボール遊び');tool.refresh()
            tool.action.set('（なにか）+心を開く');tool.refresh()
            self.assertEqual(tool.selection(), ('cat-a', 'ball__open_up'))
            tool.action.set('心をつかむ+心を開く');tool.refresh()
            self.assertEqual(tool.selection(), ('cat-a', 'ball__simultaneous'))
            special = [label for label in tool.action_selector['values'] if '+心を開く' in label]
            self.assertEqual(len(special), 2)
            tool.window.destroy()

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'requires a display')
    def test_lora_editor_save_and_generation_toggle(self):
        import tkinter as tk
        from image_tool_gui import ContentImageToolWindow
        from lora_settings import LoraSettings
        root = tk.Tk()
        root.withdraw()
        self.addCleanup(root.destroy)
        with tempfile.TemporaryDirectory() as directory:
            settings = LoraSettings(Path(directory) / 'loras.json')
            tool = ContentImageToolWindow(root, lambda: {'cat-a': 'ミケ'},
                registry=ImageRegistry(Path(directory) / 'images'), lora_settings=settings)
            root.update()
            tool.open_lora_editor()
            editor = tool.lora_editor
            with patch('lora_editor_gui.ComfyClient.list_loras', return_value=['design.safetensors', 'pose.safetensors']):
                editor.fetch()
                deadline = time.monotonic() + 5
                while editor.timer is not None and time.monotonic() < deadline:
                    root.update()
                    time.sleep(0.01)
            self.assertIn('design.safetensors', editor.selectors[0].cget('values'))
            editor.rows[0]['enabled'].set(True)
            editor.rows[0]['name'].set('design.safetensors')
            editor.rows[0]['trigger'].set('mikecat')
            editor.save()
            self.assertTrue(settings.path.exists())
            editor.window.destroy()
            tool.open_generation()
            generation = tool.generation_window
            self.assertTrue(generation.use_loras.get())
            self.assertEqual(generation.loras[0]['name'], 'design.safetensors')
            captured = []
            def generate(client, values, *args, **kwargs):
                captured.append(values)
                return ASSET
            with patch('comfy_generation_gui.ComfyClient.generate', new=generate):
                for enabled in (True, False):
                    generation.use_loras.set(enabled)
                    generation.start()
                    deadline = time.monotonic() + 5
                    while generation.running and time.monotonic() < deadline:
                        root.update()
                        time.sleep(0.01)
            self.assertEqual(captured[0]['loras'][0]['trigger'], 'mikecat')
            self.assertEqual(captured[1]['loras'], [])
            generation.close()
            tool.window.destroy()
            root.update()

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

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'requires a display')
    def test_generation_returns_unregistered_candidate(self):
        import tkinter as tk
        from image_tool_gui import ContentImageToolWindow
        root = tk.Tk()
        root.withdraw()
        self.addCleanup(root.destroy)
        with tempfile.TemporaryDirectory() as directory:
            registry = ImageRegistry(directory)
            tool = ContentImageToolWindow(root, lambda: {'cat-1': 'ミケ'}, registry=registry)
            tool.open_generation()
            generation = tool.generation_window
            generation.fields['seed'].set('-1')
            generation.start()
            self.assertFalse(generation.running)
            generation.fields['seed'].set('42')
            with patch('comfy_generation_gui.ComfyClient.generate', return_value=ASSET):
                generation.start()
                deadline = time.monotonic() + 5
                while generation.running and time.monotonic() < deadline:
                    root.update()
                    time.sleep(0.01)
            self.assertFalse(generation.running)
            self.assertEqual(tool.pending, ASSET)
            self.assertFalse(registry.path.exists())
            generation.close()
            self.assertIsNone(generation.timer)
            tool.window.destroy()

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'requires a display')
    def test_saved_parts_fill_generation_for_selected_cat_and_scene(self):
        import tkinter as tk
        from image_tool_gui import ContentImageToolWindow
        from prompt_presets import PromptPresets
        root = tk.Tk()
        root.withdraw()
        self.addCleanup(root.destroy)
        with tempfile.TemporaryDirectory() as directory:
            presets = PromptPresets(Path(directory) / 'prompts.json')
            tool = ContentImageToolWindow(root, lambda: {'cat-a': 'ミケ'},
                registry=ImageRegistry(Path(directory) / 'images'), presets=presets)
            root.update()

            tool.open_prompt_editor()
            editor = tool.prompt_editor
            editor.fields['appearance'].delete('1.0', 'end')
            editor.fields['appearance'].insert('1.0', 'one black cat with amber eyes')
            editor.save()
            self.assertTrue(presets.path.exists())
            editor.window.destroy()
            tool.open_generation()
            generation = tool.generation_window
            self.assertIn('one black cat with amber eyes', generation.positive.get('1.0', 'end'))
            self.assertIn('sitting calmly', generation.positive.get('1.0', 'end'))
            generation.positive.delete('1.0', 'end')
            generation.positive.insert('1.0', 'temporary edit')
            with patch('tkinter.messagebox.askyesno', return_value=True):
                generation.reload_prompt()
            self.assertIn('one black cat', generation.positive.get('1.0', 'end'))
            tool.scene.set('眠る')
            tool.refresh()
            tool.open_generation()
            self.assertIn('asleep', tool.generation_window.positive.get('1.0', 'end'))
            self.assertIn('one black cat', tool.generation_window.positive.get('1.0', 'end'))
            tool.generation_window.close()
            tool.window.destroy()
            root.update()

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'requires a display')
    def test_reference_is_per_cat_and_used_in_generation(self):
        import tkinter as tk
        from image_tool_gui import ContentImageToolWindow
        root = tk.Tk()
        root.withdraw()
        self.addCleanup(root.destroy)
        with tempfile.TemporaryDirectory() as directory:
            references = ImageRegistry(Path(directory) / 'references')
            tool = ContentImageToolWindow(root, lambda: {'cat-a': 'ミケ', 'cat-b': 'タマ'},
                registry=ImageRegistry(Path(directory) / 'images'), references=references)
            root.update()
            tool.set_reference(ASSET)
            self.assertTrue(references.image_path('cat-a', 'normal').exists())
            self.assertIsNotNone(tool.reference_image)
            tool.scene.set('眠る')
            tool.refresh()
            tool.open_generation()
            generation = tool.generation_window
            self.assertTrue(generation.use_reference.get())
            self.assertEqual(generation.reference_path, references.image_path('cat-a', 'normal'))
            captured = []
            def generate(client, settings, *args, **kwargs):
                captured.append(settings)
                return ASSET
            with patch('comfy_generation_gui.ComfyClient.generate', new=generate):
                generation.start()
                deadline = time.monotonic() + 5
                while generation.running and time.monotonic() < deadline:
                    root.update()
                    time.sleep(0.01)
            self.assertEqual(captured[0]['denoise'], 0.65)
            self.assertIn('asleep', captured[0]['positive'])
            self.assertIsNotNone(tool.pending)
            self.assertFalse(tool.registry.path.exists())
            generation.close()
            tool.cat.set(next(label for label, key in tool.cats.items() if key == 'cat-b'))
            tool.refresh()
            self.assertIsNone(tool.reference_image)
            tool.window.destroy()
            root.update()
