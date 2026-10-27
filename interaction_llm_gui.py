"""選択条件で3案を生成し、選んだ案だけ指定欄へ取り込む。"""
import copy
import queue
import threading

from llm_generation_gui import LlmGenerationWindow
from interaction_llm import InteractionLlmClient, InteractionLlmSettings, DEFAULT_OUTPUT, validate_inputs
from interaction_texts import KINDS, SUBJECTS, MODES, REACTIONS
from interaction_scenes import ACTION_LABELS
from interaction_texts import NARRATIVE_SCENES


class InteractionLlmWindow(LlmGenerationWindow):
    # 既存の非同期通信・破棄時キャンセルを共有する。回答表示は交流文専用。
    def __init__(self, parent, on_result, value, *, settings=None, output=DEFAULT_OUTPUT):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        self.settings = settings or InteractionLlmSettings()
        self.subjects=self.settings.subject_registry.labels(include_disabled=True) if self.settings.subject_registry else SUBJECTS
        saved = self.settings.get(value)  # 壊れた設定は開く前に報告し、上書きしない。
        self.value = copy.deepcopy(value)
        self.on_result, self.output = on_result, output
        self.messages, self.cancel = queue.Queue(), threading.Event()
        self.timer, self.running, self.candidate = None, False, None
        self.window = tk.Toplevel(parent)
        self.window.title('日常交流文 — ローカルLLMで3案生成')
        self.window.geometry('820x720')
        self.window.minsize(740, 670)
        self.window.bind('<Destroy>', self.destroyed)
        frame = ttk.Frame(self.window, padding=12)
        frame.pack(fill='both', expand=True)
        self.target = (NARRATIVE_SCENES[value.get('narrative_scene','normal')] + ' ／ ' + KINDS[value['kind']] + ' ／ ' + self.subjects[value['kind']][value['subject_id']]
                       + ' ／ ' + MODES[value['mode']] + ' ／ '
                       + ACTION_LABELS.get(value['action'], '動作共通') + ' ／ '
                       + REACTIONS.get(value['reaction'], '人物のセリフ・結果指定なし')
                       + '\n画像キー：' + value['scene_key'])
        ttk.Label(frame, text=self.target, wraplength=710).pack(anchor='w')
        self.url = tk.StringVar(master=self.window, value=saved['url'])
        self.model = tk.StringVar(master=self.window, value=saved['model'])
        ttk.Label(frame, text='接続先（/v1付きでも可）').pack(anchor='w')
        ttk.Entry(frame, textvariable=self.url).pack(fill='x')
        ttk.Label(frame, text='モデル（一覧取得または直接入力）').pack(anchor='w')
        self.model_selector = ttk.Combobox(frame, textvariable=self.model)
        self.model_selector.pack(fill='x')
        self.tabs = ttk.Notebook(frame)
        self.tabs.pack(fill='both', expand=True, pady=8)
        inputs, results = ttk.Frame(self.tabs, padding=8), ttk.Frame(self.tabs, padding=8)
        self.tabs.add(inputs, text='生成に使う資料')
        self.tabs.add(results, text='生成した3案・取り込み先')
        self.results_tab = results
        self.notes = {}
        for name, label, height in [('subject_notes', '人物の口調・設定' if value['kind'] == 'speech'
                                     else '猫の特徴・性格', 4),
                                    ('image_notes', '対応する絵の状態（画像は送信しません）', 3),
                                    ('style', '文章の雰囲気・希望', 2)]:
            ttk.Label(inputs, text=label).pack(anchor='w')
            field = ScrolledText(inputs, height=height, wrap='word', undo=True)
            field.insert('1.0', saved[name])
            field.pack(fill='both', expand=True, pady=(0, 6))
            self.notes[name] = field
        self.selected, self.destinations, self.result_fields = [], [], []
        for i in range(3):
            row = ttk.Frame(results)
            row.pack(fill='x')
            checked = tk.BooleanVar(master=self.window, value=True)
            ttk.Checkbutton(row, text=f'生成案{i + 1}を取り込む', variable=checked).pack(side='left')
            ttk.Label(row, text='　取り込み先：').pack(side='left')
            slot = tk.StringVar(master=self.window, value=f'案{i + 1}')
            ttk.Combobox(row, textvariable=slot, values=['案1', '案2', '案3'],
                         state='readonly', width=8).pack(side='left')
            field = ScrolledText(results, height=2, wrap='word', state='disabled')
            field.pack(fill='both', expand=True, pady=(2, 8))
            self.selected.append(checked)
            self.destinations.append(slot)
            self.result_fields.append(field)
        self.apply_button = ttk.Button(results, text='選択した案を編集欄へ取り込む', command=self.apply)
        self.apply_button.pack(anchor='w')
        self.apply_button.state(['disabled'])
        controls = ttk.Frame(frame)
        controls.pack(fill='x')
        self.fetch_button = ttk.Button(controls, text='モデル一覧を取得', command=self.fetch)
        self.fetch_button.pack(side='left')
        self.generate_button = ttk.Button(controls, text='この条件で3案生成', command=self.start)
        self.generate_button.pack(side='left', padx=8)
        ttk.Button(controls, text='接続・資料の設定を保存', command=self.save_settings).pack(side='left')
        self.status = tk.StringVar(master=self.window, value='資料を入力して生成してください。名前だけでは設定を推測しません。')
        ttk.Label(frame, textvariable=self.status, wraplength=710).pack(anchor='w', pady=6)
        ttk.Label(frame, text='生成・取り込みだけでは下書き保存・登録しません。入力の再利用には「設定を保存」を押してください。\n'
                  '閉じると結果の受け取りを中止します。サーバー側の処理は継続する場合があります。',
                  wraplength=710).pack(anchor='w')
        ttk.Button(frame, text='閉じる', command=self.window.destroy).pack(anchor='e')

    def inputs(self):
        return {key: field.get('1.0', 'end-1c').strip() for key, field in self.notes.items()}

    def fetch(self):
        if self.running:
            return
        try:
            client = InteractionLlmClient(self.url.get(), subject_registry=self.settings.subject_registry)
        except ValueError as exc:
            self.status.set(str(exc))
            return
        self.status.set('モデル一覧を取得しています…')
        self.launch('models', client.list_models)

    def save_settings(self):
        try:
            self.settings.save(self.value, self.url.get(), self.model.get(), **self.inputs())
            self.status.set('接続・資料の設定を保存しました。文章の下書き・登録は変更していません。')
        except (OSError, ValueError) as exc:
            self.status.set(f'設定を保存できません：{exc}')

    def start(self):
        if self.running:
            return
        try:
            client = InteractionLlmClient(self.url.get(), subject_registry=self.settings.subject_registry)
            model, notes = self.model.get().strip(), self.inputs()
            validate_inputs(**notes)
            if not model:
                raise ValueError('モデルを選んでください。')
        except ValueError as exc:
            self.status.set(str(exc))
            return
        self.candidate = None
        self.show_candidates(['', '', ''])
        self.status.set('選択条件で3案を生成しています…（応答待ちは最大180秒）')
        self.launch('result', lambda: client.generate_variants(model, self.value, **notes,
                                                              cancel=self.cancel, output=self.output))

    def poll(self):
        self.timer = None
        while not self.messages.empty():
            kind, value = self.messages.get_nowait()
            self.running = False
            self.fetch_button.state(['!disabled'])
            self.generate_button.state(['!disabled'])
            if kind == 'models':
                self.model_selector.configure(values=value)
                if self.model.get() not in value:
                    self.model.set(value[0])
                self.status.set(f'{len(value)}件のモデルを取得しました。')
            elif kind == 'result':
                self.candidate, path = value
                self.show_candidates(self.candidate)
                self.tabs.select(self.results_tab)
                self.status.set(f'案と取り込み先を確認してください。生成記録：{path.name}')
            else:
                self.status.set(f'処理できません：{value}')
            self.apply_button.state(['!disabled'] if self.candidate is not None else ['disabled'])
        if self.running:
            self.timer = self.window.after(100, self.poll)

    def show_candidates(self, texts):
        for widget, text in zip(self.result_fields, texts):
            widget.configure(state='normal')
            widget.delete('1.0', 'end')
            widget.insert('1.0', text)
            widget.configure(state='disabled')

    def apply(self):
        if self.running or self.candidate is None:
            return
        changes = {}
        for i, selected in enumerate(self.selected):
            if not selected.get():
                continue
            label = self.destinations[i].get()
            if label not in ('案1', '案2', '案3'):
                self.status.set('取り込み先を案1〜3から選んでください。')
                return
            slot = int(label[-1]) - 1
            if slot in changes:
                self.status.set('取り込み先が重複しています。案ごとに別の欄を選んでください。')
                return
            changes[slot] = self.candidate[i]
        if not changes:
            self.status.set('取り込む案を1つ以上選んでください。')
            return
        if self.on_result(changes):
            self.status.set('選択した案を取り込みました。編集画面で修正・保存・登録確認を行ってください。')
        else:
            self.status.set('編集や条件が変わったか、既存の案と重複しています。編集を確認し、生成画面を開き直してください。')
