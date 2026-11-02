"""Optional Mike detail portraits; independent from normal scene image bindings."""
import hashlib
import io
import json
import os
import re
import shutil
import struct
import tempfile
import uuid
from pathlib import Path
from PIL import Image
from optional_events import read_json

STATES = {'stress_high': 'ストレス高め', 'fatigue_high': '疲労高め', 'affinity_high': '好感度高め'}
MAX_BYTES = 16 * 1024 * 1024


def png(data):
    if len(data) < 33 or len(data) > MAX_BYTES or data[:8] != b'\x89PNG\r\n\x1a\n' or data[12:16] != b'IHDR':
        raise ValueError('PNG画像は16MiB以内にしてください。')
    width, height = struct.unpack('>II', data[16:24])
    if min(width, height) < 1 or max(width, height) > 2048:
        raise ValueError('PNGの長辺は2048px以内にしてください。')
    try:
        with Image.open(io.BytesIO(data)) as image:
            if image.format != 'PNG' or min(image.size) < 1 or max(image.size) > 2048:
                raise ValueError('PNGの長辺は2048px以内にしてください。')
            image.verify()
        with Image.open(io.BytesIO(data)) as image:
            image.load()
    except (OSError, SyntaxError) as exc:
        raise ValueError('PNG画像が破損しています。') from exc
    return data


def validate(data, catalog=None):
    if not isinstance(data, dict) or set(data) != {'version', 'pack_id', 'bindings'} or type(data['version']) is not int or data['version'] != 1:
        raise ValueError('状態画像の形式が不正です。')
    if not isinstance(data['pack_id'], str) or not re.fullmatch('[a-f0-9]{32}', data['pack_id']):
        raise ValueError('状態画像のパックIDが不正です。')
    if not isinstance(data['bindings'], list) or len(data['bindings']) > 3:
        raise ValueError('状態画像はミケの3状態までです。')
    seen = set()
    for row in data['bindings']:
        if not isinstance(row, dict) or set(row) != {'cat_id', 'content_cat_id', 'state', 'image'}:
            raise ValueError('状態画像の対応が不正です。')
        if row['cat_id'] != 'cat-mike' or row['content_cat_id'] != 'playtest-mike' or not isinstance(row['state'], str) or row['state'] not in STATES or row['state'] in seen:
            raise ValueError('状態画像の猫・状態・重複を確認してください。')
        if not isinstance(row['image'], str) or not re.fullmatch(r'images/[a-f0-9]{64}\.png', row['image']):
            raise ValueError('状態画像のパスが不正です。')
        if catalog is not None and row['content_cat_id'] not in catalog:
            raise ValueError('素材一覧にミケの画像IDがありません。')
        seen.add(row['state'])
    return data


class StateImageStore:
    def __init__(self, directory):
        self.directory = Path(directory) / 'state_images'
        self.path = self.directory / 'manifest.json'

    def read(self):
        if not self.path.exists():
            return dict(version=1, pack_id='0' * 32, bindings=[])
        return validate(read_json(self.path.read_text(encoding='utf-8-sig')))

    def image_path(self, state):
        row = next((r for r in self.read()['bindings'] if r['state'] == state), None)
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

    def adopt(self, state, source=None, expected_hash=None):
        if state not in STATES:
            raise ValueError('状態を選択してください。')
        data = self.read()  # Never overwrite a broken manifest.
        rows = [r for r in data['bindings'] if r['state'] != state]
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
            rows.append(dict(cat_id='cat-mike', content_cat_id='playtest-mike', state=state, image=relative))
        self._save(dict(version=1, pack_id=uuid.uuid4().hex, bindings=sorted(rows, key=lambda r: r['state'])))

    def export_data(self, catalog, pack_id):
        if not self.path.exists():
            return None, {}
        data = self.read()
        data['pack_id'] = pack_id
        validate(data, catalog)
        blobs = {}
        for row in data['bindings']:
            raw = png((self.directory / row['image']).read_bytes())
            if row['image'] != f'images/{hashlib.sha256(raw).hexdigest()}.png':
                raise ValueError('登録済み状態画像のハッシュが一致しません。')
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
        folder = destination / f'CatCafeStateImages-{pack_id}'
        folder.mkdir(parents=True, exist_ok=False)
        try:
            (folder / 'images').mkdir()
            for relative, raw in blobs.items():
                (folder / relative).write_bytes(raw)
            (folder / 'state_images.json').write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
        except Exception:
            if folder.resolve().parent == destination and folder.name == f'CatCafeStateImages-{pack_id}':
                shutil.rmtree(folder)
            raise
        return folder
