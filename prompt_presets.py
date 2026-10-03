"""猫の外見・場面・共通背景を保存し、生成文へ組み立てる。"""
import copy
import json
import uuid
from pathlib import Path

DEFAULT_PATH = Path(__file__).resolve().parent / 'user_data' / 'prompt_presets.json'
DEFAULTS = {
    'version': 1,
    'style': 'clean lines, best quality, soft 2d illustration, full body, fixed eye-level view, warm soft lighting, no text',
    'background': 'simple plain ivory wall, light wooden floor, sage green mat, minimal empty room, no furniture, no wall decorations',
    'cats': {'playtest-mike': 'one adult short-haired calico cat, white chest and paws, orange and black patches, amber eyes'},
    'actions': {
        'normal': 'sitting calmly on the mat, no people, no hands',
        'play': 'playing with a feather wand, one generic adult hand holding the wand from the right edge, no human face, no human body',
        'pet': 'being gently stroked on the head by one generic adult hand from the right edge, relaxed expression, no human face, no human body',
        'sleep': 'curled up asleep on the mat, closed eyes, no people, no hands',
    },
}
GENERIC_CAT = 'one adult cat, consistent coat markings and body proportions'


class PromptPresets:
    def __init__(self, path=DEFAULT_PATH):
        self.path = Path(path)

    def read(self):
        if not self.path.exists():
            return copy.deepcopy(DEFAULTS)
        data = json.loads(self.path.read_text(encoding='utf-8'))
        if (not isinstance(data, dict) or data.get('version') != 1
                or any(not isinstance(data.get(key), str) or not data[key].strip() for key in ('style', 'background'))
                or not isinstance(data.get('cats'), dict) or not isinstance(data.get('actions'), dict)
                or set(data['actions']) != set(DEFAULTS['actions'])):
            raise ValueError('プロンプト設定の形式が不正です。')
        for mapping in (data['cats'], data['actions']):
            if any(not isinstance(key, str) or not key or not isinstance(value, str) or not value.strip()
                   for key, value in mapping.items()):
                raise ValueError('外見・行動の設定が不正です。')
        return data

    def fields(self, cat_id, scene):
        data = self.read()
        return dict(appearance=data['cats'].get(cat_id, GENERIC_CAT), action=data['actions'][scene],
                    background=data['background'], style=data['style'])

    def assemble(self, cat_id, scene):
        fields = self.fields(cat_id, scene)
        return ', '.join(fields[key].strip().rstrip(',') for key in ('style', 'appearance', 'action', 'background'))

    def save(self, cat_id, scene, fields):
        if not isinstance(cat_id, str) or not cat_id or scene not in DEFAULTS['actions']:
            raise ValueError('猫と場面を選んでください。')
        if any(not isinstance(fields.get(key), str) or not fields[key].strip()
               for key in ('appearance', 'action', 'background', 'style')):
            raise ValueError('外見・行動・背景・画風をすべて入力してください。')
        data = self.read()
        data['cats'][cat_id] = fields['appearance'].strip()
        data['actions'][scene] = fields['action'].strip()
        data.update(background=fields['background'].strip(), style=fields['style'].strip())
        self.path.parent.mkdir(parents=True, exist_ok=True)
        temporary = self.path.with_name(f'{uuid.uuid4().hex}.tmp')
        try:
            temporary.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
            temporary.replace(self.path)
        finally:
            temporary.unlink(missing_ok=True)
