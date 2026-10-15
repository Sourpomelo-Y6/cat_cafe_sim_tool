"""日常交流文を条件別に手入力・下書き保存・確認登録する。"""
import copy

from interaction_texts import (KINDS, SUBJECTS, MODES, REACTIONS, MAX_LENGTH,
                               actions, reactions, condition, make_entry)
from interaction_scenes import ACTION_LABELS

ACTION_NAMES = dict(ACTION_LABELS, any='動作共通')
REACTION_NAMES = dict(REACTIONS, any='指定なし（人物のセリフ）')


class InteractionTextEditorWindow:
    def __init__(self, parent, drafts, published, *, generation_settings=None, generation_output=None,
                 image_registry=None, catalog_provider=None):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        # 初回読込に失敗しても、空の編集画面や上書き経路を作らない。
        drafts.read()
        published.read()
        self.drafts, self.published = drafts, published
        self.generation_settings, self.generation_output = generation_settings, generation_output
        self.image_registry, self.catalog_provider = image_registry, catalog_provider
        self.reviewed = None
        self.window = tk.Toplevel(parent)
        self.window.title('日常交流の文章 — 編集・生成・確認登録')
        self.window.geometry('820x720')
        self.window.minsize(720, 650)
        self.window.protocol('WM_DELETE_WINDOW', self.close)
        frame = ttk.Frame(self.window, padding=12)
        frame.pack(fill='both', expand=True)
        ttk.Label(frame, text='日常交流の文章', font=('', 14, 'bold')).pack(anchor='w')
        ttk.Label(frame, text='人物のセリフと猫の反応を別々に制作します。1〜3案、各200文字まで。\n'
                  '下書き保存 → 内容を確認 → 確認した文章を登録。画像付きで確認し、Unity向けに書き出します。',
                  wraplength=760).pack(anchor='w', pady=(4, 8))
        grid = ttk.Frame(frame)
        grid.pack(fill='x')
        self.choices, self.selectors = {}, {}
        for i, (name, label) in enumerate((('kind', '文章の種類'), ('subject_id', '人物・猫'),
                                            ('mode', '行動・絵の種類'), ('action', '操作・強さ'),
                                            ('reaction', '猫の反応条件'))):
            column, row = i % 2, (i // 2) * 2
            grid.columnconfigure(column, weight=1)
            ttk.Label(grid, text=label).grid(row=row, column=column, sticky='w', padx=(0, 8))
            var = tk.StringVar(master=self.window)
            combo = ttk.Combobox(grid, textvariable=var, state='readonly')
            combo.grid(row=row + 1, column=column, sticky='ew', padx=(0, 8), pady=(0, 4))
            combo.bind('<<ComboboxSelected>>', self.selection_changed)
            self.choices[name], self.selectors[name] = var, combo
        self.generation_button = ttk.Button(grid, text='ローカルLLMで3案生成…', command=self.open_generation)
        self.generation_button.grid(row=5, column=1, sticky='ew', padx=(0, 8), pady=(0, 4))
        ttk.Button(grid, text='画像付きで組み合わせを確認…', command=self.open_preview).grid(
            row=6, column=0, sticky='ew', padx=(0, 8), pady=(2, 0))
        self.batch_button = ttk.Button(grid, text='人物のセリフを連続生成…', command=self.open_batch_generation)
        self.batch_button.grid(row=6, column=1, sticky='ew', padx=(0, 8), pady=(2, 0))
        self.scope = tk.StringVar(master=self.window)
        ttk.Label(frame, textvariable=self.scope, wraplength=760).pack(anchor='w', pady=4)
        self.fields = []
        for i in range(3):
            ttk.Label(frame, text=f'案{i + 1}（空欄は未制作）').pack(anchor='w')
            widget = tk.Text(frame, height=2, width=64, wrap='word', undo=True)
            widget.pack(fill='x', pady=(0, 4))
            widget.bind('<<Modified>>', self.edited)
            self.fields.append(widget)
        controls = ttk.Frame(frame)
        controls.pack(fill='x', pady=6)
        for label, method in (('下書きを保存', self.save_draft), ('保存内容を再読込', self.reload),
                              ('登録内容を確認', self.preview), ('確認した文章を登録', self.register)):
            ttk.Button(controls, text=label, command=method).pack(side='left', padx=(0, 8))
        self.preview_text = ScrolledText(frame, height=4, wrap='word', state='disabled')
        self.preview_text.pack(fill='both', expand=True)
        self.notice = tk.StringVar(master=self.window)
        ttk.Label(frame, textvariable=self.notice, wraplength=760).pack(anchor='w', pady=6)
        ttk.Button(frame, text='閉じる', command=self.close).pack(anchor='e')
        self._condition = None
        self._baseline = None
        self.load_condition(condition())

    def _set_selectors(self, value):
        self.batch_button.configure(text='猫の反応を連続生成…' if value['kind'] == 'cat_response' else '人物のセリフを連続生成…')
        maps = {'kind': KINDS, 'subject_id': SUBJECTS[value['kind']], 'mode': MODES,
                'action': {k: ACTION_NAMES[k] for k in actions(value['mode'])},
                'reaction': {k: REACTION_NAMES[k] for k in reactions(value['kind'], value['mode'])}}
        for name, labels in maps.items():
            self.selectors[name].configure(values=list(labels.values()))
            self.choices[name].set(labels[value[name]])
        self.selectors['reaction'].configure(state='disabled' if value['kind'] == 'speech' else 'readonly')
        self.scope.set('対象ID：' + value['subject_id'] + '　／　画像の場面キー：' + value['scene_key']
                       + '　／　交流相手の猫：ミケ（cat-mike）')

    def values(self):
        return make_entry(self._condition, [w.get('1.0', 'end-1c') for w in self.fields])

    def dirty(self):
        if self._baseline is None:
            return False
        try:
            return self.values() != self._baseline
        except ValueError:
            return True

    def confirm_leave(self):
        from tkinter import messagebox
        if not self.dirty():
            return True
        result = messagebox.askyesnocancel('未保存の文章',
                   '現在の条件の文章を下書き保存しますか？\n「いいえ」は変更を破棄、「キャンセル」は編集を続けます。',
                   parent=self.window)
        if result is None:
            return False
        return self.save_draft() if result else True

    def load_condition(self, value):
        try:
            draft, published = self.drafts.get(value), self.published.get(value)
        except (OSError, ValueError) as exc:
            self.notice.set(f'再読込できません。編集中の文章を保持しました：{exc}')
            if self._condition is not None:
                self._set_selectors(self._condition)
            return False
        if not self.confirm_leave():
            self._set_selectors(self._condition)
            return False
        # 確認中に旧条件を保存した場合も、新条件は改めて読む。
        try:
            draft, published = self.drafts.get(value), self.published.get(value)
        except (OSError, ValueError) as exc:
            self.notice.set(f'再読込できません：{exc}')
            self._set_selectors(self._condition)
            return False
        self._condition = copy.deepcopy(value)
        self._draft_snapshot, self._published_snapshot = draft, published
        entry = draft if draft is not None else published
        entry = entry or make_entry(value, ['', '', ''])
        self._set_selectors(value)
        for widget, row in zip(self.fields, entry['variants']):
            widget.delete('1.0', 'end')
            widget.insert('1.0', row['text'])
            widget.edit_reset()
            widget.edit_modified(False)
        self._baseline = copy.deepcopy(entry)
        self.reviewed = None
        self.set_preview('')
        source = '下書き' if draft is not None else '登録済み素材' if published is not None else '空欄'
        self.notice.set(source + 'を読み込みました。人物のセリフは発話本文だけ、猫の反応は描写だけを入力してください。')
        return True

    def selection_changed(self, event=None):
        def selected(name, labels, default):
            return next((k for k, label in labels.items() if label == self.choices[name].get()), default)
        kind = selected('kind', KINDS, 'speech')
        subject = selected('subject_id', SUBJECTS[kind], next(iter(SUBJECTS[kind])))
        mode = selected('mode', MODES, 'teaser')
        action = selected('action', {k: ACTION_NAMES[k] for k in actions(mode)}, actions(mode)[0])
        reaction = selected('reaction', {k: REACTION_NAMES[k] for k in reactions(kind, mode)},
                            reactions(kind, mode)[0])
        self.load_condition(condition(kind, subject, mode, action, reaction))

    def reload(self):
        return self.load_condition(self._condition)

    def edited(self, event):
        if event.widget.edit_modified():
            self.reviewed = None
            self.set_preview('')
            event.widget.edit_modified(False)

    def set_preview(self, text):
        self.preview_text.configure(state='normal')
        self.preview_text.delete('1.0', 'end')
        self.preview_text.insert('1.0', text)
        self.preview_text.configure(state='disabled')

    def save_draft(self):
        try:
            entry = self.values()
            self.drafts.save(entry, expected=self._draft_snapshot)
            self._draft_snapshot = copy.deepcopy(entry)
            self._baseline = copy.deepcopy(entry)
            self.notice.set('この条件の下書きを保存しました。登録済み素材は変更していません。')
            return True
        except (OSError, ValueError) as exc:
            self.notice.set(f'下書きを保存できません：{exc}')
            return False

    def preview(self):
        from interaction_texts import validate_entry
        self.reviewed = None
        try:
            entry = self.values()
            validate_entry(entry, published=True)
            value = entry['condition']
            text = [KINDS[value['kind']] + '：' + SUBJECTS[value['kind']][value['subject_id']],
                    MODES[value['mode']] + ' ／ ' + ACTION_NAMES[value['action']] + ' ／ '
                    + REACTION_NAMES[value['reaction']], '画像の場面キー：' + value['scene_key']]
            text.extend(f"案{r['slot']}：{r['text'] or '（未制作・登録候補なし）'}" for r in entry['variants'])
            self.set_preview('\n'.join(text))
            self.reviewed = copy.deepcopy(entry)
            self.notice.set('対象・条件・本文を確認し、よければ「確認した文章を登録」を押してください。')
        except ValueError as exc:
            self.set_preview('')
            self.notice.set(f'確認できません：{exc}')

    def register(self):
        try:
            entry = self.values()
            if self.reviewed is None or self.reviewed != entry:
                raise ValueError('「登録内容を確認」を押して、現在の条件と文章を確認してください。')
            # 再読込時に古い下書きが採用文を覆わないよう、同じ条件だけ同期する。
            if not self.save_draft():
                return False
            self.published.save(entry, expected=self._published_snapshot)
            self._published_snapshot = copy.deepcopy(entry)
            self._baseline = copy.deepcopy(entry)
            self.notice.set('この条件の制作素材を登録しました。Unityへ反映するには素材を書き出して取り込んでください。')
            return True
        except (OSError, ValueError) as exc:
            self.notice.set(f'登録できません：{exc}')
            return False

    def close(self):
        if self.confirm_leave():
            self.window.destroy()

    def open_preview(self):
        from interaction_preview_gui import InteractionPreviewWindow
        try:
            if self.image_registry is None or self.catalog_provider is None:
                raise ValueError('画像制作画面から交流文編集を開いてください。')
            entry = self.values()
            existing = getattr(self, 'preview_window', None)
            if existing is not None and existing.window.winfo_exists():
                existing.window.destroy()
            self.preview_window = InteractionPreviewWindow(self.window, self.catalog_provider(),
                                     self.image_registry, self.published, editing_entry=entry)
        except (OSError, ValueError) as exc:
            self.notice.set(f'プレビューを開けません：{exc}')

    def open_generation(self):
        from interaction_llm_gui import InteractionLlmWindow
        existing = getattr(self, 'generation_window', None)
        if existing is not None and existing.window.winfo_exists():
            existing.window.lift()
            self.notice.set('開いている生成画面の対象・条件を確認してください。変更する場合は閉じて開き直します。')
            return
        try:
            baseline = self.values()
            def result(changes):
                nonlocal baseline
                if not self.window.winfo_exists():
                    return False
                try:
                    if self.values() != baseline:
                        return False
                    texts = [row['text'] for row in baseline['variants']]
                    for slot, text in changes.items():
                        texts[slot] = text
                    make_entry(self._condition, texts)  # 残した既存案との重複も検証する。
                except ValueError:
                    return False
                for slot, text in changes.items():
                    self.fields[slot].delete('1.0', 'end')
                    self.fields[slot].insert('1.0', text)
                self.reviewed = None
                self.set_preview('')
                baseline = self.values()
                self.notice.set('選択した生成案を取り込みました。未保存・未登録です。修正して確認してください。')
                return True
            options = dict(settings=self.generation_settings)
            if self.generation_output is not None:
                options['output'] = self.generation_output
            self.generation_window = InteractionLlmWindow(self.window, result, self._condition, **options)
        except (OSError, ValueError) as exc:
            self.notice.set(f'生成画面を開けません：{exc}')

    def open_batch_generation(self):
        from interaction_batch_gui import InteractionBatchWindow
        kind = self._condition['kind']
        if not hasattr(self, 'batch_windows'):
            self.batch_windows = {}
        existing = self.batch_windows.get(kind)
        if existing is not None and existing.window.winfo_exists():
            self.batch_window = existing
            existing.window.lift()
            return
        def result(entry):
            if not self.load_condition(entry['condition']):
                return False
            for field, variant in zip(self.fields, entry['variants']):
                field.delete('1.0', 'end')
                field.insert('1.0', variant['text'])
            self.reviewed = None
            self.set_preview('')
            self.notice.set('連続生成の3案を取り込みました。未保存・未登録です。修正して確認してください。')
            self.window.lift()
            return True
        def has_text(value):
            entries = [self.drafts.get(value), self.published.get(value)]
            if value == self._condition:
                entries.append(self.values())
            return any(row['text'] for entry in entries if entry for row in entry['variants'])
        try:
            options = dict(settings=self.generation_settings, has_text=has_text, kind=kind)
            if self.generation_output is not None:
                options['output'] = self.generation_output
            self.batch_window = InteractionBatchWindow(self.window, result, **options)
            self.batch_windows[kind] = self.batch_window
        except (OSError, ValueError) as exc:
            self.notice.set(f'連続生成画面を開けません：{exc}')
