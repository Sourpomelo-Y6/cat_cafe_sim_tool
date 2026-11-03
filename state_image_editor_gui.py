import hashlib
import tkinter as tk
from tkinter import ttk, filedialog, messagebox
from content_images import load_display_image, ImageRegistry
from state_images import STATES, png


class StateImageEditorWindow:
    def __init__(self, parent, store, catalog_provider):
        self.store, self.catalog_provider = store, catalog_provider
        store.read()
        self.window = tk.Toplevel(parent)
        self.window.title('猫の状態別画像 — 比較・採用・出力')
        self.window.geometry('920x740')
        self.candidate = None
        self.candidate_hash = None
        self.compared = False
        self.photos = []
        self.state = tk.StringVar(value=next(iter(STATES.values())))
        frame = ttk.Frame(self.window, padding=16)
        frame.pack(fill='both', expand=True)
        ttk.Label(frame, text='猫詳細だけに使用します。同じ画像IDを使う個体で共有し、状態は個体ごとに判定します。').pack(anchor='w')
        self.content = tk.StringVar()
        self.catalog_select = ttk.Combobox(frame, textvariable=self.content, state='readonly')
        self.catalog_select.pack(fill='x', pady=4)
        self.catalog_select.bind('<<ComboboxSelected>>', self.changed)
        ttk.Button(frame, text='画像対象の一覧を更新', command=lambda: self.run(self.refresh_catalog)).pack(anchor='w')
        self.summary = ttk.Label(frame)
        self.summary.pack(anchor='w')
        select = ttk.Combobox(frame, textvariable=self.state, values=list(STATES.values()), state='readonly')
        select.pack(fill='x', pady=8)
        select.bind('<<ComboboxSelected>>', self.changed)
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
        for text, command in [('候補PNGを選ぶ…', self.choose), ('採用と比較', self.compare), ('候補を採用', self.adopt), ('未登録に戻す', self.clear), ('状態画像だけ出力…', self.export)]:
            ttk.Button(actions, text=text, command=lambda c=command: self.run(c)).pack(side='left', padx=3)
        self.notice = tk.StringVar(value='比較してから採用してください。未登録の状態は従来画像を使用します。')
        ttk.Label(frame, textvariable=self.notice, wraplength=790).pack(fill='x', pady=12)
        ttk.Label(frame, text='採用だけではUnityへ反映されません。出力→取り込み→通常ビルドが必要です。').pack(anchor='w')
        self.refresh_catalog()

    def content_key(self):
        if self.content.get() not in self.contents:
            raise ValueError('画像IDを選択してください。')
        return self.contents[self.content.get()]

    def refresh_catalog(self):
        old = getattr(self, "contents", {}).get(self.content.get(), "playtest-mike")
        catalog = self.catalog_provider()
        ids = list(dict.fromkeys([*catalog, *(r["content_cat_id"] for r in self.store.read()["bindings"])]))
        self.contents = {f"{catalog.get(cid, '対象不明')}（{cid}）": cid for cid in ids}
        self.catalog_select["values"] = list(self.contents)
        self.content.set(next((label for label, cid in self.contents.items() if cid == old), next(iter(self.contents), "")))
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
        self.condition.config(text=self.state.get() + '：ストレス60以上 → 疲労60以上 → 好感度80以上の優先順。健康・在店時のみ。')
        self.run(self.show)

    def show(self):
        self.photos = []
        rows = self.store.read()['bindings']
        self.summary.config(text=f"全登録：{len({r['content_cat_id'] for r in rows})}画像ID／{len(rows)}対応。出力は全体置換用です。")
        if not self.contents:
            self.notice.set('画像対象がありません。通常の素材一覧へ画像対象を登録してください。')
            return
        cid = self.content_key()
        self.normal_path = ImageRegistry(self.store.directory.parent).image_path(cid, 'normal')
        for label, path, empty in ((self.normal, self.normal_path, '通常画像なし'), (self.current, self.store.image_path(self.state_key(), content_id=cid), '未登録・従来画像を使用'), (self.preview, self.candidate, '候補なし')):
            if path:
                if label is not self.normal:
                    png(path.read_bytes())
                photo = load_display_image(self.window, path, 240, 320)
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
        cid = self.content_key()
        if cid not in self.catalog_provider():
            raise ValueError('対象不明の画像IDには採用できません。素材一覧を戻すか登録を解除してください。')
        self.store.adopt(self.state_key(), self.candidate, self.candidate_hash, content_id=cid)
        self.compared = False
        self.show()
        self.notice.set('選択中の状態だけ採用しました。次の出力に含まれます。')

    def clear(self):
        self.store.adopt(self.state_key(), content_id=self.content_key())
        self.changed()
        self.notice.set('選択中の状態を未登録へ戻しました。原本は削除していません。')

    def export(self):
        folder = filedialog.askdirectory(parent=self.window, title='状態画像パックの出力先')
        if folder:
            rows = self.store.read()['bindings']
            if not messagebox.askokcancel('全登録を出力', f"{len({r['content_cat_id'] for r in rows})}画像ID／{len(rows)}対応をすべて出力します。Unityでは現行の全状態画像を置き換えます。", parent=self.window):
                return
            self.notice.set('出力しました：' + str(self.store.export(folder, self.catalog_provider())))
