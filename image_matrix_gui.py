"""キャラクターごとに交流画像を確認し、空欄を順次生成する。"""
import hashlib
import json
import queue
import threading
import tkinter as tk
from tkinter import ttk

from comfy_client import ComfyClient, GENERATED_DIRECTORY, random_seed, workflow_defaults
from content_images import load_display_image
from interaction_texts import MODES, NARRATIVE_SCENES, REACTIONS, reactions, scene_actions
from interaction_scenes import ACTION_LABELS, MODE_ACTIONS, SPECIAL_ACTIONS


SPECIAL_ROWS = {key: label for key, label in NARRATIVE_SCENES.items() if key != "normal"}
SPECIAL_ROWS.update(SPECIAL_ACTIONS)
OPERATION_ROWS = {key: label for key, label in ACTION_LABELS.items() if key not in SPECIAL_ROWS}
ROW_LABELS = {**REACTIONS, **OPERATION_ROWS, **SPECIAL_ROWS}


def scene_key(key):
    mode, row = key
    return f"{mode}__{row}" if row not in REACTIONS else mode


POSE = {
    'neutral': 'calm neutral expression',
    'favorable': 'friendly expression, leaning toward the activity',
    'enthusiastic': 'eager engaged expression, energetic pose',
    'turn_away': 'turning away, refusing the interaction',
    'confused': 'puzzled expression, hesitant posture',
    'listless': 'low energy, tired drooping posture',
}


