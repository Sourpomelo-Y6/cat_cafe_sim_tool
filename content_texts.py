"""制作中・採用済み文章を版付きJSONへ保存する。"""
import json
import uuid
from pathlib import Path

SCENES = ('normal', 'play', 'pet', 'sleep')
DEFAULT_DRAFT_PATH = Path(__file__).resolve().parent / 'user_data' / 'text_drafts.json'


def validate_record(record):
    if (not isinstance(record, dict) or not isinstance(record.get('introduction'), str)
            or not isinstance(record.get('scenes'), dict)):
        raise ValueError('紹介文・行動描写の形式が不正です。')
    if len(record['introduction']) > 2000:
        raise ValueError('紹介文は2000文字以内にしてください。')
    for scene, value in record['scenes'].items():
        if scene not in SCENES or not isinstance(value, str) or len(value) > 500:
            raise ValueError('行動描写は既定の4場面、各500文字以内にしてください。')


class TextStore:
    def __init__(self, path):
        self.path = Path(path)

    def read(self):
        if not self.path.exists():
            return {'version': 1, 'cats': {}}
        data = json.loads(self.path.read_text(encoding='utf-8'))
        if (not isinstance(data, dict) or type(data.get('version')) is not int
                or data['version'] != 1 or not isinstance(data.get('cats'), dict)):
            raise ValueError('文章ファイルの形式が不正です。')
        for cat_id, record in data['cats'].items():
            if not isinstance(cat_id, str) or not cat_id:
                raise ValueError('猫IDが不正です。')
            validate_record(record)
        return data

    def get(self, cat_id):
        return self.read()['cats'].get(cat_id)

    def save(self, cat_id, record):
        if not isinstance(cat_id, str) or not cat_id:
            raise ValueError('猫を選んでください。')
        validate_record(record)
        data = self.read()  # 壊れたファイルは上書きしない。
        data['cats'][cat_id] = record
        self.path.parent.mkdir(parents=True, exist_ok=True)
        temp = self.path.with_name(f'{self.path.stem}-{uuid.uuid4().hex}.tmp')
        try:
            temp.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
            temp.replace(self.path)
        finally:
            temp.unlink(missing_ok=True)
