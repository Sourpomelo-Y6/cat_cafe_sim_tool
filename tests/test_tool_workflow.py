"""Workflow handoff and comparison regression tests using temporary stores."""
import os
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from interaction_texts import InteractionTextStore, condition, make_entry
from interaction_text_editor_gui import InteractionTextEditorWindow

@unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'requires display')
class WorkflowTests(unittest.TestCase):
    def test_initial_subject_compare_cancel_and_special_roundtrip(self):
        import tkinter as tk
        with tempfile.TemporaryDirectory() as directory:
            root = tk.Tk()
            root.deiconify()
            try:
                drafts = InteractionTextStore(Path(directory)/'draft.json')
                published = InteractionTextStore(Path(directory)/'published.json', published=True)
                normal = condition('cat_response', 'cat-mike', reaction='neutral')
                published.save(make_entry(normal, ['採用文', '', '']))
                editor = InteractionTextEditorWindow(root, drafts, published, initial_condition=normal)
                root.update()
                self.assertEqual(editor._condition, normal)
                self.assertIn('採用文', editor.comparison.get('1.0', 'end'))
                editor.fields[0].insert('end', '修正')
                root.update()
                special = condition('cat_response', 'cat-mike', action='switch', narrative_scene='switch')
                with patch('tkinter.messagebox.askyesnocancel', return_value=None):
                    self.assertFalse(editor.load_condition(special))
                self.assertEqual(editor._condition, normal)
                self.assertTrue(editor.save_draft())
                self.assertTrue(editor.load_condition(special))
                self.assertTrue(editor.load_condition(normal))
                self.assertEqual(editor.values()['variants'][0]['text'], '採用文修正')
                self.assertEqual(published.get(normal)['variants'][0]['text'], '採用文')
                editor.window.geometry('900x700')
                root.update()
                editor.resize_comparison(type('Resize', (), dict(widget=editor.window, width=900))())
                self.assertFalse(editor.comparison.frame.winfo_manager())
                editor.open_comparison()
                root.update()
            finally:
                root.destroy()

    def test_shared_subject_search_and_explicit_navigation(self):
        import tkinter as tk
        from image_tool_gui import ContentImageToolWindow
        from content_images import ImageRegistry
        from content_texts import TextStore
        root = tk.Tk(); root.withdraw()
        try:
            with tempfile.TemporaryDirectory() as directory:
                base = Path(directory)
                tool = ContentImageToolWindow(root, lambda: {'playtest-mike':'ミケ'},
                    registry=ImageRegistry(base/'images'),
                    text_drafts=TextStore(base/'texts.json'),
                    interaction_drafts=InteractionTextStore(base/'drafts.json'))
                tool.subject_search.set('プレイヤー')
                self.assertTrue(tool.subject_selector['values'])
                label = next(k for k,r in tool.subject_rows.items() if r['id']=='player')
                tool.subject_choice.set(label); tool.select_subject()
                self.assertEqual(tool.tabs.tab(tool.pages['画像'], 'state'), 'disabled')
                tool.open_interaction_text_editor('switch')
                editor = tool.interaction_text_editor
                self.assertEqual(editor._condition['subject_id'], 'player')
                self.assertEqual(editor._condition['narrative_scene'], 'switch')
                editor.fields[0].insert('end', '編集中')
                with patch('tkinter.messagebox.askyesnocancel', return_value=None):
                    tool.open_interaction_text_editor()
                self.assertEqual(editor._condition['narrative_scene'], 'switch')
                self.assertEqual(editor.values()['variants'][0]['text'], '編集中')
                self.assertTrue(editor.save_draft())
                tool.open_interaction_text_editor()
                self.assertNotIn('narrative_scene', editor._condition)
                tool.select_image_cat()
                self.assertEqual(tool.tabs.tab(tool.pages['画像'], 'state'), 'normal')
                tool.open_interaction_text_editor()
                self.assertEqual(editor._condition['subject_id'], 'cat-mike')
                self.assertIn('画像', editor.comparison.get('1.0','end'))
                from PIL import Image
                source = base/'fixture.png'
                Image.new('RGB', (32, 32), 'white').save(source)
                tool.registry.register('playtest-mike', 'teaser', source)
                editor.refresh_comparison()
                self.assertIsNotNone(editor.comparison_image)
                self.assertEqual(editor.comparison_image_result['resolved_scene_key'], 'teaser')
                self.assertTrue(editor.comparison.image_names())
        finally:
            root.destroy()
