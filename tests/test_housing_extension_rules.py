import hashlib
import json
from pathlib import Path
import sys
import unittest

TOOL = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOL))
from export_standalone_rules import exported_rules


class HousingExtensionRulesTests(unittest.TestCase):
    def test_only_housing_extensions_change_existing_rules(self):
        current = exported_rules()
        legacy = json.loads((TOOL / 'unity_preview_template/Resources/CatCafeStandalone/housing_legacy_rules.json').read_text(encoding='utf-8'))
        self.assertEqual(current['housing_extensions'], [
            dict(capacity_bonus=5, cost=1500, daily_cost=70),
            dict(capacity_bonus=5, cost=2000, daily_cost=100),
            dict(capacity_bonus=5, cost=2500, daily_cost=130),
            dict(capacity_bonus=5, cost=3000, daily_cost=160),
        ])
        del current['housing_extensions']
        del current['rules_id']
        current['rules_id'] = hashlib.sha256(json.dumps(current, sort_keys=True, ensure_ascii=True).encode()).hexdigest()
        self.assertEqual(current, legacy)

    def test_export_and_presentation_are_current(self):
        resources = TOOL / 'unity_preview_template/Resources/CatCafeStandalone'
        saved = json.loads((resources / 'rules.json').read_text(encoding='utf-8'))
        self.assertEqual(exported_rules(), saved)
        self.assertEqual(json.loads((resources / 'presentation.json').read_text(encoding='utf-8'))['rules_id'], saved['rules_id'])
