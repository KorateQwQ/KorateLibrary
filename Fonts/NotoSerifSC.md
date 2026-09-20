# Noto Serif SC

`FontManager.NotoSerifSC.Value` provides a `DynamicSpriteFont` for titles and skill names.
It is a new opt-in font; existing LoliFont/HarmonyOS text is unchanged.

```csharp
using KL.Drawing;

title.Font = FontManager.NotoSerifSC.Value;
title.TextScale = 30f / 48f; // nominal 30px title; measure final layout with this font
```

The resource uses Noto Serif SC Regular, weight 400, at 36pt (nominal 48px).
It is derived from the same installed variable font used by the HTML reference.
`NotoSerifSC.source.json` records the source version and SHA-256.
`NotoSerifSC.dynamicfont` records the exact glyph ranges and generation parameters.
The source is licensed under SIL OFL 1.1; see `NotoSerifSC-OFL.txt`.
Source font copyright: © 2017–2023 Adobe (http://www.adobe.com/).

Coverage: 30,485 printable BMP characters from the source font's actual cmap.
This includes CJK ideographs and Extension A, Latin letters, numbers, punctuation,
Greek, and supported symbols. Supplementary-plane characters and emoji are outside
this char-based resource. Missing characters use `?`. Always check
`font.AreCharactersSupported(text)` when introducing new symbols.
In particular, `✧` and `✦` are absent from this font: draw them as the existing UI
ornaments, or use a separate font explicitly supporting those characters.

This is a bitmap font: `TextScale` controls drawing scale, not regeneration of glyphs.
Use its own `MeasureString` and `LineSpacing` for layout.
The verified resource has `LineSpacing = 69` at scale 1; its five-character Chinese
sample measures 240px wide. A nominal 30px title uses scale 0.625 and has a 43.125px
font line box, so do not equate the CSS font size with the control's total height.
Letter spacing and shadows from CSS need separate UI settings. KLTextView's inline texture offsets for the other
two fonts do not automatically apply to Noto Serif SC; rich icon snippets need their
own visual check. FontManager clears the new asset reference on unload; tML owns the resource.

Rebuild/preview scripts are in the sibling Elaina mod's `Tools/FontPreview` directory.
The generator uses a temporary, uniquely named Regular instance so the old XNA
pipeline cannot select the variable font's weight-200 default. It registers this
instance only for conversion and removes the session registration on process exit;
no font folder or registry is changed. The shipped XNB needs no installed system font.
