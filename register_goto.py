"""Register Goto through the existing stores and export people only."""
import hashlib
import json
from pathlib import Path
from PIL import Image
from interaction_subjects import SubjectRegistry
from human_portraits import HumanPortraitStore

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'cat_cafe_sim/saves/content_images'
SOURCE = ROOT / 'Docs/Prototypes/GotoIcon-v1/goto-icon.png'
OUT = ROOT / 'Logs/GotoIcon'
NOTES = '役割：三毛猫との特別な時間を望むVIP客の後藤さん。既存確定設定は名前・VIP接客・三毛猫の好み。今回新しく採用した制作設定：50代後半の男性。小さな画廊を長年営み、気に入った作品には値札より先に愛称を付ける裕福な目利き。審美眼に絶対の自信があり、少し気取って芝居がかった物言いをする。核は「猫にだけは鑑定眼が甘くなる審美家」。三毛模様の不揃いを唯一無二と愛し、そっぽを向かれても「その気位、実にいい」と得意げに喜ぶ。気難しさより誇らしさと猫への甘さを前に出す。口調：歯切れのよい気取った常体。「ほう、見事だ」「その気位、実にいい」「本日の主役は君だな」。一人称は私。猫の呼び方は {cat_call}。失敗や休憩では「ふむ、鑑賞の時間としよう」と自分の方針を切り替える。例はプロフィール用で、既存セリフ本文へは追加しない。外見：銀灰色の髪を大きな波で後ろへ流し、濃灰色のこめかみと広い額、面長で鼻筋の通った年輪のある顔。眼鏡・髭なし。深い葡萄色のテーラードジャケット、生成りシャツ、黄土色のクラバット。表情：顔をわずかに斜めに向け顎を上げ、片眉を高く、細い自信のある目と片側の口角を上げた閉じ口の笑み。既存の正面の聞き顔から差別化する。VIPやcalicoを創作の手掛かりとして積極的に使用した新規設定であり、ゲームの登場・接客条件や数値は変えない。'
ROW = dict(id='person-vip-calico', kind='speech', name='後藤さん', game_id='vip-calico',
           content_id='', notes=NOTES, call='{cat}君', enabled=True)

def main():
    OUT.mkdir(exist_ok=True, parents=True)
    registry = SubjectRegistry(DATA / 'interaction_subjects.json')
    before = registry.read()
    portraits = HumanPortraitStore(DATA)
    previous = portraits.read()['people']
    # Special customers are explicitly identified in presentation labels, not normal guest candidates.
    presentation = json.loads((ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/presentation.json').read_text(encoding='utf-8'))
    assert {r['id']:r['label'] for r in presentation['special_names']}['vip-calico'] == '後藤さん'
    assert not any(r['game_id'] == 'vip-calico' and r['id'] != ROW['id'] for r in before['subjects'])
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
    assert [r for r in current if r['game_id'] != 'vip-calico'] == previous
    folder, warnings = portraits.export(OUT)
    assert not warnings
    exported = json.loads((folder/'human_portraits.json').read_text(encoding='utf-8'))
    assert exported['people'] == current and len(current) == 19
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
