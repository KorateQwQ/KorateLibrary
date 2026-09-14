using KL.ActionsSystem.TemplateActions;
using KL.Utils;
using KL.Utils.Net;

namespace KL.ActionsSystem;

/// <summary>
/// 负责保存和推进玩家当前动作状态。
/// </summary>
public class ActionModPlayer : KLModPlayer
{
    /// <summary>
    /// 当前正在播放的动作。
    /// </summary>
    public AnimAction CurrentAnimAction { get; private set; }

    /// <summary>
    /// 当前动作已经播放的帧数。
    /// </summary>
    public int CurrentActionElapsedFrame { get; private set; }

    /// <summary>
    /// 当前动作实例的运行时标识，用于拒绝属于旧动作的延迟状态包。
    /// </summary>
    public int CurrentActionToken { get; private set; }

    /// <summary>
    /// 当前动作时间轴是否正保持在某一帧。
    /// </summary>
    public bool IsActionHolding { get; private set; }

    /// <summary>
    /// 当前动作正在保持的帧；没有保持时为 -1。
    /// </summary>
    public int CurrentActionHoldFrame { get; private set; } = -1;

    /// <summary>
    /// 当前保持状态已经持续的游戏帧数。
    /// </summary>
    public int CurrentActionHoldTime { get; private set; }

    private int nextActionToken;
    private int currentHoldRevision;

    /// <summary>
    /// 当前动作的总时长，单位为帧。
    /// </summary>
    public int CurrentActionTotalFrame => CurrentAnimAction?.TotalFrame ?? 0;

    /// <summary>
    /// 当前动作所在帧。
    /// </summary>
    public int CurrentActionFrame
    {
        get
        {
            if (CurrentAnimAction == null || CurrentAnimAction.TotalFrame <= 0)
            {
                return 0;
            }

            return Math.Min(CurrentActionElapsedFrame, CurrentAnimAction.TotalFrame - 1);
        }
    }

    /// <summary>
    /// 当前动作整体播放进度。
    /// </summary>
    public float CurrentActionProgress
    {
        get
        {
            int totalFrame = CurrentActionTotalFrame;
            if (totalFrame <= 0)
            {
                return 0f;
            }

            float progress = CurrentActionElapsedFrame / (float)totalFrame;
            if (progress > 1f)
            {
                return 1f;
            }

            return progress;
        }
    }

    /// <summary>
    /// 当前是否正在播放动作。
    /// </summary>
    public bool HasAction => CurrentAnimAction != null;
    
    /// <summary>
    /// 开始播放指定动作。
    /// </summary>
    /// <param name="animAction">需要播放的动作。</param>
    /// <param name="interruptCurrentAction">是否中断当前动作。</param>
    /// <param name="rotation">动作开始时同步的方向角。</param>
    /// <returns>动作是否成功开始播放。</returns>
    public bool StartAction(AnimAction animAction, bool interruptCurrentAction = true, float rotation = 0f)
    {
        if (animAction == null)
        {
            return false;
        }

        int animActionId = animAction.TypeId;
        int actionToken = CreateNextActionToken();
        if (!StartActionLocal(animAction, interruptCurrentAction, rotation, actionToken))
        {
            return false;
        }

        RPC("StartActionById", [animActionId, animAction.TotalFrame, interruptCurrentAction, rotation, actionToken], KLNetModule.NetSendType.ClientToAll);
        return true;
    }

    /// <summary>
    /// 通过动画动作类型 id 开始播放动作。
    /// </summary>
    /// <param name="animActionId">动画动作类型 id。</param>
    /// <param name="totalFrame">动作总时长，单位为帧。</param>
    /// <param name="interruptCurrentAction">是否中断当前动作。</param>
    /// <param name="rotation">动作开始时同步的方向角。</param>
    public void StartActionById(int animActionId, int totalFrame, bool interruptCurrentAction, float rotation, int actionToken)
    {
        if (CurrentAnimAction != null && CurrentActionToken == actionToken)
        {
            return;
        }

        if (!AnimActionRegistry.TryCreate(animActionId, out AnimAction animAction))
        {
            return;
        }

        try
        {
            animAction.SetTotalFrame(totalFrame);
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }

        StartActionLocal(animAction, interruptCurrentAction, rotation, actionToken);
    }

