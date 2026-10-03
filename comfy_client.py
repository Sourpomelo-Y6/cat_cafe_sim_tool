"""標準ライブラリだけでComfyUIへ生成依頼し、PNGを取得する。"""
import json
import hashlib
import secrets
import threading
import time
import uuid
from pathlib import Path
from urllib.request import Request, urlopen
from urllib.parse import urlencode, urlsplit
from urllib.error import HTTPError

SOURCE_WORKFLOW = Path(__file__).resolve().parent / 'workflows' / 't2i-simple-source.json'
GENERATED_DIRECTORY = Path(__file__).resolve().parent / 'generated'


def workflow_defaults():
    data = json.loads(SOURCE_WORKFLOW.read_text(encoding='utf-8'))
    nodes = {node['type']: node for node in data['nodes']}
    sampler = nodes['KSamplerAdvanced']['widgets_values']
    texts = {node['id']: node['widgets_values'][0] for node in data['nodes'] if node['type'] == 'CLIPTextEncode'}
    return dict(model=nodes['CheckpointLoaderSimple']['widgets_values'][0], negative=texts[3],
                width=nodes['EmptyLatentImage']['widgets_values'][0],
                height=nodes['EmptyLatentImage']['widgets_values'][1],
                steps=sampler[3], cfg=sampler[4], sampler=sampler[5], scheduler=sampler[6])


def build_prompt(settings, reference_name=None):
    s = settings
    if not s['positive'].strip() or not s['model'].strip():
        raise ValueError('モデル名とプロンプトを入力してください。')
    if type(s['seed']) is not int or not 0 <= s['seed'] <= 2**64 - 1:
        raise ValueError('seedは0以上、2の64乗未満の整数にしてください。')
    if any(type(s[key]) is not int or not 64 <= s[key] <= 2048 or s[key] % 8 for key in ('width', 'height')):
        raise ValueError('幅・高さは64～2048の8の倍数にしてください。')
    if not 1 <= s['steps'] <= 100 or not 0 <= s['cfg'] <= 30:
        raise ValueError('Stepsは1～100、CFGは0～30にしてください。')
    # 添付の単純なt2iグラフをAPI形式へ組み立てる。seedは直接渡す。
    prompt = {
        '1': {'class_type': 'CheckpointLoaderSimple', 'inputs': {'ckpt_name': s['model']}},
        '2': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['1', 1], 'text': s['positive']}},
        '3': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['1', 1], 'text': s['negative']}},
        '8': {'class_type': 'EmptyLatentImage', 'inputs': {'width': s['width'], 'height': s['height'], 'batch_size': 1}},
        '5': {'class_type': 'KSamplerAdvanced', 'inputs': {
            'model': ['1', 0], 'positive': ['2', 0], 'negative': ['3', 0], 'latent_image': ['8', 0],
            'add_noise': 'enable', 'noise_seed': s['seed'], 'steps': s['steps'], 'cfg': s['cfg'],
            'sampler_name': s['sampler'], 'scheduler': s['scheduler'], 'start_at_step': 0,
            'end_at_step': 7500, 'return_with_leftover_noise': 'disable'}},
        '6': {'class_type': 'VAEDecode', 'inputs': {'samples': ['5', 0], 'vae': ['1', 2]}},
        '7': {'class_type': 'SaveImage', 'inputs': {'images': ['6', 0], 'filename_prefix': 'CatCafeSimTool/cat'}},
    }
    if s.get('reference_path'):
        if not reference_name:
            reference_name = 'reference.png'  # GUI側の入力検証用。
        if not 0 < s.get('denoise', 0) <= 1:
            raise ValueError('denoiseは0より大きく1以下にしてください。')
        prompt['10'] = {'class_type': 'LoadImage', 'inputs': {'image': reference_name}}
        prompt['11'] = {'class_type': 'ImageScale', 'inputs': {'image': ['10', 0], 'upscale_method': 'area',
            'width': s['width'], 'height': s['height'], 'crop': 'disabled'}}
        prompt['12'] = {'class_type': 'VAEEncode', 'inputs': {'pixels': ['11', 0], 'vae': ['1', 2]}}
        prompt.pop('8')
        prompt['5'] = {'class_type': 'KSampler', 'inputs': {'model': ['1', 0], 'positive': ['2', 0],
            'negative': ['3', 0], 'latent_image': ['12', 0], 'seed': s['seed'], 'steps': s['steps'],
            'cfg': s['cfg'], 'sampler_name': s['sampler'], 'scheduler': s['scheduler'], 'denoise': s['denoise']}}
    from lora_settings import validate_lora
    model, clip = ['1', 0], ['1', 1]
    triggers = []
    for index, row in enumerate(s.get('loras', [])):
        row = validate_lora(row)
        if not row['enabled']:
            continue
        node_id = str(20 + index)
        prompt[node_id] = {'class_type': 'LoraLoader', 'inputs': {
            'model': model, 'clip': clip, 'lora_name': row['name'],
            'strength_model': row['strength_model'], 'strength_clip': row['strength_clip']}}
        model, clip = [node_id, 0], [node_id, 1]
        if row['trigger'].strip() and row['trigger'].strip() not in triggers:
            triggers.append(row['trigger'].strip())
    prompt['5']['inputs']['model'] = model
    prompt['2']['inputs']['clip'] = clip
    prompt['3']['inputs']['clip'] = clip
    if triggers:
        prompt['2']['inputs']['text'] = ', '.join([s['positive']] + triggers)
    return prompt


