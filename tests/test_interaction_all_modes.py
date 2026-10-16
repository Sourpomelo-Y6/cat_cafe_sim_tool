import json
import os
from pathlib import Path
import tempfile
import threading
import time
import unittest
from unittest.mock import patch

from make_full_interaction_fixture import create
from interaction_texts import MODES,actions,reactions,condition,make_entry,key,InteractionTextStore
from interaction_presentation import ACTION_TEXTS,compose_preview,coverage,select_entry
from interaction_llm import InteractionLlmClient,InteractionLlmSettings
from interaction_batch_gui import prepare_jobs,InteractionBatchWindow
from interaction_subjects import SubjectRegistry


class AllModeTests(unittest.TestCase):
    def test_condition_matrix_matches_real_game_operations(self):
        rules=json.loads((Path(__file__).parents[1]/'unity_preview_template/Resources/CatCafeStandalone/rules.json').read_text(encoding='utf8'))
        self.assertEqual(set(MODES),{t['id'] for t in rules['types']}|{'pause'})
        for t in rules['types']:
            self.assertEqual(set(actions(t['id'])),set(t['actions'])|{'any'})
            for action in t['actions']:
                self.assertTrue(condition(mode=t['id'],action=action))
                self.assertTrue(any(r['mode']==t['id'] and r['action']==action for r in ACTION_TEXTS))
        self.assertEqual(len(ACTION_TEXTS),24)
        self.assertEqual(sum(len(actions(m)) for m in MODES),32)
        for mode,action in [('plush','intense'),('brush','feint'),('voice','intense'),('presence','feint')]:
            with self.assertRaises(ValueError):condition(mode=mode,action=action)
        with self.assertRaises(ValueError):condition('cat_response','cat-mike','pause','pause','favorable')

    def test_all_modes_export_and_missing_only_plans_preserve_old_ids(self):
        with tempfile.TemporaryDirectory() as folder:
            pack,report=create(folder)
            data=json.loads((pack/'interaction_texts.json').read_text(encoding='utf8'))
            self.assertEqual((data['version'],len(data['image_bindings']),len(data['action_texts'])),(3,64,24))
            self.assertEqual(report['interactions']['missing_count'],0)
            self.assertEqual(len(data['entries']),3*32+2*188)
            source=Path(folder)/'source';registry=SubjectRegistry(source/'interaction_subjects.json')
            store=InteractionTextStore(source/'interaction_texts.json',published=True,subject_registry=registry)
            old=make_entry(condition(),['元のミケ専用文','','']);store.save(old)
            settings=InteractionLlmSettings(source/'settings.json',subject_registry=registry)
            all_jobs=prepare_jobs(settings,['player'],list(MODES),scope='all',target_cat='*')
            self.assertEqual(len(all_jobs),32)
            cat_jobs=prepare_jobs(settings,['author-tama'],list(MODES),kind='cat_response',scope='all')
            common=prepare_jobs(settings,['author-tama'],list(MODES),kind='cat_response')
            self.assertEqual((len(cat_jobs),len(common)),(188,50))
            completed={key(v) for v,_ in all_jobs if v['mode'] in ('teaser','pet','pause')}
            missing=prepare_jobs(settings,['player'],list(MODES),scope='all',target_cat='*',has_text=lambda v:key(v) in completed)
            self.assertEqual(len(missing),23)
            self.assertEqual(store.get(condition()),old)
            for mode in MODES:
                for action in actions(mode):
                    if action=='any':continue
                    value=condition('cat_response','author-tama',mode,action,reactions('cat_response',mode)[0],subjects=registry.labels())
                    found=select_entry(store.read()['entries'],value)
                    self.assertTrue(found['candidates'])
                    preview=compose_preview('鈴木さん',mode,action,value['reaction'],'{cat_call}、こちらだよ。',found['candidates'][0]['text'],cat_name='タマ',call='{cat}ちゃん')
                    self.assertIn('タマちゃん',preview);self.assertNotIn('ミケ',preview)

    def test_presence_and_voice_generation_prompts_keep_noncontact_constraints(self):
        with tempfile.TemporaryDirectory() as folder:
            client=InteractionLlmClient()
            for mode in ('ball','plush','tunnel','brush','voice','presence'):
                value=condition(mode=mode,action='adapt',target_cat='*')
                response={'choices':[{'message':{'content':json.dumps({'variants':['{cat_call}、こちらだよ。','少し待とうね。','ゆっくり付き合ってね。']},ensure_ascii=False)}}]}
                with patch.object(client,'_request',return_value=response) as request:
                    client.generate_variants('test-model',value,'人物の口調資料','姿勢は未確認','短い文',threading.Event(),output=folder)
                prompt=request.call_args.args[1]['messages'][1]['content']
                if mode=='presence':self.assertIn('接触を追加せず',prompt)
                if mode=='voice':self.assertIn('人間の言葉で会話させない',prompt)
                self.assertIn('{cat_call}',prompt)


@unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI')=='1','requires display')
class AllModeGuiTests(unittest.TestCase):
    def test_new_modes_batch_save_and_saved_condition_navigation(self):
        import tkinter as tk
        from interaction_text_editor_gui import InteractionTextEditorWindow
        with tempfile.TemporaryDirectory() as folder:
            registry=SubjectRegistry(Path(folder)/'subjects.json',settings_path=Path(folder)/'settings.json')
            row=registry.get('player');row['notes']='確認済みの穏やかな口調';registry.save(row)
            settings=InteractionLlmSettings(Path(folder)/'settings.json',subject_registry=registry)
            data=settings.read();data['model']='test-model';settings.path.write_text(json.dumps(data),encoding='utf8')
            drafts=InteractionTextStore(Path(folder)/'drafts.json',subject_registry=registry)
            published=InteractionTextStore(Path(folder)/'published.json',published=True,subject_registry=registry)
            root=tk.Tk();root.withdraw()
            try:
                editor=InteractionTextEditorWindow(root,drafts,published,subject_registry=registry,generation_settings=settings,generation_output=Path(folder)/'records')
                editor.open_batch_generation();batch=editor.batch_window
                for mode,var in batch.modes.items():var.set(mode in ('ball','plush','tunnel','brush','voice','presence'))
                with patch('interaction_batch_gui.InteractionLlmClient.generate_variants',return_value=(['少し付き合ってね。','様子を見ながらにしよう。','ゆっくりでいいよ。'],Path(folder)/'raw.json')):
                    batch.start();deadline=time.monotonic()+5
                    while batch.running and time.monotonic()<deadline:root.update();time.sleep(.01)
                self.assertFalse(batch.running);self.assertEqual(len(batch.results),23)
                self.assertTrue(batch.save_completed())
                for entry in batch.completed_entries():
                    label=next(label for label,c in editor.saved_conditions.items() if c==entry['condition'])
                    editor.saved_choice.set(label);self.assertTrue(editor.open_saved_condition());self.assertEqual(editor.values(),entry)
                batch.window.geometry('980x840+0+0');root.update()
                for widget in (batch.preview,batch.draft_button,batch.open_saved_button):
                    self.assertGreater(widget.winfo_height(),10)
                    self.assertLessEqual(widget.winfo_rootx()+widget.winfo_width(),batch.window.winfo_rootx()+batch.window.winfo_width())
            finally:root.destroy()


if __name__=='__main__':unittest.main()
