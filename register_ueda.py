"""Register Ueda through the existing stores and export people only."""
import hashlib
import json
from pathlib import Path
from PIL import Image
from interaction_subjects import SubjectRegistry
from human_portraits import HumanPortraitStore

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'cat_cafe_sim/saves/content_images'
SOURCE = ROOT / 'Docs/Prototypes/UedaIcon-v1/ueda-icon.png'
OUT = ROOT / 'Logs/UedaIcon'
NOTES = '役割：8席目増設の翌営業日以降、刻24に来店する上田さん。名前・ゲーム客ID・来店条件は既存定義。今回採用した制作設定：58歳の女性、手描き看板の文字職人。核は「余白に厳しいが、猫の寝姿には即座に合格を出す人」。文字の傾きや間隔には容赦なく指摘する一方、猫が好きな場所を占めると「そこが正解か」と潔く認める。猫のしっぽを勝手に動かしたり姿勢を直したりはしない。短い断定形の常体で、評点のように言い切る。猫の呼び名は{cat_call}。一人称はあたし。例「その余白、いいね」「親方、そこを取るか」「よし。一発で決まった」。父の看板店で文字を習い、今も商店街の小店の手書き看板を請け負う。趣味は古い商店の看板採集と銭湯めぐり。外見は幅広の頬と短めの四角い顔、耳にかけた黒灰色のボブと前髪の太い銀白の一房、眼鏡・帽子なし。赤紫の作業上着と生成りのハイネック。両目は横長の開眼、眉は低く太い直線寄りで左右に角度差、口はすぼめず閉じて口角をやや下げる。少し顎を上げ、見る側の右へ浅く向けた顔と値踏みする視線で自負と審美眼を表す。採用画像は指定した水平眉より角度があり、切りそろえ前髪でなく流した前髪。髪だけでなく両目・眉・口・顔向きで差別化。年齢・性別・職業・来歴・趣味・性格・服装・口調は今回採用した創作設定。条件・数値・過去本文は変更しない。例文は人物制作資料のみ。'
ROW = dict(id='person-expanded-guest-5', kind='speech', name='上田さん', game_id='expanded-guest-5',
           content_id='', notes=NOTES, call='{cat}親方', enabled=True)

def main():
    OUT.mkdir(exist_ok=True, parents=True)
    registry = SubjectRegistry(DATA / 'interaction_subjects.json')
    before = registry.read()
    portraits = HumanPortraitStore(DATA)
    previous = portraits.read()['people']
    # Special customers are explicitly identified in presentation labels, not normal guest candidates.
    presentation = json.loads((ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/presentation.json').read_text(encoding='utf-8'))
    assert {r['id']:r['label'] for r in presentation['special_names']}['expanded-guest-5'] == '上田さん'
    assert not any(r['game_id'] == 'expanded-guest-5' and r['id'] != ROW['id'] for r in before['subjects'])
    with Image.open(SOURCE) as im:
        assert im.mode == 'RGBA' and im.width == im.height
        alpha = im.getchannel('A').getextrema()
        assert alpha == (0, 255)
        image_size = im.size
    protected = [DATA/'interaction_texts.json', DATA/'manifest.json',
                 Path('C:/Users/hi-wa/unity_workspase/CatCafeSim/Assets/Resources/CatCafeContent/catalog.json')]
    hashes = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in protected}
    if not (OUT/'subjects-before.json').exists():
        (OUT/'subjects-before.json').write_text(json.dumps(before, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    registry.save(ROW, expected=before)
    assert SubjectRegistry(registry.path).get(ROW['id']) == ROW
    assert [r for r in registry.read()['subjects'] if r['id'] != ROW['id']] == [r for r in before['subjects'] if r['id'] != ROW['id']]
    portraits.register(ROW['id'], SOURCE)
    current = HumanPortraitStore(DATA).read()['people']
    assert [r for r in current if r['game_id'] != 'expanded-guest-5'] == [r for r in previous if r['game_id'] != 'expanded-guest-5']
    folder, warnings = portraits.export(OUT)
    assert not warnings
    exported = json.loads((folder/'human_portraits.json').read_text(encoding='utf-8'))
    assert exported['people'] == current and len(current) == 24
    assert portraits.export(OUT)[0] == folder
    assert not (folder/'bundle.json').exists()
    assert {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in protected} == hashes
    result = dict(status='PASS', subject=ROW, pack=str(folder), image_size=image_size,
                  alpha=alpha, portraits=current, protected_sha256=hashes,
                  scope='Store API save/reload and people-only export; no GUI input claim')
    (OUT/'registration.json').write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(dict(status='PASS', pack=str(folder), image_size=image_size, alpha=alpha), ensure_ascii=False))

if __name__ == '__main__':
    main()
