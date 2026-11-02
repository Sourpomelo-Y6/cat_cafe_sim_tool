"""Edit the single optional story without exposing a script/graph editor."""
import copy
from optional_events import BRANCHES, export_event_pack


class OptionalEventEditorWindow:
    def __init__(self, parent, drafts, published, catalog_provider):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        self.drafts, self.published, self.catalog_provider = drafts, published, catalog_provider
        adopted = published.read()  # A broken store must never become an empty overwrite.
        data = drafts.read() if drafts.path.exists() else adopted
        self.window = tk.Toplevel(parent)
        self.window.title('ミケの個別イベント — 編集・確認・出力')
        self.window.geometry('1020x800')
        self.window.minsize(780, 660)
        self.window.protocol('WM_DELETE_WINDOW', self.close)
        self.notice = tk.StringVar(master=self.window)
        self.enabled = tk.BooleanVar(master=self.window)
        outer = ttk.Frame(self.window, padding=12)
        outer.pack(fill='both', expand=True)
        ttk.Label(outer, text='ミケのイベント1件。導入3行 → 選択肢2つ → 各結果2行。数値報酬はありません。').pack(anchor='w')
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
            self.drafts.save(self.validated())
            self.saved_form = self.collect()
            self.notice.set('下書きを保存しました。採用・Unity出力はまだ行っていません。')
        return self._run(save)

    def preview(self):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        def show():
            data = self.validated()
            adopted = self.published.read()
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
            data = self.validated()
            if self.reviewed != data:
                raise ValueError('プレビュー・採用比較で現在の内容を確認してください。')
            self.published.save(data)
            self.drafts.save(data)
            self.saved_form = self.collect()
            self.notice.set('採用登録しました。次のUnity出力に含まれます。')
        return self._run(save)

    def load_published(self):
        from tkinter import messagebox
        if self.collect() != self.saved_form and not messagebox.askyesno('編集中の内容', '未保存の編集を破棄して採用内容を読み直しますか？', parent=self.window):
            return
        def load():
            self.populate(self.published.read())
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
                self.populate(self.drafts.import_pack(path))
                self.saved_form = self.collect()
                self.notice.set('下書きへ読み込みました。採用内容は変更していません。')
            self._run(load)

    def export(self):
        from tkinter import filedialog
        path = filedialog.askdirectory(parent=self.window, title='採用イベントの出力先')
        if path:
            self._run(lambda: self.notice.set('採用内容を出力しました：' + str(export_event_pack(self.published, self.catalog_provider(), path))))

    def confirm_leave(self):
        from tkinter import messagebox
        return self.collect() == self.saved_form or messagebox.askyesno('未保存の編集', '未保存の編集を破棄しますか？', parent=self.window)

    def close(self):
        if self.confirm_leave():
            self.window.destroy()
