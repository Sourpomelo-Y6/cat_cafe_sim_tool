"""別スレッドでLLMへ接続し、生成案を確認して編集欄へ渡す。"""
import queue
import threading
from llm_client import LlmClient, DEFAULT_URL, DEFAULT_OUTPUT, GenerationCancelled


class LlmGenerationWindow:
    def __init__(self, parent, on_result, cat_id, label, *, output=DEFAULT_OUTPUT):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        self.window = tk.Toplevel(parent)
        self.window.title(f'ローカルLLMで文章案を生成 — {label}')
        self.cat_id, self.label, self.on_result, self.output = cat_id, label, on_result, output
        self.messages = queue.Queue()
        self.cancel = threading.Event()
        self.timer = None
        self.running = False
        self.candidate = None
        self.window.bind('<Destroy>', self.destroyed)
        frame = ttk.Frame(self.window, padding=12)
        frame.pack(fill='both', expand=True)
        ttk.Label(frame, text=f'対象：{label}', wraplength=600).pack(anchor='w')
        self.url = tk.StringVar(master=self.window, value=DEFAULT_URL)
        self.model = tk.StringVar(master=self.window)
        ttk.Label(frame, text='接続先（/v1付きでも可）').pack(anchor='w')
        ttk.Entry(frame, textvariable=self.url, width=68).pack(fill='x')
        ttk.Label(frame, text='モデル').pack(anchor='w')
        self.model_selector = ttk.Combobox(frame, textvariable=self.model, width=68)
        self.model_selector.pack(fill='x')
        self.fetch_button = ttk.Button(frame, text='モデル一覧を取得', command=self.fetch)
        self.fetch_button.pack(anchor='w', pady=6)
        ttk.Label(frame, text='猫の特徴（毛色・柄・体格・性格など。名前だけでは推測しません）').pack(anchor='w')
        self.features = ScrolledText(frame, width=68, height=4, wrap='word')
        self.features.pack(fill='x')
        ttk.Label(frame, text='文章の雰囲気・希望').pack(anchor='w')
        self.style = ScrolledText(frame, width=68, height=2, wrap='word')
        self.style.insert('1.0', 'やさしく自然な日本語。短い説明文。猫を主役にする。')
        self.style.pack(fill='x')
        controls = ttk.Frame(frame)
        controls.pack(fill='x', pady=8)
        self.generate_button = ttk.Button(controls, text='紹介文と4場面を生成', command=self.start)
        self.generate_button.pack(side='left')
        self.apply_button = ttk.Button(controls, text='この案を編集欄へ取り込む', command=self.apply)
        self.apply_button.pack(side='left', padx=8)
        self.apply_button.state(['disabled'])
        self.preview = ScrolledText(frame, width=68, height=9, wrap='word', state='disabled')
        self.preview.pack(fill='both', expand=True)
        self.status = tk.StringVar(master=self.window, value='モデル一覧を取得して、特徴を入力してください。')
        ttk.Label(frame, textvariable=self.status, wraplength=600).pack(anchor='w', pady=6)
        ttk.Label(frame, text='生成案は下書き保存・登録しません。閉じると受け取りを中止します。サーバー側の処理は継続する場合があります。',
                  wraplength=600).pack(anchor='w')
        ttk.Button(frame, text='閉じる', command=self.window.destroy).pack(anchor='e')

    def launch(self, kind, work):
        if self.running:
            return
        self.running = True
        for button in (self.fetch_button, self.generate_button, self.apply_button):
            button.state(['disabled'])
        def worker():
            try:
                value = work()
                if not self.cancel.is_set():
                    self.messages.put((kind, value))
            except GenerationCancelled:
                pass
            except Exception as exc:
                if not self.cancel.is_set():
                    self.messages.put(('error', str(exc)))
        threading.Thread(target=worker, daemon=True).start()
        self.poll()

    def fetch(self):
        if self.running:
            return
        try:
            client = LlmClient(self.url.get())
        except ValueError as exc:
            self.status.set(str(exc))
            return
        self.status.set('モデル一覧を取得しています…')
        self.launch('models', client.list_models)

    def start(self):
        if self.running:
            return
        try:
            client = LlmClient(self.url.get())
            model = self.model.get().strip()
            features = self.features.get('1.0', 'end-1c').strip()
            style = self.style.get('1.0', 'end-1c').strip()
            if not model or not features:
                raise ValueError('モデルと猫の特徴を入力してください。')
        except ValueError as exc:
            self.status.set(str(exc))
            return
        self.candidate = None
        self.show('')
        self.status.set('紹介文と4場面の描写を生成しています…（応答待ちは最大180秒）')
        self.launch('result', lambda: client.generate(model, self.cat_id, self.label, features, style,
                                                     self.cancel, output=self.output))

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
                record = self.candidate
                labels = {'normal': '通常', 'play': '遊ぶ', 'pet': '撫でられる', 'sleep': '眠る'}
                self.show('紹介文：' + record['introduction'] + '\n\n' + '\n'.join(
                    label + '：' + record['scenes'][key] for key, label in labels.items()))
                self.status.set(f'生成案を確認して取り込んでください。生成記録：{path}')
            else:
                self.status.set(f'処理できません：{value}')
            self.apply_button.state(['!disabled'] if self.candidate is not None else ['disabled'])
        if self.running:
            self.timer = self.window.after(100, self.poll)

    def show(self, text):
        self.preview.configure(state='normal')
        self.preview.delete('1.0', 'end')
        self.preview.insert('1.0', text)
        self.preview.configure(state='disabled')

    def apply(self):
        if self.running or self.candidate is None:
            return
        if self.on_result(self.candidate):
            self.status.set('編集欄へ取り込みました。修正し、下書き保存・登録内容の確認へ進んでください。')
        else:
            self.status.set('編集欄が変更されたため取り込めません。現在の編集を保存し、この生成画面を閉じて開き直してください。')

    def destroyed(self, event):
        if event.widget == self.window:
            self.cancel.set()
            if self.timer is not None:
                self.window.after_cancel(self.timer)
                self.timer = None
