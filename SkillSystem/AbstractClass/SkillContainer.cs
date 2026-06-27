using System.Linq;
using KL.SkillSystem.SilkyUI;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;

namespace KL.SkillSystem.AbstractClass;

public abstract class SkillContainer : BaseBody, IDraggableUI
{
    public UIView DragUI => this;

    public bool IsDragging { get; set; }
    
    public Vector2 LastMousePosition { get; set; } = Vector2.Zero;
    
    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        ((IDraggableUI)this).StartDrag(Main.MouseScreen);
        base.OnLeftMouseDown(evt);
    }

    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        ((IDraggableUI)this).StopDrag();
        base.OnLeftMouseUp(evt);
    }

    protected override void Update(GameTime gameTime)
    {
        ((IDraggableUI)this).UpdateDrag(); // 调用接口的拖拽更新
        base.Update(gameTime);
    }
    
    /// <summary>
    /// 自定义技能槽的类型，由具体的技能容器自己实现
    /// </summary>
    /// <param name="icon">不传递则为一个空的技能槽，否则有默认技能图标</param>
    /// <returns></returns>
    protected abstract SkillSlot CreateSkillSlot(SkillIcon icon = null);

    public virtual int GetSkillSlotNum()
    {
        return Children.Count;
    }

    public virtual int GetAvailableSkillNum()
    {
        int num = 0;
        foreach (var slot in Children)
        {
            if (slot is SkillSlot skillSlot)
            {
                if(skillSlot.HasSkill)num++;
            }
        }
        return num;
    }

    public virtual SkillSlot GetLastSkillSlot()
    {
        SkillSlot result = null;
        foreach (var child in Children)
        {
            if(child is SkillSlot skillSlot)result = skillSlot;
        }
        return result;
    }


}