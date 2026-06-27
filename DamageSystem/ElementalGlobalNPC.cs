using System.Collections.Generic;
using System.IO;
using KL.Buffs.ElementalDebuff;
using KL.Configs;
using KL.Drawing;
using KL.Dusts;
using KL.Dusts.Fire;
using KL.Dusts.Lightning;
using KL.Utils;
using KL.Utils.Net;
using Terraria.ModLoader.IO;

namespace KL.DamageSystem;

/// <summary>
/// 元素异常各种相关的异常积蓄，元素易伤，增伤等效果在此判定。异常条绘制也在此绘制。
/// </summary>
public class ElementalGlobalNPC : KLGlobalNpc
{
    public override bool InstancePerEntity => true;

    /// <summary>
    /// 当前实体的蠕虫血条位置缓存。
    /// </summary>
    private Vector2 wormHealthBarAnchorCache;

    private bool hasWormHealthBarAnchorCache;
    
    // Vulnerable 易伤，所有元素异常造成的易伤效果都在此累计。
    public float Vuln = 0f;

    // 积蓄结算伤害，在任意异常触发前，积攒此异常的伤害总和。
    private Dictionary<ElementType, float> damageAccumulation = new Dictionary<ElementType, float>();

    public bool GetAffliction(ElementType type)
    {
        return ElementAccumulation != null
               && ElementAccumulation.TryGetValue(type, out BuildUpProgressContext ctx)
               && ctx != null
               && ctx.InAffliction;
    }

    // 设置是否进入或退出异常状态
    public void SetAffliction(ElementType type, bool value)
    {
        if (ElementAccumulation == null)
            return;

        if (!ElementAccumulation.TryGetValue(type, out BuildUpProgressContext ctx) || ctx == null)
        {
            if (!value)
                return;

            ctx = new BuildUpProgressContext();
            ElementAccumulation[type] = ctx;
        }

        ctx.InAffliction = value;
        if (value) ctx.VisualTime = 120;
    }
    
    //设置积蓄值
    public void SetCurrentAccumulation(ElementType type, float value)
    {
        if (ElementAccumulation == null)
            ElementAccumulation = new Dictionary<ElementType, BuildUpProgressContext>();
        
        if (!ElementAccumulation.TryGetValue(type, out BuildUpProgressContext ctx) || ctx == null)
        {
            ctx = new BuildUpProgressContext();
            ElementAccumulation[type] = ctx;
        }
        
        ctx.Current = value;
    }
    
    //获取当前积蓄值
    public float? GetCurrentAccumulation(ElementType type)
    {
        if (ElementAccumulation == null)
            return null;
        
        if (!ElementAccumulation.TryGetValue(type, out BuildUpProgressContext ctx) || ctx == null)
            return null;
        
        return ctx.Current;
    }
    
    //获取最大积蓄值
    public float? GetMaxAccumulation(ElementType type)
    {
        if (ElementAccumulation == null)
            return null;
        
        if (!ElementAccumulation.TryGetValue(type, out BuildUpProgressContext ctx) || ctx == null)
            return null;
        
        return ctx.Max;
    }

    //设置异常持续时间
    public void SetDuration(ElementType type, int value)
    {
        if (ElementAccumulation == null)
            return;
        
        if (!ElementAccumulation.TryGetValue(type, out BuildUpProgressContext ctx) || ctx == null)
            return;
        
        ctx.Duration = value;
    }
    
    //获取最大异常持续时间
    public float? GetMaxDuration(ElementType type)
    {
        if (ElementAccumulation == null)
            return null;
        
        if (!ElementAccumulation.TryGetValue(type, out BuildUpProgressContext ctx) || ctx == null)
            return null;
        
        return ctx.MaxDuration;
    }
    
    // 对于另一个npc的主体逻辑npc，如果其部件被冻结，其逻辑npc也会被视为冻结
    public bool IsLogicFrozen;
    
    private Rectangle frozenFrame;
    private double frozenFrameCounter;
    private static Texture2D frozenTex;
    
    Dictionary<int,int> dustCount = new Dictionary<int,int>();

