"""Person image workspace; adopts through the existing portrait store."""
from pathlib import Path
from tkinter import ttk, filedialog, messagebox
from content_images import load_display_image
from human_portraits import HumanPortraitStore


class PortraitPanel:
    def __init__(self, parent, directory):
        self.frame = ttk.Frame(parent)
        self.store = HumanPortraitStore(directory)
        self.subject = None
        self.pending = None
        self.candidates = {}
        self.photos = []
        self.heading = ttk.Label(self.frame, text='人物を選んでください。')
        self.heading.pack(anchor='w', pady=8)
        columns = ttk.Frame(self.frame)
        columns.pack(fill='both', expand=True)
        left = ttk.LabelFrame(columns, text='採用済み画像')
        left.pack(side='left', fill='both', expand=True)
        right = ttk.LabelFrame(columns, text='未採用の候補')
        right.pack(side='left', fill='both', expand=True)
        self.adopted = ttk.Label(left, text='採用画像なし', anchor='center')
        self.adopted.pack(fill='both', expand=True)
        self.candidate = ttk.Label(right, text='候補画像なし', anchor='center')
        self.candidate.pack(fill='both', expand=True)
        controls = ttk.Frame(self.frame)
        controls.pack(fill='x', pady=8)
        ttk.Button(controls, text='候補PNGを選ぶ…', command=self.choose).pack(side='left')
        self.adopt_button = ttk.Button(controls, text='この人物画像を採用', command=self.adopt, state='disabled')
        self.adopt_button.pack(side='left', padx=8)
        ttk.Button(controls, text='画像対応を解除…', command=self.remove).pack(side='right')
        self.notice = ttk.Label(self.frame, text='', wraplength=700)
        self.notice.pack(anchor='w')

    def select(self, subject):
        self.subject = subject
        self.pending = None
        self.photos = []
        self.adopt_button.state(['disabled'])
        self.candidate.configure(image='', text='候補画像なし')
        self.adopted.configure(image='', text='採用画像なし（共通人物記号）')
        self.heading.configure(text=subject['name'] + ' ／ 客ID：' + (subject['game_id'] or '制作専用'))
        self.notice.configure(text='候補の選択だけでは採用されません。32〜2048pxのPNGを使用します。')
        try:
            row = next((r for r in self.store.read()['people'] if r['subject_id']==subject['id']), None)
            if row:
                self.show(self.adopted, self.store.directory/row['image'])
            if subject['id'] in self.candidates:
                self.select_file(self.candidates[subject['id']])
        except (OSError, ValueError) as exc:
            self.notice.configure(text=str(exc))

    def show(self, widget, path):
        photo = load_display_image(self.frame, path, 320, 280)
        self.photos.append(photo)
        widget.configure(image=photo, text='')

    def choose(self):
        source = filedialog.askopenfilename(parent=self.frame, filetypes=[('PNG', '*.png')])
        if source:
            self.select_file(source)

    def select_file(self, source):
        try:
            if not self.subject or not self.subject['enabled'] or not self.subject['game_id'] or self.subject['game_id']=='player':
                raise ValueError('人物画像には保存済みの有効な客IDが必要です。資料で対応を確認してください。')
            from PIL import Image
            with Image.open(source) as im:
                if im.format!='PNG' or min(im.size)<32 or max(im.size)>2048:
                    raise ValueError('32〜2048pxのPNGを選んでください。')
                im.verify()
            self.show(self.candidate, source)
            self.pending = Path(source)
            self.candidates[self.subject['id']] = self.pending
            self.adopt_button.state(['!disabled'])
            self.notice.configure(text='未採用の候補です。左の採用画像と比較してください。')
        except (OSError, ValueError) as exc:
            self.notice.configure(text=str(exc))

    def adopt(self):
        try:
            if self.pending is None:
                return False
            self.store.register(self.subject['id'], self.pending)
            self.candidates.pop(self.subject['id'], None)
            self.select(self.subject)
            self.notice.configure(text='人物画像を採用しました。Unityへの反映は書き出し・取り込み後です。')
            return True
        except (OSError, ValueError) as exc:
            self.notice.configure(text=str(exc))
            return False

    def remove(self):
        if not self.subject or not messagebox.askyesno('画像対応を解除', self.subject['name']+'の画像対応を解除しますか？素材ファイルは保持します。', parent=self.frame):
            return
        try:
            self.store.remove(self.subject['id'])
            self.select(self.subject)
        except (OSError, ValueError) as exc:
            self.notice.configure(text=str(exc))
