"""画像・文章・生成設定を作業別にまとめた猫の素材制作画面。"""
from pathlib import Path
from content_images import ImageRegistry, load_display_image

from interaction_scenes import INTERACTION_SCENES, MODE_ACTIONS, ACTION_LABELS, SPECIAL_ACTIONS
SCENES = (('normal', '通常'), ('play', '遊ぶ'), ('pet', '撫でられる'), ('sleep', '眠る')) + INTERACTION_SCENES


class ContentImageToolWindow:
    def __init__(self, parent, catalog_provider, *, registry=None, cat_id=None, presets=None, references=None, lora_settings=None, text_drafts=None, interaction_drafts=None):
        import tkinter as tk
        from tkinter import ttk
        self.window = tk.Toplevel(parent)
        self.window.title('猫の素材制作')
        self.window.protocol('WM_DELETE_WINDOW', self.close)
        self.base_catalog_provider = catalog_provider
        self.registry = registry or ImageRegistry()
        from interaction_subjects import SubjectRegistry
        self.subject_registry=SubjectRegistry(self.registry.directory/'interaction_subjects.json')
        self.catalog_provider=lambda subjects=self.subject_registry,base=catalog_provider:subjects.image_catalog(base())
        from content_texts import TextStore, DEFAULT_DRAFT_PATH
        self.text_drafts = text_drafts or TextStore(DEFAULT_DRAFT_PATH)
        self.published_texts = TextStore(self.registry.directory / 'texts.json')
        from interaction_texts import InteractionTextStore, DEFAULT_DRAFT_PATH as INTERACTION_DRAFT_PATH
        self.interaction_drafts = interaction_drafts or InteractionTextStore(INTERACTION_DRAFT_PATH)
        self.interaction_drafts.subject_registry=self.subject_registry
        self.interaction_published = InteractionTextStore(self.registry.directory / 'interaction_texts.json', published=True, subject_registry=self.subject_registry)
        from prompt_presets import PromptPresets
        self.presets = presets or PromptPresets()
        from lora_settings import LoraSettings
        self.lora_settings = lora_settings or LoraSettings()
        self.references = references or ImageRegistry(Path(__file__).resolve().parent / 'user_data' / 'reference_images')
        self.reference_image = None
        self.pending = None
        self.image = None
        self.window.geometry('1000x760')
        self.window.minsize(900, 700)
        frame = ttk.Frame(self.window, padding=16)
        frame.pack(fill='both', expand=True)
        ttk.Label(frame, text='猫の素材制作', font=('', 16, 'bold')).pack(anchor='w')
        ttk.Label(frame, text='対象を選ぶ → 画像・文章を制作 → 確認してUnityへ書き出す').pack(anchor='w', pady=(4, 12))
        target = ttk.Frame(frame)
        target.pack(fill='x', pady=(0, 12))
        ttk.Label(target, text='対象の猫').pack(side='left', padx=(0, 12))
        self.cat = tk.StringVar(master=self.window)
        self.scene = tk.StringVar(master=self.window, value='通常')
        self.cat_selector = ttk.Combobox(target, textvariable=self.cat, state='readonly', width=40)
        self.cat_selector.pack(side='left', fill='x', expand=True)
        ttk.Button(target, text='猫一覧を更新', command=self.reload_cats).pack(side='left', padx=(12, 0))
        self.tabs = ttk.Notebook(frame)
        self.tabs.pack(fill='both', expand=True)
        self.pages = {}
        for name in ('対象管理', '画像', '文章', '生成設定'):
            page = ttk.Frame(self.tabs, padding=12)
            self.tabs.add(page, text=name)
            self.pages[name] = page
        subjects_page=self.pages['対象管理']
        ttk.Label(subjects_page,text='猫・人物・お客さんを追加し、生成用の資料とゲーム・画像IDの対応を管理します。',wraplength=780).pack(anchor='w',pady=12)
        ttk.Button(subjects_page,text='猫・人物の登録と資料…',command=self.open_subject_manager).pack(anchor='w')
        ttk.Label(subjects_page,text='登録した対象は文章編集・連続生成で選べます。ゲームIDが空欄の対象は制作専用です。',wraplength=780).pack(anchor='w',pady=12)
        self.tabs.select(self.pages['画像'])
        image_page = self.pages['画像']
        selection = ttk.LabelFrame(image_page, text='1. 場面・行動を選ぶ', padding=10)
        selection.pack(fill='x')
        for col in (0, 1):
            selection.columnconfigure(col, weight=1)
        ttk.Label(selection, text='場面・動作').grid(row=0, column=0, sticky='w')
        ttk.Label(selection, text='行動・強さ').grid(row=0, column=1, sticky='w', padx=(12, 0))
        self.scene_selector = ttk.Combobox(selection, textvariable=self.scene, state='readonly',
                                          values=[row[1] for row in SCENES])
        self.scene_selector.grid(row=1, column=0, sticky='ew')
        self.action = tk.StringVar(master=self.window, value='動作の共通画像')
        self.action_selector = ttk.Combobox(selection, textvariable=self.action, state='readonly',
                                            values=['動作の共通画像'])
        self.action_selector.grid(row=1, column=1, sticky='ew', padx=(12, 0))
        self.action_selector.bind('<<ComboboxSelected>>', lambda event: self.refresh())
        self.cat_selector.bind('<<ComboboxSelected>>', lambda event: self.refresh())
        self.scene_selector.bind('<<ComboboxSelected>>', lambda event: self.refresh())
        controls = ttk.LabelFrame(image_page, text='2. 画像を用意する → 確認して登録', padding=10)
        controls.pack(fill='x', pady=10)
        ttk.Button(controls, text='画像を選ぶ…', command=self.choose).pack(side='left')
        ttk.Button(controls, text='ComfyUIで生成…', command=self.open_generation).pack(side='left', padx=8)
        self.save_button = ttk.Button(controls, text='この画像を登録', command=self.save)
        self.save_button.pack(side='right')
        images = ttk.Frame(image_page)
        images.pack(fill='both', expand=True)
        images.columnconfigure(0, weight=1, uniform='preview')
        images.columnconfigure(1, weight=1, uniform='preview')
        images.rowconfigure(1, weight=1)
        ttk.Label(images, text='基準画像（この猫の外見）').grid(row=0, column=0, sticky='w')
        ttk.Label(images, text='登録画像・未登録の候補').grid(row=0, column=1, sticky='w', padx=(12, 0))
        self.reference_label = ttk.Label(images, anchor='center')
        self.reference_label.grid(row=1, column=0, sticky='nsew', padx=(0, 6))
        self.image_label = ttk.Label(images, anchor='center')
        self.image_label.grid(row=1, column=1, sticky='nsew', padx=(6, 0))
        reference_controls = ttk.Frame(image_page)
        reference_controls.pack(fill='x', pady=(8, 0))
        ttk.Button(reference_controls, text='基準画像を選ぶ…', command=self.choose_reference).pack(side='left')
        ttk.Button(reference_controls, text='登録済み通常画像を基準に', command=self.use_normal_reference).pack(side='left', padx=8)
        text_page = self.pages['文章']
        ttk.Label(text_page, text='作りたい文章を選んでください。', font=('', 12, 'bold')).pack(anchor='w', pady=(0, 12))
        for title, description, label, command in (
            ('猫の紹介・場面の描写', '選択した猫の紹介文と、通常・遊ぶ・撫でられる・眠る場面の文章を制作します。',
             '紹介文・行動描写の編集…', self.open_text_editor),
            ('交流の文章（通常・特別場面）', '選択した猫の反応から編集を開始します。人物のセリフへの切り替え、資料・採用文の比較、画像付き確認に対応しています。',
             '日常交流の文章を編集…', self.open_interaction_text_editor)):
            group = ttk.LabelFrame(text_page, text=title, padding=14)
            group.pack(fill='x', pady=(0, 16))
            ttk.Label(group, text=description, wraplength=780).pack(anchor='w', pady=(0, 10))
            ttk.Button(group, text=label, command=command).pack(anchor='w')
        ttk.Label(text_page, text='下書き保存 → 内容を確認 → 確認した文章を登録 → Unity向けに書き出す', wraplength=780).pack(anchor='w')
        settings = self.pages['生成設定']
        ttk.Label(settings, text='画像生成の準備', font=('', 12, 'bold')).pack(anchor='w', pady=(0, 12))
        self.settings_target = tk.StringVar(master=self.window)
        ttk.Label(settings, textvariable=self.settings_target, wraplength=780).pack(anchor='w', pady=(0, 16))
        for title, description, label, command in (
            ('生成する内容', '猫の外見、動作・行動、背景を設定します。場面・行動は「画像」タブで選択します。',
             '外見・行動・背景の設定…', self.open_prompt_editor),
            ('モデルへの追加設定', 'デザイン・ポーズ用LoRAと適用する強さを設定します。',
             'デザイン・ポーズLoRAの設定…', self.open_lora_editor)):
            group = ttk.LabelFrame(settings, text=title, padding=14)
            group.pack(fill='x', pady=(0, 16))
            ttk.Label(group, text=description, wraplength=780).pack(anchor='w', pady=(0, 10))
            ttk.Button(group, text=label, command=command).pack(anchor='w')
        self.notice = tk.StringVar(master=self.window)
        ttk.Separator(frame).pack(fill='x', pady=(12, 8))
        ttk.Label(frame, textvariable=self.notice, wraplength=840).pack(anchor='w')
        footer = ttk.Frame(frame)
        footer.pack(fill='x', pady=(8, 0))
        ttk.Label(footer, text='書き出し対象：登録済みの全猫・画像・文章').pack(side='left')
        ttk.Button(footer, text='閉じる', command=self.close).pack(side='right')
        ttk.Button(footer, text='Unity向けに書き出す…', command=self.export_unity).pack(side='right', padx=12)
        self.reload_cats()
        if cat_id is not None:
            self.cat.set(next((label for label, key in self.cats.items() if key == cat_id), self.cat.get()))
            self.refresh()

    def close(self):
        editor = getattr(self, 'interaction_text_editor', None)
        if editor is not None and editor.window.winfo_exists() and not editor.confirm_leave():
            return
        manager=getattr(self,'subject_manager',None)
        if manager is not None and manager.window.winfo_exists() and not manager.confirm_leave():return
        self.window.destroy()

    def open_subject_manager(self):
        from interaction_subject_gui import SubjectManagerWindow
        existing=getattr(self,'subject_manager',None)
        if existing is not None and existing.window.winfo_exists():existing.window.lift();return
        def saved():
            self.cats={f'{name}（{id}）':id for id,name in self.catalog_provider().items()}
            self.cat_selector.configure(values=list(self.cats))
            editor=getattr(self,'interaction_text_editor',None)
            if editor is not None and editor.window.winfo_exists():editor.refresh_subjects()
        try:
            self.subject_manager=SubjectManagerWindow(self.window,self.subject_registry,catalog=self.catalog_provider(),drafts=self.interaction_drafts,published=self.interaction_published,on_saved=saved)
        except (OSError,ValueError) as exc:self.notice.set(f'対象台帳を開けません：{exc}')

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
        allowed = MODE_ACTIONS.get(key, ())
        choices = [(action, ACTION_LABELS[action]) for action in allowed]
        if allowed:
            choices += list(SPECIAL_ACTIONS)
        labels = ['動作の共通画像'] + [label for _, label in choices]
        self.action_selector.configure(values=labels, state='readonly' if allowed else 'disabled')
        if self.action.get() not in labels:
            self.action.set(labels[0])
        if self.action.get() != labels[0]:
            key += '__' + next(action for action, label in choices if label == self.action.get())
        return cat_id, key

    def display(self, path):
        image = load_display_image(self.window, path, 380, 260)
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
            self.settings_target.set(f'設定対象：{self.cat.get()} / {self.scene.get()} / {self.action.get()}')
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
                label=f'{self.cat.get()} / {self.scene.get()} / {self.action.get()}', reference_path=reference,
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
                                                    f'{self.cat.get()} / {self.scene.get()} / {self.action.get()}')
        except (OSError, ValueError) as exc:
            self.notice.set(f'設定画面を開けません：{exc}')

    def show_reference(self, cat_id):
        self.reference_image = None
        self.reference_label.configure(image='', text='基準画像は未登録')
        path = self.references.image_path(cat_id, 'normal')
        if path is not None:
            self.reference_image = load_display_image(self.window, path, 380, 260)
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
            load_display_image(self.window, path, 380, 260)
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
                                                f'{self.cat.get()} / {self.scene.get()} / {self.action.get()}')
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

    def open_interaction_text_editor(self):
        from interaction_text_editor_gui import InteractionTextEditorWindow
        existing = getattr(self, 'interaction_text_editor', None)
        if existing is not None and existing.window.winfo_exists():
            existing.window.lift()
            return
        try:
            from interaction_texts import condition
            content_id, _ = self.selection()
            subject = next((r for r in self.subject_registry.read()['subjects'] if r['kind']=='cat_response' and r['content_id']==content_id), None)
            initial = condition('cat_response', subject['id'], reaction='neutral', subjects=self.subject_registry.labels(include_disabled=True)) if subject else None
            self.interaction_text_editor = InteractionTextEditorWindow(
                self.window, self.interaction_drafts, self.interaction_published,
                image_registry=self.registry, catalog_provider=self.catalog_provider, subject_registry=self.subject_registry, initial_condition=initial)
        except (OSError, ValueError) as exc:
            self.notice.set(f'日常交流の文章を開けません：{exc}')

    def export_unity(self):
        from tkinter import filedialog
        from unity_export import export_bundle
        parent = filedialog.askdirectory(parent=self.window, title='新しい素材パックを作る保存先を選ぶ')
        if not parent:
            return
        try:
            from cat_appearance import catalog_profiles
            defaults = catalog_profiles()
            profiles = {key: dict(defaults.get(key, {}), appearance=self.presets.appearance_profile(key))
                        for key in self.catalog_provider()}
            folder, report = export_bundle(self.catalog_provider(), self.registry, self.published_texts, parent, profiles=profiles,
                                           interaction_texts=self.interaction_published)
            self.notice.set(f"書き出しました：{folder}\n猫{report['cat_count']}匹・画像{report['image_count']}枚・交流文{report['interactions']['candidate_count']}案。")
            self.show_export_report(folder, report)
        except (OSError, ValueError) as exc:
            self.notice.set(f'書き出せません：{exc}')

    def show_export_report(self, folder, report):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        window = tk.Toplevel(self.window)
        window.title('Unity向け素材の書き出し結果')
        frame = ttk.Frame(window, padding=12)
        frame.pack(fill='both', expand=True)
        ttk.Label(frame, text=f'保存先：{folder}\nUnityの Cat Cafe → 素材 → 素材パックを取り込む… から、このフォルダを選んでください。',
                  wraplength=650).pack(anchor='w')
        body = ScrolledText(frame, width=85, height=18, wrap='word')
        body.pack(fill='both', expand=True, pady=8)
        from interaction_presentation import condition_label
        interaction = report.get('interactions', {})
        details = '\n'.join(report['warnings']) or '登録素材に欠落はありません。'
        if interaction:
            details += (f"\n\n交流文：{interaction['entry_count']}条件・{interaction['candidate_count']}案"
                        + f"\n対応：{interaction['covered_count']} / {interaction['expected_count']}条件"
                        + '\n交流文カタログも同梱しました。Unityで取り込むと、以後の交流に登録済みの文章が使われます。'
                        + '\n\n未制作の条件：\n'
                        + '\n'.join(condition_label(v) for v in interaction['missing_conditions']))
        body.insert('1.0', details)
        body.configure(state='disabled')
        ttk.Button(frame, text='閉じる', command=window.destroy).pack(anchor='e')
