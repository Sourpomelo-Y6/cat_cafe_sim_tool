"""ゲームを起動せずに使う、独立した画像登録ツールの入口。"""
import argparse
import json
from pathlib import Path
from content_images import ImageRegistry, DEFAULT_DIRECTORY
from image_tool_gui import ContentImageToolWindow

DEFAULT_CATALOG = Path(__file__).resolve().parent.parent / 'cat_cafe_sim' / 'config' / 'cafe_playtest_cats.json'


def load_catalog(path):
    rows = json.loads(Path(path).read_text(encoding='utf-8'))
    if not isinstance(rows, list):
        raise ValueError('猫一覧はcat_idとnameを持つ配列にしてください。')
    result = {}
    for row in rows:
        if (not isinstance(row, dict) or not isinstance(row.get('cat_id'), str)
                or not row['cat_id'] or not isinstance(row.get('name'), str)
                or not row['name'] or row['cat_id'] in result):
            raise ValueError('猫ID・名前が不正、または重複しています。')
        result[row['cat_id']] = row['name']
    if not result:
        raise ValueError('猫一覧に1匹以上を指定してください。')
    return result


def main():
    parser = argparse.ArgumentParser(description='独立した猫画像登録ツール')
    parser.add_argument('--catalog', type=Path, default=DEFAULT_CATALOG)
    parser.add_argument('--output', type=Path, default=DEFAULT_DIRECTORY)
    args = parser.parse_args()
    # GUI開始前に入力を検証する。ゲームセッションやセーブは生成しない。
    catalog = load_catalog(args.catalog)
    import tkinter as tk
    root = tk.Tk()
    root.withdraw()
    window = ContentImageToolWindow(root, lambda: catalog, registry=ImageRegistry(args.output))
    window.window.bind('<Destroy>', lambda event: root.quit() if event.widget == window.window else None)
    root.mainloop()
    root.destroy()


if __name__ == '__main__':
    main()
