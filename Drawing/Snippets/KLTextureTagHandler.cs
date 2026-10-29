using Microsoft.Xna.Framework;
using Terraria.UI.Chat;
using Terraria.ModLoader;
using ReLogic.Content;
using Microsoft.Xna.Framework.Graphics;
using KL.Drawing.Snippets;
using Terraria.ID;

namespace KL.Drawing.Snippets;

/// <summary>
/// klicon 标签：<c>[klicon/尺寸,纵向偏移:贴图路径]</c>。路径写成 <c>Item:原版物品名</c>（如 <c>Item:Cauldron</c>）时使用原版物品贴图。
/// </summary>
public sealed class KLTextureTagHandler : ITagHandler
{
    private const string ItemPrefix = "Item:";

    /// <summary>按 klicon 路径请求贴图，支持 <c>Item:原版物品名</c>。</summary>
    public static Asset<Texture2D> RequestIcon(string path)
    {
        if (path.StartsWith(ItemPrefix) && ItemID.Search.TryGetId(path[ItemPrefix.Length..], out int type))
            path = "Terraria/Images/Item_" + type;
        return ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
    }

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

        return new KLTextureSnippet(RequestIcon(text), size, yOffset, Color.White, text);
    }
}