"""交流文プレビューと書き出しで共有する、素材の対応・選択・不足条件。"""
import copy
import hashlib
import json
import struct
import uuid
from pathlib import Path

from interaction_texts import (SUBJECTS, MODES, REACTIONS, actions, reactions, condition,
                               key, validate_entry, InteractionTextStore)

CAT_ID = 'cat-mike'
DEFAULT_CONTENT_ID = 'playtest-mike'  # ゲームの既存presentation.jsonと同じ明示的対応。


class InteractionImageBindings:
    def __init__(self, path):
        self.path = Path(path)

    def get(self, catalog):
        if not self.path.exists():
            return DEFAULT_CONTENT_ID if DEFAULT_CONTENT_ID in catalog else ''
        data = json.loads(self.path.read_text(encoding='utf-8'))
        if (not isinstance(data, dict) or set(data) != {'version', 'cat_id', 'content_cat_id'}
                or type(data['version']) is not int or data['version'] != 1
                or data['cat_id'] != CAT_ID or not isinstance(data['content_cat_id'], str)
                or data['content_cat_id'] not in catalog):
            raise ValueError('交流文の画像ID対応が不正です。現在の猫一覧から設定し直してください。')
        return data['content_cat_id']

    def save(self, content_id, catalog):
        if content_id not in catalog:
            raise ValueError('対応する制作画像の猫IDを選んでください。')
        # 誤った対応を明示的に修正する用途。本文ファイルには触れない。
        self.path.parent.mkdir(parents=True, exist_ok=True)
        temp = self.path.with_name(self.path.stem + '-' + uuid.uuid4().hex + '.tmp')
        try:
            temp.write_text(json.dumps(dict(version=1, cat_id=CAT_ID, content_cat_id=content_id),
                                       ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
            temp.replace(self.path)
        finally:
            temp.unlink(missing_ok=True)


def read_png(path):
    data = Path(path).read_bytes()
    if (len(data) < 33 or data[:8] != b'\x89PNG\r\n\x1a\n' or data[12:16] != b'IHDR'
            or min(struct.unpack('>II', data[16:24])) <= 0):
        raise ValueError('PNGヘッダーが不正です。')
    return data


def image_choices(mode, action):
    requested = mode if action in ('any', 'pause') else mode + '__' + action
    return list(dict.fromkeys([requested, mode] + (['play'] if mode == 'teaser' else []) + ['normal']))


def resolve_image(registry, content_id, mode, action):
    images = registry.read()['cats'].get(content_id, {})
    choices = image_choices(mode, action)
    warnings = []
    for scene in choices:
        if scene not in images:
            continue
        path = registry._resolve(images[scene])
        try:
            data = read_png(path)
        except (OSError, ValueError) as exc:
            warnings.append(scene + '：画像を読み込めません（' + str(exc) + '）')
            continue
        if scene != choices[0]:
            warnings.append('要求画像 ' + choices[0] + ' がないため ' + scene + ' を代替表示します。')
        return dict(requested_scene_key=choices[0], resolved_scene_key=scene, source_path=path,
                    image='images/' + hashlib.sha256(data).hexdigest() + '.png', data=data, warnings=warnings)
    warnings.append('画像が未登録です。' if content_id else 'ミケの制作画像IDが未設定です。')
    return dict(requested_scene_key=choices[0], resolved_scene_key='', source_path=None,
                image='', data=None, warnings=warnings)


def select_entry(entries, value, *, overlay=None):
    """専用操作を優先し、なければ同じ反応の操作共通文を使う。"""
    rows = [(row, '登録済み') for row in entries]
    if overlay is not None:
        validate_entry(overlay)
        rows = [(row, source) for row, source in rows if key(row['condition']) != key(overlay['condition'])]
        rows.append((overlay, '編集中・未登録'))
    targets = [value]
    if value['action'] not in ('any', 'pause'):
        targets.append(condition(value['kind'], value['subject_id'], value['mode'], 'any', value['reaction']))
    for target in targets:
        for row, source in rows:
            if key(row['condition']) == key(target):
                candidates = [v for v in row['variants'] if v['text']]
                if candidates:
                    return dict(candidates=candidates, source=source,
                                match='専用' if target == value else '操作共通', condition=row['condition'])
    return dict(candidates=[], source='未制作', match='', condition=value)


def expected_conditions():
    for kind, subjects in SUBJECTS.items():
        for subject in subjects:
            for mode in MODES:
                for action in actions(mode):
                    if action == 'any':
                        continue
                    for reaction in reactions(kind, mode):
                        yield condition(kind, subject, mode, action, reaction)


def coverage(entries):
    missing, covered, common = [], 0, 0
    for value in expected_conditions():
        found = select_entry(entries, value)
        if not found['candidates']:
            missing.append(value)
        else:
            covered += 1
            common += found['match'] == '操作共通'
    return dict(expected_count=covered + len(missing), covered_count=covered,
                common_count=common, missing_count=len(missing), missing_conditions=missing)


def condition_label(value):
    from interaction_scenes import ACTION_LABELS
    return ('人物：' if value['kind'] == 'speech' else '猫：') + SUBJECTS[value['kind']][value['subject_id']] \
        + ' ／ ' + MODES[value['mode']] + ' ／ ' + ACTION_LABELS.get(value['action'], '操作共通') \
        + ' ／ ' + REACTIONS.get(value['reaction'], '反応指定なし')


ACTION_TEXTS = [dict(mode=mode, action=action, text=text) for mode, action, text in [
    ('teaser', 'direct', '{human}はねこじゃらしを床の近くで揺らした。'),
    ('teaser', 'adapt', '{human}は{cat}の様子に合わせてねこじゃらしを動かした。'),
    ('teaser', 'intense', '{human}はねこじゃらしを勢いよく動かした。'),
    ('teaser', 'feint', '{human}はねこじゃらしの動きに緩急をつけた。'),
    ('pet', 'direct', '{human}は{cat}の頭をなでた。'),
    ('pet', 'adapt', '{human}は{cat}の様子に合わせ、そっとなでた。'),
    ('pause', 'pause', '{human}は手を止め、{cat}が落ち着くのを待った。')]]
NARRATION_TEXTS = [dict(reaction=reaction, text=text) for reaction, text in [
    ('neutral', '静かな交流の時間が流れた。'), ('favorable', '穏やかな交流の時間が流れた。'),
    ('enthusiastic', '交流の時間に、明るい空気が広がった。'), ('turn_away', '今は少し距離を置きたいようだ。'),
    ('confused', '働きかけと反応の間に、小さな間が空いた。'), ('listless', '今は休む時間が必要そうだ。')]]
REST_NARRATION = '急がず、静かな休憩の時間を過ごした。'


def compose_preview(human, mode, action, reaction, speech, cat_response):
    move = next(r['text'] for r in ACTION_TEXTS if r['mode'] == mode and r['action'] == action)
    narration = REST_NARRATION if mode == 'pause' else next(r['text'] for r in NARRATION_TEXTS if r['reaction'] == reaction)
    return (human + '「' + speech + '」' if speech else '（未制作：人物のセリフ）') + '\n' \
        + move.format(human=human, cat='ミケ') + '\n\n' \
        + (cat_response or '（未制作：ミケの反応）') + '\n\n' + narration


def build_interaction_catalog(catalog, registry, *, store=None, bindings=None):
    store = store or InteractionTextStore(registry.directory / 'interaction_texts.json', published=True)
    if not store.published:
        raise ValueError('Unity向けには登録済み文章ストアを指定してください。')
    entries = store.read()['entries']
    bindings = bindings or InteractionImageBindings(registry.directory / 'interaction_image_bindings.json')
    content_id = bindings.get(catalog)
    result = dict(version=1, cat_bindings=[dict(cat_id=CAT_ID, content_cat_id=content_id)],
                  people=[dict(id=id, name=name) for id, name in SUBJECTS['speech'].items()],
                  entries=[], image_bindings=[], action_texts=copy.deepcopy(ACTION_TEXTS),
                  narration_texts=copy.deepcopy(NARRATION_TEXTS), rest_narration=REST_NARRATION)
    for entry in sorted(entries, key=lambda r: key(r['condition'])):
        result['entries'].append(dict(entry['condition'], variants=[copy.deepcopy(v) for v in entry['variants'] if v['text']]))
    blobs, warnings = {}, []
    for mode in MODES:
        for action in actions(mode):
            image = resolve_image(registry, content_id, mode, action)
            result['image_bindings'].append(dict(cat_id=CAT_ID, content_cat_id=content_id,
                 scene_key=image['requested_scene_key'], resolved_scene_key=image['resolved_scene_key'], image=image['image']))
            warnings.extend(image['requested_scene_key'] + '：' + w for w in image['warnings'])
            if image['data'] is not None:
                blobs[image['image']] = image['data']
    report = coverage(entries)
    report.update(entry_count=len(result['entries']), candidate_count=sum(len(r['variants']) for r in result['entries']),
                  image_warnings=warnings)
    # パックIDや出力場所ではなく、採用内容・画像の対応だけから版を固定する。
    result['revision'] = hashlib.sha256(json.dumps(result, ensure_ascii=False, sort_keys=True,
                                                  separators=(',', ':')).encode('utf-8')).hexdigest()
    return result, report, blobs
