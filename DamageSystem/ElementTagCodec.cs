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
     * - 只有 a.b 形式下 b 为单个十进制位、且 a 不超过 MaxEncodableBaseKnockback 时才会编码，
     *   否则原样返回击退值（宁可不带元素，也不能污染击退数值）。
     */

    // 前缀必须为两位数（10-99）。
    public static int TagPrefix { get; set; } = 91;

    // 真实击退的可用上限：编码后 /100000 只占小数点后第 5 位，因此整数部分最多两位数，
    // 即 baseKb 最大 99.9；这里取 9.9 作为更保守的安全边界。
    public const float MaxEncodableBaseKnockback = 9.9f;

    static float NormalizeBaseKnockback(float knockback)
        => MathF.Truncate(knockback * 10f) / 10f;

    // 判断击退是否还能安全承载 tag：必须有限、非负，且规范化后整数部分不超过一位数。
    // 小数位多于 1 位是允许的（规范化会截断），这不是风险；风险在于整数部分太长时，
    // baseKb + tag/100000f 会发生进位，解码端算出的 baseKb 会落到另一个整数位上，
    // 于是击退被改成错误的值。
    static bool IsBaseKnockbackSafe(float knockback)
    {
        // NaN / Infinity 会在截断与减法中产生无意义的小数部分，直接拒绝。
        if (!float.IsFinite(knockback))
            return false;

        if (knockback < 0f)
            return false;

        // 9.95、9.99 这类会被截断成 9.9，仍然安全；10.0 及以上则越界。
        return NormalizeBaseKnockback(knockback) <= MaxEncodableBaseKnockback;
    }

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

        // 击退超出安全范围时放弃编码，保留原始击退，避免把数值写坏。
        if (!IsBaseKnockbackSafe(knockback))
            return knockback;

        float baseKb = NormalizeBaseKnockback(knockback);

        // 追加4位到小数点后：base(1位小数) + tag(4位) => /100000
        float encoded = baseKb + tag / 100000f;

        // 浮点加法本身发生了失真（编码值不再能还原出 baseKb 与 tag）时同样放弃编码。
        if (MathF.Round((encoded - baseKb) * 100000f) != tag)
            return knockback;

        return encoded;
    }

    public static bool TryDecodeFromKnockback(ref float knockback, out byte elementId)
    {
        elementId = 0;

        // 非法击退值不做任何“规范化”改写，原样交还给调用方。
        if (!float.IsFinite(knockback))
            return false;

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