
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace KL.SkillSystem;

/// <summary>
/// 表示一个技能实例，负责管理技能的CD、层数、图标等状态
/// </summary>
/// <remarks>
/// 该类实现了TagSerializable接口，支持序列化和反序列化
/// </remarks>
public class Skill : TagSerializable, ILoadable//ICustomSerializable
{
    #region 加载相关接口实现

    /// <summary>
    /// 加载技能资源
    /// </summary>
    /// <param name="mod">所属的Mod实例</param>
    public void Load(Mod mod)
    {
        //Console.WriteLine($"Load Skill By Mode: {mod.Name}, SkillType: {GetType().FullName}");
        UnloadSkillIcon = ModContent.Request<Texture2D>("KL/SkillSystem/UnloadSkillIcon", AssetRequestMode.ImmediateLoad);
    }

    public void Unload()
    {
        
    }
    
    //所有modskill会自我注册在此map中
    public static Dictionary<string,Skill> RegisterSkill = new();

    //之前存在于技能栏，但因为mod卸载后未加载的技能。
    private bool UnloadSkill = false;
    private static Asset<Texture2D> UnloadSkillIcon;
    private string _modSkillType;
    private TagCompound _modSkillData;
    public static readonly Func<TagCompound, Skill> DESERIALIZER = LoadSkill;
    
    public Mod Mod=>ModSkill?.Mod;
    
    /// <summary>
    /// 序列化技能数据
    /// </summary>
    /// <returns>包含技能数据的TagCompound</returns>
    public virtual TagCompound SerializeData()
    {
        TagCompound tag = new TagCompound();
        if (UnloadSkill)
        {
            tag.Add("modSkillType", _modSkillType);
            tag.Add("modSkillData", _modSkillData);
        }
        else if (ModSkill != null)
        {
            tag.Add("modSkillType", ModSkill.GetType().FullName);
            TagCompound modSkillData = new TagCompound();
            ModSkill.SerializeData(modSkillData);
            tag.Add("modSkillData", modSkillData);
        }
        return tag;
    }
    
    /// <summary>
    /// 从TagCompound加载技能实例
    /// </summary>
    /// <param name="tag">包含技能数据的TagCompound</param>
    /// <returns>加载后的Skill实例</returns>
    public static Skill LoadSkill(TagCompound tag)
    {
        Skill skill = new Skill();
        skill.DeserializeData(tag);
        return skill;
    }
    
    /// <summary>
    /// 反序列化技能数据
    /// </summary>
    /// <param name="tag">包含技能数据的TagCompound</param>
    protected virtual void DeserializeData(TagCompound tag)
    {
        if (tag.ContainsKey("modSkillType"))
        {
            
            string modSkillTypeName = tag.GetString("modSkillType");
            TagCompound modSkillData = tag.GetCompound("modSkillData");

            bool hasValidModSkill = modSkillTypeName != null;
            Type modSkillType = null;
            RegisterSkill.TryGetValue(modSkillTypeName, out Skill templateModSkill);
            modSkillType = templateModSkill?.ModSkill.GetType();

            if (modSkillType != null)// && typeof(ModSkill).IsAssignableFrom(modSkillType)
            {
                try
                {
                    var skill = Activator.CreateInstance(modSkillType);
                    // 创建ModSkill实例
                    ModSkill = (ModSkill)skill;
                    if (ModSkill != null)
                    {
                        ModSkill.Skill = this; // 设置反向引用
                        ModSkill.Initialize();
                        // 加载ModSkill的自定义数据
                        ModSkill.DeserializeData(modSkillData);
                    }
                }
                catch
                {
                    _modSkillType = modSkillTypeName;
                    _modSkillData = modSkillData;
                    UnloadSkill = true;
                }
            }
            else if(hasValidModSkill)
            {
                _modSkillType = modSkillTypeName;
                _modSkillData = modSkillData;
                UnloadSkill = true;
            }
        }
    }
    #endregion
    
    
    #region 技能图标
    private Asset<Texture2D> _skillIcon;
    
    /// <summary>
    /// 获取技能图标
    /// </summary>
    /// <remarks>
    /// 如果技能已卸载，返回卸载图标；否则返回ModSkill的图标或默认图标
    /// </remarks>
    public virtual Asset<Texture2D> SkillIcon
    {
        get
        {
            if(UnloadSkill)return UnloadSkillIcon;
            if (ModSkill is { SkillIcon: not null }) return ModSkill.SkillIcon;
            return _skillIcon ??= ModContent.Request<Texture2D>("KL/SkillSystem/冰锥", AssetRequestMode.ImmediateLoad);
        }
        set => _skillIcon = value;
    }
    #endregion

