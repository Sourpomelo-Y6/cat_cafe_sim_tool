"""ゲームを進めず、画像・採用済み文章・編集中の案を組み合わせて確認する。"""
from interaction_texts import SUBJECTS, MODES, REACTIONS, actions, reactions, condition, validate_entry
from interaction_scenes import ACTION_LABELS
from interaction_presentation import (InteractionImageBindings, resolve_image, select_entry,
                                      coverage, condition_label, compose_preview)


class InteractionPreviewWindow:
    def __init__(self, parent, catalog, registry, published, *, editing_entry=None, bindings=None):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        if editing_entry is not None:
            validate_entry(editing_entry)
        self.entries = published.read()['entries']
        self.catalog, self.registry, self.published = dict(catalog), registry, published
        self.overlay, self.image = editing_entry, None
        self.bindings = bindings or InteractionImageBindings(registry.directory / 'interaction_image_bindings.json')
        binding_error = ''
        try:
            content_id = self.bindings.get(self.catalog)
        except (OSError, ValueError) as exc:
            content_id, binding_error = '', str(exc)
        self.saved_content_id = content_id
        self.window = tk.Toplevel(parent)
        self.window.title('日常交流の画像付きプレビュー — ゲームは進行しません')
        self.window.geometry('1000x760')
        self.window.minsize(840, 680)
        frame = ttk.Frame(self.window, padding=12)
        frame.pack(fill='both', expand=True)
        ttk.Label(frame, text='素材の組み合わせ確認です。実際に発生した交流の記録ではありません。',
                  wraplength=800).pack(anchor='w')
        initial = editing_entry['condition'] if editing_entry else condition()
        defaults = dict(human=initial['subject_id'] if initial['kind'] == 'speech' else 'player',
                        mode=initial['mode'], action=initial['action'],
                        reaction=initial['reaction'] if initial['kind'] == 'cat_response' else 'neutral')
        self.variables, self.selectors = {}, {}
        grid = ttk.Frame(frame)
        grid.pack(fill='x', pady=6)
        for i, (name, label) in enumerate([('human', '人の話者'), ('mode', '行動'), ('action', '実行する操作'),
                                          ('reaction', '猫の反応条件'), ('speech', 'セリフの候補'), ('cat', '猫の反応の候補')]):
            col, row = i % 3, (i // 3) * 2
            grid.columnconfigure(col, weight=1)
            ttk.Label(grid, text=label).grid(row=row, column=col, sticky='w')
            var = tk.StringVar(master=self.window)
            combo = ttk.Combobox(grid, textvariable=var, state='readonly', width=20)
            combo.grid(row=row + 1, column=col, sticky='ew', padx=(0, 8))
            combo.bind('<<ComboboxSelected>>', lambda event: self.render())
            self.variables[name], self.selectors[name] = var, combo
        for name, labels in [('human', SUBJECTS['speech']), ('mode', MODES),
                             ('action', dict(ACTION_LABELS, any='操作共通')), ('reaction', REACTIONS)]:
            self.variables[name].set(labels[defaults[name]])
        line = ttk.Frame(frame)
        line.pack(fill='x', pady=6)
        ttk.Label(line, text='ミケ（cat-mike）の制作画像ID：').pack(side='left')
        self.image_ids = {'未設定': ''}
        self.image_ids.update({f'{name}（{id}）': id for id, name in self.catalog.items()})
        self.content = tk.StringVar(master=self.window, value=next(label for label, id in self.image_ids.items() if id == content_id))
        self.content_selector = ttk.Combobox(line, textvariable=self.content, values=list(self.image_ids), state='readonly', width=32)
        self.content_selector.pack(side='left')
        self.content_selector.bind('<<ComboboxSelected>>', lambda event: self.render())
        ttk.Button(line, text='画像ID対応を保存', command=self.save_binding).pack(side='left', padx=8)
        self.use_edit = tk.BooleanVar(master=self.window, value=editing_entry is not None)
        edit = ttk.Checkbutton(frame, text='編集中の案を一時的に重ねる（未登録・書き出し対象外）',
                               variable=self.use_edit, command=self.render)
        edit.pack(anchor='w')
        if editing_entry is None:
            edit.state(['disabled'])
        tabs = ttk.Notebook(frame)
        tabs.pack(fill='both', expand=True, pady=6)
        view, missing = ttk.Frame(tabs, padding=8), ttk.Frame(tabs, padding=8)
        tabs.add(view, text='画像と文章')
        tabs.add(missing, text='未制作の条件（登録済み素材だけを集計）')
        view.columnconfigure(0, weight=0)
        view.columnconfigure(1, weight=1)
        view.rowconfigure(0, weight=1)
        left = ttk.Frame(view)
        left.grid(row=0, column=0, sticky='nsew', padx=(0, 10))
        self.picture = ttk.Label(left, text='画像なし', anchor='center')
        self.picture.pack(fill='both', expand=True)
        self.image_notice = tk.StringVar(master=self.window)
        ttk.Label(left, textvariable=self.image_notice, wraplength=310).pack(anchor='w')
        self.body = ScrolledText(view, width=40, height=12, wrap='word', state='disabled')
        self.body.grid(row=0, column=1, sticky='nsew')
        self.missing_text = ScrolledText(missing, height=12, wrap='word', state='disabled')
        self.missing_text.pack(fill='both', expand=True)
        self.source_notice = tk.StringVar(master=self.window)
        ttk.Label(frame, textvariable=self.source_notice, wraplength=800).pack(anchor='w')
        ttk.Label(frame, text='行動文・地の文は試作用の共通文です。絵は操作別のため、反応との一致も目視で確認してください。',
                  wraplength=800).pack(anchor='w')
        controls = ttk.Frame(frame)
        controls.pack(fill='x', pady=4)
        ttk.Button(controls, text='登録済み素材を再読込', command=self.reload).pack(side='left')
        ttk.Button(controls, text='画像・組み合わせを更新', command=self.render).pack(side='left', padx=8)
        ttk.Button(controls, text='閉じる', command=self.window.destroy).pack(side='right')
        self.notice = tk.StringVar(master=self.window, value=binding_error)
        ttk.Label(frame, textvariable=self.notice, wraplength=800).pack(anchor='w')
        self.render()

    @staticmethod
    def write(widget, text):
        widget.configure(state='normal')
        widget.delete('1.0', 'end')
        widget.insert('1.0', text)
        widget.configure(state='disabled')

    def reload(self):
        try:
            self.entries = self.published.read()['entries']
            self.notice.set('登録済み素材を再読込しました。編集中の案は開いた時点の内容です。')
            self.render()
        except (OSError, ValueError) as exc:
            self.notice.set(f'再読込できません。前回の表示内容を保持します：{exc}')

    def save_binding(self):
        try:
            content_id = self.image_ids[self.content.get()]
            self.bindings.save(content_id, self.catalog)
            self.saved_content_id = content_id
            self.notice.set('ミケと制作画像IDの対応を保存しました。文章・ゲームのセーブは変更していません。')
            self.render()
        except (OSError, ValueError, KeyError) as exc:
            self.notice.set(f'対応を保存できません：{exc}')

    def render(self):
        import tkinter as tk
        from content_images import load_display_image
        def choice(name, labels):
            labels = dict(labels)
            self.selectors[name].configure(values=list(labels.values()))
            if self.variables[name].get() not in labels.values():
                self.variables[name].set(next(iter(labels.values())))
            return next(k for k, label in labels.items() if label == self.variables[name].get())
        human = choice('human', SUBJECTS['speech'])
        mode = choice('mode', MODES)
        action = choice('action', {k: ACTION_LABELS[k] for k in actions(mode) if k != 'any'})
        reaction = choice('reaction', {k: REACTIONS[k] for k in reactions('cat_response', mode)})
        selections, texts = [], []
        for name, kind, subject, outcome in [('speech', 'speech', human, 'any'),
                                              ('cat', 'cat_response', 'cat-mike', reaction)]:
            found = select_entry(self.entries, condition(kind, subject, mode, action, outcome),
                                 overlay=self.overlay if self.use_edit.get() else None)
            labels = {str(i): '案' + str(v['slot']) for i, v in enumerate(found['candidates'])}
            if labels:
                index = int(choice(name, labels))
                texts.append(found['candidates'][index]['text'])
            else:
                self.selectors[name].configure(values=['未制作'])
                self.variables[name].set('未制作')
                texts.append('')
            selections.append(('人：' if name == 'speech' else '猫：') + found['source'] + ' ' + found['match'])
        self.source_notice.set(' ／ '.join(selections) + '（専用操作を優先、なければ同じ反応の操作共通文）')
        self.write(self.body, compose_preview(SUBJECTS['speech'][human], mode, action, reaction, *texts))
        self.image = None
        self.picture.configure(image='', text='画像なし')
        try:
            content_id = self.image_ids.get(self.content.get(), '')
            image = resolve_image(self.registry, content_id, mode, action)
            notes = ['画像：' + image['requested_scene_key'] + ' → ' + (image['resolved_scene_key'] or 'なし')]
            notes.extend(image['warnings'])
            if content_id != self.saved_content_id:
                notes.append('制作画像IDの一時選択です。書き出しに使うには対応を保存してください。')
            self.image_notice.set('\n'.join(notes))
            if image['source_path']:
                self.image = load_display_image(self.window, image['source_path'], 310, 240)
                self.picture.configure(image=self.image, text='')
        except (OSError, ValueError, tk.TclError) as exc:
            self.image_notice.set(f'画像を表示できません：{exc}')
        report = coverage(self.entries)
        self.write(self.missing_text, f"登録済みで対応：{report['covered_count']} / {report['expected_count']}条件"
                   + f"（操作共通文を利用：{report['common_count']}）\n未制作：{report['missing_count']}条件\n\n"
                   + '\n'.join(condition_label(v) for v in report['missing_conditions']))
