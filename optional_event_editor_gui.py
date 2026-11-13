"""Edit optional stories by image family."""
import copy
from optional_events import BRANCHES, MAX_BRANCH_LINES, MILESTONES, ARTWORK_SLOTS, EVENT_BACKGROUNDS, EVENT_STILLS, EVENT_ID, custom_background, event_slot, export_event_pack


class OptionalEventEditorWindow:
    def __init__(self, parent, drafts, published, catalog_provider):
        import tkinter as tk
        from tkinter import ttk
        self.drafts, self.published, self.catalog_provider = drafts, published, catalog_provider
        self.import_review_pending=False;self.reviewed_all=None
        self.content_id='playtest-mike'
        adopted = published.read(self.content_id)  # A broken store must never become an empty overwrite.
        data = adopted
        self.slot = event_slot(adopted['event'])[1]
        self.window = tk.Toplevel(parent)
        self.window.title('猫の個別イベント — 編集・確認・出力')
        self.window.geometry('1020x800')
        self.window.minsize(780, 660)
        self.window.protocol('WM_DELETE_WINDOW', self.close)
        self.notice = tk.StringVar(master=self.window)
        self.enabled = tk.BooleanVar(master=self.window)
        outer = ttk.Frame(self.window, padding=12)
        outer.pack(fill='both', expand=True)
        ttk.Label(outer, text=f'進行は個体・節目ごと。導入 → 2択 → 結果（各1〜{MAX_BRANCH_LINES}行）。').pack(anchor='w')
        self.content=tk.StringVar(master=self.window)
        self.selector=ttk.Combobox(outer,textvariable=self.content,state='readonly')
        self.selector.pack(fill='x');self.selector.bind('<<ComboboxSelected>>',self.change_target)
        self.milestones = {'従来の開始条件': 0, **{f'好感度 {value}': value for value in MILESTONES}}
        self.milestone_choice = ttk.Combobox(outer, values=list(self.milestones), state='readonly')
        self.milestone_choice.pack(fill='x')
        self.milestone_choice.bind('<<ComboboxSelected>>', self.change_milestone)
        self.show_milestone()
        self.summary=ttk.Label(outer);self.summary.pack(anchor='w')
        ttk.Button(outer,text='画像対象の一覧を更新',command=lambda:self._run(self.refresh_catalog)).pack(anchor='w')
        ttk.Button(outer,text='この系統で共有する（旧ミケのみ）',command=lambda:self._run(self.share_legacy)).pack(anchor='w')
        fields = ttk.Frame(outer)
        fields.pack(fill='x', pady=8)
        fields.columnconfigure(1, weight=1)
        self.values = {}
        for index, (key, caption) in enumerate((('title', 'タイトル'), ('min_day', '開始できる最小日数'), ('min_affinity', '開始できる最小好感度'), ('accept_caption', '選択肢1'), ('decline_caption', '選択肢2'))):
            ttk.Label(fields, text=caption).grid(row=index, column=0, sticky='w', pady=3)
            value = tk.StringVar(master=self.window)
            self.values[key] = value
            entry = ttk.Entry(fields, textvariable=value)
            entry.grid(row=index, column=1, sticky='ew', padx=8, pady=3)
            if key == 'min_affinity':
                self.affinity_entry = entry
        ttk.Checkbutton(outer, text='採用後の出力に含める（外しても開始済みの会話は保持）', variable=self.enabled).pack(anchor='w')
        self.artwork = []
        self.background_choices = dict(EVENT_BACKGROUNDS)
        background = ttk.Frame(outer)
        background.pack(fill='x', pady=(6, 0))
        self.background_selector = self.add_artwork_selector(background, '背景', self.background_choices)
        ttk.Button(background, text='個別のPNGを選ぶ…', command=self.choose_background).pack(side='left')
        ttk.Button(background, text='共通背景を変更…', command=lambda: self.choose_background(shared=True)).pack(side='left', padx=4)
        ttk.Button(background, text='共通背景を初期画像に戻す', command=self.reset_shared_background).pack(side='left')
        book = ttk.Notebook(outer)
        book.pack(fill='both', expand=True, pady=8)
        self.lines = {}
        self.line_lists = {}
        self.line_hosts = {}
        self.line_buttons = {}
        for branch, caption in zip(BRANCHES, ('導入', '結果1', '結果2')):
            page = ttk.Frame(book, padding=8)
            book.add(page, text=caption)
            self.lines[branch] = []
            tools = ttk.Frame(page)
            tools.pack(fill='x', pady=(0, 8))
            self.add_artwork_selector(tools, 'スチル', EVENT_STILLS)
            self.line_buttons[branch] = {}
            for action, caption in (('add', '行を追加'), ('remove', '行を削除'), ('up', '上へ'), ('down', '下へ')):
                button = ttk.Button(tools, text=caption, command=lambda b=branch, a=action: self.edit_lines(b, a))
                button.pack(side='left', padx=(0, 4))
                self.line_buttons[branch][action] = button
            side = ttk.Frame(page)
            side.pack(side='left', fill='y', padx=(0, 8))
            rows = tk.Listbox(side, width=10, exportselection=False)
            scroll = ttk.Scrollbar(side, orient='vertical', command=rows.yview)
            rows.configure(yscrollcommand=scroll.set)
            scroll.pack(side='right', fill='y')
            rows.pack(side='left', fill='y')
            rows.bind('<<ListboxSelect>>', lambda _, b=branch: self.show_line(b))
            self.line_lists[branch] = rows
            self.line_hosts[branch] = ttk.Frame(page)
            self.line_hosts[branch].pack(side='left', fill='both', expand=True)
        controls = ttk.Frame(outer)
        controls.pack(fill='x')
        self.buttons = {}
        for col in range(3):controls.columnconfigure(col, weight=1)
        for index, (caption, action) in enumerate((('下書き保存', self.save_draft), ('プレビュー・採用比較', self.preview), ('この内容を採用登録', self.publish), ('採用を読み直す', self.load_published), ('出力済み定義を下書きへ…', self.import_definition), ('イベントだけ出力…', self.export))):
            button = ttk.Button(controls, text=caption, command=action)
            button.grid(row=index//3, column=index%3, sticky='ew', padx=3, pady=3)
            self.buttons[caption] = button
        ttk.Label(outer, textvariable=self.notice, wraplength=940).pack(fill='x', pady=6)
        self.populate(data)
        self.saved_form = self.collect()
        self.refresh_catalog()
        self.notice.set('下書きの編集だけではUnityへ反映されません。プレビューで確認し、採用登録して出力してください。')

    def add_artwork_selector(self, parent, label, choices):
        import tkinter as tk
        from tkinter import ttk

        value = tk.StringVar(master=self.window)
        ttk.Label(parent, text=label).pack(side='left', padx=(0, 4))
        selector = ttk.Combobox(parent, textvariable=value, values=list(choices.values()), state='readonly', width=24)
        selector.pack(side='left', padx=(0, 8))
        self.artwork.append(value)
        return selector

    @staticmethod
    def background_label(asset):
        return f'個別画像 {asset[3:15]}' if custom_background(asset) else EVENT_BACKGROUNDS[asset]

    def select_background(self, asset):
        if custom_background(asset):
            self.background_choices[asset] = self.background_label(asset)
            self.background_selector.configure(values=list(self.background_choices.values()))
        self.artwork[0].set(self.background_label(asset))

    def choose_background(self, *, shared=False):
        from tkinter import filedialog
        path = filedialog.askopenfilename(parent=self.window, title='背景画像を選ぶ', filetypes=[('PNG画像', '*.png')])
        if not path:
            return

        def apply():
            asset = self.published.register_background(path, shared=shared)
            self.reviewed = self.reviewed_all = None
            if shared:
                self.notice.set('共通背景を更新しました。次回の出力に含まれます。')
            else:
                self.select_background(asset)
                self.notice.set('個別背景を選びました。プレビューで確認して採用登録してください。')
        self._run(apply)

    def reset_shared_background(self):
        def reset():
            self.published.reset_shared_background()
            self.reviewed = self.reviewed_all = None
            self.notice.set('共通背景を初期画像に戻しました。次回の出力で反映されます。')
        self._run(reset)

    @staticmethod
    def artwork_for(event):
        if 'artwork' in event:
            return event['artwork']
        if event['event_id'] == EVENT_ID and event.get('target_kind') == 'individual':
            return ['mike-window-v1', '', 'mike-approaches-v1', '']
        return ['', '', '', '']

    def populate(self, data):
        self.identity = copy.deepcopy(data['event'])
        self.enabled.set(data['enabled'])
        artwork = self.artwork_for(self.identity)
        self.select_background(artwork[0])
        for variable, options, asset in zip(self.artwork[1:], ARTWORK_SLOTS[1:], artwork[1:]):
            variable.set(options[asset])
        for key, value in self.values.items():
            value.set(str(data['event'][key]))
        self.affinity_entry.configure(state='readonly' if self.slot else 'normal')
        for branch in BRANCHES:
            for frame, _, _ in self.lines[branch]:
                frame.destroy()
            self.lines[branch].clear()
            for line in data['event'][branch]:
                self.insert_line(branch, len(self.lines[branch]), line)
            self.select_line(branch, 0)
        self.reviewed = None
        self.reviewed_all = None

    def insert_line(self, branch, index, line):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText

        frame = ttk.Frame(self.line_hosts[branch])
        ttk.Label(frame, text='話者（空欄は地の文）').pack(anchor='w')
        speaker = tk.StringVar(master=frame, value=line['speaker'])
        ttk.Entry(frame, textvariable=speaker).pack(fill='x', pady=(0, 6))
        widget = ScrolledText(frame, height=3, wrap='word', undo=True)
        widget.pack(fill='both', expand=True)
        widget.insert('1.0', line['text'])
        widget.edit_reset()
        self.lines[branch].insert(index, (frame, speaker, widget))

    def select_line(self, branch, index):
        rows = self.line_lists[branch]
        rows.delete(0, 'end')
        rows.insert('end', *(f'{i + 1}行目' for i in range(len(self.lines[branch]))))
        rows.selection_set(index)
        rows.activate(index)
        rows.see(index)
        self.show_line(branch)

    def show_line(self, branch):
        selection = self.line_lists[branch].curselection()
        if not selection:
            return
        index = selection[0]
        lines = self.lines[branch]
        for frame, _, _ in lines:
            frame.pack_forget()
        lines[index][0].pack(fill='both', expand=True)
        for action, enabled in (
            ('add', len(lines) < MAX_BRANCH_LINES), ('remove', len(lines) > 1),
            ('up', index > 0), ('down', index < len(lines) - 1),
        ):
            self.line_buttons[branch][action].state(['!disabled' if enabled else 'disabled'])

    def edit_lines(self, branch, action):
        from tkinter import messagebox

        index = self.line_lists[branch].curselection()[0]
        lines = self.lines[branch]
        if action == 'add' and len(lines) < MAX_BRANCH_LINES:
            index += 1
            self.insert_line(branch, index, dict(speaker='', text=''))
        elif action == 'remove' and len(lines) > 1:
            frame, speaker, widget = lines[index]
            if (speaker.get() or widget.get('1.0', 'end-1c')) and not messagebox.askyesno(
                '会話行の削除', f'{index + 1}行目を削除しますか？', parent=self.window,
            ):
                return
            frame.destroy()
            lines.pop(index)
            index = min(index, len(lines) - 1)
        elif action in ('up', 'down'):
            target = index + (-1 if action == 'up' else 1)
            if not 0 <= target < len(lines):
                return
            lines[index], lines[target] = lines[target], lines[index]
            index = target
        else:
            return
        self.reviewed = self.reviewed_all = None
        self.select_line(branch, index)

    def collect(self):
        event = copy.deepcopy(self.identity)
        selected = [
            next(asset for asset, label in choices.items() if label == value.get())
            for value, choices in zip(self.artwork, (self.background_choices, *ARTWORK_SLOTS[1:]))
        ]
        if 'artwork' in event or selected != self.artwork_for(event):
            event['artwork'] = selected
        for key, value in self.values.items():
            event[key] = value.get()
        for branch in BRANCHES:
            event[branch] = [
                dict(speaker=speaker.get(), text=widget.get('1.0', 'end-1c'))
                for _, speaker, widget in self.lines[branch]
            ]
        return dict(version=1, enabled=self.enabled.get(), event=event)

    def validated(self):
        from optional_events import validate_event
        data = self.collect()
        for key in ('min_day', 'min_affinity'):
            if not data['event'][key].isascii() or not data['event'][key].isdigit():
                raise ValueError('日数・好感度は整数で入力してください。')
            data['event'][key] = int(data['event'][key])
        data['event'].pop('revision', None)
        if self.slot and data['event']['min_affinity'] != self.slot:
            raise ValueError('開始好感度は選択中の節目と一致させてください。')
        if 'artwork' not in data['event'] and any(self.artwork_for(data['event'])):
            data['event']['artwork'] = self.artwork_for(data['event'])
        data['event'] = validate_event(data['event'], require_revision=False)
        return data

    def _run(self, action):
        try:
            return action()
        except (OSError, ValueError, UnicodeError) as exc:
            self.notice.set('操作できません：' + str(exc))

    def save_draft(self):
        def save():
            self.save_row(self.drafts,self.validated())
            self.saved_form = self.collect()
            self.update_summary()
            self.notice.set('下書きを保存しました。採用・Unity出力はまだ行っていません。')
        return self._run(save)

    def preview(self):
        import tkinter as tk
        from pathlib import Path
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        def show():
            if self.import_review_pending:
                return self.preview_all()
            data = self.validated()
            adopted = next((row for row in self.effective_published()['entries']
                            if event_slot(row['event']) == (self.content_id, self.slot)), None)
            window = tk.Toplevel(self.window)
            window.title('プレビューと採用内容の比較')
            window.geometry('1000x700')
            book = ttk.Notebook(window)
            book.pack(fill='both', expand=True)
            images = {}
            window.artwork_images = images
            for branch, label in (('accepted', '選択肢1の流れ'), ('declined', '選択肢2の流れ')):
                pane = ttk.Frame(book)
                book.add(pane, text=label)
                for content, heading in ((adopted, '現在の採用'), (data, '編集中')):
                    text = ScrolledText(pane, width=48, wrap='word')
                    text.pack(side='left', fill='both', expand=True)
                    if content is None:
                        text.insert('end', heading + '\n採用された定義はありません。')
                        text.configure(state='disabled')
                        continue
                    e = content['event']
                    scope = '旧ミケ個体専用' if e.get('target_kind') == 'individual' else '画像系統で共有'
                    output = '出力対象' if content['enabled'] else '出力対象外'
                    text.insert('end', f"{heading}（{scope}／{output}）\n{e['title']}\n"
                                f"{e['min_day']}日目以降 / 好感度{e['min_affinity']}以上\n\n")
                    background, intro, accepted, declined = self.artwork_for(e)
                    for label, asset, options in (
                        ('背景', background, EVENT_BACKGROUNDS),
                        ('導入', intro, EVENT_STILLS),
                        ('結果', accepted if branch == 'accepted' else declined, EVENT_STILLS),
                    ):
                        caption = self.background_label(asset) if label == '背景' else options[asset]
                        text.insert('end', f'{label}：{caption}\n')
                        if asset or label == '背景':
                            path = self.published.background_path(asset)
                            if not path.is_file():
                                raise ValueError(f'背景・スチルが見つかりません：{caption}')
                            if asset not in images:
                                image = tk.PhotoImage(master=window, file=path)
                                images[asset] = image.subsample(max(1, (image.width() + 299) // 300))
                            text.image_create('end', image=images[asset])
                            text.insert('end', '\n')
                    text.insert('end', '\n')
                    for line in e['introduction'] + [dict(speaker='あなた', text=e['accept_caption' if branch == 'accepted' else 'decline_caption'])] + e[branch]:
                        text.insert('end', (line['speaker'] or '地の文') + '\n' + line['text'] + '\n\n')
                    text.configure(state='disabled')
            self.reviewed = data
            self.notice.set('両分岐をプレビューできます。編集すると採用前にもう一度比較が必要です。')
        return self._run(show)

    def publish(self):
        def save():
            if self.import_review_pending:
                all_data=self.bulk_collection()
                if self.reviewed_all!=all_data or self.reviewed!=self.collect():raise ValueError('全下書きの比較をやり直してください。')
                self.published.save_all(all_data);self.import_review_pending=False
            else:
                data=self.validated()
                if self.reviewed!=data:raise ValueError('プレビュー・採用比較で現在の内容を確認してください。')
                if self.content_id not in self.catalog_provider() and data['enabled']:raise ValueError('対象不明の定義は、復帰するか出力対象を外してください。')
                self.save_row(self.published,data)
            self.saved_form = self.collect()
            self.update_summary()
            self.notice.set('採用登録しました。出力対象に設定した定義が次のUnity出力に含まれます。')
        return self._run(save)

    def load_published(self):
        from tkinter import messagebox
        if self.collect() != self.saved_form and not messagebox.askyesno('編集中の内容', '未保存の編集を破棄して採用内容を読み直しますか？', parent=self.window):
            return
        def load():
            self.import_review_pending=False
            self.populate(self.published.read(self.content_id, self.slot))
            self.saved_form = self.collect()
            self.update_summary()
            self.notice.set('採用内容を読みました。')
        self._run(load)

    def import_definition(self):
        from tkinter import filedialog, messagebox
        if self.collect() != self.saved_form and not messagebox.askyesno('編集中の内容', '未保存の編集を破棄して出力済み定義を読みますか？', parent=self.window):
            return
        path = filedialog.askopenfilename(parent=self.window, filetypes=[('イベント定義', 'optional_events.json')])
        if path:
            def load():
                from pathlib import Path
                from optional_events import read_pack, normalize_event
                data=read_pack(Path(path).read_text(encoding='utf-8-sig'))
                if not messagebox.askokcancel('全下書きを読み込む',f"{len(data['events'])}件の定義で下書き全体を置き換えます。空の場合は既存本文を保持して全件を出力対象外にします。採用は比較後に確定します。",parent=self.window):return
                self.published.copy_backgrounds(data['events'], Path(path).parent)
                if data['events']:rows=[dict(enabled=True,event=normalize_event(e)) for e in data['events']]
                else:
                    merged={event_slot(r['event']):r for r in self.effective_published()['entries']}
                    if self.drafts.path.exists():merged.update({event_slot(r['event']):r for r in self.drafts.read_all()['entries']})
                    rows=[dict(enabled=False,event=r['event']) for r in merged.values()]
                self.drafts.save_all(dict(version=2,entries=rows))
                self.import_review_pending=True
                self.refresh_catalog(False)
                self.saved_form = self.collect()
                self.notice.set('下書きへ読み込みました。採用内容は変更していません。')
            self._run(load)

    def export(self):
        from tkinter import filedialog, messagebox
        path = filedialog.askdirectory(parent=self.window, title='採用イベントの出力先')
        if path:
            rows=self.published.read_all()['entries'];count=sum(r['enabled'] for r in rows)
            if not messagebox.askokcancel('全登録の出力',f'{count}件の有効定義でUnityの全イベント定義を置き換えます。進行済みの会話は残ります。',parent=self.window):return
            self._run(lambda: self.notice.set('採用内容を出力しました：' + str(export_event_pack(self.published, self.catalog_provider(), path))))

    def save_row(self,store,data):
        if not store.path.exists() and 'playtest-mike' not in self.catalog_provider():
            store.save_all(dict(version=2,entries=[dict(enabled=data['enabled'],event=data['event'])]))
        else:store.save(data)

    def bulk_collection(self):
        data=self.drafts.read_all();rows=data['entries']
        slot = self.content_id, self.slot
        if any(event_slot(r['event']) == slot for r in rows) or self.collect()!=self.saved_form:
            current=self.validated();rows=[r for r in rows if event_slot(r['event']) != slot]
            rows.append(dict(enabled=current['enabled'],event=current['event']))
        result=dict(version=2,entries=rows)
        self.drafts._validate_all(result)
        if any(r['enabled'] and r['event']['content_cat_id'] not in self.catalog_provider() for r in rows):raise ValueError('対象不明の定義は復帰または出力除外が必要です。')
        return result

    def preview_all(self):
        import tkinter as tk
        from tkinter.scrolledtext import ScrolledText
        def describe(data):
            result=[]
            for row in data['entries']:
                e=row['event'];result.append(e['title']+'（'+e['content_cat_id']+'）'+(' 出力する' if row['enabled'] else ' 出力しない'))
                result.append('対象：旧ミケ専用' if e.get('target_kind')=='individual' else '対象：画像系統で共有')
                result.append(f"{e['min_day']}日目／好感度{e['min_affinity']}以上")
                background, *stills = self.artwork_for(e)
                artwork = [self.background_label(background), *(EVENT_STILLS[asset] for asset in stills)]
                result.append('背景・導入・結果1・結果2：' + '／'.join(artwork))
                for branch in BRANCHES:
                    result.append({'introduction':'導入','accepted':e['accept_caption'],'declined':e['decline_caption']}[branch])
                    result.extend((line['speaker'] or '地の文')+'：'+line['text'] for line in e[branch])
                result.append('')
            return '\n'.join(result)
        data=self.bulk_collection();window=tk.Toplevel(self.window);window.title('全下書きと全採用の比較');window.geometry('1000x700')
        for value,label in ((self.effective_published(),'現在の全採用'),(data,'採用予定の全下書き')):
            text=ScrolledText(window,width=48,wrap='word');text.pack(side='left',fill='both',expand=True);text.insert('end',label+'\n\n'+describe(value));text.configure(state='disabled')
        self.reviewed=self.collect();self.reviewed_all=data
        self.notice.set(f"全{len(data['entries'])}件の両分岐を比較しました。採用ボタンで全体を確定します。")

    def effective_published(self):
        return self.published.read_all() if self.published.path.exists() or 'playtest-mike' in self.catalog_provider() else dict(version=2,entries=[])

    def update_summary(self):
        rows=self.effective_published()['entries']
        adopted = next((row for row in rows if event_slot(row['event']) == (self.content_id, self.slot)), None)
        status = '未採用' if adopted is None else ('採用・出力対象' if adopted['enabled'] else '採用・出力対象外')
        scope = '旧ミケ個体専用' if self.identity.get('target_kind') == 'individual' else '画像系統で共有'
        self.summary.config(text=f"選択中：{status}／編集対象：{scope}　全体：採用{len(rows)}件・出力{sum(r['enabled'] for r in rows)}件")

    def refresh_catalog(self,confirm=True):
        if confirm and not self.confirm_leave():return
        catalog=self.catalog_provider()
        ids=list(catalog)
        for store in (self.published,self.drafts):
            if store.path.exists():ids.extend(r['event']['content_cat_id'] for r in store.read_all()['entries'])
        self.targets={f"{catalog.get(cid,'対象不明')}（{cid}）":cid for cid in dict.fromkeys(ids)}
        self.selector['values']=list(self.targets)
        self.content.set(next((name for name,cid in self.targets.items() if cid==self.content_id),next(iter(self.targets),'')))
        self.load_target()

    def load_target(self):
        if self.content.get() not in self.targets:
            self.notice.set('画像対象がありません。');return
        content_id = self.targets[self.content.get()]
        rows=self.drafts.read_all()['entries'] if self.drafts.path.exists() else []
        if content_id != self.content_id:
            self.content_id = content_id
            registered = rows + self.effective_published()['entries']
            slots = [event_slot(row['event']) for row in registered]
            self.slot = min((slot for cid, slot in slots if cid == content_id), default=0)
        self.show_milestone()
        saved=next((r for r in rows if event_slot(r['event']) == (self.content_id, self.slot)),None)
        self.populate(dict(version=1,enabled=saved['enabled'],event=saved['event']) if saved else self.published.read(self.content_id, self.slot))
        self.saved_form=self.collect();self.update_summary()

    def show_milestone(self):
        self.milestone_choice.set(next(label for label, value in self.milestones.items() if value == self.slot))

    def change_milestone(self, _=None):
        if self.confirm_leave():
            self.slot = self.milestones[self.milestone_choice.get()]
            self.load_target()
        else:
            self.show_milestone()

    def change_target(self,_=None):
        if not self.confirm_leave():
            self.content.set(next((name for name,cid in self.targets.items() if cid==self.content_id),''));return
        self.load_target()

    def share_legacy(self):
        from tkinter import messagebox
        if self.identity.get('target_kind')!='individual':raise ValueError('この定義は既に画像系統で共有します。')
        if not messagebox.askokcancel('旧ミケの共有化','同じ画像系統の別個体も開始可能になります。本文中の猫名を確認し、比較してから採用してください。',parent=self.window):return
        self.identity['target_kind']='content';self.identity.pop('cat_id',None);self.identity.pop('revision',None)
        self.reviewed=None;self.update_summary();self.notice.set('共有化は未採用です。本文を確認して比較・採用してください。')

    def confirm_leave(self):
        from tkinter import messagebox
        return self.collect() == self.saved_form or messagebox.askyesno('未保存の編集', '未保存の編集を破棄しますか？', parent=self.window)

    def close(self):
        if self.confirm_leave():
            self.window.destroy()
