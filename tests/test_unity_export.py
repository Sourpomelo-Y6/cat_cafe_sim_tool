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

    def test_published_scene_metadata_survives_export(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            registry = ImageRegistry(root / 'source')
            registry.register('a', 'ball', ASSET)
            texts = TextStore(registry.directory / 'texts.json')
            texts.save('a', {
                'introduction': '本番の紹介文',
                'scenes': {},
                'extra_scene_texts': {'ball': '本番の場面説明'},
                'scene_labels': {'ball': '本番の表示名'},
                'scene_order': ['ball', 'sleep'],
            })
            folder, _ = export_bundle({'a': 'ミケ'}, registry, texts, root / 'exports')
            cat, = json.loads((folder / 'bundle.json').read_text(encoding='utf-8'))['cats']
            self.assertEqual(cat['introduction'], '本番の紹介文')
            self.assertEqual([scene['key'] for scene in cat['scenes']], ['ball', 'sleep', 'normal', 'play', 'pet'])
            self.assertEqual(cat['scenes'][0]['label'], '本番の表示名')
            self.assertEqual(cat['scenes'][0]['text'], '本番の場面説明')

    def test_production_comparison_is_read_only_and_detects_missing_material(self):
        from optional_events import OptionalEventStore
        from production_comparison import compare_production
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            registry = ImageRegistry(root / 'source')
            texts = TextStore(registry.directory / 'texts.json')
            image = registry.register('playtest-mike', 'ball', ASSET)
            catalog = {'playtest-mike': 'ミケ'}
            texts.save('playtest-mike', {
                'introduction': '本番の紹介文', 'scenes': {'play': '本番の遊びの説明'},
            })
            folder, _ = export_bundle(catalog, registry, texts, root / 'exports')
            baseline = folder / 'bundle.json'
            self.assertFalse(compare_production(baseline, catalog, registry, texts)['changes'])
            texts.save('playtest-mike', {'introduction': '', 'scenes': {'play': '新しい説明'}})
            image.unlink()
            events = OptionalEventStore(registry.directory / 'optional_events.json')
            data = events.read_all()
            data['entries'][0]['enabled'] = False
            events.save_all(data)
            before = {p: p.read_bytes() for p in root.rglob('*') if p.is_file()}
            diff = compare_production(baseline, catalog, registry, texts)
            self.assertEqual(diff['events'], (1, 0))
            changes = {(row['field'], row['kind']) for row in diff['changes']}
            self.assertEqual(changes, {('紹介文', '欠落'), ('説明文', '変更'),
                                       ('画像参照', '欠落'), ('イベント', '欠落')})
            self.assertEqual(before, {p: p.read_bytes() for p in root.rglob('*') if p.is_file()})

    def test_comparison_finds_dispatch_toggles_and_published_narrative_changes(self):
        from interaction_subjects import SubjectRegistry, game_subjects
        from interaction_texts import InteractionTextStore, condition, make_entry
        from production_comparison import compare_production
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            registry = ImageRegistry(root / 'source')
            texts = TextStore(registry.directory / 'texts.json')
            subjects = SubjectRegistry(registry.directory / 'interaction_subjects.json')
            sato = subjects.get('guest-1')
            sato['named_dispatch'] = {'enabled': True, 'scenes': {'request': ['{cat}ちゃんにお願いします。', '']}}
            subjects.save(sato)
            original = InteractionTextStore(registry.directory / 'interaction_texts.json', published=True, subject_registry=subjects)
            speech = condition(target_cat='*')
            removed = condition(mode='pause', action='pause')
            original.save_many([make_entry(speech, ['おいで。', '一緒に遊ぼう。', '']),
                                make_entry(removed, ['休もうね。', '', ''])])
            catalog = {'playtest-mike': 'ミケ'}
            folder, _ = export_bundle(catalog, registry, texts, root / 'exports', interaction_texts=original)
            baseline = folder / 'bundle.json'
            self.assertEqual(compare_production(baseline, catalog, registry, texts)['changes'], [])
            sato.pop('named_dispatch')
            subjects.save(sato)
            third = dict(sato, id='guest-3', **game_subjects()['speech']['guest-3'])
            third['named_dispatch'] = {'enabled': True, 'scenes': {}}
            subjects.save(third)
            candidate = InteractionTextStore(root / 'other-published.json', published=True, subject_registry=subjects)
            candidate.save_many([make_entry(speech, ['ここにおいで。', '', '']),
                                 make_entry(condition(mode='brush'), ['ブラシをしよう。', '', ''])])
            with patch('interaction_presentation.resolve_image', side_effect=AssertionError('unrelated image lookup')):
                report = compare_production(baseline, catalog, registry, texts, interaction_texts=candidate)
            self.assertEqual(report['interactions'], (2, 2))
            dispatch = [row for row in report['changes'] if row['item'].startswith('指名派遣')]
            self.assertTrue(any(row['kind'] == '欠落' and '最初の依頼' in row['field']
                                and 'guest-1' in row['item'] for row in dispatch))
            self.assertTrue(any(row['field'] == '有効・無効' and row['old'] == '無効' and row['new'] == '有効'
                                and 'guest-3' in row['item'] for row in dispatch))
            self.assertFalse(any(row['field'] == '有効・無効' and 'guest-1' in row['item'] for row in dispatch))
            speech_changes = {(row['field'], row['kind']) for row in report['changes'] if row['item'].startswith('交流文')}
            self.assertEqual(speech_changes, {('条件', '追加'), ('条件', '欠落'), ('候補1', '変更'), ('候補2', '欠落')})

    def test_reviewed_export_rejects_a_changed_baseline_before_creating_files(self):
        from functools import partial
        from production_comparison import StaleProductionReview, compare_production, verify_review
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            registry = ImageRegistry(root / 'source')
            texts = TextStore(registry.directory / 'texts.json')
            catalog = {'a': 'ミケ'}
            live, _ = export_bundle(catalog, registry, texts, root / 'baseline')
            baseline = live / 'bundle.json'
            reviewed = compare_production(baseline, catalog, registry, texts)
            data = json.loads(baseline.read_text(encoding='utf-8'))
            data['cats'][0]['introduction'] = '新しい本番文'
            baseline.write_text(json.dumps(data), encoding='utf-8')
            destination = root / 'out'
            with self.assertRaises(StaleProductionReview):
                export_bundle(catalog, registry, texts, destination, verify_candidate=partial(verify_review, baseline, reviewed))
            self.assertFalse(destination.exists())

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
            with patch('tkinter.filedialog.askopenfilename', return_value=str(folders[0] / 'bundle.json')):
                tool.compare_unity()
            review, = [w for w in tool.window.winfo_children()
                       if w.winfo_class() == 'Toplevel' and w.title() == '本番パックとの照合結果']

            def descendants(widget):
                for child in widget.winfo_children():
                    yield child
                    yield from descendants(child)

            buttons = {button['text']: button for button in descendants(review) if button.winfo_class() == 'TButton'}
            tool.published_texts.save('a', {'introduction': '再照合で確認する新しい紹介文', 'scenes': {}})
            destination = Path(directory) / 'reviewed'
            approved = buttons['この内容で書き出す…']
            with patch('tkinter.filedialog.askdirectory', return_value=str(destination)):
                approved.invoke()
                self.assertFalse(destination.exists())
                self.assertTrue(approved.instate(['disabled']))
                buttons['再照合'].invoke()
                self.assertFalse(approved.instate(['disabled']))
                approved.invoke()
            self.assertFalse(review.winfo_exists())
            written, = destination.glob('CatCafeContent-*/bundle.json')
            self.assertEqual(json.loads(written.read_text(encoding='utf-8'))['cats'][0]['introduction'],
                             '再照合で確認する新しい紹介文')
            tool.window.destroy()
            root.update()
