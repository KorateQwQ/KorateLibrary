using KL.DamageSystem;
using KL.Drawing.Snippets;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace KL.SkillSystem;

public abstract class ModSkill : ILoadable
{
    #region 加载相关接口实现

    public virtual void Load(Mod mod)
    {
        var fullName = GetType().FullName;
        if (fullName != null) Skill.RegisterSkill.Add(fullName, Skill.NewSkill(GetType(),mod));
    }

    public virtual void Unload()
    {
    }

    public virtual void SerializeData(TagCompound tag)
    {
        tag.Add("BasicStatus",(byte)BasicStatus);
        tag.Add("CurrentCD",CurrentCD);
        tag.Add("MaxCD",MaxCD);
        tag.Add("Stack",Stack);
        tag.Add("MaxStack",MaxStack);
        tag.Add("SkillSlot",SkillSlot);
        tag.Add("Level",Level);
    }
    public virtual void DeserializeData(TagCompound tag)
    {
        if (tag.TryGet("BasicStatus", out byte basicStatus))
        {
            BasicStatus = (Skill.SKillBasicStatus)basicStatus;
        }
        tag.TryGet("CurrentCD",out CurrentCD);
        tag.TryGet("MaxCD",out MaxCD);
        tag.TryGet("Stack",out Stack);
        tag.TryGet("MaxStack",out MaxStack);
        tag.TryGet("SkillSlot",out SkillSlot);
        tag.TryGet("Level",out Level);
    }

    #endregion

    public Mod Mod { get; set; }
    public Skill Skill { get; set; }
    public Player Player => Main.LocalPlayer;

    #region 默认属性
    //未解锁的技能在Panel中呈现灰白，无法被拖拽。
    public Skill.SKillBasicStatus BasicStatus = Skill.SKillBasicStatus.Lock;

    public virtual bool IsPassiveSkill => false;
    
    public float CurrentCD = 0;
    public float MaxCD = 60;

    public int Stack = 1;
    public int MaxStack = 1;
    public int Level = 0;

    public virtual SkillUnlockCondition UnlockCondition { get; set; } =
        SkillUnlockCondition.ByItemsAndSkillPoint(10, new SkillUnlockItem(ItemID.Wood, 10));
    
    #endregion


    //这个技能如果被装备到技能槽中，则会有对应的slot位置，否则为-1
    public int SkillSlot = -1;
    
    protected Asset<Texture2D> _skillIcon;

    /// <summary>
    /// 技能默认路径
    /// </summary>
    protected string SkillTexturePath => (GetType().Namespace + "." + GetType().Name).Replace('.', '/');
    public virtual Asset<Texture2D> SkillIcon
    {
        get
        {
            return _skillIcon ??= ModContent.Request<Texture2D>(SkillTexturePath, AssetRequestMode.ImmediateLoad);
        }
        set { _skillIcon = value; }
    }

    
    /// <summary>
    /// 可以在Skill图标绘制前执行自己的绘制，也可以取消掉Skill的绘制。传入的effect为CD效果，默认为扇形倒计时。
    /// </summary>
    /// <param name="position"></param>
    /// <param name="scale"></param>
    /// <param name="effect"></param>
    /// <returns></returns>
    public virtual bool PreDrawSkillIcon(Vector2 position, Vector2 scale,Color color, Effect effect=null)
    {
        return true;
    }

    public virtual void PostDrawSkillIcon(Vector2 position, Vector2 scale,Color color,Effect effect=null)
    {
        
    }

    public virtual void Initialize()
    {
    }

    /// <summary>
    /// 所有已经解锁的技能会在ResetEffects时调用
    /// </summary>
    /// <param name="player"></param>
    public virtual void ResetEffects(Player player)
    {
    }

    /// <summary>
    /// 所有已经解锁的技能会在更新饰品之后的时机调用
    /// </summary>
    public virtual void UpdateEquips(Player player)
    {
    }

    /// <summary>
    /// 在变动CD前调用，返回false则不更新CD
    /// </summary>
    /// <returns></returns>
    public virtual bool PreUpdateCD()
    {
        return true;
    }

    public virtual bool PreUseSkill(IEntitySource source = null)
    {
        return true;
    }

    public virtual void OnLockSkill()
    {

    }

    public virtual void OnUnlockSkillAdded()
    {

    }

    /// <summary>
    /// 默认情况下，只有已解锁的技能才能被拖拽到技能面板中
    /// </summary>
    /// <returns></returns>
    public virtual bool CanDragInSkillPanel()
    {
        return BasicStatus == Skill.SKillBasicStatus.UnLock;
    }

    public virtual bool CanDragInSkillBar()
    {
        return true;
    }

    public virtual void OnRightClickInSkillPanel()
    {
        //PrintText(6);
    }

    public virtual void TryLevelUp()
    {
        Level++;
    }

    public virtual void TryLevelDown()
    {
        if(Level>1) Level--;
    }

    /// <summary>
    /// 判定是否可以解锁技能
    /// </summary>
    /// <returns></returns>
    public virtual bool TryUnlockSkill(KLSkillModPlayer skillPlayer)
    {
        return UnlockCondition.TryUnlock(skillPlayer, this);
    }

    /// <summary>
    /// 解锁技能时调用
    /// </summary>
    public virtual void OnUnlockSkill()
    {
    }

    public virtual string GetUnlockConditionDescription(KLSkillModPlayer skillPlayer)
    {
        return UnlockCondition.GetDescription(skillPlayer, this);
    }

    public virtual bool TryGetToolTip(ref string name, ref  string level, ref string desc)
    {
        //name = GetType().Name;
        level = $"Lv. {Level}";
        //desc = "造成100" +ElementType.Fire.GetIcon(offsetY:6,size:48) + "火元素伤害";

        /*if (BasicStatus == Skill.SKillBasicStatus.Lock)
        {
            string unlockConditionText = GetUnlockConditionDescription();
            if (!string.IsNullOrWhiteSpace(unlockConditionText))
            {
                desc += $"\n\n解锁条件:\n{unlockConditionText}";
            }
        }*/
        return true;
    }
}