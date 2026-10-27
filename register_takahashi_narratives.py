"""高橋さんの限定制作。既存Tk編集・確認登録経路を使用し、パックは出力しない。"""
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
import tkinter as tk

from interaction_subjects import SubjectRegistry, expand
from interaction_texts import InteractionTextStore, DEFAULT_DRAFT_PATH, condition, key, actions
from interaction_text_editor_gui import InteractionTextEditorWindow
from interaction_presentation import select_entry, compose_preview

ROOT = Path(__file__).resolve().parents[1]
OUT = Path(__file__).resolve().parent / 'Docs/Results/TakahashiNarratives'
TEXTS = {
    'teaser': ['{cat_call}、このねこじゃらしは気になるかな。', 'ねこじゃらしの動き、どんなふうに見るかな。', 'ねこじゃらしで誘ってみようか。'],
    'ball': ['{cat_call}、転がるボールはどうかな。', 'ボールの行き先、一緒に眺めようか。', '丸いものの動きは、ちょっと面白いね。'],
    'plush': ['{cat_call}、このぬいぐるみはどうかな。', 'ぬいぐるみの動き、気になるところはあるかな。', 'ぬいぐるみと遊んでみようか。'],
    'tunnel': ['{cat_call}、トンネルの向こうは気になるかな。', '入口と出口で、見え方が違うね。', '入らなくても、トンネル遊びはどうかな。'],
    'pet': ['{cat_call}、なでてみてもいいかな。', '触れ方ひとつでも、様子は違うのかな。', 'なでるのは、気が向いたときでいいからね。'],
    'brush': ['{cat_call}、ブラシをかけてみようか。', 'ブラシの感触、どんなふうに感じるかな。', '毛並みを見ながら、ブラシを試そうか。'],
    'voice': ['{cat_call}、少し話しかけてもいいかな。', 'どんな声の調子が聞きやすいのかな。', '返事がなくても、こうして話す時間はいいね。'],
    'presence': ['{cat_call}の小さな変化も、見逃したくないな。', '何もしないで眺める時間も、いいものだね。', 'この距離から、そっと様子を見ていよう。'],
    'pause': ['{cat_call}、ここでひと休みしようか。', '何かするばかりでなく、休む時間も大事だね。', '手を止めると、気づくこともありそうだね。'],
}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def capacity():
    paths = {'Logs': ROOT/'Logs', 'generated': ROOT/'cat_cafe_sim_tool/generated',
             'content_images': ROOT/'cat_cafe_sim/saves/content_images',
             'Latest': ROOT/'Logs/StandaloneBuild/Latest',
             'Library': Path('C:/Users/hi-wa/unity_workspase/CatCafeSim/Library')}
    return {k: sum(p.stat().st_size for p in folder.rglob('*') if p.is_file()) for k, folder in paths.items()}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    base = ROOT/'cat_cafe_sim/saves/content_images'
    registry = SubjectRegistry(base/'interaction_subjects.json')
    person = registry.get('person-guest-3')
    assert person['game_id'] == 'guest-3' and person['call'] == '{cat}'
    store = InteractionTextStore(base/'interaction_texts.json', published=True, subject_registry=registry)
    drafts = InteractionTextStore(DEFAULT_DRAFT_PATH, subject_registry=registry)
    before, olddraft = store.read(), drafts.read()
    generated = list((ROOT/'cat_cafe_sim_tool/generated/interaction_texts').rglob('*.json'))
    matches = [str(p) for p in generated if 'guest-3' in p.read_text(encoding='utf-8')]
    assert not matches, 'Review generated candidates before proceeding'
    # 既存対象があれば新規追加せず停止してレビューする。
    assert not [e for e in before['entries'] + olddraft['entries'] if e['condition']['subject_id'] == person['id']]
    protected = [base/'interaction_subjects.json', base/'manifest.json',
                 Path('C:/Users/hi-wa/unity_workspase/CatCafeSim/Assets/Resources/CatCafeContent/catalog.json')]
    hashes = {str(p): digest(p) for p in protected}
    start = capacity()
    root = tk.Tk()
    root.withdraw()
    editor = InteractionTextEditorWindow(root, drafts, store, subject_registry=registry)
    entries, checks, previews = [], [], []
    try:
        for mode, texts in TEXTS.items():
            value = condition('speech', person['id'], mode, 'pause' if mode == 'pause' else 'any',
                              subjects=registry.labels(), target_cat='*')
            assert editor.load_condition(value)
            for field, text in zip(editor.fields, texts):
                field.delete('1.0', 'end')
                field.insert('1.0', text)
            root.update()
            assert editor.save_draft()
            editor.preview()
            assert editor.reviewed == editor.values()
            assert editor.register(), editor.notice.get()
            entry = editor.values()
            assert store.get(value) == drafts.get(value) == entry
            assert editor.load_condition(value) and editor.values() == entry
            entries.append(entry)
        # 別画面・ストア再読込で登録を再開する。
        editor.close()
        editor = InteractionTextEditorWindow(root, drafts, store, subject_registry=registry)
        for entry in entries:
            assert editor.load_condition(entry['condition']) and editor.values() == entry
        after = store.read()
        assert len(after['entries']) == len(before['entries']) + 9
        assert all(e in after['entries'] for e in before['entries'])
        assert all(e in drafts.read()['entries'] for e in olddraft['entries'])
        assert len({v['text'] for e in entries for v in e['variants']}) == 27
        for entry in entries:
            mode = entry['condition']['mode']
            for cat in [r for r in registry.read()['subjects'] if r['kind'] == 'cat_response']:
                for action in actions(mode):
                    if action == 'any':
                        continue
                    query = condition('speech', person['id'], mode, action, subjects=registry.labels(), target_cat=cat['id'])
                    found = select_entry(after['entries'], query)
                    assert found['candidates'] == entry['variants']
                    for variant in found['candidates']:
                        rendered = expand(variant['text'], cat['name'], person['call'])
                        assert '{' not in rendered and '}' not in rendered
                        checks.append({'mode': mode, 'action': action, 'cat': cat['id'], 'id': variant['id'], 'text': rendered})
            for variant in entry['variants']:
                previews.append({'mode': mode, 'id': variant['id'], 'preview': compose_preview(person['name'], mode,
                    next(a for a in actions(mode) if a != 'any'), 'neutral', variant['text'], '', call=person['call'])})
        assert all(digest(Path(p)) == h for p, h in hashes.items())
        result = dict(registered_at=datetime.now(timezone.utc).isoformat(), subject=person,
                      generated_files_reviewed=len(generated), matching_generated=matches,
                      before_conditions=len(before['entries']), after_conditions=len(after['entries']),
                      entries=entries, selection_checks=checks, previews=previews,
                      unrelated_entries_and_drafts_unchanged=True, protected_hashes=hashes,
                      capacity_before=start, capacity_after=capacity(),
                      route='Existing Tk editor save_draft -> preview -> register -> reopen; authored manually, no LLM request',
                      unity_imported=False)
        (OUT/'registration.json').write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
        print(f'PASS: 9 conditions / 27 texts; {len(checks)} selection/expansion checks; reopened Tk editor')
    finally:
        root.destroy()


if __name__ == '__main__':
    main()
