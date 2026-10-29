"""Edit aliases without changing IDs, conditions or existing prose."""
from display_labels import DEFAULTS, DisplayLabelStore

class DisplayLabelsWindow:
    def __init__(self, parent, store=None):
        import tkinter as tk
        from tkinter import ttk, messagebox
        self.store = store or DisplayLabelStore()
        try:
            data = self.store.read()
        except (ValueError, OSError) as exc:
            messagebox.showerror('表示名を開けません', str(exc), parent=parent)
            return
        self.window = tk.Toplevel(parent)
        self.window.title('行動・反応の表示名')
        self.window.geometry('760x650')
        self.window.minsize(650, 580)
        body = ttk.Frame(self.window, padding=12)
        body.pack(fill='both', expand=True)
        ttk.Label(body, text='名前だけを変更します。行動の追加・効果・内部ID・既存本文は変更しません。\n空欄は標準に戻します。Toolの画面表示は再起動後に反映されます。', wraplength=700).pack(anchor='w', pady=8)
        tabs = ttk.Notebook(body)
        tabs.pack(fill='both', expand=True)
        self.fields = {}
        for group, title in [('modes', '行動の種類'), ('actions', '操作・強さ'), ('reactions', '反応・特別場面')]:
            page = ttk.Frame(tabs, padding=12)
            tabs.add(page, text=title)
            for col, caption in enumerate(('内部ID（固定）', '標準の表示名', '変更後の表示名')):
                ttk.Label(page, text=caption).grid(row=0, column=col, sticky='w', padx=6, pady=8)
            page.columnconfigure(2, weight=1)
            for row, (key, default) in enumerate(DEFAULTS[group].items(), 1):
                ttk.Label(page, text=key).grid(row=row, column=0, sticky='w', padx=6, pady=7)
                ttk.Label(page, text=default).grid(row=row, column=1, sticky='w', padx=6)
                value = tk.StringVar(master=self.window, value=data['overrides'].get(group, {}).get(key, ''))
                ttk.Entry(page, textvariable=value).grid(row=row, column=2, sticky='ew', padx=6)
                self.fields[group, key] = value
        self.notice = tk.StringVar(master=self.window, value='Unityには次回の「全素材書き出し」→取り込み→再ビルドで反映します。')
        ttk.Label(body, textvariable=self.notice, wraplength=700).pack(anchor='w', pady=10)
        buttons = ttk.Frame(body)
        buttons.pack(fill='x')
        ttk.Button(buttons, text='全欄を標準に戻す（保存前）', command=self.reset).pack(side='left')
        ttk.Button(buttons, text='保存', command=self.save).pack(side='right')
        ttk.Button(buttons, text='閉じる', command=self.window.destroy).pack(side='right', padx=8)
    def reset(self):
        for value in self.fields.values():
            value.set('')
    def save(self):
        from tkinter import messagebox
        values = {group: {} for group in DEFAULTS}
        for (group, key), value in self.fields.items():
            if value.get().strip():
                values[group][key] = value.get()
        try:
            self.store.save(dict(version=1, overrides=values))
        except (ValueError, OSError) as exc:
            messagebox.showerror('保存できません', str(exc), parent=self.window)
            return False
        self.notice.set('保存しました。Toolを再起動すると画面へ反映します。Unityは全素材の再書き出し・取り込み・再ビルド後に反映します。')
        return True
