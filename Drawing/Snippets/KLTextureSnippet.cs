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


    public KLTextureSnippet(Asset<Texture2D> texture, float baseSize, float yOffset, Color color)


        : base(" ", color)
    {
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
        var drawScale = new Vector2(size.X / tex.Width, size.Y / tex.Height);

        Vector2 drawPosition = position + new Vector2(0, _yOffset * snippetScale + _fontVerticalOffset * scale);
        spriteBatch.Draw(tex, drawPosition, null, color, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);

        
        return true;
    }

}