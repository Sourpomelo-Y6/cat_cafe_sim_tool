import json
import sys
import tempfile
import threading
import unittest
from pathlib import Path
from unittest.mock import patch
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from comfy_client import ComfyClient, GenerationCancelled, build_prompt, workflow_defaults


class ComfyTests(unittest.TestCase):
    def settings(self):
        return dict(workflow_defaults(), positive='one calico cat', seed=42)

    def test_source_settings_and_seed_validation(self):
        settings = self.settings()
        prompt = build_prompt(settings)
        self.assertEqual(prompt['5']['inputs']['noise_seed'], 42)
        self.assertEqual(prompt['5']['inputs']['steps'], 27)
        self.assertEqual(prompt['8']['inputs']['width'], 1024)
        self.assertNotIn('11', prompt)
        settings['seed'] = -1
        with self.assertRaises(ValueError):
            build_prompt(settings)

    def test_completed_job_download_and_metadata(self):
        with tempfile.TemporaryDirectory() as directory:
            client = ComfyClient(output_directory=directory)
            calls = []
            def request(route, payload=None, **kwargs):
                calls.append(route)
                if route == '/prompt':
                    return {'prompt_id': 'test-job'}
                if route.startswith('/history'):
                    return {'test-job': {'outputs': {'7': {'images': [dict(filename='cat.png', subfolder='', type='output')]}}}}
                return b'\x89PNG\r\n\x1a\nexample'
            with patch.object(client, 'request', side_effect=request):
                path = client.generate(self.settings())
            self.assertTrue(path.exists())
            self.assertEqual(json.loads(path.with_suffix('.json').read_text(encoding='utf-8'))['settings']['seed'], 42)
            self.assertEqual(len(calls), 3)

    def test_cancel_error_and_timeout(self):
        client = ComfyClient()
        cancel = threading.Event()
        cancel.set()
        with patch.object(client, 'request') as request:
            with self.assertRaises(GenerationCancelled):
                client.generate(self.settings(), cancel)
            request.assert_not_called()
        with patch.object(client, 'request', side_effect=[{'prompt_id': 'a'}, {'a': {'status': {'status_str': 'error', 'messages': ['failure']}}}]):
            with self.assertRaises(ValueError):
                client.generate(self.settings())
        with patch.object(client, 'request', return_value={'prompt_id': 'a'}):
            with self.assertRaises(TimeoutError):
                client.generate(self.settings(), timeout=0)

    def test_img2img_graph_and_reference_snapshot(self):
        with tempfile.TemporaryDirectory() as directory:
            reference = Path(directory) / 'source.png'
            reference.write_bytes(b'\x89PNG\r\n\x1a\nsource')
            settings = dict(self.settings(), reference_path=str(reference), denoise=0.75)
            prompt = build_prompt(settings, 'uploaded.png')
            self.assertEqual(prompt['5']['class_type'], 'KSampler')
            self.assertEqual(prompt['5']['inputs']['latent_image'], ['12', 0])
            self.assertEqual(prompt['10']['inputs']['image'], 'uploaded.png')
            self.assertNotIn('8', prompt)
            client = ComfyClient(output_directory=Path(directory) / 'output')
            responses = [{'prompt_id': 'job'}, {'job': {'outputs': {'7': {'images': [dict(filename='cat.png', type='output')]}}}},
                         b'\x89PNG\r\n\x1a\nresult']
            with patch.object(client, 'upload_image', return_value='uploaded.png') as upload, \
                    patch.object(client, 'request', side_effect=responses):
                output = client.generate(settings)
            upload.assert_called_once_with(reference.read_bytes())
            metadata = json.loads(output.with_suffix('.json').read_text(encoding='utf-8'))
            self.assertEqual((output.parent / metadata['reference']['file']).read_bytes(), reference.read_bytes())
            self.assertEqual(metadata['source_workflow'], 'i2prompt-source.json')
            for denoise in (0, -0.1, 1.1, float('nan')):
                with self.assertRaises(ValueError):
                    build_prompt(dict(settings, denoise=denoise))
