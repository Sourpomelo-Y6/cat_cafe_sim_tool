"""日常交流文の条件・最大3案を、既存の紹介文と独立して保存する。"""
import copy
import json
import uuid
from pathlib import Path

from interaction_scenes import MODE_ACTIONS

DEFAULT_DRAFT_PATH = Path(__file__).resolve().parent / 'user_data' / 'interaction_text_drafts.json'
KINDS = {'speech': '人物のセリフ', 'cat_response': '猫の反応'}
SUBJECTS = {'speech': {'player': 'プレイヤー', 'guest-1': '佐藤さん'},
            'cat_response': {'cat-mike': 'ミケ'}}
MODES = {'teaser': 'ねこじゃらし', 'pet': 'なでる', 'pause': '休憩'}
REACTIONS = {'neutral': '通常', 'favorable': '好意的', 'enthusiastic': '熱心',
             'turn_away': '拒否', 'confused': '戸惑い', 'listless': '低体力'}
CONDITION_FIELDS = ('kind', 'subject_id', 'mode', 'action', 'reaction', 'scene_key')
MAX_LENGTH = 200
_UNSET = object()


def actions(mode):
    if mode == 'pause':
        return ('pause',)
    return ('any',) + tuple(a for a in MODE_ACTIONS.get(mode, ())
                            if a in ('direct', 'adapt', 'intense', 'feint'))


def reactions(kind, mode):
    if kind == 'speech':
        return ('any',)
    return ('neutral', 'listless') if mode == 'pause' else tuple(REACTIONS)


def condition(kind='speech', subject_id='player', mode='teaser', action='any', reaction='any'):
    result = dict(kind=kind, subject_id=subject_id, mode=mode, action=action, reaction=reaction,
                  scene_key=mode if action in ('any', 'pause') else mode + '__' + action)
    validate_condition(result)
    return result


def validate_condition(value):
    if not isinstance(value, dict) or set(value) != set(CONDITION_FIELDS):
        raise ValueError('文章条件の形式が不正です。')
    if any(not isinstance(v, str) for v in value.values()):
        raise ValueError('文章条件は文字列で指定してください。')
    kind, subject, mode, action, reaction, scene = (value[k] for k in CONDITION_FIELDS)
    if kind not in KINDS or subject not in SUBJECTS[kind] or mode not in MODES:
        raise ValueError('人物・猫・行動を選び直してください。')
    if action not in actions(mode) or reaction not in reactions(kind, mode):
        raise ValueError('この行動では選べない操作・反応です。')
    expected = mode if action in ('any', 'pause') else mode + '__' + action
    if scene != expected:
        raise ValueError('画像の場面キーが行動・操作と一致しません。')


def key(value):
    validate_condition(value)
    return '/'.join(value[k] for k in CONDITION_FIELDS)


def make_entry(value, texts):
    validate_condition(value)
    if not isinstance(texts, (tuple, list)) or len(texts) != 3 or any(not isinstance(t, str) for t in texts):
        raise ValueError('文章欄は3案分を指定してください。')
    entry = dict(condition=copy.deepcopy(value), variants=[
        dict(id=uuid.uuid5(uuid.NAMESPACE_URL, 'catcafe/interaction/' + key(value) + '/' + str(i)).hex,
             slot=i, text=text.strip()) for i, text in enumerate(texts, 1)])
    validate_entry(entry)
    return entry


def validate_entry(entry, *, published=False):
    if not isinstance(entry, dict) or set(entry) != {'condition', 'variants'}:
        raise ValueError('交流文章の形式が不正です。')
    value, variants = entry['condition'], entry['variants']
    validate_condition(value)
    if not isinstance(variants, list) or len(variants) != 3:
        raise ValueError('文章欄は3案分必要です。')
    texts = []
    for i, row in enumerate(variants, 1):
        if not isinstance(row, dict) or set(row) != {'id', 'slot', 'text'}:
            raise ValueError('文章候補の形式が不正です。')
        expected = uuid.uuid5(uuid.NAMESPACE_URL, 'catcafe/interaction/' + key(value) + '/' + str(i)).hex
        if type(row['slot']) is not int or row['slot'] != i or row['id'] != expected:
            raise ValueError('文章候補のID・順番が不正です。')
        text = row['text']
        if (not isinstance(text, str) or text != text.strip() or len(text) > MAX_LENGTH
                or any(ord(c) < 32 and c not in '\n\t' for c in text)):
            raise ValueError(f'各案は{MAX_LENGTH}文字以内にしてください。制御文字は使えません。')
        if text:
            texts.append(text)
    if len(texts) != len(set(texts)):
        raise ValueError('同じ条件内で文章が重複しています。言い回しを変えてください。')
    if published and not texts:
        raise ValueError('登録には1案以上の文章が必要です。空欄は未制作として扱います。')


class InteractionTextStore:
    def __init__(self, path, *, published=False):
        self.path, self.published = Path(path), published

    def read(self):
        if not self.path.exists():
            return {'version': 1, 'entries': []}
        data = json.loads(self.path.read_text(encoding='utf-8'))
        if (not isinstance(data, dict) or set(data) != {'version', 'entries'}
                or type(data['version']) is not int or data['version'] != 1
                or not isinstance(data['entries'], list) or len(data['entries']) > 10000):
            raise ValueError('交流文章ファイルの形式が不正です。')
        seen = set()
        for entry in data['entries']:
            validate_entry(entry, published=self.published)
            identity = key(entry['condition'])
            if identity in seen:
                raise ValueError('同じ文章条件が重複しています。')
            seen.add(identity)
        return data

    def get(self, value):
        identity = key(value)
        return next((row for row in self.read()['entries'] if key(row['condition']) == identity), None)

    def save(self, entry, *, expected=_UNSET):
        validate_entry(entry, published=self.published)
        data = self.read()  # 不正な既存ファイルは上書きしない。
        identity = key(entry['condition'])
        old = next((row for row in data['entries'] if key(row['condition']) == identity), None)
        if expected is not _UNSET and old != expected:
            raise ValueError('この条件は別の編集で更新されています。再読込して確認してください。')
        data['entries'] = [row for row in data['entries'] if key(row['condition']) != identity]
        data['entries'].append(copy.deepcopy(entry))
        self.path.parent.mkdir(parents=True, exist_ok=True)
        temp = self.path.with_name(self.path.stem + '-' + uuid.uuid4().hex + '.tmp')
        try:
            temp.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
            temp.replace(self.path)
        finally:
            temp.unlink(missing_ok=True)
