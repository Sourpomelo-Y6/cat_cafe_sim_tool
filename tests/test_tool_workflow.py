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
                self.assertEqual(tool.tabs.tab(tool.pages['画像'], 'state'), 'normal')
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

    def test_portrait_candidate_adoption_and_both_export_routes(self):
        import tkinter as tk
        import json
        from PIL import Image
        from image_tool_gui import ContentImageToolWindow
        from content_images import ImageRegistry
        from content_texts import TextStore
        root = tk.Tk(); root.withdraw()
        try:
            with tempfile.TemporaryDirectory() as directory:
                base = Path(directory)
                tool = ContentImageToolWindow(root, lambda: {'playtest-mike':'ミケ'},
                    registry=ImageRegistry(base/'images'), text_drafts=TextStore(base/'textdraft.json'),
                    interaction_drafts=InteractionTextStore(base/'draft.json'))
                tool.subject_registry.save(tool.subject_registry.get('guest-1'))
                tool.filter_subjects()
                tool.subject_choice.set(next(k for k,r in tool.subject_rows.items() if r['id']=='guest-1'))
                tool.select_subject()
                self.assertTrue(tool.portrait_panel.frame.winfo_manager())
                self.assertFalse(tool.cat_image_panel.winfo_manager())
                source=base/'portrait.png'; Image.new('RGBA',(32,32),(30,40,50,100)).save(source)
                panel=tool.portrait_panel
                panel.select_file(source)
                panel.select(tool.subject_registry.get('player'))
                panel.select(tool.subject_registry.get('guest-1'))
                self.assertEqual(panel.pending, source)
                self.assertEqual(panel.store.read()['people'], [])
                self.assertTrue(panel.adopt())
                adopted=panel.store.read()['people'][0]
                root.deiconify()
                tool.window.geometry('900x700')
                tool.tabs.select(tool.pages['確認・書き出し'])
                root.update()
                self.assertIn('書き出し可能な人物画像：1人', tool.export_summary.get('1.0', 'end'))
                tool.tabs.select(tool.pages['画像'])
                root.update()
                tool.interaction_published.save(make_entry(condition('cat_response', 'cat-mike', reaction='neutral'), ['検証文', '', '']))
                tool.tabs.select(tool.pages['確認・書き出し'])
                root.update()
                self.assertIn('採用交流文：1条件', tool.export_summary.get('1.0', 'end'))
                def descendants(widget):
                    for child in widget.winfo_children():
                        yield child
                        yield from descendants(child)
                buttons = [w for w in descendants(tool.pages['確認・書き出し']) if w.winfo_class()=='TButton']
                for button in buttons:
                    self.assertTrue(button.winfo_ismapped())
                    self.assertLessEqual(button.winfo_rooty()+button.winfo_height(), tool.window.winfo_rooty()+tool.window.winfo_height())

                with patch('tkinter.filedialog.askdirectory', return_value=str(base/'all')):
                    tool.export_unity()
                full=next((base/'all').glob('*/human_portraits.json'))
                self.assertEqual(json.loads(full.read_text(encoding='utf-8'))['people'], [adopted])
                with patch('tkinter.filedialog.askdirectory', return_value=str(base/'people')):
                    tool.export_people()
                self.assertTrue(list((base/'people').glob('*/human_portraits.json')))
                with patch('tkinter.messagebox.askyesno', return_value=True):
                    panel.remove()
                self.assertEqual(panel.store.read()['people'], [])
                self.assertTrue((panel.store.directory/adopted['image']).exists())
                with patch('tkinter.messagebox.askyesno', return_value=False), patch('tkinter.filedialog.askdirectory') as dialog:
                    tool.export_people()
                    dialog.assert_not_called()
        finally:
            root.destroy()

    def test_missing_navigation_diff_and_binding_editor(self):
        import tkinter as tk
        from image_tool_gui import ContentImageToolWindow
        from content_images import ImageRegistry
        from content_texts import TextStore
        root=tk.Tk(); root.withdraw()
        try:
            with tempfile.TemporaryDirectory() as directory:
                base=Path(directory)
                tool=ContentImageToolWindow(root, lambda:{'playtest-mike':'ミケ'},
                    registry=ImageRegistry(base/'images'), text_drafts=TextStore(base/'textdraft.json'),
                    interaction_drafts=InteractionTextStore(base/'drafts.json'))
                tool.subject_registry.save(tool.subject_registry.get('cat-mike'))
                original=tool.subject_registry.path.read_bytes()
                tool.open_review_preview(); preview=tool.review_window
                self.assertEqual(str(preview.content_selector['state']), 'disabled')
                special=next(k for k,v in __import__('interaction_texts').NARRATIVE_SCENES.items() if k=='switch')
                preview.variables['narrative_scene'].set(__import__('interaction_texts').NARRATIVE_SCENES[special])
                preview.render()
                label=next(k for k,v in preview.missing_conditions.items() if v.get('narrative_scene')=='switch' and v['kind']=='speech')
                preview.missing_choice.set(label)
                expected=preview.missing_conditions[label]
                self.assertTrue(preview.edit_missing())
                editor=tool.interaction_text_editor
                self.assertEqual(editor._condition, expected)
                self.assertEqual(expected['target_cat'], 'cat-mike')
                editor.fields[0].insert('end','新しい候補')
                other=next(k for k,v in preview.missing_conditions.items() if v['kind']=='cat_response')
                preview.missing_choice.set(other)
                with patch('tkinter.messagebox.askyesnocancel', return_value=None):
                    self.assertFalse(preview.edit_missing())
                self.assertEqual(editor._condition, expected)
                editor.preview()
                review=editor.preview_text.get('1.0','end')
                self.assertIn('追加', review)
                self.assertIn('今回採用：新しい候補', review)
                self.assertTrue(editor.register())
                editor.fields[0].delete('1.0','end'); editor.fields[1].insert('end','別の候補')
                editor.preview()
                review=editor.preview_text.get('1.0','end')
                self.assertIn('削除',review); self.assertIn('採用済み：新しい候補',review)
                tool.interaction_published.save(make_entry(expected, ['別操作の更新','','']))
                editor.preview()
                self.assertIsNone(editor.reviewed)
                self.assertIn('更新', editor.notice.get())
                self.assertFalse(editor.register())
                preview.open_binding_editor()
                manager=preview.binding_editor
                self.assertEqual(manager.current['id'],'cat-mike')
                self.assertEqual(tool.subject_registry.path.read_bytes(), original)
                manager.variables['content_id'].set('new-image-id')
                with patch('tkinter.messagebox.askyesnocancel', return_value=None):
                    self.assertFalse(preview.confirm_leave())
                self.assertTrue(manager.save())
                self.assertEqual(tool.subject_registry.get('cat-mike')['content_id'],'new-image-id')
                self.assertEqual(preview.saved_content_id,'new-image-id')
        finally:
            root.destroy()
