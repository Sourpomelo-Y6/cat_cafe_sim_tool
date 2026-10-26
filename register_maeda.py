"""Register Maeda through the existing stores and export people only."""
import hashlib
import json
from pathlib import Path
from PIL import Image
from interaction_subjects import SubjectRegistry
from human_portraits import HumanPortraitStore

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'cat_cafe_sim/saves/content_images'
SOURCE = ROOT / 'Docs/Prototypes/MaedaIcon-v1/maeda-icon.png'
OUT = ROOT / 'Logs/MaedaIcon'
NOTES = '役割：5席目の増設翌日から刻6に来店する前田さん。既存確定設定は名前・ゲーム客ID・増設来店条件。今回新しく採用した制作設定：38歳の女性で、地方の小さな出版社に勤める紀行編集者。核は「旅の失敗談まで見どころに変える辛口な観察者」。予定どおりの旅より、道に迷って見つけた食堂の話を熱く語る。取材記事の余白に猫の行動地図を描くのが趣味。初めての海外取材で乗り過ごした駅の猫に会って以来、脱線を楽しむようになった。短く言い切る口調で、感想は具体的。少し皮肉屋だが、失敗や猫の拒否を責めず、自分の予想が外れたことを面白がる。一人称は私、猫は {cat_call} と呼ぶ。例「ふむ、そのルートを選ぶか」「見事に予想外。採用」「隊長、今日はここを拠点にする？」。休憩には「了解、取材はここまで」と切り上げる。例文は制作資料だけで既存ゲーム本文へ追加しない。外見：丸みのあるやや広い顔、暗い赤茶の非対称な短いピクシーカット、長めの横流し前髪と片耳出し、眼鏡・帽子なし。焦げた黄土色のアウトドアシャツと炭灰色の丸首。顔を少し左へ向け顎を引き、半眼の観察する目、少し寄せた細眉、小さく結び片端だけわずかに上げた閉じ口で、好奇心を含んだ「ふむ」を示す。笑顔・歯・太眉の反復を避ける。年代・性別・職業・来歴・性格・服装は今回採用した創作設定であり、ゲームの既存事実ではない。条件・数値・本文は維持。'
ROW = dict(id='person-expanded-guest-2', kind='speech', name='前田さん', game_id='expanded-guest-2',
           content_id='', notes=NOTES, call='{cat}隊長', enabled=True)

def main():
    OUT.mkdir(exist_ok=True, parents=True)
    registry = SubjectRegistry(DATA / 'interaction_subjects.json')
    before = registry.read()
    portraits = HumanPortraitStore(DATA)
    previous = portraits.read()['people']
    # Special customers are explicitly identified in presentation labels, not normal guest candidates.
    presentation = json.loads((ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/presentation.json').read_text(encoding='utf-8'))
    assert {r['id']:r['label'] for r in presentation['special_names']}['expanded-guest-2'] == '前田さん'
    assert not any(r['game_id'] == 'expanded-guest-2' and r['id'] != ROW['id'] for r in before['subjects'])
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
    assert [r for r in current if r['game_id'] != 'expanded-guest-2'] == [r for r in previous if r['game_id'] != 'expanded-guest-2']
    folder, warnings = portraits.export(OUT)
    assert not warnings
    exported = json.loads((folder/'human_portraits.json').read_text(encoding='utf-8'))
    assert exported['people'] == current and len(current) == 21
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
