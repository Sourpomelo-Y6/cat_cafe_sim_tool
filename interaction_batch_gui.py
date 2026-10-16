"""人物のセリフと猫の反応を条件別に連続生成し、個別に確認する。"""
import copy
import queue
import threading
import json
from pathlib import Path

from interaction_llm import InteractionLlmClient, InteractionLlmSettings, DEFAULT_OUTPUT, validate_inputs
from interaction_texts import SUBJECTS, MODES, REACTIONS, condition, make_entry, key, actions, reactions
from interaction_scenes import ACTION_LABELS
from llm_client import GenerationCancelled


def prepare_jobs(settings, subjects, modes, *, scope='common', has_text=None, kind='speech', selected_reactions=None, subject_registry=None, target_cat=None, get_entry=None, fill_partial=False):
    """開始前に全資料を検証し、実行中の設定変更から切り離す。"""
    registry=subject_registry or getattr(settings,'subject_registry',None)
    labels=registry.labels() if registry else SUBJECTS
    if scope not in ('common', 'specific', 'all'):
        raise ValueError('操作の範囲が不正です。')
    if kind not in SUBJECTS:
        raise ValueError('文章の種類が不正です。')
    chosen_reactions = list(REACTIONS) if selected_reactions is None else list(selected_reactions)
    if kind == 'cat_response' and (not chosen_reactions or any(r not in REACTIONS for r in chosen_reactions)):
        raise ValueError('猫の反応を1つ以上選んでください。')
    jobs = []
    for subject in subjects:
        if subject not in labels[kind]:
            raise ValueError('対象の人物・猫が不正です。')
        for mode in modes:
            if mode not in MODES:
                raise ValueError('行動が不正です。')
            for action in actions(mode):
                common = action in ('any', 'pause')
                if scope == 'common' and not common or scope == 'specific' and common:
                    continue
                for reaction in reactions(kind, mode):
                    if kind == 'cat_response' and reaction not in chosen_reactions:
                        continue
                    value = condition(kind, subject, mode, action, reaction, subjects=labels, target_cat=target_cat if kind=='speech' else None)
                    base=get_entry(value) if get_entry else None
                    if fill_partial:
                        if base and all(v['text'] for v in base['variants']):continue
                    elif has_text is not None and has_text(value):continue
                    notes = settings.get(value)
                    if fill_partial and base:
                        notes=copy.deepcopy(notes)
                        notes['base_entry']=copy.deepcopy(base)
                        notes['subject_notes']+='\n次の既存案は変更せず、重複しない別の案を作る：'+json.dumps([v['text'] for v in base['variants'] if v['text']],ensure_ascii=False)
                    try:
                        validate_inputs(notes['subject_notes'], notes['image_notes'], notes['style'])
                    except ValueError as exc:
                        raise ValueError(f"{InteractionBatchWindow.label(value, labels)}：{exc}") from exc
                    jobs.append((value, copy.deepcopy(notes)))
    if not jobs:
        raise ValueError('生成対象がありません。人物・行動の選択と入力済み条件を確認してください。')
    return jobs


