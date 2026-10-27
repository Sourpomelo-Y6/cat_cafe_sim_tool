"""交流文プレビューと書き出しで共有する、素材の対応・選択・不足条件。"""
import copy
import hashlib
import json
import struct
import uuid
from pathlib import Path

from interaction_texts import (SUBJECTS, MODES, REACTIONS, actions, reactions, condition,
                               key, validate_entry, InteractionTextStore)
from interaction_texts import NARRATIVE_SCENES, scene_actions, scene_reactions

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
    warnings.append('画像が未登録です。' if content_id else '対象猫の制作画像IDが未設定です。')
    return dict(requested_scene_key=choices[0], resolved_scene_key='', source_path=None,
                image='', data=None, warnings=warnings)


def select_entry(entries, value, *, overlay=None, prefer_overlay=False):
    """専用操作を優先し、なければ同じ反応の操作共通文を使う。"""
    rows = [(row, '登録済み') for row in entries]
    if overlay is not None:
        validate_entry(overlay)
        rows = [(row, source) for row, source in rows if key(row['condition']) != key(overlay['condition'])]
        rows.append((overlay, '編集中・未登録'))
    cat_id = value.get('target_cat', 'cat-mike')
    if prefer_overlay and overlay is not None:
        c=overlay['condition']
        if (c.get('narrative_scene','normal')==value.get('narrative_scene','normal') and all(c[k]==value[k] for k in ('kind','subject_id','mode','reaction'))
                and c['action'] in (value['action'],'any')
                and (c['kind']!='speech' or c.get('target_cat','cat-mike') in (cat_id,'*'))):
            candidates=[v for v in overlay['variants'] if v['text']]
            if candidates:return dict(candidates=candidates,source='編集中・未登録',
                match='専用' if c['action']==value['action'] else '操作共通',condition=c)

    for scope in ([cat_id, '*'] if value['kind']=='speech' and cat_id!='*' else [cat_id]):
        for action in dict.fromkeys([value['action'], 'any']):
            for row, source in rows:
                c=row['condition']
                if (c.get('narrative_scene','normal')==value.get('narrative_scene','normal') and all(c[k]==value[k] for k in ('kind','subject_id','mode','reaction'))
                        and c['action']==action and (c['kind']!='speech' or c.get('target_cat','cat-mike')==scope)):
                    candidates=[v for v in row['variants'] if v['text']]
                    if candidates:return dict(candidates=candidates,source=source,
                        match='専用' if action==value['action'] else '操作共通',condition=c)
    return dict(candidates=[], source='未制作', match='', condition=value)


def expected_conditions(subjects=None, target_cat=None, narrative_scene='normal'):
    for kind, members in (subjects or SUBJECTS).items():
        for subject in members:
            for mode in MODES:
                for action in scene_actions(mode, narrative_scene):
                    if action == 'any':
                        continue
                    for reaction in scene_reactions(kind, mode, narrative_scene):
                        yield condition(kind, subject, mode, action, reaction, subjects=subjects,target_cat=target_cat if kind=="speech" else None,narrative_scene=narrative_scene)


def coverage(entries, subjects=None, target_cat=None, narrative_scene='normal'):
    missing, covered, common = [], 0, 0
    for value in expected_conditions(subjects,target_cat,narrative_scene):
        found = select_entry(entries, value)
        if not found['candidates']:
            missing.append(value)
        else:
            covered += 1
            common += found['match'] == '操作共通'
    return dict(expected_count=covered + len(missing), covered_count=covered,
                common_count=common, missing_count=len(missing), missing_conditions=missing)


def condition_label(value, subjects=None):
    from interaction_scenes import ACTION_LABELS
    return ('人物：' if value['kind'] == 'speech' else '猫：') + (subjects or SUBJECTS)[value['kind']][value['subject_id']] \
        + ' ／ ' + MODES[value['mode']] + ' ／ ' + ACTION_LABELS.get(value['action'], '操作共通') \
        + ' ／ ' + REACTIONS.get(value['reaction'], '反応指定なし') + ' ／ ' + NARRATIVE_SCENES[value.get('narrative_scene','normal')]


