"""登録済みToolデータを新規ストアから読む限定照合。書込は検証記録だけ。"""
import json
from pathlib import Path
from interaction_subjects import SubjectRegistry
from interaction_texts import InteractionTextStore, DEFAULT_DRAFT_PATH, condition, make_entry
from interaction_presentation import select_entry
from tama_normal_texts import SID, TEXTS, ACTIONS

def main():
    base = Path(__file__).resolve().parents[1] / 'cat_cafe_sim/saves/content_images'
    subjects = SubjectRegistry(base / 'interaction_subjects.json')
    published = InteractionTextStore(base / 'interaction_texts.json', published=True, subject_registry=subjects)
    drafts = InteractionTextStore(DEFAULT_DRAFT_PATH, subject_registry=subjects)
    entries = published.read()['entries']
    checks = []
    for mode, actions in ACTIONS.items():
        for reaction in ('neutral','favorable','enthusiastic','turn_away','confused','listless'):
            expected = TEXTS.get((mode, reaction))
            common = condition('cat_response', SID, mode, 'any', reaction, subjects=subjects.labels())
            if expected:
                entry = make_entry(common, expected+[''])
                assert published.get(common) == drafts.get(common) == entry
            else:
                assert published.get(common) is None and drafts.get(common) is None
            for action in actions:
                query = condition('cat_response', SID, mode, action, reaction, subjects=subjects.labels())
                result = select_entry(entries, query)
                if expected:
                    assert result['source'] == '登録済み' and result['match'] == '操作共通'
                    assert result['candidates'] == entry['variants'][:2]
                else:
                    assert not result['candidates'] and result['source'] == '未制作'
                checks.append(dict(mode=mode, action=action, reaction=reaction, source=result['source'],
                                   match=result['match'], candidate_ids=[v['id'] for v in result['candidates']]))
    out = Path(__file__).parent / 'Docs/Results/TamaNormalNarratives/tool-reload.json'
    out.write_text(json.dumps(dict(checks=checks, passed=len(checks)), ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print('PASS',len(checks),'fresh Tool source/candidate/fallback checks')

if __name__ == '__main__': main()
