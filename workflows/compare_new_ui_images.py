"""Assemble existing GPU outputs without resampling full comparisons. No source edits."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import csv,json,hashlib,shutil
ROOT=Path(__file__).resolve().parents[1]
inputs=ROOT.parent/'Logs/ImageStorageNewUI/comparison'
out=ROOT/'Docs/Results/ImageStorageNewUI'; out.mkdir(parents=True,exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
scenes=['normal','teaser__switch','teaser__direct','teaser__open_up']
cats=['playtest-mike','playtest-tama','playtest-sora']
project=Path('C:/Users/hi-wa/unity_workspase/CatCafeSim')
pointer=json.loads((project/'Assets/Resources/CatCafeContent/catalog.json').read_text(encoding='utf-8-sig'))
bundlepath=project/'Assets/Resources'/ (pointer['bundleResource']+'.json')
bundle=json.loads(bundlepath.read_text(encoding='utf-8-sig'))
manifest={'pack':bundle['pack_id'],'method':'Unity Graphics.Blit -> ReadPixels; transient CompressTexture Best; full panels pixel-exact; crops nearest 2x','sources':[],'panels':[]}
for cat in cats:
 c=next(c for c in bundle['cats'] if c['cat_id']==cat)
 for scene in scenes:
  s=next(s for s in c['scenes'] if s['key']==scene)
  p=bundlepath.parent/s['image']; h=hashlib.sha256(p.read_bytes()).hexdigest()
  assert p.stem==h
  manifest['sources'].append({'cat':cat,'scene':scene,'image':s['image'],'sha256':h,'dimensions':Image.open(p).size})
 for size in [400,320]:
  canvas=Image.new('RGB',(2*size+36,4*(size+34)+34),'#edf0ed'); d=ImageDraw.Draw(canvas)
  d.text((8,6),f'{cat} / {size}px / source (left) | BC7 1024 (right)',font=font,fill='black')
  for i,scene in enumerate(scenes):
   y=34+i*(size+34); d.text((8,y),scene,font=font,fill='black')
   for k,fmt in enumerate(['source','BC7']):
    p=inputs/f'{cat}-{scene}-{fmt}-{size}.png'; im=Image.open(p); assert im.size==(size,size)
    xy=(8+k*(size+20),y+26); canvas.paste(im,xy)
    manifest['panels'].append({'board':f'{cat}-{size}.png','scene':scene,'format':fmt,'xy':xy,'size':size,'input_sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
  canvas.save(out/f'{cat}-{size}.png',optimize=True)
 # Three informative areas, across all scenes and both actual display sizes.
 # Normal/switch: face and side fur. Direct/open-up: face and top tool/hand.
 canvas=Image.new('RGB',(1256,8*220+34),'#edf0ed'); d=ImageDraw.Draw(canvas)
 d.text((8,6),f'{cat} / displayed-pixel crops, nearest 2x / source | BC7',font=font,fill='black')
 for i,(size,scene) in enumerate((s,k) for s in [400,320] for k in scenes):
  y=34+i*220; d.text((8,y),f'{size}px {scene}: face / fur / hand-tool; normalized ROI in manifest',font=font,fill='black')
  face=(.32,.10,.52,.30)
  if scene in ['teaser__direct','teaser__open_up']:
   face={'playtest-mike':(.35,.22,.55,.42),'playtest-tama':(.32,.18,.52,.38),'playtest-sora':(.43,.20,.63,.40)}[cat]
   if cat=='playtest-sora' and scene=='teaser__open_up': face=(.32,.12,.52,.32)
  tool=(.80,.00,1,.20)
  if scene=='teaser__open_up': tool=(.80,.05,1,.25)
  if cat=='playtest-sora' and scene=='teaser__direct': tool=(.00,.00,.20,.20)
  if scene=='normal' or (scene=='teaser__switch' and cat!='playtest-mike'): tool=(.50,.66,.70,.86)
  regions=[face,(.22,.45,.42,.65),tool]
  for j,box in enumerate(regions):
   # 64x64 displayed pixels at 320, 80x80 at 400; .2 edge.
   bounds=tuple(round(v*size) for v in box)
   for k,fmt in enumerate(['source','BC7']):
    im=Image.open(inputs/f'{cat}-{scene}-{fmt}-{size}.png').crop(bounds)
    im=im.resize((im.width*2,im.height*2),Image.Resampling.NEAREST)

    canvas.paste(im,(8+j*416+k*204,y+26))
   manifest.setdefault('crops',[]).append({'cat':cat,'size':size,'scene':scene,'bounds':bounds,'magnification':2,'region':j})
  d.text((8,y+194),'2x nearest; no sharpening / not original-texture 1:1',font=font,fill='black')

 d.rectangle((0,0,1256,32),fill='#edf0ed'); d.text((8,6),f'{cat} / displayed-pixel crops, nearest 2x / source | BC7',font=font,fill='black')
 canvas.save(out/f'{cat}-crops.png',optimize=True)
shutil.copyfile(inputs/'comparison.csv',out/'comparison.csv')
(out/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
rows=list(csv.DictReader((inputs/'comparison.csv').open()))
assert len(rows)==72
stats={}
for size in ['400','320']:
 for fmt in ['RGBA32','DXT5','BC7']:
  v=[float(r['rgbPSNR']) for r in rows if r['displaySize']==size and r['candidate']==fmt]
  stats[f'{size}-{fmt}']={'n':len(v),'min':min(v),'mean':sum(v)/len(v),'max':max(v)}
(out/'stats.json').write_text(json.dumps(stats,indent=2),encoding='utf-8'); print(json.dumps(stats,indent=2))
html='<meta charset="utf-8"><title>新UI 実寸画像比較</title><style>body{font:16px sans-serif;background:#edf0ed}img{display:block;margin:12px 0}p{max-width:900px}</style><h1>新UI 400px / 320px 比較</h1><p>各組は左：原本GPU描画、右：1024px BC7最高品質の一時GPU比較。PNGを100%で表示してください。実Importer・配布Playerの結果ではありません。寸法は新UIと一致、通常画面への組み込みはしていません。拡大は表示後のピクセルを2倍 nearestで拡大しています。</p>'
for cat in cats:
 html+=f'<h2>{cat}</h2>'
 for suffix in ['400','320','crops']:
  html+=f'<p>{suffix}</p><img src="{cat}-{suffix}.png">'
html+='<h2>既存新UI Canvas（Editor、ミケ実行・一時差替え）</h2><p>左に相当するsourceは現行Resources RGB24、BC7は一時GPU候補。表示サイズは実測400/320px。配布Playerの結果ではありません。</p>'
for width in [1280,1024]:
 for fmt in ['source','BC7']:
  name=f'canvas-{width}-{fmt}.png'
  shutil.copyfile(inputs.parent/'canvas'/name,out/name)
  html+=f'<p>{width} {fmt}</p><img src="{name}">'
shutil.copyfile(inputs.parent/'canvas/canvas.csv',out/'canvas.csv')
(out/'index.html').write_text(html,encoding='utf-8')
