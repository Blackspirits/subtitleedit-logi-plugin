# Portuguese (Portugal) localisation audit — 2026-10-31

## Scope and source

Compared the official Subtitle Edit 5 language JSON files on `SubtitleEdit/subtitleedit@main`:
`src/ui/Assets/Languages/English.json`, `Portuguese.json`, and
`Portuguese (Brazil).json`. The upstream pt-PT file identifies Hugo Carvalho and
Filipe Mota (BlackSpirits) among its translators.

## Findings

- Before the upstream correction: 4,958 English leaf keys; 4,953 pt-PT keys;
  5 absent keys; no blank pt-PT values; 0 mismatched indexed placeholders.
- The five missing pt-PT keys concerned replace-from-start confirmations (3)
  and two shortcuts for pushing the adjacent subtitle when editing timings (2).
- 60 strings equal English, mostly proper names, technology labels and literals.
  This alone is not evidence of untranslated strings.
- Identified awkward word order in video seek commands, selection extension,
  accessibility text and the canvas labels.
- The published Logitech plugin XLIFF had 75/78 action labels translated but
  27 other UI strings (groups, dials, reload, descriptions) untranslated.
  `scripts/localization-overrides-pt-PT.json` supplies reviewed translations;
  the 105-unit pt-PT XLIFF now has 105 populated targets.

## Upstream contribution

The branch `Blackspirits/subtitleedit:fix/ptpt-l10n-audit-20261031` adds the
five missing JSON keys and improves 11 existing entries while retaining all
placeholder sequences. This is a narrow, reviewable localisation contribution;
it is not a claim that all 4,958 strings underwent manual linguistic review.

## Regeneration contract

`gen-localization.py` reads Subtitle Edit's `ShortcutsMain.cs` mappings and
language JSON files and then applies the pt-PT plugin-only overrides. The
overrides must be reviewed when the upstream wording changes; do not modify
generated XLIFF by hand without updating the source or the overrides.

## Remaining validation

- Run the plugin against a real Logitech device on Windows and macOS.
- Check visible truncation, diacritics and screen-reader descriptions.
- Verify the generated XLIFF against the *current* Subtitle Edit source
  after the upstream pt-PT PR is merged.
