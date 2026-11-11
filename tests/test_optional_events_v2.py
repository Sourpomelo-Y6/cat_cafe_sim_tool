import copy
import json
import os
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
from optional_events import (OptionalEventStore, default_event, validate_event, event_slot,
                             validate_pack, normalize_event, blank_event, read_pack, definition_text)


def authored(cid):
    row = blank_event(cid)
    row.update(title='午後のひととき', accept_caption='待つ', decline_caption='声をかける')
    for branch in ('introduction', 'accepted', 'declined'):
        for line in row[branch]:
            line['text'] = '窓辺で静かな時間が流れた。'
    return validate_event(row, require_revision=False)


class OptionalEventsV2Tests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.store = OptionalEventStore(self.root / 'events.json')

    def test_independent_registration_and_exclusion(self):
        for cid in ('playtest-sora', 'playtest-tama'):
            self.store.save(dict(version=1, enabled=True, event=authored(cid)))
        data = self.store.read('playtest-sora'); data['enabled'] = False; self.store.save(data)
        pack = self.store.export_data({'playtest-mike': '', 'playtest-tama': ''})
        self.assertEqual({e['content_cat_id'] for e in pack['events']}, {'playtest-mike', 'playtest-tama'})
        self.assertEqual(len(self.store.read_all()['entries']), 3)
        with self.assertRaises(ValueError): self.store.export_data({})

    def test_one_family_can_publish_multiple_milestones(self):
        for content_id, milestone in (('playtest-mike', 40), ('playtest-tama', 20), ('playtest-sora', 20)):
            event = authored(content_id)
            event.pop('revision')
            event.update(event_id=f'optional.{content_id}.affinity-{milestone}', milestone=milestone, min_affinity=milestone)
            self.store.save(dict(version=1, enabled=True, event=event))
        catalog = {cid: cid for cid in ('playtest-mike', 'playtest-tama', 'playtest-sora')}
        exported = self.store.export_data(catalog)
        self.assertEqual({event_slot(e) for e in exported['events']}, {
            ('playtest-mike', 20), ('playtest-mike', 40), ('playtest-tama', 20), ('playtest-sora', 20),
        })
        self.assertEqual(self.store.read('playtest-mike', 40)['event']['milestone'], 40)
        self.assertEqual(self.store.read('playtest-mike', 20)['event']['event_id'], 'optional.mike.afternoon')

    def test_legacy_read_export_migration_and_failure(self):
        old = dict(version=1, enabled=True, event=validate_event(default_event(), require_revision=False))
        raw = json.dumps(old).encode() + b'\n'; self.store.path.write_bytes(raw)
        self.assertEqual(self.store.read()['event']['target_kind'], 'individual')
        self.assertEqual(self.store.export_data({'playtest-mike': ''})['version'], 2)
        self.assertEqual(self.store.path.read_bytes(), raw)
        backup = self.root / 'events.v1.json'; backup.write_bytes(b'other')
        with self.assertRaises(ValueError): self.store.save(self.store.read())
        self.assertEqual(self.store.path.read_bytes(), raw)
        backup.unlink()
        with patch('optional_events.os.replace', side_effect=OSError('busy')):
            with self.assertRaises(OSError): self.store.save(self.store.read())
        self.assertEqual(self.store.path.read_bytes(), raw)
        self.store.save(self.store.read())
        self.assertEqual(backup.read_bytes(), raw)
        self.assertEqual(json.loads(self.store.path.read_text(encoding="utf-8"))['version'], 2)

    def test_share_mike_explicitly_without_identity_change(self):
        first = self.store.read()['event']; shared = copy.deepcopy(first)
        shared['target_kind'] = 'content'; shared.pop('cat_id'); shared.pop('revision')
        shared = validate_event(shared, require_revision=False)
        self.assertEqual(first['event_id'], shared['event_id'])
        self.assertNotEqual(first['revision'], shared['revision'])
        self.assertEqual(self.store.read()['event']['target_kind'], 'individual')

    def test_variable_line_counts_change_revision_and_round_trip(self):
        event = authored('playtest-sora')
        initial_revision = event.pop('revision')
        event['accepted'].insert(0, event['introduction'].pop())
        edited = validate_event(event, require_revision=False)
        self.assertNotEqual(initial_revision, edited['revision'])
        self.store.save(dict(version=1, enabled=True, event=edited))
        pack = self.store.export_data({'playtest-mike': 'ミケ', 'playtest-sora': 'ソラ'})
        self.assertIn(edited, read_pack(definition_text(pack))['events'])
        for count in (0, 21):
            with self.assertRaises(ValueError):
                validate_event(dict(event, declined=event['declined'][:1] * count), require_revision=False)
        legacy = default_event()
        legacy['accepted'].append(legacy['introduction'].pop())
        with self.assertRaises(ValueError):
            validate_event(legacy, require_revision=False)

    def test_contract_limits_and_json_utf16(self):
        row = authored('cat-image')
        rows = [dict(row, content_cat_id='c' + str(i), event_id='optional.c' + str(i) + '.story') for i in range(64)]
        rows = [validate_event({k: v for k, v in r.items() if k != 'revision'}, require_revision=False) for r in rows]
        pack = dict(version=2, pack_id='1' * 32, events=rows); validate_pack(pack)
        bad = copy.deepcopy(pack); bad['events'].append(authored('extra'))
        with self.assertRaises(ValueError): validate_pack(bad)
        bad = copy.deepcopy(pack); bad['events'][1] = bad['events'][0]
        with self.assertRaises(ValueError): validate_pack(bad)
        for edit in ({'cat_id': 'cat-sora'}, {'target_kind': 'script'}, {'event_id': 'bad'}, {'min_day': True}):
            with self.assertRaises(ValueError): validate_event(dict(row, **edit), require_revision=True)
        text = definition_text(pack); units = len(text.encode('utf-16-le')) // 2
        read_pack(text + ' ' * (1000000 - units))
        with self.assertRaises(ValueError): read_pack(text + ' ' * (1000001 - units))

    def test_empty_import_retains_authored_text_and_corrupt_store(self):
        self.store.save(dict(version=1, enabled=True, event=authored('playtest-sora')))
        path = self.root / 'optional_events.json'; path.write_text(json.dumps(dict(version=2, pack_id='1' * 32, events=[])))
        self.store.import_pack(path)
        rows = self.store.read_all()['entries']
        self.assertTrue(all(not r['enabled'] for r in rows)); self.assertEqual(len(rows), 2)
        self.store.path.write_text('{broken')
        with self.assertRaises(ValueError): self.store.import_pack(path)
        self.assertEqual(self.store.path.read_text(), '{broken')

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'Tk callbacks')
    def test_gui_target_switch_publish_and_draft_independence(self):
        import tkinter as tk
        from optional_event_editor_gui import OptionalEventEditorWindow
        root = tk.Tk(); root.withdraw()
        try:
            draft = OptionalEventStore(self.root / 'draft.json')
            gui = OptionalEventEditorWindow(root, draft, self.store, lambda: {'playtest-mike': '同名', 'playtest-sora': '同名'})
            gui.content.set(next(k for k, v in gui.targets.items() if v == 'playtest-sora')); gui.change_target()
            gui.populate(dict(version=1, enabled=True, event=authored('playtest-sora')))
            gui.preview()
            gui.edit_lines('introduction', 'add')
            gui.lines['introduction'][1][2].insert('1.0', '追加した行')
            gui.edit_lines('introduction', 'up')
            with patch('tkinter.messagebox.askyesno', return_value=True):
                gui.edit_lines('declined', 'remove')
            gui.publish()
            self.assertFalse(self.store.path.exists())
            gui.preview(); gui.publish()
            self.assertFalse(draft.path.exists())
            event = self.store.read('playtest-sora')['event']
            self.assertEqual(event['title'], '午後のひととき')
            self.assertEqual(len(event['introduction']), 4)
            self.assertEqual(event['introduction'][0]['text'], '追加した行')
            self.assertEqual(len(event['declined']), 1)
            gui.saved_form = gui.collect(); gui.content.set(next(k for k, v in gui.targets.items() if v == 'playtest-mike')); gui.change_target()
            self.assertIsNone(gui.reviewed); self.assertEqual(gui.identity['target_kind'], 'individual')
            gui.window.update_idletasks()
            for button in gui.buttons.values():
                self.assertLessEqual(button.winfo_rooty() + button.winfo_height(), gui.window.winfo_rooty() + gui.window.winfo_height())
        finally:
            root.update(); root.destroy()

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'Tk callbacks')
    def test_gui_empty_pack_requires_whole_collection_review(self):
        import tkinter as tk
        from optional_event_editor_gui import OptionalEventEditorWindow
        root=tk.Tk();root.withdraw()
        try:
            self.store.save(dict(version=1,enabled=True,event=authored('playtest-sora')))
            draft=OptionalEventStore(self.root/'draft.json')
            original=dict(version=1,enabled=True,event=validate_event(default_event(),require_revision=False))
            raw=json.dumps(original).encode();draft.path.write_bytes(raw)
            empty=self.root/'optional_events.json';empty.write_text(json.dumps(dict(version=2,pack_id='1'*32,events=[])))
            gui=OptionalEventEditorWindow(root,draft,self.store,lambda:{'playtest-mike':'ミケ','playtest-sora':'ソラ'})
            with patch('tkinter.filedialog.askopenfilename',return_value=str(empty)),patch('tkinter.messagebox.askokcancel',return_value=True):gui.import_definition()
            self.assertTrue(gui.import_review_pending)
            self.assertEqual(len(draft.read_all()['entries']),2)
            self.assertEqual((self.root/'draft.v1.json').read_bytes(),raw)
            gui.publish();self.assertTrue(any(r['enabled'] for r in self.store.read_all()['entries']))
            gui.preview();gui.publish()
            self.assertFalse(any(r['enabled'] for r in self.store.read_all()['entries']))
            self.assertEqual(self.store.read('playtest-sora')['event']['title'],'午後のひととき')
        finally:root.update(); root.destroy()

    def test_maximum_authored_catalog_and_store_fit_json_limit(self):
        rows=[]
        for i in range(64):
            cid='c'+str(i).zfill(63);event=authored(cid)
            event.update(title='長'*30,accept_caption='待'*24,decline_caption='呼'*24)
            for branch in ('introduction','accepted','declined'):
                for line in event[branch]:line.update(speaker='話'*7,text='\n'*999+'終')
            event=validate_event({k:v for k,v in event.items() if k!='revision'},require_revision=False);rows.append(event)
        data=dict(version=2,pack_id='1'*32,events=rows)
        self.assertLessEqual(len(definition_text(data).encode('utf-16-le'))//2,1000000)
        self.store.save_all(dict(version=2,entries=[dict(enabled=True,event=e) for e in rows]))
        self.assertEqual(len(self.store.read_all()['entries']),64)

    @unittest.skipUnless(os.environ.get('CAT_CAFE_TEST_GUI') == '1', 'Tk callbacks')
    def test_gui_catalog_without_mike_and_empty_new_collection(self):
        import tkinter as tk
        from optional_event_editor_gui import OptionalEventEditorWindow
        root=tk.Tk();root.withdraw()
        try:
            draft=OptionalEventStore(self.root/'draft.json')
            gui=OptionalEventEditorWindow(root,draft,self.store,lambda:{'playtest-sora':'ソラ'})
            gui.populate(dict(version=1,enabled=True,event=authored('playtest-sora')));gui.preview();gui.publish()
            self.assertEqual([r['event']['content_cat_id'] for r in self.store.read_all()['entries']],['playtest-sora'])
            empty=self.root/'empty.json';empty.write_text(json.dumps(dict(version=2,pack_id='1'*32,events=[])))
            fresh=OptionalEventStore(self.root/'fresh.json');fresh_draft=OptionalEventStore(self.root/'fresh-draft.json')
            view=OptionalEventEditorWindow(root,fresh_draft,fresh,lambda:{'playtest-sora':'ソラ'})
            with patch('tkinter.filedialog.askopenfilename',return_value=str(empty)),patch('tkinter.messagebox.askokcancel',return_value=True):view.import_definition()
            view.preview();view.publish()
            self.assertEqual(fresh.read_all()['entries'],[])
        finally:root.update();root.destroy()
