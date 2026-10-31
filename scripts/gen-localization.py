#!/usr/bin/env python3
"""
Generates the plugin's XLIFF translations from Subtitle Edit's own language files.

Subtitle Edit already ships reviewed translations of every one of these command names
in 26 languages. ShortcutsMain.BuildCommandTranslations() says which language string
belongs to which action, so the plugin's action names are taken from there rather than
translated again.

The neutral XLIFF is produced by the plugin service itself, which is the only reliable
source of the string ids:

    loupedeck://plugin/SubtitleEdit/xliff        (writes localization.generated/)

Strings with no Subtitle Edit equivalent (group names, dial names, descriptions) are
left with an empty target and fall back to English.

Usage:  python3 scripts/gen-localization.py [path-to-subtitleedit-repo]
"""
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parent.parent
SE = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT.parent / "subtitleedit"
NEUTRAL = ROOT / "SubtitleEditPlugin" / "Release" / "localization.generated" / "SubtitleEdit.xliff"
OUT = ROOT / "SubtitleEditPlugin" / "package" / "localization"
PT_OVERRIDES = ROOT / "scripts" / "localization-overrides-pt-PT.json"

ID_PREFIX = "$SubtitleEdit___Loupedeck.SubtitleEditPlugin.SeCommand___"

# Subtitle Edit writes some language files with camelCase keys and others with
# PascalCase. It deserializes case insensitively, so both are valid, and a case
# sensitive lookup here would silently produce six empty languages.
def get_ci(node, key):
    if not isinstance(node, dict):
        return None
    for k, v in node.items():
        if k.lower() == key.lower():
            return v
    return None


def resolve(doc, path):
    node = doc
    for part in path:
        node = get_ci(node, part)
        if node is None:
            return None
    return node if isinstance(node, str) and node.strip() else None


def action_paths():
    """ActionName -> the language path holding its display name."""
    src = (SE / "src/ui/Logic/ShortcutsMain.cs").read_text(encoding="utf-8")
    pairs = re.findall(r"nameof\(MainViewModel\.(\w+)\),\s*Se\.Language\.([\w.]+)", src)
    return {action: path.split(".") for action, path in pairs}


# Culture ids as Subtitle Edit records them, corrected where they are not the
# language code the Logitech software expects.
FIXUP = {"jp-JP": "ja-JP"}   # Japanese is "ja" in ISO 639-1, not "jp"
EXTRA = {"ar": ["ar", "ar-SA"]}  # Subtitle Edit stores a bare "ar"


def cultures():
    out = {}
    for f in sorted((SE / "src/ui/Assets/Languages").glob("*.json")):
        if f.stem == "English":
            continue
        doc = json.load(open(f, encoding="utf-8-sig"))
        code = get_ci(doc, "cultureName")
        if not code:
            continue
        code = FIXUP.get(code, code)
        out[f.stem] = (EXTRA.get(code, [code]), doc)
    return out


def main():
    if not NEUTRAL.exists():
        sys.exit(f"No neutral XLIFF at {NEUTRAL}.\n"
                 f"Load the plugin, then run:  open 'loupedeck://plugin/SubtitleEdit/xliff'")

    paths = action_paths()
    OUT.mkdir(parents=True, exist_ok=True)
    for old in OUT.glob("*.xliff"):
        old.unlink()

    template = NEUTRAL.read_text(encoding="utf-8-sig")
    written = []

    for name, (codes, doc) in cultures().items():
        tree = ET.ElementTree(ET.fromstring(template))
        root = tree.getroot()
        filled = total = 0

        for unit in root.iter("trans-unit"):
            uid = unit.get("id", "")
            target = unit.find("target")
            if target is None:
                target = ET.SubElement(unit, "target")
            target.text = None
            if not uid.startswith(ID_PREFIX):
                continue
            total += 1
            action = uid[len(ID_PREFIX):]
            path = paths.get(action)
            if not path:
                continue
            value = resolve(doc, path)
            if value:
                target.text = value
                filled += 1

        for code in codes:
            for f in root.iter("file"):
                f.set("target-language", code)
            # the service strips its own header from translated files
            for f in root.iter("file"):
                h = f.find("header")
                if h is not None:
                    f.remove(h)
            # Plugin-only strings lack upstream equivalents; a small reviewed override
            # table also corrects known UI labels while upstream fixes are adopted.
            if code == "pt-PT":
                overrides = json.loads(PT_OVERRIDES.read_text(encoding="utf-8"))
                for unit in root.iter("trans-unit"):
                    source = unit.find("source")
                    target = unit.find("target")
                    if source is not None and target is not None and source.text in overrides:
                        target.text = overrides[source.text]

            path = OUT / f"SubtitleEdit_{code}.xliff"
            tree.write(path, encoding="utf-8", xml_declaration=True)
            written.append((code, filled, total))

    print(f"wrote {len(written)} xliff files to {OUT}\n")
    print(f"{'culture':10} {'translated':>12}")
    for code, filled, total in written:
        print(f"  {code:10} {filled:3}/{total}")
    weak = [c for c, f, t in written if f == 0]
    if weak:
        sys.exit(f"\nERROR: no translations resolved for: {', '.join(weak)}")


if __name__ == "__main__":
    main()
