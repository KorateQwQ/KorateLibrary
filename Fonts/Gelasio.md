# Gelasio slot labels

Gelasio Regular, pinned at `wght=400`, baked at 36pt / 48px. Loaded as
`FontManager.Gelasio` from `KL/Fonts/Gelasio` on clients and released on unload.
The 15 KB XNB contains 95 printable ASCII characters (U+0020–U+007E), including
all slot digits. It intentionally contains no Chinese or supplementary characters.
Chinese text continues to use NotoSerifSC and HarmonyOS_Sans_SC.

Gelasio is an OFL-licensed, Georgia metrics-compatible alternative; it is not the
Georgia font from the HTML. The source URL, revision and SHA-256 are recorded in
`Gelasio.source.json`; redistribution terms are in `Gelasio-OFL.txt`.

To rebuild with the `terraria-font-import` skill, download the exact source URL
in the manifest to an isolated work directory, then run:

```powershell
python <skill>/scripts/prepare_font.py --font <source.ttf> --output <work> --family 'KL Gelasio' --weight 400 --size 36 --include 'U+0020-U+007E'
pwsh -File <skill>/scripts/build_xnb.ps1 -Work <work> -Generator <DynamicFontGenerator.exe>
```

Copy `KLGelasio.xnb` and `KLGelasio.dynamicfont` to `Fonts/Gelasio.xnb` and
`Fonts/Gelasio.dynamicfont`. The internal family name stays `KL Gelasio`.
No permanent Windows font installation is required.

The Elaina `Tools/TypographyPreview/run.ps1` runner loads the actual XNB with
FNA/ReLogic, checks digits, and renders current footer code at 100% and 150%.
Use the font's baked 48px em for sizing; measuring a Chinese character would be
invalid for this ASCII-only asset. Rebuild/reload KL before loading the updated UI.
