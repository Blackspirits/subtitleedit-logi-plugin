#!/usr/bin/env python3
"""Audit Subtitle Edit's European Portuguese language file against English.

Usage: python scripts/audit-se-languages.py ../subtitleedit [--json]
Reads the *current* source files; never changes the original translation.
"""
import argparse
from collections import Counter
import json
from pathlib import Path
import re

PLACEHOLDER = re.compile(r"(?<!\{)\{(?:\d+(?:[^{}]*)?|[A-Za-z_][\w.]*)\}|%[sdf]")
PT_BR = re.compile(r"\b(?:você|vocês|usuário|usuários|tela|arquivos?|salvar|baixar)\b", re.I)


def flatten(source, prefix="", result=None):
    result = {} if result is None else result
    if isinstance(source, dict):
        for key, value in source.items():
            flatten(value, prefix + "." + key if prefix else key, result)
    elif isinstance(source, list):
        for i, value in enumerate(source):
            flatten(value, f"{prefix}[{i}]", result)
    else:
        result[prefix] = source
    return result


def audit(en, pt):
    a, b = flatten(en), flatten(pt)
    missing, extra = sorted(set(a) - set(b)), sorted(set(b) - set(a))
    blank, mismatches, same_english, possible_brazilian = [], [], [], []
    for key in sorted(set(a) & set(b)):
        source, target = a[key], b[key]
        if isinstance(source, str) and source.strip() and (
            not isinstance(target, str) or not target.strip()
        ):
            blank.append(key)
        if not isinstance(source, str) or not isinstance(target, str):
            continue
        if Counter(PLACEHOLDER.findall(source)) != Counter(PLACEHOLDER.findall(target)):
            mismatches.append(key)
        if source == target and re.search(r"[A-Za-z]{5,}", source):
            same_english.append(key)  # Advisory: may be a brand or accepted term.
        if PT_BR.search(target):
            possible_brazilian.append(key)  # Advisory, not a hard error.
    return {
        "english_version": en.get("version"), "portuguese_version": pt.get("version"),
        "english_keys": len(a), "portuguese_keys": len(b),
        "missing": missing, "extra": extra, "blank": blank,
        "placeholder_mismatches": mismatches,
        "same_english_review_candidates": same_english,
        "potential_pt_br_terms": possible_brazilian,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("subtitleedit_repo", type=Path)
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    root = args.subtitleedit_repo / "src" / "ui" / "Assets" / "Languages"
    en = json.loads((root / "English.json").read_text(encoding="utf-8-sig"))
    pt = json.loads((root / "Portuguese.json").read_text(encoding="utf-8-sig"))
    result = audit(en, pt)
    if args.json:
        print(json.dumps(result, ensure_ascii=False, indent=2))
    else:
        for key in ("english_version", "portuguese_version", "english_keys", "portuguese_keys"):
            print(f"{key}: {result[key]}")
        for key in ("missing", "extra", "blank", "placeholder_mismatches",
                    "same_english_review_candidates", "potential_pt_br_terms"):
            print(f"{key}: {len(result[key])}")
            for path in result[key][:20]:
                print("  " + path)
    if result["missing"] or result["blank"] or result["placeholder_mismatches"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
