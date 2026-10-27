"""現行設定・個性を変更せず通常反応の短い到達例を記録する。"""
import json
from dataclasses import replace
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'cat_cafe_sim'))
from cat_cafe_sim.core.human_cat_types import TypesConfig, TypesInteraction, load_presets

OUT = Path(__file__).parent / 'Docs/Results/TamaNormalNarratives'
MODES = ('ball', 'plush', 'tunnel', 'brush', 'voice', 'presence')

def main():
    config = replace(TypesConfig.load(), personality=load_presets()['活発な探検家'])
    witnesses = {}
    for mode in MODES:
        # 切替倍率、連続減衰、負反応後の穏やかなadapt、低体力を実stepで確認。
        for stamina in (100, 35, 15):
            for setup in ([], [('switch', 'voice'), ('direct', None)], [('direct', None)]):
                for action in config.types[('teaser','ball','plush','tunnel','pet','brush','voice','presence').index(mode)].actions:
                    core = TypesInteraction(config, stamina=stamina)
                    trace = []
                    plan = setup + [('switch', mode)] + [(action, None)] * 12
                    for a, target in plan:
                        if a == 'switch' and target == core.state['mode']: continue
                        if a not in core.valid_actions(): break
                        record = core.step(a, target)
                        trace.append(dict(action=a, target=target))
                        if a in ('direct','adapt','intense','feint') and record['before']['mode'] == mode and record['cat_action'] == 'normal':
                            key = mode + '/' + record['reaction']
                            value = dict(initial_stamina=stamina, actions=list(trace), record=record)
                            if key not in witnesses or len(trace) < len(witnesses[key]['actions']): witnesses[key] = value
        # pauseは「開始時」の体力でlistlessを記録した後に回復する。
        # 20を越えた直後のadaptはgentle扱いとなり、遊びでも戸惑い得る。
        core = TypesInteraction(config, stamina=35)
        trace = []
        def step(a, target=None):
            result = core.step(a, target); trace.append(dict(action=a, target=target)); return result
        step('switch', mode)
        while core.state['stamina'] > 20 and not core.state['end_reason']: step('direct')
        while core.state['stamina'] <= 20 and not core.state['end_reason']: step('pause')
        if 'adapt' in core.valid_actions():
            record = step('adapt')
            if record['cat_action'] == 'normal':
                key = mode+'/'+record['reaction']
                value = dict(initial_stamina=35, actions=list(trace), record=record)
                if key not in witnesses or len(trace) < len(witnesses[key]['actions']): witnesses[key] = value
    expected = dict(ball=['neutral','favorable','enthusiastic','confused','listless'],
                    plush=['neutral','favorable','confused','listless'],
                    tunnel=['neutral','favorable','enthusiastic','confused','listless'],
                    brush=['neutral','confused','listless'], voice=['neutral','confused','listless'],
                    presence=['confused','listless'])
    assert set(witnesses) == {m+'/'+r for m, rs in expected.items() for r in rs}, set(witnesses)
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'reachability.json').write_text(json.dumps(dict(config=config.to_dict(), witnesses=witnesses,
        scope='実ルールの限定到達例。自然プレイ頻度、全セーブ、将来の設定変更は未確認。'), ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    for k, v in witnesses.items(): print(k, v['record']['diagnostic']['score'], v['initial_stamina'], v['actions'])

if __name__ == '__main__': main()
