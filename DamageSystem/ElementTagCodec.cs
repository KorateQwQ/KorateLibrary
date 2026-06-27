namespace KL.DamageSystem;

internal static class ElementTagCodec
{
    /*
     * Knockback 编码约定：a.bxxxx
     * - a.b：真实击退，仅保留 1 位小数（多余小数位直接舍弃，不做四舍五入）
     * - xxxx：元素 tag，固定为“前缀(2位) + 元素id(2位)”，例如：
     *     前缀=91，元素=03 => tag=9103
     *     前缀=66，元素=12 => tag=6612
     *
     * 写入时机：只在 ModifyHitInfo 内把 tag 写入 HitInfo.Knockback
     * 读取时机：只在 StrikeNPC 内解析 tag，并把 Knockback 还原为 a.b
     *
     * 容错/保险：
     * - 如果 Knockback 本身带有更多小数（例如 12.456），会先规范化为 12.4（舍弃 56）。
     * - 如果解析到的小数位并不符合 tag 规则，则认为“无元素tag”，并仍然把 Knockback 规范化为 a.b。
     */

    // 前缀必须为两位数（10-99）。
    public static int TagPrefix { get; set; } = 91;

    static float NormalizeBaseKnockback(float knockback)
        => MathF.Truncate(knockback * 10f) / 10f;

    static bool TryBuildTag(byte elementId, out int tag)
    {
        tag = 0;

        // 元素 id 约定为 01-99
        if (elementId is 0 or > 99)
            return false;

        // 前缀必须为两位数，否则无法保证 tag 恒为4位（前缀2位 + 元素2位）
        if (TagPrefix is < 10 or > 99)
            return false;

        tag = TagPrefix * 100 + elementId;
        return true;
    }

    public static float EncodeIntoKnockback(float knockback, byte elementId)
    {
        if (!TryBuildTag(elementId, out int tag))
            return knockback;

        float baseKb = NormalizeBaseKnockback(knockback);

        // 追加4位到小数点后：base(1位小数) + tag(4位) => /100000
        return baseKb + tag / 100000f;
    }

    public static bool TryDecodeFromKnockback(ref float knockback, out byte elementId)
    {
        elementId = 0;

        float baseKb = NormalizeBaseKnockback(knockback);
        float frac = knockback - baseKb;

        // 取 xxxx（4位）。这里用 Round 是为了对抗 float 误差。
        int tag = (int)MathF.Round(frac * 100000f);

        // 前缀非法时直接视为无 tag，并恢复击退。
        if (TagPrefix is < 10 or > 99)
        {
            knockback = baseKb;
            return false;
        }

        int min = TagPrefix * 100 + 1;
        int max = TagPrefix * 100 + 99;

        if (tag < min || tag > max)
        {
            // 没有 tag：仍然执行“舍弃多余小数位”的规范化
            knockback = baseKb;
            return false;
        }

        elementId = (byte)(tag - TagPrefix * 100);
        knockback = baseKb;
        return true;
    }
}