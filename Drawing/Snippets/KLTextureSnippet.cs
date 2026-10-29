using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.UI.Chat;
using Terraria.ModLoader;

namespace KL.Drawing.Snippets;

public sealed class KLTextureSnippet : TextSnippet
{
    private readonly Asset<Texture2D> _texture;
    private readonly float _baseSize;
    private readonly float _yOffset;
    private float _fontScale = 1f;
    private float _fontVerticalOffset;

    /// <summary>klicon 标签中的贴图路径，用于识别 <see cref="KLTerms"/> 中的专有名词。</summary>
    public string Path { get; }

    public KLTextureSnippet(Asset<Texture2D> texture, float baseSize, float yOffset, Color color, string path = null)
        : base(" ", color)
    {
        Path = path;
        _texture = texture;
        _baseSize = baseSize;
        _yOffset = yOffset;
    }

    public void SetFontScale(float fontScale, float fontVerticalOffset)
    {
        _fontScale = fontScale;
        _fontVerticalOffset = fontVerticalOffset;
    }

    public override float GetStringLength(DynamicSpriteFont font)

    {
        return _baseSize * Scale * _fontScale;
    }
    
    public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch,
        Vector2 position = default, Color color = default, float scale = 1f)
    {
        float snippetScale = scale * Scale * _fontScale;
        size = new Vector2(_baseSize, _baseSize) * snippetScale;

        if (justCheckingString)
            return true;

        if (_texture?.Value == null)
            return true;

        var tex = _texture.Value;
        // 非正方形贴图（如原版物品）等比缩放并居中于图标格。
        float drawScale = size.X / Math.Max(tex.Width, tex.Height);
        Vector2 inset = (size - new Vector2(tex.Width, tex.Height) * drawScale) / 2;

        Vector2 drawPosition = position + inset + new Vector2(0, _yOffset * snippetScale + _fontVerticalOffset * scale);
        spriteBatch.Draw(tex, drawPosition, null, color, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);

        
        return true;
    }

}