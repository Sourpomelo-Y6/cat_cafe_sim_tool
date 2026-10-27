"""ソラの残る4動作の通常反応を既存Toolの編集・確認登録経路で再現する。"""
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
OUT = Path(__file__).parent / 'Docs/Results/SoraRemainingNarratives'
SID = 'cat-52bb33a93ad94bb582a5304f64a75000'
TEXTS = {
 ('tunnel','neutral'): ['{cat}はトンネル遊びの誘いに、静かな様子で応じた。', '{cat}はトンネルへ目を向け、しばらく様子を見ていた。', ''],
 ('tunnel','favorable'): ['{cat}はトンネル遊びに興味を示し、穏やかに応じた。', '{cat}はトンネルに注意を向け、遊びの誘いを受け止めた。', ''],
 ('tunnel','enthusiastic'): ['{cat}はトンネル遊びに強い関心を示し、熱心に応じた。', '{cat}は遊びの誘いに気持ちを向け、いつもより積極的に応じた。', ''],
 ('tunnel','confused'): ['{cat}はトンネル遊びの誘いに戸惑い、様子をうかがった。', '{cat}はどうしたものか迷うように、トンネルへ目を向けた。', ''],
 ('tunnel','listless'): ['{cat}は疲れた様子で、トンネル遊びへの反応が鈍かった。', '{cat}は遊びの誘いにゆっくりと反応し、疲れを見せた。', ''],
 ('brush','turn_away'): ['{cat}はブラシから気持ちをそらし、今は距離を置きたそうだ。', '{cat}はブラッシングの誘いを避けるように、そっと身を引いた。', ''],
 ('voice','neutral'): ['{cat}は声かけに気づき、静かに耳を向けた。', '{cat}は落ち着いた様子で、かけられた声を聞いていた。', ''],
 ('voice','confused'): ['{cat}は声に少し戸惑ったように、相手の様子をうかがった。', '{cat}は声の方へ注意を向けたまま、どう応じるか迷っているようだ。', ''],
 ('voice','listless'): ['{cat}は疲れた様子で、声への反応も控えめだった。', '{cat}は呼びかけを聞きながら、けだるそうに過ごしていた。', ''],
 ('presence','neutral'): ['{cat}は見守られながら、落ち着いた時間を過ごした。', '{cat}はそばの気配を感じつつ、自分のペースで過ごしていた。', ''],
 ('presence','confused'): ['{cat}はそばの気配に少し戸惑い、様子をうかがった。', '{cat}は見守られる中で、どう過ごそうか迷うようにしていた。', ''],
 ('presence','listless'): ['{cat}は疲れた様子で、静かに見守られていた。', '{cat}はそばに人がいる中、けだるそうに過ごしていた。', ''],
}


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
    assert not any(json.loads(p.read_text(encoding='utf-8')).get('condition', {}).get('subject_id') == SID and json.loads(p.read_text(encoding='utf-8')).get('condition', {}).get('mode') in ('tunnel','brush','voice','presence') for p in generated)
    protected = [BASE / 'interaction_subjects.json', BASE / 'manifest.json'] + generated
    hashes = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in protected}
    root = tk.Tk(); root.withdraw()
    editor = InteractionTextEditorWindow(root, drafts, pub, subject_registry=subjects, image_registry=images, catalog_provider=lambda: subjects.image_catalog({}))
    entries, previews = {}, []
    # すべての条件で画像付きプレビューを先に確認する。
    for (mode, reaction), c in conditions.items():
        entry = make_entry(c, TEXTS[(mode, reaction)]); entries[(mode, reaction)] = entry
        preview = InteractionPreviewWindow(root, subjects.image_catalog({}), images, pub, editing_entry=entry, drafts=drafts)
        root.update()
        for action in (('direct', 'adapt', 'intense', 'feint') if mode == 'tunnel' else ('direct', 'adapt')):
            from interaction_scenes import ACTION_LABELS
            preview.variables['action'].set(ACTION_LABELS[action])
            for slot, text in enumerate(TEXTS[(mode, reaction)][:2], 1):
                preview.variables['cat'].set('案' + str(slot)); preview.render(); root.update()
                body = preview.body.get('1.0', 'end-1c')
                assert text.replace('{cat}', 'ソラ') in body and preview.image is not None
                image = resolve_image(images, 'playtest-sora', mode, action)
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
    for (mode, reaction), c in conditions.items():
        assert fresh.get(c) == entries[(mode, reaction)]
        for action in (('direct', 'adapt', 'intense', 'feint') if mode == 'tunnel' else ('direct', 'adapt')):
            q = condition('cat_response', SID, mode, action, reaction, subjects=subjects.labels())
            assert select_entry(after['entries'], q)['candidates'] == entries[(mode, reaction)]['variants'][:2]
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'registration.json').write_text(json.dumps(dict(entries={m+'/'+r: e for (m,r),e in entries.items()}, previews=previews, before_count=len(before['entries']), after_count=len(after['entries']), draft_counts=[len(dbefore['entries']),len(dafter['entries'])], generated_count=len(generated), protected_unchanged=True, unity_imported=False), ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    root.destroy(); print('PASS', len(entries), len(previews))

if __name__ == '__main__': main()
