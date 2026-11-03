"""Optional detail portraits keyed by image identity; reads legacy Mike v1 packs."""
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
MAX_TOTAL_BYTES = 256 * 1024 * 1024
MAX_JSON = 100000


def read_definition(text):
    if len(text) > MAX_JSON:
        raise ValueError('状態画像JSONは100,000文字以内にしてください。')
    return validate(read_json(text))


def definition_text(data):
    validate(data)
    text = json.dumps(data, ensure_ascii=False, indent=2) + '\n'
    if len(text) > MAX_JSON:
        raise ValueError('状態画像JSONは100,000文字以内にしてください。')
    return text


def normalize(data):
    validate(data)
    return dict(version=2, pack_id=data['pack_id'], bindings=[
        {key: row[key] for key in ('content_cat_id', 'state', 'image')} for row in data['bindings']])


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
    if not isinstance(data, dict) or set(data) != {'version', 'pack_id', 'bindings'} or type(data['version']) is not int or data['version'] not in (1, 2):
        raise ValueError('状態画像の形式が不正です。')
    if not isinstance(data['pack_id'], str) or not re.fullmatch('[a-f0-9]{32}', data['pack_id']):
        raise ValueError('状態画像のパックIDが不正です。')
    legacy = data['version'] == 1
    if not isinstance(data['bindings'], list) or len(data['bindings']) > (3 if legacy else 192):
        raise ValueError('状態画像の対応件数が上限を超えています。')
    seen = set()
    identities = set()
    for row in data['bindings']:
        keys = {'content_cat_id', 'state', 'image'} | ({'cat_id'} if legacy else set())
        if not isinstance(row, dict) or set(row) != keys:
            raise ValueError('状態画像の対応が不正です。')
        if legacy and (row['cat_id'] != 'cat-mike' or row['content_cat_id'] != 'playtest-mike'):
            raise ValueError('v1の状態画像はミケ専用です。')
        cid = row['content_cat_id']
        if not isinstance(cid, str) or not re.fullmatch('[a-z][a-z0-9_-]{0,63}', cid):
            raise ValueError('状態画像の画像IDが不正です。')
        if not isinstance(row['state'], str) or row['state'] not in STATES or (cid, row['state']) in seen:
            raise ValueError('状態画像の猫・状態・重複を確認してください。')
        if not isinstance(row['image'], str) or not re.fullmatch(r'images/[a-f0-9]{64}\.png', row['image']):
            raise ValueError('状態画像のパスが不正です。')
        if catalog is not None and row['content_cat_id'] not in catalog:
            raise ValueError('素材一覧に状態画像の画像IDがありません：' + cid)
        seen.add((cid, row['state']))
        identities.add(cid)
    if len(identities) > 64:
        raise ValueError('状態画像は64画像IDまでです。')
    return data


class StateImageStore:
    def __init__(self, directory):
        self.directory = Path(directory) / 'state_images'
        self.path = self.directory / 'manifest.json'

    def read(self):
        if not self.path.exists():
            return dict(version=2, pack_id='0' * 32, bindings=[])
        if self.path.stat().st_size > 400003:
            raise ValueError('状態画像JSONが大きすぎます。')
        return normalize(read_definition(self.path.read_text(encoding='utf-8-sig')))

    def image_path(self, state, *, content_id='playtest-mike'):
        row = next((r for r in self.read()['bindings'] if r['state'] == state and r['content_cat_id'] == content_id), None)
        return self.directory / row['image'] if row else None

    def _save(self, data):
        text = definition_text(data)
        self.directory.mkdir(parents=True, exist_ok=True)
        fd, name = tempfile.mkstemp(dir=self.directory, prefix='manifest-', suffix='.tmp')
        try:
            with os.fdopen(fd, 'w', encoding='utf-8', newline='\n') as stream:
                stream.write(text)
                stream.flush()
                os.fsync(stream.fileno())
            if self.path.exists():
                old = self.path.read_bytes()
                if read_definition(old.decode('utf-8-sig'))['version'] == 1:
                    backup = self.directory / 'manifest.v1.json'
                    if backup.exists():
                        if backup.read_bytes() != old:
                            raise ValueError('旧登録の退避内容が異なります。元の登録を保持しました。')
                    else:
                        created = False
                        try:
                            with backup.open('xb') as stream:
                                created = True
                                stream.write(old)
                                stream.flush()
                                os.fsync(stream.fileno())
                        except Exception:
                            if created:
                                backup.unlink()
                            raise
            os.replace(name, self.path)
        finally:
            if os.path.exists(name):
                os.unlink(name)

    def adopt(self, state, source=None, expected_hash=None, *, content_id='playtest-mike'):
        if state not in STATES:
            raise ValueError('状態を選択してください。')
        data = self.read()  # Never overwrite a broken manifest.
        if not isinstance(content_id, str) or not re.fullmatch('[a-z][a-z0-9_-]{0,63}', content_id):
            raise ValueError('画像IDを選択してください。')
        rows = [r for r in data['bindings'] if (r['content_cat_id'], r['state']) != (content_id, state)]
        if source is not None:
            if Path(source).stat().st_size > MAX_BYTES:
                raise ValueError('状態画像が大きすぎます。')
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
            rows.append(dict(content_cat_id=content_id, state=state, image=relative))
        self._save(dict(version=2, pack_id=uuid.uuid4().hex, bindings=sorted(rows, key=lambda r: (r['content_cat_id'], r['state']))))

    def export_data(self, catalog, pack_id):
        if not self.path.exists():
            return None, {}
        data = self.read()
        data['pack_id'] = pack_id
        validate(data, catalog)
        definition_text(data)
        blobs = {}
        total = 0
        for row in data['bindings']:
            if row['image'] in blobs:
                continue
            path = self.directory / row['image']
            size = path.stat().st_size
            if size > MAX_BYTES or total + size > MAX_TOTAL_BYTES:
                raise ValueError('状態画像は1枚16MiB、パック合計256MiB以内にしてください。')
            raw = png(path.read_bytes())
            if row['image'] != f'images/{hashlib.sha256(raw).hexdigest()}.png':
                raise ValueError('登録済み状態画像のハッシュが一致しません。')
            blobs[row['image']] = raw
            total += len(raw)
            if total > MAX_TOTAL_BYTES:
                raise ValueError('状態画像の合計が256MiBを超えています。')
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
            (folder / 'state_images.json').write_text(definition_text(data), encoding='utf-8')
        except Exception:
            if folder.resolve().parent == destination and folder.name == f'CatCafeStateImages-{pack_id}':
                shutil.rmtree(folder)
            raise
        return folder
