"""起動済みローカルLLMを実Tk画面から検証。素材の保存先は検証用フォルダのみ。"""
import argparse
import json
from pathlib import Path
import time

from interaction_llm import InteractionLlmSettings
from interaction_texts import InteractionTextStore, condition
from interaction_text_editor_gui import InteractionTextEditorWindow


def run(output, url):
    import tkinter as tk
    output.mkdir(parents=True, exist_ok=True)
    root = tk.Tk()
    root.withdraw()
    settings = InteractionLlmSettings(output / 'settings.json')
    editor = InteractionTextEditorWindow(root, InteractionTextStore(output / 'drafts.json'),
               InteractionTextStore(output / 'published.json', published=True),
               generation_settings=settings, generation_output=output / 'records')
    editor.window.withdraw()
    results = []
    def wait(g):
        deadline = time.monotonic() + 190
        while g.running and time.monotonic() < deadline:
            root.update()
            time.sleep(.02)
        if g.running:
            raise RuntimeError('生成の検証がタイムアウトしました。')
    cases = [
        ('player-teaser', condition(), '成人のプレイヤー。穏やかで自然な口調でミケに話す。来歴は未設定。',
         'ミケは座って、床の近くで揺れるねこじゃらしを見ている。'),
        ('guest-pet', condition(subject_id='guest-1', mode='pet'),
         '成人のお客さんの佐藤さん。落ち着いた優しい口調でミケに話す。来歴は未設定。',
         '座っているミケの頭を、人物の手がそっとなでている。'),
        ('mike-favorable', condition('cat_response', 'cat-mike', mode='pet', action='adapt', reaction='favorable'),
         'ミケは穏やかな短毛の三毛猫。未指定の外見や来歴は書かない。',
         'ミケは座って頭をなでられ、目を細めている。走ったり跳んだりしない。'),
        ('mike-low-rest', condition('cat_response', 'cat-mike', mode='pause', action='pause', reaction='listless'),
         'ミケは穏やかな短毛の三毛猫。低体力で休んでいる。病気や来歴を創作しない。',
         '相手が手を止めて待ち、ミケは体を丸めて静かに休んでいる。動き回らない。')]
    try:
        for name, value, profile, image in cases:
            if not editor.load_condition(value):
                raise RuntimeError(editor.notice.get())
            editor.open_generation()
            g = editor.generation_window
            g.window.withdraw()
            g.url.set(url)
            g.fetch()
            wait(g)
            if not g.model.get():
                raise RuntimeError(g.status.get())
            for field, text in [('subject_notes', profile), ('image_notes', image)]:
                g.notes[field].delete('1.0', 'end')
                g.notes[field].insert('1.0', text)
            g.save_settings()
            g.start()
            wait(g)
            if g.candidate is None:
                raise RuntimeError(name + ': ' + g.status.get())
            g.apply()
            if ([row['text'] for row in editor.values()['variants']] != g.candidate
                    or editor.reviewed is not None):
                raise RuntimeError(name + ': ' + g.status.get())
            if editor.register():
                raise RuntimeError('確認前に登録できました。')
            editor.preview()
            if not editor.register():
                raise RuntimeError(editor.notice.get())
            if editor.published.get(value) != editor.values():
                raise RuntimeError('登録した文章が一致しません。')
            results.append(dict(case=name, model=g.model.get(), condition=value, variants=g.candidate))
            print(name + ': 3案生成・取り込み・確認登録成功', flush=True)
            g.window.destroy()
        (output / 'result.json').write_text(json.dumps(results, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    finally:
        root.destroy()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--url', default='http://127.0.0.1:8080')
    args = parser.parse_args()
    run(args.output, args.url)
