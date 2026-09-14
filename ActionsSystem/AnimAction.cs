using Terraria.DataStructures;
using KL.Extensions;

namespace KL.ActionsSystem;

/// <summary>
/// 表示一个在启动时确定总时长、可并行执行多个节点的动作。
/// </summary>
public abstract class AnimAction
{
    /// <summary>
    /// 是否使用物品时间，如果为是，动作发生时，角色当前手持物品会被强制设置为动作时间。
    /// </summary>
    public virtual bool UseItemTime => true;
    
    private readonly List<ActionNode> nodes = new();

    /// <summary>
    /// 动作中包含的全部节点。
    /// </summary>
    public IReadOnlyList<ActionNode> Nodes => nodes;

    /// <summary>
    /// 动作每帧更新前触发的本地监听。
    /// </summary>
    public Action<ActionModPlayer, int, float> FrameUpdateListener { get; set; }

    /// <summary>
    /// 动作节点执行前触发的本地监听。返回 false 可阻止该节点在本帧执行。
    /// </summary>
    public Func<ActionModPlayer, ActionNode, int, float, bool> PreNodeUpdateListener { get; set; }

    /// <summary>
    /// 动作类型 id。
    /// </summary>
    public int TypeId => AnimActionRegistry.GetId(GetType());

    /// <summary>
    /// 动作总时长，单位为帧。
    /// </summary>
    public int TotalFrame { get; private set; }

    /// <summary>
    /// 动作开始时同步的方向角。
    /// </summary>
    public float StartRotation { get; private set; }

    /// <summary>
    /// 设置动作开始时同步的方向角。
    /// </summary>
    /// <param name="rotation">动作开始时的方向角。</param>
    internal void SetStartRotation(float rotation)
    {
        StartRotation = rotation;
    }