class GenerationCancelled(Exception):
    pass


class ComfyClient:
    def __init__(self, base_url='http://127.0.0.1:8188', output_directory=GENERATED_DIRECTORY):
        parts = urlsplit(base_url)
        if parts.scheme not in ('http', 'https') or not parts.netloc or parts.query or parts.fragment:
            raise ValueError('ComfyUIのHTTP URLを入力してください。')
        self.base_url = base_url.rstrip('/')
        self.output_directory = Path(output_directory)

    def request(self, route, payload=None, *, binary=False):
        data = json.dumps(payload).encode('utf-8') if payload is not None else None
        request = Request(self.base_url + route, data=data, headers={'Content-Type': 'application/json'})
        try:
            with urlopen(request, timeout=15) as response:
                raw = response.read()
        except HTTPError as exc:
            detail = exc.read().decode('utf-8', errors='replace')[:1500]
            raise ValueError(f'ComfyUI HTTP {exc.code}: {detail}') from exc
        return raw if binary else json.loads(raw)

    def list_loras(self):
        info = self.request('/object_info/LoraLoader')
        try:
            names = info['LoraLoader']['input']['required']['lora_name'][0]
        except (KeyError, IndexError, TypeError) as exc:
            raise ValueError('ComfyUIにLoraLoaderノードがありません。') from exc
        if not isinstance(names, list) or any(not isinstance(name, str) for name in names):
            raise ValueError('LoRA一覧の形式が不正です。')
        return names

    def generate(self, settings, cancel=None, progress=lambda message: None, timeout=600):
        cancel = cancel or threading.Event()
        prompt = build_prompt(settings)
        if cancel.is_set():
            raise GenerationCancelled()
        enabled_loras = [row for row in settings.get('loras', []) if row['enabled']]
        if enabled_loras:
            available = self.list_loras()
            missing = [row['name'] for row in enabled_loras if row['name'] not in available]
            if missing:
                raise ValueError('ComfyUIにLoRAが見つかりません：' + ', '.join(missing))
        reference_bytes = None
        if settings.get('reference_path'):
            reference_bytes = Path(settings['reference_path']).read_bytes()
            if not reference_bytes.startswith(b'\x89PNG\r\n\x1a\n'):
                raise ValueError('基準画像にはPNGを指定してください。')
            progress('基準画像をComfyUIへ送っています…')
            reference_name = self.upload_image(reference_bytes)
            if cancel.is_set():
                raise GenerationCancelled()
            prompt = build_prompt(settings, reference_name)
        submitted = self.request('/prompt', {'prompt': prompt, 'client_id': str(uuid.uuid4())})
        if submitted.get('node_errors'):
            raise ValueError(f"ワークフロー検証エラー：{submitted['node_errors']}")
        prompt_id = submitted['prompt_id']
        progress(f'生成待ち・実行中：{prompt_id}')
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            if cancel.is_set():
                raise GenerationCancelled()
            history = self.request('/history/' + prompt_id).get(prompt_id)
            if history:
                status = history.get('status', {})
                if status.get('status_str') == 'error':
                    raise ValueError(f"生成エラー：{status.get('messages', [])}")
                images = history.get('outputs', {}).get('7', {}).get('images', [])
                if images:
                    image = images[0]
                    raw = self.request('/view?' + urlencode({key: image.get(key, '') for key in ('filename', 'subfolder', 'type')}), binary=True)
                    if cancel.is_set():
                        raise GenerationCancelled()
                    if not raw.startswith(b'\x89PNG\r\n\x1a\n'):
                        raise ValueError('出力画像がPNGではありません。')
                    self.output_directory.mkdir(parents=True, exist_ok=True)
                    path = self.output_directory / f'{uuid.uuid4().hex}.png'
                    path.write_bytes(raw)
                    reference_metadata = None
                    if reference_bytes is not None:
                        reference_path = path.with_suffix('.reference.png')
                        reference_path.write_bytes(reference_bytes)
                        reference_metadata = dict(file=reference_path.name, sha256=hashlib.sha256(reference_bytes).hexdigest())
                    path.with_suffix('.json').write_text(json.dumps(dict(settings=settings, prompt_id=prompt_id,
                        source_workflow='i2prompt-source.json' if reference_bytes is not None else SOURCE_WORKFLOW.name,
                        reference=reference_metadata, prompt=prompt), ensure_ascii=False, indent=2), encoding='utf-8')
                    return path
                if status.get('completed'):
                    raise ValueError('生成は完了しましたが、保存画像がありません。')
            cancel.wait(0.5)
        raise TimeoutError('生成待ちが10分を超えました。ComfyUI側の履歴を確認してください。')

    def upload_image(self, raw):
        boundary = 'CatCafeBoundary' + uuid.uuid4().hex
        filename = 'cat-reference-' + uuid.uuid4().hex + '.png'
        body = (f'--{boundary}\r\nContent-Disposition: form-data; name="image"; filename="{filename}"\r\n'
                'Content-Type: image/png\r\n\r\n').encode('ascii') + raw + f'\r\n--{boundary}--\r\n'.encode('ascii')
        request = Request(self.base_url + '/upload/image', data=body,
                          headers={'Content-Type': 'multipart/form-data; boundary=' + boundary})
        with urlopen(request, timeout=30) as response:
            uploaded = json.loads(response.read())
        return '/'.join(part for part in (uploaded.get('subfolder', ''), uploaded['name']) if part)


def random_seed():
    return secrets.randbits(63)
