import os
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from content_texts import TextStore


class TextTests(unittest.TestCase):
    def test_save_scope_and_invalid_file_preserved(self):
        with tempfile.TemporaryDirectory() as directory:
            store = TextStore(Path(directory) / 'texts.json')
            self.assertIsNone(store.get('a'))
            store.save('a', {'introduction': 'ミケの紹介', 'scenes': {'play': '遊んでいます。'}})
            store.save('b', {'introduction': 'タマの紹介', 'scenes': {}})
            self.assertEqual(store.get('a')['scenes']['play'], '遊んでいます。')
            before = store.path.read_bytes()
            for record in ({'introduction': 3, 'scenes': {}},
                           {'introduction': '', 'scenes': {'invalid': ''}},
                           {'introduction': '', 'scenes': {'normal': 'あ' * 501}}):
                with self.assertRaises(ValueError):
                    store.save('a', record)
                self.assertEqual(store.path.read_bytes(), before)
            store.path.write_text('{bad', encoding='utf-8')
            with self.assertRaises(ValueError):
                store.save('a', {'introduction': '', 'scenes': {}})
            self.assertEqual(store.path.read_text(encoding='utf-8'), '{bad')

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'requires a display')
    def test_gui_draft_review_register_and_game_display(self):
        import tkinter as tk
        from image_tool_gui import ContentImageToolWindow
        from content_images import ImageRegistry
        sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'cat_cafe_sim'))
        from cat_cafe_sim.content_texts import TextReader
        from cat_cafe_sim.cafe_action_preview_gui import CatActionPreviewWindow
        root = tk.Tk()
        root.withdraw()
        self.addCleanup(root.destroy)
        with tempfile.TemporaryDirectory() as directory:
            drafts = TextStore(Path(directory) / 'drafts.json')
            registry = ImageRegistry(Path(directory) / 'images')
            tool = ContentImageToolWindow(root, lambda: {'cat-a': 'ミケ', 'cat-b': 'タマ'},
                                          registry=registry, text_drafts=drafts)
            tool.open_text_editor()
            editor = tool.text_editor
            editor.fields['introduction'].insert('1.0', '好奇心いっぱいのミケです。')
            editor.fields['play'].insert('1.0', 'おもちゃを追いかけています。')
            editor.save_draft()
            self.assertFalse(tool.published_texts.path.exists())
            editor.register()
            self.assertFalse(tool.published_texts.path.exists())
            editor.preview()
            editor.fields['play'].insert('end', '楽しそうです。')
            editor.register()
            self.assertFalse(tool.published_texts.path.exists())
            editor.preview()
            editor.register()
            reader = TextReader(tool.published_texts.path)
            expected = editor.values()
            self.assertEqual(reader.display('cat-a', 'ミケ', 'play')[1], expected['scenes']['play'])
            preview = CatActionPreviewWindow(root, text_reader=reader)
            preview.target_id = 'cat-a'
            preview.show_scene('play')
            self.assertEqual(preview.introduction.get(), expected['introduction'])
            self.assertIn(expected['scenes']['play'], preview.caption.get())
            preview.target_id = 'cat-b'
            preview.show_scene('play')
            self.assertNotIn('おもちゃを追いかけ', preview.caption.get())
            tool.published_texts.path.write_text('{bad', encoding='utf-8')
            preview.target_id = 'cat-a'
            preview.show_scene('sleep')
            self.assertIn('丸くなって', preview.caption.get())
            preview.window.destroy()
            editor.window.destroy()
            tool.open_text_editor()
            self.assertIn('好奇心', tool.text_editor.values()['introduction'])
            tool.window.destroy()
            root.update()
