"""画像・文章・生成設定を作業別にまとめた猫の素材制作画面。"""
from pathlib import Path
from content_images import ImageRegistry, load_display_image

from interaction_scenes import INTERACTION_SCENES, MODE_ACTIONS, ACTION_LABELS, SPECIAL_ACTIONS
from display_labels import label as display_label
SCENES = (('normal', '通常'), ('play', '遊ぶ'), ('pet', '撫でられる'), ('sleep', '眠る')) + INTERACTION_SCENES
SCENES = tuple((key, display_label('modes', key, value)) for key, value in SCENES)


class ContentImageToolWindow:
    def open_cat_outfits(self):
        from cat_outfit_editor_gui import CatOutfitEditorWindow
        from cat_outfits import CatOutfitStore
        try:
            current = getattr(self, 'cat_outfit_editor', None)
            if current and current.window.winfo_exists():
                current.window.lift(); return
            self.cat_outfit_editor = CatOutfitEditorWindow(self.window, CatOutfitStore(self.registry.directory), self.catalog_provider)
        except (OSError, ValueError) as exc:
            self.notice.set('見た目を開けません：' + str(exc))

    def open_state_images(self):
        from state_image_editor_gui import StateImageEditorWindow
        from state_images import StateImageStore
        try:
            current = getattr(self, 'state_image_editor', None)
            if current and current.window.winfo_exists():
                current.window.lift(); return
            self.state_image_editor = StateImageEditorWindow(self.window, StateImageStore(self.registry.directory), self.catalog_provider)
        except (OSError, ValueError) as exc:
            self.notice.set('状態画像を開けません：' + str(exc))

    def open_optional_event_editor(self):
        from optional_event_editor_gui import OptionalEventEditorWindow
        from optional_events import OptionalEventStore, DEFAULT_DRAFT
        try:
            current = getattr(self, "optional_event_editor", None)
            if current and current.window.winfo_exists():
                current.window.lift(); return
            self.optional_event_editor = OptionalEventEditorWindow(self.window, OptionalEventStore(DEFAULT_DRAFT), OptionalEventStore(self.registry.directory / "optional_events.json"), self.catalog_provider)
        except (OSError, ValueError) as exc:
            self.notice.set("イベント編集を開けません：" + str(exc))

    def open_display_labels(self):
        from display_labels_gui import DisplayLabelsWindow
        DisplayLabelsWindow(self.window)

    def __init__(self, parent, catalog_provider, *, registry=None, cat_id=None, presets=None, references=None, lora_settings=None, text_drafts=None, interaction_drafts=None):
        import tkinter as tk
        from tkinter import ttk
        self.window = tk.Toplevel(parent)
        self.window.title('CatCafeSim 素材制作')
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
        ttk.Label(frame, text='CatCafeSim 素材制作', font=('', 16, 'bold')).pack(anchor='w')
        ttk.Label(frame, text='対象を選ぶ → 画像・文章を制作 → 確認してUnityへ書き出す').pack(anchor='w', pady=(4, 12))
        target = ttk.Frame(frame)
        target.pack(fill='x', pady=(0, 12))
        ttk.Label(target, text='猫画像の対象').pack(side='left', padx=(0, 12))
        self.cat = tk.StringVar(master=self.window)
        self.scene = tk.StringVar(master=self.window, value='通常')
        self.cat_selector = ttk.Combobox(target, textvariable=self.cat, state='readonly', width=40)
        self.cat_selector.pack(side='left', fill='x', expand=True)
        ttk.Button(target, text='猫一覧を更新', command=self.reload_cats).pack(side='left', padx=(12, 0))
        subjects = ttk.Frame(frame)
        subjects.pack(fill='x', pady=(0, 8))
        ttk.Label(subjects, text='制作対象を検索').pack(side='left')
        self.subject_search = tk.StringVar(master=self.window)
        ttk.Entry(subjects, textvariable=self.subject_search, width=16).pack(side='left', padx=8)
        self.subject_choice = tk.StringVar(master=self.window)
        self.subject_selector = ttk.Combobox(subjects, textvariable=self.subject_choice, state='readonly')
        self.subject_selector.pack(side='left', fill='x', expand=True)
        self.subject_selector.bind('<<ComboboxSelected>>', self.select_subject)
        self.subject_search.trace_add('write', lambda *args: self.filter_subjects())
        self.subject_rows = {}
        self.active_subject = None
        self.tabs = ttk.Notebook(frame)
        self.tabs.pack(fill='both', expand=True)
        self.pages = {}
        for name in ('対象管理', '文章', '画像', '確認・書き出し'):
            page = ttk.Frame(self.tabs, padding=12)
            self.tabs.add(page, text=name)
            self.pages[name] = page
        subjects_page=self.pages['対象管理']
        ttk.Label(subjects_page,text='猫・人物・お客さんを追加し、生成用の資料とゲーム・画像IDの対応を管理します。',wraplength=780).pack(anchor='w',pady=12)
        ttk.Button(subjects_page,text='猫・人物の登録と資料…',command=self.open_subject_manager).pack(anchor='w')
        ttk.Label(subjects_page,text='登録した対象は文章編集・連続生成で選べます。ゲームIDが空欄の対象は制作専用です。',wraplength=780).pack(anchor='w',pady=12)
        ttk.Button(subjects_page, text='行動・反応の表示名…', command=self.open_display_labels).pack(anchor='w', pady=8)
        self.tabs.select(self.pages['画像'])
        image_page = ttk.Frame(self.pages['画像'])
        image_page.pack(fill='both', expand=True)
        self.cat_image_panel = image_page
        ttk.Button(image_page, text='猫の状態別画像…', command=self.open_state_images).pack(anchor='w', pady=4)
        ttk.Button(image_page, text='ミケの見た目…', command=self.open_cat_outfits).pack(anchor='w', pady=4)
        from portrait_panel import PortraitPanel
        self.portrait_panel = PortraitPanel(self.pages['画像'], self.registry.directory)
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
        self.cat_selector.bind('<<ComboboxSelected>>', self.select_image_cat)
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
        ttk.Button(text_page, text="ミケの個別イベントを編集…", command=self.open_optional_event_editor).pack(anchor="w", pady=6)
        ttk.Label(text_page, text='作りたい文章を選んでください。', font=('', 12, 'bold')).pack(anchor='w', pady=(0, 12))
        for title, description, label, command in (
            ('猫の紹介・場面の描写', '選択した猫の紹介文と、通常・遊ぶ・撫でられる・眠る場面の文章を制作します。',
             '紹介文・行動描写の編集…', self.open_text_editor),
            ('交流の文章（通常・特別場面）', '選択した猫・人物の通常文を開きます。資料・採用文・画像を比較し、確認して登録します。',
             '通常交流の文章を編集…', self.open_interaction_text_editor),
            ('特別場面の文章', '動作切替・心をつかむ・心を開く・同時成立の文章を制作します。',
             '特別場面の文章を編集…', lambda: self.open_interaction_text_editor('switch'))):
            group = ttk.LabelFrame(text_page, text=title, padding=8)
            group.pack(fill='x', pady=(0, 8))
            ttk.Label(group, text=description, wraplength=780).pack(anchor='w', pady=(0, 10))
            ttk.Button(group, text=label, command=command).pack(anchor='w')
        ttk.Label(text_page, text='下書き保存 → 内容を確認 → 確認した文章を登録 → Unity向けに書き出す', wraplength=780).pack(anchor='w')
        self.settings_window = tk.Toplevel(self.window)
        self.settings_window.title('画像生成の設定')
        self.settings_window.geometry('800x450')
        self.settings_window.withdraw()
        self.settings_window.protocol('WM_DELETE_WINDOW', self.settings_window.withdraw)
        settings = ttk.Frame(self.settings_window, padding=12)
        settings.pack(fill='both', expand=True)
        self.pages['生成設定'] = settings
        self.settings_button = ttk.Button(subjects, text='生成設定…', command=self.settings_window.deiconify)
        self.settings_button.pack(side='right', padx=8)
        ttk.Label(settings, text='画像生成の準備', font=('', 12, 'bold')).pack(anchor='w', pady=(0, 12))
        self.settings_target = tk.StringVar(master=self.window)
        ttk.Label(settings, textvariable=self.settings_target, wraplength=780).pack(anchor='w', pady=(0, 16))
        for title, description, label, command in (
            ('生成する内容', '猫の外見、動作・行動、背景を設定します。場面・行動は「画像」タブで選択します。',
             '外見・行動・背景の設定…', self.open_prompt_editor),
            ('モデルへの追加設定', 'デザイン・ポーズ用LoRAと適用する強さを設定します。',
             'デザイン・ポーズLoRAの設定…', self.open_lora_editor)):
            group = ttk.LabelFrame(settings, text=title, padding=14)
            group.pack(fill='x', pady=(0, 8))
            ttk.Label(group, text=description, wraplength=780).pack(anchor='w', pady=(0, 10))
            ttk.Button(group, text=label, command=command).pack(anchor='w')
        review = self.pages['確認・書き出し']
        ttk.Label(review, text='全素材には猫画像・紹介文・交流文・人物画像が含まれます。下書きと生成候補は含みません。', wraplength=760).pack(anchor='w', pady=8)
        ttk.Button(review, text='選択対象の文章・画像を確認…', command=self.open_review_preview).pack(anchor='w', pady=4)
        ttk.Button(review, text='登録状況を更新', command=self.refresh_export_summary).pack(anchor='w', pady=4)
        from tkinter.scrolledtext import ScrolledText
        self.export_summary = ScrolledText(review, height=8, wrap='word', state='disabled')
        self.export_summary.pack(fill='both', expand=True, pady=8)
        ttk.Button(review, text='全素材をUnity向けに書き出す…', command=self.export_unity).pack(anchor='w', pady=4)
        ttk.Button(review, text='人物画像だけを書き出す…', command=self.export_people).pack(anchor='w', pady=4)
        ttk.Label(review, text='書き出し後、Unityの「Cat Cafe → 素材 → 素材パックを取り込む…」で取り込みます。Unity収録状態：未確認', wraplength=760).pack(anchor='w', pady=8)
        self.notice = tk.StringVar(master=self.window)
        ttk.Separator(frame).pack(fill='x', pady=(12, 8))
        ttk.Label(frame, textvariable=self.notice, wraplength=840).pack(anchor='w')
        footer = ttk.Frame(frame)
        footer.pack(fill='x', pady=(8, 0))
        ttk.Label(footer, text='書き出し対象：全登録素材（人物画像を含む）').pack(side='left')
        ttk.Button(footer, text='閉じる', command=self.close).pack(side='right')
        ttk.Button(footer, text='確認・書き出しへ', command=self.show_export_page).pack(side='right', padx=12)
        self.reload_cats()
        self.filter_subjects()
        self.select_image_cat()
        if cat_id is not None:
            self.cat.set(next((label for label, key in self.cats.items() if key == cat_id), self.cat.get()))
            self.select_image_cat()
        self.tabs.bind('<<NotebookTabChanged>>', self.on_tab_changed)

    def on_tab_changed(self, event=None):
        if self.tabs.select() == str(self.pages['確認・書き出し']):
            self.refresh_export_summary()

    def filter_subjects(self):
        query = self.subject_search.get().casefold()
        rows = self.subject_registry.read()['subjects']
        self.subject_rows = {('猫：' if row['kind']=='cat_response' else '人物：') + row['name'] + '（' + row['id'] + '）': row for row in rows}
        self.subject_selector.configure(values=[label for label in self.subject_rows if query in label.casefold()])

    def select_image_cat(self, event=None):
        content_id = self.cats.get(self.cat.get())
        self.active_subject = next((r for r in self.subject_rows.values() if r['kind']=='cat_response' and r['content_id']==content_id), None)
        self.subject_choice.set(next((label for label,r in self.subject_rows.items() if r==self.active_subject), '画像対象のみ（資料未登録）'))
        for name in ('画像',):
            self.tabs.tab(self.pages[name], state='normal')
        self.settings_button.state(['!disabled'])
        self.show_image_panel()
        self.refresh()

    def select_subject(self, event=None):
        row = self.subject_rows.get(self.subject_choice.get())
        if not row:
            return
        self.active_subject = row
        if row['kind']=='cat_response':
            for name in ('画像',):
                self.tabs.tab(self.pages[name], state='normal')
            self.cat.set(next((label for label,id in self.cats.items() if id==row['content_id']), ''))
            self.settings_button.state(['!disabled'])
            self.show_image_panel()
            self.refresh()
        else:
            self.show_image_panel()
            self.settings_button.state(['disabled'])
            self.settings_window.withdraw()
            self.notice.set('人物の資料・文章・画像を制作できます。')

    def show_image_panel(self):
        self.tabs.tab(self.pages['画像'], state='normal')
        if self.active_subject and self.active_subject['kind']=='speech':
            self.cat_image_panel.pack_forget()
            self.portrait_panel.frame.pack(fill='both', expand=True)
            self.portrait_panel.select(self.subject_registry.get(self.active_subject['id']))
        else:
            self.portrait_panel.frame.pack_forget()
            self.cat_image_panel.pack(fill='both', expand=True)

    def show_export_page(self):
        self.tabs.select(self.pages['確認・書き出し'])
        self.refresh_export_summary()

    def refresh_export_summary(self):
        try:
            from human_portraits import HumanPortraitStore
            people, _, warnings = HumanPortraitStore(self.registry.directory).export_data()
            entries = self.interaction_published.read()['entries']
            rows = self.subject_registry.read()['subjects']
            excluded = [r['name'] for r in rows if not r['enabled'] or not r['game_id']]
            text = ('全素材パック：猫画像・紹介文・交流文・人物画像\n'
                    + f"猫画像対象：{len(self.catalog_provider())} ／ 採用交流文：{len(entries)}条件\n"
                    + f"書き出し可能な人物画像：{len(people['people'])}人\n"
                    + '制作専用・利用対象外：' + ('、'.join(excluded) or 'なし')
                    + '\n人物画像の注意：' + ('\n'.join(warnings) or 'なし')
                    + '\nこれは登録状況の概要です。本文の不足・画像の代替は組合せ確認、出力時の詳細は結果とvalidation.jsonで確認してください。'
                    + '\n書き出しは全登録範囲が対象です。未保存の編集・下書きは含みません。')
            self.export_summary.configure(state='normal')
            self.export_summary.delete('1.0', 'end')
            self.export_summary.insert('1.0', text)
            self.export_summary.configure(state='disabled')
            return True
        except (OSError, ValueError) as exc:
            self.notice.set(f'登録状況を読み込めません：{exc}')
            return False

    def open_review_preview(self):
        from interaction_preview_gui import InteractionPreviewWindow
        from interaction_texts import condition, make_entry
        try:
            existing = getattr(self, 'review_window', None)
            if existing and existing.window.winfo_exists():
                if not existing.confirm_leave():
                    return
                existing.window.destroy()
            row = self.active_subject
            value = condition(target_cat='*') if not row else condition(row['kind'], row['id'],
                reaction='neutral' if row['kind']=='cat_response' else 'any',
                subjects=self.subject_registry.labels(include_disabled=True), target_cat='*' if row['kind']=='speech' else None)
            self.review_window = InteractionPreviewWindow(self.window, self.catalog_provider(), self.registry,
                self.interaction_published, editing_entry=make_entry(value, ['', '', '']), drafts=self.interaction_drafts, on_edit_condition=self.edit_review_condition)
            self.review_window.use_edit.set(False)
            self.review_window.render()
        except (OSError, ValueError) as exc:
            self.notice.set(f'確認画面を開けません：{exc}')

    def edit_review_condition(self, value):
        from interaction_text_editor_gui import InteractionTextEditorWindow
        try:
            editor = getattr(self, 'interaction_text_editor', None)
            if editor and editor.window.winfo_exists():
                return editor.open_condition_from_preview(value)
            self.interaction_text_editor = InteractionTextEditorWindow(self.window,
                self.interaction_drafts, self.interaction_published, image_registry=self.registry,
                catalog_provider=self.catalog_provider, subject_registry=self.subject_registry,
                initial_condition=value)
            return True
        except (OSError, ValueError) as exc:
            self.notice.set(f'条件を開けません：{exc}')
            return False

    def export_people(self):
        from tkinter import filedialog, messagebox
        from human_portraits import HumanPortraitStore
        try:
            store = HumanPortraitStore(self.registry.directory)
            data, _, warnings = store.export_data()
            if not data['people'] and not messagebox.askyesno('人物画像が0件', '空の人物パックを取り込むと人物画像の対応が解除されます。書き出しますか？', parent=self.window):
                return
            destination = filedialog.askdirectory(parent=self.window, title='人物画像パックの保存先')
            if destination:
                folder, warnings = store.export(destination)
                self.notice.set('人物画像を書き出しました：'+str(folder)+'\n'+' ／ '.join(warnings))
        except (OSError, ValueError) as exc:
            self.notice.set(f'人物画像を書き出せません：{exc}')

    def close(self):
        preview = getattr(self, 'review_window', None)
        if preview and preview.window.winfo_exists() and not preview.confirm_leave():
            return
        editor = getattr(self, 'interaction_text_editor', None)
        if editor is not None and editor.window.winfo_exists() and not editor.confirm_leave():
            return
        optional = getattr(self, 'optional_event_editor', None)
        if optional is not None and optional.window.winfo_exists() and not optional.confirm_leave():
            return
        manager=getattr(self,'subject_manager',None)
        if manager is not None and manager.window.winfo_exists() and not manager.confirm_leave():return
        self.window.destroy()

    def open_subject_manager(self):
        from interaction_subject_gui import SubjectManagerWindow
        existing=getattr(self,'subject_manager',None)
        if existing is not None and existing.window.winfo_exists():
            if self.active_subject and existing.current != self.active_subject and existing.confirm_leave():
                existing.load(self.subject_registry.get(self.active_subject['id']))
                existing.tabs.select(0 if self.active_subject['kind']=='cat_response' else 1)
            existing.window.lift()
            return
        def saved():
            self.cats={f'{name}（{id}）':id for id,name in self.catalog_provider().items()}
            self.cat_selector.configure(values=list(self.cats))
            editor=getattr(self,'interaction_text_editor',None)
            if editor is not None and editor.window.winfo_exists():editor.refresh_subjects()
            self.filter_subjects()
        try:
            self.subject_manager=SubjectManagerWindow(self.window,self.subject_registry,catalog=self.catalog_provider(),drafts=self.interaction_drafts,published=self.interaction_published,on_saved=saved)
            if self.active_subject:
                self.subject_manager.load(self.active_subject)
                self.subject_manager.tabs.select(0 if self.active_subject['kind']=='cat_response' else 1)
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
        if self.active_subject and self.active_subject['kind']=='speech':
            self.notice.set('紹介文・4場面は猫用です。人物は交流の文章を開いてください。')
            return
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

    def open_interaction_text_editor(self, narrative_scene='normal'):
        from interaction_text_editor_gui import InteractionTextEditorWindow
        from interaction_texts import condition
        try:
            subject = self.active_subject
            initial = condition(action='any' if narrative_scene=='normal' else 'switch', target_cat='*', narrative_scene=narrative_scene)
            if subject:
                kind = subject['kind']
                initial = condition(kind, subject['id'],
                    action='any' if narrative_scene=='normal' else 'switch',
                    reaction='neutral' if kind=='cat_response' and narrative_scene=='normal' else 'any',
                    subjects=self.subject_registry.labels(include_disabled=True),
                    target_cat='*' if kind=='speech' else None, narrative_scene=narrative_scene)
            existing = getattr(self, 'interaction_text_editor', None)
            if existing is not None and existing.window.winfo_exists():
                current = existing._condition
                if current['subject_id'] != initial['subject_id'] or current.get('narrative_scene', 'normal') != narrative_scene:
                    existing.load_condition(initial)
                existing.window.lift()
                return
            self.interaction_text_editor = InteractionTextEditorWindow(
                self.window, self.interaction_drafts, self.interaction_published,
                image_registry=self.registry, catalog_provider=self.catalog_provider,
                subject_registry=self.subject_registry, initial_condition=initial)
        except (OSError, ValueError) as exc:
            self.notice.set(f'交流文を開けません：{exc}')

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
