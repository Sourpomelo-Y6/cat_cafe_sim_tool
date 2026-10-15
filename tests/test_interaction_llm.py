import json
import os
from pathlib import Path
import sys
import tempfile
import threading
import time
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from interaction_llm import InteractionLlmClient, InteractionLlmSettings, parse_variants
from interaction_texts import InteractionTextStore, condition
from llm_client import GenerationCancelled

VARIANTS = ['ミケ、こっちだよ。', 'このおもちゃ、気になる？', '一緒に遊ぼうか。']


def response(variants=VARIANTS):
    return {'choices': [{'finish_reason': 'stop', 'message': {'content': json.dumps({'variants': variants})}}]}


class InteractionLlmTests(unittest.TestCase):
    def test_strict_json_and_three_unique_short_variants(self):
        text = json.dumps({'variants': VARIANTS})
        self.assertEqual(parse_variants(text), VARIANTS)
        self.assertEqual(parse_variants('```json\n' + text + '\n```'), VARIANTS)
        for invalid in ['not json', '[]', '{}', '{"variants":[]}', None,
                        json.dumps({'variants': ['a', 'a', 'b']}),
                        json.dumps({'variants': ['a', ' ', 'b']}),
                        json.dumps({'variants': ['a', None, 'b']}),
                        json.dumps({'variants': ['あ' * 201, 'a', 'b']}),
                        json.dumps({'variants': VARIANTS, 'reaction': 'favorable'})]:
            with self.subTest(invalid=invalid), self.assertRaises(ValueError):
                parse_variants(invalid)

    def test_transport_conditions_and_generation_record(self):
        client = InteractionLlmClient('http://127.0.0.1:8080/v1/')
        with tempfile.TemporaryDirectory() as directory:
            for value in [condition(subject_id='guest-1'),
                          condition('cat_response', 'cat-mike', mode='pet', action='adapt', reaction='favorable'),
                          condition('cat_response', 'cat-mike', mode='pause', action='pause', reaction='listless')]:
                with patch.object(client, '_request', return_value=response()) as request:
                    variants, path = client.generate_variants('model', value, '資料', '座っている', '短い文',
                                                              threading.Event(), output=directory)
                self.assertEqual(variants, VARIANTS)
                self.assertEqual(request.call_args.args[0], 'chat/completions')
                self.assertEqual(request.call_args.kwargs['timeout'], 180)
                payload = request.call_args.args[1]
                self.assertEqual(payload['response_format']['type'], 'json_schema')
                self.assertEqual(payload['response_format']['json_schema']['schema']['properties']['variants']['minItems'], 3)
                self.assertEqual(payload['chat_template_kwargs'], {'enable_thinking': False})
                material = json.loads(payload['messages'][1]['content'].split('資料：')[1])
                self.assertEqual(material['condition'], value)
                self.assertEqual(material['subject_notes'], '資料')
                record = json.loads(path.read_text(encoding='utf-8'))
                self.assertEqual(record['kind'], 'interaction_variants')
                self.assertEqual(record['condition'], value)
                self.assertEqual(record['variants'], VARIANTS)

    def test_invalid_inputs_responses_and_cancel_do_not_save(self):
        client = InteractionLlmClient()
        with tempfile.TemporaryDirectory() as directory:
            for bad in [{'choices': []}, {'choices': [None]}, {},
                        {'choices': [{'finish_reason': 'length', 'message': {'content': '{}'}}]},
                        response(['同じ', '同じ', '同じ'])]:
                with patch.object(client, '_request', return_value=bad), self.assertRaises(ValueError):
                    client.generate_variants('model', condition(), '資料', '絵', '', threading.Event(), output=directory)
            with patch.object(client, '_request') as request, self.assertRaises(ValueError):
                client.generate_variants('model', condition(), '', '絵', '', threading.Event(), output=directory)
            request.assert_not_called()
            cancel = threading.Event()
            cancel.set()
            with self.assertRaises(GenerationCancelled):
                client.generate_variants('model', condition(), '資料', '絵', '', cancel, output=directory)
            cancel.clear()
            def cancelled(*args, **kwargs):
                cancel.set()
                return response()
            with patch.object(client, '_request', side_effect=cancelled), self.assertRaises(GenerationCancelled):
                client.generate_variants('model', condition(), '資料', '絵', '', cancel, output=directory)
            self.assertEqual(list(Path(directory).iterdir()), [])

    def test_settings_reopen_profile_scope_and_corrupt_file(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'settings.json'
            settings = InteractionLlmSettings(path)
            player, guest = condition(), condition(subject_id='guest-1')
            settings.save(player, 'http://127.0.0.1:8080/v1', 'model', 'プレイヤーの口調', '床近くの玩具', '短い')
            settings.save(guest, 'http://127.0.0.1:8080', 'model', '佐藤さんの口調', '手元の玩具', '短い')
            reopened = InteractionLlmSettings(path)
            self.assertEqual(reopened.get(player)['subject_notes'], 'プレイヤーの口調')
            self.assertEqual(reopened.get(guest)['image_notes'], '手元の玩具')
            pet = condition(mode='pet')
            self.assertEqual(reopened.get(pet)['subject_notes'], 'プレイヤーの口調')
            self.assertEqual(reopened.get(pet)['image_notes'], '')
            path.write_text('{broken', encoding='utf-8')
            with self.assertRaises(ValueError):
                settings.save(player, 'http://localhost:8080', 'model', '資料', '絵', '')
            self.assertEqual(path.read_text(encoding='utf-8'), '{broken')


@unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'requires a display')
class InteractionLlmGuiTests(unittest.TestCase):
    def setUp(self):
        import tkinter as tk
        from interaction_text_editor_gui import InteractionTextEditorWindow
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = tk.Tk()
        self.root.withdraw()
        self.addCleanup(self.root.destroy)
        base = Path(self.temp.name)
        self.settings = InteractionLlmSettings(base / 'settings.json')
        self.editor = InteractionTextEditorWindow(self.root, InteractionTextStore(base / 'draft.json'),
                       InteractionTextStore(base / 'published.json', published=True),
                       generation_settings=self.settings, generation_output=base / 'generated')
        self.root.update()

    def open(self):
        self.editor.open_generation()
        g = self.editor.generation_window
        g.model.set('local-model')
        g.notes['subject_notes'].insert('1.0', '穏やかな口調')
        g.notes['image_notes'].insert('1.0', '座ってねこじゃらしを見ている')
        return g

    def wait(self, g):
        deadline = time.monotonic() + 5
        while g.running and time.monotonic() < deadline:
            self.root.update()
            time.sleep(.01)
        self.assertFalse(g.running)

    def generate(self, g):
        with patch('interaction_llm_gui.InteractionLlmClient.generate_variants',
                   return_value=(VARIANTS, Path(self.temp.name) / 'candidate.json')):
            g.start()
            self.wait(g)

    def test_subset_import_retains_other_slots_and_requires_review(self):
        e = self.editor
        e.fields[0].insert('1.0', '手入力の1')
        e.fields[2].insert('1.0', '手入力の3')
        self.root.update()
        g = self.open()
        with patch('interaction_llm_gui.InteractionLlmClient.list_models', return_value=['local-model']):
            g.fetch()
            self.wait(g)
        self.generate(g)
        self.assertEqual(e.values()['variants'][1]['text'], '')
        self.assertFalse(e.drafts.path.exists())
        for checked in g.selected:
            checked.set(False)
        g.selected[0].set(True)
        g.destinations[0].set('案2')
        g.apply()
        self.assertEqual([r['text'] for r in e.values()['variants']], ['手入力の1', VARIANTS[0], '手入力の3'])
        self.assertFalse(e.published.path.exists())
        self.assertFalse(e.register())
        e.preview()
        self.assertTrue(e.register())

    def test_editor_changes_and_condition_changes_block_import(self):
        e = self.editor
        g = self.open()
        self.generate(g)
        e.fields[0].insert('1.0', '待っている間の編集')
        g.apply()
        self.assertEqual(e.values()['variants'][0]['text'], '待っている間の編集')
        g.window.destroy()
        g = self.open()
        self.generate(g)
        with patch('tkinter.messagebox.askyesnocancel', return_value=False):
            e.load_condition(condition(subject_id='guest-1'))
        g.apply()
        self.assertEqual(e.values()['variants'][0]['text'], '')

    def test_duplicate_target_and_existing_duplicate_block_import(self):
        e = self.editor
        e.fields[0].insert('1.0', VARIANTS[1])
        g = self.open()
        self.generate(g)
        g.destinations[1].set('案1')
        g.apply()
        self.assertIn('取り込み先が重複', g.status.get())
        for checked in g.selected:
            checked.set(False)
        g.apply()
        self.assertIn('1つ以上', g.status.get())
        g.selected[1].set(True)
        g.destinations[1].set('案2')
        g.apply()
        self.assertEqual(e.values()['variants'][1]['text'], '')

    def test_settings_saved_and_restored_without_publishing(self):
        g = self.open()
        g.save_settings()
        g.window.destroy()
        self.editor.open_generation()
        reopened = self.editor.generation_window
        self.assertEqual(reopened.model.get(), 'local-model')
        self.assertEqual(reopened.notes['subject_notes'].get('1.0', 'end-1c'), '穏やかな口調')
        self.assertFalse(self.editor.drafts.path.exists())
        self.assertFalse(self.editor.published.path.exists())

    def test_async_window_close_ignores_late_result(self):
        g = self.open()
        entered, release = threading.Event(), threading.Event()
        def slow(*args, **kwargs):
            entered.set()
            release.wait(3)
            return VARIANTS, Path(self.temp.name) / 'ignored.json'
        with patch('interaction_llm_gui.InteractionLlmClient.generate_variants', side_effect=slow):
            g.start()
            self.assertTrue(entered.wait(2))
            g.window.destroy()
            release.set()
        self.root.update()
        self.assertTrue(g.cancel.is_set())
        self.assertIsNone(g.timer)
        self.assertEqual(self.editor.values()['variants'][0]['text'], '')

    def test_errors_do_not_replace_editor_and_window_fits(self):
        g = self.open()
        with patch('interaction_llm_gui.InteractionLlmClient.generate_variants', side_effect=ValueError('不正な応答')):
            g.start()
            self.wait(g)
        self.assertIn('不正な応答', g.status.get())
        self.assertIsNone(g.candidate)
        g.window.geometry('740x670+0+0')
        for tab in g.tabs.tabs():
            g.tabs.select(tab)
            self.root.update()
            widgets = list(g.notes.values()) if tab != str(g.results_tab) else g.result_fields + [g.apply_button]
            for widget in widgets:
                self.assertGreater(widget.winfo_height(), 10)
                self.assertLessEqual(widget.winfo_rooty() + widget.winfo_height(),
                                     g.window.winfo_rooty() + g.window.winfo_height())


if __name__ == '__main__':
    unittest.main()
