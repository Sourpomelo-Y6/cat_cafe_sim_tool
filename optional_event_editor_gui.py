"""Edit the single optional story without exposing a script/graph editor."""
import copy
from optional_events import BRANCHES, export_event_pack


class OptionalEventEditorWindow:
    def __init__(self, parent, drafts, published, catalog_provider):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        self.drafts, self.published, self.catalog_provider = drafts, published, catalog_provider
        self.import_review_pending=False;self.reviewed_all=None
        self.content_id='playtest-mike'
        adopted = published.read(self.content_id)  # A broken store must never become an empty overwrite.
        data = drafts.read() if drafts.path.exists() else adopted
        self.window = tk.Toplevel(parent)
        self.window.title('猫の個別イベント — 編集・確認・出力')
        self.window.geometry('1020x800')
        self.window.minsize(780, 660)
        self.window.protocol('WM_DELETE_WINDOW', self.close)
        self.notice = tk.StringVar(master=self.window)
        self.enabled = tk.BooleanVar(master=self.window)
        outer = ttk.Frame(self.window, padding=12)
        outer.pack(fill='both', expand=True)
        ttk.Label(outer, text='1系統1イベント。進行は個体別。導入3行 → 2択 → 各結果2行。数値報酬なし。').pack(anchor='w')
        self.content=tk.StringVar(master=self.window)
        self.selector=ttk.Combobox(outer,textvariable=self.content,state='readonly')
        self.selector.pack(fill='x');self.selector.bind('<<ComboboxSelected>>',self.change_target)
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
            ttk.Entry(fields, textvariable=value).grid(row=index, column=1, sticky='ew', padx=8, pady=3)
        ttk.Checkbutton(outer, text='採用後の出力に含める（外しても開始済みの会話は保持）', variable=self.enabled).pack(anchor='w')
        book = ttk.Notebook(outer)
        book.pack(fill='both', expand=True, pady=8)
        self.lines = {}
        for branch, caption, count in zip(BRANCHES, ('導入', '結果1', '結果2'), (3, 2, 2)):
            page = ttk.Frame(book, padding=8)
            book.add(page, text=caption)
            page.columnconfigure(1, weight=1)
            self.lines[branch] = []
            for i in range(count):
                page.rowconfigure(i * 2 + 1, weight=1)
                ttk.Label(page, text=f'{i+1}行目の話者（空欄は地の文）').grid(row=i*2, column=0, sticky='w')
                speaker = tk.StringVar(master=self.window)
                ttk.Entry(page, textvariable=speaker).grid(row=i*2, column=1, sticky='ew', padx=8)
                text = ScrolledText(page, height=3, wrap='word', undo=True)
                text.grid(row=i*2+1, column=0, columnspan=2, sticky='nsew', pady=(4, 10))
                self.lines[branch].append((speaker, text))
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

    def populate(self, data):
        self.identity = copy.deepcopy(data['event'])
        self.enabled.set(data['enabled'])
        for key, value in self.values.items():
            value.set(str(data['event'][key]))
        for branch in BRANCHES:
            for (speaker, widget), line in zip(self.lines[branch], data['event'][branch]):
                speaker.set(line['speaker'])
                widget.delete('1.0', 'end')
                widget.insert('1.0', line['text'])
        self.reviewed = None
        self.reviewed_all = None

    def collect(self):
        event = copy.deepcopy(self.identity)
        for key, value in self.values.items():
            event[key] = value.get()
        for branch in BRANCHES:
            event[branch] = [dict(speaker=s.get(), text=t.get('1.0', 'end-1c')) for s, t in self.lines[branch]]
        return dict(version=1, enabled=self.enabled.get(), event=event)

    def validated(self):
        from optional_events import validate_event
        data = self.collect()
        for key in ('min_day', 'min_affinity'):
            if not data['event'][key].isascii() or not data['event'][key].isdigit():
                raise ValueError('日数・好感度は整数で入力してください。')
            data['event'][key] = int(data['event'][key])
        data['event'].pop('revision', None)
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
            self.notice.set('下書きを保存しました。採用・Unity出力はまだ行っていません。')
        return self._run(save)

    def preview(self):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        def show():
            if self.import_review_pending:
                return self.preview_all()
            data = self.validated()
            adopted = self.published.read(self.content_id)
            window = tk.Toplevel(self.window)
            window.title('プレビューと採用内容の比較')
            window.geometry('1000x700')
            book = ttk.Notebook(window)
            book.pack(fill='both', expand=True)
            for branch, label in (('accepted', '選択肢1の流れ'), ('declined', '選択肢2の流れ')):
                pane = ttk.Frame(book)
                book.add(pane, text=label)
                for content, heading in ((adopted, '現在の採用'), (data, '編集中')):
                    text = ScrolledText(pane, width=48, wrap='word')
                    text.pack(side='left', fill='both', expand=True)
                    e = content['event']
                    text.insert('end', heading + '\n' + e['title'] + f"\n{e['min_day']}日目以降 / 好感度{e['min_affinity']}以上\n\n")
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
            self.notice.set('採用登録しました。次のUnity出力に含まれます。')
        return self._run(save)

    def load_published(self):
        from tkinter import messagebox
        if self.collect() != self.saved_form and not messagebox.askyesno('編集中の内容', '未保存の編集を破棄して採用内容を読み直しますか？', parent=self.window):
            return
        def load():
            self.import_review_pending=False
            self.populate(self.published.read(self.content_id))
            self.saved_form = self.collect()
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
                if data['events']:rows=[dict(enabled=True,event=normalize_event(e)) for e in data['events']]
                else:
                    merged={r['event']['content_cat_id']:r for r in self.published.read_all()['entries']} if self.published.path.exists() or 'playtest-mike' in self.catalog_provider() else {}
                    if self.drafts.path.exists():merged.update({r['event']['content_cat_id']:r for r in self.drafts.read_all()['entries']})
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
        if any(r['event']['content_cat_id']==self.content_id for r in rows) or self.collect()!=self.saved_form:
            current=self.validated();rows=[r for r in rows if r['event']['content_cat_id']!=self.content_id]
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
                for branch in BRANCHES:
                    result.append({'introduction':'導入','accepted':e['accept_caption'],'declined':e['decline_caption']}[branch])
                    result.extend((line['speaker'] or '地の文')+'：'+line['text'] for line in e[branch])
                result.append('')
            return '\n'.join(result)
        data=self.bulk_collection();window=tk.Toplevel(self.window);window.title('全下書きと全採用の比較');window.geometry('1000x700')
        for value,label in ((self.effective_published(),'現在の全採用'),(data,'採用予定の全下書き')):
            text=ScrolledText(window,width=48,wrap='word');text.pack(side='left',fill='both',expand=True);text.insert('end',label+'\n\n'+describe(value));text.configure(state='disabled')
        self.reviewed=self.collect();self.reviewed_all=data
        self.notice.set(f"全{len(data['entries'])}系統の両分岐を比較しました。採用ボタンで全体を確定します。")

    def effective_published(self):
        return self.published.read_all() if self.published.path.exists() or 'playtest-mike' in self.catalog_provider() else dict(version=2,entries=[])

    def update_summary(self):
        rows=self.effective_published()['entries']
        self.summary.config(text=f"採用：{len(rows)}系統／出力：{sum(r['enabled'] for r in rows)}件。"+('対象：旧ミケ個体専用' if self.identity.get('target_kind')=='individual' else '対象：画像系統で共有'))

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
        self.content_id=self.targets[self.content.get()]
        rows=self.drafts.read_all()['entries'] if self.drafts.path.exists() else []
        saved=next((r for r in rows if r['event']['content_cat_id']==self.content_id),None)
        self.populate(dict(version=1,enabled=saved['enabled'],event=saved['event']) if saved else self.published.read(self.content_id))
        self.saved_form=self.collect();self.update_summary()

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
