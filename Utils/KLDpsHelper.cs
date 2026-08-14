namespace KL.Utils;

public class KLDpsHelper
{
    public static float GetStateDps(float state)
    {
        float[] stateDps =
        [
            30f,
            50f,   // 史莱姆王
            80f,   // 克苏鲁之眼
            110f,  // 世界吞噬者 / 克苏鲁之脑
            150f,  // 蜂王
            200f,  // 骷髅王
            250f,  // 独眼巨鹿
            350f,  // 血肉墙
            400f,  // 史莱姆皇后
            450f,  // 双子魔眼
            500f,  // 毁灭者
            650f,  // 机械骷髅王
            900f,  // 世纪之花
            1100f, // 石巨人
            1200f, // 猪龙鱼公爵
            1300f, // 光之女皇
            1550f, // 光之女皇与拜月教邪教徒之间的阶段16
            1800f, // 拜月教邪教徒 / 月亮事件 / 四柱
            3500f  // 月亮领主
        ];

        state = Math.Clamp(state, 0f, stateDps.Length - 1);
        int lowerState = (int)MathF.Floor(state);
        int upperState = Math.Min(lowerState + 1, stateDps.Length - 1);
        float progress = state - lowerState;

        return stateDps[lowerState] + (stateDps[upperState] - stateDps[lowerState]) * progress;
    }

    public static float GetLevelDps(int level)
    {
        float state = Math.Max(level, 0) / 5f;
        return GetStateDps(state);
    }

    /// <summary>
    /// 根据理论 DPS、攻击总时间和攻击次数，计算每次攻击应该造成的伤害。
    /// </summary>
    /// <param name="theoreticalDps">技能期望达到的理论每秒伤害。</param>
    /// <param name="totalAttackTime">完整攻击周期的总时间，通常包含冷却时间、前后摇或持续时间等因素。</param>
    /// <param name="attackCount">在完整攻击周期内实际造成伤害的次数。</param>
    /// <returns>每次攻击应该造成的伤害；如果输入无效，则返回 0。</returns>
    public static int GetSingleHitDamage(float theoreticalDps, float totalAttackTime, int attackCount)
    {
        if (theoreticalDps <= 0f || totalAttackTime <= 0f || attackCount <= 0)
        {
            return 0;
        }

        return (int)(theoreticalDps * totalAttackTime / attackCount);
    }
}