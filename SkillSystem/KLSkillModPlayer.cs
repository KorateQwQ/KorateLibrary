using KL.Configs;
using KL.DamageSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace KL.SkillSystem;

public abstract class  KLSkillModPlayer : ModPlayer
{
    public int MaxSkillSlot = 6;
    public int SkillPoint { get; set; }

    /// <summary>
    /// 装备在技能栏的技能
    /// </summary>
    public List<Skill> ActiveSkill = new (6);
    
    /// <summary>
    /// 已解锁的所有技能（解锁的技能默认为一级），但不一定在技能栏内
    /// </summary>
    public Dictionary<string,Skill> UnlockedSkill = new();
    
    private List<Skill> SkillForSave = new();

    public override void Load()
    {
        KLSkillManager.OnSkillsUpdated += OnSkillsUpdated;
        KLSkillManager.OnSkillsUIUpdated += OnSkillsUIUpdated;
        base.Load();
    }
    
    /// <summary>
    /// 技能UI更新事件, 当技能栏中的技能发生变化时触发, 比如拖动技能到技能栏中，交换技能
    /// </summary>
    public virtual void OnSkillsUpdated()
    {
        PrintText("KL技能信息更新");
    }

    /// <summary>
    /// 默认以60fps的帧率更新技能cd
    /// </summary>
    public override void PostUpdate()
    {
        foreach (var skill in ActiveSkill)
        {
            skill?.UpdateCD(1/60f);
        }
        base.PostUpdate();
    }

    public override void ResetEffects()
    {
        //SkillPoint = 30;
        if (UnlockedSkill != null)
        {
            foreach (var skillpair in UnlockedSkill)
            {
                skillpair.Value?.ModSkill?.ResetEffects(Player);
            }
        }

        base.ResetEffects();
    }

    public override void UpdateEquips()
    {
        if (UnlockedSkill != null)
        {
            foreach (var skillpair in UnlockedSkill)
            {
                skillpair.Value?.ModSkill?.UpdateEquips(Player);
            } 
        }
        base.UpdateEquips();
    }

    /// <summary>
    /// 技能UI更新事件, 当UI信息发生变化时触发，比如技能栏增加事件等
    /// </summary>
    protected virtual void OnSkillsUIUpdated()
    {   
    }

    
    /// <summary>
    /// 解锁一个技能
    /// </summary>
    /// <param name="skill"></param>
    public virtual void UnlockSkill(Skill skill)
    {
        if (skill?.ModSkill == null) return;
        UnlockedSkill ??= new();

        if(UnlockedSkill.ContainsKey(skill.ModSkill.GetType().Name))return;

        skill.Level = Math.Max(skill.Level, 1);
        if(UnlockedSkill.TryAdd(skill.ModSkill.GetType().Name, skill))
        {
            skill.BasicStatus = Skill.SKillBasicStatus.UnLock;
            skill.ModSkill.OnUnlockSkillAdded();
        }
    }

    /// <summary>
    /// 回退一个解锁的技能的状态到未解锁状态。
    /// </summary>
    /// <param name="skill"></param>
    public virtual void LockSkill(Skill skill)
    {

        if (skill?.ModSkill == null) return;
        UnlockedSkill ??= new();

        UnlockedSkill.TryGetValue(skill.ModSkill.GetType().Name,out Skill resultSkill);
        ModSkill modSkill = resultSkill?.ModSkill;
        if (modSkill is not { BasicStatus: Skill.SKillBasicStatus.UnLock})return;

        if (UnlockedSkill.Remove(skill.ModSkill.GetType().Name))
        {
            skill.ModSkill.BasicStatus = Skill.SKillBasicStatus.Lock;
            skill.ModSkill.OnLockSkill();
        }
    }

    public virtual bool CanCostSkillPoint(int amount)
    {
        return amount <= 0 || SkillPoint >= amount;
    }

    public virtual bool TryCostSkillPoint(int amount)
    {
        if (!CanCostSkillPoint(amount))
        {
            return false;
        }

        if (amount > 0)
        {
            SkillPoint -= amount;
        }

        return true;
    }
    
    
    public override void SaveData(TagCompound tag)
    {
        tag["MaxSkillSlot"] = MaxSkillSlot;
        tag["SkillPoint"] = SkillPoint;

        if (UnlockedSkill is { Count: > 0 })
        {
            List<Skill> unlockSkillForSave = new(UnlockedSkill.Count);
            foreach (var skillPair in UnlockedSkill)
            {
                if (skillPair.Value != null)
                {
                    unlockSkillForSave.Add(skillPair.Value);
                }
            }

            if (unlockSkillForSave.Count > 0)
            {
                tag["UnlockSkill"] = unlockSkillForSave;
            }
        }

        //技能栏可以为空，但是保存不允许为空，因此只保存非空的技能，读取时根据skillslot判定是否为空
        SkillForSave = new List<Skill>();
        if (ActiveSkill is { Count: > 0 })
        {
            foreach (var skill in ActiveSkill)
            {
                if (skill != null)
                {
                    SkillForSave.Add(skill);
                }
            }
        }

        if(SkillForSave is {Count: >0})tag["SkillForSave"] = SkillForSave;
        base.SaveData(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        var name = Player.name;
        tag.TryGet("MaxSkillSlot", out MaxSkillSlot);
        tag.TryGet("SkillPoint", out int skillPoint);
        SkillPoint = skillPoint;

        UnlockedSkill = new();
        tag.TryGet("UnlockSkill", out List<Skill> unlockSkillForSave);
        if (unlockSkillForSave is { Count: > 0 })
        {
            foreach (var skill in unlockSkillForSave)
            {
                if (skill?.ModSkill == null) continue;
                skill.Level = Math.Max(skill.Level, 1);
                skill.ModSkill.BasicStatus = Skill.SKillBasicStatus.UnLock;
                UnlockedSkill[skill.ModSkill.GetType().Name] = skill;
            }
        }

        tag.TryGet("SkillForSave",out SkillForSave);
        SkillForSave ??= new();
        ActiveSkill = new(MaxSkillSlot);
        for (int i = 0; i < MaxSkillSlot; i++)
        {
            ActiveSkill.Add(null);
        }

        if (SkillForSave is { Count: > 0 })
        {
            foreach (var skill in SkillForSave)
            {
                if (skill?.ModSkill == null) continue;
                int index = skill.SkillSlot;
                if (index >= 0 && index < MaxSkillSlot)
                {
                    string skillTypeName = skill.ModSkill.GetType().Name;
                    skill.Level = Math.Max(skill.Level, 1);
                    skill.ModSkill.BasicStatus = Skill.SKillBasicStatus.UnLock;
                    UnlockedSkill[skillTypeName] = skill;
                    ActiveSkill[index] = skill;
                }
            }
        }
        
        base.LoadData(tag);
    }

    public virtual void UseSkill(int index=0,IEntitySource source = null)
    {
        if (index >= 0 && index < ActiveSkill.Count)
        {
            ActiveSkill[index].UseSkill(source);
        }
    }
    /// <summary>
    /// 进入世界时自动初始化技能栏信息，技能栏可以为空。
    /// </summary>
    public override void OnEnterWorld()
    {
        if ((ActiveSkill == null || ActiveSkill.Count == 0)&&Main.myPlayer==Main.LocalPlayer.whoAmI)
        {
            ActiveSkill = new(MaxSkillSlot);
            for (int i = 0; i < MaxSkillSlot; i++)
            {
                ActiveSkill.Add(null);
            }
        }
        base.OnEnterWorld();
    }
}