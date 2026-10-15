import os
from pathlib import Path
import sys
import tempfile
import threading
import time
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from interaction_batch_gui import prepare_jobs, InteractionBatchWindow
from interaction_llm import InteractionLlmSettings
from interaction_texts import condition, key, InteractionTextStore


def settings_at(folder):
    settings = InteractionLlmSettings(Path(folder) / 'settings.json')
    for subject in ('player', 'guest-1'):
        for mode in ('teaser', 'pet', 'pause'):
            settings.save(condition(subject_id=subject, mode=mode, action='pause' if mode == 'pause' else 'any'),
                          'http://127.0.0.1:8080', 'test-model', subject+'の資料', mode+'の絵のメモ', '短い文')
    return settings


class BatchPlanTests(unittest.TestCase):
    def test_six_conditions_and_preflight_missing_notes(self):
        with tempfile.TemporaryDirectory() as folder:
            settings = settings_at(folder)
            jobs = prepare_jobs(settings, ['player', 'guest-1'], ['teaser', 'pet', 'pause'])
            self.assertEqual(len(jobs), 6)
            self.assertEqual(len({key(v) for v, _ in jobs}), 6)
            self.assertEqual(jobs[-1][0]['action'], 'pause')
            self.assertEqual(jobs[0][1]['subject_notes'], 'playerの資料')
            settings.save(condition(), 'http://127.0.0.1:8080', 'test-model', '変更後', 'メモ', '短い文')
            self.assertEqual(jobs[0][1]['subject_notes'], 'playerの資料')
            for subjects, modes in [([], ['pet']), (['player'], []), (['unknown'], ['pet'])]:
                with self.assertRaises(ValueError):
                    prepare_jobs(settings, subjects, modes)
            with self.assertRaises(ValueError):
                prepare_jobs(InteractionLlmSettings(Path(folder)/'empty.json'), ['player'], ['pet'])


@unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'requires display')
class BatchGuiTests(unittest.TestCase):
    def setUp(self):
        import tkinter as tk
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = tk.Tk(); self.root.withdraw()
        self.addCleanup(self.root.destroy)
        self.settings = settings_at(self.temp.name)
        self.received = []
        self.window = InteractionBatchWindow(self.root, lambda entry: self.received.append(entry) or True,
                settings=self.settings, output=Path(self.temp.name)/'records')
        self.root.update()

    def wait(self):
        until = time.monotonic()+5
        while self.window.running and time.monotonic()<until:
            self.root.update();time.sleep(.01)
        self.assertFalse(self.window.running)

    def fake(self, model, value, *args, **kwargs):
        return ['案一', '案二', '案三'], Path(self.temp.name)/'record.json'

    def test_sequential_six_results_retry_without_regeneration_and_apply(self):
        calls = []
        def generate(model, value, *args, **kwargs):
            calls.append(key(value))
            if len(calls)==3:
                raise ValueError('検証用の失敗')
            return self.fake(model, value)
        with patch('interaction_batch_gui.InteractionLlmClient.generate_variants', side_effect=generate):
            self.window.start();self.wait()
            self.assertEqual(len(self.window.results), 2)
            self.assertIn('失敗', self.window.status.get())
            first = calls[:2]
            self.window.start();self.wait()
        self.assertEqual(len(self.window.results), 6)
        self.assertEqual(len(calls), 7)
        self.assertEqual(calls[:2], first)
        for ident in first:
            self.assertEqual(calls.count(ident), 1)
        self.assertFalse(self.received)
        self.window.apply()
        self.assertEqual(len(self.received), 1)
        self.assertEqual([r['text'] for r in self.received[0]['variants']], ['案一','案二','案三'])

    def test_cancel_waits_for_request_and_does_not_start_next(self):
        entered, release = threading.Event(), threading.Event()
        def generate(*args, **kwargs):
            entered.set();release.wait(3)
            return ['案一','案二','案三'], Path(self.temp.name)/'record.json'
        with patch('interaction_batch_gui.InteractionLlmClient.generate_variants', side_effect=generate) as call:
            self.window.start()
            self.assertTrue(entered.wait(1))
            self.window.stop();self.window.start()
            self.assertTrue(self.window.running)
            release.set();self.wait()
            self.assertEqual(call.call_count,1)
        self.assertFalse(self.window.results)
        with patch('interaction_batch_gui.InteractionLlmClient.generate_variants', side_effect=self.fake):
            self.window.start();self.wait()
        self.assertEqual(len(self.window.results),6)

    def test_close_cancels_and_removes_poll(self):
        entered, release = threading.Event(), threading.Event()
        def generate(*args, **kwargs):
            entered.set();release.wait(3)
            return self.fake(None,None)
        with patch('interaction_batch_gui.InteractionLlmClient.generate_variants', side_effect=generate):
            self.window.start();self.assertTrue(entered.wait(1))
            self.window.window.destroy()
            self.assertTrue(self.window.cancel.is_set())
            self.assertIsNone(self.window.timer)
            release.set()
        self.assertFalse(self.received)

    def test_import_protects_dirty_editor_and_does_not_save(self):
        from interaction_text_editor_gui import InteractionTextEditorWindow
        from interaction_texts import make_entry
        drafts=InteractionTextStore(Path(self.temp.name)/'drafts.json')
        published=InteractionTextStore(Path(self.temp.name)/'published.json',published=True)
        editor=InteractionTextEditorWindow(self.root,drafts,published,generation_settings=self.settings)
        editor.fields[0].insert('1.0','消してはいけない文')
        editor.open_batch_generation();batch=editor.batch_window
        entry=make_entry(condition(subject_id='guest-1',mode='pet'),['案一','案二','案三'])
        with patch('tkinter.messagebox.askyesnocancel',return_value=None):
            self.assertFalse(batch.on_result(entry))
        self.assertIn('消してはいけない文',editor.fields[0].get('1.0','end'))
        with patch('tkinter.messagebox.askyesnocancel',return_value=False):
            self.assertTrue(batch.on_result(entry))
        self.assertEqual(editor._condition,entry['condition'])
        self.assertEqual(editor.values(),entry)
        self.assertFalse(drafts.path.exists())
        self.assertFalse(published.path.exists())
        self.assertIsNone(editor.reviewed)


if __name__=='__main__':
    unittest.main()