class ImageMatrix:
    def __init__(self, owner):
        self.owner = owner
        self.window = tk.Toplevel(owner.window)
        self.window.title('行動・反応別の画像表')
        self.window.geometry('1120x720')
        bar = ttk.Frame(self.window, padding=8)
        bar.pack(fill='x')
        self.cats = dict(owner.cats)
        self.cat = ttk.Combobox(bar, values=list(self.cats), state='readonly', width=32)
        self.cat.set(owner.cat.get())
        self.cat.pack(side='left')
        self.cat.bind('<<ComboboxSelected>>', lambda _: self.refresh())
        ttk.Button(bar, text='猫のベースプロンプト…', command=self.edit_cat_prompt).pack(side='left', padx=4)
        ttk.Label(bar, text='ComfyUI 接続先').pack(side='left', padx=8)
        self.url = ttk.Entry(bar, width=26)
        self.url.insert(0, 'http://127.0.0.1:8188')
        self.url.pack(side='left')
        self.batch = ttk.Button(bar, text='空欄を順番に生成', command=self.fill)
        self.batch.pack(side='left', padx=8)
        self.status = tk.StringVar(self.window)
        ttk.Label(self.window, textvariable=self.status, wraplength=980).pack(anchor='w', padx=8)
        body = ttk.Frame(self.window)
        body.pack(fill='both', expand=True)
        canvas = tk.Canvas(body, highlightthickness=0)
        for axis, side, fill in [('x', 'bottom', 'x'), ('y', 'right', 'y')]:
            scroll = ttk.Scrollbar(body, orient='horizontal' if axis == 'x' else 'vertical',
                                   command=getattr(canvas, axis + 'view'))
            scroll.pack(side=side, fill=fill)
            canvas.configure(**{axis + 'scrollcommand': scroll.set})
        canvas.pack(fill='both', expand=True)
        self.grid = ttk.Frame(canvas, padding=8)
        canvas.create_window(0, 0, window=self.grid, anchor='nw')
        self.grid.bind('<Configure>', lambda _: canvas.configure(scrollregion=canvas.bbox('all')))
        self.cells = {}
        self.paths = {}
        self.busy = False
        self.messages = queue.Queue()
        self.cancel = threading.Event()
        self.window.bind('<Destroy>', self.destroyed)
        ttk.Label(self.grid, text='反応・操作 ＼ 行動').grid(row=0, column=0)
        for col, (mode, label) in enumerate(MODES.items(), 1):
            ttk.Button(self.grid, text=label, command=lambda k=mode: self.edit_prompt(
                'matrix_columns', k, MODES[k])).grid(row=0, column=col, pady=6, sticky='ew')
            for row, (reaction, title) in enumerate(ROW_LABELS.items(), 1):
                if col == 1:
                    ttk.Button(self.grid, text=title, command=lambda k=reaction: self.edit_prompt(
                        'matrix_rows', k, ROW_LABELS[k])).grid(row=row, column=0, padx=6, sticky='ew')
                if reaction in OPERATION_ROWS:
                    supported = reaction in MODE_ACTIONS.get(mode, ())
                elif reaction in SPECIAL_ROWS:
                    supported = scene_actions(mode, reaction)
                else:
                    supported = reaction in reactions('cat_response', mode)
                if not supported:
                    ttk.Label(self.grid, text='対象外').grid(row=row, column=col)
                    continue
                key = mode, reaction
                btn = ttk.Button(self.grid, width=12, command=lambda k=key: self.detail(k))
                btn.grid(row=row, column=col, padx=2, pady=2, sticky='nsew', ipady=6)
                self.cells[key] = btn
        self.refresh()

    def destroyed(self, event):
        if event.widget == self.window:
            self.cancel.set()

    def folder(self, key):
        ident = hashlib.sha256(self.cats[self.cat.get()].encode()).hexdigest()
        return GENERATED_DIRECTORY / 'interaction_matrix' / ident / key[0] / key[1]

    def refresh(self):
        if self.cat.get() not in self.cats:
            self.batch.state(['disabled'])
            self.status.set('猫を登録してから画像表を開いてください。')
            return
        self.paths.clear()
        for key, btn in self.cells.items():
            try:
                # A metadata file is written after its PNG has finished saving.
                records = sorted(self.folder(key).glob('*.json'), key=lambda p: p.stat().st_mtime_ns, reverse=True)
                path = next((p.with_suffix('.png') for p in records if p.with_suffix('.png').is_file()), None)
                if path is None and (key[1] == 'neutral' or key[1] not in REACTIONS):
                    path = self.owner.registry.image_path(self.cats[self.cat.get()], scene_key(key))
                if path and path.is_file():
                    btn.preview = load_display_image(self.window, path, 100, 78)
                    btn.configure(image=btn.preview, text='')
                    self.paths[key] = path
                else:
                    btn.configure(image='', text='未作成')
                    btn.preview = None
            except (OSError, ValueError, tk.TclError) as exc:
                btn.configure(image='', text='読込エラー')
                self.status.set(str(exc))

    def detail(self, key):
        if self.cat.get() not in self.cats:
            return
        win = tk.Toplevel(self.window)
        win.title(f'{self.cat.get()} ／ {MODES[key[0]]} ／ {ROW_LABELS[key[1]]}')
        path = self.paths.get(key)
        try:
            prompt = self.prompt(key)
            if path:
                win.preview = load_display_image(win, path, 640, 420)
                ttk.Label(win, image=win.preview).pack()
                ttk.Label(win, text=str(path), wraplength=620).pack()
                record = path.with_suffix('.json')
                if record.is_file() and path.parent == self.folder(key):
                    previous = json.loads(record.read_text(encoding='utf-8'))['settings']['positive']
                    if previous != prompt:
                        ttk.Label(win, text='前回の生成プロンプト\n' + previous, wraplength=620).pack()
            ttk.Label(win, text='次回の生成プロンプト\n' + prompt, wraplength=620, padding=10).pack()
        except (OSError, ValueError, KeyError, tk.TclError) as exc:
            ttk.Label(win, text=str(exc)).pack()
        def generate():
            win.destroy()
            self.start([key])
        btn = ttk.Button(win, text='再作成' if path else 'この画像を生成', command=generate)
        btn.pack(pady=8)
        if self.busy:
            btn.state(['disabled'])
        win.transient(self.window)
        win.grab_set()

    def edit_cat_prompt(self):
        cat = self.cats.get(self.cat.get())
        if cat:
            self.edit_prompt('cats', cat, self.cat.get())

    def edit_prompt(self, section, key, label):
        presets = self.owner.presets
        try:
            if section == 'cats':
                initial = presets.fields(key, 'normal')['appearance']
                scope = 'この猫のベースプロンプト'
            else:
                default = POSE.get(key, '') if section == 'matrix_rows' else ''
                initial = presets.read().get(section, {}).get(key, default)
                scope = '全ての猫に共通する' + ('行' if section == 'matrix_rows' else '列') + 'の追加プロンプト'
        except (ValueError, OSError) as exc:
            self.status.set(str(exc))
            return
        win = tk.Toplevel(self.window)
        win.title(label + ' ／ ' + scope)
        ttk.Label(win, text=label + ' ／ ' + scope, padding=8).pack(anchor='w')
        editor = tk.Text(win, width=68, height=7, wrap='word', undo=True)
        editor.pack(fill='both', expand=True, padx=8)
        editor.insert('1.0', initial)
        error = ttk.Label(win)
        error.pack(anchor='w', padx=8)
        def save():
            try:
                presets.save_matrix_field(section, key, editor.get('1.0', 'end-1c'))
            except (ValueError, OSError) as exc:
                error.configure(text=str(exc))
                return
            self.status.set(label + 'のプロンプトを保存しました。次に開始する生成から反映します。')
            win.destroy()
        controls = ttk.Frame(win, padding=8)
        controls.pack(fill='x')
        ttk.Button(controls, text='保存', command=save).pack(side='right')
        ttk.Button(controls, text='キャンセル', command=win.destroy).pack(side='right', padx=8)
        win.transient(self.window)
        win.grab_set()
        editor.focus_set()

    def prompt(self, key):
        base = self.owner.presets.assemble(self.cats[self.cat.get()], scene_key(key))
        data = self.owner.presets.read()
        column = data.get('matrix_columns', {}).get(key[0], '')
        row = data.get('matrix_rows', {}).get(key[1], POSE.get(key[1], ''))
        return ', '.join(part for part in (base, column, row) if part)

    def fill(self):
        self.start([key for key in self.cells if key not in self.paths])

    def start(self, keys):
        if self.busy or not keys:
            if not keys:
                self.status.set('すべての画像が揃っています。')
            return
        try:
            cat = self.cats[self.cat.get()]
            ref = self.owner.references.image_path(cat, 'normal')
            defaults = workflow_defaults()
            jobs = []
            for key in keys:
                settings = dict(defaults, seed=random_seed(), positive=self.prompt(key),
                                loras=self.owner.lora_settings.active(cat, scene_key(key)))
                if ref:
                    settings.update(reference_path=str(ref), denoise=0.65)
                jobs.append((key, ComfyClient(self.url.get().strip(), self.folder(key)), settings))
        except (ValueError, OSError, KeyError) as exc:
            self.status.set(str(exc))
            return
        self.busy = True
        self.cat.state(['disabled'])
        self.batch.state(['disabled'])
        def work():
            try:
                for i, (key, client, settings) in enumerate(jobs, 1):
                    if self.cancel.is_set():
                        return
                    self.messages.put(('status', f'{i}/{len(jobs)} 生成中　{MODES[key[0]]} ／ {ROW_LABELS[key[1]]}'))
                    path = client.generate(settings, self.cancel)
                    self.messages.put(('image', (key, path)))
                self.messages.put(('done', f'{len(jobs)}枚の生成が完了しました。'))
            except Exception as exc:
                self.messages.put(('done', f'{MODES[key[0]]} ／ {ROW_LABELS[key[1]]} で停止しました：{exc}'))
        threading.Thread(target=work, daemon=True).start()
        self.poll()

    def poll(self):
        while not self.messages.empty():
            kind, value = self.messages.get_nowait()
            if kind == 'image':
                key, path = value
                btn = self.cells[key]
                try:
                    btn.preview = load_display_image(self.window, path, 100, 78)
                    btn.configure(image=btn.preview, text='')
                    self.paths[key] = path
                except (tk.TclError, OSError) as exc:
                    btn.configure(image='', text='読込エラー')
                    self.status.set(str(exc))
            else:
                self.status.set(value)
                if kind == 'done':
                    self.busy = False
                    self.cat.state(['!disabled'])
                    self.batch.state(['!disabled'])
        if self.busy:
            self.window.after(100, self.poll)