    public ModSkill ModSkill { get; internal set; }

    /// <summary>
    /// 技能基本状态,默认为Lock
    /// </summary>
    public enum SKillBasicStatus
    {
        /// <summary>
        /// 技能未解锁
        /// </summary>
        Lock = 0,
        /// <summary>
        /// 技能已解锁，解锁后默认一级
        /// </summary>
        UnLock = 1,
        /// <summary>
        /// 技能处于隐藏状态，需要通过游戏进程或者其他任务获得技能信息变为未解锁状态
        /// </summary>
        Hide = 2,
    }
    
    
    /// <summary>
    /// 技能是否已解锁
    /// </summary>
    public SKillBasicStatus BasicStatus
    {
        get => ModSkill.BasicStatus;
        set => ModSkill.BasicStatus = value;
    }

    /// <summary>
    /// 当前冷却时间
    /// </summary>
    public float CurrentCD
    {
        get => ModSkill.CurrentCD;
        set => ModSkill.CurrentCD = value;   
    }

    /// <summary>
    /// 最大冷却时间
    /// </summary>
    public float MaxCD
    {
        get => ModSkill.MaxCD;
        set => ModSkill.MaxCD = value;
    }

    /// <summary>
    /// 当前技能层数
    /// </summary>
    public int Stack
    {
        get => ModSkill.Stack;
        set => ModSkill.Stack = value;
    }

    /// <summary>
    /// 最大技能层数
    /// </summary>
    public int MaxStack
    {
        get => ModSkill.MaxStack;
        set => ModSkill.MaxStack = value;
    }

    /// <summary>
    /// 技能槽位置，如果未装备则为-1
    /// </summary>
    public int SkillSlot
    {
        get => ModSkill.SkillSlot;
        set => ModSkill.SkillSlot = value;
    }

    /// <summary>
    /// 是否已装备技能
    /// </summary>
    public bool IsEquipSkill => SkillSlot >= 0;

    /// <summary>
    /// 技能是否在冷却中
    /// </summary>
    /// <remarks>
    /// 即使层数只有1的技能也会消耗层数，因此只要层数不满意味着还在CD
    /// </remarks>
    public bool InCD => Stack < MaxStack;
    
    public int Level
    {
        get => ModSkill.Level;
        set => ModSkill.Level = Math.Max(value, 1);
    }
    
    /// <summary>
    /// 创建新的技能实例
    /// </summary>
    /// <param name="skillType">技能类型</param>
    /// <returns>新的Skill实例</returns>
    public static Skill NewSkill(Type skillType, Mod mod=null)
    {
        Skill result = new Skill();
        result.ModSkill =(ModSkill)Activator.CreateInstance(skillType);
        if(result.ModSkill != null&&mod!=null)result.ModSkill.Mod = mod;
        
        string FullName = skillType.FullName;
        string Name = skillType.Name;
        //Main.NewText($"NewSkill Info: FullName: {FullName}, Name: {Name}, GetType: {Type.GetType(FullName)}");
        if (result.ModSkill != null)
        {
            result.ModSkill.Skill = result;
        }
        result.ModSkill?.Initialize();

        
        return result;
    }
    
    public static bool IsSameSkillType(Skill skill1, Skill skill2)
    {
        if (skill1 == null || skill2 == null)
            return false;
        return skill1.ModSkill?.GetType() == skill2.ModSkill?.GetType();
    }
    
    /// <summary>
    /// 使用技能
    /// </summary>
    /// <param name="source">技能来源实体</param>
    public void UseSkill(IEntitySource source = null)
    {
        if(UnloadSkill)return;
        if (ModSkill.IsPassiveSkill) return;
        if(!ModSkill.PreUseSkill(source))return;
        if (Stack > 0)
        {
            if(CurrentCD<=0)CurrentCD = MaxCD;
            Stack--;
        }
    }

    /// <summary>
    /// 更新技能冷却时间
    /// </summary>
    public void UpdateCD(float deltaTime)
    {
        if(UnloadSkill)return;
        bool? shouldUpdate = ModSkill?.PreUpdateCD();
        if (shouldUpdate.HasValue && shouldUpdate.Value)
        {
            if (CurrentCD <= 0)
            {
                return;
            }
            CurrentCD -= deltaTime;
            if (CurrentCD < 0) CurrentCD = 0;
            //当前冷却时间小于等于0时，重置冷却时间，并且技能层数加1
            if (CurrentCD > 0) return;
            if (++Stack < MaxStack)
            {
                CurrentCD = MaxCD;
            }
        }
    }
    
}