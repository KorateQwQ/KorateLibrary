using Microsoft.Xna.Framework;
using Terraria.UI.Chat;
using Terraria.ModLoader;
using ReLogic.Content;
using Microsoft.Xna.Framework.Graphics;
using KL.Drawing.Snippets;

namespace KL.Drawing.Snippets;

public sealed class KLTextureTagHandler : ITagHandler
{
    public TextSnippet Parse(string text, Color baseColor, string options)
    {
        var size = 16f;
        var yOffset = 0f;

        if (!string.IsNullOrWhiteSpace(options))
        {
            var parts = options.Split(',');
            if (parts.Length >= 1 && float.TryParse(parts[0], out var parsedSize))
                size = parsedSize;

            if (parts.Length >= 2 && float.TryParse(parts[1], out var parsedYOffset))
                yOffset = parsedYOffset;
        }

        var asset = ModContent.Request<Texture2D>(text, AssetRequestMode.ImmediateLoad);
        return new KLTextureSnippet(asset, size, yOffset, Color.White);
    }
}