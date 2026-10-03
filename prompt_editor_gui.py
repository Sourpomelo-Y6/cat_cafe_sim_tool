"""選択した猫・場面のプロンプト部品を編集する画面。"""


class PromptEditorWindow:
    def __init__(self, parent, presets, cat_id, scene, label):
        import tkinter as tk
        from tkinter import ttk
        fields = presets.fields(cat_id, scene)
        self.presets, self.cat_id, self.scene = presets, cat_id, scene
        self.window = tk.Toplevel(parent)
        self.window.title(f'プロンプト設定 — {label}')
        frame = ttk.Frame(self.window, padding=12)
        frame.pack(fill='both', expand=True)
        ttk.Label(frame, text=label, font=('', 12, 'bold')).pack(anchor='w')
        ttk.Label(frame, text='外見はこの猫、行動は同じ場面の全猫、背景・画風は全場面で共有します。\n生成モデルへ渡す文章を入力してください（英語を推奨）。',
                  wraplength=580).pack(anchor='w', pady=6)
        self.fields = {}
        for key, title in [('appearance', '猫の外見（個体別）'), ('action', '行動（場面共通）'),
                           ('background', '固定背景（全猫・全場面共通）'), ('style', '画風・視点・照明（共通）')]:
            ttk.Label(frame, text=title).pack(anchor='w')
            entry = tk.Text(frame, height=3, width=70, wrap='word')
            entry.insert('1.0', fields[key])
            entry.pack(fill='x', pady=(0, 6))
            self.fields[key] = entry
        self.preview = tk.Text(frame, height=4, width=70, wrap='word', state='disabled')
        from cat_appearance import FIELDS
        self.appearance_fields = {}
        profile = presets.appearance_profile(cat_id)
        for key, title in FIELDS.items():
            row = ttk.Frame(frame)
            row.pack(fill='x', pady=2)
            ttk.Label(row, text=title, width=14).pack(side='left')
            entry = ttk.Entry(row, width=65)
            entry.insert(0, profile[key])
            entry.pack(side='left', fill='x', expand=True)
            self.appearance_fields[key] = entry
        self.preview.pack(fill='x', pady=4)
        controls = ttk.Frame(frame)
        controls.pack(fill='x')
        ttk.Button(controls, text='組み立てを確認', command=self.show_preview).pack(side='left')
        ttk.Button(controls, text='設定を保存', command=self.save).pack(side='left', padx=8)
        ttk.Button(controls, text='閉じる', command=self.window.destroy).pack(side='right')
        self.status = tk.StringVar(master=self.window, value='保存後、生成画面で「保存設定を読み直す」か、生成画面を開き直してください。')
        ttk.Label(frame, textvariable=self.status, wraplength=580).pack(anchor='w', pady=8)
        self.show_preview()

    def values(self):
        values = {key: widget.get('1.0', 'end').strip() for key, widget in self.fields.items()}
        values['appearance_profile'] = {key: widget.get().strip() for key, widget in self.appearance_fields.items()}
        return values

    def show_preview(self):
        fields = self.values()
        self.preview.configure(state='normal')
        self.preview.delete('1.0', 'end')
        self.preview.insert('1.0', ', '.join(fields[key].rstrip(',') for key in ('style', 'appearance', 'action', 'background')))
        details = ', '.join(f'{key}: {value}' for key, value in fields['appearance_profile'].items() if value)
        if details:
            self.preview.insert('end', ', ' + details)
        self.preview.configure(state='disabled')

    def save(self):
        try:
            self.presets.save(self.cat_id, self.scene, self.values())
            self.show_preview()
            self.status.set('設定を保存しました。生成画面を開くと組み立てた文章が呼び出されます。')
        except (OSError, ValueError) as exc:
            self.status.set(f'保存できません：{exc}')