ACTION_TEXTS = [dict(mode=mode, action=action, text=text) for mode, action, text in [
    ('teaser', 'direct', '{human}はねこじゃらしを床の近くで揺らした。'),
    ('teaser', 'adapt', '{human}は{cat}の様子に合わせてねこじゃらしを動かした。'),
    ('teaser', 'intense', '{human}はねこじゃらしを勢いよく動かした。'),
    ('teaser', 'feint', '{human}はねこじゃらしの動きに緩急をつけた。'),
    ('ball', 'direct', '{human}はボールを床の上で転がした。'),
    ('ball', 'adapt', '{human}は{cat}の様子に合わせてボールを転がした。'),
    ('ball', 'intense', '{human}はボールを勢いよく転がした。'),
    ('ball', 'feint', '{human}はボールを転がす向きや間合いを変えた。'),
    ('plush', 'direct', '{human}はぬいぐるみを{cat}の近くで動かした。'),
    ('plush', 'adapt', '{human}は{cat}の様子に合わせてぬいぐるみを動かした。'),
    ('plush', 'feint', '{human}はぬいぐるみを動かす向きや間合いを変えた。'),
    ('tunnel', 'direct', '{human}はトンネルの入口で{cat}を遊びに誘った。'),
    ('tunnel', 'adapt', '{human}は{cat}の様子に合わせてトンネル遊びに誘った。'),
    ('tunnel', 'intense', '{human}はトンネル越しに、勢いのある動きで{cat}を遊びに誘った。'),
    ('tunnel', 'feint', '{human}はトンネルの入口で動きに緩急をつけた。'),
    ('brush', 'direct', '{human}は{cat}の毛並みに沿ってブラシを動かした。'),
    ('brush', 'adapt', '{human}は{cat}の様子に合わせて、そっとブラシを動かした。'),
    ('voice', 'direct', '{human}は{cat}に穏やかに声をかけた。'),
    ('voice', 'adapt', '{human}は{cat}の様子を見ながら、声の調子や間合いを合わせた。'),
    ('presence', 'direct', '{human}は{cat}のそばで静かに様子を見守った。'),
    ('presence', 'adapt', '{human}は{cat}の様子に合わせて距離を保ち、静かに見守った。'),
    ('pet', 'direct', '{human}は{cat}の頭をなでた。'),
    ('pet', 'adapt', '{human}は{cat}の様子に合わせ、そっとなでた。'),
    ('pause', 'pause', '{human}は手を止め、{cat}が落ち着くのを待った。')]]
NARRATION_TEXTS = [dict(reaction=reaction, text=text) for reaction, text in [
    ('neutral', '静かな交流の時間が流れた。'), ('favorable', '穏やかな交流の時間が流れた。'),
    ('enthusiastic', '交流の時間に、明るい空気が広がった。'), ('turn_away', '今は少し距離を置きたいようだ。'),
    ('confused', '働きかけと反応の間に、小さな間が空いた。'), ('listless', '今は休む時間が必要そうだ。')]]
REST_NARRATION = '急がず、静かな休憩の時間を過ごした。'


def compose_preview(human, mode, action, reaction, speech, cat_response, cat_name="ミケ", call="{cat}", narrative_scene='normal'):
    from interaction_subjects import expand
    speech, cat_response = expand(speech, cat_name, call), expand(cat_response, cat_name, call)
    if narrative_scene in ('switch','connect','simultaneous'):
        switch=narrative_scene=='switch'
        speech=speech or ('今度はこれにしよう。' if switch else cat_name+'、こっちを見て。')
        cat_response=cat_response or (cat_name+'は人の手元を見つめた。' if switch else cat_name+'はその働きかけを受け止めた。'+(' '+cat_name+'は心を開いた様子を見せた。' if narrative_scene=='simultaneous' else ''))
        move=human+('は交流を「'+MODES[mode]+'」へ切り替えた。' if switch else 'は'+cat_name+'の様子を見ながら、特別な働きかけをした。')
        return human+'「'+speech+'」\n'+move+'\n\n'+cat_response+'\n\n'+('次の働きかけまで、少し間を置いた。' if switch else '二人の間合いが、少し近づいた。')
    if narrative_scene=='open_up':
        reaction='neutral' if reaction=='any' else reaction
        default_speech=('少し休もうか。' if action=='pause' else '今度はこれにしよう。' if action=='switch' else cat_name+('、遊ぼうか。' if mode=='teaser' else '、なでてもいいかな。' if mode=='pet' else '、付き合ってくれるかな。'))
        speech=speech or default_speech
        if not cat_response:
            if action=='switch':body='は人の手元を見つめた。'
            elif action=='pause':body='は体を丸め、ゆっくり息を整えた。' if reaction=='listless' else 'は前足をそろえ、静かに座った。'
            elif reaction=='turn_away':body='は顔を背け、手の届かない方へ身を引いた。'
            elif reaction=='listless':body='はまぶたを重そうに下げ、動こうとしなかった。'
            elif reaction=='confused':body='は耳を動かし、どう応じるか迷っているようだった。'
            elif reaction=='enthusiastic':body='は穂先を追い、勢いよく前足を伸ばした。' if mode=='teaser' else 'は目を細め、手に頬を寄せた。' if mode in ('pet','brush') else 'は相手へ耳を向け、声に熱心に応じた。' if mode=='voice' else 'は落ち着いた様子で、相手の気配を熱心にうかがった。' if mode=='presence' else 'は身を乗り出し、熱心に応じた。'
            elif reaction=='favorable':body='は穂先を見つめ、前足でそっと触れた。' if mode=='teaser' else 'は力を抜き、気持ちよさそうに目を細めた。' if mode in ('pet','brush') else 'は穏やかな様子で、相手へ注意を向けた。' if mode in ('voice','presence') else 'は穏やかに応じた。'
            else:body='は様子を見ながら、静かに応じた。'
            cat_response=cat_name+body+' '+cat_name+'は心を開いた様子を見せた。'
    move = next((r['text'] for r in ACTION_TEXTS if r['mode'] == mode and r['action'] == action),
                '{human}は交流を「'+MODES[mode]+'」へ切り替えた。' if action=='switch' else '{human}は手を止め、{cat}が落ち着くのを待った。')
    narration = REST_NARRATION if mode == 'pause' else next(r['text'] for r in NARRATION_TEXTS if r['reaction'] == reaction)
    if narrative_scene=='open_up' and action in ('pause','switch'):
        narration=REST_NARRATION if action=='pause' else '次の働きかけまで、少し間を置いた。'
    return (human + '「' + speech + '」' if speech else '（未制作：人物のセリフ）') + '\n' \
        + move.format(human=human, cat=cat_name) + '\n\n' \
        + (cat_response or '（未制作：'+cat_name+'の反応）') + '\n\n' + narration


