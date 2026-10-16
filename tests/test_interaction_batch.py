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
from interaction_texts import condition, key, InteractionTextStore, actions, reactions, MODES


def settings_at(folder):
    settings = InteractionLlmSettings(Path(folder) / 'settings.json')
    for subject in ('player', 'guest-1'):
        for mode in MODES:
            for action in actions(mode):
                settings.save(condition(subject_id=subject, mode=mode, action=action),
                              'http://127.0.0.1:8080', 'test-model', subject+'の資料', mode+'の絵のメモ', '短い文')
    for mode in MODES:
        for action in actions(mode):
            for reaction in reactions('cat_response',mode):
                settings.save(condition('cat_response','cat-mike',mode,action,reaction),
                    'http://127.0.0.1:8080','test-model','ミケの資料',mode+'／'+reaction,'短い文')
    return settings


class BatchPlanTests(unittest.TestCase):
    def test_cat_reactions_operations_and_missing_filter(self):
        with tempfile.TemporaryDirectory() as folder:
            settings=settings_at(folder)
            common=prepare_jobs(settings,['cat-mike'],['teaser','pet','pause'],kind='cat_response')
            self.assertEqual(len(common),14)
            all_jobs=prepare_jobs(settings,['cat-mike'],['teaser','pet','pause'],kind='cat_response',scope='all')
            self.assertEqual(len(all_jobs),50)
            specific=prepare_jobs(settings,['cat-mike'],['teaser','pet','pause'],kind='cat_response',scope='specific')
            self.assertEqual(len(specific),36)
            for value,notes in all_jobs:
                self.assertEqual(value['kind'],'cat_response')
                self.assertEqual(notes['subject_notes'],'ミケの資料')
                self.assertIn(value['reaction'],notes['image_notes'])
                if value['mode']=='pause':
                    self.assertIn(value['reaction'],('neutral','listless'))
            boundary=prepare_jobs(settings,['cat-mike'],['teaser','pet','pause'],kind='cat_response',
                selected_reactions=['turn_away','listless'])
            self.assertEqual(len(boundary),5)
            self.assertEqual({v['reaction'] for v,_ in boundary},{'turn_away','listless'})
            self.assertFalse(any(v['mode']=='pause' and v['reaction']=='turn_away' for v,_ in boundary))
            existing=key(common[0][0])
            remaining=prepare_jobs(settings,['cat-mike'],['teaser','pet','pause'],kind='cat_response',has_text=lambda v:key(v)==existing)
            self.assertEqual(len(remaining),13)
            self.assertNotIn(existing,[key(v) for v,_ in remaining])
            for chosen in ([],['invalid']):
                with self.assertRaises(ValueError):
                    prepare_jobs(settings,['cat-mike'],['teaser'],kind='cat_response',selected_reactions=chosen)

    def test_specific_operations_and_skip_existing(self):
        with tempfile.TemporaryDirectory() as folder:
            settings=settings_at(folder)
            all_jobs=prepare_jobs(settings,['player','guest-1'],['teaser','pet','pause'],scope='all')
            self.assertEqual(len(all_jobs),18)
            specific=prepare_jobs(settings,['player','guest-1'],['teaser','pet','pause'],scope='specific')
            self.assertEqual(len(specific),12)
            self.assertEqual({v['action'] for v,_ in specific},{'direct','adapt','intense','feint'})
            existing={key(v) for v,_ in all_jobs[:6]}
            remaining=prepare_jobs(settings,['player','guest-1'],['teaser','pet','pause'],scope='all',has_text=lambda v:key(v) in existing)
            self.assertEqual(len(remaining),12)
            self.assertFalse(existing.intersection(key(v) for v,_ in remaining))
            self.assertTrue(any(v['action']=='intense' for v,_ in remaining))
            with self.assertRaises(ValueError):
                prepare_jobs(settings,['player'],['pet'],scope='all',has_text=lambda _:True)

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
        for mode,var in self.window.modes.items():var.set(mode in ('teaser','pet','pause'))
        self.root.update()

    def wait(self):
        until = time.monotonic()+5
        while self.window.running and time.monotonic()<until:
            self.root.update();time.sleep(.01)
        self.assertFalse(self.window.running)

    def fake(self, model, value, *args, **kwargs):
        return ['案一', '案二', '案三'], Path(self.temp.name)/'record.json'

    def test_cat_window_routes_reactions_and_imports_after_retry(self):
        from interaction_text_editor_gui import InteractionTextEditorWindow
        from interaction_texts import make_entry
        drafts=InteractionTextStore(Path(self.temp.name)/'cat-drafts.json')
        published=InteractionTextStore(Path(self.temp.name)/'cat-published.json',published=True)
        old=make_entry(condition('cat_response','cat-mike',reaction='neutral'),['既存の反応','',''])
        drafts.save(old)
        editor=InteractionTextEditorWindow(self.root,drafts,published,generation_settings=self.settings,generation_output=Path(self.temp.name)/'records')
        editor.open_batch_generation();human=editor.batch_window
        editor.load_condition(condition('cat_response','cat-mike',reaction='neutral'))
        self.assertEqual(editor.batch_button.cget('text'),'猫の反応を連続生成…')
        editor.open_batch_generation();self.window=editor.batch_window
        self.assertIsNot(self.window,human)
        self.assertEqual(self.window.kind,'cat_response')
        self.assertEqual(self.window.scope.get(),'動作共通のみ')
        for mode,var in self.window.modes.items():var.set(mode in ('teaser','pet','pause'))
        for r,var in self.window.reaction_choices.items():
            var.set(r in ('turn_away','listless'))
        calls=[]
        def generate(model,value,*args,**kwargs):
            calls.append(value.copy())
            if len(calls)==2:raise ValueError('検証用の猫エラー')
            return ['ミケは静かに体を丸めた。','ミケは前足をそろえた。','ミケはゆっくり息をついた。'],Path(self.temp.name)/'cat-record.json'
        with patch('interaction_batch_gui.InteractionLlmClient.generate_variants',side_effect=generate):
            self.window.continue_errors.set(False)
            self.window.start();self.wait()
            self.assertEqual(len(self.window.results),1)
            self.window.start();self.wait()
        self.assertEqual(len(self.window.results),5)
        self.assertTrue(all(v['kind']=='cat_response' for v in calls))
        self.assertEqual(sum(key(v)==key(calls[0]) for v in calls),1)
        self.window.apply()
        self.assertEqual(editor._condition['kind'],'cat_response')
        self.assertEqual(editor._condition['mode'],'pause')
        self.assertEqual(editor._condition['reaction'],'listless')
        self.assertTrue(editor.dirty())
        self.assertEqual(drafts.get(old['condition']),old)
        self.assertEqual(len(drafts.read()['entries']),1)
        self.assertFalse(published.path.exists())
        editor.open_batch_generation();self.assertIs(editor.batch_window,self.window)

    def test_sequential_six_results_retry_without_regeneration_and_apply(self):
        calls = []
        def generate(model, value, *args, **kwargs):
            calls.append(key(value))
            if len(calls)==3:
                raise ValueError('検証用の失敗')
            return self.fake(model, value)
        with patch('interaction_batch_gui.InteractionLlmClient.generate_variants', side_effect=generate):
            self.window.continue_errors.set(False)
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
        editor=InteractionTextEditorWindow(self.root,drafts,published,generation_settings=self.settings,generation_output=Path(self.temp.name)/'records')
        editor.load_condition(condition())
        editor.fields[0].insert('1.0','消してはいけない文')
        editor.open_batch_generation();batch=editor.batch_window
        self.assertEqual(batch.scope.get(),'共通＋操作別')
        self.assertTrue(batch.only_missing.get())
        self.assertTrue(batch.has_text(condition()))
        self.assertFalse(batch.has_text(condition(action='intense')))
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
