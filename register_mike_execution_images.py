"""Register Mike's approved preparation/execution pairs without duplicating Unity packs."""
import argparse
import copy
import hashlib
import json
import uuid
from pathlib import Path

from content_images import ImageRegistry
from interaction_presentation import read_png


def write_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_suffix(path.suffix + '.tmp')
    temp.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    temp.replace(path)


def register(root, project):
    # Use the versioned production assets so checkout can reproduce registration.
    prototypes = Path(__file__).resolve().parent / 'Docs/Results/MikeExecutionImages/Prototypes'
    registry = ImageRegistry(root / 'cat_cafe_sim/saves/content_images')
    before = registry.read()
    catalog_path = project / 'Assets/Resources/CatCafeContent/catalog.json'
    catalog = json.loads(catalog_path.read_text(encoding='utf-8-sig'))
    resources = project / 'Assets/Resources'
    bundle_path = resources / (catalog['bundleResource'] + '.json')
    text_path = resources / (catalog['interactionResource'] + '.json')
    if bundle_path.parent != text_path.parent:
        raise ValueError('Expected a shared active content pack')
    bundle = json.loads(bundle_path.read_text(encoding='utf-8'))
    texts = json.loads(text_path.read_text(encoding='utf-8'))
    original_bundle, original_texts = copy.deepcopy(bundle), copy.deepcopy(texts)
    mike = next(c for c in bundle['cats'] if c['cat_id'] == 'playtest-mike')
    if next(c for c in texts['cat_bindings'] if c['cat_id'] == 'cat-mike')['content_cat_id'] != 'playtest-mike':
        raise ValueError('Mike image binding differs; inspect before registering')
    planned, blobs = {}, {}
    for mode in ('teaser', 'ball', 'plush', 'tunnel', 'pet', 'brush', 'voice', 'presence'):
        prep = prototypes / 'MikeBasePoses-v1' / f'mike-{mode}-base.png'
        if mode in ('voice', 'presence'):
            prep = prototypes / 'CatAppearance-v1/mike-normal.png'
        execute = (prototypes / 'CatAppearance-v1/mike-play.png' if mode == 'teaser' else
                   prototypes / 'MikeBasePoses-v1' / f'mike-{mode}-base.png' if mode in ('voice', 'presence') else
                   prototypes / 'MikePetExecution-v1/mike-pet-execute.png' if mode == 'pet' else
                   prototypes / 'MikeActionExecution-v1' / f'mike-{mode}-execute.png')
        for scene, source in ((mode, prep), (mode + '__switch', prep),
                              (mode + '__direct', execute), (mode + '__adapt', execute)):
            data = read_png(source)
            image = 'images/' + hashlib.sha256(data).hexdigest() + '.png'
            blobs[image] = data
            planned[scene] = dict(image=image, source=str(source))
    # Reuse registered files with the same bytes, including legacy UUID filenames.
    existing = {}
    for scenes in before['cats'].values():
        for relative in scenes.values():
            path = registry._resolve(relative)
            if path.is_file():
                existing.setdefault(hashlib.sha256(path.read_bytes()).hexdigest(), relative)
    updated = copy.deepcopy(before)
    for cat_id in ('cat-mike', 'playtest-mike'):
        scenes = updated['cats'].setdefault(cat_id, {})
        for key, row in planned.items():
            scenes[key] = existing.get(Path(row['image']).stem, row['image'])
    for key, row in planned.items():
        scene = next((s for s in mike['scenes'] if s['key'] == key), None)
        if scene is None:
            scene = {'key': key}
            mike['scenes'].append(scene)
        preparing = '__' not in key or key.endswith('__switch')
        scene.update(image=row['image'], label=key,
                     text='交流を始める前のミケの様子です。' if preparing else 'ミケが通常の交流に応じています。')
    for binding in texts['image_bindings']:
        key = binding['scene_key']
        if binding['cat_id'] == 'cat-mike' and key in planned:
            binding.update(image=planned[key]['image'], resolved_scene_key=key)
    payload = {k: v for k, v in texts.items() if k not in ('pack_id', 'revision')}
    texts['revision'] = hashlib.sha256(json.dumps(payload, ensure_ascii=False, sort_keys=True,
                                                 separators=(',', ':')).encode('utf-8')).hexdigest()
    # All unrelated cats, authored text and special reactions must remain intact.
    assert [c for c in bundle['cats'] if c['cat_id'] != 'playtest-mike'] == [c for c in original_bundle['cats'] if c['cat_id'] != 'playtest-mike']
    assert texts['entries'] == original_texts['entries']
    for scene in next(c for c in original_bundle['cats'] if c['cat_id'] == 'playtest-mike')['scenes']:
        if scene['key'] not in planned:
            assert next(s for s in mike['scenes'] if s['key'] == scene['key']) == scene
    backup = root / 'Logs/MikeExecutionImages' / uuid.uuid4().hex
    backup.mkdir(parents=True)
    for name, data in (('registry-before.json', before), ('bundle-before.json', original_bundle),
                       ('interaction-texts-before.json', original_texts)):
        write_json(backup / name, data)
    added = []
    for image, data in blobs.items():
        digest = Path(image).stem
        if digest not in existing:
            path = registry.directory / image
            path.parent.mkdir(parents=True, exist_ok=True)
            if not path.exists():
                path.write_bytes(data)
                added.append(str(path))
        path = bundle_path.parent / image
        if not path.exists():
            path.write_bytes(data)
            added.append(str(path))
    write_json(registry.path, updated)
    write_json(bundle_path, bundle)
    write_json(text_path, texts)
    for key, row in planned.items():
        for cat_id in ('cat-mike', 'playtest-mike'):
            assert registry.image_path(cat_id, key).read_bytes() == blobs[row['image']]
        assert (bundle_path.parent / row['image']).read_bytes() == blobs[row['image']]
    report = dict(scenes=planned, active_pack=str(bundle_path.parent), backup=str(backup),
                  added_files=added, revision=texts['revision'], build_run=False)
    write_json(root / 'Docs/Prototypes/MikeActionExecution-v1/Registration.json', report)
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project-path', type=Path, required=True)
    args = parser.parse_args()
    result = register(Path(__file__).resolve().parent.parent, args.project_path.resolve())
    print(json.dumps({k: result[k] for k in ('active_pack', 'backup', 'revision', 'build_run')}, indent=2))
