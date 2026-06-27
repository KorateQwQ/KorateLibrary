using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;


namespace KL.SkillSystem.SilkyUI;

/// <summary>
/// 技能表上的技能图标
/// </summary>
/// <param name="skill"></param>
public class PanelSkillIcon(Skill skill) : SUIImage,IDraggableUI
{
    public int SlotIndex => Parent is PreviewSkillSlotUI skillSlot ? skillSlot.SlotIndex : -1;
    
    public bool Choose = false;
    public bool IsDragging { get; set; }
    public UIView DragUI => this;
    public Vector2 LastMousePosition { get; set; } = Vector2.Zero;
    
    //技能可以与此技能栏交互。
    public PreviewSkillBarUI PrewViewSkillBar;
    public SkillPanelUI SkillPanelUI;
    
    bool mouseHovered;
    int unlockEffectTime = -1;
    bool unlockEffectTriggered;
    int unlockFailedShakeTime;
    Vector2 unlockFailedShakeOffset;
    
    public bool MouseHovered => mouseHovered;
    public bool IsUnlockEffectActive => unlockEffectTime >= 0;

    public float UnlockEffectAlpha
    {
        get
        {
            if (unlockEffectTime < 0)
            {
                return 0f;
            }

            return unlockEffectTime <= 10 ? unlockEffectTime / 10f : (20f - unlockEffectTime) / 10f;
        }
    }

    public Vector2 UnlockEffectCenter
    {
        get
        {
            Vector2 position = InnerBounds.Position;
            Vector2 size = (Vector2)InnerBounds.Size;
            Vector2 imageOriginalSize = ImageOriginalSize;
            Vector2 completeOffset = ImageOffset + size * ImagePercent + (size - imageOriginalSize * ImageScale) * ImageAlign;
            return position + completeOffset + new Vector2(Texture2D.Width() / 2f) * ImageScale;
        }
    }

    public static Asset<Texture2D> NullTexture;
    static Asset<Texture2D> LockTexture;
    static Asset<Texture2D> HideTexture;

    public override void OnMouseEnter(UIMouseEvent evt)
    {
        mouseHovered = true;
        base.OnMouseEnter(evt);
    }

    public override void OnMouseLeave(UIMouseEvent evt)
    {
        mouseHovered = false;
        base.OnMouseLeave(evt);
    }

    private PanelSlotUI GetOwnerPanelSlot()
    {
        UIView current = Parent;
        while (current != null)
        {
            if (current is PanelSlotUI panelSlot)
            {
                return panelSlot;
            }

            current = current.Parent;
        }

        return null;
    }

    public bool CanDragAt(Vector2 mousePosition)
    {
        if(Skill?.ModSkill == null)return false;
        //只有已经学习技能的技能才能拖动到技能栏
        return Skill.ModSkill.CanDragInSkillPanel();
    }

    public void UpdateDrag()
    {
        if (IsDragging)
        {

        }
    }

    public void TryUnlockFromFooter()
    {
        if (Skill?.ModSkill == null || Skill.BasicStatus != Skill.SKillBasicStatus.Lock || unlockEffectTime >= 0)
        {
            return;
        }

        if (SkillPanelUI?.TryUnlockPanelSkill(Skill) == true)
        {
            unlockEffectTime = 0;
            unlockEffectTriggered = false;
            PrintText("解锁技能");
        }
        else
        {
            unlockFailedShakeTime = 20;
            PrintText("解锁技能失败");
        }
    }

    public override void OnRightMouseClick(UIMouseEvent evt)
    {
        Skill?.ModSkill?.OnRightClickInSkillPanel();
        base.OnRightMouseClick(evt);
    }

    public Skill Skill = skill;
    
    protected bool ShouldDrawChooseHint => Choose;
    
    bool InCD=> Skill.InCD;
    
    protected override void OnInitialize()
    {
        NullTexture ??= ModContent.Request<Texture2D>("KL/SkillSystem/SilkyUI/NullSkill", AssetRequestMode.ImmediateLoad);
        LockTexture = ModContent.Request<Texture2D>("KL/SkillSystem/SilkyUI/Lock_Icon", AssetRequestMode.ImmediateLoad);
        HideTexture ??= ModContent.Request<Texture2D>("KL/SkillSystem/SilkyUI/QuestionMark_Icon", AssetRequestMode.ImmediateLoad);
        /*SetLeft(alignment:0.5f);
        SetTop(alignment:0.15f);*/
        Texture2D = NullTexture;
            
        //自适应子元素大小
        FitWidth = false;
        FitHeight = false;

        //圆角
        BorderRadius = new Vector4(4);

        //内边距
        Padding = new Margin(8);
        ImageAlign = new Vector2(0.5f);

        base.OnInitialize();
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        if(Skill?.ModSkill == null) return;
        GetOwnerPanelSlot()?.TryShowToolTip();
        if (Skill.BasicStatus == Skill.SKillBasicStatus.UnLock)
        {
            SkillPanelUI?.SetDraggingSkillIcon(this);
            ((IDraggableUI)this).StartDrag(Main.MouseScreen);
        }
        base.OnLeftMouseDown(evt);
    }

    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        if (IsDragging&&PrewViewSkillBar?.ActiveSkillList != null)
        {
            if (PrewViewSkillBar?.GetElementAt(Main.MouseScreen) is PreviewSkillSlotUI skillSlot) 
            {
                PrintText("装备技能");
                int index = skillSlot.SlotIndex;
                KLSkillManager.EquipSkill(PrewViewSkillBar.ActiveSkillList, Skill,index);
                PrintText(index);
            }
            else if (PrewViewSkillBar?.GetElementAt(Main.MouseScreen) is PanelSkillIcon basicSkill) //和技能发生交互
            {
                if (basicSkill == this)
                {
                    Main.NewText("是自己。不操作");
                }
                else
                {
                    if (Parent is PreviewSkillSlotUI skillSlot2)
                    {
                        Main.NewText("与另一个技能交换位置");
                        int? index1 = basicSkill.SlotIndex;
                        int index2 = skillSlot2.SlotIndex;
                        KLSkillManager.SwitchSkill(PrewViewSkillBar.ActiveSkillList,index1.Value,index2);
                        PrintText(index1);
                        PrintText(index2);
                    }
                }
            }
            else
            {
                if (Parent is PreviewSkillSlotUI skillSlot3)
                {
                    if (InCD)
                    {
                        Main.NewText("技能处于CD，不执行操作");
                    }
                    else
                    {
                        Main.NewText("将技能移出技能槽");
                        KLSkillManager.UnEquipSkill(PrewViewSkillBar.ActiveSkillList,skillSlot3.SlotIndex);
                        //skillModPlayer.RemoveSkillFromSkillBar(Skill.SkillSlot);
                    }
                }
                else
                {
                    Main.NewText("技能表技能未发生互动");
                }
            }
        }
        
