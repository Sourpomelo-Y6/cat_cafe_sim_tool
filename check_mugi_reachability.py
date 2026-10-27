"""現行設定・個性を変更せず通常反応の短い到達例を記録する。"""
import json
from dataclasses import replace
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'cat_cafe_sim'))
from cat_cafe_sim.core.human_cat_types import TypesConfig, TypesInteraction, load_presets

OUT = Path(__file__).parent / 'Docs/Results/MugiNormalNarratives'
MODES = ('teaser', 'pet', 'ball', 'plush', 'tunnel', 'brush', 'voice', 'presence')

def main():
    config = replace(TypesConfig.load(), personality=load_presets()['中立'])
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
        if mode != core.state['mode']: step('switch', mode)
        while core.state['stamina'] > 20 and not core.state['end_reason']: step('direct')
        while core.state['stamina'] <= 20 and not core.state['end_reason']: step('pause')
        if 'adapt' in core.valid_actions():
            record = step('adapt')
            if record['cat_action'] == 'normal':
                key = mode+'/'+record['reaction']
                value = dict(initial_stamina=35, actions=list(trace), record=record)
                if key not in witnesses or len(trace) < len(witnesses[key]['actions']): witnesses[key] = value
    for mode in ('teaser','ball','plush','tunnel'):
        core=TypesInteraction(config,stamina=100); trace=[]
        plan=([('switch',mode)] if mode!='teaser' else [])+[('direct',None),('pause',None),('feint',None)]
        for a,target in plan:
            record=core.step(a,target);trace.append(dict(action=a,target=target))
        assert record['cat_action']=='normal'
        if mode+'/'+record['reaction'] not in witnesses:
            witnesses[mode+'/'+record['reaction']]=dict(initial_stamina=100,actions=trace,record=record)
    for stamina in (100,15):
        core=TypesInteraction(config, stamina=stamina)
        record=core.step('pause')
        witnesses['pause/'+record['reaction']]=dict(initial_stamina=stamina,actions=[dict(action='pause',target=None)],record=record)
    # 現行Unity用ルール・猫定義と、Pythonで使用した数値を現物照合する。
    rules=json.loads((ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json').read_text(encoding='utf-8'))
    cat=next(c for c in rules['cats'] if c['id']=='cat-mugi')
    assert cat['type_preferences']==list(config.personality.type_preferences)
    assert cat['intensity_preferences']==list(config.personality.intensity_preferences)
    assert cat['boredom_decay']==config.personality.boredom_decay and cat['switch_affinity']==config.personality.switch_affinity
    assert all(rules['exchange'][k]==v for k,v in config.to_dict()['rules'].items())
    assert rules['types']==config.to_dict()['types']
    after_rest={}
    for mode in MODES:
        core=TypesInteraction(config,stamina=21); trace=[]; records=[]
        plan=([('switch',mode)] if mode!='teaser' else [])+[('direct',None)]
        for a,target in plan:
            records.append(core.step(a,target));trace.append(dict(action=a,target=target))
        while core.state['stamina']<=20:
            records.append(core.step('pause'));trace.append(dict(action='pause',target=None))
        records.append(core.step('adapt'));trace.append(dict(action='adapt',target=None))
        after_rest[mode]=dict(initial_stamina=21,actions=trace,records=records)
    expected=dict(teaser=5,pet=4,ball=5,plush=4,tunnel=5,brush=4,voice=3,presence=3,pause=2)
    assert len(witnesses)==sum(expected.values())==35
    assert all(sum(k.startswith(m+'/') for k in witnesses)==n for m,n in expected.items())

    from mugi_normal_texts import TEXTS
    assert set(witnesses)=={m+'/'+r for m,r in TEXTS}
    # 全合法操作・コンボ・負反応後adaptを含む保守的得点上限。
    # 減衰 <= 1、切替親和性0、好みはすべて1なので初回得点だけでは除外しない。
    upper_bounds={}
    for t in config.types:
        bases=[getattr(config,a+'_gain') for a in t.actions]
        if 'adapt' in t.actions: bases.append(config.gentle_gain)
        if 'feint' in t.actions: bases.append(config.combo_gain)
        upper_bounds[t.id]=max(bases)*t.gain
    assert all(p>0 for p in config.personality.type_preferences+config.personality.intensity_preferences)
    assert config.personality.switch_affinity==0
    excluded={m+'/turn_away':'動作・強さの好みがすべて1。0好みの拒否条件なし' for m in MODES}
    for m in MODES:
        for r,threshold in [('favorable',config.favorable_threshold),('enthusiastic',config.enthusiastic_threshold)]:
            if m+'/'+r not in witnesses:
                assert upper_bounds[m]<threshold
                excluded[m+'/'+r]=f'全合法操作の得点上限{upper_bounds[m]:g} < {threshold:g}'
    assert len(excluded)==15
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'reachability.json').write_text(json.dumps(dict(config=config.to_dict(), upper_bounds=upper_bounds, excluded=excluded, witnesses=witnesses, after_rest=after_rest, runtime_rules_match=True,
        scope='実ルールの限定到達例。自然プレイ頻度、全セーブ、将来の設定変更は未確認。'), ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    for k, v in witnesses.items(): print(k, v['record']['diagnostic']['score'], v['initial_stamina'], v['actions'])

if __name__ == '__main__': main()