    private bool StartActionLocal(AnimAction animAction, bool interruptCurrentAction, float rotation, int actionToken)
    {
        if (animAction == null || animAction.TotalFrame <= 0 || actionToken <= 0)
        {
            return false;
        }

        if (CurrentAnimAction != null)
        {
            if (!interruptCurrentAction)
            {
                return false;
            }

            EndCurrentAction(true);
        }

        CurrentAnimAction = animAction;
        CurrentActionElapsedFrame = 0;
        CurrentActionToken = actionToken;
        ResetHoldState();
        CurrentAnimAction.SetStartRotation(rotation);
        if (animAction.UseItemTime)
        {
            Player.itemAnimation = animAction.TotalFrame;
            Player.itemTime = animAction.TotalFrame;
            Item heldItem = Player.HeldItem;
            if (heldItem != null && !heldItem.IsAir)
            {
                heldItem.useTime = animAction.TotalFrame;
                heldItem.useAnimation = animAction.TotalFrame;
            }
        }

        CurrentAnimAction.OnStart(this);
        return true;
    }

    private int CreateNextActionToken()
    {
        unchecked
        {
            nextActionToken++;
        }

        if (nextActionToken <= 0)
        {
            nextActionToken = 1;
        }

        return nextActionToken;
    }

    /// <summary>
    /// 请求当前动作进入或退出保持状态，并将结果同步到其他端。
    /// 只有动作所属的本地玩家可以发起请求。
    /// </summary>
    /// <param name="holding">true 为进入保持，false 为退出保持。</param>
    /// <returns>是否产生了新的保持状态。</returns>
    public bool SetCurrentActionHold(bool holding)
    {
        int holdFrame = holding ? CurrentActionFrame : CurrentActionHoldFrame;
        return SetCurrentActionHold(holding, holdFrame);
    }

    /// <summary>
    /// 请求当前动作在指定帧进入或退出保持状态，并将结果同步到其他端。
    /// </summary>
    public bool SetCurrentActionHold(bool holding, int holdFrame)
    {
        if (Player.whoAmI != Main.myPlayer || CurrentAnimAction == null)
        {
            return false;
        }

        if (holdFrame < 0 || holdFrame >= CurrentAnimAction.TotalFrame)
        {
            return false;
        }

        if (holding == IsActionHolding && (!holding || CurrentActionHoldFrame == holdFrame))
        {
            return false;
        }

        int revision = currentHoldRevision + 1;
        int actionToken = CurrentActionToken;
        if (!ApplyActionHoldStateLocal(actionToken, revision, holdFrame, holding))
        {
            return false;
        }

        RPC(nameof(SyncActionHoldState), [actionToken, revision, holdFrame, holding], KLNetModule.NetSendType.ClientToAll);
        return true;
    }

    /// <summary>
    /// 应用由动作拥有者同步的保持状态。
    /// </summary>
    public void SyncActionHoldState(int actionToken, int revision, int holdFrame, bool holding)
    {
        ApplyActionHoldStateLocal(actionToken, revision, holdFrame, holding);
    }

    private bool ApplyActionHoldStateLocal(int actionToken, int revision, int holdFrame, bool holding)
    {
        if (CurrentAnimAction == null || CurrentActionToken != actionToken ||
            revision <= currentHoldRevision || holdFrame < 0 || holdFrame >= CurrentAnimAction.TotalFrame)
        {
            return false;
        }

        currentHoldRevision = revision;

        if (holding)
        {
            if (IsActionHolding)
            {
                CurrentAnimAction.OnHoldExit(this, CurrentActionHoldFrame);
            }

            CurrentActionElapsedFrame = holdFrame;
            EnterActionHoldLocal(holdFrame);
            return true;
        }

        if (IsActionHolding)
        {
            CurrentAnimAction.OnHoldExit(this, CurrentActionHoldFrame);
        }

        IsActionHolding = false;
        CurrentActionHoldFrame = -1;
        CurrentActionHoldTime = 0;

        // 保持帧已经完整执行过；解除时原子地越过它，避免再次自动进入保持。
        int resumeFrame = holdFrame + 1;
        CurrentActionElapsedFrame = Math.Max(CurrentActionElapsedFrame, resumeFrame);
        RestoreRemainingItemTime(resumeFrame);
        return true;
    }

