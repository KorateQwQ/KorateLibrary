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
        if (!StartActionLocal(animAction, interruptCurrentAction, rotation))
        {
            return false;
        }

        RPC("StartActionById", [animActionId, interruptCurrentAction, rotation], KLNetModule.NetSendType.ClientToAll);
        return true;
    }

    /// <summary>
    /// 通过动画动作类型 id 开始播放动作。
    /// </summary>
    /// <param name="animActionId">动画动作类型 id。</param>
    /// <param name="interruptCurrentAction">是否中断当前动作。</param>
    /// <param name="rotation">动作开始时同步的方向角。</param>
    public void StartActionById(int animActionId, bool interruptCurrentAction = true, float rotation = 0f)
    {
        if (CurrentAnimAction != null &&
            CurrentAnimAction.TypeId == animActionId &&
            CurrentActionElapsedFrame == 0)
        {
            return;
        }

        if (!AnimActionRegistry.TryCreate(animActionId, out AnimAction animAction))
        {
            return;
        }

        StartActionLocal(animAction, interruptCurrentAction, rotation);
    }

    private bool StartActionLocal(AnimAction animAction, bool interruptCurrentAction = true, float rotation = 0f)
    {
        if (animAction == null || animAction.TotalFrame <= 0)
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
        CurrentAnimAction.SetStartRotation(rotation);
        if (animAction.UseItemTime)
        {
            Player.itemAnimation = animAction.TotalFrame;
            Player.itemTime = animAction.TotalFrame;
            Player.HeldItem.useTime = animAction.TotalFrame;
            Player.HeldItem.useAnimation = animAction.TotalFrame;
        }

        CurrentAnimAction.OnStart(this);
        return true;
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

        if (CurrentAnimAction.IsFinished(CurrentActionElapsedFrame))
        {
            EndCurrentAction(false);
            return;
        }

        CurrentAnimAction.Update(this, CurrentActionFrame, CurrentAnimAction.GetProgress(CurrentActionFrame));
        CurrentActionElapsedFrame++;
    }

    private void EndCurrentAction(bool interrupted)
    {
        if (CurrentAnimAction == null)
        {
            return;
        }

        AnimAction animAction = CurrentAnimAction;
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
    }
}