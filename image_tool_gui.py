"""猫と場面を選んで、背景込みPNGを登録する制作画面。"""
from pathlib import Path
from content_images import ImageRegistry, load_display_image

SCENES = (('normal', '通常'), ('play', '遊ぶ'), ('pet', '撫でられる'), ('sleep', '眠る'))


class ContentImageToolWindow:
    def __init__(self, parent, catalog_provider, *, registry=None, cat_id=None):
        import tkinter as tk
        from tkinter import ttk
        self.window = tk.Toplevel(parent)
        self.window.title('猫の画像制作 — 登録とプレビュー')
        self.catalog_provider = catalog_provider
        self.registry = registry or ImageRegistry()
        self.pending = None
        self.image = None
        frame = ttk.Frame(self.window, padding=12)
        frame.pack(fill='both', expand=True)
        ttk.Label(frame, text='猫と場面を選び、背景込みのPNGを登録します。', wraplength=520).pack(anchor='w')
        self.cat = tk.StringVar(master=self.window)
        self.scene = tk.StringVar(master=self.window, value='通常')
        self.cat_selector = ttk.Combobox(frame, textvariable=self.cat, state='readonly', width=40)
        self.cat_selector.pack(fill='x', pady=6)
        self.scene_selector = ttk.Combobox(frame, textvariable=self.scene, state='readonly',
                                          values=[row[1] for row in SCENES])
        self.scene_selector.pack(fill='x')
        self.cat_selector.bind('<<ComboboxSelected>>', lambda event: self.refresh())
        self.scene_selector.bind('<<ComboboxSelected>>', lambda event: self.refresh())
        controls = ttk.Frame(frame)
        controls.pack(fill='x', pady=8)
        ttk.Button(controls, text='画像を選ぶ…', command=self.choose).pack(side='left')
        self.save_button = ttk.Button(controls, text='この画像を登録', command=self.save)
        self.save_button.pack(side='left', padx=8)
        ttk.Button(controls, text='猫一覧を更新', command=self.reload_cats).pack(side='left')
        self.image_label = ttk.Label(frame, anchor='center')
        self.image_label.pack(fill='both', expand=True)
        self.notice = tk.StringVar(master=self.window)
        ttk.Label(frame, textvariable=self.notice, wraplength=540).pack(anchor='w', pady=8)
        ttk.Button(frame, text='閉じる', command=self.window.destroy).pack(anchor='e')
        self.reload_cats()
        if cat_id is not None:
            self.cat.set(next((label for label, key in self.cats.items() if key == cat_id), self.cat.get()))
            self.refresh()

    def reload_cats(self):
        catalog = self.catalog_provider()
        self.cats = {f"{name}（{key}）": key for key, name in catalog.items()}
        self.cat_selector.configure(values=list(self.cats))
        if self.cat.get() not in self.cats:
            self.cat.set(next(iter(self.cats), ''))
        self.refresh()

    def selection(self):
        cat_id = self.cats.get(self.cat.get())
        if cat_id not in self.catalog_provider():
            raise ValueError('猫一覧を更新して、登録する猫を選んでください。')
        key = next(row[0] for row in SCENES if row[1] == self.scene.get())
        return cat_id, key

    def display(self, path):
        image = load_display_image(self.window, path)
        self.image = image
        self.image_label.configure(image=image, text='')

    def refresh(self):
        import tkinter as tk
        self.pending = None
        self.save_button.state(['disabled'])
        self.image = None
        self.image_label.configure(image='', text='未登録：ゲームでは仮画像を使います。')
        try:
            cat_id, key = self.selection()
            path = self.registry.image_path(cat_id, key)
            if path:
                self.display(path)
            self.notice.set('登録済み画像' if path else '画像を選んでプレビューしてください。')
        except (OSError, ValueError, tk.TclError) as exc:
            self.notice.set(f'登録画像を表示できません：{exc}')

    def choose(self):
        from tkinter import filedialog
        path = filedialog.askopenfilename(parent=self.window, title='背景込みのPNG画像を選ぶ',
                                         filetypes=[('PNG画像', '*.png')])
        if path:
            self.select_file(path)

    def select_file(self, path):
        import tkinter as tk
        self.pending = None
        self.save_button.state(['disabled'])
        try:
            self.selection()
            if Path(path).suffix.lower() != '.png':
                raise ValueError('PNG画像を選んでください。')
            self.display(path)
            self.pending = Path(path)
            self.notice.set('未保存：この画像を登録するには「この画像を登録」を押してください。')
            self.save_button.state(['!disabled'])
        except (OSError, ValueError, tk.TclError) as exc:
            self.notice.set(f'画像を選べません：{exc}')

    def save(self):
        if self.pending is None:
            return
        try:
            cat_id, key = self.selection()
            self.registry.register(cat_id, key, self.pending)
            self.refresh()
            self.notice.set('登録しました。「猫の画像」の自動表示へ反映されます。')
        except (OSError, ValueError) as exc:
            self.notice.set(f'登録できません：{exc}')
