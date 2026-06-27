using KL.SkillSystem.SilkyUI;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace KL.SkillSystem;

[SkillUIInfo(State = 1, Pixels = 400)]
public class TestSkill : ModSkill
{
    public override SkillUnlockCondition UnlockCondition => SkillUnlockCondition.ByItemsAndSkillPoint(
        3,
        new SkillUnlockItem(ItemID.Gel, 25),
        new SkillUnlockItem(ItemID.FallenStar, 2));

    public override Asset<Texture2D> SkillIcon =>_skillIcon ??= ModContent.Request<Texture2D>("KL/SkillSystem/冰锥", AssetRequestMode.ImmediateLoad);
    public override void Initialize()
    {
        Skill.MaxCD = 120;
        base.Initialize();
    }

    public override void PostDrawSkillIcon(Vector2 position, Vector2 scale,Color color, Effect effect = null)
    {
        EndBeginDrawUI(1,1,shader: effect);
        Asset<Texture2D> SkillIconEffect = ModContent.Request<Texture2D>("KL/SkillSystem/风魔法特效", AssetRequestMode.ImmediateLoad);
        //DrawInScreen(SkillIconEffect.Value,position ,Color.White*1f,scale);
        EndBeginDrawUI();
        base.PostDrawSkillIcon(position, scale, color,effect);
    }
}