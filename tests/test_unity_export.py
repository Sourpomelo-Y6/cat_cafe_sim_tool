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

    def test_interaction_images_are_optional_and_exported(self):
        from prompt_presets import PromptPresets, DEFAULTS
        from interaction_scenes import ACTION_PROMPTS
        from lora_settings import LoraSettings, EMPTY
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            registry = ImageRegistry(root / 'source')
            registry.register('a', 'ball', ASSET)
            folder, _ = export_bundle({'a': 'ミケ'}, registry, TextStore(root / 'texts.json'), root / 'exports')
            scenes = json.loads((folder / 'bundle.json').read_text(encoding='utf-8'))['cats'][0]['scenes']
            self.assertEqual(len(scenes), 5)
            self.assertEqual(scenes[-1]['key'], 'ball')
            self.assertTrue((folder / scenes[-1]['image']).is_file())
            old = dict(DEFAULTS, actions={k: v for k, v in DEFAULTS['actions'].items() if k not in ACTION_PROMPTS})
            path = root / 'presets.json'
            path.write_text(json.dumps(old), encoding='utf-8')
            presets = PromptPresets(path)
            self.assertIn('small ball', presets.assemble('a', 'ball'))
            self.assertEqual(json.loads(path.read_text(encoding='utf-8')), old)
            lora = LoraSettings(root / 'lora.json')
            lora.save('a', 'ball', EMPTY, EMPTY)
            self.assertIn('ball', lora.read()['scenes'])

    def test_combination_registration_export_and_generation(self):
        from interaction_scenes import MODE_ACTIONS, COMBINATION_SCENES
        from prompt_presets import PromptPresets
        from lora_settings import LoraSettings, EMPTY
        config = json.loads((Path(__file__).resolve().parents[2] / 'cat_cafe_sim/config/human_cat_types.json').read_text(encoding='utf-8'))
        self.assertEqual(len(COMBINATION_SCENES), 47)
        for row in config['types']:
            self.assertEqual(set(MODE_ACTIONS[row['id']]), set(row['actions']) | {'pause', 'switch', 'connect'})
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            registry = ImageRegistry(root / 'source')
            registry.register('a', 'ball__feint', ASSET)
            registry.register('a', 'brush__adapt', ASSET)
            with self.assertRaises(ValueError):
                registry.register('a', 'brush__intense', ASSET)
            folder, report = export_bundle({'a': 'ミケ'}, registry, TextStore(root / 'texts.json'), root / 'exports')
            scenes = json.loads((folder / 'bundle.json').read_text(encoding='utf-8'))['cats'][0]['scenes']
            self.assertEqual({row['key'] for row in scenes}, {'normal','play','pet','sleep','ball__feint','brush__adapt'})
            self.assertEqual(report['image_count'], 1)
            presets = PromptPresets(root / 'prompts.json')
            fields = presets.fields('a', 'ball__feint')
            self.assertIn('change of direction', fields['action'])
            presets.save('a', 'ball__feint', fields)
            self.assertEqual(PromptPresets(presets.path).fields('a', 'ball__feint'), fields)
            settings = LoraSettings(root / 'lora.json')
            settings.save('a', 'ball__feint', EMPTY, EMPTY)
            self.assertIn('ball__feint', settings.read()['scenes'])

    def test_mode_special_reaction_images(self):
        from prompt_presets import PromptPresets
        from lora_settings import LoraSettings, EMPTY
        from interaction_scenes import SPECIAL_SCENES
        self.assertEqual(len(SPECIAL_SCENES), 16)
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            registry = ImageRegistry(root / 'source')
            for key in ('ball__open_up', 'ball__simultaneous'):
                registry.register('a', key, ASSET)
                self.assertIn('trust', PromptPresets(root / 'presets.json').assemble('a', key))
                settings = LoraSettings(root / 'loras.json')
                settings.save('a', key, EMPTY, EMPTY)
            folder, _ = export_bundle({'a':'ミケ'}, registry, TextStore(root / 'texts.json'), root / 'exports')
            scenes = json.loads((folder / 'bundle.json').read_text(encoding='utf-8'))['cats'][0]['scenes']
            self.assertEqual({row['key'] for row in scenes}, {'normal','play','pet','sleep','ball__open_up','ball__simultaneous'})

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
