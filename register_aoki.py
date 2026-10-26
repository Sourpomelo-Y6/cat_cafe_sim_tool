"""Register Aoki through the existing stores and export people only."""
import hashlib
import json
from pathlib import Path
from PIL import Image
from interaction_subjects import SubjectRegistry
from human_portraits import HumanPortraitStore

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'cat_cafe_sim/saves/content_images'
SOURCE = ROOT / 'Docs/Prototypes/AokiIcon-v1/aoki-icon.png'
OUT = ROOT / 'Logs/AokiIcon'
NOTES = '役割：猫カフェを訪れる青木さん。確定設定は触れ合いを希望する条件付き接客客。contactという内部IDや回数条件から接触を強要する人格を決めない。今回の制作設定：相手の様子を確かめ、落ち着いて聞く実直な人。接し方：触れる前に短く声をかけ、猫が近づくか待つ。拒否・休憩を尊重し、追わず手を止める。口調：短く穏やかな丁寧語。「少し待ちましょうか」「ここで見ていますね」「今日は休みましょう」。猫の呼び方は {cat_call}。命令・幼児語・猫の気持ちや結果の断定を避ける。職業・来歴・年齢・来店歴は追加しない。外見：あごに届く暗い灰褐色のゆるいボブ、横へ流れた分け目、眼鏡なし、くすんだオリーブ色の襟なし上着と生成りのインナー。表情：横長で開いた目と正面寄りの視線、水平寄りの太い両眉、小さく開いた口の聞き顔。目は指定より大きめだが、西村さんの半開きの横目・短い閉じ口とは異なる。強い怒り・悲しみ・驚きに固定しない。'
ROW = dict(id='person-advanced-contact', kind='speech', name='青木さん', game_id='advanced-contact',
           content_id='', notes=NOTES, call='{cat}', enabled=True)

def main():
    OUT.mkdir(exist_ok=True, parents=True)
    registry = SubjectRegistry(DATA / 'interaction_subjects.json')
    before = registry.read()
    portraits = HumanPortraitStore(DATA)
    previous = portraits.read()['people']
    # Special customers are explicitly identified in presentation labels, not normal guest candidates.
    presentation = json.loads((ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/presentation.json').read_text(encoding='utf-8'))
    assert {r['id']:r['label'] for r in presentation['special_names']}['advanced-contact'] == '青木さん'
    assert not any(r['game_id'] == 'advanced-contact' and r['id'] != ROW['id'] for r in before['subjects'])
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
    assert [r for r in current if r['game_id'] != 'advanced-contact'] == previous
    folder, warnings = portraits.export(OUT)
    assert not warnings
    exported = json.loads((folder/'human_portraits.json').read_text(encoding='utf-8'))
    assert exported['people'] == current and len(current) == 18
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
