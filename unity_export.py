"""採用済み素材だけを、Unityへ取り込める独立したパックへ書き出す。"""
import argparse
import hashlib
import json
import shutil
import struct
import uuid
from pathlib import Path
from content_images import ImageRegistry
from content_texts import TextStore

SCENES = (
    ('normal', '通常', 'マットの上で落ち着いて座っています。'),
    ('play', '遊ぶ', 'ねこじゃらしに手を伸ばしています。'),
    ('pet', '撫でられる', '頭を撫でられて、気持ちよさそうにしています。'),
    ('sleep', '眠る', 'マットの上で丸くなって眠っています。'),
)


def export_bundle(catalog, registry, texts, destination, *, profiles=None, interaction_texts=None, interaction_bindings=None):
    if (not isinstance(catalog, dict) or not catalog or any(
            not isinstance(key, str) or not key or not isinstance(name, str) or not name
            for key, name in catalog.items())):
        raise ValueError('有効な猫一覧が必要です。')
    images = registry.read()
    from cat_appearance import catalog_profiles, validate_appearance
    profiles = catalog_profiles() if profiles is None else profiles
    records = texts.read()
    from interaction_presentation import build_interaction_catalog
    interactions, interaction_report, interaction_blobs = build_interaction_catalog(
        catalog, registry, store=interaction_texts, bindings=interaction_bindings)
    destination = Path(destination).resolve()
    if destination == registry.directory.resolve() or registry.directory.resolve() in destination.parents:
        raise ValueError('書き出し先は登録素材のフォルダとは別にしてください。')
    pack_id = uuid.uuid4().hex
    interactions['pack_id'] = pack_id
    folder = destination / f'CatCafeContent-{pack_id}'
    warnings, cats, blobs = [], [], {}
    for cat_id, name in catalog.items():
        record = records['cats'].get(cat_id, {})
        introduction = record.get('introduction', '').strip()
        if not introduction:
            warnings.append(f'{name}（{cat_id}）：紹介文が未登録。定型文を使用します。')
        cat = {'cat_id': cat_id, 'name': name, 'introduction': introduction or f'{name}の様子をご覧ください。', 'scenes': []}
        profile = profiles.get(cat_id, {})
        cat['appearance'] = validate_appearance(profile.get('appearance', {}))
        cat['features'] = list(profile.get('features', []))
        from interaction_scenes import INTERACTION_SCENES, COMBINATION_SCENES, SPECIAL_SCENES
        extra = tuple((key, label, label + 'をしています。') for key, label in INTERACTION_SCENES + COMBINATION_SCENES + SPECIAL_SCENES
                      if key in images['cats'].get(cat_id, {}))
        for key, label, fallback in SCENES + extra:
            text = record.get('scenes', {}).get(key, '').strip()
            if not text:
                warnings.append(f'{name} / {label}：描写が未登録。定型文を使用します。')
            image = ''
            value = images['cats'].get(cat_id, {}).get(key)
            if value:
                path = registry._resolve(value)
                try:
                    data = path.read_bytes()
                    if (len(data) < 33 or data[:8] != b'\x89PNG\r\n\x1a\n' or data[12:16] != b'IHDR'
                            or min(struct.unpack('>II', data[16:24])) <= 0):
                        raise ValueError('PNGヘッダーが不正です。')
                    image = f'images/{hashlib.sha256(data).hexdigest()}.png'
                    blobs[image] = data
                except (OSError, ValueError) as exc:
                    warnings.append(f'{name} / {label}：登録画像を読み込めません（{exc}）。画像なしで書き出します。')
            else:
                warnings.append(f'{name} / {label}：画像が未登録。画像なしで書き出します。')
            cat['scenes'].append({'key': key, 'label': label, 'text': text or fallback, 'image': image})
        cats.append(cat)
    blobs.update(interaction_blobs)
    warnings.extend(interaction_report['image_warnings'])
    if interaction_report['missing_count']:
        warnings.append(f"交流文：{interaction_report['missing_count']}条件が未制作です。validation.jsonを確認してください。")
    bundle = {'version': 1, 'pack_id': pack_id, 'cats': cats}
    report = {'version': 1, 'cat_count': len(cats), 'image_count': len(blobs), 'warnings': warnings, 'interactions': interaction_report}
    folder.mkdir(parents=True, exist_ok=False)
    try:
        (folder / 'images').mkdir()
        for relative, data in blobs.items():
            (folder / relative).write_bytes(data)
        for filename, value in (('bundle.json', bundle), ('interaction_texts.json', interactions), ('validation.json', report)):
            (folder / filename).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    except Exception:
        # 自分で作成した新規パックだけを片付ける。既存の出力は触らない。
        if folder.resolve().parent != destination or folder.name != f'CatCafeContent-{pack_id}':
            raise RuntimeError('一時出力の場所を確認できないため削除しません。')
        shutil.rmtree(folder)
        raise
    return folder, report


def main():
    from run import load_catalog, DEFAULT_CATALOG
    from content_images import DEFAULT_DIRECTORY
    parser = argparse.ArgumentParser(description='採用済み画像・文章のUnity向け書き出し')
    parser.add_argument('--catalog', type=Path, default=DEFAULT_CATALOG)
    parser.add_argument('--source', type=Path, default=DEFAULT_DIRECTORY)
    parser.add_argument('--destination', type=Path, required=True)
    args = parser.parse_args()
    from cat_appearance import catalog_profiles
    from prompt_presets import PromptPresets
    profiles = catalog_profiles(args.catalog)
    overrides = PromptPresets().read().get('appearance_profiles', {})
    for key, profile in profiles.items():
        if key in overrides:
            profile['appearance'] = overrides[key]
    folder, report = export_bundle(load_catalog(args.catalog), ImageRegistry(args.source),
                                  TextStore(args.source / 'texts.json'), args.destination, profiles=profiles)
    print(folder)
    print(f"猫 {report['cat_count']}匹 / 画像 {report['image_count']}枚 / 注意 {len(report['warnings'])}件")


if __name__ == '__main__':
    main()