    private void EnterActionHoldLocal(int holdFrame)
    {
        IsActionHolding = true;
        CurrentActionHoldFrame = holdFrame;
        CurrentActionHoldTime = 0;
        CurrentActionElapsedFrame = holdFrame;
        CurrentAnimAction.OnHoldEnter(this, holdFrame);
    }

    private void RestoreRemainingItemTime(int resumeFrame)
    {
        if (CurrentAnimAction?.UseItemTime != true)
        {
            return;
        }

        int remainingFrame = Math.Max(0, CurrentAnimAction.TotalFrame - resumeFrame);
        Player.itemAnimation = remainingFrame;
        Player.itemTime = remainingFrame;
    }

    private void ResetHoldState()
    {
        IsActionHolding = false;
        CurrentActionHoldFrame = -1;
        CurrentActionHoldTime = 0;
        currentHoldRevision = 0;
    }

    /// <summary>
    /// 中断当前正在播放的动作。
    /// </summary>
    public void InterruptCurrentAction()
    {
        EndCurrentAction(true);
    }

    /// <summary>
    /// 应用当前动作的帧表现效果。
    /// </summary>
    public override void FrameEffects()
    {
        CurrentAnimAction?.ApplyFrameEffects(this, CurrentActionFrame, CurrentAnimAction.GetProgress(CurrentActionFrame));

        base.FrameEffects();
    }

    /// <summary>
    /// 推进当前动作的播放状态。
    /// </summary>
    public override void PostUpdate()
    {
        if (IsLeftClick())
        {
            //StartAction(new FocusCast(),true);
        }

        if (Main.mouseLeft)
        {
            //Player.SetCompositeArmFront(true,Player.CompositeArmStretchAmount.Full,0);
            //Player.HandPosition += new Vector2(-10);

        }
        
        //layer.legFrame.Y = Player.legFrame.Height * 9;


        UpdateCurrentAction();
        base.PostUpdate();
    }

    private void UpdateCurrentAction()
    {
        if (CurrentAnimAction == null)
        {
            return;
        }

        AnimAction animAction = CurrentAnimAction;

        if (IsActionHolding)
        {
            int holdFrame = CurrentActionHoldFrame;
            if (animAction.UseItemTime)
            {
                Player.itemAnimation = 2;
                Player.itemTime = 2;
            }

            animAction.OnHoldUpdate(this, holdFrame, CurrentActionHoldTime);
            CurrentActionHoldTime++;

            if (!ReferenceEquals(CurrentAnimAction, animAction))
            {
                return;
            }

            if (Player.whoAmI == Main.myPlayer && IsActionHolding &&
                !animAction.ShouldContinueHolding(this, holdFrame))
            {
                SetCurrentActionHold(false, holdFrame);
            }

            // 解除保持的方法已经把时间轴推进到下一帧，本 tick 不再更新新帧。
            return;
        }

        if (animAction.IsFinished(CurrentActionElapsedFrame))
        {
            EndCurrentAction(false);
            return;
        }

        int actionFrame = CurrentActionFrame;
        animAction.Update(this, actionFrame, animAction.GetProgress(actionFrame));

        if (!ReferenceEquals(CurrentAnimAction, animAction) || IsActionHolding)
        {
            return;
        }

        // 自动保持点的普通时间轴逻辑只执行一次，之后改走 OnHoldUpdate。
        if (animAction.IsAutomaticHoldPoint(this, actionFrame))
        {
            EnterActionHoldLocal(actionFrame);

            // 若拥有者在到达保持点前已经松开输入，当帧就同步解除，不额外停留一帧。
            if (Player.whoAmI == Main.myPlayer &&
                !animAction.ShouldContinueHolding(this, actionFrame))
            {
                SetCurrentActionHold(false, actionFrame);
            }

            return;
        }

        CurrentActionElapsedFrame++;
    }

    private void EndCurrentAction(bool interrupted)
    {
        if (CurrentAnimAction == null)
        {
            return;
        }

        AnimAction animAction = CurrentAnimAction;
        if (IsActionHolding)
        {
            animAction.OnHoldExit(this, CurrentActionHoldFrame);
        }

        if (interrupted)
        {
            animAction.OnInterrupt(this);
        }
        else
        {
            animAction.OnFinish(this);
        }

        CurrentAnimAction = null;
        CurrentActionElapsedFrame = 0;
        CurrentActionToken = 0;
        ResetHoldState();
    }
}
