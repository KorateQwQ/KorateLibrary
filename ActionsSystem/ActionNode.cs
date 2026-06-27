using Terraria;
using Terraria.DataStructures;

namespace KL.ActionsSystem;

/// <summary>
/// 复合手臂的控制目标。
/// </summary>
public enum ActionArmType
{
    /// <summary>
    /// 前手。
    /// </summary>
    Front,

    /// <summary>
    /// 后手。
    /// </summary>
    Back
}

/// <summary>
/// 动作节点基类，表示动作时间轴上的一个生效区间。
/// </summary>
public class ActionNode
{
    /// <summary>
    /// 节点开始生效的动作帧。
    /// </summary>
    public int StartFrame { get; }

    /// <summary>
    /// 节点结束生效的动作帧。
    /// </summary>
    public int EndFrame { get; }

    /// <summary>
    /// 节点持续时长，单位为帧。
    /// </summary>
    public int DurationFrame => EndFrame - StartFrame;

    /// <summary>
    /// 创建指定生效区间的动作节点。
    /// </summary>
    /// <param name="startFrame">节点开始生效的动作帧。</param>
    /// <param name="endFrame">节点结束生效的动作帧。</param>
    public ActionNode(int startFrame, int endFrame)
    {
        if (startFrame < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startFrame), "节点起始帧不能小于0。");
        }

        if (endFrame <= startFrame)
        {
            throw new ArgumentOutOfRangeException(nameof(endFrame), "节点结束帧必须大于起始帧。");
        }

        StartFrame = startFrame;
        EndFrame = endFrame;
    }

    /// <summary>
    /// 判断节点在指定动作帧是否生效。
    /// </summary>
    /// <param name="actionFrame">当前动作帧。</param>
    /// <returns>节点是否生效。</returns>
    public bool IsActive(int actionFrame)
    {
        return actionFrame >= StartFrame && actionFrame < EndFrame;
    }

    /// <summary>
    /// 获取节点在指定动作帧的播放进度。
    /// </summary>
    /// <param name="actionFrame">当前动作帧。</param>
    /// <returns>节点播放进度。</returns>
    public float GetProgress(int actionFrame)
    {
        if (DurationFrame <= 1)
        {
            return 1f;
        }

        return Clamp01((actionFrame - StartFrame) / (float)(DurationFrame - 1));
    }

    /// <summary>
    /// 更新节点逻辑。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    /// <param name="actionFrame">当前动作帧。</param>
    /// <param name="nodeProgress">节点播放进度。</param>
    public virtual void Update(ActionModPlayer actionPlayer, int actionFrame, float nodeProgress)
    {
    }

    /// <summary>
    /// 应用节点的帧表现效果。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    /// <param name="actionFrame">当前动作帧。</param>
    /// <param name="nodeProgress">节点播放进度。</param>
    public virtual void ApplyFrameEffects(ActionModPlayer actionPlayer, int actionFrame, float nodeProgress)
    {
    }

    /// <summary>
    /// 绘制节点的表现效果。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    /// <param name="drawInfo">玩家绘制信息。</param>
    /// <param name="actionFrame">当前动作帧。</param>
    /// <param name="nodeProgress">节点播放进度。</param>
    /// <param name="drawLayerType">当前动作绘制层位置。</param>
    public virtual void Draw(ActionModPlayer actionPlayer, ref PlayerDrawSet drawInfo, int actionFrame, float nodeProgress, ActionDrawLayerType drawLayerType)
    {
    }

    /// <summary>
    /// 线性插值两个浮点值。
    /// </summary>
    /// <param name="start">起始值。</param>
    /// <param name="end">结束值。</param>
    /// <param name="progress">插值进度。</param>
    /// <returns>插值结果。</returns>
    protected static float Lerp(float start, float end, float progress)
    {
        progress = Clamp01(progress);
        return start + (end - start) * progress;
    }

    /// <summary>
    /// 将浮点值限制在0到1之间。
    /// </summary>
    /// <param name="value">需要限制的浮点值。</param>
    /// <returns>限制后的浮点值。</returns>
    protected static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        if (value > 1f)
        {
            return 1f;
        }

        return value;
    }
}

