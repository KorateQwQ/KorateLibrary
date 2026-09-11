using System.Runtime.CompilerServices;

namespace KL.AttributeSystem;

public enum AttributeKind
{
    /// <summary>
    /// 每帧重置的属性类型，一般为受装备buff加成的属性
    /// </summary>
    Rebuilt,
    /// <summary>
    /// 不需要重置的属性类型，一般为持续统计的资源类型
    /// </summary>
    Resource,
}

/// <summary>
/// 声明单个角色属性及其静态取值范围。
/// 必须以 static readonly 字段保存并复用定义；属性身份由对象引用决定。
/// </summary>
public sealed class AttributeDefinition
{
    /// <summary>创建属性定义，应直接保存到 static readonly 字段中供所有使用方复用。</summary>
    /// <param name="defaultBaseValue">默认白值；普通属性每帧重置到此值，资源属性以此值开始。</param>
    /// <param name="minValue">最终值的静态下限。</param>
    /// <param name="maxValue">最终值的静态上限。</param>
    /// <param name="kind">普通属性每帧重建，资源属性保留当前持有量。</param>
    /// <param name="declarationName">
    /// 调用方应省略此参数。CallerMemberName 会让编译器自动填入调用成员名；
    /// 直接在字段初始化器中构造时，得到字段名，例如 Power。
    /// 仅用于异常日志诊断，不是属性 ID，不参与属性识别、查找或序列化。
    /// 经由工厂方法构造时，得到的是工厂中的调用成员名，不一定是最终字段名。
    /// </param>
    public AttributeDefinition(float defaultBaseValue = 0f, float minValue = float.NegativeInfinity,
        float maxValue = float.PositiveInfinity, AttributeKind kind = AttributeKind.Rebuilt,
        [CallerMemberName] string declarationName = "")
    {
        if (float.IsNaN(minValue) || float.IsNaN(maxValue) || minValue > maxValue)
        {
            throw new ArgumentException("Attribute minimum cannot be greater than its maximum.", nameof(minValue));
        }

        if (kind != AttributeKind.Rebuilt && kind != AttributeKind.Resource)
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (!float.IsFinite(defaultBaseValue))
            throw new ArgumentOutOfRangeException(nameof(defaultBaseValue));

        // 保存编译器提供的诊断名称；属性身份仍由定义对象的引用决定。
        DiagnosticName = declarationName;
        DefaultBaseValue = defaultBaseValue;
        MinValue = minValue;
        MaxValue = maxValue;
        Kind = kind;
    }

    // 编译器自动传入字段名，仅用于诊断，不参与属性身份、查找或序列化。
    internal string DiagnosticName { get; }

    public float DefaultBaseValue { get; }

    public float MinValue { get; }

    public float MaxValue { get; }

    public AttributeKind Kind { get; }

    public float ClampFinalValue(float value)
    {
        return MathF.Min(MathF.Max(value, MinValue), MaxValue);
    }
}

public enum AttributeModifierKind
{
    Flat,
    Percent,
}
