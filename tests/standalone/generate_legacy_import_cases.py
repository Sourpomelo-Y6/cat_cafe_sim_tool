"""Generate actual standard Python saves and independent continuation expectations."""
import json, sys
from pathlib import Path
from generate_reference import ROOT
from generate_patron_cases import expected as patron_expected
from generate_expansion_cases import expected as goal_expected
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import create_game, starting_conditions
from cat_cafe_sim.storage.cafe_saves import save_game
from convert_python_save import convert, load_readonly, sha

def expected(session):
    result=patron_expected(session);result.update(goal_expected(session))
    if session.core.goal.get('tracking_only'):result['goal']=[]
    from cat_cafe_sim.core import cafe_player
    p=cafe_player.state(session.core)
    result['player_cats']=[dict(cat_id=key,affinity=p['affinity'][key],today=p['today'][key],total=p['total'][key]) for key in session.core.cats]
    return result

def generate(directory,extended=False):
    directory=Path(directory).resolve();directory.mkdir(parents=True,exist_ok=True);cases=[]
    for mode in ('free','popularity','bond','patron'):
        for stage in (('recruitment','recruited','shop','housing','rest','equipment','item','dispatch') if extended else ('preparation','open','closed','next-day','player-active','player-done')):
            folder=directory/(mode+'-'+stage);folder.mkdir(exist_ok=True)
            session=create_game(folder/'source',starting_conditions(mode),seed=42)
            if stage in ('recruitment','recruited'):
                session.open_recruitment()
                if stage=='recruited':session.recruit_cat(next(iter(session.core.recruitment['candidates'])))
            elif stage=='shop':session.purchase_cat(next(iter(session.core.pet_shop['candidates'])))
            elif stage=='housing':session.purchase_housing()
            elif stage=='rest':session.purchase_rest_space();session.set_shifts(['cat-mike','cat-mugi'])
            elif stage=='equipment':
                from cat_cafe_sim.core.cafe_seat_equipment import catalog
                session.purchase_seat_equipment('seat-1',catalog()[0])
            elif stage=='item':session.purchase_item()
            elif stage=='dispatch':session.dispatch('cat-sora')
            elif stage in ('player-active','player-done'):
                session.play_with_player('cat-mike');session.player_command('direct')
                if stage=='player-done':session.player_command(finish=True)
            elif stage!='preparation':
                while not session.core.closed:
                    session.automatic_step()
                    if stage=='open' and session.core.tick==11:break
                if stage=='next-day':session.next_day()
            source=save_game(session,folder/'python.json',auto_assign=True)
            relation=session.store.path;hashes=(sha(source),sha(relation))
            bundle=convert(source,folder/'import.json');assert hashes==(sha(source),sha(relation))
            session,_,_=load_readonly(source);case=dict(variant=mode+'-'+stage,rules=exported_rules(),bundle=str(bundle),operations=[]);cases.append(case)
            def record(kind,**kwargs):
                if kind=='player_finish':session.player_command(finish=True)
                elif kind=='next_day':session.next_day()
                elif kind=='resolve_activity':session.resolve_activity(kwargs['source'])
                else:session.automatic_step()
                case['operations'].append(dict(kind=kind,**kwargs,expected=expected(session)))
            if stage=='player-active':record('player_finish')
            if session.core.closed:record('next_day')
            while not session.core.closed:
                record('start_business' if session.core.can_set_shifts else 'advance_business')
            for event in (session.core.activities or {}).get('events',{}).values():
                if event['status']=='waiting':record('resolve_activity',source=event['id'],choice='receive')
            record('next_day')
            assert hashes==(sha(source),sha(relation))
            print(case['variant'],len(case['operations']),flush=True)
    return dict(cases=cases)

if __name__=='__main__':
    directory=Path(sys.argv[1]);(directory/'cases.json').write_text(json.dumps(generate(directory,"--extended" in sys.argv),ensure_ascii=False,allow_nan=False),encoding='utf-8')
