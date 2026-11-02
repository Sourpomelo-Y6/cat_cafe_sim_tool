"""Optional Mike outfit portraits, separate from normal and state image bindings."""
import hashlib
import json
import os
import re
import shutil
import tempfile
import uuid
from pathlib import Path
from optional_events import read_json

STATES = {'ribbon': 'リボン姿'}


from state_images import png


def validate(data, catalog=None):
    if not isinstance(data, dict) or set(data) != {'version', 'pack_id', 'outfits'} or type(data['version']) is not int or data['version'] != 1:
        raise ValueError('着せ替え画像の形式が不正です。')
    if not isinstance(data['pack_id'], str) or not re.fullmatch('[a-f0-9]{32}', data['pack_id']):
        raise ValueError('着せ替え画像のパックIDが不正です。')
    if not isinstance(data['outfits'], list) or len(data['outfits']) > 1:
        raise ValueError('着せ替え画像はミケの1セットまでです。')
    seen = set()
    for row in data['outfits']:
        if not isinstance(row, dict) or set(row) != {'cat_id', 'content_cat_id', 'outfit_id', 'label', 'image'}:
            raise ValueError('着せ替え画像の対応が不正です。')
        if row['cat_id'] != 'cat-mike' or row['content_cat_id'] != 'playtest-mike' or not isinstance(row['outfit_id'], str) or row['outfit_id'] not in STATES or row['outfit_id'] in seen:
            raise ValueError('着せ替え画像の猫・状態・重複を確認してください。')
        if not isinstance(row['image'], str) or not re.fullmatch(r'images/[a-f0-9]{64}\.png', row['image']):
            raise ValueError('着せ替え画像のパスが不正です。')
        if not isinstance(row['label'], str) or not row['label'].strip() or len(row['label'].encode('utf-16-le')) // 2 > 40:
            raise ValueError('表示名は1～40文字にしてください。')
        if catalog is not None and row['content_cat_id'] not in catalog:
            raise ValueError('素材一覧にミケの画像IDがありません。')
        seen.add(row['outfit_id'])
    return data


class CatOutfitStore:
    def __init__(self, directory):
        self.directory = Path(directory) / 'cat_outfits'
        self.path = self.directory / 'manifest.json'

    def read(self):
        if not self.path.exists():
            return dict(version=1, pack_id='0' * 32, outfits=[])
        return validate(read_json(self.path.read_text(encoding='utf-8-sig')))

    def image_path(self, state):
        row = next((r for r in self.read()['outfits'] if r['outfit_id'] == state), None)
        return self.directory / row['image'] if row else None

    def _save(self, data):
        validate(data)
        self.directory.mkdir(parents=True, exist_ok=True)
        fd, name = tempfile.mkstemp(dir=self.directory, prefix='manifest-', suffix='.tmp')
        try:
            with os.fdopen(fd, 'w', encoding='utf-8', newline='\n') as stream:
                json.dump(data, stream, ensure_ascii=False, indent=2)
                stream.write('\n')
            os.replace(name, self.path)
        finally:
            if os.path.exists(name):
                os.unlink(name)

    def adopt(self, state, source=None, expected_hash=None, label="リボン姿"):
        if state not in STATES:
            raise ValueError('状態を選択してください。')
        data = self.read()  # Never overwrite a broken manifest.
        rows = [r for r in data['outfits'] if r['outfit_id'] != state]
        if source is not None:
            raw = png(Path(source).read_bytes())
            digest = hashlib.sha256(raw).hexdigest()
            if expected_hash is not None and digest != expected_hash:
                raise ValueError('比較後に画像が変わりました。もう一度比較してください。')
            relative = f'images/{digest}.png'
            target = self.directory / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            if target.exists() and target.read_bytes() != raw:
                raise ValueError('登録済み画像が破損しています。')
            if not target.exists():
                target.write_bytes(raw)
            rows.append(dict(cat_id='cat-mike', content_cat_id='playtest-mike', outfit_id=state, label=label, image=relative))
        self._save(dict(version=1, pack_id=uuid.uuid4().hex, outfits=sorted(rows, key=lambda r: r['outfit_id'])))

    def export_data(self, catalog, pack_id):
        if not self.path.exists():
            return None, {}
        data = self.read()
        data['pack_id'] = pack_id
        validate(data, catalog)
        blobs = {}
        for row in data['outfits']:
            raw = png((self.directory / row['image']).read_bytes())
            if row['image'] != f'images/{hashlib.sha256(raw).hexdigest()}.png':
                raise ValueError('登録済み着せ替え画像のハッシュが一致しません。')
            blobs[row['image']] = raw
        return data, blobs

    def export(self, destination, catalog):
        pack_id = uuid.uuid4().hex
        data, blobs = self.export_data(catalog, pack_id)
        if data is None:
            raise ValueError('まだ採用登録がありません。解除を出力する場合は「未登録に戻す」を実行してください。')
        destination = Path(destination).resolve()
        if destination == self.directory.resolve() or self.directory.resolve() in destination.parents:
            raise ValueError('書き出し先は登録フォルダーと別にしてください。')
        folder = destination / f'CatCafeOutfits-{pack_id}'
        folder.mkdir(parents=True, exist_ok=False)
        try:
            (folder / 'images').mkdir()
            for relative, raw in blobs.items():
                (folder / relative).write_bytes(raw)
            (folder / 'cat_outfits.json').write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
        except Exception:
            if folder.resolve().parent == destination and folder.name == f'CatCafeOutfits-{pack_id}':
                shutil.rmtree(folder)
            raise
        return folder
