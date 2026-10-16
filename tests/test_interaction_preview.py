import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from content_images import ImageRegistry
from content_texts import TextStore
from interaction_texts import InteractionTextStore, condition, make_entry
from interaction_presentation import (InteractionImageBindings, resolve_image, select_entry, coverage,
                                      build_interaction_catalog, expected_conditions)
from unity_export import export_bundle

ASSET = Path(__file__).resolve().parents[2] / 'Docs/Prototypes/CalicoActionBackgroundPreview-v1.png'


class PreviewDataTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.registry = ImageRegistry(self.root / 'source')
        self.published = InteractionTextStore(self.registry.directory / 'interaction_texts.json', published=True)
        self.catalog = {'playtest-mike': 'ミケ'}

    def save(self, value, text):
        entry = make_entry(value, [text, '', ''])
        self.published.save(entry)
        return entry

    def export(self):
        return export_bundle(self.catalog, self.registry, TextStore(self.registry.directory / 'texts.json'),
                             self.root / 'exports', profiles={})

    def test_specific_common_reaction_and_overlay_are_kept_separate(self):
        common = self.save(condition(), '共通セリフ')
        specific = self.save(condition(action='adapt'), '専用セリフ')
        entries = self.published.read()['entries']
        self.assertEqual(select_entry(entries, condition(action='adapt'))['candidates'][0]['text'], '専用セリフ')
        self.assertEqual(select_entry(entries, condition(action='direct'))['match'], '操作共通')
        cat = condition('cat_response', 'cat-mike', action='direct', reaction='listless')
        self.assertFalse(select_entry(entries, cat)['candidates'])
        overlay = make_entry(condition(action='adapt'), ['編集案', '', ''])
        self.assertEqual(select_entry(entries, condition(action='adapt'), overlay=overlay)['source'], '編集中・未登録')
        self.assertEqual(select_entry(entries, condition(action='adapt'))['candidates'], specific['variants'][:1])
        self.assertEqual(common['condition']['subject_id'], 'player')

    def test_export_published_only_stable_ids_and_image_dedup(self):
        self.registry.register('playtest-mike', 'normal', ASSET)
        self.registry.register('playtest-mike', 'play', ASSET)
        entry = make_entry(condition(), ['', '採用した2案目', '採用した3案目'])
        self.published.save(entry)
        draft = InteractionTextStore(self.root / 'draft.json')
        draft.save(make_entry(condition(subject_id='guest-1'), ['未採用の下書き', '', '']))
        folder, report = self.export()
        exported = json.loads((folder / 'interaction_texts.json').read_text(encoding='utf-8'))
        self.assertEqual(len(exported['entries']), 1)
        self.assertEqual(exported['pack_id'], json.loads((folder / 'bundle.json').read_text(encoding='utf-8'))['pack_id'])
        self.assertEqual(exported['entries'][0]['variants'], entry['variants'][1:])
        self.assertNotIn('未採用', json.dumps(exported, ensure_ascii=False))
        self.assertEqual(report['image_count'], 1)
        self.assertEqual(report['interactions']['candidate_count'], 2)
        for row in exported['image_bindings']:
            if row['image']:
                self.assertTrue((folder / row['image']).is_file())
        self.assertEqual(len(exported['revision']), 64)

    def test_revision_is_content_based_and_previous_pack_is_unchanged(self):
        self.save(condition(), '最初の本文')
        first, _ = self.export()
        old = (first / 'interaction_texts.json').read_bytes()
        second, _ = self.export()
        a, b = json.loads(old), json.loads((second / 'interaction_texts.json').read_bytes())
        self.assertEqual(a['revision'], b['revision'])
        self.save(condition(), '変更後の本文')
        third, _ = self.export()
        self.assertNotEqual(a['revision'], json.loads((third / 'interaction_texts.json').read_bytes())['revision'])
        self.assertEqual((first / 'interaction_texts.json').read_bytes(), old)

    def test_missing_coverage_counts_actual_operations_with_common_fallback(self):
        self.save(condition(), '人の共通文')
        self.save(condition('cat_response', 'cat-mike', reaction='neutral'), '猫の通常文')
        self.save(condition('cat_response', 'cat-mike', action='adapt', reaction='favorable'), '猫の好反応文')
        report = coverage(self.published.read()['entries'])
        self.assertEqual((report['expected_count'], report['covered_count'], report['common_count']), (188, 9, 8))
        self.assertTrue(any(v['subject_id'] == 'guest-1' for v in report['missing_conditions']))
        self.assertTrue(any(v['reaction'] == 'listless' for v in report['missing_conditions']))
        for value in expected_conditions():
            self.save(value, 'この条件の専用文')
        self.assertEqual(coverage(self.published.read()['entries'])['missing_count'], 0)

    def test_id_binding_is_explicit_and_never_guessed_from_name(self):
        self.catalog = {'custom-id': 'ミケ'}
        self.registry.register('custom-id', 'normal', ASSET)
        result, _, _ = build_interaction_catalog(self.catalog, self.registry)
        self.assertEqual(result['cat_bindings'][0]['content_cat_id'], '')
        self.assertTrue(all(not row['image'] for row in result['image_bindings']))
        bindings = InteractionImageBindings(self.registry.directory / 'interaction_image_bindings.json')
        bindings.save('custom-id', self.catalog)
        result, _, _ = build_interaction_catalog(self.catalog, self.registry)
        self.assertEqual(result['cat_bindings'][0]['content_cat_id'], 'custom-id')
        self.assertTrue(all(row['image'] for row in result['image_bindings']))

    def test_bad_or_missing_images_fall_back_and_rest_does_not_assume_sleep(self):
        self.registry.register('playtest-mike', 'normal', ASSET)
        broken = self.registry.register('playtest-mike', 'teaser__adapt', ASSET)
        broken.write_bytes(b'broken')
        self.registry.register('playtest-mike', 'play', ASSET)
        image = resolve_image(self.registry, 'playtest-mike', 'teaser', 'adapt')
        self.assertEqual(image['resolved_scene_key'], 'play')
        self.assertTrue(any('読み込めません' in s for s in image['warnings']))
        self.registry.register('playtest-mike', 'sleep', ASSET)
        self.assertEqual(resolve_image(self.registry, 'playtest-mike', 'pause', 'pause')['resolved_scene_key'], 'normal')

    def test_bad_registered_source_or_mapping_produces_no_output(self):
        self.published.path.parent.mkdir(parents=True)
        self.published.path.write_text('{broken', encoding='utf-8')
        with self.assertRaises(ValueError):
            self.export()
        self.assertFalse((self.root / 'exports').exists())
        self.published.path.unlink()
        binding = self.registry.directory / 'interaction_image_bindings.json'
        binding.write_text('{broken', encoding='utf-8')
        with self.assertRaises(ValueError):
            self.export()
        self.assertFalse((self.root / 'exports').exists())
        self.assertEqual(binding.read_text(encoding='utf-8'), '{broken')

    def test_draft_store_is_not_an_export_source(self):
        with self.assertRaises(ValueError):
            build_interaction_catalog(self.catalog, self.registry,
                                      store=InteractionTextStore(self.root / 'draft.json'))

    def test_failed_sidecar_write_removes_only_the_new_pack(self):
        first, _ = self.export()
        original = Path.write_text
        def fail(path, *args, **kwargs):
            if path.name == 'interaction_texts.json':
                raise OSError('disk failure')
            return original(path, *args, **kwargs)
        with patch.object(Path, 'write_text', new=fail), self.assertRaises(OSError):
            self.export()
        self.assertEqual(list((self.root / 'exports').iterdir()), [first])
        self.assertTrue((first / 'bundle.json').is_file())


@unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'requires a display')
class PreviewGuiTests(unittest.TestCase):
    def setUp(self):
        import tkinter as tk
        from interaction_text_editor_gui import InteractionTextEditorWindow
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = tk.Tk()
        self.root.withdraw()
        self.addCleanup(self.root.destroy)
        self.registry = ImageRegistry(Path(self.temp.name) / 'source')
        self.registry.register('playtest-mike', 'play', ASSET)
        self.catalog = {'playtest-mike': 'ミケ'}
        self.published = InteractionTextStore(self.registry.directory / 'interaction_texts.json', published=True)
        self.published.save(make_entry(condition(), ['採用セリフ1', '採用セリフ2', '採用セリフ3']))
        self.published.save(make_entry(condition('cat_response', 'cat-mike', reaction='neutral'),
                                       ['通常反応1', '通常反応2', '通常反応3']))
        self.editor = InteractionTextEditorWindow(self.root, InteractionTextStore(Path(self.temp.name) / 'drafts.json'),
                          self.published, image_registry=self.registry, catalog_provider=lambda: self.catalog)
        self.root.update()

    def open(self):
        self.editor.open_preview()
        p = self.editor.preview_window
        self.root.update()
        return p

    def test_overlay_is_labeled_and_never_published_by_reading(self):
        e = self.editor
        e.fields[0].delete('1.0', 'end')
        e.fields[0].insert('1.0', '編集中の未採用文')
        before = self.published.path.read_bytes()
        image_before = self.registry.path.read_bytes()
        p = self.open()
        self.assertIn('編集中の未採用文', p.body.get('1.0', 'end'))
        self.assertIn('編集中・未登録', p.source_notice.get())
        p.use_edit.set(False)
        p.render()
        self.assertNotIn('編集中の未採用文', p.body.get('1.0', 'end'))
        self.assertIn('採用セリフ1', p.body.get('1.0', 'end'))
        p.variables['speech'].set('案3')
        p.variables['cat'].set('案2')
        for _ in range(10):
            p.render()
        self.assertIn('採用セリフ3', p.body.get('1.0', 'end'))
        self.assertIn('通常反応2', p.body.get('1.0', 'end'))
        self.assertEqual(self.published.path.read_bytes(), before)
        self.assertEqual(self.registry.path.read_bytes(), image_before)
        self.assertFalse(e.drafts.path.exists())
        self.assertEqual(p.body.cget('state'), 'disabled')

    def test_missing_reactions_and_image_fallback_are_explicit(self):
        p = self.open()
        self.assertIsNotNone(p.image)
        self.assertIn('teaser__direct → play', p.image_notice.get())
        p.variables['reaction'].set('低体力')
        p.render()
        self.assertIn('未制作：ミケの反応', p.body.get('1.0', 'end'))
        self.assertNotIn('通常反応1', p.body.get('1.0', 'end'))
        self.assertIn('未制作', p.missing_text.get('1.0', 'end'))
        p.variables['mode'].set('休憩')
        p.render()
        self.assertNotIn('好意的', p.selectors['reaction'].cget('values'))

    def test_id_mapping_can_be_selected_and_saved_without_modifying_texts(self):
        from interaction_preview_gui import InteractionPreviewWindow
        p = InteractionPreviewWindow(self.editor.window, {'other': '登録したミケ'}, self.registry, self.published)
        self.registry.register('other', 'normal', ASSET)
        before = self.published.path.read_bytes()
        p.content.set('登録したミケ（other）')
        p.render()
        self.assertIn('一時選択', p.image_notice.get())
        p.save_binding()
        self.assertEqual(p.bindings.get({'other': '登録したミケ'}), 'other')
        self.assertEqual(self.published.path.read_bytes(), before)

    def test_failed_reload_keeps_preview_and_does_not_overwrite_corrupt_file(self):
        p = self.open()
        before = p.body.get('1.0', 'end')
        self.published.path.write_text('{broken', encoding='utf-8')
        p.reload()
        self.assertEqual(p.body.get('1.0', 'end'), before)
        self.assertIn('再読込できません', p.notice.get())
        self.assertEqual(self.published.path.read_text(encoding='utf-8'), '{broken')

    def test_geometry_and_export_button_include_sidecar_and_missing_report(self):
        from image_tool_gui import ContentImageToolWindow
        p = self.open()
        p.window.geometry('840x680+0+0')
        self.root.update()
        for widget in [p.picture, p.body]:
            self.assertGreater(widget.winfo_height(), 10)
            self.assertLessEqual(widget.winfo_rootx() + widget.winfo_width(), p.window.winfo_rootx() + p.window.winfo_width())
            self.assertLessEqual(widget.winfo_rooty() + widget.winfo_height(), p.window.winfo_rooty() + p.window.winfo_height())
        tool = ContentImageToolWindow(self.root, lambda: self.catalog, registry=self.registry)
        with patch('tkinter.filedialog.askdirectory', return_value=self.temp.name):
            tool.export_unity()
        folder = next(Path(self.temp.name).glob('CatCafeContent-*'))
        data = json.loads((folder / 'interaction_texts.json').read_text(encoding='utf-8'))
        self.assertEqual(len(data['entries']), 2)
        self.assertIn('交流文6案', tool.notice.get())
        report = json.loads((folder / 'validation.json').read_text(encoding='utf-8'))
        self.assertGreater(report['interactions']['missing_count'], 0)


if __name__ == '__main__':
    unittest.main()
