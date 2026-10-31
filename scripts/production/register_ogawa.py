"""Register Ogawa through the existing stores and export people only."""

from pathlib import Path as _Path
import sys as _sys
TOOL_ROOT = _Path(__file__).resolve().parents[2]
if str(TOOL_ROOT) not in _sys.path:
    _sys.path.insert(0, str(TOOL_ROOT))

import hashlib
import json
from pathlib import Path
from PIL import Image
from interaction_subjects import SubjectRegistry
from human_portraits import HumanPortraitStore

ROOT = TOOL_ROOT.parent
DATA = ROOT / 'cat_cafe_sim/saves/content_images'
SOURCE = ROOT / 'Docs/Prototypes/OgawaIcon-v1/ogawa-icon.png'
OUT = ROOT / 'Logs/OgawaIcon'
NOTES = '役割：祭りの日に来店する臨時客。直近の開発観戦patronルート41日目の会話で customer_id=event-guest-41 と小川さんの本文を照合。今回採用した外見：短い波打つ暗褐色の髪、眼鏡なし、赤茶の上着と生成りのシャツ、開いた目と穏やかな閉じ口。外見のみの制作設定。年齢・職業・来歴・趣味・固有口調は追加しない。既存の来店条件・数値・過去本文は変更しない。'
ROW = dict(id='person-event-guest-41', kind='speech', name='小川さん', game_id='event-guest-41',
           content_id='', notes=NOTES, call='{cat}', enabled=True)

def main():
    OUT.mkdir(exist_ok=True, parents=True)
    registry = SubjectRegistry(DATA / 'interaction_subjects.json')
    before = registry.read()
    portraits = HumanPortraitStore(DATA)
    previous = portraits.read()['people']
    # Event visitor name derives from the stable ID hash; verify it without assigning by display name.
    presentation = json.loads((ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/presentation.json').read_text(encoding='utf-8'))
    assert presentation['extra_names'][int.from_bytes(hashlib.sha256(b'event-guest-41').digest(), 'big') % len(presentation['extra_names'])] == '小川'
    assert not any(r['game_id'] == 'event-guest-41' and r['id'] != ROW['id'] for r in before['subjects'])
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
    assert [r for r in current if r['game_id'] != 'event-guest-41'] == [r for r in previous if r['game_id'] != 'event-guest-41']
    folder, warnings = portraits.export(OUT)
    assert not warnings
    exported = json.loads((folder/'human_portraits.json').read_text(encoding='utf-8'))
    assert exported['people'] == current and len(current) == 25
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
