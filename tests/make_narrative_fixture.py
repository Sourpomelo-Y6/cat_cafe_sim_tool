"""Unity接続検証用の短い採用文パックを作る。実制作・ゲームセーブは変更しない。"""
import json
from pathlib import Path
import shutil
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from content_images import ImageRegistry
from content_texts import TextStore
from interaction_texts import InteractionTextStore, condition, make_entry, REACTIONS
from unity_export import export_bundle


def create(output):
    source = output / 'source'
    source.mkdir(parents=True, exist_ok=True)
    asset = Path(__file__).resolve().parents[2] / 'Docs/Prototypes/CalicoActionBackgroundPreview-v1.png'
    shutil.copyfile(asset, source / 'image.png')
    scenes = ['normal', 'play', 'pet', 'pause', 'teaser', 'teaser__adapt']
    (source / 'manifest.json').write_text(json.dumps(dict(version=1, cats={
        'playtest-mike': {s: 'image.png' for s in scenes}})), encoding='utf-8')
    store = InteractionTextStore(source / 'interaction_texts.json', published=True)
    for person in ['player', 'guest-1']:
        for mode, texts in [('teaser', ['ミケ、こっちで遊ぼう。', 'これ、気になる？', 'ゆっくり追いかけてみよう。']),
                            ('pet', ['ミケ、なでてもいいかな。', 'そっとなでるね。', '少し触らせてね。']),
                            ('pause', ['少し休もうか。', '急がなくていいよ。', 'ここでゆっくりしていよう。'])]:
            store.save(make_entry(condition(subject_id=person, mode=mode, action='pause' if mode == 'pause' else 'any'), texts))
    responses = {
        'neutral': ['ミケは様子を見ながら、静かに応じた。', 'ミケは耳を動かし、相手の様子を見た。', 'ミケは落ち着いた姿勢で、ゆっくり反応した。'],
        'favorable': ['ミケは気持ちよさそうに目を細めた。', 'ミケは力を抜き、穏やかに応じた。', 'ミケは相手の手元へそっと顔を寄せた。'],
        'enthusiastic': ['ミケは身を乗り出し、熱心に応じた。', 'ミケは目を輝かせ、相手の動きに応じた。', 'ミケは待ちきれない様子で、すぐに反応した。'],
        'turn_away': ['ミケは顔を背け、距離を取った。', 'ミケは相手からそっと身を引いた。', 'ミケは視線をそらし、働きかけを避けた。'],
        'confused': ['ミケは耳を動かし、戸惑う様子を見せた。', 'ミケはどう応じるか迷うように動きを止めた。', 'ミケは相手の手元を見つめ、少し首をかしげた。'],
        'listless': ['ミケはまぶたを重そうに下げ、静かに休んだ。', 'ミケは体を丸め、動かずにひと息ついた。', 'ミケは前足をそろえ、ゆっくり息を整えた。']}
    for mode in ['teaser', 'pet', 'pause']:
        for reaction in (['neutral', 'listless'] if mode == 'pause' else REACTIONS):
            store.save(make_entry(condition('cat_response', 'cat-mike', mode,
                            'pause' if mode == 'pause' else 'any', reaction), responses[reaction]))
    store.save(make_entry(condition(action='direct'), ['ミケ、穂先を見てごらん。', 'ここで揺らしてみるね。', 'ゆっくり動かしてみよう。']))
    folder, report = export_bundle({'playtest-mike': 'ミケ'}, ImageRegistry(source),
                                  TextStore(source / 'texts.json'), output / 'packs', profiles={})
    (output / 'fixture-path.txt').write_text(str(folder.resolve()), encoding='utf-8')
    print(folder.resolve())
    return folder


if __name__ == '__main__':
    create(Path(sys.argv[1]))
