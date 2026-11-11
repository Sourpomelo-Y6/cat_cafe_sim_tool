"""Optional stories with shared definitions, legacy Mike scope and atomic local editing."""
import copy
import hashlib
import json
import os
import re
import tempfile
import uuid
from pathlib import Path

EVENT_ID = 'optional.mike.afternoon'
FIELDS = ('event_id', 'cat_id', 'content_cat_id', 'min_day', 'min_affinity', 'title',
          'accept_caption', 'decline_caption', 'introduction', 'accepted', 'declined')
BRANCHES = ('introduction', 'accepted', 'declined')
DEFAULT_LINE_COUNTS = (3, 2, 2)
MAX_BRANCH_LINES = 20
DEFAULT_DRAFT = Path(__file__).resolve().parent / 'user_data' / 'optional_event_draft.json'


def _unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError('JSONの項目が重複しています。')
        result[key] = value
    return result


def read_json(text):
    return json.loads(text, object_pairs_hook=_unique_object)


def default_event():
    return read_json((Path(__file__).resolve().parent / 'defaults' / 'optional_event.json').read_text(encoding='utf-8'))


def _text(value, maximum, *, empty=False, single_line=False):
    if not isinstance(value, str) or (not empty and not value.strip()):
        raise ValueError('タイトル・本文・選択肢を入力してください。')
    if single_line and ('\n' in value or '\t' in value):
        raise ValueError('タイトル・話者・選択肢には改行を入れないでください。')
    if len(value.encode('utf-16-le')) // 2 > maximum or any(ord(c) < 32 and c not in '\n\t' for c in value) or '<' in value or '>' in value:
        raise ValueError(f'文章は{maximum}文字以内、制御文字・<>なしで入力してください。')
    return value


def revision(event):
    identity = [event[k] for k in ('event_id','cat_id','content_cat_id')] if 'target_kind' not in event else ['2',event['event_id'],event['target_kind'],event['content_cat_id'],event.get('cat_id','')]
    values = identity + [str(event[key]) for key in ('min_day','min_affinity','title','accept_caption','decline_caption')]
    counts = tuple(len(event[branch]) for branch in BRANCHES)
    if counts != DEFAULT_LINE_COUNTS:
        # Include the branch boundaries while preserving all existing revision IDs.
        values.extend(('branch_counts', *(str(count) for count in counts)))
    for branch in BRANCHES:
        for line in event[branch]:
            values.extend((line['speaker'], line['text']))
    canonical = ''.join(f'{len(value.encode("utf-16-le")) // 2}:{value}' for value in values)
    return hashlib.sha256(canonical.encode('utf-8')).hexdigest()


def validate_event(event, *, require_revision=True):
    if not isinstance(event, dict):
        raise ValueError('イベント定義の項目が不正です。')
    fields = set(FIELDS) if 'target_kind' not in event else (set(FIELDS) - {'cat_id'}) | {'target_kind'} | ({'cat_id'} if event.get('target_kind') == 'individual' else set())
    if set(event) != fields | ({'revision'} if require_revision else set()):
        raise ValueError('イベント定義の項目が不正です。')
    clean = copy.deepcopy(event)
    legacy = 'target_kind' not in clean
    if not legacy and (clean['target_kind'] not in ('content','individual') or not isinstance(clean['event_id'],str) or not re.fullmatch(r'optional\.[a-z][a-z0-9_.-]{0,86}',clean['event_id']) or not isinstance(clean['content_cat_id'],str) or not re.fullmatch('[a-z][a-z0-9_-]{0,63}',clean['content_cat_id'])):
        raise ValueError('イベントのID・対象が不正です。')
    if (legacy or clean['target_kind']=='individual') and (clean['event_id'] != EVENT_ID or clean['cat_id'] != 'cat-mike' or clean['content_cat_id'] != 'playtest-mike'):
        raise ValueError('個体専用の定義は旧ミケ互換だけです。')
    for key, low, high in (('min_day', 1, 9999), ('min_affinity', 0, 100)):
        if type(clean[key]) is not int or not low <= clean[key] <= high:
            raise ValueError(('開始日数' if key == 'min_day' else '開始好感度') + 'の範囲が不正です。')
    for key, maximum in (('title', 30), ('accept_caption', 24), ('decline_caption', 24)):
        _text(clean[key], maximum, single_line=True)
    for branch, count in zip(BRANCHES, DEFAULT_LINE_COUNTS):
        lines = clean[branch]
        if not isinstance(lines, list) or not 1 <= len(lines) <= MAX_BRANCH_LINES:
            raise ValueError(f'導入と各結果はそれぞれ1〜{MAX_BRANCH_LINES}行で入力してください。')
        if legacy and len(lines) != count:
            raise ValueError('旧形式では導入3行・各結果2行が必要です。')
        for line in clean[branch]:
            if not isinstance(line, dict) or set(line) != {'speaker', 'text'}:
                raise ValueError('会話行の形式が不正です。')
            _text(line['speaker'], 7, empty=True, single_line=True)
            _text(line['text'], 1000)
    digest = revision(clean)
    if require_revision and clean['revision'] != digest:
        raise ValueError('本文と内容の版が一致しません。Toolから出力し直してください。')
    clean['revision'] = digest
    return clean