class InteractionBatchWindow:
    def __init__(self, parent, on_result, *, settings=None, output=DEFAULT_OUTPUT, has_text=None, kind='speech', subject_registry=None, initial_subject=None, target_cat='*', get_entry=None, drafts=None, published=None, before_save=None, after_save=None, on_open_saved=None):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        self.subject_registry=subject_registry or getattr(settings,'subject_registry',None)
        self.labels=self.subject_registry.labels() if self.subject_registry else SUBJECTS
        self.settings = settings or InteractionLlmSettings(subject_registry=self.subject_registry)
        self.target_cat,self.get_entry=target_cat,get_entry
        self.drafts,self.published=drafts,published
        self.before_save,self.after_save=before_save,after_save
        self.on_open_saved=on_open_saved
        self.skipped,self.errors,self.excluded=set(),{},set()
        saved = self.settings.read()
        self.output, self.on_result = output, on_result
        self.has_text = has_text
        if kind not in SUBJECTS:
            raise ValueError('文章の種類が不正です。')
        self.kind = kind
        cat = kind == 'cat_response'
        self.jobs, self.results = [], {}
        self.messages, self.cancel = queue.Queue(), threading.Event()
        self.running, self.timer = False, None
        self.window = tk.Toplevel(parent)
        self.window.title('猫の反応を連続生成' if cat else '人物のセリフを連続生成')
        self.window.geometry('1040x900' if cat else '1040x820')
        self.window.minsize(980, 860 if cat else 780)
        self.window.bind('<Destroy>', self.destroyed)
        frame = ttk.Frame(self.window, padding=12)
        frame.pack(fill='both', expand=True)
        ttk.Label(frame, text='猫 × 行動 × 確定反応ごとに3案ずつ生成します。' if cat else '人物 × 行動ごとに3案ずつ、1条件ずつ順番に生成します。', font=('', 12, 'bold')).pack(anchor='w')
        ttk.Label(frame, text=('拒否・低体力などは別条件です。休憩は通常・低体力のみ生成します。\n' if cat else
                  '共通文に加え「激しく動かす」「フェイント」など操作別のセリフも生成できます。\n') +
                  '生成だけでは下書き・登録済み文章を変更しません。', wraplength=720).pack(anchor='w', pady=6)
        choices = ttk.LabelFrame(frame, text='生成する対象', padding=8)
        choices.pack(fill='x')
        labels=self.labels[kind]
        self.subjects, self.modes, self.option_widgets = {}, {}, []
        subjects_frame=ttk.Frame(choices)
        subjects_frame.grid(row=0,column=0,columnspan=3,sticky='ew')
        canvas=tk.Canvas(subjects_frame,height=58,highlightthickness=0)
        canvas.pack(side='left',fill='x',expand=True)
        scrollbar=ttk.Scrollbar(subjects_frame,orient='vertical',command=canvas.yview)
        scrollbar.pack(side='right',fill='y');canvas.configure(yscrollcommand=scrollbar.set)
        checks=ttk.Frame(canvas);canvas.create_window((0,0),window=checks,anchor='nw')
        checks.bind('<Configure>',lambda event:canvas.configure(scrollregion=canvas.bbox('all')))
        for i,(name,label) in enumerate(labels.items()):
            var=tk.BooleanVar(master=self.window,value=initial_subject is None or name==initial_subject)
            widget=ttk.Checkbutton(checks,text=label,variable=var)
            widget.grid(row=i//3,column=i%3,sticky='w',padx=10)
            self.option_widgets.append(widget);self.subjects[name]=var
        for col,(name,label) in enumerate(MODES.items()):
            var=tk.BooleanVar(master=self.window,value=True)
            widget=ttk.Checkbutton(choices,text=label,variable=var)
            widget.grid(row=1,column=col,sticky='w',padx=10)
            self.option_widgets.append(widget);self.modes[name]=var
        self.scopes = {'動作共通のみ': 'common', '操作別のみ': 'specific', '共通＋操作別': 'all'}
        self.scope = tk.StringVar(master=self.window, value='共通＋操作別' if has_text and not cat else '動作共通のみ')
        scope_selector = ttk.Combobox(choices, textvariable=self.scope, values=list(self.scopes), state='readonly', width=18)
        scope_selector.grid(row=2, column=0, sticky='w', padx=10, pady=(6, 0))
        self.only_missing = tk.BooleanVar(master=self.window, value=has_text is not None)
        missing = ttk.Checkbutton(choices, text='未入力の条件だけ生成', variable=self.only_missing)
        missing.grid(row=2, column=1, columnspan=2, sticky='w', padx=10, pady=(6, 0))
        if has_text is None:
            missing.state(['disabled'])
        self.partial=tk.BooleanVar(master=self.window,value=False)
        partial=ttk.Checkbutton(choices,text='3案未満の空欄を補う',variable=self.partial)
        partial.grid(row=3,column=0,columnspan=1,sticky='w',padx=10,pady=(4,0))
        if get_entry is None:partial.state(['disabled'])
        self.option_widgets.extend([scope_selector, missing, partial])
        self.continue_errors=tk.BooleanVar(master=self.window,value=True)
        self.continue_checkbox=ttk.Checkbutton(choices,text='失敗した項目を飛ばして続行',variable=self.continue_errors)
        self.continue_checkbox.grid(row=3,column=1,columnspan=2,sticky='w',padx=10)
        if not cat:
            scope_label='どの猫でも使う' if self.target_cat=='*' else self.labels['cat_response'].get(self.target_cat or 'cat-mike','猫専用')+'専用'
            ttk.Label(choices,text='猫への適用：'+scope_label).grid(row=4,column=0,columnspan=3,sticky='w',padx=10)
        self.reaction_choices = {}
        if cat:
            reaction_frame = ttk.LabelFrame(frame, text='生成する反応（初期範囲：共通14条件・42案、入力済みは除外）', padding=6)
            reaction_frame.pack(fill='x', pady=(6, 0))
            for i, (name, label) in enumerate(REACTIONS.items()):
                var = tk.BooleanVar(master=self.window, value=True)
                widget = ttk.Checkbutton(reaction_frame, text=label, variable=var)
                widget.grid(row=i//3, column=i%3, sticky='w', padx=10)
                self.reaction_choices[name] = var
                self.option_widgets.append(widget)
        connection = ttk.Frame(frame)
        connection.pack(fill='x', pady=8)
        self.url = tk.StringVar(master=self.window, value=saved['url'])
        self.model = tk.StringVar(master=self.window, value=saved['model'])
        self.connection_widgets = []
        for row, (label, var) in enumerate((('接続先', self.url), ('モデル', self.model))):
            ttk.Label(connection, text=label).grid(row=row, column=0, sticky='w', padx=(0, 8))
            widget = ttk.Entry(connection, textvariable=var)
            widget.grid(row=row, column=1, sticky='ew', pady=2)
            self.option_widgets.append(widget)
            self.connection_widgets.append(widget)
        connection.columnconfigure(1, weight=1)
        ttk.Label(frame, text='保存済みの人物・猫の資料と条件別メモを使います。資料の修正は単条件の生成画面で保存してください。', wraplength=720).pack(anchor='w')
        table = ttk.Frame(frame)
        table.pack(fill='x', pady=8)
        self.tree = ttk.Treeview(table, columns=('target', 'status'), show='headings', height=6, selectmode='browse')
        self.tree.heading('target', text='猫／行動／操作／反応' if cat else '人物／行動／操作')
        self.tree.heading('status', text='進捗')
        self.tree.column('target', width=460 if cat else 270)
        self.tree.column('status', width=160 if cat else 260)
        self.tree.pack(side='left', fill='x', expand=True)
        scrollbar = ttk.Scrollbar(table, orient='vertical', command=self.tree.yview)
        scrollbar.pack(side='right', fill='y')
        self.tree.configure(yscrollcommand=scrollbar.set)
        self.tree.bind('<<TreeviewSelect>>', self.show_result)
        self.preview = ScrolledText(frame, height=6, wrap='word', state='disabled')
        self.preview.pack(fill='both', expand=True)
        controls = ttk.Frame(frame)
        controls.pack(fill='x', pady=8)
        self.start_button = ttk.Button(controls, text='選択条件を連続生成', command=self.start)
        self.start_button.pack(side='left')
        self.stop_button = ttk.Button(controls, text='停止', command=self.stop, state='disabled')
        self.stop_button.pack(side='left', padx=8)
        self.apply_button = ttk.Button(controls, text='選択結果の3案を編集欄へ', command=self.apply, state='disabled')
        self.apply_button.pack(side='left')
        self.open_saved_button=ttk.Button(controls,text='選択条件を個別画面で確認',command=self.open_saved_selected,state='disabled')
        self.open_saved_button.pack(side='left',padx=6)
        ttk.Button(controls,text='前回の続き',command=self.resume).pack(side='left',padx=6)
        ttk.Button(controls, text='閉じる', command=self.window.destroy).pack(side='right')
        navigation=ttk.Frame(frame);navigation.pack(fill='x',pady=(0,4))
        self.skip_button=ttk.Button(navigation,text='選択項目を飛ばす／保存対象外',command=self.skip_selected)
        self.skip_button.pack(side='left')
        self.retry_button=ttk.Button(navigation,text='選択項目を対象へ戻す',command=self.unskip_selected)
        self.retry_button.pack(side='left',padx=6)
        self.draft_button=ttk.Button(navigation,text='完了分をまとめて下書き保存',command=lambda:self.save_completed(False))
        self.draft_button.pack(side='left')
        self.review_button=ttk.Button(navigation,text='完了分をまとめて確認・登録…',command=self.review_completed)
        self.review_button.pack(side='left',padx=6)
        if not drafts:self.draft_button.state(['disabled'])
        if not drafts or not published:self.review_button.state(['disabled'])
        self.status = tk.StringVar(master=self.window, value='対象と接続設定を確認して開始してください。')
        ttk.Label(frame, textvariable=self.status, wraplength=720).pack(anchor='w')
        ttk.Label(frame, text='失敗・停止時は残りを再開できます。完了済み結果は再生成しません。\n'
                  '停止・閉じる後も実行中のサーバー処理は続く場合があります。各条件の応答待ちは最大180秒。', wraplength=720).pack(anchor='w', pady=(6, 0))

    def start(self):
        if self.running:
            return
        try:
            client = InteractionLlmClient(self.url.get(), subject_registry=self.subject_registry)
            model = self.model.get().strip()
            if not model:
                raise ValueError('モデルを入力してください。')
            if not self.jobs:
                jobs = prepare_jobs(self.settings, [k for k, v in self.subjects.items() if v.get()],
                    [k for k, v in self.modes.items() if v.get()], scope=self.scopes[self.scope.get()],
                    has_text=self.has_text if self.only_missing.get() else None, kind=self.kind,
                    selected_reactions=[k for k, v in self.reaction_choices.items() if v.get()] if self.kind == 'cat_response' else None, subject_registry=self.subject_registry, target_cat=self.target_cat, get_entry=self.get_entry, fill_partial=self.partial.get())
                for value, notes in jobs:
                    if self.drafts:notes['draft_snapshot']=self.drafts.get(value)
                    if self.published:notes['published_snapshot']=self.published.get(value)
                self.jobs=jobs
                for value,_ in jobs:
                    self.tree.insert('', 'end', iid=key(value), values=(self.label(value,self.labels), '待機'))
            remaining = [(v, n) for v, n in self.jobs if key(v) not in self.results and key(v) not in self.skipped]
            if not remaining:
                self.status.set(f'生成済み {len(self.results)}件／スキップ {len(self.skipped)}件。飛ばした項目は選択して解除すると再試行できます。')
                return
        except (OSError, ValueError) as exc:
            self.status.set(str(exc) if str(exc).startswith('生成対象がありません') else f'開始できません：{exc}')
            return
        try:self.checkpoint()
        except (OSError,ValueError) as exc:self.status.set(f'計画を保存できません：{exc}');return
        continue_errors=self.continue_errors.get()
        self.cancel.clear()
        self.running = True
        self.show_result()
        self.start_button.state(['disabled'])
        self.stop_button.state(['!disabled'])
        self.apply_button.state(['disabled'])
        for widget in self.option_widgets:
            widget.state(['disabled'])
        # Keep Tk objects out of the worker's lifetime; release discarded UI cycles on the UI thread.
        import gc
        gc.collect()
        cancel,messages,output=self.cancel,self.messages,self.output
        def worker():
            try:
                for value, notes in remaining:
                    if cancel.is_set():
                        break
                    messages.put(('working', value))
                    try:
                        variants, path = client.generate_variants(model, value, notes['subject_notes'],
                            notes['image_notes'], notes['style'], cancel, output=output)
                        if cancel.is_set():
                            break
                        base=notes.get('base_entry')
                        if base:
                            merged=[v['text'] for v in base['variants']]
                            available=[t for t in variants if t not in merged]
                            for i,text in enumerate(merged):
                                if not text:
                                    if not available:raise ValueError('既存案と重複しない生成案が不足しています。')
                                    merged[i]=available.pop(0)
                            variants=merged
                        messages.put(('result', (make_entry(value, variants), path)))
                    except GenerationCancelled:
                        break
                    except Exception as exc:
                        messages.put(('error', (value, str(exc), continue_errors)))
                        if not continue_errors:break
            finally:
                messages.put(('done', None))
        threading.Thread(target=worker, daemon=True).start()
        self.poll()

    @staticmethod
    def label(value, labels=None):
        return ((labels or SUBJECTS)[value['kind']][value['subject_id']] + '／' + MODES[value['mode']]
                + '／' + ACTION_LABELS.get(value['action'], '動作共通')
                + ('／' + REACTIONS[value['reaction']] if value['kind'] == 'cat_response' else '／猫共通' if value.get('target_cat')=='*' else '／'+(labels or SUBJECTS)['cat_response'].get(value.get('target_cat','cat-mike'),'指定猫')+'専用'))

    def poll(self):
        self.timer = None
        while not self.messages.empty():
            kind, payload = self.messages.get_nowait()
            if kind == 'working':
                self.tree.set(key(payload), 'status', '生成中')
                self.status.set(f'{len(self.results)}/{len(self.jobs)}条件完了　' + self.label(payload,self.labels) + 'を生成中…')
            elif kind == 'result':
                entry, path = payload
                ident = key(entry['condition'])
                self.results[ident] = (entry, path)
                self.errors.pop(ident,None)
                try:self.checkpoint()
                except OSError as exc:self.status.set(f'結果の再開記録を保存できません：{exc}')
                self.tree.set(ident, 'status', '完了・未取り込み')
                self.tree.selection_set(ident)
                self.show_result()
            elif kind == 'error':
                value, error, skipped = payload
                ident=key(value);self.errors[ident]=error
                if skipped:self.skipped.add(ident)
                self.tree.set(ident, 'status', '失敗・スキップ' if skipped else '失敗・再開待ち')
                self.status.set(('失敗した項目を飛ばしました：' if skipped else 'この条件で失敗して停止しました：')+error)
                self.persist()
            elif kind == 'done':
                self.running = False
                self.stop_button.state(['disabled'])
                self.start_button.configure(text='未完了の条件を再開')
                self.start_button.state(['!disabled'])
                for widget in self.connection_widgets:
                    widget.state(['!disabled'])
                for ident in self.tree.get_children():
                    if self.tree.set(ident, 'status') == '生成中':
                        self.tree.set(ident, 'status', '停止・再開待ち')
                if len(self.results)+len(self.skipped) == len(self.jobs):
                    self.status.set(f'生成済み {len(self.results)}件／スキップ {len(self.skipped)}件。完了分をまとめて保存・確認登録できます。')
                elif self.cancel.is_set():
                    self.status.set(f'{len(self.results)}/{len(self.jobs)}条件完了で停止しました。完了済みの結果は残っています。')
                self.show_result()
        if self.running:
            self.timer = self.window.after(100, self.poll)

    def show_result(self, event=None):
        selected = self.tree.selection()
        result = self.results.get(selected[0]) if selected else None
        text = ''
        if result:
            entry, path = result
            text = self.label(entry['condition'],self.labels) + '\n\n' + '\n\n'.join(
                f"案{row['slot']}：{row['text']}" for row in entry['variants']) + '\n\n生成記録：' + str(path)
        self.preview.configure(state='normal')
        self.preview.delete('1.0', 'end')
        self.preview.insert('1.0', text)
        self.preview.configure(state='disabled')
        if selected and selected[0] in self.errors:
            self.preview.configure(state='normal');self.preview.insert('end','\n失敗理由：'+self.errors[selected[0]]);self.preview.configure(state='disabled')
        self.apply_button.state(['!disabled'] if result and not self.running else ['disabled'])
        for widget,enabled in ((self.open_saved_button,bool(selected) and self.on_open_saved is not None),
                (self.skip_button, bool(selected)),
                (self.retry_button,bool(selected) and selected[0] in self.skipped|self.excluded),
                (self.draft_button,bool(self.completed_entries()) and self.drafts is not None),
                (self.review_button,bool(self.completed_entries()) and self.drafts is not None and self.published is not None)):
            widget.state(['!disabled'] if enabled and not self.running else ['disabled'])

    def open_saved_selected(self):
        selected=self.tree.selection()
        if self.running or not selected or not self.on_open_saved:return False
        value=next((v for v,_ in self.jobs if key(v)==selected[0]),None)
        if value is None:return False
        try:
            opened=self.on_open_saved(copy.deepcopy(value))
            self.status.set('個別画面で保存済みの文章を開きました。' if opened else '個別画面へ移動できません。保存状況と個別画面の案内を確認してください。')
            return opened
        except (OSError,ValueError) as exc:
            self.status.set(f'個別画面を開けません：{exc}');return False

    def apply(self):
        selected = self.tree.selection()
        if self.running or not selected or selected[0] not in self.results:
            return
        entry, _ = self.results[selected[0]]
        notes=next(n for v,n in self.jobs if key(v)==selected[0])
        if notes.get('base_entry') and self.get_entry and self.get_entry(entry['condition'])!=notes['base_entry']:
            self.status.set('既存の案が生成開始後に変更されました。空欄補完の取り込みを中止しました。');return
        if self.on_result(copy.deepcopy(entry)):
            self.tree.set(selected[0], 'status', '編集欄へ取り込み済み')
            self.status.set('編集欄へ取り込みました。下書き保存・内容確認・登録は編集画面で行ってください。')
        else:
            self.status.set('取り込みを中止しました。現在の編集内容を確認してください。結果は残っています。')

    def checkpoint(self):
        output=Path(self.output);output.mkdir(parents=True,exist_ok=True)
        path=output/('batch-'+self.kind+'.json')
        data=dict(version=1,kind=self.kind,jobs=self.jobs,skipped=sorted(self.skipped),excluded=sorted(self.excluded),errors=self.errors,results=[dict(entry=e,path=str(p)) for e,p in self.results.values()])
        temp=path.with_suffix('.tmp')
        try:
            temp.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');temp.replace(path)
        finally:temp.unlink(missing_ok=True)

    def resume(self):
        if self.running:return
        try:
            from interaction_texts import validate_condition, validate_entry
            data=json.loads((Path(self.output)/('batch-'+self.kind+'.json')).read_text(encoding='utf-8'))
            if data['version']!=1 or data['kind']!=self.kind or len(data['jobs'])>5000:raise ValueError('再開記録が不正です。')
            jobs=data['jobs'];identities=set()
            for value,notes in jobs:
                validate_condition(value,subjects=self.labels)
                if value['kind']!=self.kind or key(value) in identities:raise ValueError('再開条件が不正です。')
                identities.add(key(value));validate_inputs(notes['subject_notes'],notes['image_notes'],notes['style'])
            results={}
            for result in data['results']:
                entry=result['entry'];validate_entry(entry)
                ident=key(entry['condition'])
                if ident not in identities or ident in results:raise ValueError('再開結果が不正です。')
                results[ident]=(entry,Path(result['path']))
            skipped=set(data.get('skipped',[]));errors=data.get('errors',{})
            if not skipped<=identities or skipped.intersection(results) or not isinstance(errors,dict) or not set(errors)<=identities or any(not isinstance(v,str) for v in errors.values()):raise ValueError('スキップ・失敗記録が不正です。')
            excluded=set(data.get('excluded',[]))
            if not excluded<=set(results):raise ValueError('保存対象外の記録が不正です。')
            self.jobs,self.results,self.skipped,self.errors,self.excluded=jobs,results,skipped,errors,excluded
            self.tree.delete(*self.tree.get_children())
            for value,_ in jobs:self.tree.insert('','end',iid=key(value),values=(self.label(value,self.labels),'生成済み・保存対象外' if key(value) in excluded else '完了・未取り込み' if key(value) in results else 'スキップ' if key(value) in skipped else '再開待ち'))
            for widget in self.option_widgets:widget.state(['disabled'])
            for widget in self.connection_widgets:widget.state(['!disabled'])
            self.status.set(f'{len(results)}/{len(jobs)}条件の記録を復元しました。未完了の条件を再開できます。')
            if results:self.tree.selection_set(next(iter(results)))
            self.show_result()
        except (OSError,ValueError,KeyError,TypeError) as exc:self.status.set(f'再開できません：{exc}')

    def persist(self):
        try:self.checkpoint();return True
        except (OSError,ValueError) as exc:
            self.status.set(f'再開記録を保存できません：{exc}');return False

    def skip_selected(self):
        selected=self.tree.selection()
        if self.running or not selected:return
        if selected[0] in self.results:
            self.excluded.add(selected[0]);self.tree.set(selected[0],'status','生成済み・保存対象外')
            self.persist();self.show_result();self.status.set('この結果をまとめ保存・登録の対象から外しました。');return
        self.skipped.add(selected[0]);self.tree.set(selected[0],'status','スキップ')
        self.show_result()
        if self.persist():self.start()

    def unskip_selected(self):
        selected=self.tree.selection()
        if self.running or not selected:return
        self.skipped.discard(selected[0]);self.excluded.discard(selected[0]);self.tree.set(selected[0],'status','完了・未取り込み' if selected[0] in self.results else '再試行待ち')
        self.persist();self.show_result()
        self.status.set('スキップを解除しました。「未完了の条件を再開」で再試行できます。')

    def completed_entries(self):
        return [copy.deepcopy(self.results[key(value)][0]) for value,_ in self.jobs if key(value) in self.results and key(value) not in self.excluded]

    def review_completed(self):
        import tkinter as tk
        from tkinter import ttk
        from tkinter.scrolledtext import ScrolledText
        if self.running or not self.completed_entries() or not self.published:return
        existing=getattr(self,'review_window',None)
        if existing is not None and existing.winfo_exists():existing.destroy()
        entries=self.completed_entries()
        self.review_window=tk.Toplevel(self.window);self.review_window.title('完了した文章をまとめて確認・登録')
        self.review_window.geometry('900x700');self.review_window.minsize(760,550)
        frame=ttk.Frame(self.review_window,padding=12);frame.pack(fill='both',expand=True)
        ttk.Label(frame,text=f'{len(entries)}条件を確認してください。失敗・スキップ・保存対象外の項目は含みません。').pack(anchor='w')
        text=ScrolledText(frame,wrap='word',height=18);text.pack(fill='both',expand=True,pady=8)
        text.insert('1.0','\n\n'.join(self.label(e['condition'],self.labels)+'\n'+('猫範囲：'+e['condition'].get('target_cat','従来のミケ専用')+'\n' if self.kind=='speech' else '')+'\n'.join(f"案{v['slot']}：{v['text']}" for v in e['variants']) for e in entries))
        text.configure(state='disabled')
        controls=ttk.Frame(frame);controls.pack(fill='x')
        ttk.Button(controls,text=f'確認した{len(entries)}条件をまとめて登録',command=lambda:self.save_completed(True,entries)).pack(side='left')
        ttk.Button(controls,text='閉じる',command=self.review_window.destroy).pack(side='right')

    def save_completed(self, publish=False, reviewed=None):
        if self.running or not self.drafts or publish and not self.published:return False
        entries=copy.deepcopy(reviewed) if reviewed is not None else self.completed_entries()
        if not entries:return False
        if publish and reviewed is None:
            self.status.set('先に「完了分をまとめて確認・登録」を開いて確認してください。');return False
        try:
            if self.before_save and not self.before_save(entries):raise ValueError('対象条件に未保存の編集があります。編集画面で保存・確認してから実行してください。')
            current={key(e['condition']):e for e in self.completed_entries()}
            if any(current.get(key(e['condition']))!=e for e in entries):raise ValueError('確認後に生成結果が変わりました。もう一度確認してください。')
            notes={key(v):n for v,n in self.jobs}
            def snapshots(store,field):
                result={}
                for e in entries:
                    ident=key(e['condition']);actual=store.get(e['condition'])
                    expected=notes[ident].get(field)
                    if actual!=expected and actual!=e:raise ValueError('生成開始後に対象の文章が更新されています。上書きせず中止しました。')
                    result[ident]=actual
                return result
            draft_expected=snapshots(self.drafts,'draft_snapshot')
            published_expected=snapshots(self.published,'published_snapshot') if publish else None
            for e in entries:
                self.drafts.validate_for_save(e)
                if publish:self.published.validate_for_save(e)
            self.drafts.save_many(entries,expected=draft_expected)
            for e in entries:notes[key(e['condition'])]['draft_snapshot']=copy.deepcopy(e)
            if publish:
                self.published.save_many(entries,expected=published_expected)
                for e in entries:notes[key(e['condition'])]['published_snapshot']=copy.deepcopy(e)
            for e in entries:self.tree.set(key(e['condition']),'status','登録済み' if publish else '下書き保存済み')
            if self.after_save:self.after_save(entries)
            persisted=self.persist()
            self.status.set(f'{len(entries)}条件をまとめて'+('登録しました。Unityへ反映するには書き出して取り込んでください。' if publish else '下書き保存しました。登録は「まとめて確認・登録」から行えます。'))
            if not persisted:self.status.set(self.status.get()+' 再開記録の更新には失敗しました。')
            if publish and getattr(self,'review_window',None) is not None and self.review_window.winfo_exists():self.review_window.destroy()
            return True
        except (OSError,ValueError) as exc:
            self.status.set(f'まとめ保存できません：{exc}');return False

    def stop(self):
        if self.running:
            self.cancel.set()
            self.stop_button.state(['disabled'])
            self.status.set('停止を要求しました。応答待ちが終わるまで再開できません。')

    def destroyed(self, event):
        if event.widget == self.window:
            self.cancel.set()
            if self.timer is not None:
                self.window.after_cancel(self.timer)
                self.timer = None
