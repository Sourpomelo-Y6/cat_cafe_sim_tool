"""交流文の制作対象台帳。ゲームや採用済み本文は書き換えない。"""
import copy
import json
import re
import uuid
from pathlib import Path

ID = re.compile(r'^[a-z][a-z0-9_-]{0,99}$')
FIELDS = {'id', 'kind', 'name', 'game_id', 'content_id', 'notes', 'call', 'enabled'}


def identifier(value):
    return isinstance(value, str) and ID.fullmatch(value) is not None


def expand(text, cat, call='{cat}'):
    if not isinstance(text, str) or not isinstance(cat, str) or not isinstance(call, str):
        raise ValueError('名前の差し込みが不正です。')
    if '{' in call.replace('{cat}', '') or '}' in call.replace('{cat}', ''):
        raise ValueError('呼び名には {cat} だけを使えます。')
    result = text.replace('{cat_call}', call.replace('{cat}', cat)).replace('{cat}', cat)
    if '{' in result or '}' in result or len(result)>1200:
        raise ValueError('不明な差し込み項目、または展開後の文章が長すぎます。')
    return result


class SubjectRegistry:
    def __init__(self, path, *, settings_path=None):
        self.path = Path(path)
        self.settings_path = settings_path

    def defaults(self):
        from interaction_llm import DEFAULT_SETTINGS
        source = Path(self.settings_path) if self.settings_path else DEFAULT_SETTINGS
        notes = {}
        if source.exists():
            notes = json.loads(source.read_text(encoding='utf-8')).get('profiles', {})
        image = 'playtest-mike'
        legacy = self.path.parent/'interaction_image_bindings.json'
        if legacy.exists():
            binding=json.loads(legacy.read_text(encoding='utf-8'))
            if binding.get('version')!=1 or binding.get('cat_id')!='cat-mike':
                raise ValueError('旧画像ID対応を確認してください。')
            image=binding['content_cat_id']
        rows=[]
        for id, kind, name, game, content, call in (
                ('player','speech','プレイヤー','player','','{cat}'),
                ('guest-1','speech','佐藤さん','guest-1','','{cat}ちゃん'),
                ('cat-mike','cat_response','ミケ','cat-mike',image,'{cat}')):
            rows.append(dict(id=id,kind=kind,name=name,game_id=game,content_id=content,
                             notes=notes.get(kind+'/'+id,''),call=call,enabled=True))
        return dict(version=1,subjects=rows)

    @staticmethod
    def validate(data):
        if not isinstance(data,dict) or set(data)!={'version','subjects'} or type(data['version']) is not int or data['version']!=1 or not isinstance(data['subjects'],list) or len(data['subjects'])>500:
            raise ValueError('対象台帳の形式が不正です。')
        ids,bindings=set(),set()
        for row in data['subjects']:
            if not isinstance(row,dict) or set(row)!=FIELDS or not identifier(row['id']) or row['id'] in ids or row['kind'] not in ('speech','cat_response'):
                raise ValueError('対象ID・種類が不正または重複しています。')
            ids.add(row['id'])
            for name,limit in [('name',100),('notes',4000),('call',100),('game_id',100),('content_id',100)]:
                if not isinstance(row[name],str) or len(row[name])>limit or row[name]!=row[name].strip() or any(ord(c)<32 and c not in '\n\t' for c in row[name]):
                    raise ValueError('対象の名前・資料・対応の形式が不正です。')
            if any(c in row['name'] for c in '{}') or not row['call'] or not row['name'] or type(row['enabled']) is not bool or any(row[k] and not identifier(row[k]) for k in ('game_id','content_id')):
                raise ValueError('名前・利用状態・対応IDを確認してください。')
            if row['kind']=='speech' and row['content_id']:
                raise ValueError('人物には猫の画像IDを設定しません。')
            expand('{cat_call}','対象猫',row['call'])
            if row['game_id']:
                binding=(row['kind'],row['game_id'])
                if binding in bindings:raise ValueError('同じゲームIDに複数の対象を対応させられません。')
                bindings.add(binding)
        player=next((r for r in data['subjects'] if r['id']=='player'),None)
        if not player or player['kind']!='speech' or player['game_id']!='player' or not player['enabled']:
            raise ValueError('プレイヤーは有効な共通対象として残してください。')

    def read(self):
        data=json.loads(self.path.read_text(encoding='utf-8')) if self.path.exists() else self.defaults()
        self.validate(data)
        return data

    def labels(self, *, include_disabled=False):
        result={'speech':{},'cat_response':{}}
        rows=self.read()['subjects']
        for row in rows:
            if row['enabled'] or include_disabled:
                duplicate=sum(r['kind']==row['kind'] and r['name']==row['name'] for r in rows)>1
                result[row['kind']][row['id']]=row['name']+('（'+row['id']+'）' if duplicate else '')
        return result

    def get(self,id):
        return next((r for r in self.read()['subjects'] if r['id']==id),None)

    def save(self,row,*,expected=None):
        data=self.read()
        if expected is not None and data!=expected:raise ValueError('対象台帳が更新されています。再読込してください。')
        old=next((r for r in data['subjects'] if r['id']==row['id']),None)
        if old and old['kind']!=row['kind']:raise ValueError('登録済み対象の種類は変更できません。')
        data['subjects']=[r for r in data['subjects'] if r['id']!=row['id']]+[copy.deepcopy(row)]
        self.validate(data)
        self.path.parent.mkdir(parents=True,exist_ok=True)
        temp=self.path.with_name(self.path.stem+'-'+uuid.uuid4().hex+'.tmp')
        try:
            temp.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');temp.replace(self.path)
        finally:temp.unlink(missing_ok=True)

    def image_catalog(self,base):
        result=dict(base)
        for r in self.read()['subjects']:
            if r['enabled'] and r['kind']=='cat_response' and r['content_id']:result[r['content_id']]=r['name']
        return result


def game_subjects():
    """既存ゲームIDの参照候補。ゲームデータは更新しない。"""
    root=Path(__file__).parent/'unity_preview_template/Resources/CatCafeStandalone'
    rules=json.loads((root/'rules.json').read_text(encoding='utf-8'))
    display=json.loads((root/'presentation.json').read_text(encoding='utf-8'))
    content={r['id']:r['label'] for r in display['content_ids']}
    cats={r['id']:dict(game_id=r['id'],name=r['name'],content_id=content.get(r['id'],'')) for r in rules['cats']}
    # Named/intake cats may be defined outside the initial rules.cats array.
    for r in display['profiles']:
        if r['id'].startswith('cat-') and r['id'] in content:
            cats.setdefault(r['id'],dict(game_id=r['id'],name=r['name'],content_id=content[r['id']]))
    for profile in display['profiles']:
        if profile['id'] in cats:
            cats[profile['id']]['notes']='性格：'+profile.get('personality','未設定')+'\n外見資料：'+profile.get('appearance','未設定')
    people={f'guest-{i+1}':dict(game_id=f'guest-{i+1}',name=name+'さん',content_id='') for i,name in enumerate(display['names'])}
    return {'speech':people,'cat_response':cats}
