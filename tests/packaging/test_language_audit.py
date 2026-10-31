import importlib.util
from pathlib import Path
import unittest

SCRIPT = Path(__file__).resolve().parents[2] / "scripts" / "audit-se-languages.py"
SPEC = importlib.util.spec_from_file_location("subtitleedit_language_audit", SCRIPT)
audit_module = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(audit_module)


class PortugueseAuditTests(unittest.TestCase):
    def test_matching_nested_keys_and_placeholders(self):
        en = {"version": "x", "group": {"hello": "Hello {0}", "name": "Codec"}}
        pt = {"version": "x", "group": {"hello": "Olá {0}", "name": "Codec"}}
        result = audit_module.audit(en, pt)
        self.assertEqual(result["missing"], [])
        self.assertEqual(result["placeholder_mismatches"], [])
        self.assertEqual(result["same_english_review_candidates"], ["group.name"])

    def test_missing_and_wrong_placeholder(self):
        en = {"group": {"hello": "Hello {0}", "other": "Other"}}
        pt = {"group": {"hello": "Olá {1}"}}
        result = audit_module.audit(en, pt)
        self.assertEqual(result["missing"], ["group.other"])
        self.assertEqual(result["placeholder_mismatches"], ["group.hello"])

    def test_short_literal_can_be_unchanged(self):
        self.assertEqual(audit_module.audit({"x": "VLC"}, {"x": "VLC"})["missing"], [])


if __name__ == "__main__":
    unittest.main()
