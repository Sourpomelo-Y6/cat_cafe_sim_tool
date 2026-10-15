"""人物のセリフと猫の反応を条件別に連続生成し、個別に確認する。"""
import copy
import queue
import threading

from interaction_llm import InteractionLlmClient, InteractionLlmSettings, DEFAULT_OUTPUT, validate_inputs
from interaction_texts import SUBJECTS, MODES, REACTIONS, condition, make_entry, key, actions, reactions
from interaction_scenes import ACTION_LABELS
from llm_client import GenerationCancelled


def prepare_jobs(settings, subjects, modes, *, scope='common', has_text=None, kind='speech', selected_reactions=None):
    """開始前に全資料を検証し、実行中の設定変更から切り離す。"""
    if scope not in ('common', 'specific', 'all'):
        raise ValueError('操作の範囲が不正です。')
    if kind not in SUBJECTS:
        raise ValueError('文章の種類が不正です。')
    chosen_reactions = list(REACTIONS) if selected_reactions is None else list(selected_reactions)
    if kind == 'cat_response' and (not chosen_reactions or any(r not in REACTIONS for r in chosen_reactions)):
        raise ValueError('猫の反応を1つ以上選んでください。')
    jobs = []
    for subject in subjects:
        if subject not in SUBJECTS[kind]:
            raise ValueError('対象の人物・猫が不正です。')
        for mode in modes:
            if mode not in MODES:
                raise ValueError('行動が不正です。')
            for action in actions(mode):
                common = action in ('any', 'pause')
                if scope == 'common' and not common or scope == 'specific' and common:
                    continue
                for reaction in reactions(kind, mode):
                    if kind == 'cat_response' and reaction not in chosen_reactions:
                        continue
                    value = condition(kind, subject, mode, action, reaction)
                    if has_text is not None and has_text(value):
                        continue
                    notes = settings.get(value)
                    try:
                        validate_inputs(notes['subject_notes'], notes['image_notes'], notes['style'])
                    except ValueError as exc:
                        raise ValueError(f"{InteractionBatchWindow.label(value)}：{exc}") from exc
                    jobs.append((value, copy.deepcopy(notes)))
    if not jobs:
        raise ValueError('生成対象がありません。人物・行動の選択と入力済み条件を確認してください。')
    return jobs


