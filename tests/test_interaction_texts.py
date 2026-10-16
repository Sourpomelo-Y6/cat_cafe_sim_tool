import copy
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from interaction_texts import InteractionTextStore, condition, make_entry, validate_entry


class InteractionTextTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name) / 'draft.json'
        self.store = InteractionTextStore(self.path)

    def test_condition_isolation_and_reopen(self):
        conditions = [condition(), condition(subject_id='guest-1'), condition(mode='pet'),
                      condition('cat_response', 'cat-mike', reaction='favorable'),
                      condition('cat_response', 'cat-mike', reaction='listless'),
                      condition('cat_response', 'cat-mike', action='adapt', reaction='favorable'),
                      condition('cat_response', 'cat-mike', mode='pause', action='pause', reaction='listless')]
        for i, value in enumerate(conditions):
            self.store.save(make_entry(value, [str(i), '', '']))
        reopened = InteractionTextStore(self.path)
        for i, value in enumerate(conditions):
            self.assertEqual(reopened.get(value)['variants'][0]['text'], str(i))
        self.assertEqual(len(reopened.read()['entries']), len(conditions))

    def test_invalid_conditions(self):
        for kwargs in [dict(subject_id='unknown'), dict(kind='cat_response', subject_id='player'),
                       dict(mode='pet', action='intense'), dict(mode='unknown'),
                       dict(mode='pause', action='pause', reaction='favorable'),
                       dict(reaction='favorable')]:
            with self.subTest(kwargs=kwargs), self.assertRaises(ValueError):
                condition(**kwargs)
        bad = condition()
        bad['scene_key'] = 'pet'
        with self.assertRaises(ValueError):
            make_entry(bad, ['', '', ''])

    def test_three_slots_stable_ids_and_partial_draft(self):
        old = make_entry(condition(), ['一', '', '三'])
        new = make_entry(condition(), ['変更', '', '別案'])
        self.assertEqual([r['id'] for r in old['variants']], [r['id'] for r in new['variants']])
        self.assertEqual([r['slot'] for r in new['variants']], [1, 2, 3])
        self.store.save(old)
        self.assertEqual(self.store.get(condition()), old)
        for texts in [[], ['1', '2', '3', '4'], ['重複', '重複', ''], ['あ' * 201, '', ''],
                      ['a\x00', '', ''], [1, '', '']]:
            with self.subTest(texts=texts), self.assertRaises(ValueError):
                make_entry(condition(), texts)

    def test_published_requires_one_nonempty_candidate(self):
        published = InteractionTextStore(Path(self.temp.name) / 'published.json', published=True)
        blank = make_entry(condition(), ['', '', ''])
        self.store.save(blank)
        with self.assertRaises(ValueError):
            published.save(blank)
        entry = make_entry(condition(), ['', '遊ぼうか。', ''])
        published.save(entry)
        self.assertEqual(published.get(condition()), entry)

    def test_corrupt_file_is_not_overwritten(self):
        entry = make_entry(condition(), ['案', '', ''])
        for data in ['{broken', '{"version":2,"entries":[]}',
                     json.dumps({'version': 1, 'entries': [entry, entry]})]:
            self.path.write_text(data, encoding='utf-8')
            with self.assertRaises(ValueError):
                self.store.save(entry)
            self.assertEqual(self.path.read_text(encoding='utf-8'), data)

    def test_bad_slot_id_order_and_type_are_rejected(self):
        entry = make_entry(condition(), ['案', '', ''])
        for field, value in [('id', 'unknown'), ('slot', True), ('slot', 2), ('text', None)]:
            bad = copy.deepcopy(entry)
            bad['variants'][0][field] = value
            with self.subTest(field=field, value=value), self.assertRaises(ValueError):
                validate_entry(bad)

    def test_concurrent_conditions_merge_but_stale_same_condition_is_rejected(self):
        entry = make_entry(condition(), ['元', '', ''])
        self.store.save(entry)
        snapshot = self.store.get(condition())
        other = InteractionTextStore(self.path)
        other.save(make_entry(condition(subject_id='guest-1'), ['客', '', '']))
        changed = make_entry(condition(), ['変更', '', ''])
        self.store.save(changed, expected=snapshot)
        self.assertIsNotNone(self.store.get(condition(subject_id='guest-1')))
        with self.assertRaises(ValueError):
            other.save(entry, expected=snapshot)
        self.assertEqual(self.store.get(condition()), changed)

    def test_failed_atomic_replace_preserves_file_and_removes_temp(self):
        self.store.save(make_entry(condition(), ['元', '', '']))
        before = self.path.read_bytes()
        with patch.object(Path, 'replace', side_effect=OSError('disk failure')):
            with self.assertRaises(OSError):
                self.store.save(make_entry(condition(), ['変更', '', '']))
        self.assertEqual(self.path.read_bytes(), before)
        self.assertFalse(list(self.path.parent.glob('*.tmp')))


@unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'requires a display')
class InteractionEditorTests(unittest.TestCase):
    def setUp(self):
        import tkinter as tk
        from interaction_text_editor_gui import InteractionTextEditorWindow
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = tk.Tk()
        self.root.withdraw()
        self.addCleanup(self.root.destroy)
        self.drafts = InteractionTextStore(Path(self.temp.name) / 'draft.json')
        self.published = InteractionTextStore(Path(self.temp.name) / 'published.json', published=True)
        self.editor = InteractionTextEditorWindow(self.root, self.drafts, self.published)
        self.editor.load_condition(condition())
        self.root.update()

    def fill(self, texts):
        for field, text in zip(self.editor.fields, texts):
            field.delete('1.0', 'end')
            field.insert('1.0', text)
        self.root.update()

    def test_review_edit_register_and_reopen_keeps_latest(self):
        from interaction_text_editor_gui import InteractionTextEditorWindow
        e = self.editor
        self.fill(['遊ぼうか。', 'こっちだよ。', '気になる？'])
        self.assertTrue(e.save_draft())
        self.assertFalse(self.published.path.exists())
        self.assertFalse(e.register())
        e.preview()
        self.fill(['一緒に遊ぼう。', 'こっちだよ。', '気になる？'])
        self.assertFalse(e.register())
        e.preview()
        self.assertTrue(e.register())
        expected = e.values()
        e.close()
        self.editor = InteractionTextEditorWindow(self.root, self.drafts, self.published)
        self.editor.load_condition(condition())
        self.assertEqual(self.editor.values(), expected)
        self.assertEqual(self.published.get(condition()), expected)

    def test_switch_cancel_save_and_reaction_isolation(self):
        e = self.editor
        self.fill(['未保存のプレイヤー文', '', ''])
        e.choices['subject_id'].set('佐藤さん')
        with patch('tkinter.messagebox.askyesnocancel', return_value=None):
            e.selection_changed()
        self.assertEqual(e.choices['subject_id'].get(), 'プレイヤー')
        self.assertEqual(e.values()['variants'][0]['text'], '未保存のプレイヤー文')
        with patch('tkinter.messagebox.askyesnocancel', return_value=True):
            self.assertTrue(e.load_condition(condition('cat_response', 'cat-mike', reaction='favorable')))
        self.assertEqual(self.drafts.get(condition())['variants'][0]['text'], '未保存のプレイヤー文')
        self.fill(['ミケは目を細めた。', '', ''])
        e.save_draft()
        e.load_condition(condition('cat_response', 'cat-mike', reaction='listless'))
        self.assertEqual(e.values()['variants'][0]['text'], '')
        e.load_condition(condition('cat_response', 'cat-mike', reaction='favorable'))
        self.assertIn('目を細め', e.values()['variants'][0]['text'])

    def test_close_cancel_and_reload_discard(self):
        e = self.editor
        self.fill(['下書き', '', ''])
        e.save_draft()
        self.fill(['変更', '', ''])
        with patch('tkinter.messagebox.askyesnocancel', return_value=None):
            e.close()
        self.assertTrue(e.window.winfo_exists())
        with patch('tkinter.messagebox.askyesnocancel', return_value=False):
            e.reload()
        self.assertEqual(e.values()['variants'][0]['text'], '下書き')

    def test_corruption_during_edit_preserves_text(self):
        e = self.editor
        self.fill(['消してはいけない文章', '', ''])
        self.drafts.path.write_text('{broken', encoding='utf-8')
        self.assertFalse(e.save_draft())
        self.assertFalse(e.reload())
        self.assertEqual(e.values()['variants'][0]['text'], '消してはいけない文章')
        self.assertEqual(self.drafts.path.read_text(encoding='utf-8'), '{broken')

    def test_published_fallback_empty_rejected_and_other_editor_update_detected(self):
        e = self.editor
        e.preview()
        self.assertIsNone(e.reviewed)
        self.assertFalse(e.register())
        self.published.save(make_entry(condition(), ['登録済み', '', '']))
        e.reload()
        self.assertEqual(e.values()['variants'][0]['text'], '登録済み')
        self.fill(['自分の編集', '', ''])
        self.drafts.save(make_entry(condition(), ['別画面の編集', '', '']))
        self.assertFalse(e.save_draft())
        self.assertEqual(e.values()['variants'][0]['text'], '自分の編集')
        self.assertEqual(self.drafts.get(condition())['variants'][0]['text'], '別画面の編集')

    def test_parent_close_protects_unsaved_child(self):
        from image_tool_gui import ContentImageToolWindow
        from content_images import ImageRegistry
        tool = ContentImageToolWindow(self.root, lambda: {'playtest-mike': 'ミケ'},
                    registry=ImageRegistry(Path(self.temp.name) / 'images'), interaction_drafts=self.drafts)
        tool.open_interaction_text_editor()
        tool.interaction_text_editor.load_condition(condition())
        tool.interaction_text_editor.fields[0].insert('1.0', '親を閉じても保護する文章')
        with patch('tkinter.messagebox.askyesnocancel', return_value=None):
            tool.close()
        self.assertTrue(tool.window.winfo_exists())
        with patch('tkinter.messagebox.askyesnocancel', return_value=True):
            tool.close()
        self.assertEqual(self.drafts.get(condition())['variants'][0]['text'], '親を閉じても保護する文章')

    def test_selectors_restrict_actions_and_reset_review(self):
        e = self.editor
        self.fill(['セリフ', '', ''])
        e.preview()
        with patch('tkinter.messagebox.askyesnocancel', return_value=False):
            e.choices['mode'].set('なでる')
            e.selection_changed()
        self.assertNotIn('激しく動かす', e.selectors['action'].cget('values'))
        self.assertFalse(e.register())
        e.choices['mode'].set('休憩')
        e.selection_changed()
        self.assertEqual(e.values()['condition']['scene_key'], 'pause')
        self.assertEqual(e.values()['condition']['action'], 'pause')

    def test_parent_tool_button_opens_single_editor_and_scopes_output(self):
        from image_tool_gui import ContentImageToolWindow
        from content_images import ImageRegistry
        tool = ContentImageToolWindow(self.root, lambda: {'playtest-mike': 'ミケ'},
                    registry=ImageRegistry(Path(self.temp.name) / 'images'), interaction_drafts=self.drafts)
        self.addCleanup(tool.window.destroy)
        def descendants(widget):
            for child in widget.winfo_children():
                yield child
                yield from descendants(child)
        button = next(w for w in descendants(tool.window)
                      if w.winfo_class() == 'TButton' and w.cget('text') == '日常交流の文章を編集…')
        button.invoke()
        first = tool.interaction_text_editor
        button.invoke()
        self.assertIs(tool.interaction_text_editor, first)
        self.assertEqual(first.published.path, tool.registry.directory / 'interaction_texts.json')
        self.assertFalse(tool.published_texts.path.exists())

    def test_controls_fit_at_minimum_window_size(self):
        e = self.editor
        e.window.geometry('720x650+0+0')
        self.root.update()
        widgets = e.fields + [e.preview_text]
        widgets += [w for w in e.window.winfo_children()[0].winfo_children() if w.winfo_class() == 'TButton']
        for widget in widgets:
            self.assertGreater(widget.winfo_height(), 10)
            self.assertLessEqual(widget.winfo_rooty() + widget.winfo_height(),
                                 e.window.winfo_rooty() + e.window.winfo_height())
            self.assertGreaterEqual(widget.winfo_rootx(), e.window.winfo_rootx())
            self.assertLessEqual(widget.winfo_rootx() + widget.winfo_width(),
                                 e.window.winfo_rootx() + e.window.winfo_width())


if __name__ == '__main__':
    unittest.main()
