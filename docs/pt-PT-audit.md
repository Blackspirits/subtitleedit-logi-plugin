# Subtitle Edit 5 — Portuguese (Portugal) localisation audit (2026-10-31)

## Scope and reference

Source: `SubtitleEdit/subtitleedit@main`, current `src/ui/Assets/Languages/English.json` (v5.4.0-beta11), corresponding `Portuguese.json`, and `ShortcutsMain.cs` (action-to-resource mappings). The pt-PT file acknowledges Hugo Carvalho (hugok79) and Filipe Mota (BlackSpirits).

This is a **static, full-catalogue structural audit plus a targeted contextual linguistic review**, not a claim to have manually inspected every on-screen control, live voice, tooltip or all 4,958 entries one by one. Actual GUI use and human linguistic review remain mandatory before release.

## Automated whole-catalogue checks

| Metric | Before | Corrected branch |
| --- | ---: | ---: |
| English leaf keys | 4,958 | 4,958 |
| pt-PT leaf keys | 4,953 | 4,958 |
| Missing pt-PT keys | 5 | 0 |
| Extra pt-PT keys | 0 | 0 |
| Empty translations | 0 | 0 |
| Mismatched format placeholders / simple markup | 0 | 0 |
| Old English/Portuguese version metadata | beta11 / beta9 | beta11 / beta11 |

There are approximately 60 strings identical in English and pt-PT, often proper names, keyboard commands or technology identifiers. These are review candidates, not necessarily errors. The upstream pt-BR file was used only to detect missing concepts, **not** as a source of automatic Portuguese (Portugal) wording.

Run the reproducible audit at any time:

```bash
python scripts/audit-se-languages.py ../subtitleedit --json
```

The script requires a local checkout of the current official Subtitle Edit repository, fails for missing/empty keys or changed placeholders, and prints advisory English-identical and possible pt-BR terms.

## Contextual linguistic review

The standalone translation changes are available in
[`Blackspirits/subtitleedit` PR #156](https://github.com/Blackspirits/subtitleedit/pull/156), currently a **draft inside the user's fork**. After adding the five missing strings and correcting 11 wording issues, a wider review found 52 further context errors and restored their intended meaning: **63 corrected existing strings + 5 newly added**.

Selected high-confidence examples:

| Key | Before (incorrect) | Corrected |
| --- | --- | --- |
| `options.chooseLanguage.title` | Imagem de legenda | Escolher idioma da interface |
| `video.videoTransparent.title` | Abrir segunda legenda... | Gerar vídeo transparente com legendas |
| `spellCheck.spellCheck` | Verificação ortográfica cancelada | Verificação ortográfica |
| `options.settings.favoriteSubtitleFormats` | Long explanatory note, not the setting label | Formatos de legenda favoritos |
| `options.settings.downloadMpv` | “mpv lib transferido...” status | Transferir mpv |
| `translate.blockCopyGetFromClipboard` | De: | Obter texto da área de transferência (do tradutor ou da IA) |
| `options.settings.resetShortcuts` | Atalho | Repor atalhos |
| `options.settings.deleteWaveformAndSpectrogramFoldersQuestion` | Forma de onda/espectrograma | Eliminar os ficheiros das pastas «Waveforms» e «Spectrogram»? |
| `assa.drawHelpText` | Missing several user interactions | Restored context-menu, selection, resize/rotate, pan and zoom instructions |
| `general.videoOneSecondBack` | Vídeo, há um segundo | Recuar o vídeo 1 segundo |

Changes preserve the original format tokens and use European Portuguese conventions. Deliberate nonliteral examples are preserved where they teach the same operation. UI-specific mnemonic underscores, ellipses and questions were also reviewed in flagged actions.

## Plugin localisation

The Logitech plugin originally had 75/78 command targets and **zero** of 27 plugin-only UI strings translated. The new `SubtitleEdit_pt-PT.xliff` has **105/105 populated targets** with a reviewed, reproducible `scripts/localization-overrides-pt-PT.json`. The other shipped locale files were left untouched rather than guessed from pt-PT.

`gen-localization.py` derives command names from upstream `ShortcutsMain.cs` and JSON files. pt-PT plugin-only strings are overlaid from the overrides file. Regenerate against the official source, then re-run plugin localisation and packaging tests; do not hand-edit generated translations without updating their source.

## Upstream PR and access

The fork's `main` was synchronised to the official `SubtitleEdit/subtitleedit/main`, preserving its previous head in `backup/main-before-upstream-sync-20261031`; the historical `master` was left untouched. The GitHub connector is permitted to create a draft PR on the fork but returns `HTTP 403 Resource not accessible by integration` when asked to create a PR directly on the official upstream repository. Users can submit the reviewed changes through GitHub's normal cross-fork PR interface.

## Unfinished verification

- Visual language review in running Subtitle Edit 5.4, especially available menu shortcuts, ellipses, translations on narrow controls, theme variants, long tooltips and line breaks.
- Dynamic functional tests with an installed Logitech SDK, Logi Options+ and an MX Master 4; an Actions Ring press on an actual device remains untested by this automated environment.
- Independent pt-PT review of the proposed wording, especially ambiguous toolbar labels and accelerators.
- Confirm any newly added English resources after the source commit snapshot. Run the audit again after upstream updates.
