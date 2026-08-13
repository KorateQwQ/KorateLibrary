using System;
using System.Collections.Generic;
using System.Reflection;
using KL.DamageSystem;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;

namespace KL.Utils;

public class NpcHPGlobalNpc : GlobalNPC
{
    public override bool InstancePerEntity => true;

    public int RealMaxHP;
    public int RealHP;

    public override void AI(NPC npc)
    {
        base.AI(npc);
    }
}

public static class NpcHPHelper
{
    internal static int GetRealMaxHPInternal(NPC npc)
    {
        if (npc == null)
            return 0;
        
        var g = npc.GetGlobalNPC<NpcHPGlobalNpc>();
        if (g.RealMaxHP > 0)
        {
            //PrintText($"GetRealMaxHP From g.RealMaxHP {npc.whoAmI}: {g.RealMaxHP}");
            return g.RealMaxHP;
        }

        BindRealHpIfNeeded(npc, g);
        return g.RealMaxHP > 0 ? g.RealMaxHP : npc.lifeMax;
    }
    

    private static void BindRealHpIfNeeded(NPC npc, NpcHPGlobalNpc g)
    {
        if (npc == null || !npc.active)
            return;

        if (TryBindRealHpFromBossBar(npc, g))
            return;

        g.RealMaxHP = npc.lifeMax;
        g.RealHP = npc.life;
    }

    private static bool TryBindRealHpFromBossBar(NPC npc, NpcHPGlobalNpc g)
    {
        IBigProgressBar bar = BossBarQuery.GetCandidateBarFor(npc);
        if (bar == null)
            return false;

        bar = BossBarQuery.GetIsolatedInstanceIfPossible(bar);

        BigProgressBarInfo info = default;
        info.npcIndexToAimAt = npc.whoAmI;
        info.showText = true;

        if (!BossBarQuery.TryGetBarLife(bar, ref info, out float life, out float lifeMax))
            return false;

        if (lifeMax <= 0f)
            return false;

        g.RealMaxHP = (int)MathF.Round(lifeMax);
        g.RealHP = (int)MathF.Round(life);
        return true;
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

        private static FieldInfo _barsByNetIdField;
        private static Dictionary<int, IBigProgressBar> _barsByNetIdCached;

        private static readonly Dictionary<Type, FieldInfo> CacheFieldByBarType = new Dictionary<Type, FieldInfo>();
        private static readonly Dictionary<Type, IBigProgressBar> IsolatedBarInstances = new Dictionary<Type, IBigProgressBar>();

        private static CommonBossBigProgressBar _commonBossBar;

        private static Dictionary<int, IBigProgressBar> GetBarsByNetId()
        {
            if (_barsByNetIdCached != null)
                return _barsByNetIdCached;

            _barsByNetIdField ??= typeof(BigProgressBarSystem).GetField("_bossBarsByNpcNetId", InstanceFlags);
            _barsByNetIdCached = (Dictionary<int, IBigProgressBar>)_barsByNetIdField?.GetValue(Main.BigBossProgressBar);
            return _barsByNetIdCached;
        }

        public static IBigProgressBar GetCandidateBarFor(NPC npc)
        {
            if (npc == null)
                return null;

            if (npc.BossBar != null)
                return npc.BossBar;

            Dictionary<int, IBigProgressBar> map = GetBarsByNetId();
            if (map != null && map.TryGetValue(npc.netID, out IBigProgressBar mapped))
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
    }
}