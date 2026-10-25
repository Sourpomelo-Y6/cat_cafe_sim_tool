"""Register Kobayashi through the existing stores and export people only."""
import hashlib
import json
from pathlib import Path
from PIL import Image
from interaction_subjects import SubjectRegistry, game_subjects
from human_portraits import HumanPortraitStore

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'cat_cafe_sim/saves/content_images'
SOURCE = ROOT / 'Docs/Prototypes/KobayashiIcon-v1/kobayashi-icon.png'
OUT = ROOT / 'Logs/KobayashiIcon'
NOTES = ('役割：猫カフェを訪れ、猫と交流するお客さんの小林さん。'
         '性格：落ち着いて親しみやすく、小さな変化を楽しむ。'
         '接し方：短く一度だけ誘って待ち、猫が離れたら追わずに見守る。休憩を尊重する。'
         '口調：簡潔で穏やかな常体。「〜してみようか」「〜もいいね」「ここで待とう」を基本とする。'
         '猫への呼びかけは {cat_call}。一人称はなるべく省略する。命令・幼児語・猫の気持ちや結果の決めつけを避ける。'
         '年齢・性別の文章設定・職業・来歴・来店歴は固定しない。'
         '外見：横分けの短い黒に近いチャコール髪、耳が見えるコンパクトな丸い髪型。柔らかな角形の輪郭、濃茶の角形眼鏡。'
         '深い青緑のカーディガンと象牙色の襟、口を閉じた穏やかな微笑み。')
ROW = dict(id='person-guest-9', kind='speech', name='小林さん', game_id='guest-9',
           content_id='', notes=NOTES, call='{cat}', enabled=True)

def main():
    OUT.mkdir(exist_ok=True, parents=True)
    registry = SubjectRegistry(DATA / 'interaction_subjects.json')
    before = registry.read()
    portraits = HumanPortraitStore(DATA)
    previous = portraits.read()['people']
    assert game_subjects()['speech']['guest-9']['name'] == '小林さん'
    assert not any(r['game_id'] == 'guest-9' and r['id'] != ROW['id'] for r in before['subjects'])
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
    assert [r for r in current if r['game_id'] != 'guest-9'] == previous
    folder, warnings = portraits.export(OUT)
    assert not warnings
    exported = json.loads((folder/'human_portraits.json').read_text(encoding='utf-8'))
    assert exported['people'] == current and len(current) == 10
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
