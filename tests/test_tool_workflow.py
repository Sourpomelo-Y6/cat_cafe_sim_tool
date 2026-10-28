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
