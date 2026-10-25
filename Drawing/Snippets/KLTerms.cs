using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace KL.Drawing.Snippets;

/// <summary>
/// 富文本中的专有名词，写法为 klicon 图标紧跟同色名称，例如 <c>[klicon/16,0:路径][c/颜色:名称]</c>。
/// 支持悬停的界面按图标路径识别，并显示名称与说明；名称和说明在显示时读取，以跟随当前语言。
/// <paramref name="Stats"/> 可选，给出固定规则数值（标签、数值、单位），说明卡以大号数字分栏显示。
/// </summary>
public sealed record KLTerm(string IconPath, Color Color, Func<string> Name, Func<string> Description,
    Func<(string Label, string Value, string Unit)[]> Stats = null);

/// <summary>专有名词注册表，以 klicon 图标路径为键；各模组在 Load 中注册自己的名词。</summary>
public static class KLTerms
{
    private static readonly Dictionary<string, KLTerm> Terms = new(StringComparer.Ordinal);

    /// <summary>注册或替换以 <see cref="KLTerm.IconPath"/> 为键的专有名词。</summary>
    public static void Register(KLTerm term) => Terms[term.IconPath] = term;

    /// <summary>按 klicon 贴图路径查找专有名词；未注册时返回 null。</summary>
    public static KLTerm Find(string iconPath)
        => iconPath != null && Terms.TryGetValue(iconPath, out var term) ? term : null;

    /// <summary>卸载时清空注册表。</summary>
    public static void Clear() => Terms.Clear();
}
