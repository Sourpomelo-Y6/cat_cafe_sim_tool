"""Register Sakamoto through the existing stores and export people only."""
import hashlib
import json
from pathlib import Path
from PIL import Image
from interaction_subjects import SubjectRegistry
from human_portraits import HumanPortraitStore

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'cat_cafe_sim/saves/content_images'
SOURCE = ROOT / 'Docs/Prototypes/SakamotoIcon-v1/sakamoto-icon.png'
OUT = ROOT / 'Logs/SakamotoIcon'
NOTES = '役割：猫カフェを訪れる坂本さん。長毛猫が好みという確定設定を保持する。気まぐれという既存の客種名は確率来店の説明であり性格の断定には使わない。今回の制作設定：気さくで、猫の反応を面白がりながら待てる。接し方：軽く一度誘って反応を見守り、触れる前に距離を確かめ、嫌がれば引いて休憩を尊重する。口調：短く気さくな常体。「〜してみようか」「そっちが気になる？」「ここで待ってるよ」。猫の呼び方は {cat_call}。命令・幼児語・猫の気持ちや結果の断定を避ける。長毛の接客条件は猫側の条件であり人物の髪の長さと無関係。職業・来歴・来店歴・年齢設定は追加しない。外見：額の一部が見える短い濃茶の髪、眼鏡なし、黄土色の襟付きシャツと生成りのインナー。表情：細めの開いた下がり目、片側を少し上げた左右差のある眉、話しかけるように小さく開いた口。軽い親しみと関心を示す平常顔。'
ROW = dict(id='person-advanced-longhair', kind='speech', name='坂本さん', game_id='advanced-longhair',
           content_id='', notes=NOTES, call='{cat}', enabled=True)

def main():
    OUT.mkdir(exist_ok=True, parents=True)
    registry = SubjectRegistry(DATA / 'interaction_subjects.json')
    before = registry.read()
    portraits = HumanPortraitStore(DATA)
    previous = portraits.read()['people']
    # Special customers are explicitly identified in presentation labels, not normal guest candidates.
    presentation = json.loads((ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/presentation.json').read_text(encoding='utf-8'))
    assert {r['id']:r['label'] for r in presentation['special_names']}['advanced-longhair'] == '坂本さん'
    assert not any(r['game_id'] == 'advanced-longhair' and r['id'] != ROW['id'] for r in before['subjects'])
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
    assert [r for r in current if r['game_id'] != 'advanced-longhair'] == previous
    folder, warnings = portraits.export(OUT)
    assert not warnings
    exported = json.loads((folder/'human_portraits.json').read_text(encoding='utf-8'))
    assert exported['people'] == current and len(current) == 15
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
