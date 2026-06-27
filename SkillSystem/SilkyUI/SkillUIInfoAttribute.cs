namespace KL.SkillSystem.SilkyUI;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class SkillUIInfoAttribute : Attribute
{
    /// <summary>
    /// 技能在UI上所属的阶段，UI会根据最大的阶段分为X行，并且从上往下排列所有技能，最顶部技能阶段为0。
    /// </summary>
    public int State = 0;

    /// <summary>
    /// 技能在UI上所属的像素位置，UI会根据像素位置进行排序。
    /// </summary>
    public float Pixels = 0;

    /// <summary>
    /// 技能tag，可以用此标记做一些便捷的区分。
    /// </summary>
    public string Tag = "";
    

}