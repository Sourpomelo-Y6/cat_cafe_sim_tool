"""Reproducible, isolated Tool -> Unity fixtures; never reads production saves."""
import argparse
import copy
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from optional_events import OptionalEventStore, default_event, validate_event
from unity_export import export_bundle
from content_images import ImageRegistry
from content_texts import TextStore


def make_fixture(output):
    output = Path(output).resolve()
    output.mkdir(parents=True, exist_ok=True)
    store = OptionalEventStore(output / 'fixture-source' / 'optional_events.json')
    original = dict(version=1, enabled=True, event=validate_event(default_event(), require_revision=False))
    store.save(original)
    first = store.export_data({'playtest-mike': 'ミケ'}, '1' * 32)
    edited = copy.deepcopy(original)
    edited['event']['title'] += '・改稿'
    edited['event']['introduction'][0]['text'] += ' 改稿の確認です。🐈'
    store.save(edited)
    second = store.export_data({'playtest-mike': 'ミケ'}, '2' * 32)
    edited['enabled'] = False
    store.save(edited)
    disabled = store.export_data({'playtest-mike': 'ミケ'}, '3' * 32)
    for name, data in (('first', first), ('second', second), ('disabled', disabled)):
        folder = output / 'fixtures' / name
        folder.mkdir(parents=True, exist_ok=True)
        (folder / 'optional_events.json').write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    # A tiny pack with no images tests the full content importer without duplicating art.
    source = output / 'full-bundle-source'
    pack, _ = export_bundle({'playtest-mike': 'ミケ'}, ImageRegistry(source), TextStore(source / 'texts.json'), output / 'full-bundle-exports')
    # Portraits are outside this probe; the real portrait library must stay untouched.
    (pack / 'human_portraits.json').unlink()
    (output / 'full-bundle.json').write_text(json.dumps(dict(folder=str(pack)), indent=2) + '\n', encoding='utf-8')
    return output / 'fixtures', pack


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    for path in make_fixture(args.output):
        print(path)
