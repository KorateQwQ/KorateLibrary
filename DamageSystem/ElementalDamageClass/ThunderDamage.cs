namespace KL.DamageSystem.ElementalDamageClass;

public class ThunderDamage: ElementalDamage
{
    public override void Load()
    {
        DamageManager.RegisterDamageColor(this, new Color(255, 207, 73, 130));
        base.Load();
    }
}