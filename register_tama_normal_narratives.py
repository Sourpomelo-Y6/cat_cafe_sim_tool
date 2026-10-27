"""タマの6動作の通常反応を既存Toolの編集・確認登録経路で再現する。"""
import hashlib
import json
import tkinter as tk
from pathlib import Path
from content_images import ImageRegistry
from interaction_subjects import SubjectRegistry
from interaction_texts import InteractionTextStore, DEFAULT_DRAFT_PATH, condition, make_entry
from interaction_text_editor_gui import InteractionTextEditorWindow
from interaction_preview_gui import InteractionPreviewWindow
from interaction_presentation import resolve_image, select_entry

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / 'cat_cafe_sim/saves/content_images'
OUT = Path(__file__).parent / 'Docs/Results/TamaNormalNarratives'
from tama_normal_texts import SID, TEXTS, ACTIONS
import sys
TEXTS = {k: v + [''] for k, v in TEXTS.items()}



def main():
    subjects = SubjectRegistry(BASE / 'interaction_subjects.json')
    pub = InteractionTextStore(BASE / 'interaction_texts.json', published=True, subject_registry=subjects)
    drafts = InteractionTextStore(DEFAULT_DRAFT_PATH, subject_registry=subjects)
    images = ImageRegistry(BASE)
    before, dbefore = pub.read(), drafts.read()
    conditions = {(m,r): condition('cat_response', SID, m, 'any', r, subjects=subjects.labels()) for m,r in TEXTS}
    for c in conditions.values():
        assert pub.get(c) is None and drafts.get(c) is None, '既存条件があるため中止'
    generated = list((ROOT / 'cat_cafe_sim_tool/generated/interaction_texts').rglob('*.json'))
    assert not any(json.loads(p.read_text(encoding='utf-8')).get('condition', {}).get('subject_id') == SID and json.loads(p.read_text(encoding='utf-8')).get('condition', {}).get('mode') in tuple(ACTIONS) for p in generated)
    protected = [BASE / 'interaction_subjects.json', BASE / 'manifest.json'] + generated
    hashes = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in protected}
    # 全対象操作別も再照合。既存の文章・下書きは作り直さない。
    for data in (before, dbefore):
        assert not any(e['condition']['subject_id'] == SID and e['condition']['mode'] in ACTIONS for e in data['entries'])
    texts = [t for rows in TEXTS.values() for t in rows if t]
    assert len(texts) == len(set(texts)) == 44
    assert all(t.startswith('{cat}') and '{' not in t.replace('{cat}', '') and '}' not in t.replace('{cat}', '') for t in texts)
    root = tk.Tk(); root.withdraw()
    if '--review' in sys.argv:
        review(root, subjects, images, pub, drafts, conditions)
        return
    editor = InteractionTextEditorWindow(root, drafts, pub, subject_registry=subjects, image_registry=images, catalog_provider=lambda: subjects.image_catalog({}))
    entries, previews = {}, []
    # すべての条件で画像付きプレビューを先に確認する。
    for (mode, reaction), c in conditions.items():
        entry = make_entry(c, TEXTS[(mode, reaction)]); entries[(mode, reaction)] = entry
        preview = InteractionPreviewWindow(root, subjects.image_catalog({}), images, pub, editing_entry=entry, drafts=drafts)
        root.update()
        for action in ACTIONS[mode]:
            from interaction_scenes import ACTION_LABELS
            preview.variables['action'].set(ACTION_LABELS[action])
            for slot, text in enumerate(TEXTS[(mode, reaction)][:2], 1):
                preview.variables['cat'].set('案' + str(slot)); preview.render(); root.update()
                body = preview.body.get('1.0', 'end-1c')
                assert text.replace('{cat}', 'タマ') in body and preview.image is not None
                image = resolve_image(images, 'playtest-tama', mode, action)
                previews.append(dict(mode=mode, reaction=reaction, action=action, slot=slot, body=body, source=preview.source_notice.get(), image=str(image['source_path']), resolved=image['resolved_scene_key']))
        preview.window.destroy()
    # 登録は単一条件ずつ既存画面の確認機構を通す。
    for (mode, reaction), c in conditions.items():
        assert editor.load_condition(c)
        for widget, text in zip(editor.fields, TEXTS[(mode, reaction)]):
            widget.delete('1.0', 'end'); widget.insert('1.0', text)
        root.update(); assert editor.values() == entries[(mode, reaction)]
        assert editor.save_draft(); editor.preview(); assert editor.reviewed == entries[(mode, reaction)]
        assert editor.register(), editor.notice.get()
        assert editor.reload() and editor.values() == entries[(mode, reaction)]
    fresh = InteractionTextStore(pub.path, published=True, subject_registry=subjects)
    after, dafter = fresh.read(), drafts.read()
    assert len(after['entries']) == len(before['entries']) + len(entries)
    assert len(dafter['entries']) == len(dbefore['entries']) + len(entries)
    assert all(e in after['entries'] for e in before['entries']) and all(e in dafter['entries'] for e in dbefore['entries'])
    assert all(hashlib.sha256(Path(p).read_bytes()).hexdigest() == h for p, h in hashes.items())
    ids = [v['id'] for e in entries.values() for v in e['variants'] if v['text']]
    assert len(ids) == len(set(ids)) == 44
    assert not set(ids) & {v['id'] for e in before['entries'] for v in e['variants']}
    for (mode, reaction), c in conditions.items():
        assert fresh.get(c) == entries[(mode, reaction)]
        for action in ACTIONS[mode]:
            q = condition('cat_response', SID, mode, action, reaction, subjects=subjects.labels())
            assert select_entry(after['entries'], q)['candidates'] == entries[(mode, reaction)]['variants'][:2]
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'registration.json').write_text(json.dumps(dict(entries={m+'/'+r: e for (m,r),e in entries.items()}, previews=previews, before_count=len(before['entries']), after_count=len(after['entries']), draft_counts=[len(dbefore['entries']),len(dafter['entries'])], generated_count=len(generated), protected_unchanged=True, unity_imported=False), ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    root.destroy(); print('PASS', len(entries), len(previews))

def review(root, subjects, images, pub, drafts, conditions):
    # 保存なし。F8で次の条件、F9で次の候補へ進む画像付き校正用。
    items = list(conditions)
    state = {'index': 0, 'slot': 1, 'preview': None}
    def show(event=None):
        if state['preview']: state['preview'].window.destroy()
        mode, reaction = items[state['index']]
        preview = InteractionPreviewWindow(root, subjects.image_catalog({}), images, pub,
            editing_entry=make_entry(conditions[(mode,reaction)], TEXTS[(mode,reaction)]), drafts=drafts)
        state['preview'] = preview
        preview.window.title('タマ通常反応・校正 F8次条件 F9次候補')
        preview.variables['cat'].set('案'+str(state['slot'])); preview.render()
        preview.window.bind('<F8>', next_condition)
        preview.window.bind('<F9>', next_slot)
    def next_condition(event=None):
        state['index'] = (state['index']+1) % len(items); state['slot'] = 1; show()
    def next_slot(event=None):
        state['slot'] = 3-state['slot']
        p=state['preview']; p.variables['cat'].set('案'+str(state['slot'])); p.render()
    show(); root.mainloop()

if __name__ == '__main__': main()
