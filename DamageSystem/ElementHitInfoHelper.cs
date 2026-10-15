using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace KL.DamageSystem;

internal static class ElementTables
{
    // 51..55是对应1..5元素的无积蓄命中tag；沿用现有网络击退编码，无需客户端命中后回滚进度。
    public const byte NoBuildupOffset = 50;

    public static bool TryReadTag(byte tag, out byte elementId, out Color color, out bool suppressBuildup)
    {
        suppressBuildup = tag > NoBuildupOffset;
        elementId = suppressBuildup ? (byte)(tag - NoBuildupOffset) : tag;
        return IdToColor.TryGetValue(elementId, out color);
    }
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
    public static void TryAttachElementTag(ref NPC.HitModifiers modifiers, ElementType elementType, bool suppressBuildup = false)
    {
        if (!ElementTables.TryGetId(elementType, out byte elementId))
            return;
        // 冰积蓄点数由来源攻击另行发送；任何冰伤tag都不能再领取全局固定积蓄。
        if (suppressBuildup || elementType == ElementType.Ice) elementId += ElementTables.NoBuildupOffset;

        modifiers.ModifyHitInfo += (ref NPC.HitInfo info) =>
        {
            info.Knockback = ElementTagCodec.EncodeIntoKnockback(info.Knockback, elementId);
        };
    }
}
