"""Relocated scripts must import without registering or exporting content."""
import json
from pathlib import Path
import subprocess
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]

class ScriptLayoutTests(unittest.TestCase):
    def test_imports_without_writes_and_preserves_resource_roots(self):
        code = r'''
import sys, json, importlib, runpy
from pathlib import Path
root=Path(sys.argv[1]);sys.path.insert(0,str(root));sys.dont_write_bytecode=True
mapping=json.loads((root/'scripts/relocations.json').read_text(encoding='utf-8'))
def guard(event,args):
    if event=='open':
        mode=args[1];flags=args[2]
        if (isinstance(mode,str) and any(c in mode for c in 'wax+')) or (isinstance(flags,int) and flags & (1|2|64|512|1024)):
            raise AssertionError('Unexpected write: '+str(args[0]))
    if event in ('os.mkdir','os.remove','os.rename','subprocess.Popen','socket.connect'):
        raise AssertionError('Unexpected side effect: '+event)
sys.addaudithook(guard)
for old,new in mapping.items():
    assert not (root/old).exists(),old
    module=importlib.import_module(new[:-3].replace('/','.'))
    if new.startswith('scripts/'):
        assert module.TOOL_ROOT==root,new
        values=runpy.run_path(str(root/new),run_name='layout_check')
        assert values['TOOL_ROOT']==root
    for name in ('ROOT','BASE','OUT','SOURCE'):
        value=getattr(module,name,None)
        if isinstance(value,Path):
            if name=='ROOT':assert value==root.parent,(new,name,value)
            if name=='BASE':assert value==root.parent/'cat_cafe_sim/saves/content_images',(new,name,value)
            if name in ('OUT','SOURCE'):assert any(value.is_relative_to(base) for base in (root/'Docs',root.parent/'Docs',root.parent/'Logs')),(new,name,value)
print('Validated',len(mapping),'relocations without writes')
'''
        result=subprocess.run([sys.executable,'-B','-c',code,str(ROOT)],cwd=ROOT.parent,
                              capture_output=True,text=True,encoding='utf-8')
        self.assertEqual(result.returncode,0,result.stdout+result.stderr)
        count = len(json.loads((ROOT/'scripts/relocations.json').read_text(encoding='utf-8')))
        self.assertIn(f'Validated {count}',result.stdout)
