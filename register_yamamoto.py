"""Register Yamamoto through the existing stores and export people only."""
import hashlib
import json
from pathlib import Path
from PIL import Image
from interaction_subjects import SubjectRegistry, game_subjects
from human_portraits import HumanPortraitStore

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'cat_cafe_sim/saves/content_images'
SOURCE = ROOT / 'Docs/Prototypes/YamamotoIcon-v1/yamamoto-icon.png'
OUT = ROOT / 'Logs/YamamotoIcon'
NOTES = ('役割：猫カフェを訪れ、猫と交流するお客さんの山本さん。'
         '性格：率直で落ち着いており、猫の小さな反応をよく見る。'
         '接し方：短く声をかけて反応を待つ。遊びや接触を重ねず、猫が離れたら静かに見守る。'
         '口調：気負わない短い常体。「〜してみる？」「〜もいいね」「ここで休もう」を基本とする。'
         '猫への呼びかけは {cat_call}。一人称はなるべく省略する。'
         '命令・幼児語・猫の気持ちや結果の決めつけを避ける。年齢・性別の文章設定・職業・来歴・来店歴は固定しない。'
         '外見：額を見せて後ろへ流した赤褐色の短髪、耳を見せるコンパクトな髪型。'
         '幅のある柔らかな角形の輪郭、やや太い眉、眼鏡なし、くすんだ橙色のカーディガンと象牙色の襟、口を閉じた微笑み。')
ROW = dict(id='person-guest-7', kind='speech', name='山本さん', game_id='guest-7',
           content_id='', notes=NOTES, call='{cat}', enabled=True)

def main():
    OUT.mkdir(exist_ok=True, parents=True)
    registry = SubjectRegistry(DATA / 'interaction_subjects.json')
    before = registry.read()
    portraits = HumanPortraitStore(DATA)
    previous = portraits.read()['people']
    assert game_subjects()['speech']['guest-7']['name'] == '山本さん'
    assert not any(r['game_id'] == 'guest-7' and r['id'] != ROW['id'] for r in before['subjects'])
    with Image.open(SOURCE) as im:
        assert im.mode == 'RGBA' and im.width == im.height
        alpha = im.getchannel('A').getextrema()
        assert alpha == (0, 255)
        image_size = im.size
    protected = [DATA/'interaction_texts.json', DATA/'manifest.json',
                 Path('C:/Users/hi-wa/unity_workspase/CatCafeSim/Assets/Resources/CatCafeContent/catalog.json')]
    hashes = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in protected}
    (OUT/'subjects-before.json').write_text(json.dumps(before, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    registry.save(ROW, expected=before)
    assert SubjectRegistry(registry.path).get(ROW['id']) == ROW
    assert [r for r in registry.read()['subjects'] if r['id'] != ROW['id']] == [r for r in before['subjects'] if r['id'] != ROW['id']]
    portraits.register(ROW['id'], SOURCE)
    current = HumanPortraitStore(DATA).read()['people']
    assert [r for r in current if r['game_id'] != 'guest-7'] == previous
    folder, warnings = portraits.export(OUT)
    assert not warnings
    exported = json.loads((folder/'human_portraits.json').read_text(encoding='utf-8'))
    assert exported['people'] == current and len(current) == 8
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
