"""Aggregate bounded existing Player logs. Never estimates reading time or missing UI calls."""
import bisect
import json
from collections import Counter
from pathlib import Path
import sys
root=Path(sys.argv[1]); dest=Path(sys.argv[2]); dest.mkdir(parents=True,exist_ok=True)
commands=[json.loads(line) for line in (root/'natural/commands.jsonl').read_text(encoding='utf-8-sig').splitlines()]
times=[r['utc'] for r in commands]
actions=[]
for line in (root/'natural/ui-actions.txt').read_text(encoding='utf-8-sig').splitlines():
    stamp,label=line.split(' ',1); i=max(0,bisect.bisect_right(times,stamp)-1)
    actions.append({'utc':stamp,'day':commands[i]['day'],'label':label})
days=[]
for day in (4,5,8):
    selected=[a for a in actions if a['day']==day]
    days.append({'day':day,'logged_actions':dict(Counter(a['label'] for a in selected)),
                 'logged_ui_callbacks':sum(' / ' in a['label'] for a in selected),
                 'direct_route_calls':sum(a['label'].startswith('required route') for a in selected),
                 'direct_start_autoplay':sum(a['label'].startswith('autoplay fast') for a in selected)})
events=[];last_reason=''
for r in commands:
    try:c=json.loads(r['command'])
    except (TypeError,ValueError):continue
    if r['autoplay']=='blocked' and last_reason!='blocked':
        events.append({'day':r['day'],'kind':'blocked-run-end','requested':c['kind']})
    last_reason=r['autoplay']
    if c['kind'] not in ('step_autoplay','advance_business'):
        events.append({'day':r['day'],'kind':c['kind'],'choice':c.get('choice'),'event_id':c.get('event_id'),'error':r['error']})
result={'scope':'standard rules seed 0 popularity fast; days 1-8, stopped at first day 9 observation; no objective completion',
        'natural_days':days,'commands':len([r for r in commands if str(r['command']).startswith('{')]),'errors':max(r['errors'] for r in commands),
        'events':events,'limitations':['ui-actions omits fallback d.Next and direct list/method calls; UI callback counts are lower bounds, not physical clicks','requested step may become effective pause; old observer omits paused flag; cannot reconstruct every transient pause','no reading time measured','headless UI listeners do not establish rendered layout']}
(dest/'natural-summary.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
(dest/'natural-actions.json').write_text(json.dumps(actions,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2))
