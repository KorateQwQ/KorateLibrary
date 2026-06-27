using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Bestiary;

namespace KL.DamageSystem;

/// <summary>
/// NPC 在原版图鉴中记录的生成环境标签，可通过位标记同时表示多个环境。
/// </summary>
[Flags]
public enum NpcEnvironmentFlags : long
{
    /// <summary>
    /// 没有匹配到任何环境标签。
    /// </summary>
    None = 0,

    /// <summary>
    /// 地表环境。
    /// </summary>
    Surface = 1 << 0,

    /// <summary>
    /// 地下层环境。
    /// </summary>
    Underground = 1 << 1,

    /// <summary>
    /// 洞穴层环境。
    /// </summary>
    Caverns = 1 << 2,

    /// <summary>
    /// 天空环境。
    /// </summary>
    Sky = 1 << 3,

    /// <summary>
    /// 海洋环境。
    /// </summary>
    Ocean = 1 << 4,

    /// <summary>
    /// 沙漠环境。
    /// </summary>
    Desert = 1 << 5,

    /// <summary>
    /// 地下沙漠环境。
    /// </summary>
    UndergroundDesert = 1 << 6,

    /// <summary>
    /// 绿洲环境。
    /// </summary>
    Oasis = 1 << 7,

    /// <summary>
    /// 雪地环境。
    /// </summary>
    Snow = 1 << 8,

    /// <summary>
    /// 地下雪地环境。
    /// </summary>
    UndergroundSnow = 1 << 9,

    /// <summary>
    /// 丛林环境。
    /// </summary>
    Jungle = 1 << 10,

    /// <summary>
    /// 地下丛林环境。
    /// </summary>
    UndergroundJungle = 1 << 11,

    /// <summary>
    /// 地表发光蘑菇地环境。
    /// </summary>
    SurfaceMushroom = 1 << 12,

    /// <summary>
    /// 地下发光蘑菇地环境。
    /// </summary>
    MushroomUnderground = 1 << 13,

    /// <summary>
    /// 腐化之地环境。
    /// </summary>
    Corruption = 1 << 14,

    /// <summary>
    /// 地下腐化之地环境。
    /// </summary>
    UndergroundCorruption = 1 << 15,

    /// <summary>
    /// 腐化沙漠环境。
    /// </summary>
    CorruptDesert = 1 << 16,

    /// <summary>
    /// 地下腐化沙漠环境。
    /// </summary>
    CorruptUndergroundDesert = 1 << 17,

    /// <summary>
    /// 腐化雪地环境。
    /// </summary>
    CorruptIce = 1 << 18,

    /// <summary>
    /// 猩红之地环境。
    /// </summary>
    Crimson = 1 << 19,

    /// <summary>
    /// 地下猩红之地环境。
    /// </summary>
    UndergroundCrimson = 1 << 20,

    /// <summary>
    /// 猩红沙漠环境。
    /// </summary>
    CrimsonDesert = 1 << 21,

    /// <summary>
    /// 地下猩红沙漠环境。
    /// </summary>
    CrimsonUndergroundDesert = 1 << 22,

    /// <summary>
    /// 猩红雪地环境。
    /// </summary>
    CrimsonIce = 1 << 23,

    /// <summary>
    /// 神圣之地环境。
    /// </summary>
    Hallow = 1 << 24,

    /// <summary>
    /// 地下神圣之地环境。
    /// </summary>
    UndergroundHallow = 1 << 25,

    /// <summary>
    /// 神圣沙漠环境。
    /// </summary>
    HallowDesert = 1 << 26,

    /// <summary>
    /// 地下神圣沙漠环境。
    /// </summary>
    HallowUndergroundDesert = 1 << 27,

    /// <summary>
    /// 神圣雪地环境。
    /// </summary>
    HallowIce = 1 << 28,

    /// <summary>
    /// 地牢环境。
    /// </summary>
    Dungeon = 1 << 29,

    /// <summary>
    /// 丛林神庙环境。
    /// </summary>
    Temple = 1 << 30,

    /// <summary>
    /// 地狱环境。
    /// </summary>
    Underworld = 1L << 31,

    /// <summary>
    /// 花岗岩洞环境。
    /// </summary>
    Granite = 1L << 32,

    /// <summary>
    /// 大理石洞环境。
    /// </summary>
    Marble = 1L << 33,

