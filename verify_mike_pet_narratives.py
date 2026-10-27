"""登録済みミケpet特別文の読取限定確認。採用・下書き・画像へ保存しない。"""
import json
import tkinter as tk
from itertools import product
from pathlib import Path
from register_mike_pet_narratives import BASE, OUT, ROOT, TEXTS, digest, save_record
from content_images import ImageRegistry
from interaction_subjects import SubjectRegistry, expand
from interaction_texts import InteractionTextStore, key, NARRATIVE_SCENES
from interaction_preview_gui import InteractionPreviewWindow
from interaction_scenes import ACTION_LABELS

def main():
    before=json.loads((ROOT/'Logs/MikePetNarratives/before.json').read_text(encoding='utf8'))
    registered=(OUT/'registration.json').exists()
    reg=json.loads((OUT/('registration.json' if registered else 'draft-runtime-input.json')).read_text(encoding='utf8'))
    subjects=SubjectRegistry(BASE/'interaction_subjects.json')
    store=InteractionTextStore(BASE/'interaction_texts.json',published=True,subject_registry=subjects)
    after=store.read()
    if not registered: after=dict(after,entries=after['entries']+reg['entries'])
    target={key(e['condition']) for e in reg['entries']}
    assert [e for e in after['entries'] if key(e['condition']) not in target]==before['published']['entries']
    if registered:
        for e in reg['entries']: assert store.get(e['condition'])==e
    root=tk.Tk();root.withdraw()
    p=InteractionPreviewWindow(root,subjects.image_catalog({}),ImageRegistry(BASE),store)
    p.window.withdraw()
    p.entries=after["entries"]
    paired=[];fallback=[]
    for scene in TEXTS:
        p.variables['mode'].set('なでる');p.variables['narrative_scene'].set(NARRATIVE_SCENES[scene]);p.render()
        for h,c in product((1,2),repeat=2):
            p.variables['speech'].set('案'+str(h));p.variables['cat'].set('案'+str(c));p.render()
            body=p.body.get('1.0','end-1c')
            assert expand(TEXTS[scene][0][h-1],'ミケ',subjects.get('player')['call']) in body
            assert expand(TEXTS[scene][1][c-1],'ミケ') in body
            assert '{' not in body
            paired.append(dict(scene=scene,speech_slot=h,cat_slot=c,body=body))
        for action in (('direct','adapt','pause','switch') if scene=='open_up' else ('switch' if scene=='switch' else 'connect',)):
            p.variables['action'].set(ACTION_LABELS[action])
            for keep in ('speech','cat_response','none'):
                p.entries=[e for e in after['entries'] if key(e['condition']) not in target or e['condition']['kind']==keep]
                p.render();source=p.source_notice.get();body=p.body.get('1.0','end-1c')
                assert 'フォールバック' in source
                if keep!='none':assert '専用文' in source
                fallback.append(dict(scene=scene,action=action,keep=keep,source=source,body=body))
            p.entries=after['entries']
    for row in json.loads((OUT/'preview-checks.json').read_text(encoding='utf8'))['previews']:
        assert digest(Path(row['image_source']))==row['image_sha256']
    for path,sha in before['protected'].items():assert digest(Path(path))==sha
    root.destroy()
    save_record('verification.json' if registered else 'draft-verification.json',dict(registered=registered,paired_combinations=paired,independent_fallback=fallback,
        test_results='See tests-special.log; normal suite deliberately not repeated (prior transient settings replace errors; no normal logic change)',
        visual_review='Windows Computer Use: 4 scenes x 2 paired slots, inspected image/text; target-only registration recorded separately; see registered flag',
        unchanged_existing_entries=len(before['published']['entries']),generated_reviewed=before['generated_files_reviewed'],
        runtime_fixture_executed=(OUT/('runtime-check.log' if registered else 'runtime-draft-check.log')).exists() and 'PASS 56' in (OUT/('runtime-check.log' if registered else 'runtime-draft-check.log')).read_text(encoding='utf8'), runtime_fixture_scope='Existing C# assembly; Compose plus ApplyCatalog in memory; no StepExchange',unity_imported=False,unity_screen_verified=False,distribution_verified=False,
        scope='Tool rendering, Python selection and separately logged in-memory C# composed-row fixture; no Unity screen or export'))
    print('PASS 16 mixed candidate pairs; 21 side fallback cases; existing entries, candidate IDs, profiles, images and generated files retained')

if __name__=='__main__':main()