/// <summary>
/// 控制玩家复合手臂姿态的动作节点。
/// </summary>
public class ArmActionNode : ActionNode
{
    /// <summary>
    /// 需要控制的手臂类型。
    /// </summary>
    public ActionArmType ArmType { get; }

    /// <summary>
    /// 节点开始时的手臂伸展长度。
    /// </summary>
    public Player.CompositeArmStretchAmount StartStretch { get; }

    /// <summary>
    /// 节点结束时的手臂伸展长度。
    /// </summary>
    public Player.CompositeArmStretchAmount EndStretch { get; }

    /// <summary>
    /// 节点开始时的手臂旋转角度。
    /// </summary>
    public float StartRotation { get; }

    /// <summary>
    /// 节点结束时的手臂旋转角度。
    /// </summary>
    public float EndRotation { get; }

    /// <summary>
    /// 创建控制复合手臂姿态的动作节点。
    /// </summary>
    /// <param name="startFrame">节点开始生效的动作帧。</param>
    /// <param name="endFrame">节点结束生效的动作帧。</param>
    /// <param name="armType">需要控制的手臂类型。</param>
    /// <param name="startStretch">节点开始时的手臂伸展长度。</param>
    /// <param name="endStretch">节点结束时的手臂伸展长度。</param>
    /// <param name="startRotation">节点开始时的手臂旋转角度。</param>
    /// <param name="endRotation">节点结束时的手臂旋转角度。</param>
    public ArmActionNode(
        int startFrame,
        int endFrame,
        ActionArmType armType,
        Player.CompositeArmStretchAmount startStretch,
        Player.CompositeArmStretchAmount endStretch,
        float startRotation,
        float endRotation) : base(startFrame, endFrame)
    {
        ArmType = armType;
        StartStretch = startStretch;
        EndStretch = endStretch;
        StartRotation = startRotation;
        EndRotation = endRotation;
    }

    /// <summary>
    /// 更新当前帧的复合手臂姿态。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    /// <param name="actionFrame">当前动作帧。</param>
    /// <param name="nodeProgress">节点播放进度。</param>
    public override void Update(ActionModPlayer actionPlayer, int actionFrame, float nodeProgress)
    {
        Player player = actionPlayer.Player;
        Player.CompositeArmStretchAmount stretch = GetStretch(nodeProgress);
        float rotation = Lerp(StartRotation, EndRotation, nodeProgress) * player.direction;

        if (ArmType == ActionArmType.Front)
        {
            player.SetCompositeArmFront(true, stretch, rotation);
            return;
        }

        player.SetCompositeArmBack(true, stretch, rotation);
    }

    private Player.CompositeArmStretchAmount GetStretch(float progress)
    {
        float stretchValue = Lerp(GetStretchValue(StartStretch), GetStretchValue(EndStretch), progress);

        if (stretchValue <= 0.125f)
        {
            return Player.CompositeArmStretchAmount.None;
        }

        if (stretchValue <= 0.5f)
        {
            return Player.CompositeArmStretchAmount.Quarter;
        }

        if (stretchValue <= 0.875f)
        {
            return Player.CompositeArmStretchAmount.ThreeQuarters;
        }

        return Player.CompositeArmStretchAmount.Full;
    }

    private static float GetStretchValue(Player.CompositeArmStretchAmount stretch)
    {
        return stretch switch
        {
            Player.CompositeArmStretchAmount.None => 0f,
            Player.CompositeArmStretchAmount.Quarter => 0.25f,
            Player.CompositeArmStretchAmount.ThreeQuarters => 0.75f,
            _ => 1f
        };
    }
}

/// <summary>
/// 控制玩家腿部帧过渡的动作节点。
/// </summary>
public class LegFrameActionNode : ActionNode
{
    /// <summary>
    /// 节点开始时的腿部帧索引。
    /// </summary>
    public int StartLegFrameIndex { get; }

    /// <summary>
    /// 节点结束时的腿部帧索引。
    /// </summary>
    public int EndLegFrameIndex { get; }

