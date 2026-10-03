"""ComfyUIのLoRA一覧と、猫・場面の設定編集。"""
import queue
import threading
from comfy_client import ComfyClient


class LoraEditorWindow:
    def __init__(self, parent, settings, cat_id, scene, label):
        import tkinter as tk
        from tkinter import ttk
        saved = settings.fields(cat_id, scene)
        self.settings, self.cat_id, self.scene = settings, cat_id, scene
        self.window = tk.Toplevel(parent)
        self.window.title('LoRA設定 — ' + label)
        self.timer = None
        self.messages = queue.Queue()
        self.window.bind('<Destroy>', self.destroyed)
        frame = ttk.Frame(self.window, padding=12)
        frame.pack(fill='both', expand=True)
        ttk.Label(frame, text=label, font=('', 12, 'bold')).pack(anchor='w')
        ttk.Label(frame, text='デザイン用はこの猫、ポーズ用はこの場面の全猫で共有します。\n使用するチェックポイントに対応したLoRAを選んでください。', wraplength=580).pack(anchor='w', pady=6)
        connection = ttk.Frame(frame)
        connection.pack(fill='x', pady=6)
        self.url = tk.StringVar(master=self.window, value='http://127.0.0.1:8188')
        ttk.Entry(connection, textvariable=self.url, width=40).pack(side='left')
        self.fetch_button = ttk.Button(connection, text='LoRA一覧を取得', command=self.fetch)
        self.fetch_button.pack(side='left', padx=8)
        self.rows = []
        self.selectors = []
        for title, values in zip(('デザイン用（猫ごと）', 'ポーズ用（場面ごと）'), saved):
            panel = ttk.LabelFrame(frame, text=title, padding=8)
            panel.pack(fill='x', pady=6)
            row = {}
            row['enabled'] = tk.BooleanVar(master=self.window, value=values['enabled'])
            ttk.Checkbutton(panel, text='有効', variable=row['enabled']).grid(row=0, column=0, sticky='w')
            row['name'] = tk.StringVar(master=self.window, value=values['name'])
            selector = ttk.Combobox(panel, textvariable=row['name'], width=60)
            selector.grid(row=0, column=1, sticky='ew')
            self.selectors.append(selector)
            for line, (key, name) in enumerate((('strength_model', 'モデル強度'), ('strength_clip', 'CLIP強度'), ('trigger', 'トリガーワード')), 1):
                ttk.Label(panel, text=name).grid(row=line, column=0, sticky='w', padx=(0, 8))
                row[key] = tk.StringVar(master=self.window, value=str(values[key]))
                ttk.Entry(panel, textvariable=row[key], width=60).grid(row=line, column=1, sticky='ew', pady=2)
            panel.columnconfigure(1, weight=1)
            self.rows.append(row)
        self.status = tk.StringVar(master=self.window, value='一覧を取得して選択してください。強度は-2～2、初期値0.7です。')
        ttk.Label(frame, textvariable=self.status, wraplength=600).pack(anchor='w', pady=8)
        ttk.Button(frame, text='設定を保存', command=self.save).pack(side='left')
        ttk.Button(frame, text='閉じる', command=self.window.destroy).pack(side='right')

    def fetch(self):
        if self.timer is not None:
            return
        try:
            client = ComfyClient(self.url.get().strip())
        except ValueError as exc:
            self.status.set(str(exc))
            return
        self.fetch_button.state(['disabled'])
        self.status.set('LoRA一覧を取得しています…')
        def work():
            try:
                self.messages.put(('result', client.list_loras()))
            except Exception as exc:
                self.messages.put(('error', str(exc)))
        threading.Thread(target=work, daemon=True).start()
        self.timer = self.window.after(100, self.poll)

    def poll(self):
        self.timer = None
        try:
            kind, value = self.messages.get_nowait()
        except queue.Empty:
            self.timer = self.window.after(100, self.poll)
            return
        self.fetch_button.state(['!disabled'])
        if kind == 'result':
            for selector in self.selectors:
                selector.configure(values=value)
            self.status.set(f'{len(value)}件のLoRAを取得しました。モデルとの互換性は自動判定しません。')
        else:
            self.status.set(f'取得できません：{value}')

    def save(self):
        try:
            rows = [dict(enabled=row['enabled'].get(), name=row['name'].get().strip(),
                         strength_model=float(row['strength_model'].get()), strength_clip=float(row['strength_clip'].get()),
                         trigger=row['trigger'].get().strip()) for row in self.rows]
            self.settings.save(self.cat_id, self.scene, *rows)
            self.status.set('保存しました。生成画面を開き直すと反映されます。')
        except (ValueError, OSError) as exc:
            self.status.set(f'保存できません：{exc}')

    def destroyed(self, event):
        if event.widget == self.window and self.timer is not None:
            self.window.after_cancel(self.timer)
            self.timer = None
