"""Register Fujiwara through the existing stores and export people only."""
import hashlib
import json
from pathlib import Path
from PIL import Image
from interaction_subjects import SubjectRegistry
from human_portraits import HumanPortraitStore

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'cat_cafe_sim/saves/content_images'
SOURCE = ROOT / 'Docs/Prototypes/FujiwaraIcon-v1/fujiwara-icon.png'
OUT = ROOT / 'Logs/FujiwaraIcon'
NOTES = '役割：猫カフェを訪れる藤原さん。白猫が好みというゲーム上の確定設定を保持する。性格：落ち着いて率直で、猫の小さな動きに関心を向ける。接し方：短く穏やかに一度誘い、猫が選ぶ間を待つ。近づかない猫を追わず休憩を尊重する。口調：簡潔な自然な丁寧語。「〜してみますか」「ここで待ちましょう」「ゆっくりで大丈夫です」。猫の呼び方は {cat_call}。命令・幼児語・猫の気持ちや結果の断定を避ける。接客の成功条件は性格の厳しさや高慢さを意味しない。年齢・性別の文章設定・職業・来歴・来店歴を追加しない。外見：片耳にかけた顎丈のストレートなアッシュブラウンのボブ、横分け、眼鏡なし。くすんだプラム色のハイネック、象牙色の襟縁。横長のアーモンド形に開いた目、小さな瞳はやや横向き、力を抜いた水平寄りの眉、小さく水平に閉じた口で静かに関心を向ける平常の表情。'
ROW = dict(id='person-advanced-white', kind='speech', name='藤原さん', game_id='advanced-white',
           content_id='', notes=NOTES, call='{cat}', enabled=True)

def main():
    OUT.mkdir(exist_ok=True, parents=True)
    registry = SubjectRegistry(DATA / 'interaction_subjects.json')
    before = registry.read()
    portraits = HumanPortraitStore(DATA)
    previous = portraits.read()['people']
    # Special customers are explicitly identified in presentation labels, not normal guest candidates.
    presentation = json.loads((ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/presentation.json').read_text(encoding='utf-8'))
    assert {r['id']:r['label'] for r in presentation['special_names']}['advanced-white'] == '藤原さん'
    assert not any(r['game_id'] == 'advanced-white' and r['id'] != ROW['id'] for r in before['subjects'])
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
    assert [r for r in current if r['game_id'] != 'advanced-white'] == previous
    folder, warnings = portraits.export(OUT)
    assert not warnings
    exported = json.loads((folder/'human_portraits.json').read_text(encoding='utf-8'))
    assert exported['people'] == current and len(current) == 14
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
