"""ミケ・見守り限定。既存Tk編集経路を使う再現用制作記録。

初回実行は下書きと画像付き確認画面まで。目視後に画面の登録ボタンで
対象8条件だけを登録する。既存対象がある場合は上書きせず停止する。
"""
import argparse
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
import tkinter as tk
from tkinter import ttk

from content_images import ImageRegistry
from interaction_subjects import SubjectRegistry, expand
from interaction_texts import InteractionTextStore, DEFAULT_DRAFT_PATH, condition, key, NARRATIVE_SCENES
from interaction_text_editor_gui import InteractionTextEditorWindow
from interaction_preview_gui import InteractionPreviewWindow
from interaction_presentation import select_entry, resolve_image

ROOT = Path(__file__).resolve().parents[1]
OUT = Path(__file__).resolve().parent / 'Docs/Results/MikePresenceNarratives'
BASE = ROOT / 'cat_cafe_sim/saves/content_images'
TEXTS = {
    'switch': (
        ['今度は、そばでゆっくり過ごそうか。', '{cat_call}の様子を、少し見守っていようかな。'],
        ['{cat}はそばにいる相手へ、ぱっと目を向けた。', '{cat}はちょこんと座ったまま、相手の気配をうかがった。']),
    'connect': (
        ['この距離だと、目が合うね。', 'そばにいると、{cat_call}も気になるのかな。'],
        ['{cat}は見守る相手に目を向け、じっとその様子を見つめた。', 'そばの気配を追うように、{cat}の注意が相手へ向いた。']),
    'open_up': (
        ['こうしてそばにいられるの、うれしいな。', '{cat_call}と過ごす時間は、ほっとするね。'],
        ['{cat}はしっぽを立て、気を許したような顔で相手へ歩み寄った。', 'そばの相手へ、{cat}は親しげなまなざしを向けた。']),
    'simultaneous': (
        ['見守っていたら、{cat_call}がぐっと身近に感じられたよ。', 'そばでこんな顔を見られると、うれしくなるね。'],
        ['{cat}は見守る相手へ顔を向け、気を許すように目を細めた。', '相手の気配に注意を向けた{cat}は、うれしそうに口元をゆるめた。']),
}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save_record(name, data):
    folder = ROOT / 'Logs/MikePresenceNarratives' if name == 'before.json' else OUT
    folder.mkdir(parents=True, exist_ok=True)
    (folder / name).write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--resume-reviewed-drafts', action='store_true')
    resume = parser.parse_args().resume_reviewed_drafts
    OUT.mkdir(parents=True, exist_ok=True)
    subjects = SubjectRegistry(BASE / 'interaction_subjects.json')
    published = InteractionTextStore(BASE / 'interaction_texts.json', published=True, subject_registry=subjects)
    drafts = InteractionTextStore(DEFAULT_DRAFT_PATH, subject_registry=subjects)
    images = ImageRegistry(BASE)
    before, olddraft = published.read(), drafts.read()
    generated = list((ROOT / 'cat_cafe_sim_tool/generated/interaction_texts').rglob('*.json'))
    matches = []
    for path in generated:
        value = json.loads(path.read_text(encoding='utf-8')).get('condition', {})
        if value.get('subject_id') in ('player', 'cat-mike') and value.get('mode') == 'presence' and value.get('narrative_scene', 'normal') != 'normal':
            matches.append(str(path))
    assert not matches, 'Review generated candidates first'
    conditions = [condition(kind, subject, 'presence', 'any' if scene == 'open_up' else 'switch' if scene == 'switch' else 'connect',
                            subjects=subjects.labels(), target_cat='cat-mike' if kind == 'speech' else None, narrative_scene=scene)
                  for scene in TEXTS for kind, subject in [('speech', 'player'), ('cat_response', 'cat-mike')]]
    target_keys = {key(c) for c in conditions}
    assert not [e for e in before['entries'] if key(e['condition']) in target_keys], 'Already adopted: review before authoring'
    existing_drafts = [e for e in olddraft['entries'] if key(e['condition']) in target_keys]
    if resume:
        snapshot = json.loads((ROOT / 'Logs/MikePresenceNarratives/before.json').read_text(encoding='utf-8'))
        assert before == snapshot['published'], 'Published data changed: review first'
        assert len(existing_drafts) == 8
        for e in existing_drafts:
            expected = TEXTS[e['condition']['narrative_scene']][0 if e['condition']['kind'] == 'speech' else 1] + ['']
            assert [v['text'] for v in e['variants']] == expected, 'Draft changed: review first'
        assert [e for e in olddraft['entries'] if key(e['condition']) not in target_keys] == snapshot['drafts']['entries']
        olddraft = snapshot['drafts']
    else:
        assert not existing_drafts, 'Existing target drafts: review or resume the exact reviewed drafts'

    save_record('existing-normal.json', dict(published=[e for e in before['entries'] if e['condition']['mode']=='presence' and e['condition'].get('narrative_scene','normal')=='normal'], drafts=[e for e in olddraft['entries'] if e['condition']['mode']=='presence' and e['condition'].get('narrative_scene','normal')=='normal']))
    protected_paths = [BASE / 'interaction_subjects.json', BASE / 'manifest.json']
    protected_paths += list(BASE.glob('*bindings*.json'))
    protected_paths += generated
    protected = {str(p): digest(p) for p in protected_paths}
    if not resume:
        save_record('before.json', dict(published=before, drafts=olddraft, protected=protected,
                                      generated_files_reviewed=len(generated), matching_generated=matches))
    else:
        assert all(digest(Path(p)) == h for p, h in snapshot['protected'].items())
    root = tk.Tk()
    root.title('ミケ・見守り：画像確認後の対象限定登録')
    editor = InteractionTextEditorWindow(root, drafts, published, subject_registry=subjects,
                                         image_registry=images, catalog_provider=lambda: subjects.image_catalog({}))
    entries = []
    for value in conditions:
        assert editor.load_condition(value)
        texts = TEXTS[value['narrative_scene']][0 if value['kind'] == 'speech' else 1]
        for widget, text in zip(editor.fields, texts + ['']):
            widget.delete('1.0', 'end')
            widget.insert('1.0', text)
        root.update()
        assert editor.save_draft()
        editor.preview()
        assert editor.reviewed == editor.values()
        entries.append(editor.values())
    save_record('draft-review.json', dict(entries=entries, profiles=[subjects.get('player'), subjects.get('cat-mike')],
                                        unity_imported=False, route='existing Tk fields -> save_draft -> preview; manual authoring, no LLM request'))
    editor.window.withdraw()
    preview = InteractionPreviewWindow(root, subjects.image_catalog({}), images, published)
    # 作業中の両欄を一緒に見るための一時表示。保存ストアや画像対応は変更しない。
    preview.entries = before['entries'] + entries
    preview.window.title('ミケ・見守り：未登録下書きの画像付き確認（ゲーム進行なし）')
    preview.notice.set('対象8条件は未登録の下書きです。画像と両欄を確認してから対象限定登録します。')
    previews, checks = [], []
    scenes = list(TEXTS)
    def show(scene):
        preview.variables['mode'].set('そばで見守る')
        preview.variables['narrative_scene'].set(NARRATIVE_SCENES[scene])
        preview.render()
        root.update()
    for scene in scenes:
        show(scene)
        image_action = scene if scene in ('open_up', 'simultaneous') else 'any'
        resolved = resolve_image(images, 'playtest-mike', 'presence', image_action)
        assert resolved['resolved_scene_key'] == ('presence__' + scene if image_action != 'any' else 'presence')
        for slot in (1, 2):
            preview.variables['speech'].set('案' + str(slot))
            preview.variables['cat'].set('案' + str(slot))
            preview.render()
            body = preview.body.get('1.0', 'end-1c')
            assert '{' not in body and '}' not in body
            for index in (0, 1):
                assert expand(TEXTS[scene][index][slot-1], 'ミケ') in body
            previews.append(dict(scene=scene, slot=slot, body=body, image_notice=preview.image_notice.get(),
                                 source=preview.source_notice.get(), image_source=str(resolved['source_path']), image_sha256=digest(resolved['source_path'])))
        # 片側だけ／両方未登録をメモリ上だけで再現し、既存表示への独立復帰を確認。
        for keep in ('speech', 'cat_response', 'none'):
            preview.entries = before['entries'] + [e for e in entries if e['condition']['kind'] == keep]
            preview.render()
            source = preview.source_notice.get()
            assert 'フォールバック' in source
            if keep != 'none': assert '専用文' in source
            checks.append(dict(scene=scene, keep=keep, source=source, body=preview.body.get('1.0','end-1c')))
        preview.entries = before['entries'] + entries
    save_record('preview-checks.json', dict(previews=previews, independent_side_checks=checks,
                                         scope='Tool preview and selection only; no Unity execution'))
    current = [0]
    def next_scene():
        current[0] = (current[0] + 1) % (len(scenes) * 2)
        slot = '案' + str(current[0] % 2 + 1)
        preview.variables['speech'].set(slot); preview.variables['cat'].set(slot)
        show(scenes[current[0] // 2])
    def register():
        for e in entries:
            assert editor.load_condition(e['condition'])
            assert editor.values() == e
            editor.preview()
            assert editor.register(), editor.notice.get()
            assert editor.reload() and editor.values() == published.get(e['condition']) == e
        after = published.read()
        assert len(after['entries']) == len(before['entries']) + 8
        assert all(e in after['entries'] for e in before['entries'])
        assert all(e in drafts.read()['entries'] for e in olddraft['entries'])
        assert all(digest(Path(p)) == h for p, h in protected.items())
        texts = [v['text'] for e in entries for v in e['variants'] if v['text']]
        assert len(texts) == len(set(texts)) == 16
        ids = [v['id'] for e in entries for v in e['variants'] if v['text']]
        assert len(ids) == len(set(ids)) == 16
        # 新しいストア・編集画面で再開。空き案3は登録候補にならない。
        reopened = InteractionTextStore(published.path, published=True, subject_registry=subjects)
        editor.close()
        new_editor = InteractionTextEditorWindow(root, drafts, reopened, subject_registry=subjects)
        for e in entries:
            assert new_editor.load_condition(e['condition']) and new_editor.values() == e
            for action in (('direct','adapt','pause','switch') if e['condition']['narrative_scene'] == 'open_up' else (e['condition']['action'],)):
                query = dict(e['condition'], action=action, scene_key='presence__'+action)
                found = select_entry(after['entries'], query)
                assert len(found['candidates']) == 2
                assert [v['id'] for v in found['candidates']] == [v['id'] for v in e['variants'] if v['text']]
        new_editor.close()
        save_record('registration.json', dict(registered_at=datetime.now(timezone.utc).isoformat(),
                    entries=entries, condition_keys=[key(e['condition']) for e in entries],
                    before_count=len(before['entries']), after_count=len(after['entries']),
                    protected_hashes=protected, unrelated_entries_and_drafts_unchanged=True,
                    adopted_profiles=[subjects.get('player'), subjects.get('cat-mike')],
                    generated_files_reviewed=len(generated), matching_generated=matches,
                    route='Tk hand entry -> save_draft -> review -> image preview -> register -> reload -> new editor',
                    llm='Manual authoring through existing Tool; no model request or settings change',
                    unity_imported=False, exported=False, built=False))
        print('PASS: 8 conditions / 16 texts; 8 paired previews; 12 independent fallback previews; unrelated entries preserved', flush=True)
        root.destroy()
    ttk.Label(root, text='画像付きプレビューの4場面・各2案を確認。登録は今回の8条件のみ。').pack(padx=15,pady=8)
    ttk.Button(root, text='次の場面・案を確認', command=next_scene).pack(fill='x',padx=15,pady=4)
    ttk.Button(root, text='画像確認済み：今回の8条件だけ登録', command=register).pack(fill='x',padx=15,pady=4)
    show('switch')
    preview.variables['speech'].set('案1'); preview.variables['cat'].set('案1'); preview.render()
    root.mainloop()


if __name__ == '__main__':
    main()