def validate_pack(data, catalog=None):
    if not isinstance(data, dict) or set(data) != {'version', 'pack_id', 'events'} or type(data['version']) is not int or data['version'] not in (1,2):
        raise ValueError('イベントパックの形式が不正です。')
    import re
    if not isinstance(data['pack_id'], str) or not re.fullmatch('[a-f0-9]{32}', data['pack_id']):
        raise ValueError('パックIDが不正です。')
    if not isinstance(data['events'], list) or len(data['events']) > (1 if data['version']==1 else 64):
        raise ValueError('イベントは旧v1で1件、v2で64件までです。')
    events = [validate_event(row) for row in data['events']]
    if any(('target_kind' in e) != (data['version']==2) for e in events) or len({e['event_id'] for e in events}) != len(events) or len({e['content_cat_id'] for e in events}) != len(events):
        raise ValueError('イベントの版・対象・IDが重複または不正です。')
    if catalog is not None and any(row['content_cat_id'] not in catalog for row in events):
        raise ValueError('出力対象の画像IDが素材一覧にありません。')
    return dict(version=data['version'], pack_id=data['pack_id'], events=events)


MAX_JSON = 1000000

def definition_text(data):
    text = json.dumps(data, ensure_ascii=False, indent=2) + '\n'
    if len(text.encode('utf-16-le')) // 2 > MAX_JSON:
        raise ValueError('イベントJSONは1,000,000 UTF16文字以内にしてください。')
    return text


def read_pack(text, catalog=None):
    if len(text.encode('utf-16-le')) // 2 > MAX_JSON:
        raise ValueError('イベントJSONが大きすぎます。')
    data = validate_pack(read_json(text), catalog)
    if data['version'] == 1 and len(text.encode('utf-16-le')) // 2 > 100000:
        raise ValueError('旧イベントJSONが大きすぎます。')
    return data


def normalize_event(event):
    clean = validate_event(event)
    if 'target_kind' not in clean:
        clean['target_kind'] = 'individual'
    clean.pop('revision', None)
    return validate_event(clean, require_revision=False)


def blank_event(content_id):
    if not isinstance(content_id, str) or not re.fullmatch('[a-z][a-z0-9_-]{0,63}',content_id):
        raise ValueError('画像IDが不正です。')
    return dict(event_id='optional.'+content_id+'.story',target_kind='content',content_cat_id=content_id,
                min_day=2,min_affinity=20,title='',accept_caption='',decline_caption='',
                **{branch: [dict(speaker='', text='') for _ in range(count)]
                   for branch, count in zip(BRANCHES, DEFAULT_LINE_COUNTS)})


