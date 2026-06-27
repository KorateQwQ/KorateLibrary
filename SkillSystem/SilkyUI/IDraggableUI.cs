using KL.SkillSystem.AbstractClass;
using SilkyUIFramework.Elements;

namespace KL.SkillSystem.SilkyUI;

public interface IDraggableUI
{
    UIView DragUI { get; }
    /// <summary>
    /// 是否正在拖拽中
    /// </summary>
    bool IsDragging { get; set; }
    
    /// <summary>
    /// 上一次鼠标位置（用于计算偏移量）
    /// </summary>
    Vector2 LastMousePosition { get; set; }

    /// <summary>
    /// 检查是否有与鼠标相交的可拖拽子元素
    /// </summary>
    /// <param name="mousePosition">鼠标位置</param>
    /// <returns>是否可拖拽</returns>
    bool CanDragAt(Vector2 mousePosition)
    {
        UIView mouseUI = DragUI.GetElementAt(mousePosition);
        if (mouseUI is IDraggableUI &&  mouseUI!=DragUI)
        {
            return false;
        }
        if(mouseUI.IgnoreMouseInteraction) return false;

        if (mouseUI is SUIScrollbar) return false;
        if(mouseUI is DragScrollView or SUIScrollMask or SUIScrollContainer or SUIItemSlot) return false;
        //PrintText(mouseUI);
        return true;
    }

    /// <summary>
    /// 开始拖拽
    /// </summary>
    void StartDrag(Vector2 mousePosition)
    {
        if(!CanDragAt(mousePosition))return;
        
        if (!IsDragging)
        {
            IsDragging = true;
            LastMousePosition = Main.MouseScreen;
        }
    }

    /// <summary>
    /// 停止拖拽
    /// </summary>
    void StopDrag()
    {
        IsDragging = false;
    }

    /// <summary>
    /// 更新拖拽状态（在UI的Update方法中调用）
    /// </summary>
    void UpdateDrag()
    {
        if (IsDragging)
        {
            // 计算偏移量
            float offsetX = LastMousePosition.X - Main.MouseScreen.X;
            float offsetY = LastMousePosition.Y - Main.MouseScreen.Y;
            
            DragUI.SetLeft(DragUI.Left.Pixels - offsetX,DragUI.Left.Percent,DragUI.Left.Alignment);
            DragUI.SetTop(DragUI.Top.Pixels - offsetY,DragUI.Top.Percent,DragUI.Top.Alignment);
            LastMousePosition = Main.MouseScreen;
        }
    }
}
