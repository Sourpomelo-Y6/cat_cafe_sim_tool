import json
import os
import sys
import tempfile
import threading
import time
import unittest
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from llm_client import LlmClient, parse_record, GenerationCancelled

RECORD = {'introduction': '好奇心いっぱいのミケです。', 'scenes': {
    'normal': '静かに座っています。', 'play': 'おもちゃを追いかけます。',
    'pet': '撫でられて目を細めます。', 'sleep': '丸くなって眠っています。'}}


class LlmTests(unittest.TestCase):
    def test_response_validation(self):
        text = json.dumps(RECORD, ensure_ascii=False)
        self.assertEqual(parse_record(text), RECORD)
        self.assertEqual(parse_record('```json\n' + text + '\n```'), RECORD)
        for invalid in ('説明文です。', '[]', '{}',
                        json.dumps({'introduction': '紹介', 'scenes': {'normal': '通常'}}),
                        json.dumps(dict(RECORD, introduction=' ')),
                        json.dumps(dict(RECORD, introduction='あ' * 2001))):
            with self.assertRaises(ValueError):
                parse_record(invalid)

    def test_api_request_record_and_cancellation(self):
        calls = []
        class Reply:
            def __init__(self, data):
                self.data = data
            def __enter__(self):
                return self
            def __exit__(self, *args):
                pass
            def read(self, limit):
                return json.dumps(self.data).encode('utf-8')
        def urlopen(req, timeout):
            calls.append((req, timeout))
            if req.full_url.endswith('/models'):
                return Reply({'data': [{'id': 'local-cat'}]})
            return Reply({'choices': [{'finish_reason': 'stop', 'message': {
                'content': json.dumps(RECORD, ensure_ascii=False)}}]})
        with tempfile.TemporaryDirectory() as directory, patch('llm_client.request.urlopen', side_effect=urlopen):
            client = LlmClient('http://127.0.0.1:8080/v1/')
            self.assertEqual(client.list_models(), ['local-cat'])
            record, path = client.generate('local-cat', 'cat-a', 'ミケ', '三毛猫', 'やさしい文章',
                                           threading.Event(), directory)
            self.assertEqual(record, RECORD)
            metadata = json.loads(path.read_text(encoding='utf-8'))
            self.assertEqual(metadata['cat_id'], 'cat-a')
            self.assertEqual(metadata['record'], RECORD)
            self.assertEqual(calls[1][0].full_url, 'http://127.0.0.1:8080/v1/chat/completions')
            payload = json.loads(calls[1][0].data)
            self.assertEqual(payload['response_format'], {'type': 'json_object'})
            self.assertIn('三毛猫', payload['messages'][1]['content'])
            cancel = threading.Event()
            cancel.set()
            with self.assertRaises(GenerationCancelled):
                client.generate('local-cat', 'a', 'ミケ', '特徴', '', cancel, directory)
            self.assertEqual(len(calls), 2)

    def test_failures_do_not_save_candidate(self):
        with tempfile.TemporaryDirectory() as directory:
            client = LlmClient()
            for response in ({'choices': []}, {'choices': [{'message': {'content': None}}]},
                             {'choices': [{'finish_reason': 'length', 'message': {'content': '{}'}}]}):
                with patch.object(client, '_request', return_value=response), self.assertRaises(ValueError):
                    client.generate('model', 'a', '猫', '特徴', '', threading.Event(), directory)
            cancel = threading.Event()
            def cancelled(*args, **kwargs):
                cancel.set()
                return {'choices': []}
            with patch.object(client, '_request', side_effect=cancelled), self.assertRaises(GenerationCancelled):
                client.generate('model', 'a', '猫', '特徴', '', cancel, directory)
            self.assertEqual(list(Path(directory).iterdir()), [])
            with patch.object(client, '_request', return_value={'data': []}), self.assertRaises(ValueError):
                client.list_models()
        for url in ('file:///etc/passwd', 'http://name:secret@localhost', 'http://localhost?x=1'):
            with self.assertRaises(ValueError):
                LlmClient(url)

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'requires a display')
    def test_gui_generation_review_and_edited_text_protection(self):
        import tkinter as tk
        from text_editor_gui import TextEditorWindow
        from content_texts import TextStore
        root = tk.Tk()
        root.withdraw()
        self.addCleanup(root.destroy)
        def wait(window):
            deadline = time.monotonic() + 5
            while window.running and time.monotonic() < deadline:
                root.update()
                time.sleep(0.01)
            self.assertFalse(window.running)
        with tempfile.TemporaryDirectory() as directory:
            drafts = TextStore(Path(directory) / 'drafts.json')
            published = TextStore(Path(directory) / 'texts.json')
            editor = TextEditorWindow(root, drafts, published, 'cat-a', 'ミケ')
            editor.open_generation()
            generation = editor.generation_window
            with patch('llm_generation_gui.LlmClient.list_models', return_value=['local-model']):
                generation.fetch()
                wait(generation)
            self.assertEqual(generation.model.get(), 'local-model')
            generation.features.insert('1.0', '三毛猫、好奇心旺盛')
            with patch('llm_generation_gui.LlmClient.generate', return_value=(RECORD, Path(directory) / 'candidate.json')):
                generation.start()
                wait(generation)
            self.assertEqual(editor.values()['introduction'], '')
            self.assertFalse(drafts.path.exists())
            self.assertFalse(published.path.exists())
            generation.apply()
            self.assertEqual(editor.values(), RECORD)
            self.assertIsNone(editor.reviewed)
            editor.register()
            self.assertFalse(published.path.exists())
            editor.preview()
            editor.register()
            self.assertEqual(published.get('cat-a'), RECORD)
            generation.window.destroy()
            editor.open_generation()
            generation = editor.generation_window
            generation.model.set('local-model')
            generation.features.insert('1.0', '三毛猫')
            with patch('llm_generation_gui.LlmClient.generate', side_effect=ValueError('接続失敗')):
                generation.start()
                wait(generation)
            self.assertEqual(editor.values(), RECORD)
            self.assertIn('接続失敗', generation.status.get())
            with patch('llm_generation_gui.LlmClient.generate', return_value=(RECORD, Path(directory) / 'candidate.json')):
                generation.start()
                wait(generation)
            editor.fields['introduction'].insert('end', '手動編集です。')
            generation.apply()
            self.assertIn('手動編集', editor.values()['introduction'])
            self.assertIn('変更された', generation.status.get())
            generation.running = True
            generation.poll()
            editor.window.destroy()
            self.assertTrue(generation.cancel.is_set())
            self.assertIsNone(generation.timer)
            root.update()
