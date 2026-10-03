"""猫1匹の紹介文と4場面の描写を確認して登録する。"""
from content_texts import validate_record

SCENES = (('normal', '通常'), ('play', '遊ぶ'), ('pet', '撫でられる'), ('sleep', '眠る'))


class TextEditorWindow:
    def __init__(self, parent, drafts, published, cat_id, label):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        record = drafts.get(cat_id)
        if record is None:
            record = published.get(cat_id)
        record = record or {'introduction': '', 'scenes': {}}
        self.drafts, self.published, self.cat_id = drafts, published, cat_id
        self.reviewed = None
        self.window = tk.Toplevel(parent)
        self.window.title(f'紹介文・行動描写 — {label}')
        frame = ttk.Frame(self.window, padding=12)
        frame.pack(fill='both', expand=True)
        ttk.Label(frame, text=f'編集対象：{label}', wraplength=600).pack(anchor='w')
        ttk.Label(frame, text='下書きはゲームに反映されません。確認して登録すると、この猫の全4場面へ反映します。\n空欄はゲームの定型文を使います。紹介文2000文字、各描写500文字まで。',
                  wraplength=600).pack(anchor='w', pady=6)
        self.fields = {}
        for key, title in (('introduction', '紹介文'),) + SCENES:
            ttk.Label(frame, text=title).pack(anchor='w')
            widget = tk.Text(frame, width=64, height=3 if key == 'introduction' else 2, wrap='word', undo=True)
            widget.insert('1.0', record['introduction'] if key == 'introduction' else record['scenes'].get(key, ''))
            widget.pack(fill='x', pady=(0, 5))
            self.fields[key] = widget
        controls = ttk.Frame(frame)
        controls.pack(fill='x', pady=6)
        ttk.Button(controls, text='下書きを保存', command=self.save_draft).pack(side='left')
        ttk.Button(controls, text='登録内容を確認', command=self.preview).pack(side='left', padx=8)
        ttk.Button(controls, text='確認した文章を登録', command=self.register).pack(side='left')
        self.preview_text = ScrolledText(frame, width=64, height=6, wrap='word', state='disabled')
        self.preview_text.pack(fill='x')
        self.notice = tk.StringVar(master=self.window, value='下書きがあれば下書き、なければ登録済み文章を読み込みます。')
        ttk.Label(frame, textvariable=self.notice, wraplength=600).pack(anchor='w', pady=6)
        ttk.Button(frame, text='閉じる', command=self.window.destroy).pack(anchor='e')

    def values(self):
        values = {key: widget.get('1.0', 'end-1c').strip() for key, widget in self.fields.items()}
        record = {'introduction': values.pop('introduction'), 'scenes': values}
        validate_record(record)
        return record

    def save_draft(self):
        try:
            self.drafts.save(self.cat_id, self.values())
            self.notice.set('下書きを保存しました。ゲームへの反映は登録後です。')
        except (OSError, ValueError) as exc:
            self.notice.set(f'下書きを保存できません：{exc}')

    def preview(self):
        self.reviewed = None
        try:
            record = self.values()
            text = ['紹介文：' + (record['introduction'] or '（定型文）')]
            text.extend(label + '：' + (record['scenes'][key] or '（定型文）') for key, label in SCENES)
            self.set_preview('\n'.join(text))
            self.reviewed = record
            self.notice.set('内容を確認し、よければ「確認した文章を登録」を押してください。')
        except ValueError as exc:
            self.set_preview('')
            self.notice.set(f'確認できません：{exc}')

    def set_preview(self, text):
        self.preview_text.configure(state='normal')
        self.preview_text.delete('1.0', 'end')
        self.preview_text.insert('1.0', text)
        self.preview_text.configure(state='disabled')

    def register(self):
        try:
            record = self.values()
            if self.reviewed is None or record != self.reviewed:
                raise ValueError('「登録内容を確認」を押して、現在の文章を確認してください。')
            self.published.save(self.cat_id, record)
            self.notice.set('文章を登録しました。ゲームの「猫の画像…」へ反映されます。下書きは別途保存できます。')
        except (OSError, ValueError) as exc:
            self.notice.set(f'登録できません：{exc}')
