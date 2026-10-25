"""Register Watanabe through the existing stores and export people only."""
import hashlib
import json
from pathlib import Path
from PIL import Image
from interaction_subjects import SubjectRegistry, game_subjects
from human_portraits import HumanPortraitStore

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'cat_cafe_sim/saves/content_images'
SOURCE = ROOT / 'Docs/Prototypes/WatanabeIcon-v1/watanabe-icon.png'
OUT = ROOT / 'Logs/WatanabeIcon'
NOTES = ('役割：猫カフェを訪れ、猫と交流するお客さんの渡辺さん。'
         '性格：好奇心があり、物事をゆったり楽しむ。小さな変化を素直に言葉にする。'
         '接し方：猫の視線や距離を見て一度だけ穏やかに誘い、反応がなければ遊びを止めて待つ。'
         '口調：親しみのある短い丁寧語。「〜してみませんか」「〜もよさそうですね」「ひと休みしましょうか」を基本とする。'
         '猫への呼びかけは {cat_call}。一人称はなるべく省略する。'
         '命令・幼児語・猫の気持ちや結果の決めつけを避ける。年齢・性別の文章設定・職業・来歴・来店歴は固定しない。'
         '外見：明るい灰茶色の短いゆるいウェーブのピクシーカット、耳を見せる丸い髪の輪郭。'
         '頬に丸みのある顔、眼鏡なし、くすんだラベンダー色のカーディガンと象牙色の襟、口を閉じた微笑み。')
ROW = dict(id='person-guest-6', kind='speech', name='渡辺さん', game_id='guest-6',
           content_id='', notes=NOTES, call='{cat}', enabled=True)

def main():
    OUT.mkdir(exist_ok=True, parents=True)
    registry = SubjectRegistry(DATA / 'interaction_subjects.json')
    before = registry.read()
    portraits = HumanPortraitStore(DATA)
    previous = portraits.read()['people']
    assert game_subjects()['speech']['guest-6']['name'] == '渡辺さん'
    assert not any(r['game_id'] == 'guest-6' and r['id'] != ROW['id'] for r in before['subjects'])
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
    assert [r for r in current if r['game_id'] != 'guest-6'] == previous
    folder, warnings = portraits.export(OUT)
    assert not warnings
    exported = json.loads((folder/'human_portraits.json').read_text(encoding='utf-8'))
    assert exported['people'] == current and len(current) == 7
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
