using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace KL.DamageSystem;

internal static class ElementTables
{
    // enum -> id：实体只存 enum，通过字典找到需要塞进 Knockback 的元素 id
    public static readonly Dictionary<ElementType, byte> TypeToId = new()
    {
        [ElementType.Fire] = 1,
        [ElementType.Ice] = 2,
        [ElementType.Lightning] = 3,
        [ElementType.Wind] = 4,
        [ElementType.Water] = 5,
    };

    // id -> Color：Strike 端解码出 id 后用于染色飘字
    public static readonly Dictionary<byte, Color> IdToColor = new()
    {
        [1] = new Color(255, 60, 40),
        [2] = new Color(120, 210, 255),
        [3] = new Color(255, 207, 73),
        [4] = new Color(150, 255, 200),
        [5] = new Color(0, 146, 255, 255),
    };

    public static bool TryGetId(ElementType type, out byte id)
    {
        if (type == ElementType.None)
        {
            id = 0;
            return false;
        }

        return TypeToId.TryGetValue(type, out id);
    }
}

internal static class ElementHitInfoHelper
{
    // 将元素枚举映射为元素 id，并按约定写入 HitInfo.Knockback
    public static void TryAttachElementTag(ref NPC.HitModifiers modifiers, ElementType elementType)
    {
        if (!ElementTables.TryGetId(elementType, out byte elementId))
            return;

        modifiers.ModifyHitInfo += (ref NPC.HitInfo info) =>
        {
            info.Knockback = ElementTagCodec.EncodeIntoKnockback(info.Knockback, elementId);
        };
    }
}
