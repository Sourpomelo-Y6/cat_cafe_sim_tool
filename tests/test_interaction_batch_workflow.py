import copy
import os
from pathlib import Path
import tempfile
import time
import unittest
from unittest.mock import patch

from test_interaction_batch import settings_at
from interaction_batch_gui import InteractionBatchWindow
from interaction_texts import InteractionTextStore, condition, key, make_entry


class AtomicSaveTests(unittest.TestCase):
    def test_bulk_conflict_and_invalid_entry_write_nothing(self):
        with tempfile.TemporaryDirectory() as folder:
            store=InteractionTextStore(Path(folder)/'drafts.json')
            first=make_entry(condition(),['元の文','',''])
            other=make_entry(condition(mode='pet'),['他の文','',''])
            store.save(first);before=store.path.read_bytes()
            with self.assertRaises(ValueError):
                store.save_many([first,other],expected={key(first['condition']):None,key(other['condition']):None})
            self.assertEqual(before,store.path.read_bytes())
            bad=copy.deepcopy(other);bad['variants'][0]['id']='bad'
            with self.assertRaises(ValueError):store.save_many([first,bad])
            self.assertEqual(before,store.path.read_bytes())
            with patch.object(Path,'replace',side_effect=OSError('disk failure')):
                with self.assertRaises(OSError):store.save_many([first,other])
            self.assertEqual(before,store.path.read_bytes())
            self.assertFalse(list(Path(folder).glob('*.tmp')))
            store.save_many([first,other],expected={key(first['condition']):first,key(other['condition']):None})
            self.assertEqual(len(store.read()['entries']),2)


@unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI')=='1','requires display')
class WorkflowTests(unittest.TestCase):
    def setUp(self):
        import tkinter as tk
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=tk.Tk();self.root.withdraw();self.addCleanup(self.root.destroy)
        self.settings=settings_at(self.temp.name)
        self.drafts=InteractionTextStore(Path(self.temp.name)/'drafts.json')
        self.published=InteractionTextStore(Path(self.temp.name)/'published.json',published=True)
        self.output=Path(self.temp.name)/'records'
        self.window=self.create()

    def create(self):
        batch=InteractionBatchWindow(self.root,lambda e:True,settings=self.settings,output=self.output,
            drafts=self.drafts,published=self.published)
        for mode,var in batch.modes.items():var.set(mode in ('teaser','pet','pause'))
        return batch

    def wait(self):
        deadline=time.monotonic()+5
        while self.window.running and time.monotonic()<deadline:self.root.update();time.sleep(.01)
        self.assertFalse(self.window.running)

    def fake(self,*args,**kwargs):return ['一つ目の案','二つ目の案','三つ目の案'],self.output/'record.json'

    def test_failure_continues_and_survives_resume_then_retry(self):
        calls=[]
        def generate(model,value,*args,**kwargs):
            calls.append(key(value))
            if len(calls)==2:raise ValueError('不正なJSONの検証用エラー')
            return self.fake()
        with patch('interaction_batch_gui.InteractionLlmClient.generate_variants',side_effect=generate):
            self.window.start();self.wait()
            self.assertEqual(len(calls),6)
            self.assertEqual(len(self.window.results),5)
            failed=calls[1];self.assertIn(failed,self.window.skipped)
            self.assertIn('不正なJSON',self.window.errors[failed])
            completed=set(self.window.results)
            self.window.window.destroy();self.window=self.create();self.window.resume()
            self.assertEqual(set(self.window.results),completed)
            self.window.start();self.assertEqual(len(calls),6)
            self.window.tree.selection_set(failed);self.window.unskip_selected()
            self.window.start();self.wait()
        self.assertEqual(len(calls),7)
        self.assertEqual(set(self.window.results),completed|{failed})
        self.assertNotIn(failed,self.window.errors)
        self.assertFalse(self.window.skipped)

    def test_manual_skip_after_failure_continues_remaining(self):
        self.window.continue_errors.set(False);calls=[]
        def generate(model,value,*args,**kwargs):
            calls.append(key(value))
            if len(calls)==2:raise ValueError('毎回失敗する条件')
            return self.fake()
        with patch('interaction_batch_gui.InteractionLlmClient.generate_variants',side_effect=generate):
            self.window.start();self.wait()
            self.window.tree.selection_set(calls[1]);self.window.skip_selected();self.wait()
        self.assertEqual(len(calls),6)
        self.assertEqual(len(self.window.results),5)
        self.assertEqual(len(self.window.skipped),1)

    def test_bulk_save_review_register_and_exclusion(self):
        with patch('interaction_batch_gui.InteractionLlmClient.generate_variants',side_effect=self.fake):
            self.window.start();self.wait()
        excluded=next(iter(self.window.results))
        self.window.tree.selection_set(excluded);self.window.skip_selected()
        self.assertIn(excluded,self.window.excluded)
        with patch.object(self.drafts,'save_many',wraps=self.drafts.save_many) as save:
            self.assertTrue(self.window.save_completed())
            self.assertEqual(save.call_count,1)
        self.assertEqual(len(self.drafts.read()['entries']),5)
        self.assertFalse(self.published.path.exists())
        self.assertFalse(self.window.save_completed(True))
        self.window.review_completed();self.root.update()
        self.assertFalse(self.published.path.exists())
        def descendants(widget):
            for child in widget.winfo_children():yield child;yield from descendants(child)
        button=next(w for w in descendants(self.window.review_window)
            if w.winfo_class()=='TButton' and 'まとめて登録' in w.cget('text'))
        button.invoke()
        self.assertEqual(len(self.published.read()['entries']),5)
        self.assertEqual(self.drafts.read()['entries'],self.published.read()['entries'])
        self.window.window.destroy();self.window=self.create();self.window.resume()
        self.assertIn(excluded,self.window.excluded)
        self.assertTrue(self.window.save_completed()) # Same already-saved content is idempotent.

    def test_bulk_rejects_concurrent_edit_without_partial_write(self):
        with patch('interaction_batch_gui.InteractionLlmClient.generate_variants',side_effect=self.fake):
            self.window.start();self.wait()
        entry=self.window.completed_entries()[0]
        other=make_entry(entry['condition'],['他の編集者の文','',''])
        self.drafts.save(other);before=self.drafts.path.read_bytes()
        self.assertFalse(self.window.save_completed())
        self.assertEqual(self.drafts.path.read_bytes(),before)
        self.assertFalse(self.published.path.exists())
        # A stale review must not publish results removed after opening it.
        reviewed=self.window.completed_entries()
        self.window.excluded.add(key(reviewed[0]['condition']))
        self.assertFalse(self.window.save_completed(True,reviewed))
        self.assertFalse(self.published.path.exists())

    def test_generic_default_and_editor_dirty_protection(self):
        from interaction_text_editor_gui import InteractionTextEditorWindow
        editor=InteractionTextEditorWindow(self.root,self.drafts,self.published,
            generation_settings=self.settings,generation_output=self.output)
        self.assertEqual(editor.values()['condition']['target_cat'],'*')
        self.assertEqual(editor.target_cat.get(),'どの猫でも使う（{cat_call}）')
        editor.load_condition(condition('cat_response','cat-mike',reaction='neutral'))
        editor.choices['kind'].set('人物のセリフ');editor.selection_changed()
        self.assertEqual(editor.values()['condition']['target_cat'],'*')
        editor.open_batch_generation();self.window=editor.batch_window
        with patch('interaction_batch_gui.InteractionLlmClient.generate_variants',side_effect=self.fake):
            self.window.start();self.wait()
        editor.fields[0].insert('1.0','編集中の文章')
        self.assertFalse(self.window.save_completed())
        self.assertFalse(self.drafts.path.exists())
        self.assertIn('編集中の文章',editor.values()['variants'][0]['text'])

    def test_footer_fits_minimum_size(self):
        self.window.window.geometry('980x780+0+0');self.root.update()
        for widget in (self.window.skip_button,self.window.retry_button,self.window.draft_button,self.window.review_button,self.window.preview):
            self.assertGreater(widget.winfo_height(),10)
            self.assertLessEqual(widget.winfo_rootx()+widget.winfo_width(),self.window.window.winfo_rootx()+self.window.window.winfo_width())
            self.assertLessEqual(widget.winfo_rooty()+widget.winfo_height(),self.window.window.winfo_rooty()+self.window.window.winfo_height())


if __name__=='__main__':unittest.main()
