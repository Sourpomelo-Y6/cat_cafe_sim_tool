"""Extend prior traces with unused facility rules; preserve expectations and balance.

Usage: extend_facility_fixtures.py OUTPUT_DIRECTORY NEW_CASES PRIOR_JSON_OR_ZIP ...
The last input collection must include dispatch-cases.json or be a dispatch JSON.
"""
import copy,json,sys,zipfile
from pathlib import Path
from generate_reference import ROOT
from export_standalone_rules import exported_rules

def extend(value,rules):
 if isinstance(value,dict):
  if 'cats' in value and 'exchange' in value and 'rules_id' in value:
   for key in ('rest_space','rest_space_upgrade','rest_space_soundproof','waiting_area','waiting_area_upgrade'):
    if key not in value:value[key]=copy.deepcopy(rules[key])
  for row in value.values():extend(row,rules)
 elif isinstance(value,list):
  for row in value:extend(row,rules)

def main():
 output=Path(sys.argv[1]);output.mkdir(parents=True,exist_ok=True);rules=exported_rules();inputs=[]
 for path in sys.argv[3:]:
  source=Path(path)
  if source.suffix=='.zip':
   with zipfile.ZipFile(source) as archive:
    names=[name for name in archive.namelist() if name.endswith('.json')]
    names.sort(key=lambda name:name.endswith('dispatch-cases.json'))
    inputs.extend((Path(name).name,json.loads(archive.read(name).decode('utf-8'))) for name in names)
  else:inputs.append((source.name,json.loads(source.read_text(encoding='utf-8'))))
 for index,(name,data) in enumerate(inputs):
  extend(data,rules)
  if index==len(inputs)-1:data['cases'].extend(json.loads(Path(sys.argv[2]).read_text(encoding='utf-8'))['cases'])
  destination=output/name;destination.write_text(json.dumps(data,ensure_ascii=False),encoding='utf-8');print(destination)

if __name__=='__main__':main()
