"""猫別デザインLoRAと場面別ポーズLoRAの設定。"""
import copy
import json
import math
import uuid
from pathlib import Path

DEFAULT_PATH = Path(__file__).resolve().parent / 'user_data' / 'lora_settings.json'
EMPTY = dict(enabled=False, name='', strength_model=0.7, strength_clip=0.7, trigger='')
from content_images import SCENE_IDS as SCENES


def validate_lora(row):
    if (not isinstance(row, dict) or type(row.get('enabled')) is not bool
            or not isinstance(row.get('name'), str) or not isinstance(row.get('trigger'), str)):
        raise ValueError('LoRA設定の形式が不正です。')
    if row['enabled'] and not row['name'].strip():
        raise ValueError('有効なLoRAにはファイル名が必要です。')
    for key in ('strength_model', 'strength_clip'):
        value = row.get(key)
        if type(value) not in (int, float) or not math.isfinite(value) or not -2 <= value <= 2:
            raise ValueError('LoRA強度は-2～2の有限数にしてください。')
    return dict(row)


class LoraSettings:
    def __init__(self, path=DEFAULT_PATH):
        self.path = Path(path)

    def read(self):
        if not self.path.exists():
            return dict(version=1, cats={}, scenes={})
        data = json.loads(self.path.read_text(encoding='utf-8'))
        if (not isinstance(data, dict) or data.get('version') != 1
                or not isinstance(data.get('cats'), dict) or not isinstance(data.get('scenes'), dict)):
            raise ValueError('LoRA対応表の形式が不正です。')
        for mapping in (data['cats'], data['scenes']):
            for key, row in mapping.items():
                if not isinstance(key, str) or not key:
                    raise ValueError('LoRAの対象IDが不正です。')
                validate_lora(row)
        if any(key not in SCENES for key in data['scenes']):
            raise ValueError('ポーズLoRAの場面が不正です。')
        return data

    def fields(self, cat_id, scene):
        data = self.read()
        return copy.deepcopy([data['cats'].get(cat_id, EMPTY), data['scenes'].get(scene, EMPTY)])

    def active(self, cat_id, scene):
        return [row for row in self.fields(cat_id, scene) if row['enabled']]

    def save(self, cat_id, scene, design, pose):
        if not cat_id or scene not in SCENES:
            raise ValueError('猫と場面を選んでください。')
        design, pose = validate_lora(design), validate_lora(pose)
        data = self.read()
        data['cats'][cat_id] = design
        data['scenes'][scene] = pose
        self.path.parent.mkdir(parents=True, exist_ok=True)
        temp = self.path.with_name(uuid.uuid4().hex + '.tmp')
        try:
            temp.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
            temp.replace(self.path)
        finally:
            temp.unlink(missing_ok=True)
