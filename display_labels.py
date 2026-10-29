"""Display-only aliases; stable IDs and authored prose are never rewritten."""
import json
import os
import tempfile
import warnings
from pathlib import Path

DEFAULT_PATH = Path(__file__).resolve().parent / 'user_data' / 'display_labels.json'
DEFAULTS = {
    'modes': dict(teaser='ねこじゃらし', ball='ボール遊び', plush='ぬいぐるみ遊び', tunnel='トンネル遊び', pet='なでる', brush='ブラッシング', voice='声をかける', presence='そばで見守る', pause='休憩'),
    'actions': dict(direct='素直に動かす', adapt='相手に合わせる', intense='激しく動かす', feint='フェイントを入れる', pause='動きを止めて待つ', switch='動作を切り替える', connect='心をつかむ'),
    'reactions': dict(neutral='通常', favorable='好意的', enthusiastic='熱心', turn_away='拒否', confused='戸惑い', listless='低体力', connect='心をつかむ', open_up='心を開く', simultaneous='同時成立', received='特別な働きかけを受け止める'),
}

def validate(data):
    if not isinstance(data, dict) or set(data) != {'version', 'overrides'} or type(data['version']) is not int or data['version'] != 1 or not isinstance(data['overrides'], dict):
        raise ValueError('表示名設定の形式が不正です。')
    if set(data['overrides']) - set(DEFAULTS):
        raise ValueError('未対応の表示名分類です。')
    result = {'version': 1, 'overrides': {}}
    for group, rows in data['overrides'].items():
        if not isinstance(rows, dict) or set(rows) - set(DEFAULTS[group]):
            raise ValueError('未対応の行動・反応IDです。')
        clean = {}
        for key, value in rows.items():
            if not isinstance(value, str) or not value.strip() or len(value.strip().encode('utf-16-le')) // 2 > 32 or any(ord(c) < 32 or c in '<>' for c in value):
                raise ValueError('表示名は1～32文字、改行・制御文字・<>なしで入力してください。')
            value = value.strip()
            if value != DEFAULTS[group][key]:
                clean[key] = value
        if group == 'modes' and any(value in ('通常', '遊ぶ', '眠る') or value == '撫でられる' and key != 'pet' for key, value in clean.items()):
            raise ValueError('画像の既存場面と重複する表示名は設定できません。')
        merged = dict(DEFAULTS[group], **clean)
        if len(set(merged.values())) != len(merged):
            raise ValueError('同じ分類に重複する表示名は設定できません。')
        if clean:
            result['overrides'][group] = clean
    return result

class DisplayLabelStore:
    def __init__(self, path=None):
        self.path = Path(path) if path is not None else DEFAULT_PATH
    def read(self):
        if not self.path.exists():
            return {'version': 1, 'overrides': {}}
        return validate(json.loads(self.path.read_text(encoding='utf-8-sig')))
    def save(self, data):
        clean = validate(data)
        self.path.parent.mkdir(parents=True, exist_ok=True)
        fd, name = tempfile.mkstemp(prefix=self.path.name+'.', suffix='.tmp', dir=self.path.parent)
        try:
            with os.fdopen(fd, 'w', encoding='utf-8', newline='\n') as stream:
                json.dump(clean, stream, ensure_ascii=False, indent=2)
                stream.write('\n')
            os.replace(name, self.path)
        finally:
            if os.path.exists(name):
                os.unlink(name)
        return clean
    def export_data(self):
        data = self.read()
        return dict(version=1, **{group: [dict(id=key, label=value) for key, value in data['overrides'].get(group, {}).items()] for group in DEFAULTS})

# A Tool session uses a coherent label set. Saving new labels takes effect after restart.
try:
    _session = DisplayLabelStore().read()['overrides']
except (ValueError, OSError) as exc:
    warnings.warn('表示名設定を読み込めないため標準表示を使用します：'+str(exc))
    _session = {}

def label(group, key, fallback):
    return _session.get(group, {}).get(key, fallback)
