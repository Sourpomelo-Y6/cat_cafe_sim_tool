import sys
import tempfile
import unittest
import json
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from prompt_presets import PromptPresets
from cat_appearance import catalog_profiles
from content_images import ImageRegistry
from content_texts import TextStore
from unity_export import export_bundle


class AppearanceToolTests(unittest.TestCase):
    def test_maron_matches_game_profile_and_prompt(self):
        game = Path(__file__).resolve().parents[2] / 'cat_cafe_sim/config/cafe_regular_introduction.json'
        row = json.loads(game.read_text(encoding='utf-8'))
        profile = catalog_profiles()[row['cat_id']]
        self.assertEqual(profile['appearance'], row['appearance'])
        self.assertEqual(profile['features'], ['orange_tabby', 'short_hair'])
        with tempfile.TemporaryDirectory() as directory:
            prompt = PromptPresets(Path(directory) / 'presets.json').assemble(row['cat_id'], 'normal')
            self.assertIn('緑色', prompt)
            self.assertIn('short_hair', prompt)

    def test_nana_uses_game_identity_and_appearance(self):
        game = Path(__file__).resolve().parents[2] / 'cat_cafe_sim/config/cafe_intake_request.json'
        row = json.loads(game.read_text(encoding='utf-8'))
        profile = catalog_profiles()[row['cat_id']]
        self.assertEqual(profile['appearance'], row['appearance'])
        self.assertEqual(profile['features'], row['features'])
        with tempfile.TemporaryDirectory() as directory:
            prompt = PromptPresets(Path(directory) / 'presets.json').assemble(row['cat_id'], 'normal')
            self.assertIn('琥珀色', prompt)
            self.assertIn('long_hair', prompt)

    def test_catalog_prompt_and_export(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            presets = PromptPresets(root / 'presets.json')
            fields = presets.fields('playtest-sora', 'normal')
            self.assertIn('long_hair', fields['appearance'])
            fields['appearance_profile'] = {'eye_color': 'blue', 'body_type': 'slender'}
            presets.save('playtest-sora', 'normal', fields)
            reopened = PromptPresets(presets.path)
            self.assertIn('eye_color: blue', reopened.assemble('playtest-sora', 'play'))
            self.assertEqual(reopened.appearance_profile('playtest-tama'),
                             catalog_profiles()['playtest-tama']['appearance'])
            profiles = catalog_profiles()
            profiles['playtest-sora']['appearance'] = reopened.appearance_profile('playtest-sora')
            folder, _ = export_bundle({'playtest-sora': 'ソラ'}, ImageRegistry(root / 'images'),
                                      TextStore(root / 'texts.json'), root / 'exports', profiles=profiles)
            cat = json.loads((folder / 'bundle.json').read_text(encoding='utf-8'))['cats'][0]
            self.assertEqual(cat['appearance']['eye_color'], 'blue')
            self.assertEqual(cat['features'], ['white', 'long_hair'])
