"""制作ツール用の構造化外見。ゲームの起動は不要。"""
import json
from pathlib import Path

FIELDS = {'eye_color': '目の色', 'body_type': '体格', 'markings': '模様の詳細',
          'face_shape': '顔立ち', 'tail': '尻尾', 'notes': '外見の補足'}


def validate_appearance(value):
    if not isinstance(value, dict) or not set(value) <= set(FIELDS):
        raise ValueError('外見プロフィールの項目が不正です。')
    if any(not isinstance(text, str) or len(text) > 1000 for text in value.values()):
        raise ValueError('外見は各項目1000文字以内の文字列で指定してください。')
    return {key: value.get(key, '').strip() for key in FIELDS}


def catalog_profiles(path=None):
    path = Path(path) if path else Path(__file__).resolve().parent.parent / 'cat_cafe_sim/config/cafe_playtest_cats.json'
    rows = json.loads(path.read_text(encoding='utf-8'))
    if isinstance(rows, dict):
        rows = rows['cats']
    return {row['cat_id']: {'appearance': validate_appearance(row.get('appearance', {})),
                           'features': row.get('features', [])} for row in rows}
