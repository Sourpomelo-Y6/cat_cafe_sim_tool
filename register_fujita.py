"""Register Fujita through the existing stores and export people only."""
import hashlib
import json
from pathlib import Path
from PIL import Image
from interaction_subjects import SubjectRegistry
from human_portraits import HumanPortraitStore

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'cat_cafe_sim/saves/content_images'
SOURCE = ROOT / 'Docs/Prototypes/FujitaIcon-v1/fujita-icon.png'
OUT = ROOT / 'Logs/FujitaIcon'
NOTES = '役割：6席目の増設翌日から刻12に来店する藤田さん。既存確定設定は名前・ゲーム客ID・増設来店条件。今回採用した制作設定：54歳の女性、小さなボードゲーム店の店主。核は「勝負好きで、猫の一手に感心してしまう人」。負けず嫌いでつい勝負を提案するが、負けを相手の見事な手として大きく褒める。猫が玩具でなく紐の影を追うと、ほう、その手があったか！と感嘆する。一人称はあたし、猫は{cat_call}。短く弾む断定口調で、作戦・一手・参ったをよく使う。猫の無反応は反則や失敗扱いせず、こちらの読み負けと受け止める。例「さあ名人、次の一手は？」「ほう、そっち！ 一本取られた」「参った、今日は名人の勝ち。閉会！」。昔は印刷会社の営業で、出張土産に集めた小さな盤上遊戯をきっかけに独立。趣味は古い双六の収集と詰め将棋。休憩も明るく勝負を畳み、猫を追い回さない。外見は丸くふくよかな頬、短い銀灰色の巻き髪、額を見せる横分け、眼鏡・帽子なし。珊瑚色のカーディガン、生成りのブラウスと紺の小さなスカーフ。顔を少し斜め上へ向け顎をわずかに上げ、丸みのある開眼・高く丸く上がる両眉・感嘆する丸い開き口で、思いつかなかった一手への感心を表す。大きな目と微笑み、半眼と閉じ口、歯を見せる笑顔を繰り返さない。職業・年齢・性別・趣味・来歴・性格・外見・口調は今回の創作設定。ゲーム条件・数値・本文は変更しない。例文は人物資料のみ。'
ROW = dict(id='person-expanded-guest-3', kind='speech', name='藤田さん', game_id='expanded-guest-3',
           content_id='', notes=NOTES, call='{cat}名人', enabled=True)

def main():
    OUT.mkdir(exist_ok=True, parents=True)
    registry = SubjectRegistry(DATA / 'interaction_subjects.json')
    before = registry.read()
    portraits = HumanPortraitStore(DATA)
    previous = portraits.read()['people']
    # Special customers are explicitly identified in presentation labels, not normal guest candidates.
    presentation = json.loads((ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/presentation.json').read_text(encoding='utf-8'))
    assert {r['id']:r['label'] for r in presentation['special_names']}['expanded-guest-3'] == '藤田さん'
    assert not any(r['game_id'] == 'expanded-guest-3' and r['id'] != ROW['id'] for r in before['subjects'])
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
    assert [r for r in current if r['game_id'] != 'expanded-guest-3'] == [r for r in previous if r['game_id'] != 'expanded-guest-3']
    folder, warnings = portraits.export(OUT)
    assert not warnings
    exported = json.loads((folder/'human_portraits.json').read_text(encoding='utf-8'))
    assert exported['people'] == current and len(current) == 22
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
