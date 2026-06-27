using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KL.ActionsSystem;

/// <summary>
/// 动作绘制层位置。
/// </summary>
public enum ActionDrawLayerType
{
    /// <summary>
    /// 整个玩家下面。
    /// </summary>
    UnderPlayer,

    /// <summary>
    /// 前手臂下面。
    /// </summary>
    UnderArm,

    /// <summary>
    /// 手臂和武器上面。
    /// </summary>
    OverArm,

    /// <summary>
    /// 整个玩家上面。
    /// </summary>
    OverPlayer
}

/// <summary>
/// 将当前动作的绘制效果接入玩家绘制层。
/// </summary>
public abstract class ActionPlayerDrawLayer : PlayerDrawLayer
{
    protected abstract ActionDrawLayerType DrawLayerType { get; }

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        return drawInfo.drawPlayer.GetModPlayer<ActionModPlayer>().HasAction;
    }

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        ActionModPlayer actionPlayer = drawInfo.drawPlayer.GetModPlayer<ActionModPlayer>();
        AnimAction animAction = actionPlayer.CurrentAnimAction;
        animAction?.Draw(actionPlayer, ref drawInfo, actionPlayer.CurrentActionFrame, animAction.GetProgress(actionPlayer.CurrentActionFrame), DrawLayerType);
    }
}

/// <summary>
/// 绘制在整个玩家下面的动作效果。
/// </summary>
public class ActionUnderPlayerDrawLayer : ActionPlayerDrawLayer
{
    protected override ActionDrawLayerType DrawLayerType => ActionDrawLayerType.UnderPlayer;

    public override Position GetDefaultPosition()
    {
        return new BeforeParent(PlayerDrawLayers.FirstVanillaLayer);
    }
}

/// <summary>
/// 绘制在前手臂下面的动作效果。
/// </summary>
public class ActionUnderArmDrawLayer : ActionPlayerDrawLayer
{
    protected override ActionDrawLayerType DrawLayerType => ActionDrawLayerType.UnderArm;

    public override Position GetDefaultPosition()
    {
        return new BeforeParent(PlayerDrawLayers.ArmOverItem);
    }
}

/// <summary>
/// 绘制在手臂和武器上面的动作效果。
/// </summary>
public class ActionOverArmDrawLayer : ActionPlayerDrawLayer
{
    protected override ActionDrawLayerType DrawLayerType => ActionDrawLayerType.OverArm;

    public override Position GetDefaultPosition()
    {
        return new AfterParent(PlayerDrawLayers.ProjectileOverArm);
    }
}

/// <summary>
/// 绘制在整个玩家上面的动作效果。
/// </summary>
public class ActionOverPlayerDrawLayer : ActionPlayerDrawLayer
{
    protected override ActionDrawLayerType DrawLayerType => ActionDrawLayerType.OverPlayer;

    public override Position GetDefaultPosition()
    {
        return new AfterParent(PlayerDrawLayers.LastVanillaLayer);
    }
}