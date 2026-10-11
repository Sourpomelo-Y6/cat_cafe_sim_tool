"""Locate remaining differences from full popularity mode with normal goal targets and store events disabled on both sides."""
import json,sys,tempfile
from pathlib import Path
from generate_reference import ROOT
from generate_expansion_cases import expected
from compare_full_customer_progress import differences
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions

def compare(data, exclude_unported=False):
    fixture=next(c for c in data['cases'] if c['objective']=='popularity' and c['rules']['starting_funds']==1000);rows=fixture['operations']
    with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
        selected=starting_conditions('popularity');selected['store_events']['probability']=0
        full=create_game(Path(temp),selected,seed=0)
        if exclude_unported:
            for name in ("quiet_customer","play_customer","contact_customer","longhair_customer","vip_customer"):setattr(full.core,name,None)
        report=dict(exclude_unported=exclude_unported,conditions='Normal costs/goal targets, seed 0, store event probability 0 on both sides; full popularity enables specialised customers.',equal_operations=0,first_difference=None)
        for op in rows:
            kind=op['kind'];failed=False
            try:
                if kind=='set_shifts':full.set_shifts(op['working'])
                elif kind=='resolve_missing':full.resolve_missing(next(e['id'] for e in full.core.management['events'].values() if e['cat_id']==op['cat_id'] and e['status']=='waiting'))
                elif kind=='resolve_customer_trust':full.core.resolve_customer_trust(op['source'],op['choice'])
                elif kind.startswith('resolve_growth'):getattr(full.core,kind)(op['cat_id'],op['choice'])
                elif kind=='resolve_intake':full.resolve_intake_request(op['choice'])
                elif kind=='resolve_regular_introduction':full.resolve_regular_introduction(op['choice'])
                elif kind=='resolve_reservation':full.resolve_reservation(op['choice'])
                elif kind in ('expand_seats','continue_goal','advance_goal','purchase_housing'):getattr(full.core,kind)()
                elif kind=='next_day':full.next_day()
                elif kind=='day_off':full.day_off()
                else:full.automatic_step()
            except ValueError as error:failed=True
            delta=differences(op['expected'],expected(full))
            if failed!=op['rejected'] or delta:
                report['first_difference']=dict(day=full.core.day,tick=full.core.tick,command=kind,rejection_difference=failed!=op['rejected'],full_arrivals=[v.id for v in full.core.visits.values()],differences=delta[:12]);break
            report['equal_operations']+=1
        return report
if __name__=='__main__':
    result=compare(json.loads(Path(sys.argv[1]).read_text(encoding='utf-8')),exclude_unported=len(sys.argv)>3);Path(sys.argv[2]).write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(sys.argv[2])
