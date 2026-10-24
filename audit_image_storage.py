"""Read-only PNG/pack/build inventory; never modify images or saved games."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import struct


def inventory(project, build):
    root = Path(project) / 'Assets/Resources/CatCafeContent/Imports'
    images = []
    for path in sorted(root.rglob('*.png')):
        data = path.read_bytes()
        width, height = struct.unpack('>II', data[16:24])
        images.append(dict(path=path.relative_to(root).as_posix(), pack=path.relative_to(root).parts[0],
                           bytes=len(data), width=width, height=height,
                           sha256=hashlib.sha256(data).hexdigest()))
    unique = {r['sha256']: r for r in images}
    packs = []
    for pack in sorted({r['pack'] for r in images}):
        rows = [r for r in images if r['pack'] == pack]
        packs.append(dict(pack=pack, count=len(rows), png_bytes=sum(r['bytes'] for r in rows),
                          rgb24_nomip_bytes=sum(r['width']*r['height']*3 for r in rows)))
    files = sorted((dict(path=p.relative_to(build).as_posix(), bytes=p.stat().st_size)
                    for p in Path(build).rglob('*') if p.is_file()), key=lambda r: -r['bytes'])
    return dict(png_count=len(images), unique_png_count=len(unique), png_bytes=sum(r['bytes'] for r in images),
                duplicate_png_bytes=sum(r['bytes'] for r in images)-sum(r['bytes'] for r in unique.values()),
                dimensions=dict(Counter(f"{r['width']}x{r['height']}" for r in images)),
                build_bytes=sum(r['bytes'] for r in files), build_files=files, packs=packs, images=images)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, required=True)
    parser.add_argument('--build', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = inventory(args.project, args.build)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print(json.dumps({k: v for k, v in result.items() if k not in ('images', 'build_files')}, ensure_ascii=False, indent=2))
