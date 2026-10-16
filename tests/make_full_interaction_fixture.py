"""全動作の読込・実表示検証用。実制作データは使わず、小さな画像2枚で作る。"""
import json
from pathlib import Path
import sys

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from interaction_subjects import SubjectRegistry
from interaction_texts import InteractionTextStore,MODES,actions,reactions,condition,make_entry
from content_images import ImageRegistry
from content_texts import TextStore
from unity_export import export_bundle


RESPONSES={
    'neutral':['{cat}は落ち着いて相手の様子を見た。','{cat}は静かに相手へ注意を向けた。','{cat}は相手の様子をゆっくりうかがった。'],
    'favorable':['{cat}は穏やかな様子で相手に応じた。','{cat}は力を抜き、相手へ注意を向けた。','{cat}は落ち着いた様子で働きかけを受け入れた。'],
    'enthusiastic':['{cat}は強い関心を示して相手に応じた。','{cat}は相手へ熱心に注意を向けた。','{cat}は待ちきれない様子で働きかけに応じた。'],
    'turn_away':['{cat}は顔をそむけ、相手から距離を取った。','{cat}は視線をそらし、働きかけを避けた。','{cat}は相手からそっと身を引いた。'],
    'confused':['{cat}は応じ方を迷うように動きを止めた。','{cat}は相手を見つめ、戸惑う様子を見せた。','{cat}は耳を動かし、相手の様子をうかがった。'],
    'listless':['{cat}はあまり動かず、静かに息を整えた。','{cat}は動きを控え、相手の様子を見た。','{cat}はまぶたを重そうに下げ、静かに休んだ。']}


def create(output):
    from PIL import Image
    output=Path(output);source=output/'source';source.mkdir(parents=True,exist_ok=True)
    subjects=SubjectRegistry(source/'interaction_subjects.json',settings_path=source/'settings.json')
    for row in subjects.read()['subjects']:
        row['notes']='確認済みの制作資料。来歴・背景は追加しない。';subjects.save(row)
    for id,kind,name,game,content in [('author-tama','cat_response','タマ','cat-tama','playtest-tama'),
                                      ('author-suzuki','speech','鈴木さん','guest-2','')]:
        subjects.save(dict(id=id,kind=kind,name=name,game_id=game,content_id=content,notes='確認済みの制作資料。来歴・背景は追加しない。',call='{cat}ちゃん' if kind=='speech' else '{cat}',enabled=True))
    store=InteractionTextStore(source/'interaction_texts.json',published=True,subject_registry=subjects)
    labels=subjects.labels();entries=[]
    for person in labels['speech']:
        for mode in MODES:
            for action in actions(mode):
                entries.append(make_entry(condition(subject_id=person,mode=mode,action=action,subjects=labels,target_cat='*'),
                    ['{cat_call}、ゆっくり付き合ってね。','{cat_call}、少し付き合ってくれるかな。','{cat_call}、様子を見ながらにしよう。']))
    for cat in labels['cat_response']:
        for mode in MODES:
            for action in actions(mode):
                for reaction in reactions('cat_response',mode):
                    entries.append(make_entry(condition('cat_response',cat,mode,action,reaction,subjects=labels),RESPONSES[reaction]))
    store.save_many(entries)
    cats={}
    for i,(content,name) in enumerate(subjects.image_catalog({}).items()):
        filename=f'cat-{i}.png';Image.new('RGB',(16,16),(140+70*i,170,110)).save(source/filename)
        scenes={'normal':filename,'play':filename}
        for mode in MODES:
            for action in actions(mode):scenes[condition(mode=mode,action=action)['scene_key']]=filename
        cats[content]=scenes
    (source/'manifest.json').write_text(json.dumps(dict(version=1,cats=cats)),encoding='utf8')
    pack,report=export_bundle(subjects.image_catalog({}),ImageRegistry(source),TextStore(source/'texts.json'),output/'packs',profiles={},interaction_texts=store)
    (output/'pack-path.txt').write_text(str(pack.resolve()),encoding='utf8')
    return pack,report


if __name__=='__main__':print(create(Path(sys.argv[1]))[0])