    /// <summary>
    /// 陨石环境。
    /// </summary>
    Meteor = 1L << 34,

    /// <summary>
    /// 墓地环境。
    /// </summary>
    Graveyard = 1L << 35,

    /// <summary>
    /// 蜘蛛洞环境。
    /// </summary>
    SpiderNest = 1L << 36,

    /// <summary>
    /// 星云柱区域环境。
    /// </summary>
    NebulaPillar = 1L << 37,

    /// <summary>
    /// 日耀柱区域环境。
    /// </summary>
    SolarPillar = 1L << 38,

    /// <summary>
    /// 星旋柱区域环境。
    /// </summary>
    VortexPillar = 1L << 39,

    /// <summary>
    /// 星尘柱区域环境。
    /// </summary>
    StardustPillar = 1L << 40,
}

public static class NpcEnvironmentHelper
{
    /// <summary>
    /// 所有丛林类型环境。
    /// </summary>
    public const NpcEnvironmentFlags JungleType = NpcEnvironmentFlags.Jungle
        | NpcEnvironmentFlags.UndergroundJungle;

    /// <summary>
    /// 所有冰雪类型环境。
    /// </summary>
    public const NpcEnvironmentFlags SnowType = NpcEnvironmentFlags.Snow
        | NpcEnvironmentFlags.UndergroundSnow
        | NpcEnvironmentFlags.CorruptIce
        | NpcEnvironmentFlags.CrimsonIce
        | NpcEnvironmentFlags.HallowIce;

    /// <summary>
    /// 所有沙漠类型环境，不包含绿洲。
    /// </summary>
    public const NpcEnvironmentFlags DesertType = NpcEnvironmentFlags.Desert
        | NpcEnvironmentFlags.UndergroundDesert
        | NpcEnvironmentFlags.CorruptDesert
        | NpcEnvironmentFlags.CorruptUndergroundDesert
        | NpcEnvironmentFlags.CrimsonDesert
        | NpcEnvironmentFlags.CrimsonUndergroundDesert
        | NpcEnvironmentFlags.HallowDesert
        | NpcEnvironmentFlags.HallowUndergroundDesert;

    /// <summary>
    /// 所有发光蘑菇地类型环境。
    /// </summary>
    public const NpcEnvironmentFlags MushroomType = NpcEnvironmentFlags.SurfaceMushroom
        | NpcEnvironmentFlags.MushroomUnderground;

    /// <summary>
    /// 所有腐化类型环境。
    /// </summary>
    public const NpcEnvironmentFlags CorruptionType = NpcEnvironmentFlags.Corruption
        | NpcEnvironmentFlags.UndergroundCorruption
        | NpcEnvironmentFlags.CorruptDesert
        | NpcEnvironmentFlags.CorruptUndergroundDesert
        | NpcEnvironmentFlags.CorruptIce;

    /// <summary>
    /// 所有猩红类型环境。
    /// </summary>
    public const NpcEnvironmentFlags CrimsonType = NpcEnvironmentFlags.Crimson
        | NpcEnvironmentFlags.UndergroundCrimson
        | NpcEnvironmentFlags.CrimsonDesert
        | NpcEnvironmentFlags.CrimsonUndergroundDesert
        | NpcEnvironmentFlags.CrimsonIce;

    /// <summary>
    /// 所有神圣类型环境。
    /// </summary>
    public const NpcEnvironmentFlags HallowType = NpcEnvironmentFlags.Hallow
        | NpcEnvironmentFlags.UndergroundHallow
        | NpcEnvironmentFlags.HallowDesert
        | NpcEnvironmentFlags.HallowUndergroundDesert
        | NpcEnvironmentFlags.HallowIce;

    /// <summary>
    /// 所有月亮事件柱类型环境。
    /// </summary>
    public const NpcEnvironmentFlags LunarPillarType = NpcEnvironmentFlags.NebulaPillar
        | NpcEnvironmentFlags.SolarPillar
        | NpcEnvironmentFlags.VortexPillar
        | NpcEnvironmentFlags.StardustPillar;

