using System;
using System.Collections.Generic;
using System.Reflection;
using KL.DamageSystem;
using KL.Extensions;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.ModLoader;

namespace KL.Utils;

public class NpcHPGlobalNpc : GlobalNPC
{
    /// <summary>
    /// 战斗中采样血条的间隔（tick）。
    /// </summary>
    private const int SampleInterval = 30;

    public override bool InstancePerEntity => true;

    /// <summary>
    /// 血条上限在整场战斗中的峰值；从未成功读到血条时为 0。
    /// </summary>
    public int RealMaxHP;
    /// <summary>
    /// 最近一次成功采样时血条显示的当前生命。
    /// </summary>
    public int RealHP;
    /// <summary>
    /// 血条读取失败时的回退值：npc.lifeMax 的峰值，兼容战斗中途被强化的 Boss。
    /// </summary>
    internal int PeakLifeMax;

    private int _sampleTimer;

    public override void PostAI(NPC npc)
    {
        // 服务器不会更新血条系统，这里主动按 npc 调用血条计算；结算只在服务端/单机进行，客户端不必采样。
        if (Main.netMode == NetmodeID.MultiplayerClient || --_sampleTimer > 0)
            return;

        _sampleTimer = SampleInterval;
        if (NpcHPHelper.ShouldSample(npc))
            NpcHPHelper.Sample(npc, this);
    }
}

public static class NpcHPHelper
{
    internal static int GetRealMaxHPInternal(NPC npc)
    {
        if (npc == null)
            return 0;

        var g = npc.GetGlobalNPC<NpcHPGlobalNpc>();
        // 结算时再采样一次；死亡瞬间血条常常无效（双子另一只已消失、月总处于死亡动画），失败不会覆盖战斗中的峰值。
        Sample(npc, g);
        if (g.RealMaxHP > 0)
        {
            //PrintText($"GetRealMaxHP From g.RealMaxHP {npc.whoAmI}: {g.RealMaxHP}");
            return g.RealMaxHP;
        }

        return Math.Max(g.PeakLifeMax, npc.lifeMax);
    }

    /// <summary>
    /// Boss 本体与已建立父子关系的部件需要采样；共享 realLife 的体节由主体结算。
    /// </summary>
    internal static bool ShouldSample(NPC npc)
    {
        if (npc.realLife >= 0 && npc.realLife != npc.whoAmI)
            return false;

        if (npc.boss)
            return true;

        var link = npc.GetGlobalNPC<MainParentGlobalNpc>();
        return link.ExpectedParent != null || link.childid is { Count: > 0 };
    }

    internal static void Sample(NPC npc, NpcHPGlobalNpc g)
    {
        if (npc == null || !npc.active)
            return;

        g.PeakLifeMax = Math.Max(g.PeakLifeMax, npc.lifeMax);

        if (!BossBarQuery.TrySampleBar(npc, out float life, out float lifeMax))
            return;

        int sampledMax = (int)MathF.Round(lifeMax);
        if (sampledMax <= 0)
            return;

        g.RealMaxHP = Math.Max(g.RealMaxHP, sampledMax);
        g.RealHP = (int)MathF.Round(life);
    }