    bool ShouldSpawnDust(int dustType,/*间隔时间*/ int interval = 10)
    {
        if (!dustCount.ContainsKey(dustType))
        {
            dustCount[dustType] = 0;
            return true;
        }
        if (dustCount[dustType]++ >=interval) return true;
        return true;
    }
    
    public sealed class BuildUpProgressContext
    {
        private float current;
        private float max;
        // 附着可视时间，归零一定时间后不再显示异常条
        private float visualTime;
        //处于元素异常中   
        private bool inAffliction;
        
        private int updateCounter;

        private int duration = 1;
        
        private int maxDuration = 1;
        
        public float Current
        {
            get => current;
            set => current = value < 0f ? 0f : value;
        }

        public float Max
        {
            get => max;
            set => max = value < 1f ? 1f : value;
        }

        public float VisualTime
        {
            get => visualTime;
            set => visualTime = value < 0f ? 0f : value;
        }

        public bool InAffliction
        {
            get => inAffliction;
            set => inAffliction = value;
        }

        public int UpdateCounter
        {
            get => updateCounter;
            set => updateCounter = value;
        }

        public int Duration
        {
            get => duration;
            set => duration = value < 0 ? 0 : value;
        }
        public int MaxDuration
        {
            get => maxDuration;
            set => maxDuration = value < 1 ? 1 : value;
        }
    }

    // 异常积蓄槽，元素异常的积蓄值在此累计。
    public Dictionary<ElementType, BuildUpProgressContext> ElementAccumulation = new Dictionary<ElementType, BuildUpProgressContext>();

    // 同步异常积蓄槽
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        int validCount = 0;
        if (ElementAccumulation != null)
        {
            foreach (var accumulationPair in ElementAccumulation)
            {
                if (accumulationPair.Value != null)
                    validCount++;
            }
        }

        binaryWriter.Write(validCount);

        if (ElementAccumulation != null)
        {
            foreach (var accumulationPair in ElementAccumulation)
            {
                BuildUpProgressContext context = accumulationPair.Value;
                if (context == null)
                    continue;

                binaryWriter.Write((byte)accumulationPair.Key);
                binaryWriter.Write(context.Current);
                binaryWriter.Write(context.Max);
                //binaryWriter.Write(context.VisualTime);
                binaryWriter.Write(context.InAffliction);
                binaryWriter.Write(context.UpdateCounter);
                binaryWriter.Write(context.Duration);
                binaryWriter.Write(context.MaxDuration);
            }
        }

        base.SendExtraAI(npc, bitWriter, binaryWriter);
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        Dictionary<ElementType, BuildUpProgressContext> oldElementAccumulation = ElementAccumulation;
        ElementAccumulation = new Dictionary<ElementType, BuildUpProgressContext>();

        int count = binaryReader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            ElementType type = (ElementType)binaryReader.ReadByte();
            float visualTime = 0f;
            if (oldElementAccumulation != null
                && oldElementAccumulation.TryGetValue(type, out BuildUpProgressContext oldContext)
                && oldContext != null)
            {
                visualTime = oldContext.VisualTime;
            }
            BuildUpProgressContext context = new BuildUpProgressContext
            {
                Current = binaryReader.ReadSingle(),
                Max = binaryReader.ReadSingle(),
                VisualTime = visualTime,
                InAffliction = binaryReader.ReadBoolean(),
                UpdateCounter = binaryReader.ReadInt32(),
                Duration = binaryReader.ReadInt32(),
                MaxDuration = binaryReader.ReadInt32(),
            };

