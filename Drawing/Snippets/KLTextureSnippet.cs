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

    private float lineSpacing = 0f;
    public KLTextureSnippet(Asset<Texture2D> texture, float baseSize, float yOffset, Color color)
        : base(" ", color)
    {
        _texture = texture;
        _baseSize = baseSize;
        _yOffset = yOffset;
    }

    public override float GetStringLength(DynamicSpriteFont font)
    {
        return _baseSize*Scale;
    }
    
    public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch,
        Vector2 position = default, Color color = default, float scale = 1f)
    {
        size = new Vector2(_baseSize, _baseSize) * scale;

        if (justCheckingString)
            return true;

        if (_texture?.Value == null)
            return true;

        var tex = _texture.Value;
        var drawScale = new Vector2(size.X / tex.Width, size.Y / tex.Height);

        spriteBatch.Draw(tex, position + new Vector2(0, _yOffset) * scale, null, color, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);
        
        return true;
    }
}