"""Add unused rest rules to prior traces; preserve all old expectations and coefficients.

Usage: extend_rest_space_fixtures.py OUTPUT_DIRECTORY NEW_CASES PRIOR_JSON ...
The last prior JSON is the dispatch collection, to which new cases are appended.
"""
import copy,json,sys
from pathlib import Path
from generate_reference import ROOT
from export_standalone_rules import exported_rules

def extend(value,rules):
 if isinstance(value,dict):
  if 'cats' in value and 'exchange' in value and 'rules_id' in value:
   for key in ('rest_space','rest_space_upgrade','rest_space_soundproof'):
    if key not in value:value[key]=copy.deepcopy(rules[key])
  for row in value.values():extend(row,rules)
 elif isinstance(value,list):
  for row in value:extend(row,rules)

if __name__=='__main__':
 output=Path(sys.argv[1]);output.mkdir(parents=True,exist_ok=True);rules=exported_rules()
 for index,path in enumerate(sys.argv[3:]):
  source=Path(path);data=json.loads(source.read_text(encoding='utf-8'));extend(data,rules)
  if index==len(sys.argv[3:])-1:data['cases'].extend(json.loads(Path(sys.argv[2]).read_text(encoding='utf-8'))['cases'])
  destination=output/source.name;destination.write_text(json.dumps(data,ensure_ascii=False),encoding='utf-8');print(destination)
