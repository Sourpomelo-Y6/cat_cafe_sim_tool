"""個体・場面別画像の登録。元画像をコピーし、版付き対応表を保存する。"""
import json
import shutil
import uuid
from pathlib import Path

DEFAULT_DIRECTORY = Path(__file__).resolve().parent.parent / 'cat_cafe_sim' / 'saves' / 'content_images'
from interaction_scenes import INTERACTION_SCENES, COMBINATION_SCENES, SPECIAL_SCENES
SCENE_IDS = ('normal', 'play', 'pet', 'sleep', 'open_up', 'simultaneous') + tuple(key for key, _ in INTERACTION_SCENES + COMBINATION_SCENES + SPECIAL_SCENES)


class ImageRegistry:
    def __init__(self, directory=DEFAULT_DIRECTORY):
        self.directory = Path(directory)
        self.path = self.directory / 'manifest.json'

    def read(self):
        if not self.path.exists():
            return {'version': 1, 'cats': {}}
        data = json.loads(self.path.read_text(encoding='utf-8'))
        if not isinstance(data, dict) or data.get('version') != 1 or not isinstance(data.get('cats'), dict):
            raise ValueError('画像対応表の形式が不正です。')
        for cat_id, scenes in data['cats'].items():
            if not isinstance(cat_id, str) or not isinstance(scenes, dict):
                raise ValueError('猫の画像登録が不正です。')
            for key, value in scenes.items():
                if key not in SCENE_IDS or not isinstance(value, str):
                    raise ValueError('場面の画像登録が不正です。')
                self._resolve(value)
        return data

    def _resolve(self, value):
        path = (self.directory / value).resolve()
        if self.directory.resolve() not in path.parents:
            raise ValueError('画像は登録フォルダ内に置いてください。')
        return path

    def image_path(self, cat_id, scene):
        value = self.read()['cats'].get(cat_id, {}).get(scene)
        return self._resolve(value) if value else None

    def register(self, cat_id, scene, source):
        if not isinstance(cat_id, str) or not cat_id or scene not in SCENE_IDS:
            raise ValueError('猫と場面を選んでください。')
        source = Path(source)
        if source.suffix.lower() != '.png':
            raise ValueError('PNG画像を選んでください。')
        data = self.read()  # 壊れた対応表を上書きしない。
        images = self.directory / 'images'
        images.mkdir(parents=True, exist_ok=True)
        filename = f'images/{uuid.uuid4().hex}.png'
        shutil.copyfile(source, self.directory / filename)
        data['cats'].setdefault(cat_id, {})[scene] = filename
        temp = self.directory / f'manifest-{uuid.uuid4().hex}.tmp'
        try:
            temp.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
            temp.replace(self.path)
        finally:
            temp.unlink(missing_ok=True)
        return self.directory / filename


def load_display_image(master, path, max_width=640, max_height=420):
    import tkinter as tk
    image = tk.PhotoImage(master=master, file=str(path))
    scale = max(1, (image.width() + max_width - 1) // max_width, (image.height() + max_height - 1) // max_height)
    return image.subsample(scale, scale)
