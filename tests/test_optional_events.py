import copy
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from optional_events import OptionalEventStore, default_event, validate_event, validate_pack, export_event_pack, read_json


class OptionalEventTests(unittest.TestCase):
    def test_revisions_follow_content_not_export_identity(self):
        event = validate_event(default_event(), require_revision=False)
        self.assertEqual(event, validate_event(event))
        changed = copy.deepcopy(event)
        changed['introduction'][0]['text'] += ' 🐈'
        changed.pop('revision')
        changed = validate_event(changed, require_revision=False)
        self.assertNotEqual(event['revision'], changed['revision'])
        for field, value in [('title', '改稿'), ('min_day', 3), ('min_affinity', 21), ('accept_caption', '待ってみる')]:
            row = default_event();row[field] = value
            self.assertNotEqual(event['revision'], validate_event(row, require_revision=False)['revision'])

    def test_invalid_contracts_and_tampering(self):
        event = validate_event(default_event(), require_revision=False)
        with self.assertRaises(ValueError):validate_event(dict(event,revision='a'*64))
        with self.assertRaises(ValueError):read_json('{"version":1,"version":1}')
        for update in [ {'min_day': True}, {'min_day': 0}, {'min_affinity': 101}, {'cat_id': 'cat-sora'}, {'event_id': 'new-id'}, {'title': '<b>text</b>'}, {'title': '長'*31}, {'title': '題\n名'}, {'accept_caption': '長'*25}, {'accepted': []}, {'reward': 10}]:
            with self.subTest(update=update), self.assertRaises(ValueError):
                row=dict(event, **update);row.pop('revision');validate_event(row, require_revision=False)
        for events in [[event, event], None]:
            with self.assertRaises(ValueError):
                validate_pack(dict(version=1, pack_id='1'*32, events=events))
        with self.assertRaises(ValueError):
            validate_pack(dict(version=1, pack_id='1'*32, events=[event]), {'other': '猫'})

    def test_atomic_local_save_and_no_silent_repair(self):
        with tempfile.TemporaryDirectory() as folder:
            store = OptionalEventStore(Path(folder)/'events.json')
            self.assertFalse(store.path.exists())
            original = store.save(store.read())
            content = store.path.read_bytes()
            changed = copy.deepcopy(original);changed['event']['title'] = '改稿'
            with patch('optional_events.os.replace', side_effect=OSError('busy')), self.assertRaises(OSError):
                store.save(changed)
            self.assertEqual(content, store.path.read_bytes())
            store.path.write_text('{invalid', encoding='utf-8')
            with self.assertRaises(ValueError):store.read()
            self.assertEqual('{invalid', store.path.read_text(encoding='utf-8'))

    def test_edit_reopen_publish_export_reimport(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            adopted, draft = OptionalEventStore(root/'adopted.json'), OptionalEventStore(root/'draft.json')
            original = adopted.read();changed = copy.deepcopy(original)
            changed['event']['title'] = '改稿した午後'
            changed['event']['min_day'] = 3
            draft.save(changed)
            self.assertEqual(original, adopted.read())
            reopened = OptionalEventStore(draft.path).read()
            adopted.save(reopened)
            catalog = {'playtest-mike': 'ミケ'}
            first = export_event_pack(adopted, catalog, root/'exports')
            second = export_event_pack(adopted, catalog, root/'exports')
            a, b = [json.loads((p/'optional_events.json').read_text(encoding='utf-8')) for p in (first, second)]
            self.assertNotEqual(a['pack_id'], b['pack_id'])
            self.assertEqual(a['events'], b['events'])
            self.assertEqual(['optional_events.json'], [p.name for p in first.iterdir()])
            imported = OptionalEventStore(root/'imported.json')
            self.assertEqual(reopened, imported.import_pack(first/'optional_events.json'))
            disabled = adopted.read();disabled['enabled'] = False;adopted.save(disabled)
            self.assertEqual([], adopted.export_data(catalog)['events'])
            self.assertEqual([], OptionalEventStore(root/'absent.json').export_data({'other': '猫'})['events'])
            with self.assertRaises(ValueError):adopted.save(dict(disabled,event=dict(disabled['event'],title='')))

    def test_full_bundle_uses_same_contract(self):
        from unity_export import export_bundle
        from content_images import ImageRegistry
        from content_texts import TextStore
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);registry=ImageRegistry(root/'source')
            pack,_=export_bundle({'playtest-mike':'ミケ'},registry,TextStore(root/'texts.json'),root/'packs')
            bundle=json.loads((pack/'bundle.json').read_text(encoding='utf-8'))
            events=validate_pack(json.loads((pack/'optional_events.json').read_text(encoding='utf-8')),{'playtest-mike':'ミケ'})
            self.assertEqual('optional_events.json',bundle['optional_events'])
            self.assertEqual(bundle['pack_id'],events['pack_id'])

    def test_editor_draft_review_and_publish(self):
        import tkinter as tk
        from optional_event_editor_gui import OptionalEventEditorWindow
        root=tk.Tk();root.withdraw()
        try:
            with tempfile.TemporaryDirectory() as folder:
                adopted=OptionalEventStore(Path(folder)/'adopted.json')
                draft=OptionalEventStore(Path(folder)/'draft.json')
                editor=OptionalEventEditorWindow(root,draft,adopted,lambda:{'playtest-mike':'ミケ'})
                editor.window.withdraw()
                original=adopted.read()
                editor.values['title'].set('編集した午後')
                editor.publish()
                self.assertFalse(adopted.path.exists())
                editor.save_draft()
                self.assertEqual('編集した午後',draft.read()['event']['title'])
                self.assertEqual(original,adopted.read())
                editor.preview();editor.publish()
                self.assertEqual('編集した午後',adopted.read()['event']['title'])
                editor.values['min_affinity'].set('abc');editor.save_draft()
                self.assertEqual(20,draft.read()['event']['min_affinity'])
                editor.values['min_affinity'].set('20')
                editor.values['title'].set('さらに変更');editor.publish()
                self.assertEqual('編集した午後',adopted.read()['event']['title'])
                with patch('tkinter.messagebox.askyesno',return_value=False):self.assertFalse(editor.confirm_leave())
                with patch('tkinter.messagebox.askyesno',return_value=True):self.assertTrue(editor.confirm_leave())
                self.assertEqual(2,len(set(button.grid_info()['row'] for button in editor.buttons.values())))
                for child in root.winfo_children():child.destroy()
        finally:root.destroy()


if __name__ == '__main__':unittest.main()