class OptionalEventStore:
    def __init__(self, path):
        self.path = Path(path)

    def read_all(self):
        if not self.path.exists():
            return dict(version=2,entries=[dict(enabled=True,event=normalize_event(validate_event(default_event(),require_revision=False)))])
        if self.path.stat().st_size > 4000003:
            raise ValueError('イベント登録が大きすぎます。')
        text = self.path.read_text(encoding='utf-8-sig')
        if len(text.encode('utf-16-le')) // 2 > MAX_JSON:
            raise ValueError('イベント登録が大きすぎます。')
        data = read_json(text)
        if not isinstance(data,dict) or type(data.get('version')) is not int:
            raise ValueError('イベント登録が不正です。')
        if data['version'] == 1:
            if set(data) != {'version','enabled','event'} or type(data['enabled']) is not bool:
                raise ValueError('旧イベント登録が不正です。')
            event = validate_event(data['event'])
            if 'target_kind' in event:
                raise ValueError('旧イベント登録の版が不正です。')
            return dict(version=2,entries=[dict(enabled=data['enabled'],event=normalize_event(event))])
        return self._validate_all(data)

    @staticmethod
    def _validate_all(data):
        if not isinstance(data,dict) or set(data) != {'version','entries'} or type(data['version']) is not int or data['version'] != 2 or not isinstance(data['entries'],list) or len(data['entries']) > 64:
            raise ValueError('イベント登録一覧が不正です。')
        rows=[]
        for row in data['entries']:
            if not isinstance(row,dict) or set(row) != {'enabled','event'} or type(row['enabled']) is not bool:
                raise ValueError('イベント登録行が不正です。')
            event=validate_event(row['event'])
            if 'target_kind' not in event:
                raise ValueError('イベント登録行の版が不正です。')
            rows.append(dict(enabled=row['enabled'],event=event))
        validate_pack(dict(version=2,pack_id='0'*32,events=[r['event'] for r in rows]))
        return dict(version=2,entries=copy.deepcopy(rows))

    def read(self, content_id='playtest-mike'):
        row = next((r for r in self.read_all()['entries'] if r['event']['content_cat_id']==content_id),None)
        return dict(version=1,enabled=row['enabled'],event=row['event']) if row else dict(version=1,enabled=True,event=blank_event(content_id))

    def save_all(self, data):
        self.read_all()
        clean=self._validate_all(data)
        text=definition_text(clean)
        self.path.parent.mkdir(parents=True,exist_ok=True)
        fd,name=tempfile.mkstemp(prefix=self.path.name+'.',suffix='.tmp',dir=self.path.parent)
        try:
            with os.fdopen(fd,'w',encoding='utf-8',newline='\n') as stream:
                stream.write(text);stream.flush();os.fsync(stream.fileno())
            if self.path.exists():
                raw=self.path.read_bytes()
                if read_json(raw.decode('utf-8-sig')).get('version')==1:
                    backup=self.path.with_name(self.path.stem+'.v1'+self.path.suffix)
                    if backup.exists() and backup.read_bytes()!=raw:
                        raise ValueError('旧登録の退避内容が異なります。元を保持しました。')
                    if not backup.exists():
                        created=False
                        try:
                            with backup.open('xb') as stream:
                                created=True;stream.write(raw);stream.flush();os.fsync(stream.fileno())
                        except Exception:
                            if created:backup.unlink()
                            raise
            os.replace(name,self.path)
        finally:
            if os.path.exists(name):os.unlink(name)
        return clean

    def save(self,data):
        self.read_all()  # Do not silently repair corrupt existing data.
        if not isinstance(data,dict) or set(data)!={'version','enabled','event'} or type(data['version']) is not int or data['version']!=1 or type(data['enabled']) is not bool:
            raise ValueError('イベント編集データが不正です。')
        event={k:v for k,v in data['event'].items() if k!='revision'}
        event=validate_event(event,require_revision=False)
        if 'target_kind' not in event:event=normalize_event(event)
        rows=[r for r in self.read_all()['entries'] if r['event']['content_cat_id']!=event['content_cat_id']]
        rows.append(dict(enabled=data['enabled'],event=event))
        self.save_all(dict(version=2,entries=rows))
        return dict(version=1,enabled=data['enabled'],event=event)

    def export_data(self,catalog,pack_id=None):
        data=self.read_all()
        rows=[r['event'] for r in data['entries'] if r['enabled']]
        if not self.path.exists() and 'playtest-mike' not in catalog:rows=[]
        result=validate_pack(dict(version=2,pack_id=pack_id or uuid.uuid4().hex,events=rows),catalog)
        definition_text(result)
        return result

    def import_pack(self,path):
        data=read_pack(Path(path).read_text(encoding='utf-8-sig'))
        rows=[dict(enabled=True,event=normalize_event(e)) for e in data['events']]
        if not rows:rows=[dict(enabled=False,event=r['event']) for r in self.read_all()['entries']]
        self.save_all(dict(version=2,entries=rows))
        return self.read()


def export_event_pack(store,catalog,destination):
    data=store.export_data(catalog)
    destination=Path(destination).resolve()
    folder=destination/('CatCafeEvents-'+data['pack_id'])
    folder.mkdir(parents=True,exist_ok=False)
    try:
        (folder/'optional_events.json').write_text(definition_text(data),encoding='utf-8')
    except Exception:
        import shutil
        if folder.resolve().parent==destination and folder.name=='CatCafeEvents-'+data['pack_id']:shutil.rmtree(folder)
        raise
    return folder
