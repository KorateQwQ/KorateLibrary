using Terraria;

namespace KL.ActionsSystem.TemplateActions;

/// <summary>
/// 聚焦施法动作。
/// </summary>
public sealed class FocusCast : AnimAction
{

    /// <summary>
    /// 创建聚焦施法动作实例。
    /// </summary>
    public FocusCast() : base(120)
    {
        AddNode(new ArmActionNode(
                0,
                10,
                ActionArmType.Front,
                Player.CompositeArmStretchAmount.Full,
                Player.CompositeArmStretchAmount.ThreeQuarters,
                0f,
                -1.8f))
            .AddNode(new ArmActionNode(
                10,
                20,
                ActionArmType.Front,
                Player.CompositeArmStretchAmount.ThreeQuarters,
                Player.CompositeArmStretchAmount.Quarter,
                -1.8f,
                -2.0f))
            .AddNode(new ArmActionNode(
                20,
                25,
                ActionArmType.Front,
                Player.CompositeArmStretchAmount.Quarter,
                Player.CompositeArmStretchAmount.Full,
                -2.0f,
                1.0f))
            .AddNode(new ArmActionNode(
                25,
                35,
                ActionArmType.Front,
                Player.CompositeArmStretchAmount.Full,
                Player.CompositeArmStretchAmount.Full,
                1.0f,
                0.0f))
            /*.AddNode(new ArmActionNode(
                0,
                30,
                ActionArmType.Back,
                Player.CompositeArmStretchAmount.ThreeQuarters,
                Player.CompositeArmStretchAmount.ThreeQuarters,
                0.3f,
                0.3f))*/;
    }
}