    /// <summary>
    /// 创建指定总时长的动作。
    /// </summary>
    /// <param name="totalFrame">动作总时长，单位为帧。</param>
    protected AnimAction(int totalFrame)
    {
        if (totalFrame <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalFrame), "动作总时长必须大于0。");
        }

        TotalFrame = totalFrame;
    }

    /// <summary>
    /// 在动作正式启动前应用网络同步的总时长。
    /// </summary>
    /// <param name="totalFrame">动作总时长，单位为帧。</param>
    internal void SetTotalFrame(int totalFrame)
    {
        if (totalFrame <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalFrame), "动作总时长必须大于0。");
        }

        TotalFrame = totalFrame;
    }

    /// <summary>
    /// 创建指定总时长的动作，并添加初始节点。
    /// </summary>
    /// <param name="totalFrame">动作总时长，单位为帧。</param>
    /// <param name="nodes">初始节点集合。</param>
    protected AnimAction(int totalFrame, IEnumerable<ActionNode> nodes) : this(totalFrame)
    {
        foreach (ActionNode node in nodes)
        {
            AddNode(node);
        }
    }
    
    /// <summary>
    /// 添加一个动作节点。
    /// </summary>
    /// <param name="node">需要添加的动作节点。</param>
    /// <returns>当前动作实例。</returns>
    public AnimAction AddNode(ActionNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.EndFrame > TotalFrame)
        {
            throw new ArgumentException("节点结束帧不能超过动作总时长。", nameof(node));
        }

        nodes.Add(node);
        return this;
    }

    /// <summary>
    /// 判断动作是否已经播放结束。
    /// </summary>
    /// <param name="elapsedFrame">动作已经播放的帧数。</param>
    /// <returns>动作是否结束。</returns>
    public bool IsFinished(int elapsedFrame)
    {
        return elapsedFrame >= TotalFrame;
    }

    /// <summary>
    /// 获取动作在指定帧的整体播放进度。
    /// </summary>
    /// <param name="actionFrame">当前动作帧。</param>
    /// <returns>动作整体播放进度。</returns>
    public float GetProgress(int actionFrame)
    {
        if (TotalFrame <= 1)
        {
            return 1f;
        }

        return MathHelper.Clamp(actionFrame / (float)(TotalFrame - 1), 0f, 1f);
    }

    /// <summary>
    /// 动作开始播放时调用。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    public virtual void OnStart(ActionModPlayer actionPlayer)
    {
    }

    /// <summary>
    /// 判断当前动作帧是否是由动作自身定义的自动保持点。
    /// 此方法会在所有端调用，返回值必须只依赖已同步的确定性状态，不能读取本地输入。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    /// <param name="actionFrame">刚刚执行完毕的动作帧。</param>
    /// <returns>是否应在当前帧进入保持状态。</returns>
    public virtual bool IsAutomaticHoldPoint(ActionModPlayer actionPlayer, int actionFrame)
    {
        return false;
    }

    /// <summary>
    /// 由动作拥有者判断当前保持状态是否应继续。
    /// 可以在此读取拥有者的本地输入；其他端等待拥有者发来的保持状态 RPC。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    /// <param name="actionFrame">当前保持的动作帧。</param>
    /// <returns>是否继续保持。</returns>
    public virtual bool ShouldContinueHolding(ActionModPlayer actionPlayer, int actionFrame)
    {
        return true;
    }

    /// <summary>
    /// 进入动作保持状态时调用。
    /// </summary>
    public virtual void OnHoldEnter(ActionModPlayer actionPlayer, int actionFrame)
    {
    }

    /// <summary>
    /// 动作处于保持状态时每个游戏帧调用。普通时间轴节点不会在保持期间重复执行。
    /// </summary>
    public virtual void OnHoldUpdate(ActionModPlayer actionPlayer, int actionFrame, int holdTime)
    {
    }

    /// <summary>
    /// 离开动作保持状态时调用。解除后时间轴从保持帧的下一帧继续。
    /// </summary>
    public virtual void OnHoldExit(ActionModPlayer actionPlayer, int actionFrame)
    {
    }

    /// <summary>
    /// 更新当前帧内所有激活节点的逻辑。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    /// <param name="actionFrame">当前动作帧。</param>
    /// <param name="actionProgress">动作整体播放进度。</param>
    public virtual void Update(ActionModPlayer actionPlayer, int actionFrame, float actionProgress)
    {
        try
        {
            FrameUpdateListener?.Invoke(actionPlayer, actionFrame, actionProgress);
        }
        catch (Exception ex)
        {
            // 捕获委托调用中的异常，防止整个动作系统崩溃
            // 记录异常但继续执行
            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText($"[AnimAction] FrameUpdateListener 异常: {ex.GetType().Name}", Color.Orange);
            }
            // 清除有问题的监听器，防止后续帧继续崩溃
            FrameUpdateListener = null;
        }

        foreach (ActionNode node in nodes)
        {
            if (!node.IsActive(actionFrame))
            {
                continue;
            }

            float nodeProgress = node.GetProgress(actionFrame);
            if (PreNodeUpdateListener?.Invoke(actionPlayer, node, actionFrame, nodeProgress) == false)
            {
                continue;
            }

            node.Update(actionPlayer, actionFrame, nodeProgress);
        }
    }

    /// <summary>
    /// 应用当前帧内所有激活节点的表现效果。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    /// <param name="actionFrame">当前动作帧。</param>
    /// <param name="actionProgress">动作整体播放进度。</param>
    public virtual void ApplyFrameEffects(ActionModPlayer actionPlayer, int actionFrame, float actionProgress)
    {
        foreach (ActionNode node in nodes)
        {
            if (!node.IsActive(actionFrame))
            {
                continue;
            }

            node.ApplyFrameEffects(actionPlayer, actionFrame, node.GetProgress(actionFrame));
        }
    }

    /// <summary>
    /// 绘制当前帧内所有激活节点的表现效果。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    /// <param name="drawInfo">玩家绘制信息。</param>
    /// <param name="actionFrame">当前动作帧。</param>
    /// <param name="actionProgress">动作整体播放进度。</param>
    /// <param name="drawLayerType">当前动作绘制层位置。</param>
    public virtual void Draw(ActionModPlayer actionPlayer, ref PlayerDrawSet drawInfo, int actionFrame, float actionProgress, ActionDrawLayerType drawLayerType)
    {
        foreach (ActionNode node in nodes)
        {
            if (!node.IsActive(actionFrame))
            {
                continue;
            }

            node.Draw(actionPlayer, ref drawInfo, actionFrame, node.GetProgress(actionFrame), drawLayerType);
        }
    }

    /// <summary>
    /// 动作自然结束时调用。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    public virtual void OnFinish(ActionModPlayer actionPlayer)
    {
    }

    /// <summary>
    /// 动作被中断时调用。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    public virtual void OnInterrupt(ActionModPlayer actionPlayer)
    {
    }
    
    public static void ShootFromAction(
        Player player,
        int projToShoot,
        Vector2 position,
        Vector2 velocity,
        int damage,
        float knockback)
    {
        ShootFromAction(player, projToShoot, position, velocity, damage, knockback, null);
    }

    public static void ShootFromAction(
        Player player,
        int projToShoot,
        Vector2 position,
        Vector2 velocity,
        int damage,
        float knockback,
        Action<Projectile> configureProjectile)
    {
        // 动作可能跨帧执行；执行时玩家或手持物品状态可能已经失效。
        if (player == null || !player.active || player.dead)
        {
            return;
        }

        if (player.whoAmI != Main.myPlayer)
        {
            return;
        }

        Item item = player.HeldItem;

        if (item == null || item.IsAir)
        {
            return;
        }

        if (!CombinedHooks.CanShoot(player, item))
        {
            return;
        }

        if (projToShoot <= ProjectileID.None)
        {
            return;
        }
        
        //int damage = player.GetWeaponDamage(item);
        //float knockback = player.GetWeaponKnockback(item, item.knockBack);
        
        EntitySource_ItemUse_WithAmmo source = new(player, item, 0);

        //Vector2 position = player.RotatedRelativePoint(player.MountedCenter);


        if (item.ChangePlayerDirectionOnShoot)
        {
            if (velocity.X > 0f)
            {
                player.ChangeDir(1);
            }
            else if (velocity.X < 0f)
            {
                player.ChangeDir(-1);
            }
        }

        CombinedHooks.ModifyShootStats(player, item, ref position, ref velocity, ref projToShoot, ref damage, ref knockback);

        // Mod hooks may replace the projectile type; retain the same lower-bound guard after mutation.
        if (projToShoot <= ProjectileID.None)
        {
            return;
        }

        if (!CombinedHooks.Shoot(player, item, source, position, velocity, projToShoot, damage, knockback))
        {
            return;
        }

        GamePlayStatic.NewProjectile(
            source,
            position,
            velocity,
            projToShoot,
            damage,
            knockback,
            player.whoAmI,
            configureProjectile: configureProjectile);
    }

}
