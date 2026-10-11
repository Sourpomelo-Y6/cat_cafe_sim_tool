"""Isolated original-Python specialist result boundaries; no production rule overrides."""
import itertools,json,sys
from pathlib import Path
from types import SimpleNamespace
from generate_reference import ROOT
from export_standalone_rules import exported_rules
from cat_cafe_sim.core import cafe_longhair_customer as lh,cafe_vip_customer as vip
from cat_cafe_sim.core.cafe_growth import TYPE_GROUPS

def generate():
 r=exported_rules();rows=[]
 for id,module in ((lh.CUSTOMER_ID,lh),(vip.CUSTOMER_ID,vip)):
  for matched,equipment,contact,connect,open_up,simultaneous in itertools.product((False,True),(False,True),(0,2,3,4),(0,1,2),(0,1,2),(0,1,2)):
   if simultaneous>min(connect,open_up):continue
   cat=r['cats'][0]['id'];features=['long_hair' if id==lh.CUSTOMER_ID else 'calico'] if matched else []
   core=SimpleNamespace(longhair_customer=r['longhair_customer'],vip_customer=r['vip_customer'],cat_features={cat:features})
   actions={k:0 for k in TYPE_GROUPS};actions[next(k for k,g in TYPE_GROUPS.items() if g=='contact')]=contact
   result=dict(customer_id=id,cat_id=cat,type_actions=actions,connect_count=connect,open_up_count=open_up,simultaneous_count=simultaneous)
   value=module.evaluate(core,result)
   rows.append(dict(id=id,cat=cat,features=features,connect=connect,open=open_up,simultaneous=simultaneous,count=contact,equipment=equipment,result='success' if value['success'] else 'failure',bonus=value['bonus'],popularity=module.popularity_bonus(core,result)))
 return dict(cases=rows)
if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8');print(sys.argv[1])
