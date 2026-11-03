using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;

namespace KL.Drawing;

/// <summary>
/// A 48px master font plus copies pre-baked at exact pixel sizes (Fonts/Name_px).
/// When the on-screen em rounds to a baked size, text is measured and drawn with that copy at 1:1 on the
/// device pixel grid, keeping the generator's hinted strokes. Other sizes, and text with characters the
/// size copy was not baked with, fall back to the scaled master.
/// </summary>
public sealed class PixelFont(Asset<DynamicSpriteFont> master, IReadOnlyDictionary<int, Asset<DynamicSpriteFont>> sizes)
{
    private const float MasterEm = 48;

    public DynamicSpriteFont Master => master.Value;

    /// <summary>The font that draws <paramref name="text"/> at this on-screen em (device pixels).</summary>
    public DynamicSpriteFont Resolve(string text, float em)
    {
        if (!sizes.TryGetValue((int)(em + .5f), out var size) || !size.IsLoaded) return master.Value;
        var glyphs = size.Value.SpriteCharacters;
        foreach (char c in text)
            if (c is not ('\n' or '\r') && !glyphs.ContainsKey(c)) return master.Value;
        return size.Value;
    }

    /// <summary>Width of <paramref name="text"/> in device pixels at this on-screen em.</summary>
    public float Measure(string text, float em)
    {
        var font = Resolve(text, em);
        return font.MeasureString(text).X * (font == master.Value ? em / MasterEm : 1);
    }

    /// <summary>
    /// Draws at <paramref name="position"/> in batch space; <paramref name="transform"/> is the batch matrix
    /// (axis-aligned and uniformly scaled, as for UI), which maps batch units to device pixels.
    /// </summary>
    public void Draw(SpriteBatch batch, string text, Vector2 position, Color color, float em, Matrix transform)
    {
        if (string.IsNullOrEmpty(text)) return;
        float device = transform.M11;
        var font = Resolve(text, em);
        if (font == master.Value)
        {
            batch.DrawString(font, text, position, color, 0, Vector2.Zero, em / MasterEm / device, SpriteEffects.None, 0);
            return;
        }
        // Whole-pixel origin plus integer glyph offsets put every glyph texel on one device pixel.
        position.X = (MathF.Round(position.X * device + transform.M41) - transform.M41) / device;
        position.Y = (MathF.Round(position.Y * transform.M22 + transform.M42) - transform.M42) / transform.M22;
        batch.DrawString(font, text, position, color, 0, Vector2.Zero, 1 / device, SpriteEffects.None, 0);
    }
}