    private static readonly Dictionary<IBestiaryInfoElement, NpcEnvironmentFlags> EnvironmentMap = new()
    {
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface] = NpcEnvironmentFlags.Surface,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground] = NpcEnvironmentFlags.Underground,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns] = NpcEnvironmentFlags.Caverns,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky] = NpcEnvironmentFlags.Sky,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Ocean] = NpcEnvironmentFlags.Ocean,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Desert] = NpcEnvironmentFlags.Desert,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundDesert] = NpcEnvironmentFlags.UndergroundDesert,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Oasis] = NpcEnvironmentFlags.Oasis,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Snow] = NpcEnvironmentFlags.Snow,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundSnow] = NpcEnvironmentFlags.UndergroundSnow,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Jungle] = NpcEnvironmentFlags.Jungle,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundJungle] = NpcEnvironmentFlags.UndergroundJungle,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.SurfaceMushroom] = NpcEnvironmentFlags.SurfaceMushroom,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundMushroom] = NpcEnvironmentFlags.MushroomUnderground,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheCorruption] = NpcEnvironmentFlags.Corruption,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundCorruption] = NpcEnvironmentFlags.UndergroundCorruption,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.CorruptDesert] = NpcEnvironmentFlags.CorruptDesert,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.CorruptUndergroundDesert] = NpcEnvironmentFlags.CorruptUndergroundDesert,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.CorruptIce] = NpcEnvironmentFlags.CorruptIce,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheCrimson] = NpcEnvironmentFlags.Crimson,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundCrimson] = NpcEnvironmentFlags.UndergroundCrimson,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.CrimsonDesert] = NpcEnvironmentFlags.CrimsonDesert,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.CrimsonUndergroundDesert] = NpcEnvironmentFlags.CrimsonUndergroundDesert,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.CrimsonIce] = NpcEnvironmentFlags.CrimsonIce,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheHallow] = NpcEnvironmentFlags.Hallow,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundHallow] = NpcEnvironmentFlags.UndergroundHallow,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.HallowDesert] = NpcEnvironmentFlags.HallowDesert,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.HallowUndergroundDesert] = NpcEnvironmentFlags.HallowUndergroundDesert,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.HallowIce] = NpcEnvironmentFlags.HallowIce,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheDungeon] = NpcEnvironmentFlags.Dungeon,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheTemple] = NpcEnvironmentFlags.Temple,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheUnderworld] = NpcEnvironmentFlags.Underworld,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Granite] = NpcEnvironmentFlags.Granite,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Marble] = NpcEnvironmentFlags.Marble,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Meteor] = NpcEnvironmentFlags.Meteor,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Graveyard] = NpcEnvironmentFlags.Graveyard,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.SpiderNest] = NpcEnvironmentFlags.SpiderNest,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.NebulaPillar] = NpcEnvironmentFlags.NebulaPillar,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.SolarPillar] = NpcEnvironmentFlags.SolarPillar,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.VortexPillar] = NpcEnvironmentFlags.VortexPillar,
        [BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.StardustPillar] = NpcEnvironmentFlags.StardustPillar,
    };

    private static readonly NpcEnvironmentFlags[] PrimaryEnvironmentPriority =
    [
        NpcEnvironmentFlags.SpiderNest,
        NpcEnvironmentFlags.Temple,
        NpcEnvironmentFlags.Dungeon,
        NpcEnvironmentFlags.Underworld,
        NpcEnvironmentFlags.Granite,
        NpcEnvironmentFlags.Marble,
        NpcEnvironmentFlags.Meteor,
        NpcEnvironmentFlags.Graveyard,
        NpcEnvironmentFlags.Ocean,
        NpcEnvironmentFlags.Oasis,
        NpcEnvironmentFlags.HallowUndergroundDesert,
        NpcEnvironmentFlags.CrimsonUndergroundDesert,
        NpcEnvironmentFlags.CorruptUndergroundDesert,
        NpcEnvironmentFlags.UndergroundDesert,
        NpcEnvironmentFlags.HallowDesert,
        NpcEnvironmentFlags.CrimsonDesert,
        NpcEnvironmentFlags.CorruptDesert,
        NpcEnvironmentFlags.Desert,
        NpcEnvironmentFlags.HallowIce,
        NpcEnvironmentFlags.CrimsonIce,
        NpcEnvironmentFlags.CorruptIce,
        NpcEnvironmentFlags.UndergroundSnow,
        NpcEnvironmentFlags.Snow,
        NpcEnvironmentFlags.SurfaceMushroom,
        NpcEnvironmentFlags.MushroomUnderground,
        NpcEnvironmentFlags.UndergroundJungle,
        NpcEnvironmentFlags.Jungle,
        NpcEnvironmentFlags.UndergroundHallow,
        NpcEnvironmentFlags.Hallow,
        NpcEnvironmentFlags.UndergroundCrimson,
        NpcEnvironmentFlags.Crimson,
        NpcEnvironmentFlags.UndergroundCorruption,
        NpcEnvironmentFlags.Corruption,
        NpcEnvironmentFlags.Caverns,
        NpcEnvironmentFlags.Underground,
        NpcEnvironmentFlags.Sky,
        NpcEnvironmentFlags.Surface,
        NpcEnvironmentFlags.NebulaPillar,
        NpcEnvironmentFlags.SolarPillar,
        NpcEnvironmentFlags.VortexPillar,
        NpcEnvironmentFlags.StardustPillar,
    ];

    /// <summary>
    /// 获取指定 NPC 类型在原版图鉴中记录的全部环境标签。
    /// </summary>
    public static NpcEnvironmentFlags GetEnvironment(int npcType)
    {
        if (npcType <= NPCID.None || npcType >= NPCID.Count)
            return NpcEnvironmentFlags.None;

        if (Main.BestiaryDB == null)
            return NpcEnvironmentFlags.None;

        BestiaryEntry entry = Main.BestiaryDB.FindEntryByNPCID(npcType);
        if (entry == null || entry.Info == null || entry.Info.Count == 0)
            return NpcEnvironmentFlags.None;

        NpcEnvironmentFlags result = NpcEnvironmentFlags.None;
        foreach (IBestiaryInfoElement element in entry.Info)
        {
            result |= TryMapEnvironment(element);
        }

        return result;
    }

    /// <summary>
    /// 获取当前 NPC 类型在原版图鉴中记录的全部环境标签。
    /// </summary>
    public static NpcEnvironmentFlags GetEnvironment(this NPC npc)
    {
        return npc == null ? NpcEnvironmentFlags.None : GetEnvironment(npc.type);
    }

    /// <summary>
    /// 判断当前 NPC 是否包含指定的环境标签。
    /// </summary>
    public static bool HasEnvironment(this NPC npc, NpcEnvironmentFlags environment)
    {
        return npc != null && (GetEnvironment(npc.type) & environment) != 0;
    }

    /// <summary>
    /// 判断指定 NPC 类型是否包含任意丛林类型环境。
    /// </summary>
    public static bool IsJungleType(int npcType)
    {
        return HasAnyEnvironment(npcType, JungleType);
    }

    /// <summary>
    /// 判断当前 NPC 是否包含任意丛林类型环境。
    /// </summary>
    public static bool IsJungleType(this NPC npc)
    {
        return npc != null && IsJungleType(npc.type);
    }

    /// <summary>
    /// 判断指定 NPC 类型是否包含任意冰雪类型环境。
    /// </summary>
    public static bool IsSnowType(int npcType)
    {
        return HasAnyEnvironment(npcType, SnowType);
    }

    /// <summary>
    /// 判断当前 NPC 是否包含任意冰雪类型环境。
    /// </summary>
    public static bool IsSnowType(this NPC npc)
    {
        return npc != null && IsSnowType(npc.type);
    }

    /// <summary>
    /// 判断指定 NPC 类型是否包含任意沙漠类型环境。
    /// </summary>
    public static bool IsDesertType(int npcType)
    {
        return HasAnyEnvironment(npcType, DesertType);
    }

    /// <summary>
    /// 判断当前 NPC 是否包含任意沙漠类型环境。
    /// </summary>
    public static bool IsDesertType(this NPC npc)
    {
        return npc != null && IsDesertType(npc.type);
    }

    /// <summary>
    /// 判断指定 NPC 类型是否包含任意发光蘑菇地类型环境。
    /// </summary>
    public static bool IsMushroomType(int npcType)
    {
        return HasAnyEnvironment(npcType, MushroomType);
    }

    /// <summary>
    /// 判断当前 NPC 是否包含任意发光蘑菇地类型环境。
    /// </summary>
    public static bool IsMushroomType(this NPC npc)
    {
        return npc != null && IsMushroomType(npc.type);
    }

    /// <summary>
    /// 判断指定 NPC 类型是否包含任意腐化类型环境。
    /// </summary>
    public static bool IsCorruptionType(int npcType)
    {
        return HasAnyEnvironment(npcType, CorruptionType);
    }

    /// <summary>
    /// 判断当前 NPC 是否包含任意腐化类型环境。
    /// </summary>
    public static bool IsCorruptionType(this NPC npc)
    {
        return npc != null && IsCorruptionType(npc.type);
    }

    /// <summary>
    /// 判断指定 NPC 类型是否包含任意猩红类型环境。
    /// </summary>
    public static bool IsCrimsonType(int npcType)
    {
        return HasAnyEnvironment(npcType, CrimsonType);
    }

    /// <summary>
    /// 判断当前 NPC 是否包含任意猩红类型环境。
    /// </summary>
    public static bool IsCrimsonType(this NPC npc)
    {
        return npc != null && IsCrimsonType(npc.type);
    }

    /// <summary>
    /// 判断指定 NPC 类型是否包含任意神圣类型环境。
    /// </summary>
    public static bool IsHallowType(int npcType)
    {
        return HasAnyEnvironment(npcType, HallowType);
    }

    /// <summary>
    /// 判断当前 NPC 是否包含任意神圣类型环境。
    /// </summary>
    public static bool IsHallowType(this NPC npc)
    {
        return npc != null && IsHallowType(npc.type);
    }

    /// <summary>
    /// 判断指定 NPC 类型是否包含任意月亮事件柱类型环境。
    /// </summary>
    public static bool IsLunarPillarType(int npcType)
    {
        return HasAnyEnvironment(npcType, LunarPillarType);
    }

    /// <summary>
    /// 判断当前 NPC 是否包含任意月亮事件柱类型环境。
    /// </summary>
    public static bool IsLunarPillarType(this NPC npc)
    {
        return npc != null && IsLunarPillarType(npc.type);
    }

    /// <summary>
    /// 判断指定 NPC 类型是否包含环境组中的任意标签。
    /// </summary>
    public static bool HasAnyEnvironment(int npcType, NpcEnvironmentFlags environmentGroup)
    {
        return (GetEnvironment(npcType) & environmentGroup) != 0;
    }

    /// <summary>
    /// 按预设优先级返回指定 NPC 类型最主要的环境标签。
    /// </summary>
    public static NpcEnvironmentFlags GetPrimaryEnvironment(int npcType)
    {
        NpcEnvironmentFlags flags = GetEnvironment(npcType);
        if (flags == NpcEnvironmentFlags.None)
            return NpcEnvironmentFlags.None;

        foreach (NpcEnvironmentFlags candidate in PrimaryEnvironmentPriority)
        {
            if ((flags & candidate) != 0)
                return candidate;
        }

        return flags;
    }

    /// <summary>
    /// 按预设优先级返回当前 NPC 类型最主要的环境标签。
    /// </summary>
    public static NpcEnvironmentFlags GetPrimaryEnvironment(this NPC npc)
    {
        return npc == null ? NpcEnvironmentFlags.None : GetPrimaryEnvironment(npc.type);
    }

    /// <summary>
    /// 打印所有原版 NPC 类型在图鉴中记录的环境标签。
    /// </summary>
    internal static void DebugLogAllNpcEnvironments()
    {
        for (int npcType = 1; npcType < NPCID.Count; npcType++)
        {
            string npcName = Lang.GetNPCNameValue(npcType);
            if (string.IsNullOrWhiteSpace(npcName))
                continue;

            NpcEnvironmentFlags environment = GetEnvironment(npcType);
            Log($"NPC环境: [{npcType}] {npcName} => {environment}, PrimaryEnvironment: {GetPrimaryEnvironment(npcType)}");
        }
    }

    /// <summary>
    /// 将单个图鉴信息元素映射为对应的环境标签。
    /// </summary>
    private static NpcEnvironmentFlags TryMapEnvironment(IBestiaryInfoElement element)
    {
        if (element == null)
            return NpcEnvironmentFlags.None;

        if (EnvironmentMap.TryGetValue(element, out NpcEnvironmentFlags environment))
            return environment;

        if (element is IPreferenceProviderElement preferenceProvider)
        {
            IBestiaryBackgroundImagePathAndColorProvider provider = preferenceProvider.GetPreferredProvider();
            if (provider is IBestiaryInfoElement providerElement
                && EnvironmentMap.TryGetValue(providerElement, out environment))
            {
                return environment;
            }
        }

        return NpcEnvironmentFlags.None;
    }
}