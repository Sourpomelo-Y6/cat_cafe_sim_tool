"""猫と場面を選んで、背景込みPNGを登録する制作画面。"""
from pathlib import Path
from content_images import ImageRegistry, load_display_image

SCENES = (('normal', '通常'), ('play', '遊ぶ'), ('pet', '撫でられる'), ('sleep', '眠る'))


class ContentImageToolWindow:
    def __init__(self, parent, catalog_provider, *, registry=None, cat_id=None, presets=None, references=None, lora_settings=None, text_drafts=None):
        import tkinter as tk
        from tkinter import ttk
        self.window = tk.Toplevel(parent)
        self.window.title('猫の画像制作 — 登録とプレビュー')
        self.catalog_provider = catalog_provider
        self.registry = registry or ImageRegistry()
        from content_texts import TextStore, DEFAULT_DRAFT_PATH
        self.text_drafts = text_drafts or TextStore(DEFAULT_DRAFT_PATH)
        self.published_texts = TextStore(self.registry.directory / 'texts.json')
        from prompt_presets import PromptPresets
        self.presets = presets or PromptPresets()
        from lora_settings import LoraSettings
        self.lora_settings = lora_settings or LoraSettings()
        self.references = references or ImageRegistry(Path(__file__).resolve().parent / 'user_data' / 'reference_images')
        self.reference_image = None
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
        ttk.Button(controls, text='ComfyUIで生成…', command=self.open_generation).pack(side='left', padx=8)
        ttk.Button(frame, text='外見・行動・背景の設定…', command=self.open_prompt_editor).pack(anchor='w', pady=(0, 8))
        ttk.Button(frame, text='デザイン・ポーズLoRAの設定…', command=self.open_lora_editor).pack(anchor='w', pady=(0, 8))
        ttk.Button(frame, text='紹介文・行動描写の編集…', command=self.open_text_editor).pack(anchor='w', pady=(0, 8))
        reference_controls = ttk.Frame(frame)
        reference_controls.pack(fill='x', pady=(0, 8))
        ttk.Button(reference_controls, text='基準画像を選ぶ…', command=self.choose_reference).pack(side='left')
        ttk.Button(reference_controls, text='登録済み通常画像を基準に', command=self.use_normal_reference).pack(side='left', padx=8)
        images = ttk.Frame(frame)
        images.pack(fill='both', expand=True)
        images.columnconfigure(0, weight=1)
        images.columnconfigure(1, weight=1)
        ttk.Label(images, text='基準画像（個体別）').grid(row=0, column=0)
        ttk.Label(images, text='登録画像・生成候補').grid(row=0, column=1)
        self.reference_label = ttk.Label(images, anchor='center')
        self.reference_label.grid(row=1, column=0, sticky='nsew', padx=4)
        self.image_label = ttk.Label(images, anchor='center')
        self.image_label.grid(row=1, column=1, sticky='nsew', padx=4)
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
        image = load_display_image(self.window, path, 420, 360)
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
            self.show_reference(cat_id)
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

    def open_generation(self):
        from comfy_generation_gui import ComfyGenerationWindow
        try:
            target = self.selection()
        except ValueError as exc:
            self.notice.set(f'生成画面を開けません：{exc}')
            return
        existing = getattr(self, 'generation_window', None)
        if existing is not None and existing.window.winfo_exists():
            if self.generation_target == target:
                existing.window.lift()
                return
            if existing.running:
                self.notice.set('生成中です。完了後に別の猫・場面の生成画面を開いてください。')
                return
            existing.close()
        try:
            initial_prompt = self.presets.assemble(*target)
            reference = self.references.image_path(target[0], 'normal')
            if reference is not None and not reference.exists():
                raise ValueError('基準画像が見つかりません。基準画像を選び直してください。')
            def result(path):
                if self.selection() != target:
                    self.notice.set(f'生成中に猫・場面が変更されました。画像は保存済みです：{path}')
                    return
                self.select_file(path)
            self.generation_target = target
            self.generation_window = ComfyGenerationWindow(self.window, result, positive=initial_prompt,
                prompt_provider=lambda: self.presets.assemble(*target),
                label=f'{self.cat.get()} / {self.scene.get()}', reference_path=reference,
                loras=self.lora_settings.active(*target))
        except (ValueError, OSError) as exc:
            self.notice.set(f'生成画面を開けません：{exc}')

    def open_prompt_editor(self):
        from prompt_editor_gui import PromptEditorWindow
        existing = getattr(self, 'prompt_editor', None)
        if existing is not None and existing.window.winfo_exists():
            existing.window.lift()
            return
        try:
            cat_id, scene = self.selection()
            self.prompt_editor = PromptEditorWindow(self.window, self.presets, cat_id, scene,
                                                    f'{self.cat.get()} / {self.scene.get()}')
        except (OSError, ValueError) as exc:
            self.notice.set(f'設定画面を開けません：{exc}')

    def show_reference(self, cat_id):
        self.reference_image = None
        self.reference_label.configure(image='', text='基準画像は未登録')
        path = self.references.image_path(cat_id, 'normal')
        if path is not None:
            self.reference_image = load_display_image(self.window, path, 420, 360)
            self.reference_label.configure(image=self.reference_image, text='')

    def choose_reference(self):
        from tkinter import filedialog
        path = filedialog.askopenfilename(parent=self.window, title='この猫の基準PNGを選ぶ',
                                         filetypes=[('PNG画像', '*.png')])
        if path:
            self.set_reference(path)

    def set_reference(self, path):
        import tkinter as tk
        try:
            cat_id, _ = self.selection()
            # 読み込める画像だけを登録する。
            load_display_image(self.window, path, 420, 360)
            self.references.register(cat_id, 'normal', path)
            self.show_reference(cat_id)
            self.notice.set('基準画像を保存しました。生成画面を閉じて開き直すと反映されます。')
        except (OSError, ValueError, tk.TclError) as exc:
            self.notice.set(f'基準画像を登録できません：{exc}')

    def use_normal_reference(self):
        try:
            cat_id, _ = self.selection()
            path = self.registry.image_path(cat_id, 'normal')
            if path is None:
                raise ValueError('この猫の通常画像を先に登録してください。')
            self.set_reference(path)
        except (OSError, ValueError) as exc:
            self.notice.set(f'基準画像を選べません：{exc}')

    def open_lora_editor(self):
        from lora_editor_gui import LoraEditorWindow
        existing = getattr(self, 'lora_editor', None)
        if existing is not None and existing.window.winfo_exists():
            existing.window.lift()
            return
        try:
            cat_id, scene = self.selection()
            self.lora_editor = LoraEditorWindow(self.window, self.lora_settings, cat_id, scene,
                                                f'{self.cat.get()} / {self.scene.get()}')
        except (OSError, ValueError) as exc:
            self.notice.set(f'LoRA設定を開けません：{exc}')

    def open_text_editor(self):
        from text_editor_gui import TextEditorWindow
        existing = getattr(self, 'text_editor', None)
        if existing is not None and existing.window.winfo_exists():
            existing.window.lift()
            self.notice.set('開いている文章編集画面の猫を確認してください。別の猫は閉じてから開きます。')
            return
        try:
            cat_id, _ = self.selection()
            self.text_editor = TextEditorWindow(self.window, self.text_drafts, self.published_texts,
                                                cat_id, self.cat.get())
        except (OSError, ValueError) as exc:
            self.notice.set(f'文章編集画面を開けません：{exc}')
