"""ComfyUI生成画面。通信は作業スレッド、画面更新はTk側で行う。"""
import queue
import threading
from comfy_client import ComfyClient, GenerationCancelled, random_seed, workflow_defaults

DEFAULT_POSITIVE = ('clean lines, best quality, soft 2d illustration, one adult calico cat, '
                    'white chest and paws, orange and black patches, amber eyes, sitting calmly, '
                    'full body, simple ivory wall, light wooden floor, sage green mat, '
                    'fixed eye-level view, warm soft lighting, no people, no text')


class ComfyGenerationWindow:
    def __init__(self, parent, on_result, *, positive=DEFAULT_POSITIVE, prompt_provider=None, label=''):
        import tkinter as tk
        from tkinter import ttk
        self.window = tk.Toplevel(parent)
        self.window.title('ComfyUIで画像生成' + (f' — {label}' if label else ''))
        self.prompt_provider = prompt_provider
        self.on_result = on_result
        self.cancel = threading.Event()
        self.messages = queue.Queue()
        self.running = False
        self.timer = None
        self.window.protocol('WM_DELETE_WINDOW', self.close)
        self.window.bind('<Destroy>', self.destroyed)
        defaults = workflow_defaults()
        self.defaults = defaults
        frame = ttk.Frame(self.window, padding=12)
        frame.pack(fill='both', expand=True)
        frame.columnconfigure(1, weight=1)
        self.fields = {}
        values = [('url', '接続先', 'http://127.0.0.1:8188'), ('model', 'モデル', defaults['model']),
                  ('seed', 'seed', str(random_seed())), ('width', '幅', str(defaults['width'])),
                  ('height', '高さ', str(defaults['height'])), ('steps', 'Steps', str(defaults['steps'])),
                  ('cfg', 'CFG', str(defaults['cfg']))]
        for row, (key, label, value) in enumerate(values):
            ttk.Label(frame, text=label).grid(row=row, column=0, sticky='w', padx=(0, 8))
            variable = tk.StringVar(master=self.window, value=value)
            self.fields[key] = variable
            ttk.Entry(frame, textvariable=variable, width=55).grid(row=row, column=1, sticky='ew', pady=2)
        ttk.Label(frame, text=f"Sampler: {defaults['sampler']} / Scheduler: {defaults['scheduler']}").grid(row=7, columnspan=2, sticky='w')
        ttk.Label(frame, text='プロンプト（猫の外見・行動・固定背景）').grid(row=8, columnspan=2, sticky='w', pady=(8, 0))
        self.positive = tk.Text(frame, height=5, width=65, wrap='word')
        self.positive.grid(row=9, columnspan=2, sticky='ew')
        self.positive.insert('1.0', positive)
        ttk.Label(frame, text='ネガティブプロンプト').grid(row=10, columnspan=2, sticky='w', pady=(8, 0))
        self.negative = tk.Text(frame, height=3, width=65, wrap='word')
        self.negative.grid(row=11, columnspan=2, sticky='ew')
        self.negative.insert('1.0', defaults['negative'])
        controls = ttk.Frame(frame)
        controls.grid(row=12, columnspan=2, sticky='ew', pady=8)
        self.generate_button = ttk.Button(controls, text='1枚生成', command=self.start)
        self.generate_button.pack(side='left')
        if prompt_provider is not None:
            ttk.Button(controls, text='保存設定を読み直す', command=self.reload_prompt).pack(side='left', padx=8)
        ttk.Button(controls, text='seedをランダムに', command=lambda: self.fields['seed'].set(str(random_seed()))).pack(side='left', padx=8)
        ttk.Button(controls, text='閉じる', command=self.close).pack(side='right')
        self.status = tk.StringVar(master=self.window, value='生成結果は制作画面へ戻します。登録は確認後に行ってください。')
        ttk.Label(frame, textvariable=self.status, wraplength=570).grid(row=13, columnspan=2, sticky='w')
        ttk.Label(frame, text='閉じてもComfyUI側のジョブは継続します。結果の受け取りだけを中止します。', wraplength=570).grid(row=14, columnspan=2, sticky='w', pady=(6, 0))

    def reload_prompt(self):
        if self.running:
            return
        from tkinter import messagebox
        if not messagebox.askyesno('プロンプトを読み直す', '現在のプロンプトを保存設定の文章に置き換えますか？', parent=self.window):
            return
        try:
            text = self.prompt_provider()
            self.positive.delete('1.0', 'end')
            self.positive.insert('1.0', text)
        except (OSError, ValueError) as exc:
            self.status.set(f'設定を読み込めません：{exc}')

    def start(self):
        if self.running:
            return
        from comfy_client import build_prompt
        try:
            settings = dict(self.defaults)
            settings.update(model=self.fields['model'].get().strip(), seed=int(self.fields['seed'].get()),
                            width=int(self.fields['width'].get()), height=int(self.fields['height'].get()),
                            steps=int(self.fields['steps'].get()), cfg=float(self.fields['cfg'].get()),
                            positive=self.positive.get('1.0', 'end').strip(),
                            negative=self.negative.get('1.0', 'end').strip())
            build_prompt(settings)
            client = ComfyClient(self.fields['url'].get().strip())
        except (ValueError, OSError) as exc:
            self.status.set(f'設定を確認してください：{exc}')
            return
        self.running = True
        self.generate_button.state(['disabled'])
        self.status.set('ComfyUIへ送信しています…')
        def work():
            try:
                path = client.generate(settings, self.cancel,
                                       progress=lambda message: self.messages.put(('progress', message)))
                self.messages.put(('result', path))
            except GenerationCancelled:
                pass
            except Exception as exc:
                self.messages.put(('error', str(exc)))
        threading.Thread(target=work, daemon=True).start()
        self.poll()

    def poll(self):
        self.timer = None
        while not self.messages.empty():
            kind, value = self.messages.get_nowait()
            if kind == 'progress':
                self.status.set(value)
            else:
                self.running = False
                self.generate_button.state(['!disabled'])
                if kind == 'result':
                    self.on_result(value)
                    self.status.set('生成完了。制作画面で確認し、採用する場合は登録してください。')
                else:
                    self.status.set(f'生成できません：{value}')
        if self.running:
            self.timer = self.window.after(100, self.poll)

    def destroyed(self, event):
        if event.widget == self.window:
            self.cancel.set()
            if self.timer is not None:
                self.window.after_cancel(self.timer)
                self.timer = None

    def close(self):
        self.cancel.set()
        self.window.destroy()
