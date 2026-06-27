namespace KL.DamageSystem;

/// <summary>
/// 元素类型，如果要添加新的元素类型，记得添加对应图标。
/// </summary>
public enum ElementType : byte
{
    None = 0,
    Fire = 1,
    Ice = 2,
    Lightning = 3,
    Wind = 4,
    Water = 5,
}

public static class ElementTypeHelper
{
    public static Asset<Texture2D> GetElementIconTexture(ElementType type)
    {
        ElementTypeLoader.ElementalTextures.TryGetValue(type, out Asset<Texture2D> texture);
        return texture;
    }

    public static Color GetElementColor(ElementType type)
    {
        return ElementTables.IdToColor[ElementTables.TypeToId[type]];
    }
    
    
}

class ElementTypeLoader : ModSystem
{
    public static Dictionary<ElementType,Asset<Texture2D>> ElementalTextures = new Dictionary<ElementType,Asset<Texture2D>>();

    public override void Load()
    {
        ElementalTextures.Clear();
        foreach (ElementType type in (ElementType[])System.Enum.GetValues(typeof(ElementType)))
        {
            if (type == ElementType.None)
                continue;

            string path = $"KL/Drawing/Snippets/Icons/{type}Icon";
            ElementalTextures[type] = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
        }

        base.Load();
    }

    public override void Unload()
    {
        ElementalTextures.Clear();
    }

}