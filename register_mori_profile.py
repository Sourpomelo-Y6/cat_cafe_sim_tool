"""Fill Mori's missing notes via the existing Tool registry; never export assets."""
import hashlib
import json
import subprocess
from pathlib import Path

from interaction_subjects import SubjectRegistry, expand
from interaction_llm import InteractionLlmSettings, validate_inputs
from interaction_texts import condition
from human_portraits import HumanPortraitStore

ROOT = Path(__file__).resolve().parent.parent
OUT = Path(__file__).parent / 'Docs/Results/MoriProfile'
DATA = ROOT / 'cat_cafe_sim/saves/content_images'
SID = 'person-reservation-longhair'
NOTES = '役割：長毛猫との時間を望む予約客、森さん。名前・ゲーム客ID・長毛猫の担当と心を開く回数による予約条件は既存定義。今回採用した制作設定：56歳の男性、商店街の服地店主。核は「毛並みの話になると、物静かな顔のまま急に熱が入る素材好き」。普段は簡潔で実務的だが、長毛の揺れや光の当たり方に目が留まると、生地の風合いを語るように言葉が増える。触り心地を確かめたい好奇心は強いが、猫を品物として採点せず、触れることも予約の対価として要求しない。猫が離れると少し名残惜しそうに手を引き、眺める楽しみに切り替える。短い自然な丁寧語「〜ですね」「〜でしょうか」「これは見事ですね」。一人称は私。猫への呼びかけは{cat_call}、呼び名は猫の名前に「さん」を付ける。猫を先生や職人に見立てる口調にはしない。家業の服地店を継ぎ、衣服を作る客に素材を提案してきた。趣味は古い織物の見本帳集めと、季節ごとの布で小さなクッションカバーを縫うこと。店の休みに長毛猫との時間を楽しみたくて予約するが、過去の来店歴・飼育歴・特定の猫との関係は固定しない。外見は既存採用画像の銀灰色の短髪、額を見せた流し髪、太めの眉、開いた両目、閉じた口の微笑み、眼鏡・ひげなし、紺のカーディガンとクリーム色の襟を維持する。年齢・職業・来歴・趣味・性格・口調・呼称と予約の個人的動機は今回採用した創作設定。毛並みへの関心を中心にし、すべての発話を待つ・休む誘いへ寄せない。セリフ制作時は一案一発言、目安10〜35字。未確認の画像の仕草・小道具、猫の気持ち・成功・回復は断定しない。拒否・低体力は尊重する。既存会話・条件・数値は変更しない。'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def capacity():
    paths = {'Logs': ROOT/'Logs', 'Latest': ROOT/'Logs/StandaloneBuild/Latest',
             'Library': Path('C:/Users/hi-wa/unity_workspase/CatCafeSim/Library'), 'content_images': DATA}
    return {key: sum(p.stat().st_size for p in path.rglob('*') if p.is_file()) for key, path in paths.items()}


def dirty():
    result = {}
    for repo in (ROOT/'cat_cafe_sim', ROOT/'cat_cafe_sim_tool', ROOT/'Docs', Path('C:/Users/hi-wa/unity_workspase/CatCafeSim')):
        names = subprocess.check_output(['git', '-C', str(repo), 'ls-files', '--modified', '--others', '--exclude-standard', '-z']).decode().split('\0')
        for name in names:
            path = repo/name
            if name and path.is_file():
                result[str(path)] = sha(path)
    return result


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    start = capacity()
    prior = dirty()
    registry = SubjectRegistry(DATA/'interaction_subjects.json')
    before = registry.read()
    row = registry.get(SID)
    assert row['game_id'] == 'reservation-longhair' and row['name'] == '森さん' and row['enabled']
    assert not row['notes'] or row['notes'] == NOTES, 'Already filled differently: do not overwrite.'
    portraits = HumanPortraitStore(DATA).read()
    protected = {str(p): sha(p) for p in DATA.rglob('*') if p.is_file() and p != registry.path}
    row.update(notes=NOTES, call='{cat}さん')
    registry.save(row, expected=before)
    reopened = SubjectRegistry(registry.path)
    assert reopened.get(SID) == row
    assert {r['id']: r for r in reopened.read()['subjects'] if r['id'] != SID} == {r['id']: r for r in before['subjects'] if r['id'] != SID}
    assert all(sha(Path(p)) == digest for p, digest in protected.items())
    assert HumanPortraitStore(DATA).read() == portraits
    settings = InteractionLlmSettings(subject_registry=reopened)
    checked = []
    for mode, actions in [('teaser', ['any', 'direct', 'adapt', 'intense', 'feint']), ('pet', ['any', 'direct', 'adapt']), ('pause', ['pause'])]:
        for action in actions:
            value = condition('speech', SID, mode=mode, action=action, target_cat='*', subjects=reopened.labels())
            material = settings.get(value)
            assert material['subject_notes'] == NOTES
            validate_inputs(material['subject_notes'], material['image_notes'], material['style'])
            checked.append(value)
    assert expand('{cat_call}', '対象猫', row['call']) == '対象猫さん'
    inventory = json.loads((OUT.parent/'UedaIcon/fixed-24-inventory.json').read_text(encoding='utf-8'))
    for person in inventory['people']:
        subject = reopened.get(person['subject_id'])
        portrait = next(p for p in portraits['people'] if p['subject_id'] == subject['id'])
        assert subject['game_id'] == portrait['game_id'] == person['game_id']
        assert sha(DATA/portrait['image']) == person['image_sha256']
        person['profile_notes'] = bool(subject['notes'].strip())
        person['registered'] = subject['enabled']
    inventory.update(status='PASS', nonempty_profile_notes=sum(p['profile_notes'] for p in inventory['people']))
    assert inventory['nonempty_profile_notes'] == 24
    after = dirty()
    preserved = {p: after.get(p) == digest for p, digest in prior.items() if p != str(registry.path)}
    assert all(preserved.values())
    result = dict(status='PASS', subject=row, other_subjects_unchanged=True, portrait_registry_and_assets_unchanged=True,
                  protected_files=len(protected), prior_differences_preserved=preserved, generation_inputs=checked,
                  llm_requests=0, scope='Existing SubjectRegistry.save API and separate-instance reload; no GUI input, export, Unity import or build',
                  capacity_start=start, capacity_end=capacity())
    for name, data in [('registration.json', result), ('fixed-24-inventory.json', inventory)]:
        (OUT/name).write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print('PASS: saved/reloaded, 9 generation inputs, 24 profiles/IDs/images, other data preserved')


if __name__ == '__main__':
    main()