def build_interaction_catalog(catalog, registry, *, store=None, bindings=None):
    if store is None:
        from interaction_subjects import SubjectRegistry
        subjects=SubjectRegistry(registry.directory/'interaction_subjects.json')
        store=InteractionTextStore(registry.directory/'interaction_texts.json',published=True,subject_registry=subjects if subjects.path.exists() else None)
    if not store.published:
        raise ValueError('Unity向けには登録済み文章ストアを指定してください。')
    entries = store.read()['entries']
    from interaction_subjects import SubjectRegistry
    subjects=store.subject_registry or SubjectRegistry(registry.directory/'interaction_subjects.json')
    rows=subjects.read()['subjects']
    if not store.subject_registry:
        bindings=bindings or InteractionImageBindings(registry.directory/'interaction_image_bindings.json')
        content_id=bindings.get(catalog)
        rows=copy.deepcopy(rows)
        for row in rows:
            if row['id']==CAT_ID:row['content_id']=content_id
    return build_registered_catalog(catalog,registry,store,entries,rows=rows)


def build_registered_catalog(catalog, registry, store, entries, *, rows):
    all_rows=rows
    rows=[r for r in all_rows if r['enabled'] and r['game_id']]
    cats=[r for r in rows if r['kind']=='cat_response']
    people=[r for r in rows if r['kind']=='speech']
    ids={r['id'] for r in rows}; cat_ids={r['id'] for r in cats}
    special=any(e['condition'].get('narrative_scene','normal')!='normal' for e in entries)
    result=dict(version=4 if special else 3,cat_bindings=[dict(cat_id=r['game_id'],content_cat_id=r['content_id'],subject_id=r['id'],name=r['name']) for r in cats],
        people=[dict(id=r['id'],name=r['name'],game_id=r['game_id'],call=r['call']) for r in people],
        entries=[],image_bindings=[],action_texts=copy.deepcopy(ACTION_TEXTS),narration_texts=copy.deepcopy(NARRATION_TEXTS),rest_narration=REST_NARRATION)
    for entry in sorted(entries,key=lambda r:key(r['condition'])):
        c=entry['condition']; target=c.get('target_cat','cat-mike') if c['kind']=='speech' else ''
        if c['subject_id'] not in ids or (target and target!='*' and target not in cat_ids):continue
        from interaction_subjects import expand
        person=next((r for r in people if r['id']==c['subject_id']),None)
        for cat in cats:
            if (c['kind']=='cat_response' and cat['id']==c['subject_id'] or c['kind']=='speech' and target in ('*',cat['id'])):
                for v in entry['variants']:expand(v['text'],cat['name'],person['call'] if person else '{cat}')
        result['entries'].append(dict(c,target_cat=target,pool_key=key(c),variants=[copy.deepcopy(v) for v in entry['variants'] if v['text']]))
        if special:result['entries'][-1]['narrative_scene']=c.get('narrative_scene','normal')
        else:result['entries'][-1].pop('narrative_scene',None)
    blobs,warnings={},[]
    for cat in cats:
        for mode in MODES:
            for action in actions(mode):
                image=resolve_image(registry,cat['content_id'],mode,action)
                result['image_bindings'].append(dict(cat_id=cat['game_id'],content_cat_id=cat['content_id'],scene_key=image['requested_scene_key'],resolved_scene_key=image['resolved_scene_key'],image=image['image']))
                warnings.extend(cat['name']+' / '+image['requested_scene_key']+'：'+w for w in image['warnings'])
                if image['data'] is not None:blobs[image['image']]=image['data']
    labels={kind:{r['id']:r['name'] for r in rows if r['kind']==kind} for kind in SUBJECTS}
    report=coverage(entries,labels)
    report['optional_special_scenes']={s:coverage(entries,labels,narrative_scene=s) for s in NARRATIVE_SCENES if s!='normal'}
    report.update(entry_count=len(result['entries']),candidate_count=sum(len(e['variants']) for e in result['entries']),image_warnings=warnings,
        excluded_entry_count=len(entries)-len(result['entries']),excluded_subjects=[dict(id=r['id'],name=r['name'],reason='利用対象外' if not r['enabled'] else 'ゲームID未対応') for r in all_rows if not r['enabled'] or not r['game_id']])
    result['revision']=hashlib.sha256(json.dumps(result,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode('utf-8')).hexdigest()
    return result,report,blobs
