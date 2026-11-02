import hashlib
import tkinter as tk
from tkinter import ttk, filedialog
from content_images import load_display_image, ImageRegistry
from cat_outfits import STATES, png


class CatOutfitEditorWindow:
    def __init__(self, parent, store, catalog_provider):
        self.store, self.catalog_provider = store, catalog_provider
        store.read()
        self.window = tk.Toplevel(parent)
        self.window.title('ミケの見た目 — 比較・採用・出力')
        self.window.geometry('850x620')
        self.candidate = None
        self.candidate_hash = None
        self.compared = False
        self.photos = []
        self.state = tk.StringVar(value=next(iter(STATES.values())))
        frame = ttk.Frame(self.window, padding=16)
        frame.pack(fill='both', expand=True)
        ttk.Label(frame, text='ミケの猫詳細だけに使用します。通常状態で、ゲーム中に選択した見た目を表示します。').pack(anchor='w')
        select = ttk.Combobox(frame, textvariable=self.state, values=list(STATES.values()), state='readonly')
        select.pack(fill='x', pady=8)
        select.bind('<<ComboboxSelected>>', self.changed)
        self.name = tk.StringVar(value=next((r['label'] for r in store.read()['outfits']), 'リボン姿'))
        ttk.Label(frame, text='表示名（選択のIDは変わりません）').pack(anchor='w')
        ttk.Entry(frame, textvariable=self.name).pack(fill='x')
        self.normal_path = ImageRegistry(store.directory.parent).image_path('playtest-mike', 'normal')
        self.condition = ttk.Label(frame)
        self.condition.pack(anchor='w')
        panes = ttk.Frame(frame)
        panes.pack(fill='both', expand=True, pady=12)
        self.normal = ttk.Label(panes, anchor='center', text='通常画像')
        self.normal.pack(side='left', fill='both', expand=True)
        self.current = ttk.Label(panes, anchor='center', text='現在の採用')
        self.current.pack(side='left', fill='both', expand=True)
        self.preview = ttk.Label(panes, anchor='center', text='候補なし')
        self.preview.pack(side='left', fill='both', expand=True)
        actions = ttk.Frame(frame)
        actions.pack(fill='x')
        for text, command in [('候補PNGを選ぶ…', self.choose), ('採用と比較', self.compare), ('候補を採用', self.adopt), ('未登録に戻す', self.clear), ('見た目だけ出力…', self.export)]:
            ttk.Button(actions, text=text, command=lambda c=command: self.run(c)).pack(side='left', padx=3)
        self.notice = tk.StringVar(value='比較してから採用してください。未登録の見た目は従来画像を使用します。')
        ttk.Label(frame, textvariable=self.notice, wraplength=790).pack(fill='x', pady=12)
        ttk.Label(frame, text='採用だけではUnityへ反映されません。出力→取り込み→通常ビルドが必要です。').pack(anchor='w')
        self.changed()

    def run(self, action):
        try:
            action()
        except (OSError, ValueError, tk.TclError) as exc:
            self.notice.set(str(exc))

    def state_key(self):
        return next(key for key, label in STATES.items() if label == self.state.get())

    def changed(self, _=None):
        self.candidate = self.candidate_hash = None
        self.compared = False
        self.condition.config(text='状態画像を優先します。健康・在店・通常状態のミケ詳細だけで表示します。')
        self.run(self.show)

    def show(self):
        self.photos = []
        for label, path, empty in ((self.normal, self.normal_path, "通常画像なし"), (self.current, self.store.image_path(self.state_key()), '未登録・従来画像を使用'), (self.preview, self.candidate, '候補なし')):
            if path:
                png(path.read_bytes())
                photo = load_display_image(self.window, path, 230, 300)
                self.photos.append(photo)
                label.config(image=photo, text='')
            else:
                label.config(image='', text=empty)

    def choose(self):
        path = filedialog.askopenfilename(parent=self.window, filetypes=[('PNG画像', '*.png')])
        if path:
            from pathlib import Path
            candidate = Path(path)
            png(candidate.read_bytes())
            self.candidate = candidate
            self.compared = False
            self.candidate_hash = None
            self.show()
            self.notice.set('左から通常・現在の採用・候補です。「採用と比較」で内容を確認してください。')

    def compare(self):
        if self.candidate is None:
            raise ValueError('候補PNGを選択してください。')
        self.show()
        self.candidate_hash = hashlib.sha256(png(self.candidate.read_bytes())).hexdigest()
        self.compared = True
        self.notice.set('左右の画像を比較できます。採用する場合は「候補を採用」を押してください。')

    def adopt(self):
        if not self.compared:
            raise ValueError('採用前に「採用と比較」を実行してください。')
        self.store.adopt(self.state_key(), self.candidate, self.candidate_hash, self.name.get())
        self.compared = False
        self.show()
        self.notice.set('リボン姿だけ採用しました。次の出力に含まれます。')

    def clear(self):
        self.store.adopt(self.state_key())
        self.changed()
        self.notice.set('リボン姿を未登録へ戻しました。原本は削除していません。')

    def export(self):
        folder = filedialog.askdirectory(parent=self.window, title='見た目パックの出力先')
        if folder:
            self.notice.set('出力しました：' + str(self.store.export(folder, self.catalog_provider())))
