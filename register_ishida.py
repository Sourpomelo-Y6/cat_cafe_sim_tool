"""Register Ishida through the existing stores and export people only."""
import hashlib
import json
from pathlib import Path
from PIL import Image
from interaction_subjects import SubjectRegistry
from human_portraits import HumanPortraitStore

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'cat_cafe_sim/saves/content_images'
SOURCE = ROOT / 'Docs/Prototypes/IshidaIcon-v1/ishida-icon.png'
OUT = ROOT / 'Logs/IshidaIcon'
NOTES = '役割：7席目の増設翌日から刻18に来店する石田さん。既存確定設定は名前・ゲーム客ID・増設来店条件。今回採用した制作設定：46歳の男性、出張ピアノ調律師。核は「音の違いにうるさいが、猫の不協和音には弱い人」。仕事では僅かなうなりも聞き分ける几帳面さ。猫の掠れた声や気まぐれな足音には調律不要、と真顔で降参する。短く拍を取るように区切り、乾いたユーモアを交える。一人称は僕、猫は{cat_call}と呼ぶ。例「一拍、二拍。先生、そこで休符？」「その声は直さない。味がある」「本日の演奏、ここまで」。猫を鳴かせようと追い立てず、沈黙も休符として受け取る。音大の舞台裏で調律を手伝ったのをきっかけに職人へ。趣味は口笛で古い映画音楽を奏でることと真空管ラジオの修理。来店は仕事の合間の耳休めだが、つい音を採集してしまう。外見は長めの角張った顔と通った鼻、茶の横分け髪にこめかみの太い白髪一房、眼鏡・ひげ・帽子なし。生成りのバンドカラーシャツと深い藍色のベスト、銅色の音叉ピン。正面寄りに少し傾けた顔、片目をほぼ閉じて片目を細く開け、低めの直線的な眉を少し寄せ、小さくすぼめた閉じ口で口笛の音を聴き分ける集中を表す。採用画像の眉は完全な水平でなく角度と左右差がある。微笑み・歯・感嘆の開き口にしない。職業・年代・性別・趣味・来歴・性格・服装・口調は今回採用した創作設定。条件・数値・既存本文は変更しない。例文は人物資料のみ。'
ROW = dict(id='person-expanded-guest-4', kind='speech', name='石田さん', game_id='expanded-guest-4',
           content_id='', notes=NOTES, call='{cat}先生', enabled=True)

def main():
    OUT.mkdir(exist_ok=True, parents=True)
    registry = SubjectRegistry(DATA / 'interaction_subjects.json')
    before = registry.read()
    portraits = HumanPortraitStore(DATA)
    previous = portraits.read()['people']
    # Special customers are explicitly identified in presentation labels, not normal guest candidates.
    presentation = json.loads((ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/presentation.json').read_text(encoding='utf-8'))
    assert {r['id']:r['label'] for r in presentation['special_names']}['expanded-guest-4'] == '石田さん'
    assert not any(r['game_id'] == 'expanded-guest-4' and r['id'] != ROW['id'] for r in before['subjects'])
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
    assert [r for r in current if r['game_id'] != 'expanded-guest-4'] == [r for r in previous if r['game_id'] != 'expanded-guest-4']
    folder, warnings = portraits.export(OUT)
    assert not warnings
    exported = json.loads((folder/'human_portraits.json').read_text(encoding='utf-8'))
    assert exported['people'] == current and len(current) == 23
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
