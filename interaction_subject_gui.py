"""猫・人物の台帳編集。本文・ゲームルールを変更しない。"""
import copy
import uuid
from interaction_subjects import game_subjects


class SubjectManagerWindow:
    def __init__(self,parent,registry,*,catalog=None,drafts=None,published=None,on_saved=None):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        self.registry,self.catalog=registry,dict(catalog or {})
        self.drafts,self.published,self.on_saved=drafts,published,on_saved
        self.references=game_subjects();self.current=None;self.baseline=None;self.loading=False
        self.window=tk.Toplevel(parent);self.window.title('制作対象の登録 — 猫・人物・お客さん')
        self.window.geometry('960x760');self.window.minsize(840,700)
        self.window.protocol('WM_DELETE_WINDOW',self.close)
        frame=ttk.Frame(self.window,padding=12);frame.pack(fill='both',expand=True)
        ttk.Label(frame,text='既存のゲーム対象を選ぶか、制作専用の対象を追加します。ゲームの加入・来店設定は変更しません。',wraplength=800).pack(anchor='w')
        self.tabs=ttk.Notebook(frame);self.tabs.pack(fill='x',pady=8)
        self.trees={}
        for kind,label in [('cat_response','猫'),('speech','人物・お客さん')]:
            page=ttk.Frame(self.tabs);self.tabs.add(page,text=label)
            tree=ttk.Treeview(page,columns=('name','game','state','counts'),show='headings',height=5,selectmode='browse')
            for key,title,width in [('name','対象',160),('game','ゲームID',160),('state','状態',100),('counts','下書き案／登録案',160)]:
                tree.heading(key,text=title);tree.column(key,width=width)
            tree.pack(side='left',fill='x',expand=True)
            bar=ttk.Scrollbar(page,orient='vertical',command=tree.yview);bar.pack(side='right',fill='y');tree.configure(yscrollcommand=bar.set)
            tree.bind('<<TreeviewSelect>>',lambda event,k=kind:self.select(k))
            self.trees[kind]=tree
        toolbar=ttk.Frame(frame);toolbar.pack(fill='x',pady=(0,8))
        self.reference=tk.StringVar(master=self.window)
        self.reference_combo=ttk.Combobox(toolbar,textvariable=self.reference,state='readonly',width=36);self.reference_combo.pack(side='left')
        ttk.Button(toolbar,text='既存対象から追加',command=self.add_existing).pack(side='left',padx=8)
        ttk.Button(toolbar,text='制作専用の対象を追加',command=lambda:self.new()).pack(side='left')
        self.tabs.bind('<<NotebookTabChanged>>',self.tab_changed)
        form=ttk.Frame(frame);form.pack(fill='x');form.columnconfigure(1,weight=1)
        self.variables={};self.entries={}
        for i,(name,label) in enumerate([('id','制作対象ID（変更不可）'),('name','表示名'),('game_id','ゲームID（空欄なら制作のみ）'),('content_id','画像素材ID（猫のみ）'),('call','猫の呼び名（人物用、例：{cat}ちゃん）')]):
            ttk.Label(form,text=label).grid(row=i,column=0,sticky='w',pady=3,padx=(0,12))
            var=tk.StringVar(master=self.window);self.variables[name]=var
            field=ttk.Entry(form,textvariable=var);field.grid(row=i,column=1,sticky='ew',pady=3)
            self.entries[name]=field
        self.entries['id'].configure(state='readonly')
        self.enabled=tk.BooleanVar(master=self.window,value=True)
        ttk.Checkbutton(frame,text='制作・書き出しの対象として利用する（無効にしても本文は削除しません）',variable=self.enabled).pack(anchor='w',pady=6)
        ttk.Label(frame,text='生成に使う資料：猫の特徴・性格、または人物の口調・接し方。未設定の来歴などは書き足しません。',wraplength=800).pack(anchor='w')
        self.notes=ScrolledText(frame,height=5,wrap='word',undo=True);self.notes.pack(fill='both',expand=True,pady=6)
        buttons=ttk.Frame(frame);buttons.pack(fill='x')
        ttk.Button(buttons,text='対象と資料を保存',command=self.save).pack(side='left')
        ttk.Button(buttons,text='台帳を再読込',command=self.reload).pack(side='left',padx=8)
        ttk.Button(buttons,text='閉じる',command=self.close).pack(side='right')
        self.notice=tk.StringVar(master=self.window)
        ttk.Label(frame,textvariable=self.notice,wraplength=800).pack(anchor='w',pady=(6,0))
        self.refresh();self.refresh_references();self.load(registry.get('cat-mike'))

    def tab_changed(self,event=None):
        if self.loading:return
        kind=self.kind()
        if self.current and self.current['kind']!=kind:
            if not self.confirm_leave():
                self.tabs.select(0 if self.current['kind']=='cat_response' else 1);return
            row=next((r for r in self.registry.read()['subjects'] if r['kind']==kind),None)
            if row:self.load(row)
        self.refresh_references()

    def kind(self):return 'cat_response' if self.tabs.index(self.tabs.select())==0 else 'speech'

    def refresh_references(self):
        self.reference_labels={f"{r['name']}（{id}）":r for id,r in self.references[self.kind()].items()}
        self.reference_combo.configure(values=list(self.reference_labels))
        if self.reference.get() not in self.reference_labels:self.reference.set(next(iter(self.reference_labels),''))

    def refresh(self):
        self.snapshot=self.registry.read();self.loading=True
        for tree in self.trees.values():tree.delete(*tree.get_children())
        totals=[]
        for store in (self.drafts,self.published):
            counts={}
            for entry in store.read()['entries'] if store else []:
                id=entry['condition']['subject_id']
                counts[id]=counts.get(id,0)+sum(bool(v['text']) for v in entry['variants'])
            totals.append(counts)
        for row in self.snapshot['subjects']:
            counts=[total.get(row['id'],0) for total in totals]
            state='利用対象外' if not row['enabled'] else '対応済み' if row['game_id'] else '制作のみ'
            self.trees[row['kind']].insert('','end',iid=row['id'],values=(row['name'],row['game_id'],state,f'{counts[0]}／{counts[1]}'))
        self.loading=False

    def value(self):
        if self.current is None:return None
        row={k:v.get().strip() for k,v in self.variables.items()}
        row.update(kind=self.current['kind'],notes=self.notes.get('1.0','end-1c').strip(),enabled=self.enabled.get())
        return row

    def confirm_leave(self):
        from tkinter import messagebox
        if self.current is None or self.value()==self.baseline:return True
        answer=messagebox.askyesnocancel('対象の未保存編集','変更した対象と資料を保存しますか？',parent=self.window)
        return self.save() if answer is True else answer is False

    def load(self,row):
        if row is None:return
        self.current=copy.deepcopy(row)
        for k,var in self.variables.items():var.set(row[k])
        self.enabled.set(row['enabled']);self.notes.delete('1.0','end');self.notes.insert('1.0',row['notes'])
        self.baseline=copy.deepcopy(row)
        self.entries['content_id'].configure(state='normal' if row['kind']=='cat_response' else 'disabled')
        self.notice.set('猫は画像素材IDとゲームIDを別々に対応付けます。名前の変更で本文やIDは作り直しません。')

    def select(self,kind):
        tree=self.trees[kind];selected=tree.selection()
        if self.loading or not selected:return
        if self.current and self.current['id']==selected[0]:return
        if not self.confirm_leave():
            if self.current and self.current['id'] in tree.get_children():tree.selection_set(self.current['id'])
            return
        self.load(self.registry.get(selected[0]))

    def new(self,reference=None):
        if not self.confirm_leave():return
        kind=self.kind();ref=reference or {}
        game=ref.get('game_id','')
        old=next((r for r in self.registry.read()['subjects'] if r['kind']==kind and game and r['game_id']==game),None)
        if old:self.load(old);return
        id=('cat-' if kind=='cat_response' else 'person-')+uuid.uuid4().hex
        self.load(dict(id=id,kind=kind,name=ref.get('name','新しい猫' if kind=='cat_response' else '新しいお客さん'),
            game_id=game,content_id=ref.get('content_id','subject-'+uuid.uuid4().hex if kind=='cat_response' else ''),notes=ref.get('notes',''),call='{cat}',enabled=True))

    def add_existing(self):self.new(self.reference_labels.get(self.reference.get()))

    def save(self):
        try:
            row=self.value()
            if row['content_id'] and row['content_id'] not in self.catalog and self.catalog:
                # New production-only images get their own stable asset ID, never a guessed game ID.
                self.notice.set('未登録の画像素材IDです。画像画面でこのIDへ画像を登録できます。')
            self.registry.save(row,expected=self.snapshot);self.baseline=copy.deepcopy(row);self.current=copy.deepcopy(row)
            self.refresh()
            if self.on_saved:self.on_saved()
            self.notice.set('対象と資料を保存しました。本文・ゲームの加入や来店ルールは変更していません。')
            return True
        except (OSError,ValueError) as exc:self.notice.set(f'保存できません：{exc}');return False

    def reload(self):
        if self.confirm_leave():
            try:self.refresh();self.load(self.registry.get(self.current['id']))
            except (OSError,ValueError) as exc:self.notice.set(str(exc))

    def close(self):
        if self.confirm_leave():self.window.destroy()
