import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from lora_settings import LoraSettings, EMPTY
from comfy_client import ComfyClient, build_prompt, workflow_defaults


class LoraTests(unittest.TestCase):
    def test_persistence_and_scopes(self):
        with tempfile.TemporaryDirectory() as directory:
            settings = LoraSettings(Path(directory) / 'loras.json')
            design = dict(EMPTY, enabled=True, name='design.safetensors', trigger='mikecat')
            pose = dict(EMPTY, enabled=True, name='sleep.safetensors', trigger='sleepingcat')
            settings.save('cat-a', 'sleep', design, pose)
            reopened = LoraSettings(settings.path)
            self.assertEqual([row['name'] for row in reopened.active('cat-a', 'sleep')], ['design.safetensors', 'sleep.safetensors'])
            self.assertEqual([row['name'] for row in reopened.active('cat-a', 'normal')], ['design.safetensors'])
            self.assertEqual([row['name'] for row in reopened.active('cat-b', 'sleep')], ['sleep.safetensors'])
            before = settings.path.read_bytes()
            with self.assertRaises(ValueError):
                settings.save('cat-a', 'sleep', dict(design, strength_clip=float('nan')), pose)
            self.assertEqual(settings.path.read_bytes(), before)

    def test_chain_t2i_img2img_triggers_and_disable(self):
        rows = [dict(EMPTY, enabled=True, name='design.safetensors', trigger='mikecat'),
                dict(EMPTY, enabled=True, name='pose.safetensors', trigger='sleepingcat')]
        for reference in (False, True):
            settings = dict(workflow_defaults(), positive='one cat', seed=42, loras=rows)
            if reference:
                settings.update(reference_path='ref.png', denoise=0.65)
            prompt = build_prompt(settings)
            self.assertEqual(prompt['20']['inputs']['model'], ['1', 0])
            self.assertEqual(prompt['21']['inputs']['model'], ['20', 0])
            self.assertEqual(prompt['5']['inputs']['model'], ['21', 0])
            self.assertEqual(prompt['2']['inputs']['clip'], ['21', 1])
            self.assertEqual(prompt['3']['inputs']['clip'], ['21', 1])
            self.assertEqual(prompt['6']['inputs']['vae'], ['1', 2])
            self.assertEqual(prompt['2']['inputs']['text'], 'one cat, mikecat, sleepingcat')
            disabled = build_prompt(dict(settings, loras=[dict(row, enabled=False) for row in rows]))
            self.assertNotIn('20', disabled)
            self.assertEqual(disabled['5']['inputs']['model'], ['1', 0])
            self.assertEqual(disabled['2']['inputs']['text'], 'one cat')

    def test_remote_list_and_missing_lora_fails_before_submission(self):
        client = ComfyClient()
        with patch.object(client, 'request', return_value={'LoraLoader': {'input': {'required': {'lora_name': [['a.safetensors']]}}}}):
            self.assertEqual(client.list_loras(), ['a.safetensors'])
        settings = dict(workflow_defaults(), positive='one cat', seed=42,
                        loras=[dict(EMPTY, enabled=True, name='missing.safetensors')])
        with patch.object(client, 'list_loras', return_value=[]), patch.object(client, 'request') as request:
            with self.assertRaises(ValueError):
                client.generate(settings)
            request.assert_not_called()