        ((IDraggableUI)this).StopDrag();
        SkillPanelUI?.ClearDraggingSkillIcon(this);

        base.OnLeftMouseUp(evt);
    }

    protected override void Update(GameTime gameTime)
    {
        //BackgroundColor = Color.White*0;
        UpdateDrag(); // 调用接口的拖拽更新

        if (unlockEffectTime >= 0)
        {
            if (!unlockEffectTriggered && unlockEffectTime == 10)
            {
                Skill.ModSkill.OnUnlockSkill();
                unlockEffectTriggered = true;
                SkillToolTip.Instance?.RefreshUnlockFooter();
            }

            unlockEffectTime++;
            if (unlockEffectTime > 20)
            {
                unlockEffectTime = -1;
                unlockEffectTriggered = false;
            }
        }

        if (unlockFailedShakeTime > 0)
        {
            unlockFailedShakeTime--;
            unlockFailedShakeOffset = Main.rand.NextVector2Circular(3f, 3f);
        }
        else
        {
            unlockFailedShakeOffset = Vector2.Zero;
        }

        base.Update(gameTime);
    }

    public override void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Skill == null) return;
        //Skill.BasicStatus = Skill.SKillBasicStatus.UnLock;

        if (Parent is PreviewSkillSlotUI slot)
        {
            slot.SkillIsDragging = IsDragging;
        }

        var position = InnerBounds.Position;
        var size = (Vector2)InnerBounds.Size;

        var imageOriginalSize = ImageOriginalSize;
        var completeOffset = ImageOffset + size * ImagePercent + (size - imageOriginalSize * ImageScale) * ImageAlign;
        /*spriteBatch.Draw(Texture2D.Value, position + completeOffset, SourceRectangle,
            Color.Red, 0f, imageOriginalSize * ImageOriginPercent, ImageScale, 0f, 0f);*/

        Vector2 finalPosition = position + completeOffset + new Vector2(Texture2D.Width() / 2f) * ImageScale;
        Vector2 scale = ImageScale;
        base.Draw(gameTime, spriteBatch);
    
        bool needGrey = Skill.BasicStatus != Skill.SKillBasicStatus.UnLock;
        //needGrey = false;
        //EndBeginDrawUI(0,1,true,null,null);
        Color drawColor = Skill.BasicStatus==Skill.SKillBasicStatus.UnLock? Color.White: Color.White*0.5f;
        if (Skill.BasicStatus == Skill.SKillBasicStatus.Hide)
        {
            drawColor = Color.Black * 0.5f;
        }

        if(Skill.ModSkill.PreDrawSkillIcon(finalPosition, scale,drawColor,needGrey?grey:null))
        {
            if(needGrey)EndBeginDrawUI(0,1);
            DrawInScreen(Skill.SkillIcon.Value, finalPosition,drawColor);
        }

        if(Skill.BasicStatus == Skill.SKillBasicStatus.Lock)
        {
            Color failureColor = Color.Lerp(Color.White, Color.Red, unlockFailedShakeTime / 20f);
            DrawInScreen(LockTexture.Value, finalPosition + unlockFailedShakeOffset,unlockFailedShakeTime<=0?Color.White:failureColor,Vector2.One*0.5f);
        }
        else if(Skill.BasicStatus == Skill.SKillBasicStatus.Hide)
        {
            DrawInScreen(HideTexture.Value, finalPosition);
        }

        Skill.ModSkill.PostDrawSkillIcon(finalPosition,scale,drawColor,needGrey?grey:null);

        if (UnlockEffectAlpha > 0f) DrawInScreen(TextureAssets.MagicPixel.Value, finalPosition,color:new Color(1, 1, 1, UnlockEffectAlpha) * UnlockEffectAlpha,scale:new Vector2(59,0.059f));
        if(needGrey)EndBeginDrawUI();

    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
    }
}