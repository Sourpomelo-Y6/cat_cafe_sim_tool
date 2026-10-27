import copy
import json
import os
import tempfile
import threading
import unittest
from pathlib import Path
from unittest.mock import patch

from interaction_texts import (condition, make_entry, key, InteractionTextStore,
                               MODES, NARRATIVE_SCENES, scene_actions)
from interaction_presentation import select_entry, coverage
from interaction_subjects import SubjectRegistry
from interaction_llm import InteractionLlmClient, InteractionLlmSettings
from interaction_batch_gui import prepare_jobs


class SpecialNarratives(unittest.TestCase):
    def test_conditions_keys_store_and_scope(self):
        old=make_entry(condition(),['旧本文','',''])
        with tempfile.TemporaryDirectory() as folder:
            store=InteractionTextStore(Path(folder)/'draft.json')
            store.save(old)
            for mode in MODES:
                for scene in NARRATIVE_SCENES:
                    if scene=='normal':continue
                    for action in scene_actions(mode,scene):
                        value=condition(mode=mode,action=action,narrative_scene=scene)
                        entry=make_entry(value,['専用本文','',''])
                        store.save(entry)
                        self.assertEqual(store.get(value),entry)
                        self.assertFalse(select_entry([entry],condition())['candidates'])
            self.assertEqual(store.read()['version'],3)
            self.assertEqual(store.get(condition()),old)
            self.assertNotIn('/scene=',key(condition()))
            self.assertEqual(coverage([old])['missing_count'],coverage(store.read()['entries'])['missing_count'])
            bad=store.read();bad['version']=1
            store.path.write_text(json.dumps(bad),encoding='utf8')
            with self.assertRaises(ValueError):store.read()
        for scene,action in [('connect','direct'),('simultaneous','switch'),('switch','any'),('open_up','connect')]:
            with self.assertRaises(ValueError):condition(action=action,narrative_scene=scene)
        with self.assertRaises(ValueError):condition(mode='pause',action='pause',narrative_scene='open_up')

    def test_priority_and_independent_sides(self):
        query=condition(action='direct',target_cat='cat-mike',narrative_scene='open_up')
        shared=make_entry(condition(target_cat='*',narrative_scene='open_up'),['共通','',''])
        specific=make_entry(condition(target_cat='cat-mike',narrative_scene='open_up'),['猫専用','',''])
        normal=make_entry(condition(action='direct',target_cat='cat-mike'),['通常','',''])
        self.assertEqual(select_entry([normal,shared,specific],query)['candidates'][0]['text'],'猫専用')
        self.assertEqual(select_entry([normal,shared],query)['candidates'][0]['text'],'共通')
        self.assertFalse(select_entry([normal],query)['candidates'])
        cat=condition('cat_response','cat-mike',action='direct',narrative_scene='open_up')
        self.assertFalse(select_entry([specific],cat)['candidates'])

    def test_generation_profile_scene_and_frozen_batch(self):
        with tempfile.TemporaryDirectory() as folder:
            registry=SubjectRegistry(Path(folder)/'subjects.json')
            registry.read() # default adopted fixture profiles
            settings=InteractionLlmSettings(Path(folder)/'settings.json',subject_registry=registry)
            jobs=prepare_jobs(settings,['player'],['teaser'],narrative_scene='connect')
            self.assertEqual(len(jobs),1)
            value,notes=jobs[0]
            frozen=copy.deepcopy(jobs)
            prepare_jobs(settings,['player'],['teaser'],narrative_scene='switch')
            self.assertEqual(jobs,frozen)
            client=InteractionLlmClient(subject_registry=registry)
            answer={'choices':[{'message':{'content':json.dumps({'variants':['ひとつ','ふたつ','みっつ']})}}]}
            with patch.object(client,'_request',return_value=answer) as request:
                client.generate_variants('test',value,notes['subject_notes'],notes['image_notes'],'短く',threading.Event(),output=folder)
            material=json.loads(request.call_args.args[1]['messages'][1]['content'].split('資料：')[1])
            self.assertEqual(material['narrative_scene'],'心をつかむ')
            self.assertEqual(material['adopted_profile'],registry.get('player')['notes'])

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI')=='1','requires display')
    def test_editor_preview_and_batch_reopen(self):
        import tkinter as tk
        from interaction_text_editor_gui import InteractionTextEditorWindow
        from interaction_batch_gui import InteractionBatchWindow
        from interaction_preview_gui import InteractionPreviewWindow
        from content_images import ImageRegistry
        root=tk.Tk();root.withdraw()
        try:
            with tempfile.TemporaryDirectory() as folder:
                drafts=InteractionTextStore(Path(folder)/'draft.json')
                published=InteractionTextStore(Path(folder)/'published.json',published=True)
                editor=InteractionTextEditorWindow(root,drafts,published)
                for scene in list(NARRATIVE_SCENES)[1:]:
                    value=condition(action='direct' if scene=='open_up' else 'switch' if scene=='switch' else 'connect',narrative_scene=scene)
                    self.assertTrue(editor.load_condition(value))
                    editor.fields[0].insert('1.0','専用の一案')
                    self.assertTrue(editor.save_draft());editor.preview()
                    self.assertTrue(editor.register());self.assertTrue(editor.reload())
                    self.assertEqual(editor.values(),published.get(value))
                    preview=InteractionPreviewWindow(editor.window,{'playtest-mike':'ミケ'},ImageRegistry(folder),published,editing_entry=editor.values())
                    root.update()
                    self.assertIn('専用文',preview.source_notice.get())
                    self.assertIn('専用の一案',preview.body.get('1.0','end-1c'))
                    preview.variables['human'].set('佐藤さん');preview.render()
                    self.assertIn('フォールバック',preview.source_notice.get())
                    preview.window.destroy()
                editor.close()
                batch=InteractionBatchWindow(root,lambda e:True,output=folder)
                batch.narrative_scene.set(NARRATIVE_SCENES['switch'])
                self.assertFalse(batch.modes['pause'].get())
                self.assertTrue(batch.mode_widgets['pause'].instate(['disabled']))
                batch.narrative_scene.set(NARRATIVE_SCENES['normal'])
                self.assertFalse(batch.mode_widgets['pause'].instate(['disabled']))
                settings=InteractionLlmSettings(Path(folder)/'settings.json')
                settings.save(condition(),'http://localhost:8080','model','採用資料','画像の資料','短く')
                value=condition(action='connect',narrative_scene='connect')
                notes=settings.get(condition())
                batch.jobs=[(value,notes)];batch.checkpoint();batch.jobs=[];batch.resume()
                self.assertEqual(batch.jobs[0][0],value)
                batch.window.destroy()
        finally:root.destroy()
