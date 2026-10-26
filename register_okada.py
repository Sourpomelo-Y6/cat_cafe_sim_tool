"""Register Okada through the existing stores and export people only."""
import hashlib
import json
from pathlib import Path
from PIL import Image
from interaction_subjects import SubjectRegistry
from human_portraits import HumanPortraitStore

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'cat_cafe_sim/saves/content_images'
SOURCE = ROOT / 'Docs/Prototypes/OkadaIcon-v1/okada-icon.png'
OUT = ROOT / 'Logs/OkadaIcon'
NOTES = '役割：4席増設の翌日から来店する岡田さん。既存確定設定は名前・ゲーム客ID・増設来店条件。今回新しく採用した制作設定：40代前半の男性で、商店街の小さな修理店を継いだ修理職人。壊れた物の直し方も休日の予定もすぐ段取りにしてしまう。核は「つい段取りを組むが、猫には予定を崩されて嬉しい修理職人」。遊びにもつい順番を考えるが、猫が別の物へ向くと「おっと、そっちか！」と声を弾ませ、予定変更を面白がる。せっかちで快活、話は短く歯切れよい。猫の無反応を責めず「よし、作戦変更だ」と自分の計画を直す。一人称は俺、呼び方は {cat_call}。例「まずは一振り。どうだ？」「おっと、そっちか！」「よし、休憩も段取りのうちだ」。例文は人物資料用で既存ゲーム本文へ追加しない。若い頃は父の修理手順をノートに写して覚え、今も趣味で古いラジオを直す。外見：短い黒髪と小さな立ち上がり、日に焼けた角張った顔、眼鏡・髭なし、深い青緑の作業上着と生成りの襟付きシャツ。表情：ほぼ正面から少し右向き、両方の太い眉を勢いよく上げ、細めの開眼と上の歯が見える大きく開いた笑い口。銀髪・片眉・得意げな閉じ口を避け、予定外を楽しむ快活さを示す。年代・職業・来歴・性格・服装は今回採用した創作設定であり、ゲーム条件や数値からの既存事実ではない。登場条件・数値・本文は変更しない。'
ROW = dict(id='person-expanded-guest', kind='speech', name='岡田さん', game_id='expanded-guest',
           content_id='', notes=NOTES, call='{cat}親方', enabled=True)

def main():
    OUT.mkdir(exist_ok=True, parents=True)
    registry = SubjectRegistry(DATA / 'interaction_subjects.json')
    before = registry.read()
    portraits = HumanPortraitStore(DATA)
    previous = portraits.read()['people']
    # Special customers are explicitly identified in presentation labels, not normal guest candidates.
    presentation = json.loads((ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/presentation.json').read_text(encoding='utf-8'))
    assert {r['id']:r['label'] for r in presentation['special_names']}['expanded-guest'] == '岡田さん'
    assert not any(r['game_id'] == 'expanded-guest' and r['id'] != ROW['id'] for r in before['subjects'])
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
    assert [r for r in current if r['game_id'] != 'expanded-guest'] == [r for r in previous if r['game_id'] != 'expanded-guest']
    folder, warnings = portraits.export(OUT)
    assert not warnings
    exported = json.loads((folder/'human_portraits.json').read_text(encoding='utf-8'))
    assert exported['people'] == current and len(current) == 20
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
