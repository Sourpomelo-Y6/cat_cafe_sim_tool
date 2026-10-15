import copy
import json
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from interaction_subjects import SubjectRegistry, expand, game_subjects
from interaction_texts import InteractionTextStore, condition, make_entry, key, reactions
from interaction_llm import InteractionLlmSettings
from interaction_batch_gui import prepare_jobs, InteractionBatchWindow
from interaction_presentation import select_entry, build_interaction_catalog
from content_images import ImageRegistry


def fixture(folder):
    folder=Path(folder);folder.mkdir(parents=True,exist_ok=True)
    registry=SubjectRegistry(folder/'interaction_subjects.json',settings_path=folder/'old-settings.json')
    for row in registry.read()['subjects']:
        row['notes']='確認済みの制作資料。未設定の性格・来歴を補わない。'
        registry.save(row)
    for id,kind,name,game,content in [('author-tama','cat_response','タマ','cat-tama','playtest-tama'),
                                      ('author-suzuki','speech','鈴木さん','guest-2',''),
                                      ('unbound-cat','cat_response','制作猫','','production-cat')]:
        registry.save(dict(id=id,kind=kind,name=name,game_id=game,content_id=content,
            notes='確認済みの制作資料。未設定の性格・来歴を補わない。',call='{cat}ちゃん' if kind=='speech' else '{cat}',enabled=True))
    store=InteractionTextStore(folder/'interaction_texts.json',published=True,subject_registry=registry)
    labels=registry.labels()
    # Legacy Mike-only speech must retain its condition and UUIDs.
    store.save(make_entry(condition(),['ミケ、こっち。','ミケ、遊ぼう。','ミケ、見て。']))
    for person in labels['speech']:
        for mode in ('teaser','pet','pause'):
            store.save(make_entry(condition(subject_id=person,mode=mode,action='pause' if mode=='pause' else 'any',subjects=labels,target_cat='*'),
                ['{cat_call}、ゆっくりね。','{cat_call}、こちらだよ。','{cat_call}、少し待とう。']))
    for cat in labels['cat_response']:
        for mode in ('teaser','pet','pause'):
            for reaction in reactions('cat_response',mode):
                store.save(make_entry(condition('cat_response',cat,mode,'pause' if mode=='pause' else 'any',reaction,subjects=labels),
                    ['{cat}は'+reaction+'の反応をした。','{cat}は'+reaction+'の様子を見せた。','{cat}は'+reaction+'の姿勢を取った。']))
    return registry,store


class SubjectTests(unittest.TestCase):
    def test_migration_bindings_names_and_atomic_conflicts(self):
        with tempfile.TemporaryDirectory() as folder:
            registry,store=fixture(folder)
            old=store.get(condition());ids=[v['id'] for v in old['variants']]
            snapshot=registry.read();row=registry.get('cat-mike');row['name']='改名';registry.save(row,expected=snapshot)
            self.assertEqual(ids,[v['id'] for v in store.get(condition())['variants']])
            with self.assertRaises(ValueError):registry.save(row,expected=snapshot)
            duplicate=registry.get('author-tama');duplicate['game_id']='cat-mike'
            with self.assertRaises(ValueError):registry.save(duplicate)
            self.assertEqual(expand('{cat_call}、遊ぼう。','タマ','{cat}ちゃん'),'タマちゃん、遊ぼう。')
            with self.assertRaises(ValueError):expand('{unknown}','タマ')
            self.assertIn('cat-tama',game_subjects()['cat_response'])

    def test_selection_export_and_partial_plan(self):
        with tempfile.TemporaryDirectory() as folder:
            registry,store=fixture(folder);labels=registry.labels()
            for cat in ('cat-mike','author-tama'):
                value=condition(action='intense',subjects=labels,target_cat=cat)
                found=select_entry(store.read()['entries'],value)
                self.assertEqual(found['condition'].get('target_cat'),None if cat=='cat-mike' else '*')
            catalog,report,_=build_interaction_catalog(registry.image_catalog({}),ImageRegistry(folder),store=store)
            self.assertEqual(catalog['version'],2)
            self.assertEqual(len(catalog['cat_bindings']),2)
            self.assertTrue(report['excluded_entry_count'])
            self.assertNotIn('notes',json.dumps(catalog))
            self.assertEqual(next(e for e in catalog['entries'] if e['pool_key']==key(condition()))['variants'],store.get(condition())['variants'])
            settings=InteractionLlmSettings(Path(folder)/'settings.json',subject_registry=registry)
            jobs=prepare_jobs(settings,['author-tama'],['teaser','pet','pause'],kind='cat_response',scope='all')
            self.assertEqual(len(jobs),50)
            value=condition(subject_id='author-suzuki',mode='pause',action='pause',subjects=labels,target_cat='*')
            partial=make_entry(value,['既存案','',''])
            jobs=prepare_jobs(settings,['author-suzuki'],['pause'],target_cat='*',get_entry=lambda _:partial,fill_partial=True)
            self.assertEqual(jobs[0][1]['base_entry'],partial)
            self.assertIn('既存案',jobs[0][1]['subject_notes'])


@unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI')=='1','requires display')
class SubjectGuiTests(unittest.TestCase):
    def test_register_preview_partial_fill_and_resume(self):
        import tkinter as tk
        from interaction_subject_gui import SubjectManagerWindow
        from interaction_preview_gui import InteractionPreviewWindow
        with tempfile.TemporaryDirectory() as folder:
            registry,store=fixture(folder);root=tk.Tk();root.withdraw()
            try:
                manager=SubjectManagerWindow(root,registry,published=store)
                manager.tabs.select(1);root.update();manager.new(game_subjects()['speech']['guest-3'])
                manager.notes.insert('1.0','確認済みの口調資料');self.assertTrue(manager.save())
                preview=InteractionPreviewWindow(root,registry.image_catalog({}),ImageRegistry(folder),store,drafts=store)
                preview.cat_subject.set('タマ');preview.variables['human'].set('鈴木さん');preview.render();root.update()
                self.assertIn('タマちゃん',preview.body.get('1.0','end'))
                self.assertNotIn('ミケ',preview.body.get('1.0','end'))
                self.assertIn('条件別の制作状況',preview.missing_text.get('1.0','end'))
                settings=InteractionLlmSettings(Path(folder)/'settings.json',subject_registry=registry)
                value=condition(subject_id='author-suzuki',mode='pause',action='pause',subjects=registry.labels(),target_cat='*')
                base=make_entry(value,['既存案','','']);received=[]
                output=Path(folder)/'records'
                batch=InteractionBatchWindow(root,lambda e:received.append(e) or True,settings=settings,output=output,
                    subject_registry=registry,initial_subject='author-suzuki',target_cat='*',get_entry=lambda _:base)
                batch.partial.set(True)
                for name,var in batch.modes.items():var.set(name=='pause')
                batch.model.set('test-model')
                with patch('interaction_batch_gui.InteractionLlmClient.generate_variants',return_value=(['既存案','追加二','追加三'],output/'raw.json')):
                    batch.start();until=time.monotonic()+5
                    while batch.running and time.monotonic()<until:root.update();time.sleep(.02)
                self.assertFalse(batch.running);self.assertEqual(len(batch.results),1)
                batch.apply();self.assertEqual([v['text'] for v in received[0]['variants']],['既存案','追加二','追加三'])
                batch.window.destroy();root.update()
                resumed=InteractionBatchWindow(root,lambda e:True,settings=settings,output=output,subject_registry=registry)
                resumed.resume();self.assertEqual(len(resumed.results),1)
                self.assertEqual(resumed.jobs[0][1]['base_entry'],base)
            finally:root.destroy()


if __name__=='__main__':unittest.main()
