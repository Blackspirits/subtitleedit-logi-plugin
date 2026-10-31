import pathlib
import xml.etree.ElementTree as ET
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]


class PortugueseTranslationTests(unittest.TestCase):
    def test_all_plugin_portuguese_targets_are_filled(self):
        path = ROOT / "SubtitleEditPlugin/package/localization/SubtitleEdit_pt-PT.xliff"
        root = ET.parse(path).getroot()
        items = list(root.iter("trans-unit"))
        self.assertEqual(len(items), 105)
        for item in items:
            source, target = item.find("source"), item.find("target")
            self.assertIsNotNone(source)
            self.assertIsNotNone(target)
            self.assertTrue(target.text and target.text.strip(),
                            "Missing pt-PT: " + item.attrib.get("id", ""))
            self.assertEqual(source.text.count("{0}"), target.text.count("{0}"),
                             "Changed placeholder in " + item.attrib.get("id", ""))


if __name__ == "__main__":
    unittest.main()