            ElementAccumulation[type] = context;
        }

        base.ReceiveExtraAI(npc, bitReader, binaryReader);
    }

    public override void Load()
    {
        frozenTex ??= ModContent.Request<Texture2D>("KL/Effects/Tex/FrozenTexture",AssetRequestMode.ImmediateLoad).Value;
        base.Load();
    }
    

    public override void ResetEffects(NPC npc)
    {
        //IsLogicFrozen = false;
        // 重置所有属性
        if (ElementAccumulation != null)
        {
            foreach (var acc in ElementAccumulation)
            {
                if (acc.Value != null)
                {
                    if (acc.Value.InAffliction)
                    {
                        acc.Value.UpdateCounter++;
                    }
                    else
                    {
                        acc.Value.UpdateCounter = 0;
                    }
                    acc.Value.InAffliction = false;
                }
            }
        }
        base.ResetEffects(npc);
    }

    public override void HitEffect(NPC npc, NPC.HitInfo hit)
    {
        if (npc.life<=0&& npc.HasBuff<ElementalAffliction_Freeze>())
        {
            npc.DelBuff(npc.FindBuffIndex(ModContent.BuffType<ElementalAffliction_Freeze>()));
        }        
        base.HitEffect(npc, hit);
    }
    

    public override bool CheckDead(NPC npc)
    {
        return base.CheckDead(npc);
    }

    public override bool PreAI(NPC npc)
    {
        npc.buffImmune[ModContent.BuffType<ElementalAffliction_Fire>()] = false;
        if (GetAffliction(ElementType.Ice)||IsLogicFrozen)
        {
            npc.position -= npc.velocity;
            return false;
        }
        else
        {
            frozenFrame = npc.frame;
        }
        return base.PreAI(npc);
    }
    
    public override void AI(NPC npc)
    {
        //if(GetCurrentAccumulation(ElementType.Fire)>0)PrintText("GetCurrentAccumulation(ElementType.Fire) " + GetCurrentAccumulation(ElementType.Fire));
        //if (FireAffliction) ElementAccumulation[ElementType.Fire].InAffliction = true;
        //if (FreezeAffliction) ElementAccumulation[ElementType.Ice].InAffliction = true;
        //FireDustEffect(npc);
        //LightningDustEffect(npc);
        
        //KLBasicDust.SpawnDust(npc.Center+new Vector2(Main.rand.NextFloat(-npc.width/2f,npc.width/2f),Main.rand.NextFloat(-npc.height/2f,npc.height/2f)),
            //ModContent.DustType<LightningDust3>(),new Vector2(Main.rand.NextFloat(-10.5f,10.5f),-10),30,new Color(150,220,255,255)*0.7f,new Vector2(0.8f));
        base.AI(npc);
    }

    public override void PostAI(NPC npc)
    {
        IsLogicFrozen = false;
        if (Main.netMode != NetmodeID.Server)
        {
            foreach (var accPair in ElementAccumulation)
            {
                if(accPair.Value.VisualTime > 0&& accPair.Value.Current<=0) accPair.Value.VisualTime--;
                SetDuration(accPair.Key,accPair.Value.Duration-1);
                //SetAffliction(accPair.Key,false);
            }
        }
        
        base.PostAI(npc);
    }

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {

        //if(npc.GetRealNpc() is { active: true} && npc.GetRealNpc().GetGlobalNPC<ElementalGlobalNPC>().FreezeAffliction) FreezeAffliction = true;
        if (GetAffliction(ElementType.Ice))
        {
            npc.frame = frozenFrame;
            npc.frameCounter = frozenFrameCounter;
            //PrintText(npc.whoAmI+" "+npc.FullName+" "+npc.lifeMax);
            
            EndBeginDraw(0,1);
            Rectangle frame = npc.frame;
            Texture2D tex = TextureAssets.Npc[npc.type].Value; 
            
            Vector2 frostScale = Vector2.One/(frozenTex.Size()/npc.frame.Size());
            
            Vector2 uFrameUVMin = new Vector2(
                frame.X / (float)tex.Width,
                frame.Y / (float)tex.Height
            );

            Vector2 uFrameUVMax = new Vector2(
                (frame.X + frame.Width) / (float)tex.Width,
                (frame.Y + frame.Height) / (float)tex.Height
            );
            
            frozen.SetTexture(1,frozenTex);
            frozen.SetValue("uFrameUVMin",uFrameUVMin);
            frozen.SetValue("uFrameUVMax",uFrameUVMax);
            frozen.SetValue("uFrostScale",frostScale);
            frozen.SetValue("uFrostOffset",new Vector2(0));
            frozen.SetValue("uFrostStrength",0.6f);
            frozen.Apply();

        }
        else
        {
            if (IsLogicFrozen)
            {
                npc.frame = frozenFrame;
                npc.frameCounter = frozenFrameCounter;
            }
            else
            {
                frozenFrame = npc.frame;
                frozenFrameCounter = npc.frameCounter;
            }
        }
        return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
    }

    public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (GetAffliction(ElementType.Ice))
        {
            EndBeginDraw();
        }
        
        base.PostDraw(npc, spriteBatch, screenPos, drawColor);
    }

    public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
    {
        //OnHitByElement(npc, hit);
        base.OnHitByItem(npc, player, item, hit, damageDone);
    }

    public override bool PreKill(NPC npc)
    {
        return base.PreKill(npc);
    }

    public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        base.OnHitByProjectile(npc, projectile, hit, damageDone);
    }

    // 处理元素命中并造造异常
    public void OnHitByElement(ElementType element, NPC npc, NPC.HitInfo hit)
    {
        NPC realNpc = npc.GetRealNpc();
        ElementBuildupContext context = new ElementBuildupContext(realNpc, element, hit);
        ElementBuildupBuffRegistry.TryApply(context);
    }

    /// <summary>
    ///  应用异常dot伤害
    /// </summary>
    /// <param name="npc"></param>
    /// <param name="damage"></param>
    /// <param name="element"></param>
    public void ApplyAfflictionDamage(NPC target, int damage, ElementType element)
    {
        if (target == null || !target.active || damage <= 0 || target.realLife>=0)
            return ;
        
        if (!target.active)
            return ;

        if (target.life <= 0 || target.dontTakeDamage)
            return ;
        RPC("ApplyAfflictionDamageInternal", target, [target,damage,(byte)element], KLNetModule.NetSendType.ServerToAll);
    }

    public void ApplyAfflictionDamageInternal(NPC target, int damage, byte element)
    {
        if (target == null || !target.active || damage <= 0)
            return ;
        
        if (!target.active)
            return ;

        if (target.life <= 0 || target.dontTakeDamage)
            return ;

        target.life -= damage;
        if (target.life < 0)
            target.life = 0;
        

        target.HitEffect();
        if (target.life <= 0)
        { 
            target.checkDead();
        }
        
        if(Main.netMode == NetmodeID.Server)
        {
            if (target.life <= 0)
            { 
                target.netUpdate = true;
            }
        }
        else
        {
            Color elementColor = ElementTypeHelper.GetElementColor((ElementType)element);
            CombatText.NewText(new Rectangle((int)target.position.X, (int)target.position.Y, target.width, target.height), elementColor, damage,false,true);
        }
    }

    #region 异常条绘制
    // 绘制异常条
    public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
    {
        // 毁灭者血条位置由原版在 Main.DrawInterface_14_EntityHealthBars 内硬编码合并计算（Main.destroyerHB），这里每个体节都会触发 DrawHealthBar，跳过以避免重复/错位绘制。
        /*if (npc.type >= NPCID.TheDestroyer && npc.type <= NPCID.TheDestroyerTail)
            return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);*/

        if(ElementAccumulation is null || ElementAccumulation.Count == 0|| !IsBeginDrawCalled())return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);
        
        if (npc.GetRealNpc() is {} realNpc && realNpc.whoAmI!=npc.whoAmI) return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);
        
        Vector2 anchor = position;

        // 多体节蠕虫：只让主体绘制一次，并让位置按毁灭者风格跟随离玩家最近且可见的体节。
        if (npc.GetGlobalNPC<MainParentGlobalNpc>().IsWormMainParent)
        {
            if (TryGetWormHealthBarAnchor(npc, out Vector2 wormAnchor))
                anchor = wormAnchor;
        }

        EndBeginDraw(0, 1);
        Vector2 offset = new Vector2(0, 17 + 12.0f * scale);
        Vector2 barScale = new Vector2(2.0f);
        Vector2 iconScale = new Vector2(1);
        float xOffset = 35;

        //Todo: 改为反向绘制，避免最先出现的异常改变位置
        foreach (var accumulationPair in ElementAccumulation)
        {
            if(accumulationPair.Value.VisualTime<=0)continue;
            float alpha = KLMathF.ClampLerp(0, 1, accumulationPair.Value.VisualTime / 60f);
            //accumulationPair.Value.InAffliction
            Asset<Texture2D> icon = ElementTypeHelper.GetElementIconTexture(ElementType.Fire);
            //rintText(accumulationPair.Value.Current);
            Vector4 progressColor = ElementTypeHelper.GetElementColor(accumulationPair.Key).ToVector4();
            float progress = accumulationPair.Value.Current/(float)accumulationPair.Value.Max;

            if (progress <= 0.001f&&!accumulationPair.Value.InAffliction)
            {
                progressColor = new Vector4(0.20f, 0.20f, 0.20f, 0.80f);
            }
            
            if (accumulationPair.Value.InAffliction)
            {
                progress = accumulationPair.Value.Duration/(float)accumulationPair.Value.MaxDuration;

                
                ProgressCircleEffect(1, ringWidth: 0.08f, progressColor: progressColor,bloomStrength:0.5f);
                int count = accumulationPair.Value.UpdateCounter;
                //根据count持续的做从3到1的渐变
                float extraScale = KLMathF.ClampLerp(1,1.8f, count%30/30f);
                float extraAlpha = KLMathF.ClampLerp(0, 1, 1-count%30/30f);
                //PrintText(extraScale);
                DrawInWorld(icon.Value, anchor+offset, Color.White*alpha*extraAlpha, barScale*extraScale);

            }
            ProgressCircleEffect(progress, ringWidth: 0.08f, progressColor: progressColor);
            DrawInWorld(icon.Value, anchor+offset, Color.White*alpha, barScale);
            
            offset.X += xOffset;
        }
        EndBeginDraw();

        offset = new Vector2(0, 17 + 12.0f * scale);
        foreach (var accumulationPair in ElementAccumulation)
        {
            if(accumulationPair.Value.VisualTime<=0)continue;
            float alpha = KLMathF.ClampLerp(0, 1, accumulationPair.Value.VisualTime / 60f);

            Asset<Texture2D> icon = ElementTypeHelper.GetElementIconTexture(accumulationPair.Key);
            DrawInWorld(icon.Value, anchor+offset, Color.White*alpha, iconScale);
            offset.X += xOffset;
        }

        return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);
    }
    

    private bool TryGetWormHealthBarAnchor(NPC root, out Vector2 anchor)
    {
        anchor = default;

        if (root == null || !root.active)
        {
            hasWormHealthBarAnchorCache = false;
            wormHealthBarAnchorCache = default;
            return false;
        }

        int rootId = root.whoAmI;
        if (rootId < 0 || rootId >= Main.npc.Length)
        {
            hasWormHealthBarAnchorCache = false;
            wormHealthBarAnchorCache = default;
            return false;
        }

        Player p = Main.player[Main.myPlayer];
        float bestDist = 999999f;
        int bestIndex = -1;

        foreach (var n in Main.ActiveNPCs)
        {
            if (!n.active)
                continue;

            if (n.whoAmI != rootId && n.realLife != rootId)
                continue;

            Vector2 diff = p.Center - n.Center;
            float dist = diff.Length();
            if (dist >= bestDist)
                continue;

            if (!Collision.CanHit(p.Center, 1, 1, n.Center, 1, 1))
                continue;

            bestDist = dist;
            bestIndex = n.whoAmI;
        }

        if (bestIndex < 0 || bestDist >= Main.screenWidth)
        {
            hasWormHealthBarAnchorCache = false;
            wormHealthBarAnchorCache = default;
            return false;
        }

        NPC segment = Main.npc[bestIndex];
        Vector2 targetAnchorBase = segment.position;
        if (!hasWormHealthBarAnchorCache)
        {
            wormHealthBarAnchorCache = targetAnchorBase;
            hasWormHealthBarAnchorCache = true;
        }
        else
        {
            wormHealthBarAnchorCache = (wormHealthBarAnchorCache * 49f + targetAnchorBase) / 50f;
        }

        anchor = wormHealthBarAnchorCache + new Vector2(segment.width / 2f, segment.height / 2f);
        return true;
    }
    
    #endregion
}