    /// <summary>
    /// 创建控制腿部帧过渡的动作节点。
    /// </summary>
    /// <param name="startFrame">节点开始生效的动作帧。</param>
    /// <param name="endFrame">节点结束生效的动作帧。</param>
    /// <param name="startLegFrameIndex">节点开始时的腿部帧索引。</param>
    /// <param name="endLegFrameIndex">节点结束时的腿部帧索引。</param>
    public LegFrameActionNode(int startFrame, int endFrame, int startLegFrameIndex, int endLegFrameIndex) : base(startFrame, endFrame)
    {
        if (startLegFrameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startLegFrameIndex), "腿部起始帧索引不能小于0。");
        }

        if (endLegFrameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(endLegFrameIndex), "腿部结束帧索引不能小于0。");
        }

        StartLegFrameIndex = startLegFrameIndex;
        EndLegFrameIndex = endLegFrameIndex;
    }

    /// <summary>
    /// 更新当前帧的腿部帧表现。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    /// <param name="actionFrame">当前动作帧。</param>
    /// <param name="nodeProgress">节点播放进度。</param>
    public override void Update(ActionModPlayer actionPlayer, int actionFrame, float nodeProgress)
    {
        Player player = actionPlayer.Player;
        int legFrameIndex = (int)MathF.Round(Lerp(StartLegFrameIndex, EndLegFrameIndex, nodeProgress));
        player.legFrameCounter = 0.0;
        player.legFrame.Y = player.legFrame.Height * legFrameIndex;
    }
}

/// <summary>
/// 在指定动作帧触发一次本地回调，这个节点只执行一帧。
/// </summary>
public class CallbackActionNode : ActionNode
{
    private readonly Action<ActionModPlayer, int, float> callback;

    /// <summary>
    /// 创建指定动作帧触发的本地回调节点。
    /// </summary>
    /// <param name="triggerFrame">触发回调的动作帧。</param>
    /// <param name="callback">需要执行的本地回调。</param>
    public CallbackActionNode(int triggerFrame, Action<ActionModPlayer, int, float> callback) : base(triggerFrame, triggerFrame + 1)
    {
        this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>
    /// 在节点生效帧执行本地回调。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    /// <param name="actionFrame">当前动作帧。</param>
    /// <param name="nodeProgress">节点播放进度。</param>
    public override void Update(ActionModPlayer actionPlayer, int actionFrame, float nodeProgress)
    {
        if (actionPlayer.Player != Main.LocalPlayer)
        {
            return;
        }

        callback(actionPlayer, actionFrame, nodeProgress);
    }
}

/// <summary>
/// 在指定动作帧触发一次射击,这个节点只执行一帧
/// </summary>
public class ShootActionNode : ActionNode
{
    private readonly int projToShoot;
    private readonly Func<Player, Vector2> getPosition;
    private readonly int damage;
    private readonly float knockback;
    
    private readonly Func<Player, Vector2> getVelocity;

    /// <summary>
    /// 创建指定动作帧触发的射击节点。
    /// </summary>
    /// <param name="triggerFrame">触发射击的动作帧。</param>
    /// <param name="projToShoot">需要发射的弹幕类型。</param>
    /// <param name="position">发射位置</param>
    /// <param name="damage">伤害</param>
    /// <param name="knockback">击退</param>
    /// <param name="getVelocity">获取发射速度的函数。</param>
    public ShootActionNode(int triggerFrame, int projToShoot,Func<Player, Vector2> getPosition, int damage,float knockback, Func<Player, Vector2> getVelocity ) : base(triggerFrame, triggerFrame + 1)
    {

        this.projToShoot = projToShoot;
        this.getPosition = getPosition;
        this.getVelocity = getVelocity;
        this.damage = damage;
        this.knockback = knockback;
    }

    /// <summary>
    /// 在节点生效帧执行射击。
    /// </summary>
    /// <param name="actionPlayer">播放动作的玩家组件。</param>
    /// <param name="actionFrame">当前动作帧。</param>
    /// <param name="nodeProgress">节点播放进度。</param>
    public override void Update(ActionModPlayer actionPlayer, int actionFrame, float nodeProgress)
    {
        Player player = actionPlayer.Player;
        if(player!=Main.LocalPlayer)return;
        AnimAction.ShootFromAction(player, projToShoot, getPosition(player),getVelocity(player), damage,knockback);
    }
}