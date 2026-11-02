"""One authored Mike story; immutable content revisions and atomic local editing."""
import copy
import hashlib
import json
import os
import tempfile
import uuid
from pathlib import Path

EVENT_ID = 'optional.mike.afternoon'
FIELDS = ('event_id', 'cat_id', 'content_cat_id', 'min_day', 'min_affinity', 'title',
          'accept_caption', 'decline_caption', 'introduction', 'accepted', 'declined')
BRANCHES = ('introduction', 'accepted', 'declined')
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
    values = [str(event[key]) for key in FIELDS[:8]]
    for branch in BRANCHES:
        for line in event[branch]:
            values.extend((line['speaker'], line['text']))
    canonical = ''.join(f'{len(value.encode("utf-16-le")) // 2}:{value}' for value in values)
    return hashlib.sha256(canonical.encode('utf-8')).hexdigest()


def validate_event(event, *, require_revision=True):
    if not isinstance(event, dict) or set(event) != set(FIELDS) | ({'revision'} if require_revision else set()):
        raise ValueError('イベント定義の項目が不正です。')
    clean = copy.deepcopy(event)
    if clean['event_id'] != EVENT_ID or clean['cat_id'] != 'cat-mike' or clean['content_cat_id'] != 'playtest-mike':
        raise ValueError('この版はミケの固定イベント1件に対応しています。')
    for key, low, high in (('min_day', 1, 9999), ('min_affinity', 0, 100)):
        if type(clean[key]) is not int or not low <= clean[key] <= high:
            raise ValueError(('開始日数' if key == 'min_day' else '開始好感度') + 'の範囲が不正です。')
    for key, maximum in (('title', 30), ('accept_caption', 24), ('decline_caption', 24)):
        _text(clean[key], maximum, single_line=True)
    for branch, count in zip(BRANCHES, (3, 2, 2)):
        if not isinstance(clean[branch], list) or len(clean[branch]) != count:
            raise ValueError('導入3行・各結果2行が必要です。')
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
    if not isinstance(data, dict) or set(data) != {'version', 'pack_id', 'events'} or type(data['version']) is not int or data['version'] != 1:
        raise ValueError('イベントパックの形式が不正です。')
    import re
    if not isinstance(data['pack_id'], str) or not re.fullmatch('[a-f0-9]{32}', data['pack_id']):
        raise ValueError('パックIDが不正です。')
    if not isinstance(data['events'], list) or len(data['events']) > 1:
        raise ValueError('ミケのイベントは最大1件です。')
    events = [validate_event(row) for row in data['events']]
    if catalog is not None and any(row['content_cat_id'] not in catalog for row in events):
        raise ValueError('出力先の素材一覧にミケがありません。')
    return dict(version=1, pack_id=data['pack_id'], events=events)


class OptionalEventStore:
    def __init__(self, path):
        self.path = Path(path)

    def read(self):
        if not self.path.exists():
            return dict(version=1, enabled=True, event=validate_event(default_event(), require_revision=False))
        data = read_json(self.path.read_text(encoding='utf-8-sig'))
        if not isinstance(data, dict) or set(data) != {'version', 'enabled', 'event'} or type(data['version']) is not int or data['version'] != 1 or type(data['enabled']) is not bool:
            raise ValueError('イベント保存データが不正です。上書きしていません。')
        return dict(version=1, enabled=data['enabled'], event=validate_event(data['event']))

    def save(self, data):
        if not isinstance(data, dict) or set(data) != {'version', 'enabled', 'event'} or type(data['version']) is not int or data['version'] != 1 or type(data['enabled']) is not bool:
            raise ValueError('イベント保存データの形式が不正です。')
        if not isinstance(data['event'], dict):
            raise ValueError('イベント保存データの形式が不正です。')
        event = {key: value for key, value in data['event'].items() if key != 'revision'}
        clean = dict(version=1, enabled=data['enabled'], event=validate_event(event, require_revision=False))
        self.path.parent.mkdir(parents=True, exist_ok=True)
        fd, name = tempfile.mkstemp(prefix=self.path.name + '.', suffix='.tmp', dir=self.path.parent)
        try:
            with os.fdopen(fd, 'w', encoding='utf-8', newline='\n') as stream:
                json.dump(clean, stream, ensure_ascii=False, indent=2)
                stream.write('\n')
            os.replace(name, self.path)
        finally:
            if os.path.exists(name):
                os.unlink(name)
        return clean

    def export_data(self, catalog, pack_id=None):
        data = self.read()
        events = [data['event']] if data['enabled'] else []
        # Existing tiny/non-Mike export callers need no new content or user-data file.
        if not self.path.exists() and 'playtest-mike' not in catalog:
            events = []
        return validate_pack(dict(version=1, pack_id=pack_id or uuid.uuid4().hex, events=events), catalog)

    def import_pack(self, path):
        data = validate_pack(read_json(Path(path).read_text(encoding='utf-8-sig')))
        return self.save(dict(version=1, enabled=bool(data['events']), event=data['events'][0] if data['events'] else self.read()['event']))


def export_event_pack(store, catalog, destination):
    data = store.export_data(catalog)
    folder = Path(destination) / ('CatCafeEvents-' + data['pack_id'])
    folder.mkdir(parents=True, exist_ok=False)
    (folder / 'optional_events.json').write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    return folder
