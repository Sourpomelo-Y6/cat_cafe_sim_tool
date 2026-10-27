from interaction_texts import NARRATIVE_SCENES, scene_actions, scene_reactions
"""ゲームを進めず、画像・採用済み文章・編集中の案を組み合わせて確認する。"""
from interaction_texts import SUBJECTS, MODES, REACTIONS, actions, reactions, condition, validate_entry
from interaction_scenes import ACTION_LABELS
from interaction_presentation import (InteractionImageBindings, resolve_image, select_entry,
                                      coverage, condition_label, compose_preview)


class InteractionPreviewWindow:
    def __init__(self, parent, catalog, registry, published, *, editing_entry=None, bindings=None, drafts=None):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        if editing_entry is not None:
            validate_entry(editing_entry)
        self.entries = published.read()['entries']
        self.catalog, self.registry, self.published = dict(catalog), registry, published
        self.subject_registry=getattr(published,"subject_registry",None)
        self.subjects=self.subject_registry.labels(include_disabled=True) if self.subject_registry else SUBJECTS
        self.selected_cat=None
        self.overlay, self.image = editing_entry, None
        self.drafts=drafts
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
                        reaction=initial['reaction'] if initial['kind'] == 'cat_response' else 'neutral',narrative_scene=initial.get('narrative_scene','normal'))
        self.variables, self.selectors = {}, {}
        grid = ttk.Frame(frame)
        grid.pack(fill='x', pady=6)
        for i, (name, label) in enumerate([('human', '人の話者'), ('mode', '行動'), ('action', '実行する操作'),
                                          ('reaction', '猫の反応条件'), ('speech', 'セリフの候補'), ('cat', '猫の反応の候補'), ('narrative_scene','確定場面')]):
            col, row = i % 3, (i // 3) * 2
            grid.columnconfigure(col, weight=1)
            ttk.Label(grid, text=label).grid(row=row, column=col, sticky='w')
            var = tk.StringVar(master=self.window)
            combo = ttk.Combobox(grid, textvariable=var, state='readonly', width=20)
            combo.grid(row=row + 1, column=col, sticky='ew', padx=(0, 8))
            combo.bind('<<ComboboxSelected>>', lambda event: self.render())
            self.variables[name], self.selectors[name] = var, combo
        for name, labels in [('human', self.subjects['speech']), ('mode', MODES),
                             ('action', dict(ACTION_LABELS, any='操作共通')), ('reaction',dict(REACTIONS,any='確定場面の反応')), ('narrative_scene',NARRATIVE_SCENES)]:
            self.variables[name].set(labels[defaults[name]])
        line = ttk.Frame(frame)
        line.pack(fill='x', pady=6)
        self.cat_subject=tk.StringVar(master=self.window,value=self.subjects['cat_response'].get(initial['subject_id'],next(iter(self.subjects['cat_response'].values()))))
        ttk.Label(line,text='対象猫：').pack(side='left')
        self.cat_selector=ttk.Combobox(line,textvariable=self.cat_subject,values=list(self.subjects['cat_response'].values()),state='readonly',width=12)
        self.cat_selector.pack(side='left')
        self.cat_selector.bind('<<ComboboxSelected>>',lambda event:self.render())
        ttk.Label(line,text='制作画像ID：').pack(side='left')
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
            if self.subject_registry:
                row=self.subject_registry.get(self.selected_cat);row['content_id']=content_id;self.subject_registry.save(row)
            else:self.bindings.save(content_id, self.catalog)
            self.saved_content_id = content_id
            self.notice.set('対象猫と制作画像IDの対応を保存しました。文章・ゲームのセーブは変更していません。')
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
        human = choice('human', self.subjects['speech'])
        cat_id=next(k for k,v in self.subjects['cat_response'].items() if v==self.cat_subject.get())
        if self.selected_cat!=cat_id:
            self.selected_cat=cat_id
            if self.subject_registry:
                self.saved_content_id=self.subject_registry.get(cat_id)['content_id']
                self.content.set(next((label for label,id in self.image_ids.items() if id==self.saved_content_id),'未設定'))
        scene=choice('narrative_scene',NARRATIVE_SCENES)
        mode = choice('mode', MODES if scene=='normal' else {k:v for k,v in MODES.items() if k!='pause'})
        action = choice('action', {k: ACTION_LABELS[k] for k in scene_actions(mode,scene) if k != 'any'})
        reaction = choice('reaction', REACTIONS if scene=='open_up' else {k: dict(REACTIONS,any='確定場面の反応')[k] for k in scene_reactions('cat_response', mode,scene)})
        selections, texts = [], []
        for name, kind, subject, outcome in [('speech', 'speech', human, 'any'),
                                              ('cat', 'cat_response', cat_id, reaction)]:
            found = select_entry(self.entries, condition(kind, subject, mode, action, 'any' if scene!='normal' else outcome, subjects=self.subjects, target_cat=cat_id if kind=='speech' else None,narrative_scene=scene),
                                 overlay=self.overlay if self.use_edit.get() else None,prefer_overlay=True)
            if found['candidates'] and scene!='normal':found['source']=NARRATIVE_SCENES[scene]+'専用文：'+found['source']
            if not found['candidates'] and scene=='open_up':
                normal_mode='pause' if action=='pause' else mode
                found=select_entry(self.entries,condition(kind,subject,normal_mode,action,'any' if kind=='speech' else reaction,subjects=self.subjects,target_cat=cat_id if kind=='speech' else None)) if action!='switch' else dict(candidates=[],source='既存定型文',match='')
                found['source']='通常文＋特別反応追記（フォールバック）'
                if kind=='cat_response':found=dict(found,candidates=[dict(v,text=v['text']+' {cat}は心を開いた様子を見せた。') for v in found['candidates']])
            if not found['candidates'] and scene!='normal':found['source']='既存定型文（フォールバック）'
            labels = {str(i): '案' + str(v['slot']) for i, v in enumerate(found['candidates'])}
            if labels:
                index = int(choice(name, labels))
                texts.append(found['candidates'][index]['text'])
            else:
                self.selectors[name].configure(values=['未制作'])
                self.variables[name].set('未制作')
                texts.append('')
            selections.append(('人：' if name == 'speech' else '猫：') + found['source'] + ' ' + found['match'])
        self.source_notice.set(' ／ '.join(selections) + ('（編集中の案を優先して確認中。ゲームでは猫専用文・専用操作が優先されます）' if self.use_edit.get() else '（専用操作を優先、なければ同じ反応の操作共通文）'))
        self.write(self.body, compose_preview(self.subjects['speech'][human], mode, action, reaction, *texts, cat_name=self.subjects['cat_response'][cat_id],call=self.subject_registry.get(human)['call'] if self.subject_registry else '{cat}',narrative_scene=scene))
        self.image = None
        self.picture.configure(image='', text='画像なし')
        try:
            content_id = self.image_ids.get(self.content.get(), '')
            image = resolve_image(self.registry, content_id, mode,scene if scene in ('open_up','simultaneous') else 'any' if scene in ('switch','connect') else action)
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
        report = coverage(self.entries,dict(speech=self.subjects['speech'],cat_response={cat_id:self.subjects['cat_response'][cat_id]}),cat_id)
        self.write(self.missing_text, f"登録済みで対応：{report['covered_count']} / {report['expected_count']}条件"
                   + f"（操作共通文を利用：{report['common_count']}）\n未制作：{report['missing_count']}条件\n\n"
                   + '\n'.join(condition_label(v,self.subjects) for v in report['missing_conditions']))
        special=coverage(self.entries,dict(speech=self.subjects['speech'],cat_response={cat_id:self.subjects['cat_response'][cat_id]}),cat_id,narrative_scene=scene) if scene!='normal' else None
        if special:self.write(self.missing_text,self.missing_text.get('1.0','end-1c')+'\n\n── 任意の特別文（通常文不足へ含めない）──\n'+str(special['missing_count'])+'条件未制作\n'+'\n'.join(condition_label(v,self.subjects) for v in special['missing_conditions']))
        if self.drafts:
            from interaction_texts import key
            from interaction_presentation import expected_conditions
            drafts=self.drafts.read()['entries'];registered={key(e['condition']):e for e in self.entries};working={key(e['condition']):e for e in drafts}
            values={key(v):v for v in expected_conditions(self.subjects,cat_id)}
            for e in drafts+self.entries:values[key(e['condition'])]=e['condition']
            lines=[]
            for identity,value in sorted(values.items()):
                d,p=working.get(identity),registered.get(identity)
                count=lambda e:sum(bool(v['text']) for v in e['variants']) if e else 0
                lines.append(condition_label(value,self.subjects)+' / 下書き '+str(count(d))+' / 登録 '+str(count(p))+(' / 登録版と差分あり' if d and d!=p else '')+(' / 猫範囲 '+value.get('target_cat','cat-mike') if value['kind']=='speech' else ''))
            self.write(self.missing_text,self.missing_text.get('1.0','end-1c')+'\n\n── 条件別の制作状況（直接登録の案数） ──\n'+'\n'.join(lines))