    // 可选：在真正绘制该npc的BossBar时同步刷新缓存（与画面一致）
    private class CacheSyncGlobalBossBar : GlobalBossBar
    {
        public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams)
        {
            /*var g = npc.GetGlobalNPC<NpcHPGlobalNpc>();
            g.RealHP = (int)MathF.Round(drawParams.Life);
            g.RealMaxHP = (int)MathF.Round(drawParams.LifeMax);*/
            return base.PreDraw(spriteBatch, npc, ref drawParams);
        }
    }

    internal static class BossBarQuery
    {
        private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly Dictionary<Type, FieldInfo> CacheFieldByBarType = new Dictionary<Type, FieldInfo>();
        private static readonly Dictionary<Type, IBigProgressBar> IsolatedBarInstances = new Dictionary<Type, IBigProgressBar>();
        private static readonly HashSet<Type> ReportedFailures = new HashSet<Type>();

        private static FieldInfo _currentBarField;
        private static FieldInfo _displayInfoField;

        private static CommonBossBigProgressBar _commonBossBar;

        public static IBigProgressBar GetCandidateBarFor(NPC npc)
        {
            if (npc == null)
                return null;

            if (npc.BossBar != null)
                return npc.BossBar;

            if (Main.BigBossProgressBar.TryGetSpecialVanillaBossBar(npc.netID, out IBigProgressBar mapped))
                return mapped;

            _commonBossBar ??= new CommonBossBigProgressBar();
            return _commonBossBar;
        }

        public static IBigProgressBar GetIsolatedInstanceIfPossible(IBigProgressBar bar)
        {
            if (bar == null)
                return null;

            if (bar is ModBossBar)
                return bar;

            Type t = bar.GetType();
            if (t.IsAbstract)
                return bar;

            if (IsolatedBarInstances.TryGetValue(t, out IBigProgressBar inst) && inst != null)
                return inst;

            try
            {
                inst = (IBigProgressBar)Activator.CreateInstance(t);
                IsolatedBarInstances[t] = inst;
                return inst;
            }
            catch
            {
                return bar;
            }
        }

        /// <summary>
        /// 以指定 npc 为目标计算一次血条。模组血条按客户端环境编写，在服务器上可能读贴图、LocalPlayer 而抛异常，
        /// 异常只记录一次并视为本次采样失败。
        /// </summary>
        public static bool TrySampleBar(NPC npc, out float life, out float lifeMax)
        {
            life = 0f;
            lifeMax = 0f;

            IBigProgressBar bar = GetIsolatedInstanceIfPossible(GetCandidateBarFor(npc));
            if (bar == null)
                return false;

            BigProgressBarInfo info = default;
            info.npcIndexToAimAt = npc.whoAmI;
            info.showText = true;

            try
            {
                return TryGetBarLife(bar, ref info, out life, out lifeMax);
            }
            catch (Exception e)
            {
                life = 0f;
                lifeMax = 0f;
                if (ReportedFailures.Add(bar.GetType()))
                    GamePlayStatic.Warn($"读取血条 {bar.GetType().FullName} 失败（npc: {npc.FullName}），回退到 npc.lifeMax：{e}");
                return false;
            }
            finally
            {
                if (bar is ModBossBar)
                    ResyncDisplayedBar(bar);
            }
        }

        public static bool TryGetBarLife(IBigProgressBar bar, ref BigProgressBarInfo info, out float life, out float lifeMax)
        {
            life = 0f;
            lifeMax = 0f;

            if (bar == null)
                return false;

            if (!bar.ValidateAndCollectNecessaryInfo(ref info))
                return false;

            if (bar is ModBossBar modBar)
            {
                life = modBar.Life;
                lifeMax = modBar.LifeMax;
                return lifeMax > 0f;
            }

            Type t = bar.GetType();
            if (!CacheFieldByBarType.TryGetValue(t, out FieldInfo cacheField))
            {
                cacheField = t.GetField("_cache", InstanceFlags);
                CacheFieldByBarType[t] = cacheField;
            }

            if (cacheField == null)
                return false;

            BigProgressBarCache cache = (BigProgressBarCache)cacheField.GetValue(bar);
            life = cache.LifeCurrent;
            lifeMax = cache.LifeMax;
            return lifeMax > 0f;
        }

        /// <summary>
        /// ModBossBar 是全局单例，无法隔离。血条 UI 先于 NPC 更新，若采样改写了正在显示的实例，
        /// 按显示目标重新计算一次，避免同帧画出别的 npc 的数值。
        /// </summary>
        private static void ResyncDisplayedBar(IBigProgressBar bar)
        {
            if (Main.dedServ)
                return;

            _currentBarField ??= typeof(BigProgressBarSystem).GetField("_currentBar", InstanceFlags);
            _displayInfoField ??= typeof(BigProgressBarSystem).GetField("_info", InstanceFlags);
            if (_displayInfoField == null || !ReferenceEquals(_currentBarField?.GetValue(Main.BigBossProgressBar), bar))
                return;

            var displayInfo = (BigProgressBarInfo)_displayInfoField.GetValue(Main.BigBossProgressBar);
            try
            {
                if (bar.ValidateAndCollectNecessaryInfo(ref displayInfo))
                    _displayInfoField.SetValue(Main.BigBossProgressBar, displayInfo);
            }
            catch
            {
                // 显示端下一帧 Update 会自行重算，这里失败不影响结算。
            }
        }
    }
}
