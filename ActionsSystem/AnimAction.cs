using Terraria.DataStructures;

namespace KL.ActionsSystem;

/// <summary>
/// 表示一个拥有固定总时长、可并行执行多个节点的动作。
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
    /// 动作类型 id。
    /// </summary>
    public int TypeId => AnimActionRegistry.GetId(GetType());

    /// <summary>
    /// 动作总时长，单位为帧。
    /// </summary>
    public int TotalFrame { get; }

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
    /// 更新当前帧内所有激活节点的逻辑。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    /// <param name="actionFrame">当前动作帧。</param>
    /// <param name="actionProgress">动作整体播放进度。</param>
    public virtual void Update(ActionModPlayer actionPlayer, int actionFrame, float actionProgress)
    {
        foreach (ActionNode node in nodes)
        {
            if (!node.IsActive(actionFrame))
            {
                continue;
            }

            node.Update(actionPlayer, actionFrame, node.GetProgress(actionFrame));
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
    
    public static void ShootFromAction(Player player,int projToShoot,Vector2 position,Vector2 velocity,int damage,float knockback)
    {
        Item item = player.HeldItem;

        if (player.whoAmI != Main.myPlayer)
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

        if (!CombinedHooks.Shoot(player, item, source, position, velocity, projToShoot, damage, knockback))
        {
            return;
        }

        Projectile.NewProjectile(source, position, velocity, projToShoot, damage, knockback, player.whoAmI);
    }

}