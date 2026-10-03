import sys
import tempfile
import unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from prompt_presets import PromptPresets, GENERIC_CAT


class PresetTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.presets = PromptPresets(Path(self.temp.name) / 'settings.json')

    def test_four_actions_and_generic_other_cat(self):
        self.assertIn('calico', self.presets.assemble('playtest-mike', 'normal'))
        self.assertEqual(self.presets.fields('other', 'normal')['appearance'], GENERIC_CAT)
        self.assertIn('feather wand', self.presets.assemble('playtest-mike', 'play'))
        self.assertIn('stroked', self.presets.assemble('playtest-mike', 'pet'))
        self.assertIn('asleep', self.presets.assemble('playtest-mike', 'sleep'))
        self.assertFalse(self.presets.path.exists())

    def test_persistence_and_shared_scope(self):
        fields = self.presets.fields('cat-a', 'play')
        fields.update(appearance='one black cat', background='plain white wall', action='playing with a feather')
        self.presets.save('cat-a', 'play', fields)
        reopened = PromptPresets(self.presets.path)
        self.assertIn('one black cat', reopened.assemble('cat-a', 'normal'))
        self.assertNotIn('one black cat', reopened.assemble('cat-b', 'normal'))
        self.assertIn('plain white wall', reopened.assemble('cat-b', 'sleep'))
        self.assertIn('playing with a feather', reopened.assemble('cat-b', 'play'))
        other = reopened.fields('cat-b', 'pet')
        other['appearance'] = 'one white cat'
        reopened.save('cat-b', 'pet', other)
        self.assertIn('one black cat', reopened.assemble('cat-a', 'play'))
        self.assertFalse(list(self.presets.path.parent.glob('*.tmp')))

    def test_invalid_settings_not_overwritten(self):
        fields = self.presets.fields('cat-a', 'normal')
        fields['background'] = ''
        with self.assertRaises(ValueError):
            self.presets.save('cat-a', 'normal', fields)
        self.presets.path.write_text('{broken', encoding='utf-8')
        with self.assertRaises(ValueError):
            self.presets.save('cat-a', 'normal', dict(fields, background='wall'))
        self.assertEqual(self.presets.path.read_text(encoding='utf-8'), '{broken')