class InteractionBatchWindow:
    def __init__(self, parent, on_result, *, settings=None, output=DEFAULT_OUTPUT, has_text=None, kind='speech'):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        self.settings = settings or InteractionLlmSettings()
        saved = self.settings.read()
        self.output, self.on_result = output, on_result
        self.has_text = has_text
        if kind not in SUBJECTS:
            raise ValueError('文章の種類が不正です。')
        self.kind = kind
        cat = kind == 'cat_response'
        self.jobs, self.results = [], {}
        self.messages, self.cancel = queue.Queue(), threading.Event()
        self.running, self.timer = False, None
        self.window = tk.Toplevel(parent)
        self.window.title('猫の反応を連続生成' if cat else '人物のセリフを連続生成')
        self.window.geometry('900x780' if cat else '860x700')
        self.window.minsize(800 if cat else 760, 720 if cat else 650)
        self.window.bind('<Destroy>', self.destroyed)
        frame = ttk.Frame(self.window, padding=12)
        frame.pack(fill='both', expand=True)
        ttk.Label(frame, text='猫 × 行動 × 確定反応ごとに3案ずつ生成します。' if cat else '人物 × 行動ごとに3案ずつ、1条件ずつ順番に生成します。', font=('', 12, 'bold')).pack(anchor='w')
        ttk.Label(frame, text=('拒否・低体力などは別条件です。休憩は通常・低体力のみ生成します。\n' if cat else
                  '共通文に加え「激しく動かす」「フェイント」など操作別のセリフも生成できます。\n') +
                  '生成だけでは下書き・登録済み文章を変更しません。', wraplength=720).pack(anchor='w', pady=6)
        choices = ttk.LabelFrame(frame, text='生成する対象', padding=8)
        choices.pack(fill='x')
        self.subjects, self.modes, self.option_widgets = {}, {}, []
        for row, (target, labels) in enumerate(((self.subjects, SUBJECTS[kind]), (self.modes, MODES))):
            for col, (name, label) in enumerate(labels.items()):
                var = tk.BooleanVar(master=self.window, value=True)
                widget = ttk.Checkbutton(choices, text=label, variable=var)
                widget.grid(row=row, column=col, sticky='w', padx=10)
                self.option_widgets.append(widget)
                target[name] = var
        self.scopes = {'動作共通のみ': 'common', '操作別のみ': 'specific', '共通＋操作別': 'all'}
        self.scope = tk.StringVar(master=self.window, value='共通＋操作別' if has_text and not cat else '動作共通のみ')
        scope_selector = ttk.Combobox(choices, textvariable=self.scope, values=list(self.scopes), state='readonly', width=18)
        scope_selector.grid(row=2, column=0, sticky='w', padx=10, pady=(6, 0))
        self.only_missing = tk.BooleanVar(master=self.window, value=has_text is not None)
        missing = ttk.Checkbutton(choices, text='未入力の条件だけ生成', variable=self.only_missing)
        missing.grid(row=2, column=1, columnspan=2, sticky='w', padx=10, pady=(6, 0))
        if has_text is None:
            missing.state(['disabled'])
        self.option_widgets.extend([scope_selector, missing])
        self.reaction_choices = {}
        if cat:
            reaction_frame = ttk.LabelFrame(frame, text='生成する反応（初期範囲：共通14条件・42案、入力済みは除外）', padding=6)
            reaction_frame.pack(fill='x', pady=(6, 0))
            for i, (name, label) in enumerate(REACTIONS.items()):
                var = tk.BooleanVar(master=self.window, value=True)
                widget = ttk.Checkbutton(reaction_frame, text=label, variable=var)
                widget.grid(row=i//3, column=i%3, sticky='w', padx=10)
                self.reaction_choices[name] = var
                self.option_widgets.append(widget)
        connection = ttk.Frame(frame)
        connection.pack(fill='x', pady=8)
        self.url = tk.StringVar(master=self.window, value=saved['url'])
        self.model = tk.StringVar(master=self.window, value=saved['model'])
        self.connection_widgets = []
        for row, (label, var) in enumerate((('接続先', self.url), ('モデル', self.model))):
            ttk.Label(connection, text=label).grid(row=row, column=0, sticky='w', padx=(0, 8))
            widget = ttk.Entry(connection, textvariable=var)
            widget.grid(row=row, column=1, sticky='ew', pady=2)
            self.option_widgets.append(widget)
            self.connection_widgets.append(widget)
        connection.columnconfigure(1, weight=1)
        ttk.Label(frame, text='保存済みの人物・猫の資料と条件別メモを使います。資料の修正は単条件の生成画面で保存してください。', wraplength=720).pack(anchor='w')
        table = ttk.Frame(frame)
        table.pack(fill='x', pady=8)
        self.tree = ttk.Treeview(table, columns=('target', 'status'), show='headings', height=6, selectmode='browse')
        self.tree.heading('target', text='猫／行動／操作／反応' if cat else '人物／行動／操作')
        self.tree.heading('status', text='進捗')
        self.tree.column('target', width=460 if cat else 270)
        self.tree.column('status', width=160 if cat else 260)
        self.tree.pack(side='left', fill='x', expand=True)
        scrollbar = ttk.Scrollbar(table, orient='vertical', command=self.tree.yview)
        scrollbar.pack(side='right', fill='y')
        self.tree.configure(yscrollcommand=scrollbar.set)
        self.tree.bind('<<TreeviewSelect>>', self.show_result)
        self.preview = ScrolledText(frame, height=6, wrap='word', state='disabled')
        self.preview.pack(fill='both', expand=True)
        controls = ttk.Frame(frame)
        controls.pack(fill='x', pady=8)
        self.start_button = ttk.Button(controls, text='選択条件を連続生成', command=self.start)
        self.start_button.pack(side='left')
        self.stop_button = ttk.Button(controls, text='停止', command=self.stop, state='disabled')
        self.stop_button.pack(side='left', padx=8)
        self.apply_button = ttk.Button(controls, text='選択結果の3案を編集欄へ', command=self.apply, state='disabled')
        self.apply_button.pack(side='left')
        ttk.Button(controls, text='閉じる', command=self.window.destroy).pack(side='right')
        self.status = tk.StringVar(master=self.window, value='対象と接続設定を確認して開始してください。')
        ttk.Label(frame, textvariable=self.status, wraplength=720).pack(anchor='w')
        ttk.Label(frame, text='失敗・停止時は残りを再開できます。完了済み結果は再生成しません。\n'
                  '停止・閉じる後も実行中のサーバー処理は続く場合があります。各条件の応答待ちは最大180秒。', wraplength=720).pack(anchor='w', pady=(6, 0))

    def start(self):
        if self.running:
            return
        try:
            client = InteractionLlmClient(self.url.get())
            model = self.model.get().strip()
            if not model:
                raise ValueError('モデルを入力してください。')
            if not self.jobs:
                self.jobs = prepare_jobs(self.settings, [k for k, v in self.subjects.items() if v.get()],
                    [k for k, v in self.modes.items() if v.get()], scope=self.scopes[self.scope.get()],
                    has_text=self.has_text if self.only_missing.get() else None, kind=self.kind,
                    selected_reactions=[k for k, v in self.reaction_choices.items() if v.get()] if self.kind == 'cat_response' else None)
                for value, _ in self.jobs:
                    self.tree.insert('', 'end', iid=key(value), values=(self.label(value), '待機'))
            remaining = [(v, n) for v, n in self.jobs if key(v) not in self.results]
            if not remaining:
                self.status.set('全条件が完了しています。結果を選んで確認してください。')
                return
        except (OSError, ValueError) as exc:
            self.status.set(str(exc) if str(exc).startswith('生成対象がありません') else f'開始できません：{exc}')
            return
        self.cancel.clear()
        self.running = True
        self.start_button.state(['disabled'])
        self.stop_button.state(['!disabled'])
        self.apply_button.state(['disabled'])
        for widget in self.option_widgets:
            widget.state(['disabled'])
        def worker():
            try:
                for value, notes in remaining:
                    if self.cancel.is_set():
                        break
                    self.messages.put(('working', value))
                    try:
                        variants, path = client.generate_variants(model, value, notes['subject_notes'],
                            notes['image_notes'], notes['style'], self.cancel, output=self.output)
                        if self.cancel.is_set():
                            break
                        self.messages.put(('result', (make_entry(value, variants), path)))
                    except GenerationCancelled:
                        break
                    except Exception as exc:
                        self.messages.put(('error', (value, str(exc))))
                        break
            finally:
                self.messages.put(('done', None))
        threading.Thread(target=worker, daemon=True).start()
        self.poll()

    @staticmethod
    def label(value):
        return (SUBJECTS[value['kind']][value['subject_id']] + '／' + MODES[value['mode']]
                + '／' + ACTION_LABELS.get(value['action'], '動作共通')
                + ('／' + REACTIONS[value['reaction']] if value['kind'] == 'cat_response' else ''))

    def poll(self):
        self.timer = None
        while not self.messages.empty():
            kind, payload = self.messages.get_nowait()
            if kind == 'working':
                self.tree.set(key(payload), 'status', '生成中')
                self.status.set(f'{len(self.results)}/{len(self.jobs)}条件完了　' + self.label(payload) + 'を生成中…')
            elif kind == 'result':
                entry, path = payload
                ident = key(entry['condition'])
                self.results[ident] = (entry, path)
                self.tree.set(ident, 'status', '完了・未取り込み')
                self.tree.selection_set(ident)
                self.show_result()
            elif kind == 'error':
                value, error = payload
                self.tree.set(key(value), 'status', '失敗・再開待ち')
                self.status.set('この条件で停止しました：' + error)
            elif kind == 'done':
                self.running = False
                self.stop_button.state(['disabled'])
                self.start_button.configure(text='未完了の条件を再開')
                self.start_button.state(['!disabled'])
                for widget in self.connection_widgets:
                    widget.state(['!disabled'])
                for ident in self.tree.get_children():
                    if self.tree.set(ident, 'status') == '生成中':
                        self.tree.set(ident, 'status', '停止・再開待ち')
                if len(self.results) == len(self.jobs):
                    self.status.set(f'{len(self.results)}条件の生成が完了しました。結果を確認して編集欄へ取り込んでください。')
                elif self.cancel.is_set():
                    self.status.set(f'{len(self.results)}/{len(self.jobs)}条件完了で停止しました。完了済みの結果は残っています。')
                self.show_result()
        if self.running:
            self.timer = self.window.after(100, self.poll)

    def show_result(self, event=None):
        selected = self.tree.selection()
        result = self.results.get(selected[0]) if selected else None
        text = ''
        if result:
            entry, path = result
            text = self.label(entry['condition']) + '\n\n' + '\n\n'.join(
                f"案{row['slot']}：{row['text']}" for row in entry['variants']) + '\n\n生成記録：' + str(path)
        self.preview.configure(state='normal')
        self.preview.delete('1.0', 'end')
        self.preview.insert('1.0', text)
        self.preview.configure(state='disabled')
        self.apply_button.state(['!disabled'] if result and not self.running else ['disabled'])

    def apply(self):
        selected = self.tree.selection()
        if self.running or not selected or selected[0] not in self.results:
            return
        entry, _ = self.results[selected[0]]
        if self.on_result(copy.deepcopy(entry)):
            self.tree.set(selected[0], 'status', '編集欄へ取り込み済み')
            self.status.set('編集欄へ取り込みました。下書き保存・内容確認・登録は編集画面で行ってください。')
        else:
            self.status.set('取り込みを中止しました。現在の編集内容を確認してください。結果は残っています。')

    def stop(self):
        if self.running:
            self.cancel.set()
            self.stop_button.state(['disabled'])
            self.status.set('停止を要求しました。応答待ちが終わるまで再開できません。')

    def destroyed(self, event):
        if event.widget == self.window:
            self.cancel.set()
            if self.timer is not None:
                self.window.after_cancel(self.timer)
                self.timer = None
