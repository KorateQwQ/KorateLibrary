namespace KL.DamageSystem.ElementalDamageClass;

public class IceDamage: ElementalDamage
{
    public override void Load()
    {
        DamageManager.RegisterDamageColor(this, new Color(100, 200, 255, 150));
        base.Load();
    }
}