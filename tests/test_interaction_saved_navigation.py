import os
from pathlib import Path
import tempfile
import time
import unittest
from unittest.mock import patch

from test_interaction_batch import settings_at
from interaction_texts import InteractionTextStore,condition,key,make_entry
from interaction_text_editor_gui import InteractionTextEditorWindow


@unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI')=='1','requires display')
class SavedNavigationTests(unittest.TestCase):
    def setUp(self):
        import tkinter as tk
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=tk.Tk();self.root.withdraw();self.addCleanup(self.root.destroy)
        self.drafts=InteractionTextStore(Path(self.temp.name)/'drafts.json')
        self.published=InteractionTextStore(Path(self.temp.name)/'published.json',published=True)
        self.settings=settings_at(self.temp.name)
        self.editor=InteractionTextEditorWindow(self.root,self.drafts,self.published,
            generation_settings=self.settings,generation_output=Path(self.temp.name)/'records')
        self.root.update()

    def generate_guest_pause(self):
        self.editor.open_batch_generation();batch=self.editor.batch_window
        for name,var in batch.subjects.items():var.set(name=='guest-1')
        for name,var in batch.modes.items():var.set(name=='pause')
        with patch('interaction_batch_gui.InteractionLlmClient.generate_variants',return_value=(['ひと息つきましょう。','少し待ちましょう。','ここで休みましょう。'],Path(self.temp.name)/'raw.json')):
            batch.start();deadline=time.monotonic()+5
            while batch.running and time.monotonic()<deadline:self.root.update();time.sleep(.01)
        self.assertFalse(batch.running)
        self.assertEqual(len(batch.results),1)
        return batch,batch.completed_entries()[0]

    def test_bulk_save_moves_individual_editor_to_saved_selection(self):
        batch,entry=self.generate_guest_pause()
        self.assertNotEqual(self.editor._condition,entry['condition'])
        self.assertTrue(batch.save_completed())
        self.assertEqual(self.editor.values(),entry)
        self.assertFalse(self.editor.dirty())
        self.assertEqual(self.editor.choices['subject_id'].get(),'佐藤さん')
        self.assertEqual(self.editor.choices['mode'].get(),'休憩')
        self.assertIn('下書き3・登録0',self.editor.saved_choice.get())
        batch.review_completed();self.assertTrue(batch.save_completed(True,batch.completed_entries()))
        self.assertIn('下書き3・登録3',self.editor.saved_choice.get())

    def test_open_saved_from_batch_reads_file_instead_of_unsaved_result(self):
        batch,entry=self.generate_guest_pause()
        self.assertFalse(batch.open_saved_selected())
        self.assertTrue(batch.save_completed())
        self.editor.load_condition(condition(target_cat='*'))
        persisted=make_entry(entry['condition'],['保存後に校正した文','',''])
        self.drafts.save(persisted)
        self.assertTrue(batch.open_saved_selected())
        self.assertEqual(self.editor.values(),persisted)
        self.assertFalse(self.editor.dirty())

    def test_saved_list_keeps_generic_and_legacy_distinct_and_draft_priority(self):
        generic=make_entry(condition(target_cat='*'),['共通文','',''])
        legacy=make_entry(condition(),['従来の文','',''])
        explicit=make_entry(condition(target_cat='cat-mike'),['個別指定の文','',''])
        cat=make_entry(condition('cat_response','cat-mike',mode='pause',action='pause',reaction='listless'),['猫の低体力の文','',''])
        self.published.save_many([generic,legacy,explicit,cat])
        draft=make_entry(generic['condition'],['校正中の共通文','','']);self.drafts.save(draft)
        self.editor.refresh_saved_conditions()
        self.assertEqual(len(self.editor.saved_conditions),4)
        for expected in (draft,legacy,explicit,cat):
            label=next(label for label,c in self.editor.saved_conditions.items() if c==expected['condition'])
            self.editor.saved_choice.set(label);self.assertTrue(self.editor.open_saved_condition())
            self.assertEqual(self.editor.values(),expected)
            self.assertFalse(self.editor.dirty())
        self.assertEqual(self.editor.choices['reaction'].get(),'低体力')

    def test_bulk_navigation_preserves_other_dirty_condition(self):
        batch,entry=self.generate_guest_pause()
        self.editor.fields[0].insert('1.0','未保存のプレイヤー文')
        before=self.editor.values()
        self.assertTrue(batch.save_completed())
        self.assertEqual(self.editor.values(),before)
        with patch('tkinter.messagebox.askyesnocancel',return_value=None):
            self.assertFalse(batch.open_saved_selected())
            label=next(label for label,c in self.editor.saved_conditions.items() if c==entry['condition'])
            self.editor.saved_choice.set(label);self.assertFalse(self.editor.open_saved_condition())
        self.assertEqual(self.editor.values(),before)
        self.assertTrue(self.editor.dirty())

    def test_saved_list_refreshes_after_individual_save(self):
        self.editor.fields[0].insert('1.0','個別に保存した文')
        self.assertTrue(self.editor.save_draft())
        self.assertEqual(len(self.editor.saved_conditions),1)
        self.assertIn('下書き1・登録0',self.editor.saved_choice.get())
        self.editor.preview();self.assertTrue(self.editor.register())
        self.assertIn('下書き1・登録1',self.editor.saved_choice.get())


if __name__=='__main__':unittest.main()
